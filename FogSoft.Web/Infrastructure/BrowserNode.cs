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
	/// null — лист: объект не контейнер (<see cref="LoadChildren"/> с
	/// includeLeaves). Древовидный экран листьев не строит, поэтому там
	/// контейнер есть всегда.
	/// </summary>
	public required IObjectContainer? Container { get; init; }

	/// <summary>Доменный объект узла; null у корня (FakeContainer — не объект строки).</summary>
	public PresentationObject? Object { get; init; }

	/// <summary>Класс значка узла (Bootstrap Icons), см. <see cref="EntityIcons"/>.</summary>
	public string Icon { get; init; } = EntityIcons.Fallback;

	public bool Expandable { get; init; }

	/// <summary>Узел-родитель; null у корня. Нужен, чтобы перечитать его после удаления узла.</summary>
	public BrowserNode? Parent { get; init; }

	/// <summary>Дети; null — ещё не грузили. Ленивость как у FAKE_NODE в десктопе.</summary>
	public List<BrowserNode>? Children { get; set; }

	public bool Expanded { get; set; }

	/// <summary>
	/// Дети узла — перечисление его контейнера. Перечисление, а не GetContent():
	/// ObjectsIterator по пути создаёт доменные объекты и передаёт им сценарий,
	/// из которого объект узнаёт собственную дочернюю сущность. Общее у
	/// древовидного экрана (Browser.razor) и выбора из дерева (ScenarioTreePicker).
	/// </summary>
	/// <param name="includeLeaves">
	/// Показывать и объекты-не-контейнеры — листом без детей, как
	/// TreeView2.AddObject2Node. Выбору из дерева они нужны (предмет рекламы
	/// 2-го уровня — сущность 1243 без своего класса); древовидный экран их не
	/// строит: там такие объекты видны списком справа.
	/// </param>
	public static List<BrowserNode> LoadChildren(BrowserNode node, bool includeLeaves = false)
	{
		IObjectContainer parent = node.Container
			?? throw new InvalidOperationException("У листа дерева нет детей.");

		var children = new List<BrowserNode>();
		foreach (PresentationObject obj in parent)
		{
			var container = obj as IObjectContainer;
			if (container == null && !includeLeaves)
				continue;

			children.Add(new BrowserNode
			{
				Name = obj.Name,
				Container = container,
				Object = obj,
				// Как в TreeView2: значок — у дочерней сущности контейнера-родителя.
				Icon = EntityIcons.For((parent.ChildEntity ?? obj.Entity)?.IconName),
				Expandable = container != null && IsExpandable(obj, container),
				Parent = node,
			});
		}
		return children;
	}

	/// <summary>
	/// Повторяет TreeView2.IsExpandableObject. Колонка isHasChilds — способ
	/// процедуры сказать «у этой строки детей нет»; по умолчанию считаем, что
	/// есть.
	/// </summary>
	private static bool IsExpandable(PresentationObject obj, IObjectContainer container) =>
		container.IsChildNodeExpandable
		&& ParseHelper.GetBooleanFromObject(obj["isHasChilds"], true);
}

/// <param name="FromButton">Открыто кнопкой «⋯», а не правой кнопкой мыши.</param>
public sealed record BrowserNodeMenuRequest(
	BrowserNode Node, Microsoft.AspNetCore.Components.Web.MouseEventArgs Mouse, bool FromButton);
