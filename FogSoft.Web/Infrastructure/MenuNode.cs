namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Один узел дерева меню, построенного из плоских строк `UserMenuItems`.
/// Права уже посчитаны на стороне процедуры (столбец `enabled`) — здесь их
/// пересчитывать не нужно и не следует: это не то же самое, что права на
/// действие внутри экрана (см. docs/tasks/web-migration.md, раздел 7, п.1,
/// этап 1 — отдельная, ещё не сделанная задача).
/// </summary>
public sealed class MenuNode
{
	public required int MenuId { get; init; }
	public required string Name { get; init; }
	public required string? CodeName { get; init; }
	public required bool Enabled { get; init; }
	public required string? ImgResourcePath { get; init; }
	public List<MenuNode> Children { get; } = new();

	/// <summary>Пункт-разделитель ("-" в имени) — десктоп рисует его отдельной чертой.</summary>
	public bool IsSeparator => Name == "-";
}
