using FogSoft.WinForm.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Отбор экранов на время сеанса: ушёл в другой журнал, вернулся — отбор тот же,
/// с которым читал данные. Scoped, то есть живёт столько же, сколько вход в систему
/// во вкладке (<see cref="UserSession"/>); F5 и новая вкладка начинают с умолчаний.
/// Решение владельца продукта 27.09.2026: только на сеанс, в базу не пишется.
///
/// Экран сам заполняет словарь значениями по умолчанию и зовёт <see cref="Restore"/>
/// — умолчания запоминаются для кнопки «По умолчанию» (<see cref="Reset"/>), а поверх
/// них ложится сохранённый отбор. <see cref="Remember"/> зовётся там, где данные
/// читаются по отбору: в памяти то, по чему построен список, а не недоприменённые
/// правки в панели.
///
/// Значения отбора — строки, числа, даты (FilterPanel других не кладёт), поэтому
/// достаточно копии словаря; ссылку на словарь экрана хранить нельзя — панель меняет
/// его на месте.
/// </summary>
public sealed class FilterMemory
{
	private readonly Dictionary<string, Dictionary<string, object>> _applied = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, Dictionary<string, object>> _defaults = new(StringComparer.OrdinalIgnoreCase);
	private SecurityManager.User? _user;

	public FilterMemory(UserSession session)
	{
		// Changed приходит и на смену языка — забываем только при смене пользователя
		// (выход, вход другим): его отбор, например по менеджеру, следующему не достаётся.
		_user = session.User;
		session.Changed += () =>
		{
			if (ReferenceEquals(session.User, _user))
				return;
			_user = session.User;
			_applied.Clear();
			_defaults.Clear();
			_presets.Clear();
		};
	}

	/// <summary>
	/// <paramref name="values"/> уже заполнен умолчаниями экрана: они запоминаются для
	/// <see cref="Reset"/>, и, если по этому экрану отбор уже применяли, он заменяет их.
	/// </summary>
	public void Restore(string screen, Dictionary<string, object> values)
	{
		_defaults[screen] = Copy(values);
		if (_presets.Remove(screen, out var preset))
		{
			// Переход с отбором (Preset): как новое окно десктопа — умолчания плюс
			// заданные поля, прежний отбор экрана не подмешивается. DBNull — поле
			// выключено (в панели выключенное поле — отсутствие ключа).
			foreach (var (key, value) in preset)
			{
				if (value == null || value == DBNull.Value)
					values.Remove(key);
				else
					values[key] = value;
			}
			return;
		}
		if (_applied.TryGetValue(screen, out var saved))
			CopyInto(saved, values);
	}

	private readonly Dictionary<string, Dictionary<string, object>> _presets = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Отбор для ближайшего открытия экрана — переход «открыть журнал с отбором» (из
	/// строки журнала бонусов в журнал акций). Действует один раз, при следующем
	/// <see cref="Restore"/>.
	/// </summary>
	public void Preset(string screen, IReadOnlyDictionary<string, object> values) =>
		_presets[screen] = new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Отбор, по которому экран последний раз читал данные; null — ещё не читал. Для переходов,
	/// которым нужен отбор исходного журнала (десктопный IJournal.Filters): «Перейти к балансу
	/// для фирмы» берёт дату «Баланса для всех фирм».
	/// </summary>
	public IReadOnlyDictionary<string, object>? Applied(string screen) =>
		_applied.TryGetValue(screen, out var values) ? values : null;

	/// <summary>Данные прочитаны по этому отбору.</summary>
	public void Remember(string screen, Dictionary<string, object> values) => _applied[screen] = Copy(values);

	/// <summary>
	/// Вернуть в панель умолчания экрана. Данные не перечитываются и память не
	/// трогается — как при любой правке отбора, в силу вступает по «Применить».
	/// </summary>
	public void Reset(string screen, Dictionary<string, object> values)
	{
		if (_defaults.TryGetValue(screen, out var defaults))
			CopyInto(defaults, values);
	}

	private static Dictionary<string, object> Copy(Dictionary<string, object> values) => new(values, values.Comparer);

	private static void CopyInto(Dictionary<string, object> source, Dictionary<string, object> target)
	{
		target.Clear();
		foreach (var (key, value) in source)
			target[key] = value;
	}
}
