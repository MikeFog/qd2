using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace FogSoft.WinForm.Forms
{
	public partial class ProgressForm : Form
	{
		#region Show Progress Form

		public static object Show(IWin32Window form, DoWorkEventHandler method, object state)
		{
			return DoShow(form, new ProgressForm(method, state));
		}

		public static object Show(IWin32Window form, DoWorkEventHandler method, string text, object state)
		{
			return DoShow(form, new ProgressForm(method, text, state));
		}

		public static object Show(IWin32Window form, DoWorkEventHandler method, int min, int max, int step, object state)
		{
			return DoShow(form, new ProgressForm(method, min, max, step, state));
		}

		public static object Show(IWin32Window form, DoWorkEventHandler method, int min, int max, int step, string text, object state)
		{
			return DoShow(form, new ProgressForm(method, min, max, step, text, state));
		}

		private static object DoShow(IWin32Window form, ProgressForm frm)
		{
			frm.ShowDialog(form);
			return frm.Result;
		}

		#endregion

        private readonly object state;

		private ProgressForm(DoWorkEventHandler method, object state)
			: this(method, 0, 0, 0, null, state, ProgressBarStyle.Marquee)
		{
		}

		private ProgressForm(DoWorkEventHandler method, string text, object state)
			: this(method, 0, 0, 0, text, state, ProgressBarStyle.Marquee)
		{
		}

		private ProgressForm(DoWorkEventHandler method, int min, int max, int step, object state)
			: this(method, min, max, step, null, state, ProgressBarStyle.Continuous)
		{
		}

		private ProgressForm(DoWorkEventHandler method, int min, int max, int step, string text, object state)
			: this(method, min, max, step, text, state, ProgressBarStyle.Continuous)
		{
		}

		private ProgressForm(DoWorkEventHandler method, int min, int max, int step, string text, object state, ProgressBarStyle style)
		{
			InitializeComponent();

			pbClose.Visible = style != ProgressBarStyle.Marquee;
			backgroundWorker.DoWork += method;
			progressBar.Maximum = max;
			progressBar.Minimum = min;
			progressBar.Step = step;
			if (!string.IsNullOrEmpty(text))
				lblText.Text = text;
			progressBar.Style = style;
			this.state = state;
		}	

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

			Application.DoEvents();

			if (state != null)
				backgroundWorker.RunWorkerAsync(state);
			else 
				backgroundWorker.RunWorkerAsync();
		}

		private void pbClose_Click(object sender, EventArgs e)
		{
			backgroundWorker.CancelAsync();
		}

		private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			progressBar.Value = (int)(e.UserState ?? e.ProgressPercentage);
		}

		private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (!e.Cancelled)
				Result = e.Result;
			Application.DoEvents();
			Close();
		}

		private object Result { get; set; }
	}
}
