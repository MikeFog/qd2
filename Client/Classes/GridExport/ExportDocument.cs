using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using log4net;
using Merlin.Classes.GridExport.DJinSerializer;

namespace Merlin.Classes.GridExport
{
    abstract class ExportDocument
	{
		private static readonly ILog Log =
			LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void Export(DataTable data, Massmedia mm, DateTime date)
		{
			try
			{
				if (mm == null)
					return;

				MassmediaPricelist pl = (MassmediaPricelist) mm.GetPriceList(date);
				string broadcastTime = pl.BroadcastStart.ToString("HHmm");

				FolderBrowserDialog dlg = new FolderBrowserDialog();
				Application.DoEvents();
				if (dlg.ShowDialog(Globals.MdiParent) == DialogResult.OK)
				{
					string mmName = mm.Name;
					mmName = ExportHelper.RemoveInvalidFileNameChars(mmName);
					string fileName = string.Format("{0}{1}{2}", dlg.SelectedPath, Path.DirectorySeparatorChar, mmName);

					Export(mm, date, fileName, data, broadcastTime);
					if (ExportHelper.OpenFolderOnFinish)
						Process.Start(dlg.SelectedPath);
				}
			}
			catch(Exception e)
			{
				Log.Error("CouldNotToExport", e);
				Globals.ShowExclamation("CouldNotToExport");
			}
		}

		public void Export(DataTable data, Massmedia mm, DateTime date, string fileName)
		{
			if (mm == null)
				return;

			MassmediaPricelist pl = (MassmediaPricelist)mm.GetPriceList(date);
			string broadcastTime = pl == null ? string.Empty : pl.BroadcastStart.ToString("HHmm");
			Export(mm, date, fileName, data, broadcastTime);
		}

		protected abstract void Export(Massmedia mm, DateTime date, string fileName, DataTable data, string broadcastTime);

		protected void ExportBlocks(Stream file, IEnumerable<DataRow> rows, Massmedia mm, DateTime date)
		{
			PrintTitle(file, mm, date);

			DateTime? lastBlock = null;
			DateTime? lastBlockExtension = null;
			string lastTarrifID = string.Empty;
			string lastTarrifUnionID = string.Empty;
			bool isExtension = false;
			int lastType = 0;
			int lastTypeInBlock = 0;
			bool isPrintStart = false;
			Dictionary<DataRow, bool> lastBlocks = new Dictionary<DataRow, bool>();
			Additional additional = new Additional();

            var rowList = rows.ToList();
            for (int i = 0; i < rowList.Count; i++)
			{
				DataRow row = rowList[i];
				string strRollerType = row[ExportParams.rolActionTypeID].ToString();
				
				int type = string.IsNullOrEmpty(strRollerType) ? 0 : int.Parse(strRollerType);

				DateTime time = GetTime(row);
				string tariffID = row[ExportParams.tariffID].ToString();
				string tariffUnionID = row[ExportParams.tariffUnionID].ToString();
                string windowPrevId = row[ExportParams.windowPrevId].ToString();

                if (!lastBlock.HasValue || DateTime.Compare(time, lastBlock.Value) != 0)
				{
					bool beforeIsExtension = isExtension;

					if (string.IsNullOrEmpty(tariffID) || string.Compare(lastTarrifID, tariffID) != 0)
						isExtension = (!string.IsNullOrEmpty(tariffID) && !string.IsNullOrEmpty(lastTarrifUnionID) && string.Compare(lastTarrifUnionID, tariffID) == 0) || 
							!string.IsNullOrEmpty(windowPrevId);

					if (lastType > 0)
					{
						if (beforeIsExtension && lastTypeInBlock <= 0 && additional.NeedInJingle && additional.NeedOutJingle)
						{
							additional.NeedOutJingle = false;
							additional.NeedExt = false;
						}
						
						if (string.IsNullOrEmpty(windowPrevId))
							PrintBlockEnd(file, lastBlockExtension, mm, additional, isExtension);
					}

					if (!isExtension)
					{
						lastBlockExtension = time;
						lastType = 0;
						lastBlocks.Clear();
					}

					lastBlock = time;
										
					if (((string.IsNullOrEmpty(tariffID) || string.IsNullOrEmpty(lastTarrifUnionID)) || 
						(string.Compare(lastTarrifID, tariffID) != 0 && string.Compare(lastTarrifUnionID, tariffID) != 0))
                        && string.IsNullOrEmpty(windowPrevId))
                        lastBlockExtension = time;

					lastTarrifID = tariffID;
					lastTarrifUnionID = tariffUnionID;
					
					isPrintStart = false;

					additional.NeedExt = bool.Parse(row[ExportParams.needExt].ToString());
					additional.NeedInJingle = bool.Parse(row[ExportParams.needInJingle].ToString());
					additional.NeedOutJingle = bool.Parse(row[ExportParams.needOutJingle].ToString());
					additional.IsAlive = bool.Parse(row[ExportParams.isAlive].ToString());
					lastTypeInBlock = 0;
				}

				if (type > 0 && !isPrintStart)
				{
					if (!isExtension)
						lastType = 0;
					lastBlocks.Add(row, isExtension);
					isPrintStart = true;
					foreach (KeyValuePair<DataRow, bool> block in lastBlocks)
					{
						int duration = 0;
						if (string.IsNullOrEmpty(block.Key[ExportParams.tariffUnionID].ToString()) &&
							string.IsNullOrEmpty(block.Key[ExportParams.windowNextId].ToString()))
							duration = int.Parse( block.Key[ExportParams.fullDuration].ToString());
						else
							duration = int.Parse(block.Key[ExportParams.fullDuration].ToString()) + GetNextWindowsDuration(rowList, i);

                        Additional addStart = new Additional
						                      	{
						                      		NeedExt = bool.Parse(block.Key[ExportParams.needExt].ToString()),
						                      		NeedInJingle = bool.Parse(block.Key[ExportParams.needInJingle].ToString()) && string.IsNullOrEmpty(windowPrevId),
						                      		NeedOutJingle = bool.Parse(block.Key[ExportParams.needOutJingle].ToString()),
						                      		IsAlive = bool.Parse(block.Key[ExportParams.isAlive].ToString())
						                      	};

						PrintBlockStart(file, GetTime(block.Key), DateTimeUtils.Time2StringHHMMSS(duration), row[ExportParams.comment].ToString(),
										mm, addStart, type, block.Value, block.Key);
					}
					lastBlocks.Clear();
				}
				else if (!isPrintStart)
				{
					lastBlocks.Add(row, isExtension);
				}

				if (type > 0)
				{
					PrintRoller(file, row, mm, date, type, additional);
					lastTypeInBlock = lastType = type;
				}
			}
			if (lastType > 0)
			{
				if (lastTypeInBlock <= 0 && additional.NeedInJingle && additional.NeedOutJingle)
				{
					additional.NeedOutJingle = false;
					additional.NeedExt = false;
				}

				PrintBlockEnd(file, lastBlockExtension, mm, additional, false);
			}

			PrintFooter(file, mm, date);
		}

