namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Всплывающее меню действий, которое можно дождаться — как DialogService.
///
///     string? picked = await Menu.ShowAsync(items, x, y, fromButton);
///
/// Меню одно на circuit и рисуется хостом в макете (ActionMenuHost), а не
/// внутри строки: контейнер списка обрезает всё, что выходит за его край, и у
/// нижних строк меню оказалось бы под краем.
///
/// Scoped — меню одного пользователя не должно быть видно другому.
/// </summary>
public sealed class ActionMenuService
{
	private TaskCompletionSource<string?>? _completion;

	public ActionMenuRequest? Current { get; private set; }

	public event Func<Task>? Changed;

	/// <param name="x">Координаты щелчка в окне (clientX/clientY).</param>
	/// <param name="fromButton">
	/// Открыто кнопкой «⋯» — меню выравнивается по кнопке; иначе (правая
	/// кнопка мыши) — от точки щелчка, как контекстное меню ОС.
	/// </param>
	/// <returns>Имя выбранного действия; null — меню закрыто без выбора.</returns>
	public async Task<string?> ShowAsync(IReadOnlyList<ActionMenuItem> items, double x, double y, bool fromButton)
	{
		// Второе меню поверх первого не открывается: новое закрывает старое.
		_completion?.TrySetResult(null);

		var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
		_completion = completion;
		Current = new ActionMenuRequest(items, x, y, fromButton, Guid.NewGuid());
		await NotifyAsync();

		return await completion.Task;
	}

	public async Task CloseAsync(string? picked)
	{
		if (Current == null)
			return;

		TaskCompletionSource<string?>? completion = _completion;
		_completion = null;
		Current = null;
		await NotifyAsync();

		completion?.TrySetResult(picked);
	}

	private async Task NotifyAsync()
	{
		if (Changed != null)
			await Changed.Invoke();
	}
}

/// <param name="Id">Меняется у каждого открытия — хост по нему понимает, что меню надо разместить заново.</param>
public sealed record ActionMenuRequest(
	IReadOnlyList<ActionMenuItem> Items, double X, double Y, bool FromButton, Guid Id);
