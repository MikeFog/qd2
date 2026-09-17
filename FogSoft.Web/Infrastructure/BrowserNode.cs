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
	public required string Name { get; init; }
	public required IObjectContainer Container { get; init; }
	public bool Expandable { get; init; }

	/// <summary>Дети; null — ещё не грузили. Ленивость как у FAKE_NODE в десктопе.</summary>
	public List<BrowserNode>? Children { get; set; }

	public bool Expanded { get; set; }
}
