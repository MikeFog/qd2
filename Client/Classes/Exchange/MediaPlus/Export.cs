using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Properties;

namespace Merlin.Classes.Exchange.MediaPlus
{
	internal class Export
	{
		public static void ExportData(int campaignID, string fileName)
		{
			bool resExport = false;

			try
			{
				if (File.Exists(fileName))
				{
					XmlFormat format = new XmlFormat(fileName);
					
					Dictionary<string, object> dictionary = DataAccessor.CreateParametersDictionary();
					dictionary[Campaign.ParamNames.CampaignId] = campaignID;
					DataSet ds = DataAccessor.LoadDataSet("ExportMediaPlusFormat", dictionary);

					float managerDiscount = ParseHelper.GetFloatFromObject(ds.Tables[0].Rows[0]["managerDiscount"], 1);
					float volumeDiscount = ParseHelper.GetFloatFromObject(ds.Tables[0].Rows[0]["volumeDiscount"], 1);
					float packDiscount = ParseHelper.GetFloatFromObject(ds.Tables[0].Rows[0]["packDiscount"], 1);
					int mediaPlusMassmediaID = ParseHelper.GetInt32FromObject(ds.Tables[0].Rows[0]["mediaPlusMassmediaID"], 0);
					if (format.MediaPlusMassmediaID == mediaPlusMassmediaID)
					{
						format.SetDiscountValues(volumeDiscount, managerDiscount, packDiscount);
						format.ClearIssues();
						format.SetIssues(ds.Tables[1]);
						format.Save();

						resExport = true;
					}
					else
					{
						FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Resources.ExportDifferentMassmedias);
						return;
					}
				}
			}
			catch(Exception exp)
			{
				ErrorManager.LogError("Cannot export", exp);
				
			}
			if (!resExport)
				FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Resources.CannotReadExportFile);
			else
				FogSoft.WinForm.Forms.MessageBox.ShowCompleted(Resources.CampaignExportSuccess);
		}
	}
}
