using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;

namespace FogSoft.WinForm.Passport.Classes
{
	internal class ObjectsSelector : PageFieldSelector
	{
		private readonly SmartGrid grid = new SmartGrid();
		private readonly DataTable dtSource;
		private readonly PresentationObject presentationObject;

		public ObjectsSelector(XPathNavigator navigator, PageContext context)
			: base(navigator, context.PageType)
		{
			presentationObject = context.PresentationObject;
			grid.CaptionVisible = grid.QuickSearchVisible = grid.MenuEnabled = false;
			grid.CheckBoxes = HasCheckBox(navigator);
			grid.Entity = GetEntity(navigator);
			dtSource = GetSourceDataTable(navigator, context);
			grid.ObjectChecked += grid_ObjectChecked;
			grid.Height = GetHeight(navigator);
			grid.Name = GetControlName(navigator);
		}

		private static int GetHeight(XPathNavigator navigator)
		{
			return ParseHelper.ParseToInt32(navigator.GetAttribute(Attributes.Height, ""), 0);
		}

		private static bool HasCheckBox(XPathNavigator navigator)
		{
			return ParseHelper.ParseToBoolean(navigator.GetAttribute(Attributes.Multiselect, ""), false);
		}

		private void grid_ObjectChecked(PresentationObject presentationObject, bool state)
		{
			FireValueChanged();
		}

		public override void SetValue(Dictionary<string, object> parameters) {}

		public override void ApplyChanges(Dictionary<string, object> parameters)
		{
			IParentObject parentObject = presentationObject as IParentObject;
			if(parentObject != null)
				parentObject.SetChildrenChanges(
					grid.Entity, new List<PresentationObject>(grid.Added2Checked), new List<PresentationObject>(grid.RemovedFromChecked));
			grid.Added2Checked.Clear();
			grid.RemovedFromChecked.Clear();
		}

		public override void Add2Page(Control parent, int left, int top, PageDimensions dimensions)
		{
			if(dtSource != null)
				grid.DataSource = dtSource.DefaultView;

			Add2Page(controlInLeftColumn, parent, left, top, dimensions);

			int gridTop = top + controlInLeftColumn.Height;

			grid.Width = dimensions.MaximumControlWidth;
			if(grid.Height == 0)
			{
				grid.Height = parent.Height - gridTop - PageDimensions.Offsets.BottomMargin;
			}

			Add2Page(grid, parent, left, gridTop, dimensions);
		}

		public override int Height
		{
			get { return grid.Height + controlInLeftColumn.Height; }
		}

		public override void OnAfterCreate()
		{
			base.OnAfterCreate();
			grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			if (aStyles.HasValue)
				grid.Anchor = aStyles.Value;
		}
	}
}