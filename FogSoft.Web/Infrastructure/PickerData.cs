using System.Data;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Данные для <c>objectPicker</c>: откуда берутся кандидаты на выбор и как по
/// сохранённому идентификатору получить имя объекта для показа.
///
/// Веб-аналог той половины <c>ObjectPicker2</c>, что работает с данными
/// (<c>SetDataSource</c> / <c>LoadData</c> / <c>SelectObject</c>). Отрисовка —
/// в Passport.razor и ObjectSelector.razor, здесь только данные: так же, как
/// PassportSchema отделён от разметки.
/// </summary>
public static class PickerData
{
	/// <summary>
	/// Кандидаты на выбор. Если паспорт уже принёс готовый набор с таким
	/// псевдонимом — берётся он (в десктопе это <c>SetDataSource</c>), иначе
	/// сущность грузит их сама при открытии выбора (<c>LoadData</c>).
	/// </summary>
	public static DataTable Candidates(PassportPicker picker, DataSet? passportData, PresentationObject owner)
	{
		if (picker.Source != null && passportData != null && passportData.Tables.Contains(picker.Source))
			return passportData.Tables[picker.Source]!;

		Entity entity = EntityManager.GetEntity(picker.EntityName);

		// В десктопе LoadData собирает словарь parameters с начальными
		// значениями фильтра сущности, а затем передаёт в GetContent не его, а
		// filterValues — то есть собранное игнорируется. Повторяем фактическое
		// поведение, а не намерение: иначе выборка вернула бы другой состав
		// строк, чем показывает десктоп.
		return entity.GetContent(Filters(picker, owner));
	}

	/// <summary>
	/// Значения фильтра из вложенных <c>&lt;filter&gt;</c>. Разбор типов тот
	/// же, что в <c>PageFieldSelector.GetFilters</c>, включая <c>parameter</c>
	/// (значение берётся из параметров редактируемого объекта) и
	/// <c>parameter_isnew</c>.
	/// </summary>
	public static Dictionary<string, object> Filters(PassportPicker picker, PresentationObject owner)
	{
		Dictionary<string, object> values = DataAccessor.CreateParametersDictionary();

		foreach (PassportFilterValue filter in picker.Filters)
		{
			var resolver = new FieldTypeResolver(filter.Type, null);
			object? value;

			if (resolver.IsString || resolver.IsDoubleString)
				value = filter.Value;
			else if (resolver.IsBoolean)
				value = ParseHelper.ParseToBoolean(filter.Value);
			else if (resolver.IsInteger)
				value = ParseHelper.ParseToInt32(filter.Value);
			else if (resolver.IsDecimal)
				value = decimal.Parse(filter.Value);
			else if (string.Equals(filter.Type, "parameter", StringComparison.Ordinal))
				value = owner[filter.Name];
			else if (string.Equals(filter.Type, "parameter_isnew", StringComparison.Ordinal))
				value = owner.IsNew;
			else
				continue;

			values[filter.Name] = value!;
		}

		return values;
	}

	/// <summary>
	/// Имя выбранного объекта по сохранённому идентификатору. Повторяет
	/// <c>ObjectPicker2.SelectObject</c>: если есть готовый набор — строка
	/// ищется в нём по первичному ключу, иначе объект поднимается из базы
	/// своей процедурой обновления.
	///
	/// Пустая строка, если объект не нашёлся: в десктопе поле в этом случае
	/// тоже остаётся пустым (<c>SelectObject</c> при <c>row == null</c> ничего
	/// не пишет), а сохранённое значение не трогается.
	/// </summary>
	public static string NameOf(PassportPicker picker, DataSet? passportData, object? id)
	{
		if (id == null || id == DBNull.Value || id.ToString()!.Length == 0)
			return "";

		Entity entity = EntityManager.GetEntity(picker.EntityName);

		DataTable? source = picker.Source != null && passportData != null && passportData.Tables.Contains(picker.Source)
			? passportData.Tables[picker.Source]
			: null;

		if (source != null)
		{
			DataRow? row = FindByKey(source, entity, id);
			return row == null ? "" : entity.CreateObject(row).Name;
		}

		var parameters = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase)
		{
			{ entity.PKColumns[0], id.ToString()! }
		};
		PresentationObject obj = entity.CreateObject(parameters);
		obj.Refresh();
		return obj.Name;
	}

	private static DataRow? FindByKey(DataTable table, Entity entity, object id)
	{
		if (entity.PKColumns.Length == 0)
			return null;

		// PrimaryKey ставится ровно как в ObjectPicker2.SelectObject — ради
		// быстрого Rows.Find. Набор общий на открытый паспорт, поэтому
		// повторное присвоение того же ключа безвредно.
		if (table.PrimaryKey.Length == 0)
		{
			var keyColumns = new DataColumn[entity.PKColumns.Length];
			for (int i = 0; i < entity.PKColumns.Length; i++)
			{
				DataColumn? column = table.Columns[entity.PKColumns[i]];
				if (column == null)
					return null;
				keyColumns[i] = column;
			}
			table.PrimaryKey = keyColumns;
		}

		return table.Rows.Find(id);
	}
}
