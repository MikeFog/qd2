using System.Data;
using FogSoft.WinForm.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Правило быстрого поиска (QuickSearchBox): фраза целиком, подстрокой, без учёта
/// регистра, по тексту ячейки так, как он показан. Колонка задана — только по ней,
/// иначе — по всем. Булевы колонки не участвуют: их текст «Да»/пусто, и «да» нашлось бы
/// в каждой отмеченной строке.
/// </summary>
public static class QuickSearchFilter
{
	/// <param name="display">Текст ячейки, как его видит пользователь.</param>
	/// <param name="isBoolean">Булева ли колонка.</param>
	public static bool Matches(DataRow row, string text, string? column, IEnumerable<Entity.Attribute> columns,
		Func<DataRow, Entity.Attribute, string> display, Func<Entity.Attribute, bool> isBoolean)
	{
		foreach (Entity.Attribute a in columns)
		{
			if (column != null && !string.Equals(a.Name, column, StringComparison.OrdinalIgnoreCase))
				continue;
			if (isBoolean(a))
				continue;
			if (display(row, a).Contains(text, StringComparison.CurrentCultureIgnoreCase))
				return true;
		}
		return false;
	}
}
