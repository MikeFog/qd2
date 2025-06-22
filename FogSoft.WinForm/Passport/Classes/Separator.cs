using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FogSoft.WinForm.Passport.Classes
{
	internal class Separator : PageControl
	{
		public Separator()
			: base(new Label())
		{
			((Label) control).BorderStyle = BorderStyle.Fixed3D;
		}

		public override void Add2Page(Control parent, int left, int top, PageDimensions dimensions)
		{
			control.Size = new Size(dimensions.MaximumControlWidth, 2);
			base.Add2Page(parent, left, top, dimensions);
		}

		public override void SetValue(Dictionary<string, object> parameters) {}

		public override void ApplyChanges(Dictionary<string, object> parameters) {}

		internal override void Focus() {}

		public override void OnAfterCreate()
		{
			base.OnAfterCreate();
            control.Anchor =  AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		}
	}
}