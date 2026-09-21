using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Узел древовидного экрана. Держит объект и его контейнер: дети берутся из
/// контейнера, а не из объекта, — так же, как TreeView2 хранит объект в Tag
/// узла, а детей запрашивает у него как у IObjectContainer.
///
/// Вынесен из Browser.razor в отдельный тип, потому что его рисует отдельный
/// компонент (BrowserNodeView), а тот должен знать этот тип.
/// </summary>
public sealed class BrowserNode
{
	/// <summary>Подпись; меняется, когда объект переименовали в карточке.</summary>
	public required string Name { get; set; }

	/// <summary>
	/// Контейнер узла. Он же — объект, над которым выполняются действия меню
	/// узла: у корня это FakeContainer, у остальных — доменный объект строки.
	/// </summary>
	public required IObjectContainer Container { get; init; }

	/// <summary>Класс значка узла (Bootstrap Icons), см. <see cref="EntityIcons"/>.</summary>
	public string Icon { get; init; } = EntityIcons.Fallback;

	public bool Expandable { get; init; }

	/// <summary>Узел-родитель; null у корня. Нужен, чтобы перечитать его после удаления узла.</summary>
	public BrowserNode? Parent { get; init; }

	/// <summary>Дети; null — ещё не грузили. Ленивость как у FAKE_NODE в десктопе.</summary>
	public List<BrowserNode>? Children { get; set; }

	public bool Expanded { get; set; }
}

/// <param name="FromButton">Открыто кнопкой «⋯», а не правой кнопкой мыши.</param>
public sealed record BrowserNodeMenuRequest(
	BrowserNode Node, Microsoft.AspNetCore.Components.Web.MouseEventArgs Mouse, bool FromButton);
