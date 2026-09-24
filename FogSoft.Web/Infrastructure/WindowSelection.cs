using Merlin.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Выделение окон в сетке недели — как в таблицах Excel и Google Sheets: клик выбирает одно
/// окно, Shift-клик — прямоугольник от предыдущего клика, Ctrl-клик (⌘ на Mac) добавляет или
/// убирает окно. Решение владельца 2026-09-23 (docs/tasks/web-tariffgrid.md, §9); протягивание
/// мышью — позже, если понадобится. Состояние — только номера окон, поэтому переживает
/// перечитывание той же недели.
/// </summary>
public sealed class WindowSelection
{
	private readonly HashSet<int> _ids = new();
	private (int Row, int Day)? _anchor;

	public IReadOnlyCollection<int> Ids => _ids;
	public int Count => _ids.Count;

	public void Click(TariffWindowWeek week, TariffWindowCell cell, bool shift, bool ctrl)
	{
		(int Row, int Day)? position = Locate(week, cell.WindowId);
		if (position == null)
			return;

		if (shift && _anchor != null)
		{
			if (!ctrl)
				_ids.Clear();
			AddRectangle(week, _anchor.Value, position.Value);
			return;
		}

		if (ctrl)
		{
			if (!_ids.Remove(cell.WindowId))
				_ids.Add(cell.WindowId);
		}
		else
		{
			_ids.Clear();
			_ids.Add(cell.WindowId);
		}
		_anchor = position;
	}

	/// <summary>Вся строка времени недели.</summary>
	public void SelectRow(TariffWindowWeek week, TariffWindowRow row)
	{
		_ids.Clear();
		_anchor = null;
		int rowIndex = IndexOf(week, row);
		foreach (TariffWindowCell? cell in row.Cells)
			if (cell != null)
				_ids.Add(cell.WindowId);
		if (rowIndex >= 0)
			_anchor = (rowIndex, 0);
	}

	public void Clear()
	{
		_ids.Clear();
		_anchor = null;
	}

	/// <summary>После перечитывания недели — оставить только окна, которые в ней есть.</summary>
	public void Retain(TariffWindowWeek week)
	{
		var present = new HashSet<int>(Cells(week).Select(c => c.WindowId));
		_ids.IntersectWith(present);
		if (_ids.Count == 0)
			_anchor = null;
	}

	/// <summary>Выделенные окна недели в порядке сетки (строки сверху вниз, дни слева направо).</summary>
	public IReadOnlyList<TariffWindowCell> Selected(TariffWindowWeek week) =>
		Cells(week).Where(c => _ids.Contains(c.WindowId)).ToList();

	private void AddRectangle(TariffWindowWeek week, (int Row, int Day) a, (int Row, int Day) b)
	{
		for (int r = Math.Min(a.Row, b.Row); r <= Math.Max(a.Row, b.Row); r++)
			for (int d = Math.Min(a.Day, b.Day); d <= Math.Max(a.Day, b.Day); d++)
				if (week.Rows[r].Cells[d] is { } cell)
					_ids.Add(cell.WindowId);
	}

	private static IEnumerable<TariffWindowCell> Cells(TariffWindowWeek week) =>
		week.Rows.SelectMany(r => r.Cells).Where(c => c != null).Cast<TariffWindowCell>();

	private static (int Row, int Day)? Locate(TariffWindowWeek week, int windowId)
	{
		for (int r = 0; r < week.Rows.Count; r++)
			for (int d = 0; d < TariffWindowWeek.DaysInWeek; d++)
				if (week.Rows[r].Cells[d]?.WindowId == windowId)
					return (r, d);
		return null;
	}

	private static int IndexOf(TariffWindowWeek week, TariffWindowRow row)
	{
		for (int r = 0; r < week.Rows.Count; r++)
			if (ReferenceEquals(week.Rows[r], row))
				return r;
		return -1;
	}
}
