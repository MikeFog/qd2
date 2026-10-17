using FogSoft.WinForm.Classes;
using Merlin.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Память вкладки «Модули» на сеанс (docs/tasks/web-action-forms.md, этап 6.3), по акции: строки, добавленные
/// «Показать модули», и модульные кампании, которые вкладка сама создала постановкой в такую строку (В-21: опустевшую
/// кампанию, созданную вкладкой в этом сеансе, удаляют в той же транзакции — подэтапы 6.4–6.6).
///
/// Scoped, как <see cref="PlacementUndo"/>: уход на другой экран и обратно память сохраняет, F5, обрыв соединения и
/// другой пользователь — теряют (в базу не пишется). Строка без кампании так и хранится без номера кампании, когда
/// кампанию создала постановка: набор вкладки узнаёт строку отмеченной кампании по станции, типу оплаты и агентству
/// (ModuleDaySource.LoadPeriod), а если кампанию снимут с отметки или удалят, строка снова станет ожидающей, а не пропадёт.
/// </summary>
public sealed class ModuleRowsMemory
{
	private readonly Dictionary<int, List<ModuleDaySource.AddedRow>> _rows = new();
	private readonly Dictionary<int, HashSet<int>> _created = new();
	private SecurityManager.User? _user;

	public ModuleRowsMemory(UserSession session)
	{
		// Changed приходит и на смену языка — забываем только при смене пользователя.
		_user = session.User;
		session.Changed += () =>
		{
			if (ReferenceEquals(session.User, _user))
				return;
			_user = session.User;
			_rows.Clear();
			_created.Clear();
		};
	}

	/// <summary>Добавленные строки акции в порядке добавления.</summary>
	public IReadOnlyList<ModuleDaySource.AddedRow> Rows(int actionId) =>
		_rows.TryGetValue(actionId, out List<ModuleDaySource.AddedRow>? rows) ? rows : Array.Empty<ModuleDaySource.AddedRow>();

	/// <summary>Запомнить строки; такая же (ключ и кампания) уже есть — пропускается. Возвращает число новых.</summary>
	public int Add(int actionId, IEnumerable<ModuleDaySource.AddedRow> rows)
	{
		if (!_rows.TryGetValue(actionId, out List<ModuleDaySource.AddedRow>? list))
			_rows[actionId] = list = new List<ModuleDaySource.AddedRow>();
		int added = 0;
		foreach (ModuleDaySource.AddedRow row in rows)
			if (!list.Any(r => r.Key == row.Key && r.CampaignId == row.CampaignId))
			{
				list.Add(row);
				added++;
			}
		return added;
	}

	/// <summary>Кампанию создала постановка вкладки в этом сеансе (ModuleDaySource.PlacedModuleIssue.CampaignCreated).</summary>
	public void CampaignCreated(int actionId, int campaignId)
	{
		if (!_created.TryGetValue(actionId, out HashSet<int>? set))
			_created[actionId] = set = new HashSet<int>();
		set.Add(campaignId);
	}

	/// <summary>Кампании акции, созданные вкладкой в этом сеансе, — копией: «Удалить выпуски» (ModuleDaySource.Remove, В-21).</summary>
	public ISet<int> CreatedCampaigns(int actionId) =>
		_created.TryGetValue(actionId, out HashSet<int>? set) ? new HashSet<int>(set) : new HashSet<int>();

	/// <summary>
	/// Кампанию, созданную вкладкой, удалили вместе с последним выпуском (В-21) — она больше не «создана здесь». Строки без
	/// номера кампании набор снова покажет ожидающими; строки, привязанные к ней («Модули рекламной кампании»), становятся
	/// такими же ожидающими, иначе пропали бы вместе с кампанией.
	/// </summary>
	public void CampaignDeleted(int actionId, int campaignId)
	{
		if (_created.TryGetValue(actionId, out HashSet<int>? set))
			set.Remove(campaignId);
		if (!_rows.TryGetValue(actionId, out List<ModuleDaySource.AddedRow>? list))
			return;
		List<ModuleDaySource.AddedRow> bound = list.Where(r => r.CampaignId == campaignId).ToList();
		foreach (ModuleDaySource.AddedRow row in bound)
		{
			list.Remove(row);
			ModuleDaySource.AddedRow pending = row.WithoutCampaign();
			if (!list.Any(r => r.Key == pending.Key && r.CampaignId == null))
				list.Add(pending);
		}
	}
}
