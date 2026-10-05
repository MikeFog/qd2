using System.Data;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Оформление строки, которое задаёт сама процедура: служебная колонка
/// <c>row_style</c> в результате. Договорённость десктопного SmartGrid
/// (<c>dataGrid_RowPrePaint</c>, константа ROW_STYLE): список не знает, что
/// показывает, — смысл строки знает процедура, список только оформляет.
/// Значения: <c>bold</c> — строки-итоги группы (stat_VolumeOfRealization3:
/// «Группа компаний» → фирмы, «Предмет рекламы 1-го уровня» → предметы);
/// <c>united</c> — объединённые тарифы (sl_TariffRetrieve), бледно-зелёный фон,
/// как в трафик-менеджменте десктопа. Колонки нет в iEntityAttribute, поэтому в
/// таблице она не видна — как и в десктопе.
/// </summary>
public static class RowStyle
{
    public const string Column = "row_style";

    public static bool IsBold(DataRow row) => Is(row, "bold");

    public static bool IsUnited(DataRow row) => Is(row, "united");

    private static bool Is(DataRow row, string value) =>
        row.Table.Columns.Contains(Column)
        && row[Column] is string style
        && style == value;
}
