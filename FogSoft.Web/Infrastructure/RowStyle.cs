using System.Data;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Оформление строки, которое задаёт сама процедура: служебная колонка
/// <c>row_style</c> в результате. Договорённость десктопного SmartGrid
/// (<c>dataGrid_RowPrePaint</c>, константа ROW_STYLE): список не знает, что
/// показывает, — смысл строки знает процедура, список только оформляет.
/// Сегодня значение одно — <c>bold</c>; его отдаёт stat_VolumeOfRealization3
/// для строк-итогов группы («Группа компаний» → фирмы, «Предмет рекламы
/// 1-го уровня» → предметы). Колонки нет в iEntityAttribute, поэтому в
/// таблице она не видна — как и в десктопе.
/// </summary>
public static class RowStyle
{
    public const string Column = "row_style";

    public static bool IsBold(DataRow row) =>
        row.Table.Columns.Contains(Column)
        && row[Column] is string style
        && style == "bold";
}
