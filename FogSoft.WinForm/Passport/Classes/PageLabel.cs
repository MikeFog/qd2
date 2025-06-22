using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FogSoft.WinForm.Passport.Classes
{
	internal class PageLabel : PageControl
	{
		public PageLabel(string text) : base(new Label())
		{
			control.AutoSize = true;
			control.Text = text;
		}

		public Color Color
		{
			set { control.ForeColor = value; }
		}

		public void SetBold()
		{
			control.Font = new Font(control.Font, FontStyle.Bold); 
		}

		public override void SetValue(Dictionary<string, object> parameters) {}

		public override void ApplyChanges(Dictionary<string, object> parameters) {}

		internal override void Focus() {}
	}
}