		private int GetNextWindowsDuration(List<DataRow> rowList, int currentIndex)
		{ 
			int j = 1;
			int duration = 0;
			int tariffId = 0;
			
			// there're 2 possibilities how to union windows - through tarriffû and through windows
			if(rowList[currentIndex][ExportParams.tariffUnionID] != DBNull.Value)
			{
				int tariffUnionId = int.Parse(rowList[currentIndex][ExportParams.tariffUnionID].ToString());
                while (currentIndex + j < rowList.Count)
                {
                    DataRow row = rowList[currentIndex + j++];
                    if(row[ExportParams.tariffID] == DBNull.Value) continue;

                    int currentTariffId = int.Parse(row[ExportParams.tariffID].ToString());
                    if (currentTariffId == tariffUnionId) duration += int.Parse(row[ExportParams.fullDuration].ToString());

                    if (row[ExportParams.tariffUnionID] == DBNull.Value) break;

                    int currentTariffUnionId = int.Parse(row[ExportParams.tariffUnionID].ToString());
                    if (tariffUnionId != currentTariffUnionId) tariffUnionId = currentTariffUnionId;
                }
            }
			else
			{
				while (currentIndex + j < rowList.Count)
				{
					DataRow row = rowList[currentIndex + j++];
					int currentTariffId = int.Parse(row[ExportParams.tariffID].ToString());
					if (row[ExportParams.windowNextId] != DBNull.Value && row[ExportParams.windowPrevId] == DBNull.Value) continue;
					if (row[ExportParams.windowPrevId] != DBNull.Value)
					{
						if (currentTariffId != tariffId)
						{
							tariffId = currentTariffId;
							duration += int.Parse(row[ExportParams.fullDuration].ToString());
						}
						continue;
					}
					break;
				}
            }

			return duration;
		}

        protected virtual void PrintTitle(Stream file, Massmedia mm, DateTime date)	
		{			
		}

        protected virtual void PrintFooter(Stream file, Massmedia mm, DateTime date)
		{
		}

		protected abstract void PrintRoller(Stream file, DataRow row, Massmedia mm, DateTime date, int type,
		                                    Additional additional);

		protected abstract void PrintBlockStart(Stream file, DateTime? lastBlock, string fullDuration, string description, Massmedia mm,
									 Additional additional, int type, bool isExtension, DataRow block);

		protected abstract void PrintBlockEnd(Stream file, DateTime? lastBlock, Massmedia mm, Additional additional, bool isExtension);
		

		protected static DateTime GetTime(DataRow row)
		{
			string time = row[ExportParams.tariffTime].ToString();
			string[] times = time.Split(':');

			int hour = int.Parse(times[0]);
			int minute = int.Parse(times[1]);
			return DateTime.Today.AddHours(hour < 24 ? hour : hour - 24).AddMinutes(minute);
		}

		#region Nested type: Additional

		protected struct Additional
		{
			public bool IsAlive { get; set; }

			public bool NeedExt { get; set; }

			public bool NeedInJingle { get; set; }

			public bool NeedOutJingle { get; set; }
		}

		#endregion
        
		public static AudioExportType AudioExportType
		{
			get { return ConfigurationUtil.GetEnumSettings("AudioExportType", AudioExportType.DJin); }
		}

		public static ExportDocument GetDocument()
		{
			switch (AudioExportType)
			{
				case AudioExportType.DJin:
					return new DJinExportDocument();
			}

			return null;
		}
	}
}