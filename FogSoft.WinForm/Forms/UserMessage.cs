using System;
using System.Windows.Forms;

namespace FogSoft.WinForm.Forms
{
	/// <summary>
	/// Тонкая обёртка над стандартным <see cref="System.Windows.Forms.MessageBox"/>.
	/// Даёт единый заголовок окна и общий owner для всех сообщений приложения.
	/// Детали исключений пользователю не показываются — они пишутся в лог (docs/LOGGING.md).
	/// </summary>
	public static class UserMessage
	{
		private static IWin32Window Owner
		{
			get
			{
				// Сообщение может прийти с фонового потока (UnhandledException -> ErrorManager).
				// Обращаться к Handle чужого потока нельзя, поэтому owner в этом случае не задаём —
				// стандартный MessageBox сам возьмёт активное окно.
				Form owner = Form.ActiveForm ?? Globals.MdiParent;
				return (owner != null && !owner.InvokeRequired) ? owner : null;
			}
		}

		private static DialogResult Show(string title, string text, MessageBoxButtons buttons, MessageBoxIcon icon)
		{
			return MessageBox.Show(Owner, text, title ?? Application.ProductName, buttons, icon);
		}

		/// <param name="e">Не показывается пользователю: исключение уже записано в лог вызывающим кодом.</param>
		public static void ShowError(string text, Exception e)
		{
			Show(null, text, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		public static void ShowInformation(string title, string text)
		{
			Show(title, text, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		public static void ShowInformation(string text)
		{
			Show(null, text, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		public static void ShowExclamation(string title, string text)
		{
			Show(title, text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

		public static void ShowExclamation(string text)
		{
			Show(null, text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

		public static DialogResult ShowQuestion(string text)
		{
			return Show(null, text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
		}

		public static void ShowCompleted(string text)
		{
			ShowInformation(text);
		}
	}
}
