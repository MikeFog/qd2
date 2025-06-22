using System;
using System.Collections;
using System.Data.SqlClient;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using FogSoft.WinForm.Properties;

namespace FogSoft.WinForm.Forms
{
	public enum MessageBoxIcon
	{
		Question,
		Completed,
		Information,
		Error,
		Exclamation
	}

	public enum MessageBoxButtons
	{
		Ok,
		OkCancel,
		YesNo
	}

	public sealed partial class MessageBox : Form
	{
		private static string GetFullInfoAboutException(Exception exp)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(exp.ToString());
			if (exp is SqlException)
			{
				SqlException sqlExp = exp as SqlException;
				sb.AppendLine();
				sb.AppendLine("Errors collection: ");
				foreach (SqlError error in sqlExp.Errors)
				{
					sb.AppendLine(error.ToString());
					sb.AppendLine(string.Format("Procedure: {0}", error.Procedure));
					sb.AppendLine(string.Format("Server: {0}", error.Server));
					sb.AppendLine(string.Format("Source: {0}", error.Source));
					sb.AppendLine(string.Format("Number: {0}", error.Number));
					sb.AppendLine(string.Format("LineNumber: {0}", error.LineNumber));
					sb.AppendLine(string.Format("Class: {0}", error.Class));
				}
			}
			if (exp.Data != null && exp.Data.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine("Data collection: ");
				foreach (DictionaryEntry data in exp.Data)
				{
					sb.AppendLine(string.Format("{0} - {1}", data.Key, data.Value));
				}
			}
			return sb.ToString();
		}

#region ShowMessageBox

		public static void ShowError(string title, string text, Exception e)
		{
			Show(title, text, GetFullInfoAboutException(e), MessageBoxIcon.Error, MessageBoxButtons.Ok);
		}

		public static void ShowError(string text, Exception e)
		{
			Show(Globals.MdiParent != null ? Globals.MdiParent.Text : null, text, GetFullInfoAboutException(e), MessageBoxIcon.Error, MessageBoxButtons.Ok);
		}

		public static void ShowInformation(string title, string text)
		{
			Show(title, text, null, MessageBoxIcon.Information, MessageBoxButtons.Ok);
		}

		public static void ShowInformation(string text)
		{
			Show(Application.ProductName, text, null, MessageBoxIcon.Information, MessageBoxButtons.Ok);
		}

		public static void ShowExclamation(string title, string text)
		{
			Show(title, text, null, MessageBoxIcon.Exclamation, MessageBoxButtons.Ok);
		}

		public static void ShowExclamation(string text)
		{
			Show(Application.ProductName, text, null, MessageBoxIcon.Exclamation, MessageBoxButtons.Ok);
		}

		public static DialogResult ShowQuestion(string title, string text)
		{
			return Show(title, text, null, MessageBoxIcon.Question, MessageBoxButtons.YesNo);
		}

		public static DialogResult ShowQuestion(string text)
		{
			return Show(Application.ProductName, text, null, MessageBoxIcon.Question, MessageBoxButtons.YesNo);
		}

		public static DialogResult ShowQuestion(string title, string text, MessageBoxButtons buttons)
		{
			return Show(title, text, null, MessageBoxIcon.Question, buttons);
		}

		public static DialogResult ShowQuestion(string text, MessageBoxButtons buttons)
		{
			return Show(Application.ProductName, text, null, MessageBoxIcon.Question, buttons);
		}

		public static void ShowCompleted(string title, string text)
		{
			Show(title, text, null, MessageBoxIcon.Completed, MessageBoxButtons.Ok);
		}

		public static void ShowCompleted(string text)
		{
			Show(Application.ProductName, text, null, MessageBoxIcon.Completed, MessageBoxButtons.Ok);
		}

		public static DialogResult Show(string title, string text, string advancedtext, MessageBoxIcon icon, MessageBoxButtons buttons)
		{
			MessageBox mb = new MessageBox(title, text, advancedtext, icon, buttons);
			return Globals.MdiParent == null ? mb.ShowDialog() : mb.ShowDialog(Globals.MdiParent);
		}

#endregion

		private const int _HeightFullView = 288;
		private const int _HeightSimpleView = 158;

		private bool ShowingAdvanceInfo { get; set; }

		private MessageBox(string title, string text, string advancedtext, 
				MessageBoxIcon icon, 
				MessageBoxButtons buttons) 
		{
			InitializeComponent();
			SetIcon(icon);
			SetButtons(buttons);

			if (!string.IsNullOrEmpty(title))
				Text = title;

			if (!string.IsNullOrEmpty(advancedtext))
				rtbinformation.Text = advancedtext;

			llMoreInfo.Visible = !string.IsNullOrEmpty(advancedtext);

			lblText.Text = text;
		}

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);

			if (btnOne.Visible)
				btnOne.Focus();
			else
				btnTwo.Focus();
		}

		private void SetIcon(MessageBoxIcon icon)
		{
			pictureBox.Image = (icon == MessageBoxIcon.Error) ? Resources.Error :
				(icon == MessageBoxIcon.Question) ? Resources.Question :
				(icon == MessageBoxIcon.Completed) ? Resources.Completed :
				(icon == MessageBoxIcon.Information) ? Resources.Information :
				(icon == MessageBoxIcon.Exclamation) ? Resources.Exclamation : null;
		}

		private void SetButtons(MessageBoxButtons buttons)
		{
			btnOne.Text = (buttons == MessageBoxButtons.OkCancel) ? "Oк" :
				(buttons == MessageBoxButtons.YesNo) ? "Да" : string.Empty;

			btnTwo.Text = (buttons == MessageBoxButtons.OkCancel) ? "Отмена" :
				(buttons == MessageBoxButtons.YesNo) ? "Нет" :
				(buttons == MessageBoxButtons.Ok) ? "Ок" : string.Empty;

			btnOne.Visible = buttons != MessageBoxButtons.Ok;

			btnOne.DialogResult = (buttons == MessageBoxButtons.OkCancel) ? DialogResult.OK :
				(buttons == MessageBoxButtons.YesNo) ? DialogResult.Yes : DialogResult.None;

			btnTwo.DialogResult = (buttons == MessageBoxButtons.OkCancel) ? DialogResult.Cancel :
				(buttons == MessageBoxButtons.YesNo) ? DialogResult.No :
				(buttons == MessageBoxButtons.Ok) ? DialogResult.OK : DialogResult.None;
		}

		private void llCopy_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("-");
			sb.AppendLine(Text);
			sb.AppendLine("-");
			sb.AppendLine(lblText.Text);
			sb.AppendLine("-");
			if (!string.IsNullOrEmpty(rtbinformation.Text))
			{
				sb.AppendLine(rtbinformation.Text);
				sb.AppendLine("-");
			}
			Globals.CopyToClipboard(sb.ToString());
		}

		private void llMoreInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ShowingAdvanceInfo = !ShowingAdvanceInfo;
			Size = new Size(Size.Width, ShowingAdvanceInfo ? _HeightFullView : _HeightSimpleView);
			llMoreInfo.Text = string.Format("Подробнее {0}{0}", ShowingAdvanceInfo ? '<' : '>');
		}

		private void btnOne_Click(object sender, EventArgs e)
		{

		}

		private void btnTwo_Click(object sender, EventArgs e)
		{

		}
	}
}
