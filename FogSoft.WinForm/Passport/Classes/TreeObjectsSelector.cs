using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;

namespace FogSoft.WinForm.Passport.Classes
{
	internal class TreeObjectsSelector : PageFieldSelector
	{
		private readonly TreeView2 tree = new TreeView2();

		public TreeObjectsSelector(XPathNavigator navigator, PageContext context) 
			: base(navigator, context, context.PageType)
		{
			tree.CheckBoxes = true;
			tree.Height = GetHeight(navigator);
			tree.Name = GetControlName(navigator);
			tree.SelectedItemsImageColumn = "image";
			DataTable dt = GetSourceDataTable(navigator, context);
			if (dt == null)
				throw new NotImplementedException();
			tree.InitFixedTree(dt, navigator.GetAttribute(Attributes.ColumnId, ""), 
				navigator.GetAttribute(Attributes.ColumnParentid, ""), 
				navigator.GetAttribute(Attributes.ColumnName, ""));
		}

		private static int GetHeight(XPathNavigator navigator)
		{
			return ParseHelper.ParseToInt32(navigator.GetAttribute(Attributes.Height, ""), 0);
		}

		public override void SetValue(Dictionary<string, object> parameters)
		{
			if (parameters.ContainsKey(tree.Name))
			{
				throw new NotImplementedException();
			}
		}

		public override void ApplyChanges(Dictionary<string, object> parameters)
		{
			throw new NotImplementedException();
		}

		public override void Add2Page(Control parent, int left, int top, PageDimensions dimensions)
		{
			Add2Page(controlInLeftColumn, parent, left, top, dimensions);

			int gridTop = top + controlInLeftColumn.Height;

			tree.Width = dimensions.MaximumControlWidth;
			if (tree.Height == 0)
			{
				tree.Height = parent.Height - gridTop - PageDimensions.Offsets.BottomMargin;
			}

			Add2Page(tree, parent, left, gridTop, dimensions);
		}

		public override int Height
		{
			get { return tree.Height + controlInLeftColumn.Height; }
		}

		public override void OnAfterCreate()
		{
			base.OnAfterCreate();
			tree.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			if (aStyles.HasValue)
				tree.Anchor = aStyles.Value;
		}
	}
}
