using Merlin.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Выделение мест в сетке размещения — те же правила, что у окон трафика (WindowSelection,
/// решение владельца 2026-09-23): клик — одно место, Shift-клик — прямоугольник от предыдущего
/// клика, Ctrl-клик (⌘ на Mac) — добавить или убрать. Состояние — только ключи мест, поэтому
/// переживает перечитывание той же недели.
/// </summary>
public sealed class PlacementSelection
{
	private readonly HashSet<int> _keys = new();

	// Якорь Shift-прямоугольника — ключом места, а не номером строки: после перечитывания недели
	// («Показать заблокированные окна», запись) строки сдвигаются, и номер указал бы на чужую строку
	// или за конец списка.
	private int? _anchorKey;

	public IReadOnlyCollection<int> Keys => _keys;
	public int Count => _keys.Count;

	public void Click(PlacementWeek week, PlacementCell cell, bool shift, bool ctrl)
	{
		(int Row, int Day)? position = Locate(week, cell.Key);
		if (position == null)
			return;

		// Места-якоря в неделе больше нет — Shift-клик работает как обычный.
		(int Row, int Day)? anchor = _anchorKey == null ? null : Locate(week, _anchorKey.Value);
		if (shift && anchor != null)
		{
			if (!ctrl)
				_keys.Clear();
			for (int r = Math.Min(anchor.Value.Row, position.Value.Row); r <= Math.Max(anchor.Value.Row, position.Value.Row); r++)
				for (int d = Math.Min(anchor.Value.Day, position.Value.Day); d <= Math.Max(anchor.Value.Day, position.Value.Day); d++)
					if (week.Rows[r].Cells[d] is { } c)
						_keys.Add(c.Key);
			return;
		}

		if (ctrl)
		{
			if (!_keys.Remove(cell.Key))
				_keys.Add(cell.Key);
		}
		else
		{
			_keys.Clear();
			_keys.Add(cell.Key);
		}
		_anchorKey = cell.Key;
	}

	public void Clear()
	{
		_keys.Clear();
		_anchorKey = null;
	}

	/// <summary>После перечитывания недели — оставить только места, которые в ней есть.</summary>
	public void Retain(PlacementWeek week)
	{
		_keys.IntersectWith(week.Cells.Select(c => c.Key));
		if (_keys.Count == 0)
			_anchorKey = null;
	}

	/// <summary>Выделенные места недели в порядке сетки.</summary>
	public IReadOnlyList<PlacementCell> Selected(PlacementWeek week) =>
		week.Cells.Where(c => _keys.Contains(c.Key)).ToList();

	private static (int Row, int Day)? Locate(PlacementWeek week, int key)
	{
		for (int r = 0; r < week.Rows.Count; r++)
			for (int d = 0; d < PlacementWeek.DaysInWeek; d++)
				if (week.Rows[r].Cells[d]?.Key == key)
					return (r, d);
		return null;
	}
}
