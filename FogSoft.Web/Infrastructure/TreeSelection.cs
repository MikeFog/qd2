using System.Data;
using FogSoft.WinForm.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Состояние контрола <c>treeselector</c>: дерево, развёрнутое из плоской таблицы
/// с parentID, и отметки на нём. Веб-аналог десктопного <c>TreeView2</c> в режиме
/// <c>InitFixedTree</c> (<c>TreeObjectsSelector</c>).
///
/// Лежит значением поля в объекте паспорта (<c>obj[name]</c>), а не в компоненте:
/// диалог по «ОК» с ошибкой показывается заново, карточка может оказаться новым
/// экземпляром, и отметки не должны теряться. Наружу значение — <see cref="AddedIDs"/>.
///
/// <b>Отметки — дословно TvStructure_AfterCheck → CheckNode / UncheckNode.</b>
/// Отметка узла: его id в AddedIDs, все предки отмечаются, все неотмеченные
/// потомки отмечаются вместе со своими id. Снятие: id убирается, потомки снимаются,
/// предков не трогаем. Важная тонкость десктопа, сохранённая здесь: предок,
/// отмеченный каскадом вверх, отмечен <i>только на экране</i> — его id в AddedIDs не
/// попадает (в режиме DataSource CheckNode зовёт для предка CheckObject с null).
/// Поэтому отмеченный выпуск не тянет за собой в выборку весь день.
/// </summary>
public sealed class TreeSelection
{
	/// <summary>
	/// Колонка с именем картинки узла (Day.png, Issue.png). Имя зашито и в
	/// десктопе: TreeObjectsSelector ставит SelectedItemsImageColumn = "image",
	/// в XML его нет.
	/// </summary>
	private const string ImageColumn = "image";

	private readonly List<object> _addedIds = new();
	private readonly List<object> _deletedIds = new();

	private TreeSelection(DataTable table, PassportTree tree)
	{
		Table = table;
		Root = Build(table, tree);
	}

	/// <summary>Набор строк, из которого построено дерево.</summary>
	public DataTable Table { get; }

	/// <summary>
	/// Корень «Все» — как в десктопе (<c>ShowData</c> добавляет его сам). Его
	/// отметка отмечает всё дерево. Своего id у корня нет.
	/// </summary>
	public TreeSelectionNode Root { get; }

	/// <summary>
	/// Отмеченные id — десктопный <c>TreeView2.AddedIDs</c>, в порядке отметки.
	/// Значения те же, что в колонке id набора строк (тот же тип), поэтому
	/// сравниваются с <c>row["id"]</c> напрямую.
	/// </summary>
	public IReadOnlyList<object> AddedIDs => _addedIds;

	/// <summary>
	/// Состояние поля <paramref name="name"/> объекта паспорта: уже созданное либо новое по
	/// <paramref name="table"/>. Если набор строк заменили (например, паспорт
	/// перечитал данные), дерево строится заново.
	/// </summary>
	public static TreeSelection For(PresentationObject obj, string name, DataTable table, PassportTree tree)
	{
		if (obj[name] is TreeSelection existing && existing.Table == table)
			return existing;

		var created = new TreeSelection(table, tree);
		obj[name] = created;
		return created;
	}

	/// <summary>
	/// Отмеченные id поля из собранных значений паспорта (копия словаря
	/// параметров — ссылка на состояние та же); пусто, если дерево не трогали.
	/// </summary>
	public static IReadOnlyList<object> AddedIDsOf(IDictionary<string, object> values, string name) =>
		values.TryGetValue(name, out object? current) && current is TreeSelection selection
			? selection.AddedIDs
			: Array.Empty<object>();

	/// <summary>Щелчок по галочке узла: новое состояние <paramref name="on"/>.</summary>
	public void Toggle(TreeSelectionNode node, bool on)
	{
		node.Checked = on;
		if (on)
			CheckNode(node);
		else
			UncheckNode(node);
	}

	private void CheckNode(TreeSelectionNode node)
	{
		CheckId(node.Id);

		for (TreeSelectionNode? parent = node.Parent; parent != null; parent = parent.Parent)
			parent.Checked = true;

		foreach (TreeSelectionNode child in node.Children)
			if (!child.Checked)
			{
				CheckNode(child);
				child.Checked = true;
			}
	}

