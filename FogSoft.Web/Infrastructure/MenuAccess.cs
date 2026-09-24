namespace FogSoft.Web.Infrastructure;

/// <summary>Результат проверки доступа к журналу сущности.</summary>
public enum JournalAccess
{
	Allowed,

	/// <summary>Экран есть, но пользователю не разрешён (пункт меню погашен).</summary>
	Denied,

	/// <summary>Сущность вообще не выведена в веб — маршрута на неё нет.</summary>
	NotPorted,
}

/// <summary>
/// Меню текущего пользователя и вытекающий из него ответ на вопрос «можно ли
/// этому пользователю открыть журнал такой-то сущности».
///
/// Зачем нужно. В десктопе доступ к экрану — это и есть пункт меню: нет пункта
/// (<c>UserMenuItems.enabled = 0</c>) — нет и способа открыть форму. В вебе
/// адрес набирается руками, поэтому то же самое правило нужно проверять на
/// сервере. Никакой новой модели прав здесь не заводится: разрешение берётся из
/// той же процедуры <c>UserMenuItems</c>, а соответствие «пункт меню → сущность»
/// — из <see cref="MenuRoutes"/>, то есть из разбора десктопного
/// <c>MDIForm.MenuItemClick</c>.
///
/// Scoped — свой на circuit: меню персональное. Перечитывается при смене
/// пользователя, как и само меню в NavMenu.
///
/// Границы применимости. Сейчас правило строгое: сущность доступна, только если
/// на неё ведёт разрешённый пункт меню. Для этапа 1 это верно — единственный
/// маршрут <c>/journal/{id}</c> и попадают на него из меню. На этапе 2 появятся
/// дочерние журналы и селекторы, куда попадают изнутри уже открытого экрана, —
/// тогда правило нужно будет дополнить «доступно из родительского экрана»,
/// а не ослаблять это.
/// </summary>
public sealed class MenuAccess
{
	private readonly UserSession _session;

	private List<MenuNode>? _tree;
	private HashSet<int>? _allowedEntities;
	private HashSet<string>? _allowedBrowsers;
	private HashSet<string>? _allowedScreens;
	private int? _loadedFor;
	private string? _loadedLanguage;

	public MenuAccess(UserSession session)
	{
		_session = session;
	}

	/// <summary>Дерево меню для отрисовки. Загружается один раз на circuit.</summary>
	public IReadOnlyList<MenuNode> Tree
	{
		get
		{
			EnsureLoaded();
			return _tree!;
		}
	}

	/// <summary>
	/// Разрешён ли пользователю журнал этой сущности. Ответ строится из
	/// разрешённых пунктов меню, а не из отдельного списка, — чтобы права на
	/// экраны оставались ровно там же, где их правит администратор.
	///
	/// Отказ различает два случая, которые нельзя показывать одинаково: «прав
	/// нет» и «такого экрана в вебе ещё нет». Сказать администратору, у которого
	/// прав по определению все, что ему «не разрешено», — это неверная
	/// диагностика, а не строгость.
	/// </summary>
	public JournalAccess CheckJournal(int entityId)
	{
		EnsureLoaded();

		if (_allowedEntities!.Contains(entityId))
			return JournalAccess.Allowed;

		return MenuRoutes.SimpleJournal.Values.Any(r => r.EntityId == entityId)
			? JournalAccess.Denied
			: JournalAccess.NotPorted;
	}

	/// <summary>
	/// Разрешён ли пользователю древовидный экран. Правило то же, что у
	/// журнала: доступ даёт разрешённый пункт меню, а не отдельный список.
	/// </summary>
	public JournalAccess CheckBrowser(string codeName)
	{
		EnsureLoaded();

		if (_allowedBrowsers!.Contains(codeName))
			return JournalAccess.Allowed;

		return MenuRoutes.Browser.ContainsKey(codeName)
			? JournalAccess.Denied
			: JournalAccess.NotPorted;
	}

	/// <summary>Доступ к своему экрану (ScreenRoutes) — по тому же пункту меню пользователя.</summary>
	public JournalAccess CheckScreen(string codeName)
	{
		EnsureLoaded();

		if (_allowedScreens!.Contains(codeName))
			return JournalAccess.Allowed;

		return ScreenRoutes.Screens.ContainsKey(codeName)
			? JournalAccess.Denied
			: JournalAccess.NotPorted;
	}

	/// <summary>
	/// Текст пункта меню по <c>codeName</c> — заголовок журнала, как в десктопе
	/// (<c>mi.Text</c>). <c>null</c>, если пункта нет в меню пользователя.
	/// </summary>
	public string? MenuText(string codeName)
	{
		EnsureLoaded();
		return FindText(_tree!, codeName);
	}

	private static string? FindText(IEnumerable<MenuNode> nodes, string codeName)
	{
		foreach (MenuNode node in nodes)
		{
			if (string.Equals(node.CodeName, codeName, StringComparison.OrdinalIgnoreCase))
				return node.Name;

			string? inChildren = FindText(node.Children, codeName);
			if (inChildren != null)
				return inChildren;
		}
		return null;
	}

	/// <summary>
	/// Перезагружает меню, если сменился пользователь. Сверка по id, а не подписка
	/// на <c>UserSession.Changed</c>, — чтобы результат не зависел от порядка, в
	/// котором сработают обработчики события (NavMenu подписан на то же самое).
	/// </summary>
	private void EnsureLoaded()
	{
		int? currentUser = _session.User?.Id;
		// Названия пунктов зависят от языка — при его смене меню перечитывается.
		string language = _session.Language ?? WebLanguage.Default;
		if (_tree != null && _loadedFor == currentUser && _loadedLanguage == language)
			return;

		_loadedFor = currentUser;
		_loadedLanguage = language;
		_allowedEntities = new HashSet<int>();
		_allowedBrowsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		_allowedScreens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (currentUser == null)
		{
			_tree = new List<MenuNode>();
			return;
		}

		_tree = MenuService.Load(language);
		Collect(_tree);
	}

	private void Collect(IEnumerable<MenuNode> nodes)
	{
		foreach (MenuNode node in nodes)
		{
			// enabled считает сама UserMenuItems (isPublic, админ, персональное
			// разрешение, группа) — здесь только читаем результат.
			if (node.Enabled && !string.IsNullOrEmpty(node.CodeName))
			{
				if (MenuRoutes.SimpleJournal.TryGetValue(node.CodeName!, out JournalRoute? journal))
					_allowedEntities!.Add(journal.EntityId);

				if (MenuRoutes.Browser.ContainsKey(node.CodeName!))
					_allowedBrowsers!.Add(node.CodeName!);
				if (ScreenRoutes.Screens.ContainsKey(node.CodeName!))
					_allowedScreens!.Add(node.CodeName!);
			}

			Collect(node.Children);
		}
	}
}
