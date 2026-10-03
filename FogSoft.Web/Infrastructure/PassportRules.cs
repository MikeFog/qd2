using FogSoft.WinForm.Classes;
using Microsoft.AspNetCore.Components;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Поведение карточки сверх метаданных — то, что в десктопе делает своя форма паспорта поверх
/// PassportForm (RollerPassportForm: блокировки полей, кнопка загрузки файла, проверка перед
/// сохранением). <see cref="PassportDialog"/> берёт правила по классу объекта
/// (<see cref="For"/>), поэтому они действуют везде, где открывается карточка: журнал,
/// «⋯» в дереве, «Создать» у выбора объекта.
/// </summary>
public sealed class PassportRules
{
	/// <summary>Недоступные сейчас поля — см. Passport.FieldDisabled.</summary>
	public Func<string, bool>? FieldDisabled { get; init; }

	/// <summary>
	/// Свой контрол вместо поля паспорта по его имени (например, <c>button</c>, у которого в
	/// десктопе обработчик вешает форма). Второй аргумент — перерисовать карточку после того,
	/// как контрол поменял значения объекта. null — поле рисуется как обычно.
	/// </summary>
	public Func<string, Action, RenderFragment?>? Control { get; init; }

	/// <summary>
	/// Проверка перед сохранением — после обязательных полей, до записи (ApplyChanges формы).
	/// Может поправить значения объекта. Возвращает текст отказа или null.
	/// </summary>
	public Func<string?>? Validate { get; init; }

	public static PassportRules? For(PresentationObject obj, bool isNew) =>
		obj is Merlin.Classes.Roller roller ? RollerPassport.Rules(roller, isNew) : null;
}
