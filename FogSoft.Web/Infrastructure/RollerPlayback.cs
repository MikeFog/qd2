using FogSoft.WinForm.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Прослушивание ролика из любого места — веб-аналог MediaControl.Current десктопа. Проигрыватель
/// один на приложение (RollerPlayer в верхней полосе MainLayout), его зовут журнал роликов,
/// журнал использования роликов и действие «Прослушать ролик» у строк дерева кампании
/// (ObjectActions). Scoped — свой у circuit.
/// </summary>
public sealed class RollerPlayback
{
	private Func<PresentationObject, Task<string?>>? _player;

	/// <summary>Проигрыватель сообщает о себе при показе (RollerPlayer).</summary>
	public void Attach(Func<PresentationObject, Task<string?>> player) => _player = player;

	public void Detach() => _player = null;

	/// <summary>
	/// Проиграть файл ролика (параметр path объекта). Возвращает отказ для пользователя (файла
	/// нет, не читается) или null.
	/// </summary>
	public Task<string?> PlayAsync(PresentationObject roller) =>
		_player?.Invoke(roller) ?? Task.FromResult<string?>(null);
}
