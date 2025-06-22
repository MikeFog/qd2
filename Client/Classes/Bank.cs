using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	public class Bank
	{
		public static void UpdateBankList(IWin32Window owner)
		{
			Application.DoEvents();
			Cursor.Current = Cursors.WaitCursor;
			WebClient client = new WebClient();

			string exeFileName = String.Format(@"{0}\{1}\bnk.exe", 
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
				"qd2");

			FileInfo exeFileInfo = new FileInfo(exeFileName);

			if (!Directory.Exists(exeFileInfo.DirectoryName))
				Directory.CreateDirectory(exeFileInfo.DirectoryName);

			try
			{
				client.DownloadFile("http://cbrates.rbc.ru/bnk/bnk.exe", exeFileName);
			}
			catch(Exception ex)
			{
				throw new Exception(MessageAccessor.GetMessage("BanksListUpdateFailed"), ex);
			}

			ProcessStartInfo psi = new ProcessStartInfo(exeFileName);
			psi.CreateNoWindow = true;
			psi.WorkingDirectory = exeFileInfo.DirectoryName;
			psi.UseShellExecute = true;

			Process process = Process.Start(psi);
			process.WaitForExit();

			string dataFileName = String.Format(@"{0}\{1}", 
				exeFileInfo.DirectoryName,
				"bnkseek.txt");

			string data2FileName = String.Format(@"{0}\{1}",
				exeFileInfo.DirectoryName,
				"reg.txt");

			FileStream stream = File.Open(dataFileName, FileMode.Open);

			StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(1251));

			string filterCity = ConfigurationUtil.GetSettings("BankImportFilterCity", string.Empty);

			try
			{
				string bankCity;
				string bankName;
				string bik;
				string corAccount;

				while(!reader.EndOfStream)
				{
					string dataString = reader.ReadLine();
					List<string> data = new List<string>(dataString.Split('	'));

					for (int i = 0; i < data.Count; i++ )
						if (data[i].Trim().Length == 0)
							data.RemoveAt(i);

					bankCity = data[1];
					bankName = data[3];
					bik = (data.Count > 4) ? data[4] : "";
					corAccount = (data.Count > 5) ? data[5] : "";

					if (string.IsNullOrEmpty(filterCity) || bankCity.ToUpper() == filterCity.ToUpper())
					{
						Dictionary<string, object> parameters = new Dictionary<string, object>
						                                        	{
						                                        		{"bik", bik.Trim()},
						                                        		{"name", bankName.Trim()},
						                                        		{"corAccount", corAccount.Trim()}
						                                        	};

						DataAccessor.ExecuteNonQuery("bankListUpdate", parameters);
					}
				}
			}
			finally
			{
				stream.Close();

				if (File.Exists(dataFileName))
					File.Delete(dataFileName);

				if (File.Exists(data2FileName))
					File.Delete(data2FileName);

				Cursor.Current = Cursors.Default;
			}

			Globals.ShowCompleted("BanksListUpdatedSuccesfully");
		}

		public static PresentationObject Find(string bik)
		{
			Entity bankEntity = EntityManager.GetEntity((int)Entities.Bank);
            Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(bankEntity);
            procParameters[Organization.ParamNames.BankBIK] = bik;
            DataSet ds = (DataSet)DataAccessor.DoAction(procParameters);

			if (ds.Tables[Constants.TableNames.Data].Rows.Count == 0)
				return null;
			return bankEntity.CreateObject(ds.Tables[Constants.TableNames.Data].Rows[0]);
        }
	}
}
