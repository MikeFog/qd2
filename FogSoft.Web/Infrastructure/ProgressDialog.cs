using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace FogSoft.Web.Infrastructure;

/// <summary>Чем закончилась долгая операция.</summary>
/// <param name="Done">Сколько порций выполнено.</param>
/// <param name="Stopped">Пользователь остановил операцию раньше конца.</param>
public sealed record ProgressOutcome(int Done, bool Stopped);

/// <summary>
/// Долгая операция по порциям с окном прогресса и кнопкой «Остановить» — веб-аналог
/// десктопного ProgressForm + BackgroundWorker (генерация рекламных окон, удаление
/// сгенерированных окон).
///
/// Порция выполняется на circuit целиком, между порциями circuit свободен: окно
/// перерисовывается и нажатие «Остановить» доходит. Остановка — между порциями, как
/// CancellationPending у BackgroundWorker: начатая порция доделывается. Отдельного
/// потока нет — ядро синхронное и держит транзакцию и пользователя в контексте circuit.
///
/// Ошибка порции останавливает операцию: окно закрывается, исключение уходит
/// вызывающему (десктоп так же прерывает цикл исключением BackgroundWorker).
///
/// Scoped — пользуется диалогами circuit.
/// </summary>
public sealed class ProgressDialog
{
	private readonly DialogService _dialogs;

	public ProgressDialog(DialogService dialogs)
	{
		_dialogs = dialogs;
	}

	/// <param name="title">Заголовок окна.</param>
	/// <param name="steps">Порции операции.</param>
	/// <param name="describe">Подпись текущей порции, например «неделя 01.09 – 07.09».</param>
	/// <param name="work">Выполнение одной порции — синхронный вызов ядра.</param>
	public async Task<ProgressOutcome> RunAsync<T>(string title, IReadOnlyList<T> steps,
		Func<T, string> describe, Action<T> work)
	{
		var state = new State { Total = steps.Count };
		Task<DialogOutcome> dialog = _dialogs.ShowAsync(title, builder => Render(builder, state),
			okText: null, cancelText: "Остановить");

		for (int i = 0; i < steps.Count; i++)
		{
			if (dialog.IsCompleted)
				return new ProgressOutcome(i, Stopped: true);

			state.Current = describe(steps[i]);
			await _dialogs.RefreshAsync();
			// Отдать circuit: уходит перерисовка, и доходят нажатия, накопившиеся за порцию.
			await Task.Delay(1);
			if (dialog.IsCompleted)
				return new ProgressOutcome(i, Stopped: true);

			try
			{
				work(steps[i]);
			}
			catch
			{
				if (!dialog.IsCompleted)
					await _dialogs.CloseAsync(DialogOutcome.Cancel);
				throw;
			}

			state.Done = i + 1;
		}

		if (!dialog.IsCompleted)
			await _dialogs.CloseAsync(DialogOutcome.Ok);
		return new ProgressOutcome(steps.Count, Stopped: false);
	}

	private static void Render(RenderTreeBuilder builder, State state)
	{
		int percent = state.Total == 0 ? 100 : state.Done * 100 / state.Total;

		builder.OpenElement(0, "div");
		builder.AddAttribute(1, "class", "progress");
		builder.AddAttribute(2, "role", "progressbar");
		builder.AddAttribute(3, "aria-valuenow", percent);
		builder.AddAttribute(4, "aria-valuemin", 0);
		builder.AddAttribute(5, "aria-valuemax", 100);
		builder.OpenElement(6, "div");
		builder.AddAttribute(7, "class", "progress-bar");
		builder.AddAttribute(8, "style", $"width:{percent}%");
		builder.CloseElement();
		builder.CloseElement();

		builder.OpenElement(9, "p");
		builder.AddAttribute(10, "class", "mt-2 mb-0 tabular");
		builder.AddContent(11, $"{state.Done} из {state.Total}" + (state.Current != null ? $" · {state.Current}" : ""));
		builder.CloseElement();
	}

	private sealed class State
	{
		public int Total;
		public int Done;
		public string? Current;
	}
}
