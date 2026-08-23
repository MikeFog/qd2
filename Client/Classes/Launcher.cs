using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
using log4net.Config;
using Merlin.Forms;

namespace Merlin.Classes
{
	internal static class Launcher
	{
		[STAThread]
		private static void Main()
		{
			InitLogger();
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			InitCulture();
			UserInteraction.SetConfirmHandler(question =>
			{
				bool result = UserMessage.ShowQuestion(question) == DialogResult.Yes;
				// DoEvents здесь был и раньше — даёт диалогу закрыться до начала
				// длительной операции. Оставлен в UI-слое сознательно.
				Application.DoEvents();
				return result;
			});
			UserInteraction.SetNotifyHandler((messageKey, parameters) =>
				Globals.ShowCompleted(messageKey, parameters));
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

            Application.Run(new MdiForm());
		}

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
			ErrorManager.PublishError((Exception)args.ExceptionObject);
        }

        private static void InitCulture()
		{
			System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ru-RU");
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("ru");
		}

		private static void InitLogger()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			XmlConfigurator.ConfigureAndWatch(new FileInfo(assembly.Location + ".config "));
		}
	}
}