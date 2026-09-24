using FogSoft.Web.Components;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Выбор интервала дат — веб-аналог десктопного FrmDateSelector в режиме интервала
/// («Интервал генерации окон», «Интервал удаления сгенерированных окон» и т.п.).
/// Проверка интервала — колбэком (правило операции живёт в ядре); при ошибке окно
/// остаётся открытым с текстом ошибки, как у именованных паспортов.
///
/// Scoped — пользуется диалогами circuit.
/// </summary>
public sealed class PeriodDialog
{
	private readonly DialogService _dialogs;

	public PeriodDialog(DialogService dialogs)
	{
		_dialogs = dialogs;
	}

	/// <returns>Выбранный интервал; null — отказ.</returns>
	public async Task<(DateTime Start, DateTime Finish)?> ShowAsync(string title, DateTime start, DateTime finish,
		string okText, Func<DateTime, DateTime, string?> validate, string? hint = null)
	{
		var model = new PeriodForm.Model { Start = start.Date, Finish = finish.Date, Hint = hint };

		while (true)
		{
			DialogOutcome outcome = await _dialogs.ShowAsync(title, builder =>
			{
				builder.OpenComponent<PeriodForm>(0);
				builder.AddComponentParameter(1, nameof(PeriodForm.Value), model);
				builder.CloseComponent();
			}, okText);

			if (outcome != DialogOutcome.Ok)
				return null;

			model.Message = validate(model.Start, model.Finish);
			if (model.Message == null)
				return (model.Start, model.Finish);
		}
	}
}
