using Microsoft.AspNetCore.Components;

namespace FogSoft.Web.Infrastructure;

/// <summary>Чем закончился диалог. Веб-аналог DialogResult из WinForms.</summary>
public enum DialogOutcome
{
	Cancel,
	Ok
}

/// <summary>
/// Модальные диалоги, которые можно дождаться.
///
/// Это проверка довода №2 из раздела 5.1 плана: паттерн «открыл модальное окно,
/// получил результат, продолжил» должен воспроизводиться почти дословно, иначе
/// 56 мест с ShowDialog в доменных классах придётся разворачивать в цепочки
/// колбэков и маршрутов.
///
/// Десктоп:
///     if (form.ShowDialog(owner) == DialogResult.OK) { ... }
/// Веб:
///     if (await Dialogs.ShowAsync(title, body) == DialogOutcome.Ok) { ... }
///
/// Механика — <see cref="TaskCompletionSource"/>: <see cref="ShowAsync"/>
/// возвращает незавершённую задачу, вызывающий код ждёт её, а завершается она
/// в момент, когда пользователь нажал кнопку в диалоге. Между этими двумя
/// событиями проходят отдельные порции работы circuit, и именно поэтому такое
/// возможно в Blazor Server: состояние компонента живёт между действиями, как
/// жил бы стек WinForms-формы.
///
/// Сервис регистрируется Scoped — то есть свой на circuit: диалог одного
/// пользователя не должен быть виден другому.
/// </summary>
public sealed class DialogService
{
	private TaskCompletionSource<DialogOutcome>? _completion;

	/// <summary>Открытый сейчас диалог; null — открытого нет.</summary>
	public DialogRequest? Current { get; private set; }

	/// <summary>Сообщает хосту, что нужно перерисоваться.</summary>
	public event Func<Task>? Changed;

	public async Task<DialogOutcome> ShowAsync(string title, RenderFragment body, string okText = "Сохранить")
	{
		if (Current != null)
			throw new InvalidOperationException("Диалог уже открыт.");

		// RunContinuationsAsynchronously обязателен: без него продолжение
		// вызывающего кода выполнилось бы прямо внутри обработчика нажатия
		// кнопки, на диспетчере circuit, что легко приводит к взаимной
		// блокировке при повторном обращении к UI.
		_completion = new TaskCompletionSource<DialogOutcome>(
			TaskCreationOptions.RunContinuationsAsynchronously);

		Current = new DialogRequest(title, body, okText);
		await NotifyAsync();

		return await _completion.Task;
	}

	/// <summary>Закрывает открытый диалог и отдаёт результат тому, кто его ждёт.</summary>
	public async Task CloseAsync(DialogOutcome outcome)
	{
		TaskCompletionSource<DialogOutcome>? completion = _completion;
		if (completion == null)
			return;

		_completion = null;
		Current = null;
		await NotifyAsync();

		completion.TrySetResult(outcome);
	}

	private async Task NotifyAsync()
	{
		if (Changed != null)
			await Changed.Invoke();
	}
}

/// <param name="Title">Заголовок окна.</param>
/// <param name="Body">Содержимое — любой компонент, например паспорт.</param>
/// <param name="OkText">Надпись на подтверждающей кнопке.</param>
public sealed record DialogRequest(string Title, RenderFragment Body, string OkText);
