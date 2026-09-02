using System.Data;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Загружает и строит дерево меню — веб-аналог
/// <c>FogSoft.WinForm.Classes.MenuManager.CreateApplicationMenu</c>, но без
/// единой ссылки на <c>System.Windows.Forms</c>: там дерево собирается через
/// <see cref="DataRelation"/> и рисуется в <c>MenuStrip</c>, здесь — обычные
/// объекты <see cref="MenuNode"/>, которые рисует Razor-компонент.
///
/// Процедура та же самая, `UserMenuItems`, с теми же параметрами
/// (`userID`, `languageCode`) — права на видимость пункта уже посчитаны на
/// стороне SQL (столбец `enabled`), переписывать эту логику в C# не нужно.
/// </summary>
public static class MenuService
{
	public static List<MenuNode> Load()
	{
		var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			[SecurityManager.ParamNames.UserId] = SecurityManager.LoggedUser.Id,
			["languageCode"] = System.Configuration.ConfigurationManager.AppSettings["Language"] ?? "ru",
		};
		DataSet ds = DataAccessor.LoadDataSet("UserMenuItems", parameters);
		DataTable dt = ds.Tables[0];

		// menuID/parentID — smallint в базе (boxed short). Тот же класс ошибки,
		// что и сегодняшний баг в Campaign.WinForms.cs: приводить через
		// int.Parse, не через (int) — прямое приведение упало бы на unboxing.
		var byId = new Dictionary<int, MenuNode>();
		var roots = new List<MenuNode>();

		// Сортировка по position — тот же порядок, что и в десктопном меню.
		foreach (DataRow row in dt.Select(null, "position"))
		{
			var node = new MenuNode
			{
				MenuId = int.Parse(row["menuID"].ToString()!),
				Name = row["name"].ToString() ?? "",
				CodeName = row["codeName"] == DBNull.Value ? null : row["codeName"].ToString(),
				Enabled = row["enabled"] != DBNull.Value && Convert.ToBoolean(row["enabled"]),
				ImgResourcePath = row["imgResourcePath"] == DBNull.Value ? null : row["imgResourcePath"].ToString(),
			};
			byId[node.MenuId] = node;
		}

		foreach (DataRow row in dt.Select(null, "position"))
		{
			int id = int.Parse(row["menuID"].ToString()!);
			MenuNode node = byId[id];
			if (row["parentID"] == DBNull.Value)
			{
				roots.Add(node);
			}
			else
			{
				int parentId = int.Parse(row["parentID"].ToString()!);
				// Родитель мог не попасть в выборку (например, если у него самого
				// нет прав, а у потомка — по ошибке метаданных, есть). Десктоп в
				// этом случае просто не находит родителя через DataRelation и
				// пункт не попадает в дерево вообще — здесь то же самое.
				if (byId.TryGetValue(parentId, out MenuNode? parent))
					parent.Children.Add(node);
			}
		}

		return roots;
	}
}
