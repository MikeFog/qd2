using System.Data;
using FogSoft.Web.Components;
using FogSoft.WinForm.Classes;
using Microsoft.AspNetCore.Components;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Показ карточки объекта модально и сохранение по подтверждению — веб-аналог
/// <c>PresentationObject.ShowPassport</c> вместе с циклом <c>ApplyChanges</c>
/// из <c>PassportForm</c>.
///
/// Вынесено из Journal.razor, когда второй вызывающий появился: кнопка
/// «Создать» у <c>objectPicker</c> открывает карточку другой сущности прямо из
/// уже открытой карточки — в десктопе это <c>ObjectPicker2.btnCreateNew_Click</c>,
/// то есть тот же <c>ShowPassport</c> вложенным окном.
///
/// Scoped — как и DialogService, которым он пользуется.
/// </summary>
public sealed class PassportDialog
{
	private readonly DialogService _dialogs;
	private readonly BusyService _busy;

	public PassportDialog(DialogService dialogs, BusyService busy)
	{
		_dialogs = dialogs;
		_busy = busy;
	}

	/// <summary>
	/// Показывает карточку и возвращает true, если объект сохранён.
	///
	/// Последовательность та же, что в десктопе: показать → проверить результат
	/// → Update(). Неуспешная проверка обязательных полей и отказ процедуры
	/// оставляют карточку открытой с сообщением — модальная форма десктопа по
	/// неуспешному ApplyChanges тоже не закрывается.
	/// </summary>
	public async Task<bool> ShowAsync(PresentationObject obj, bool isNew)
	{
		// Справочники грузятся один раз на открытие карточки — там же, где их
		// берёт десктоп.
		DataSet? data = await _busy.RunAsync(obj.LoadPassportData);

		// Заголовок — как в PassportForm.SetFormCaption.
		string title = isNew
			? $"Новый: {Tr.T(obj.Entity.Name)}"
			: $"Свойства: {obj.Name}";

		string? message = null;
		string? invalidField = null;
		Passport? passport = null;

		while (true)
		{
			RenderFragment body = builder =>
			{
				if (message != null)
				{
					builder.OpenElement(0, "div");
					builder.AddAttribute(1, "class", "alert alert-danger");
					builder.AddContent(2, message);
					builder.CloseElement();
				}

				builder.OpenComponent<Passport>(3);
				builder.AddComponentParameter(4, nameof(Passport.Object), obj);
				builder.AddComponentParameter(5, nameof(Passport.Xml), obj.Entity.XmlPassport);
				builder.AddComponentParameter(6, nameof(Passport.Entity), obj.Entity);
				builder.AddComponentParameter(7, nameof(Passport.IsNew), isNew);
				builder.AddComponentParameter(8, nameof(Passport.Data), data);
				builder.AddComponentParameter(9, nameof(Passport.InvalidField), invalidField);
				builder.AddComponentReferenceCapture(10, c => passport = (Passport)c);
				builder.CloseComponent();
			};

			if (await _dialogs.ShowAsync(title, body) != DialogOutcome.Ok)
				return false;

			// Обязательные поля проверяются до обращения к процедуре — как
			// ValidateUserInput в ApplyChanges.
			// Проверяется то, что ввёл пользователь: подстановка значений
			// нетронутых полей живёт в ApplyChanges и идёт после проверки.
			message = passport?.Validate(out invalidField);
			if (message != null)
				continue;

			try
			{
				// Наборы дочерних объектов (selector) отдаются объекту до
				// сохранения, как ApplyChanges в десктопе, а записываются
				// после него: связь пишется по идентификатору родителя,
				// которого у нового объекта до Update() ещё нет.
				passport?.ApplyChanges();

				bool saved = await _busy.RunAsync(() =>
				{
					if (!obj.Update())
						return false;

					(obj as ObjectContainer)?.SubmitChildrenChanges();
					return true;
				});
				if (saved)
					return true;

				message = "Сохранение отклонено.";
			}
			catch (Exception ex)
			{
				message = ErrorPresenter.Describe(ex);
			}
		}
	}
}
