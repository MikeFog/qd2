using System;
using System.Windows.Forms;

namespace FogSoft.LicenseGenerator
{
	internal static class Program
	{
		[STAThread]
		private static void Main(string[] args)
		{
			bool winFroms = false;
			
			try
			{
				if (args.Length > 0)
				{
					GuiConsole.CreateConsole();

					foreach (string parameter in args)
					{
						if (parameter.Equals("help", StringComparison.InvariantCultureIgnoreCase)
							|| parameter.Equals("?"))
						{
							DisplayUsage();
							return;
						}
					}

					if (args.Length < 2)
						throw new ArgumentException("Not enough arguments");
					ExecuteAction(args);
				}
				else
				{
					winFroms = true;
					Application.EnableVisualStyles();
					Application.SetCompatibleTextRenderingDefault(false);
					Application.Run(new GeneratorForm());
				}
			}
			catch (Exception ex)
			{
				if (winFroms)
				{
					MessageBox.Show(ex.Message, "License Generator", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					Console.WriteLine(ex.Message);
					Console.ReadKey();
				}
			}
			finally
			{
				if (!winFroms)
					GuiConsole.ReleaseConsole();
			}
		}

		private static void ExecuteAction(string[] args)
		{
			bool silent = false;
			string keyFile = null;
			string templateFile = null;
			string licenseFile = null;
			string action = null;
					
			foreach (string parameter in args)
			{
				if (parameter.StartsWith("action:", StringComparison.InvariantCultureIgnoreCase))
					action = parameter.Substring("action:".Length);
				else if (parameter.StartsWith("key:", StringComparison.InvariantCultureIgnoreCase))
					keyFile = parameter.Substring("key:".Length);
				else if (parameter.StartsWith("template:", StringComparison.InvariantCultureIgnoreCase))
					templateFile = parameter.Substring("template:".Length);
				else if (parameter.StartsWith("license:", StringComparison.InvariantCultureIgnoreCase))
					licenseFile = parameter.Substring("license:".Length);
				else if (parameter.Equals("silent", StringComparison.InvariantCultureIgnoreCase))
					silent = true;
			}

			new GeneratorConsole(silent, action).Process(keyFile, templateFile, licenseFile);
		}

		private static void DisplayUsage()
		{
			Console.WriteLine("Usage:");
			Console.WriteLine("FogSoft.LicenseGenerator.exe action:<action> key:<key file> [template:<template file>] [license:<license file>] [silent]");
			Console.WriteLine("\t action:license|template|key");
			Console.WriteLine("\t\t key:Generates key file.");
			Console.WriteLine("\t\t license:Generates license from template file.");
			Console.WriteLine("\t\t template:Restores template from license file.");
			
			Console.WriteLine();
			Console.WriteLine("Examples");
			Console.WriteLine("1. Generate key file");
			Console.WriteLine("FogSoft.LicenseGenerator.exe action:key key:Default.lkey");

			Console.WriteLine("2. Generate license file");
			Console.WriteLine("FogSoft.LicenseGenerator.exe action:license key:Default.lkey template:TemplateSample.ltmpl license:TemplateSample.lic");

			Console.WriteLine("3. Restore template file from license");
			Console.WriteLine("FogSoft.LicenseGenerator.exe action:template key:Default.lkey license:TemplateSample.lic template:TemplateSample.ltmpl");

			Console.WriteLine();
			Console.WriteLine("Press any key to continue.");
			Console.ReadKey();
		}
	}
}