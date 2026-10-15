using FogSoft.WinForm.Classes;
using Merlin.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Запись для «Отменить последнее» (docs/tasks/web-action-forms.md, этап 5): выпуски, которые поставило последнее
/// размещение вкладки «Рекламные окна», по одной записи на акцию. Новое размещение заменяет запись (и пустое —
/// гасит её), отмена (с любым итогом) её снимает — как десктопный список выпусков формы, который перезаписывается
/// каждым запуском шаблона и обнуляется отменой. Отличие от десктопа: «размещение» — любое (клик, «Разместить
/// ролик»), а не только запуск шаблона (В-9).
///
/// Scoped, то есть живёт столько же, сколько вход в систему во вкладке (<see cref="UserSession"/>): уход на другой
/// экран запись не теряет, F5, обрыв соединения и другой пользователь — теряют (в базу запись не пишется, решение
/// плана: выпуски при этом остаются, снять их можно «Удалить выпуски»).
/// </summary>
public sealed class PlacementUndo
{
	/// <summary>Номер кампании в записи веера: выпуски разных кампаний, смотрит на неё тот же веер.</summary>
	public const int RangeCampaign = 0;

	/// <summary>
	/// Выпуски, поставленные одним действием: одной кампании акции (линейка) или нескольких отмеченных (веер,
	/// <see cref="Entry.CampaignId"/> = <see cref="RangeCampaign"/>; кампании — в самих выпусках).
	/// </summary>
	public sealed record Entry(int ActionId, int CampaignId, IReadOnlyList<WindowsSource.PlacedIssue> Issues)
	{
		public bool IsRange => CampaignId == RangeCampaign;
	}

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

	/// <summary>Запомнить последнее размещение; пустое (всё отказано) гасит прежнюю запись, как пустой запуск шаблона в десктопе.</summary>
	public void Remember(int actionId, int campaignId, IReadOnlyList<WindowsSource.PlacedIssue> issues)
	{
		if (issues.Count > 0)
			_byAction[actionId] = new Entry(actionId, campaignId, issues);
		else
			_byAction.Remove(actionId);
	}

	/// <summary>
	/// Выпуски перенесли в другое окно (перенос пересоздаёт выпуск с новым номером): в записи прежние номера
	/// заменяются новыми, остальное остаётся.
	/// </summary>
	public void Moved(int actionId, IEnumerable<KeyValuePair<int, WindowsSource.PlacedIssue>> moved)
	{
		if (!_byAction.TryGetValue(actionId, out Entry? entry))
			return;
		Dictionary<int, WindowsSource.PlacedIssue> byOld = moved.ToDictionary(x => x.Key, x => x.Value);
		_byAction[actionId] = entry with { Issues = entry.Issues.Select(i => byOld.GetValueOrDefault(i.IssueId, i)).ToList() };
	}

	public void Forget(int actionId) => _byAction.Remove(actionId);
}
