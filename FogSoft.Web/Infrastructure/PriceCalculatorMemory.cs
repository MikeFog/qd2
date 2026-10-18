using FogSoft.WinForm.Classes;
using Merlin.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Память экрана «Калькулятор цены» на сеанс (docs/tasks/web-price-calculator.md, §4.1, этапы К.2–К.3): параметры, правки
/// и отметки строк, сортировка, сохранённые варианты (в сеансе расчёта), открытая вкладка. Страница пересоздаётся при
/// смене языка (MainLayout, @key) — всё состояние здесь, поля страницы его только показывают.
///
/// Scoped, как <see cref="ModuleRowsMemory"/>: уход на другой экран и обратно расчёт сохраняет, F5, обрыв соединения и
/// другой пользователь — теряют (в базу не пишется).
/// </summary>
public sealed class PriceCalculatorMemory
{
	private SecurityManager.User? _user;

	public PriceCalculatorMemory(UserSession session)
	{
		// Changed приходит и на смену языка — забываем только при смене пользователя.
		_user = session.User;
		session.Changed += () =>
		{
			if (ReferenceEquals(session.User, _user))
				return;
			_user = session.User;
			Session = null;
			VariantsTab = false;
		};
	}

	/// <summary>Расчёт экрана; null — в этом сеансе экран ещё не открывали.</summary>
	public PriceCalculatorSession? Session { get; set; }

	/// <summary>Открыта вкладка «Сохранённые варианты» (иначе «Расчёты»).</summary>
	public bool VariantsTab { get; set; }
}
