using System.Data;
using FogSoft.Web.Components;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Microsoft.AspNetCore.Components;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Показ именованного паспорта (<c>iPassport</c>, <see cref="PassportLoader"/>) —
/// веб-аналог <c>UniversalPassportForm</c>. В отличие от <see cref="PassportDialog"/>,
/// который редактирует сам объект по его собственному паспорту и сохраняет
/// <c>Update()</c>, здесь паспорт не привязан ни к какой сущности: XML берётся по
/// имени из метаданных, а по «ОК» собранные значения уходят туда, куда скажет
/// вызывающий код — колбэком в ядро (как у «Добавить тариф массово» и «Изменить
/// похожие тарифы», где бизнес-логика в <c>Tariff</c>/<c>MassmediaPricelist</c>)
/// либо, вторым перегруженным вариантом, прямо в хранимую процедуру
/// (<c>ExecuteNonQuery</c>) — так живёт вторая половина именованных паспортов в
/// десктопе (<c>UniversalPassportForm(parameters, passportName, procedureName, …)</c>),
/// хотя эта задача им не пользуется. См. docs/AI_AGENT_PLAYBOOK.md, «Именованные
/// паспорта (iPassport): форма без формы».
///
/// <paramref name="obj"/>-подобный параметр (см. <see cref="ShowAsync"/>) — не сам
/// редактируемый объект экрана, а черновик-шаблон с посеянными значениями
/// (в десктопе — тот же приём: <c>Tariff template = new Tariff { Parameters = ... }</c>
/// или <c>tariffEntity.NewObject</c>). Карточка рисуется компонентом
/// <see cref="Passport"/>, который уже умеет строить контролы по произвольному XML
/// (<c>Xml</c>) — писать веб-рендерер под второй набор паспортов не пришлось.
///
/// Данные для справочников — той же процедурой, что у обычной карточки объекта
/// (<c>PresentationObject.LoadPassportData()</c>, ключ <c>EntityId_Load_PropertyPage</c>
/// по сущности шаблона) — ровно как в десктопе оба места явно грузят паспортные
/// данные через <c>DataAccessor.PrepareParameters(..., InterfaceObjects.PropertyPage,
/// Actions.Load)</c> + <c>DoAction</c> ещё до показа формы.
///
/// Scoped — как и <see cref="DialogService"/>, которым он пользуется.
/// </summary>
public sealed class NamedPassportDialog
{
	private readonly DialogService _dialogs;
	private readonly BusyService _busy;

	public NamedPassportDialog(DialogService dialogs, BusyService busy)
	{
		_dialogs = dialogs;
		_busy = busy;
	}

	/// <summary>
	/// Показывает именованный паспорт и по «ОК» вызывает <paramref name="apply"/> с
	/// собранными значениями (та же карта параметров, что десктопный
	/// <c>ApplyChangesDelegate</c>). Возвращает true, если <paramref name="apply"/>
	/// выполнен без исключения.
	/// </summary>
	/// <param name="obj">Черновик-шаблон: объект, в который контролы паспорта пишут значения.</param>
	/// <param name="passportName">Имя паспорта в <c>iPassport</c> (<see cref="PassportLoader"/>).</param>
	/// <param name="caption">Заголовок диалога.</param>
	/// <param name="isNew">
	/// Влияет на обязательность полей (<c>isMandatoryOnCreate</c>), как и у обычной
	/// карточки: true для мастеров создания («Добавить тариф массово»), false —
	/// когда шаблон засеян значениями существующего объекта («Изменить похожие»).
	/// </param>
	/// <param name="validate">
	/// Проверка сверх обязательности полей (её уже делает сам <see cref="Passport"/>) —
	/// правило самой операции, вынесенное в ядро (например
	/// <c>Tariff.ValidateMassEdit</c>). Текст ошибки или null.
	/// </param>
	/// <param name="apply">Что сделать с собранными значениями — вызов ядра.</param>
	/// <param name="data">
	/// Наборы строк паспорта, если их грузит не процедура карточки шаблона, а своя
	/// (у замены ролика — ключ CampaignRoller_Substitute_PropertyPage, как у
	/// RollerSubstitutionForm.LoadData). null — <c>obj.LoadPassportData()</c>.
	/// </param>
	/// <param name="fieldDisabled">
	/// Какие поля сейчас недоступны — взаимозависимость контролов десктопной формы
	/// (UpdateControlsStatus). Спрашивается при каждой отрисовке. null — доступно всё.
	/// </param>
	public Task<bool> ShowAsync(PresentationObject obj, string passportName, string caption, bool isNew,
		Func<Dictionary<string, object>, string?> validate, Action<Dictionary<string, object>> apply,
		DataSet? data = null, Func<string, bool>? fieldDisabled = null) =>
		ShowCoreAsync(obj, passportName, caption, isNew, validate, apply, data, fieldDisabled);

