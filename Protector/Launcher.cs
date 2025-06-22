using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using log4net.Config;
using Protector.Forms;

namespace Protector
{
	internal static class Launcher
	{
		[STAThread]
		static void Main()
		{
			InitLogger();
			Application.EnableVisualStyles();
			InitCulture();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            Application.Run(new FrmMain());
		}

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            ErrorManager.PublishError((Exception)args.ExceptionObject);
        }

        private static void InitCulture()
		{
			System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("ru-RU");
			System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("ru");
		}

		private static void InitLogger()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			XmlConfigurator.ConfigureAndWatch(new FileInfo(assembly.Location + ".config "));
		}
	}
}
