using System;
using System.Windows.Forms;

namespace FogSoft.WinForm.Controls
{
	public partial class TimeDuration : UserControl
	{
		public event EmptyDelegate ValueChanged;

		public TimeDuration()
		{
			InitializeComponent();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			Height = txtMin.Height;
		}

		public int Value
		{
			get { return (int) (txtMin.Value*60 + txtSec.Value); }
			set
			{
				int min, hour;
				hour = Math.DivRem(value, 60, out min);
				txtMin.Value = hour;
				txtSec.Value = min;
			}
		}

		private void OnValueChanged(object sender, EventArgs e)
		{
			if(ValueChanged != null) ValueChanged();
		}

		private void txtSec_KeyUp(object sender, KeyEventArgs e)
		{
			OnValueChanged(sender, e);
		}

		private void txtMin_KeyUp(object sender, KeyEventArgs e)
		{
			OnValueChanged(sender, e);
		}
	}
}