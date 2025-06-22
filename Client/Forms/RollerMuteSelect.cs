using System.Windows.Forms;

namespace Merlin.Forms
{
	public partial class RollerMuteSelect : Form
	{
		public RollerMuteSelect()
		{
			InitializeComponent();
		}

		public int TimeDuration
		{
			get { return timeDuration.Value; }
		}

		private void timeDuration_ValueChanged()
		{
			btnOk.Enabled = timeDuration.Value > 0;
		}
	}
}