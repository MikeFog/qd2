using FogSoft.WinForm.Classes;
using Merlin.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Запись для «Отменить последнее» (docs/tasks/web-action-forms.md, этапы 5 и 6.6): выпуски, которые поставило последнее
/// размещение на странице акции — на «Рекламных окнах» (одна кампания или веер) или на «Модулях», — по одной записи на
/// акцию (§3.9, В-9). Новое размещение любого вида заменяет запись (и пустое — гасит её): кнопка «Отменить» видна только на
/// вкладке вида записи и не возвращается к чужому «последнему». Отмена (с любым итогом) запись снимает — как десктопный
/// список выпусков формы, который перезаписывается каждым запуском шаблона и обнуляется отменой. Отличие от десктопа:
/// «размещение» — любое (клик, «Разместить ролик»), а не только запуск шаблона (В-9).
///
/// Scoped, то есть живёт столько же, сколько вход в систему во вкладке (<see cref="UserSession"/>): уход на другой
/// экран запись не теряет, F5, обрыв соединения и другой пользователь — теряют (в базу запись не пишется, решение
/// плана: выпуски при этом остаются, снять их можно «Удалить выпуски»).
/// </summary>
public sealed class PlacementUndo
{
	/// <summary>
	/// Вид записи — что ставили: от него зависит, какая вкладка показывает «Отменить» и какая отмена снимает запись. Задаётся
	/// явно, а не выводится по данным: номера выпусков окон (Issue) и модулей (ModuleIssue) — разные счётчики и пересекаются,
	/// чужая отмена сняла бы не те выпуски.
	/// </summary>
	public enum Kind
	{
		/// <summary>Окна одной кампании (линейная сетка) — WindowsSource.Undo.</summary>
		Windows,

		/// <summary>Веер: выпуски отмеченных кампаний — RangeSource.Undo.</summary>
		Range,

		/// <summary>Модули — ModuleDaySource.Undo.</summary>
		Modules,
	}

	/// <summary>Выпуски, поставленные одним действием; создаётся только через RememberWindows / RememberRange / RememberModules.</summary>
	public sealed record Entry
	{
		internal Entry(int actionId, Kind kind, int campaignId, IReadOnlyList<WindowsSource.PlacedIssue> issues,
			IReadOnlyList<ModuleDaySource.PlacedModuleIssue> moduleIssues)
		{
			ActionId = actionId;
			Kind = kind;
			CampaignId = campaignId;
			Issues = issues;
			ModuleIssues = moduleIssues;
		}

		public int ActionId { get; }
		public Kind Kind { get; }

		/// <summary>Кампания записи окон; у веера и модулей — 0: кампании — в самих выпусках.</summary>
		public int CampaignId { get; }

		/// <summary>Выпуски окон и веера; у модулей пусто.</summary>
		public IReadOnlyList<WindowsSource.PlacedIssue> Issues { get; init; }

		/// <summary>Выпуски модулей (номер ModuleIssue, кампания, модуль, день, ролик); у окон и веера пусто.</summary>
		public IReadOnlyList<ModuleDaySource.PlacedModuleIssue> ModuleIssues { get; init; }

		/// <summary>Выпусков в записи — для «Отменить последнее добавление выпусков (N шт.)».</summary>
		public int Count => Issues.Count + ModuleIssues.Count;
	}

	private static readonly IReadOnlyList<WindowsSource.PlacedIssue> NoIssues = Array.Empty<WindowsSource.PlacedIssue>();
	private static readonly IReadOnlyList<ModuleDaySource.PlacedModuleIssue> NoModuleIssues = Array.Empty<ModuleDaySource.PlacedModuleIssue>();

	private readonly Dictionary<int, Entry> _byAction = new();
	private SecurityManager.User? _user;

	public PlacementUndo(UserSession session)
	{
		// Changed приходит и на смену языка — забываем только при смене пользователя: чужие выпуски следующему не достаются.
		_user = session.User;
		session.Changed += () =>
		{
			if (ReferenceEquals(session.User, _user))
				return;
			_user = session.User;
			_byAction.Clear();
		};
	}

	public Entry? Of(int actionId) => _byAction.GetValueOrDefault(actionId);

	/// <summary>Размещение в окнах одной кампании (линейная сетка).</summary>
	public void RememberWindows(int actionId, int campaignId, IReadOnlyList<WindowsSource.PlacedIssue> issues) =>
		Remember(new Entry(actionId, Kind.Windows, campaignId, issues, NoModuleIssues));

	/// <summary>Размещение в веере: выпуски отмеченных кампаний.</summary>
	public void RememberRange(int actionId, IReadOnlyList<WindowsSource.PlacedIssue> issues) =>
		Remember(new Entry(actionId, Kind.Range, 0, issues, NoModuleIssues));

	/// <summary>Размещение модулей: выпуски с номерами из ответа ModuleIssueIUD, только записанные (после коммита).</summary>
	public void RememberModules(int actionId, IReadOnlyList<ModuleDaySource.PlacedModuleIssue> issues) =>
		Remember(new Entry(actionId, Kind.Modules, 0, NoIssues, issues));

	/// <summary>
	/// Запомнить запись — в том числе то, что осталось от прерванной отмены (<c>entry with { Issues = … }</c>, вид прежний);
	/// пустая (всё отказано) гасит прежнюю запись, как пустой запуск шаблона в десктопе.
	/// </summary>
	public void Remember(Entry entry)
	{
		if (entry.Count > 0)
			_byAction[entry.ActionId] = entry;
		else
			_byAction.Remove(entry.ActionId);
	}

	/// <summary>
	/// Выпуски перенесли в другое окно (перенос пересоздаёт выпуск с новым номером): в записи окон или веера прежние номера
	/// заменяются новыми, остальное остаётся. Запись модулей не трогается: номера в ней — другого счётчика.
	/// </summary>
	public void Moved(int actionId, IEnumerable<KeyValuePair<int, WindowsSource.PlacedIssue>> moved)
	{
		if (!_byAction.TryGetValue(actionId, out Entry? entry) || entry.Kind == Kind.Modules)
			return;
		Dictionary<int, WindowsSource.PlacedIssue> byOld = moved.ToDictionary(x => x.Key, x => x.Value);
		_byAction[actionId] = entry with { Issues = entry.Issues.Select(i => byOld.GetValueOrDefault(i.IssueId, i)).ToList() };
	}

	public void Forget(int actionId) => _byAction.Remove(actionId);
}
