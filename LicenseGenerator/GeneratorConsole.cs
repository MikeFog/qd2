using System;
using System.IO;

namespace FogSoft.LicenseGenerator
{
	public class GeneratorConsole : IProgress
	{
		private enum Action
		{
			None = 0,
			GenerateLicense = 1,
			RestoreTemplate = 2,
			GenerateKey = 3
		}

		private int _stepCount;
		private bool _silentMode;
		private Action _action;


		public GeneratorConsole(bool silentMode, string action)
		{
			if (string.IsNullOrEmpty(action))
				throw new ArgumentException("There is no action (action:license/template/key).");

			_silentMode = silentMode;
			if (action.Equals("license", StringComparison.InvariantCultureIgnoreCase))
				_action = Action.GenerateLicense;
			else if (action.Equals("template", StringComparison.InvariantCultureIgnoreCase))
				_action = Action.RestoreTemplate;
			else if (action.Equals("key", StringComparison.InvariantCultureIgnoreCase))
				_action = Action.GenerateKey;

			if (_action == Action.None)
				throw new ArgumentException(string.Format("Invalid action '{0}'.", action));
		}

		public void Process(string keyFile, string templateFile, string licenseFile)
		{
			if (string.IsNullOrEmpty(keyFile))
					throw new ArgumentException("There is no key file specified (key:\"...\").");
			if (_action != Action.GenerateKey)
			{
				if (string.IsNullOrEmpty(licenseFile))
					throw new ArgumentException("There is no license file specified (license:\"...\").");
				if (string.IsNullOrEmpty(templateFile))
					throw new ArgumentException("There is no template file specified (template:\"...\").");
			}

			switch (_action)
			{
				case Action.GenerateLicense:
					new LicenseWriter(keyFile, this).WriteLicense(templateFile, licenseFile);
					return;
				case Action.RestoreTemplate:
					File.WriteAllText
						(templateFile, new LicenseReader(keyFile).ReadLicenseFromFile(licenseFile),
						 KeyManager.Encoding);
					break;
				case Action.GenerateKey:
					KeyManager.GenerateKey(keyFile);
					break;
				default:
					throw new InvalidOperationException(string.Format("Invalid action '{0}'.", _action));
			}
			WorkCompleted();
		}

		public void StepCompleted()
		{
			if (!_silentMode)
				Console.Write('*');
		}

		public void WorkCompleted()
		{
			if (!_silentMode)
			{
				Console.WriteLine();
				Console.WriteLine("Successfully completed.");
				Console.WriteLine("Press any key to exit.");
				Console.ReadKey();
			}
		}

		public int StepCount
		{
			get { return _stepCount; }
			set { _stepCount = value; }
		}
	}
}