	/// <summary>
	/// Вариант «значения уходят прямо в процедуру» — веб-аналог второго
	/// конструктора <c>UniversalPassportForm(parameters, passportName,
	/// procedureName, caption, validate)</c>: своего обработчика нет вовсе,
	/// собранные параметры идут в <c>ExecuteNonQuery</c>. Этой задачей не
	/// используется (у тарифов бизнес-логика в ядре), но механизм должен
	/// поддержать и такие места без переписывания — только имя процедуры вместо
	/// колбэка применения.
	/// </summary>
	public Task<bool> ShowAsync(PresentationObject obj, string passportName, string procedureName, string caption,
		bool isNew, Func<Dictionary<string, object>, string?> validate) =>
		ShowCoreAsync(obj, passportName, caption, isNew, validate,
			parameters => DataAccessor.ExecuteNonQuery(procedureName, parameters), null, null);

	private async Task<bool> ShowCoreAsync(PresentationObject obj, string passportName, string caption, bool isNew,
		Func<Dictionary<string, object>, string?> validate, Action<Dictionary<string, object>> apply,
		DataSet? data, Func<string, bool>? fieldDisabled)
	{
		// Справочники — той же процедурой, что у обычной карточки объекта шаблона
		// (сущности, к которой относится передаваемый черновик), если вызывающий
		// не загрузил свои. Одним ожиданием с XML паспорта — оба идут в базу.
		DataSet? loaded = data;
		string xml = await _busy.RunAsync(() =>
		{
			loaded ??= obj.LoadPassportData();
			return PassportLoader.Load(passportName);
		});
		data = loaded;

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
				builder.AddComponentParameter(5, nameof(Passport.Xml), xml);
				builder.AddComponentParameter(6, nameof(Passport.Entity), obj.Entity);
				builder.AddComponentParameter(7, nameof(Passport.IsNew), isNew);
				builder.AddComponentParameter(8, nameof(Passport.Data), data);
				builder.AddComponentParameter(9, nameof(Passport.InvalidField), invalidField);
				builder.AddComponentParameter(10, nameof(Passport.FieldDisabled), fieldDisabled);
				builder.AddComponentReferenceCapture(11, c => passport = (Passport)c);
				builder.CloseComponent();
			};

			if (await _dialogs.ShowAsync(caption, body) != DialogOutcome.Ok)
				return false;

			// Обязательность полей — тем же путём, что PassportDialog: до
			// подстановки нетронутых полей, видит то, что ввёл пользователь.
			message = passport?.Validate(out invalidField);
			if (message != null)
				continue;

			passport?.ApplyChanges();

			// Правило самой операции (интервал часов, дни недели, «ничего не
			// изменено» и т.п.) — уже по собранным значениям, как ValidateData
			// у UniversalPassportForm.ApplyChanges, которая тоже идёт после
			// page.ApplyChanges().
			// Проверка может ходить в базу (замена ролика по «ОК» получает ролик-
			// молчание, как и десктопная форма), поэтому отказ базы — тем же
			// сообщением в открытом диалоге, что и отказ apply ниже.
			try
			{
				message = await _busy.RunAsync(() => validate(obj.Parameters));
			}
			catch (Exception ex)
			{
				message = ErrorPresenter.Describe(ex);
			}
			if (message != null)
			{
				invalidField = null;
				continue;
			}

			try
			{
				await _busy.RunAsync(() => apply(obj.Parameters));
				return true;
			}
			catch (Exception ex)
			{
				message = ErrorPresenter.Describe(ex);
			}
		}
	}
}
