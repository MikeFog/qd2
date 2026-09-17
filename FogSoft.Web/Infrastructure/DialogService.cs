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
/// Диалоги складываются в стек: паспорт открывает выбор объекта, выбор объекта
/// может открыть паспорт нового объекта. В десктопе это те же вложенные
/// ShowDialog (ObjectPicker2.btnSelect_Click вызывается из уже открытой
/// PassportForm), поэтому запрет на второй диалог был бы ограничением веба,
/// которого нет в переносимой системе.
///
/// Сервис регистрируется Scoped — то есть свой на circuit: диалог одного
/// пользователя не должен быть виден другому.
/// </summary>
public sealed class DialogService
{
	private readonly List<Entry> _stack = new();

	/// <summary>Открытые диалоги снизу вверх; последний — верхний.</summary>
	public IReadOnlyList<DialogRequest> Open =>
		_stack.Select(e => e.Request).ToList();

	/// <summary>Верхний открытый диалог; null — открытых нет.</summary>
	public DialogRequest? Current => _stack.Count == 0 ? null : _stack[^1].Request;

	/// <summary>Сообщает хосту, что нужно перерисоваться.</summary>
	public event Func<Task>? Changed;

	public async Task<DialogOutcome> ShowAsync(string title, RenderFragment body, string okText = "Сохранить")
	{
		// RunContinuationsAsynchronously обязателен: без него продолжение
		// вызывающего кода выполнилось бы прямо внутри обработчика нажатия
		// кнопки, на диспетчере circuit, что легко приводит к взаимной
		// блокировке при повторном обращении к UI.
		var completion = new TaskCompletionSource<DialogOutcome>(
			TaskCreationOptions.RunContinuationsAsynchronously);

		_stack.Add(new Entry(new DialogRequest(title, body, okText), completion));
		await NotifyAsync();

		return await completion.Task;
	}

	/// <summary>
	/// Закрывает верхний диалог и отдаёт результат тому, кто его ждёт. Нижние
	/// диалоги закрыть нельзя — кнопки у них погашены, см. DialogHost.
	/// </summary>
	public async Task CloseAsync(DialogOutcome outcome)
	{
		if (_stack.Count == 0)
			return;

		Entry top = _stack[^1];
		_stack.RemoveAt(_stack.Count - 1);
		await NotifyAsync();

		top.Completion.TrySetResult(outcome);
	}

	private async Task NotifyAsync()
	{
		if (Changed != null)
			await Changed.Invoke();
	}

	private sealed record Entry(DialogRequest Request, TaskCompletionSource<DialogOutcome> Completion);
}

/// <param name="Title">Заголовок окна.</param>
/// <param name="Body">Содержимое — любой компонент, например паспорт.</param>
/// <param name="OkText">Надпись на подтверждающей кнопке.</param>
public sealed record DialogRequest(string Title, RenderFragment Body, string OkText);
