using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Properties;

namespace FogSoft.WinForm.Classes
{
	public enum ViewType
	{
		Tree,
		Journal
	}

	public static class MenuManager
	{
		public static ContextMenuStrip CreatePopupMenu(
			IActionHandler actionHandler, EventHandler onMenuItemClick, ViewType type)
		{
            if (actionHandler == null)
                return null;

			ContextMenuStrip menuStrip = new ContextMenuStrip();

			if (actionHandler.ActionList == null)
				return menuStrip;

			bool? isLastSeparator = null;

			foreach(Entity.Action action in actionHandler.ActionList)
			{
				if(action.Alias == "-")
				{
					if (isLastSeparator.HasValue && !isLastSeparator.Value)
					{
						ToolStripItem menuItem = new ToolStripSeparator {Enabled = false};
						menuStrip.Items.Add(menuItem);
						isLastSeparator = true;
					}
				}
				else
				{
					string alias = action.Alias;

					if (ConfigurationUtil.IsTestMode)
						alias += string.Format(" (EntityID - {0}, entityActionID - {1})", action.EntityID, action.EntityActionID);

					if (!actionHandler.IsActionHidden(action.Name, type))
					{
						ToolStripMenuItem menuItem = new ToolStripMenuItem(alias, null, onMenuItemClick)
						                             	{
						                             		Name = action.Name,
						                             		Enabled = actionHandler.IsActionEnabled(action.Name, type)
						                             	};
						Image img = Globals.GetImage(action.ImgResourceName);
						if (img != null)
							menuItem.Image = img;

						foreach (var childAction in action.ChildActions)
						{
							if (childAction.Alias == "-")
							{
                                menuItem.DropDownItems.Add(new ToolStripSeparator { Enabled = false });
                            }
							else
							{
								ToolStripMenuItem item = new ToolStripMenuItem(childAction.Alias, null, onMenuItemClick)
								{
									Name = childAction.Name,
									Enabled = actionHandler.IsActionEnabled(childAction.Name, type)
								};
								menuItem.DropDownItems.Add(item);
							}
                        }

                        menuStrip.Items.Add(menuItem);
						isLastSeparator = false;
					}
				}
			}

			// remove last if is Separator
			if (isLastSeparator.HasValue && isLastSeparator.Value)
				menuStrip.Items.RemoveAt(menuStrip.Items.Count - 1);

			return menuStrip;
		}

		public static void CreateApplicationMenu(MenuStrip appMenu, EventHandler onMenuItemClick, string lang)
		{
			DataSet ds = GetApplicationMenu(lang);
			CreateTopLevelMenus(ds.Tables[0], appMenu, onMenuItemClick);
			CreateMDIListMenu(appMenu);
		}

		private static DataSet GetApplicationMenu(string lang)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters.Add(SecurityManager.ParamNames.UserId, SecurityManager.LoggedUser.Id);
            parameters.Add("languageCode", lang);
            
            DataSet ds = DataAccessor.LoadDataSet("UserMenuItems", parameters);
			SetRelations(ds);
			return ds;
		}

		private static void SetRelations(DataSet ds)
		{
			DataTable dt = ds.Tables[0];
			ds.Relations.Add("MenuRelation", dt.Columns["menuId"], dt.Columns["parentId"]);
		}

		public static void CreateMDIListMenu(MenuStrip appMenu)
		{
			ToolStripMenuItem menuItemWin = new ToolStripMenuItem("Окно") {Image = Resources.Window};
			appMenu.Items.Add(menuItemWin);
			appMenu.MdiWindowListItem = menuItemWin;

			CreateMDIListMenuItem(menuItemWin, "Сверху вниз", MdiLayout.TileHorizontal, Resources.WindowDown);
			CreateMDIListMenuItem(menuItemWin, "Слева направо", MdiLayout.TileVertical, Resources.WindowLeft);
			CreateMDIListMenuItem(menuItemWin, "Каскадом", MdiLayout.Cascade, Resources.WindowCascad);
			CreateMDIListMenuItem(menuItemWin, "Упорядочить значки", MdiLayout.ArrangeIcons, Resources.WindowOrder);
		}

		private static void CreateMDIListMenuItem(
			ToolStripMenuItem menuItemWin, string text, MdiLayout layoutValue, Image img)
		{
			ToolStripMenuItem mi = new ToolStripMenuItem(text, null, arrangeWindowsHandler) {Tag = layoutValue, Image = img};
			menuItemWin.DropDown.Items.Add(mi);
		}