	private void UncheckNode(TreeSelectionNode node)
	{
		UncheckId(node.Id);

		foreach (TreeSelectionNode child in node.Children)
			if (child.Checked)
			{
				UncheckNode(child);
				child.Checked = false;
			}
	}

	// CheckIDs / UncheckIDs десктопа: пара списков «добавлено / удалено». Повторная
	// отметка снятого не добавляет id, а убирает его из снятых. Id корня (null)
	// десктоп тоже кладёт в AddedIDs, но все потребители его отбрасывают — здесь не
	// кладём вовсе.
	private void CheckId(object? id)
	{
		if (id == null)
			return;
		if (_deletedIds.Contains(id))
			_deletedIds.Remove(id);
		else if (!_addedIds.Contains(id))
			_addedIds.Add(id);
	}

	private void UncheckId(object? id)
	{
		if (id == null)
			return;
		if (_addedIds.Contains(id))
			_addedIds.Remove(id);
		else if (!_deletedIds.Contains(id))
			_deletedIds.Add(id);
	}

	/// <summary>
	/// Плоская таблица → дерево: корни — строки с пустым parentID, дети ищутся
	/// по совпадению parentID с id родителя (сравнение строками, как фильтр
	/// <c>parentID = '{tag}'</c> в десктопе). Десктоп строит ровно два уровня;
	/// здесь глубина любая, для двухуровневых данных результат тот же.
	/// </summary>
	private static TreeSelectionNode Build(DataTable table, PassportTree tree)
	{
		var root = new TreeSelectionNode(null, Tr.T("Все"), null) { Expanded = true };

		var byParent = new Dictionary<string, List<DataRow>>();
		var roots = new List<DataRow>();
		foreach (DataRow row in table.Rows)
		{
			object parentId = row[tree.ColumnParentId];
			if (parentId == DBNull.Value)
			{
				roots.Add(row);
				continue;
			}

			string key = parentId.ToString() ?? "";
			if (!byParent.TryGetValue(key, out List<DataRow>? list))
				byParent[key] = list = new List<DataRow>();
			list.Add(row);
		}

		foreach (DataRow row in roots)
			root.Children.Add(CreateNode(row, root, tree, byParent));
		return root;
	}

	private static TreeSelectionNode CreateNode(DataRow row, TreeSelectionNode parent, PassportTree tree,
		Dictionary<string, List<DataRow>> byParent)
	{
		object id = row[tree.ColumnId];
		var node = new TreeSelectionNode(id, row[tree.ColumnName].ToString() ?? "", parent)
		{
			// Картинка десктопа → значок Bootstrap тем же словарём, что у сущностей.
			Icon = row.Table.Columns.Contains(ImageColumn) && row[ImageColumn] is string image && image.Length > 0
				? EntityIcons.For(image)
				: null,
		};

		if (byParent.TryGetValue(id.ToString() ?? "", out List<DataRow>? children))
			foreach (DataRow child in children)
				node.Children.Add(CreateNode(child, node, tree, byParent));
		return node;
	}
}

/// <summary>Узел дерева <see cref="TreeSelection"/>.</summary>
public sealed class TreeSelectionNode
{
	public TreeSelectionNode(object? id, string name, TreeSelectionNode? parent)
	{
		Id = id;
		Name = name;
		Parent = parent;
	}

	/// <summary>Значение колонки id; null у корня «Все».</summary>
	public object? Id { get; }
	public string Name { get; }
	public TreeSelectionNode? Parent { get; }
	public List<TreeSelectionNode> Children { get; } = new();

	/// <summary>Класс значка Bootstrap Icons; null — без значка (корень «Все», набор без колонки image).</summary>
	public string? Icon { get; init; }

	/// <summary>Галочка на экране. Не то же, что «id в AddedIDs», см. <see cref="TreeSelection"/>.</summary>
	public bool Checked { get; set; }

	/// <summary>Раскрыт ли узел. Как в десктопе: раскрыт только корень.</summary>
	public bool Expanded { get; set; }
}
