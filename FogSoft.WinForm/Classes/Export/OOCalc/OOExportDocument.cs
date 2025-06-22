using System.Reflection;
using System.Text.RegularExpressions;
using log4net;
using Microsoft.Win32;
using uno;
using uno.util;
using unoidl.com.sun.star.beans;
using unoidl.com.sun.star.container;
using unoidl.com.sun.star.frame;
using unoidl.com.sun.star.lang;
using unoidl.com.sun.star.sheet;
using unoidl.com.sun.star.style;
using unoidl.com.sun.star.uno;

namespace FogSoft.WinForm.Classes.Export.OOCalc
{
	public class OOExportDocument : IExportDocument
	{
		private static readonly ILog Log =
			LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private XSpreadsheetDocument xComponent = null;
		
		public OOExportDocument()
		{
			Log.Info("OOExportDocument()");
			xComponent = CreateOOSpreadsheet();
			((XModel) xComponent).lockControllers();
		}

		public IDocumentSheet GetNewSheet(string name, string fontName, int fontSize)
		{
			Log.Info("Start GetNewSheet");
			if (!string.IsNullOrEmpty(name))
			{
				Regex re = new Regex("\\W");
				name = re.Replace(name, " ");
				re = new Regex("  ");
				name = re.Replace(name, " ");
			}

			XIndexAccess sheets = null;
			try
			{
				sheets = (XIndexAccess) xComponent.getSheets();
			}
			catch(Exception exc)
			{
				Log.Error(exc);
			}

			if (sheets == null)
			{
				xComponent = CreateOOSpreadsheet();
				sheets = (XIndexAccess)xComponent.getSheets();
				Log.Info("XSpreadsheetDocument getSheets");
			}

			if (string.IsNullOrEmpty(name))
				name = string.Format("Sheet {0}", sheets.getCount() + 1);
			
			int i = 0;
			while (i < 100)
			{
				string newName = string.Format("{0}{1}", name, (i == 0 ? string.Empty : i.ToString()));
				i++;
				bool isFind = false;
				foreach (string sheetName in xComponent.getSheets().getElementNames())
				{
					if (string.Compare(sheetName, newName) == 0)
						isFind = true;
				}
				if (isFind) continue;
				Log.InfoFormat("Insert new sheet - '{0}'", newName);
				xComponent.getSheets().insertNewByName(newName, 0);
				break;
			}

			XSpreadsheet sheet = (XSpreadsheet)sheets.getByIndex(0).Value;
			XSpreadsheetView xSpreadsheetView = (XSpreadsheetView)((XModel)xComponent).getCurrentController();
			xSpreadsheetView.setActiveSheet(sheet);

			XPropertySet propertySheet = (XPropertySet) sheet;
			propertySheet.setPropertyValue("CharHeight", new Any((float)fontSize));
			propertySheet.setPropertyValue("CharFontName", new Any(fontName));
			Log.InfoFormat("Continue GetNewSheet");
			return new OODocumentSheet(xComponent, sheet);
		}

		private static XSpreadsheetDocument CreateOOSpreadsheet()
		{
			Log.Info("XSpreadsheetDocument CreateOOSpreadsheet()");
			Log.Info("Start Option 07");
			Option07();
			Log.Info("End Option 07");
			XComponentContext localContext = Bootstrap.bootstrap();
			XMultiServiceFactory multiServiceFactory = (XMultiServiceFactory)localContext.getServiceManager();
			XComponentLoader componentLoader = (XComponentLoader)multiServiceFactory.createInstance("com.sun.star.frame.Desktop");
			Log.Info("Return CreateOOSpreadsheet");
			return (XSpreadsheetDocument)componentLoader.loadComponentFromURL("private:factory/scalc", "_blank", 0, new PropertyValue[0]);
		}

		public static string InstallationPathRegistryEntryName = @"Software\OpenOffice.org\UNO\InstallPath";
		public static string UREInstallationPathRegistryEntryName = @"Software\OpenOffice.org\Layers\URE\1";
		public static string UREInstallationKeyName = @"UREINSTALLLOCATION";
		public static string UnoPathEnvVarName = "UNO_PATH";

		static void Option07()
		{
			string urePath = GetKeyValueInCurrentUserAndLocalMachine(UREInstallationPathRegistryEntryName, UREInstallationKeyName);
			if (!string.IsNullOrEmpty(urePath))
			{
				string path = System.Environment.GetEnvironmentVariable("PATH");
				path = (string.IsNullOrEmpty(path) ? "" : (path + ";")) + urePath + @"bin;";
				System.Environment.SetEnvironmentVariable("PATH", path);

				string installPath = OOLocation();
				if (!string.IsNullOrEmpty(installPath))
				{
					path = System.Environment.GetEnvironmentVariable("UNO_PATH");
					path = (string.IsNullOrEmpty(path) ? "" : (path + ";")) + ";" + installPath + ";";
					System.Environment.SetEnvironmentVariable("UNO_PATH", path);
				} 
			}
		}

		public static string OOLocation()
		{
			return GetKeyValueInCurrentUserAndLocalMachine(InstallationPathRegistryEntryName, "");
		}

		public static string GetKeyValueInCurrentUserAndLocalMachine(string keyString, string valueName)
		{
			string valueString = GetKeyValue(Registry.CurrentUser, keyString, valueName);
			if (string.IsNullOrEmpty(valueString))
				valueString = GetKeyValue(Registry.LocalMachine, keyString, valueName);

			return valueString;
		} 

		public static string GetKeyValue(RegistryKey startKey, string subKeyName, string valueName)
		{
			string valueString = null;
			RegistryKey subKey = null;
			try
			{
				subKey = startKey.OpenSubKey(subKeyName);
				if (subKey != null)
				{
					valueString = (string)subKey.GetValue(valueName);
					subKey.Close();
					subKey = null;
				} 
			} 
			catch (System.Exception)
			{
				if (subKey != null)
					subKey.Close();
			} 
			return valueString;
		} 

		public void StartExport()
		{
			
		}

		public void FinishExport()
		{
			// set landscape

			XStyleFamiliesSupplier style = (XStyleFamiliesSupplier)xComponent;
			XNameContainer styles = (XNameContainer)style.getStyleFamilies().getByName("PageStyles").Value;
			object obj;
			if (styles.hasByName("Базовый"))
				obj = styles.getByName("Базовый").Value;
			else
				obj = styles.getByName("Default").Value;

			XPropertySet property = (XPropertySet)obj;
			property.setPropertyValue("IsLandscape", new Any(true));
			property.setPropertyValue("HeaderIsOn", new Any(false));
			property.setPropertyValue("FooterIsOn", new Any(false));
			property.setPropertyValue("Height", new Any(21000));
			property.setPropertyValue("Width", new Any(29700));

			((XModel) xComponent).unlockControllers();
		}

		public void OnAppQuit()
		{

		}

		public bool Visible()
		{
			return xComponent != null;
		}
	}
}