		private static void arrangeWindowsHandler(object sender, EventArgs e)
		{
			ToolStripMenuItem menuItem = (ToolStripMenuItem) sender;
            Globals.MdiParent.LayoutMdi((MdiLayout)menuItem.Tag);

            //appMDIForm.LayoutMdi((MdiLayout) menuItem.Tag);
		}

		private static void CreateTopLevelMenus(
			DataTable dt, MenuStrip appMenu, EventHandler onMenuItemClick)
		{
			foreach(DataRow row in dt.Select("parentID IS Null"))
			{
				ToolStripItem tsi = CreateMenuItem(row);
				appMenu.Items.Add(tsi);

				ToolStripMenuItem mi = tsi as ToolStripMenuItem;
				if(mi != null && mi.Enabled)
				{
					mi.Click += onMenuItemClick;
					CreateChildrenMenus(mi, row, dt.DataSet.Relations["MenuRelation"], onMenuItemClick);
				}
			}
		}

		private static void CreateChildrenMenus(
			ToolStripMenuItem parentItem,
			DataRow row, DataRelation relation, EventHandler onMenuItemClick)
		{
			foreach(DataRow childRow in row.GetChildRows(relation))
			{
				ToolStripItem mi = CreateMenuItem(childRow);
				mi.Click += onMenuItemClick;

				DataRow[] rowSubChild = childRow.GetChildRows(relation);
				if (rowSubChild.Length > 0)
				{
					CreateChildrenMenus(mi as ToolStripMenuItem, childRow, relation, onMenuItemClick);
				}

				parentItem.DropDownItems.Add(mi);
			}
		}

		public static ToolStripMenuItem FindByTag(ToolStripMenuItem menu, string tag)
		{
			foreach (ToolStripItem item in menu.DropDownItems)
			{
				if (!(item is ToolStripMenuItem))
					continue;
				if (item.Tag.ToString() == tag)
					return (ToolStripMenuItem)item;
			}
			return null;
		}

		public static ToolStripMenuItem FindByTag(MenuStrip menu, string tag)
		{
			ToolStripMenuItem firstItem = null;
			foreach (ToolStripItem item in menu.Items)
			{
				if (!(item is ToolStripMenuItem))
					continue;
				firstItem = (ToolStripMenuItem)((tag == Convert.ToString(item.Tag)) ? item : FindByTag((ToolStripMenuItem)item, tag));
				if (firstItem != null)
					return firstItem;
			}
			return firstItem;
		}

		private static ToolStripItem CreateMenuItem(DataRow row)
		{
			if(row["name"].ToString() == "-")
				return new ToolStripSeparator
				{
					Enabled = ((bool)row["enabled"]),
					Alignment =
						  ParseHelper.GetEnumValue(row["align"].ToString(), ToolStripItemAlignment.Left)
				};

			ToolStripMenuItem menuItem = new ToolStripMenuItem(row["name"].ToString())
			                             	{
			                             		ShortcutKeys = ParseHotKey(row["hotKey"]),
			                             		Enabled = ((bool) row["enabled"]),
			                             		Tag = row["codeName"],
			                             		Alignment =
													ParseHelper.GetEnumValue(row["align"].ToString(), ToolStripItemAlignment.Left),
												Image = Globals.GetImage(row["imgResourcePath"].ToString())
			                             	};

			return menuItem;
		}

		private static Keys ParseHotKey(object hotKey)
		{
			Keys hotKeys = Keys.None;
			if (StringUtil.IsDBNullOrNull(hotKey))
				return hotKeys;
			string keys = Convert.ToString(hotKey);
			if (!Regex.IsMatch(keys, @"^(Alt\+|Ctrl\+|Shift\+){1,3}[a-z,A-Z,0-9]$", RegexOptions.IgnoreCase))
				return hotKeys;
			if (keys.Contains("Alt"))
				hotKeys |= Keys.Alt;
			if (keys.Contains("Shift"))
				hotKeys |= Keys.Shift;
			if (keys.Contains("Ctrl"))
				hotKeys |= Keys.Control;
			try
			{
				hotKeys |= (Keys)Enum.Parse(typeof (Keys), keys.Substring(keys.LastIndexOf('+') + 1, 1));
			}
			catch(ArgumentException)
			{
				return Keys.None;
			}
			return hotKeys;
		}
	}
}