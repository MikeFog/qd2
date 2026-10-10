using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using FogSoft.WinForm.Classes;
using log4net;
using Merlin.Classes.GridExport.DJinSerializer;

namespace Merlin.Classes.GridExport
{
	/// <summary>Готовый файл выгрузки: имя (с путём, если его задали) и содержимое.</summary>
	public sealed class ExportFile
	{
		public string Name;
		public byte[] Content;
	}

	/// <summary>
	/// Выгрузка сетки вещания для эфирной программы (DJin). Файлы собираются в памяти
	/// (<see cref="ExportToMemory"/>) — так их отдаёт веб; десктоп пишет те же байты на диск
	/// (<see cref="Export(DataTable, Massmedia, DateTime, string)"/>, диалог выбора папки —
	/// в ExportDocument.WinForms.cs).
	/// </summary>
    abstract partial class ExportDocument
	{
		private static readonly ILog Log =
			LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <param name="fileName">Путь и начало имени файлов (без даты и расширения).</param>
		public void Export(DataTable data, Massmedia mm, DateTime date, string fileName)
		{
			if (mm == null)
				return;

			foreach (ExportFile file in ExportToMemory(data, mm, date, fileName))
				File.WriteAllBytes(file.Name, file.Content);
		}

		/// <summary>
		/// Файлы выгрузки в памяти. <paramref name="fileName"/> — начало имени (у десктопа с
		/// путём папки, у веба — только имя станции); дата и время эфира дописываются.
		/// </summary>
		public IList<ExportFile> ExportToMemory(DataTable data, Massmedia mm, DateTime date, string fileName)
		{
			MassmediaPricelist pl = (MassmediaPricelist)mm.GetPriceList(date);
			string broadcastTime = pl == null ? string.Empty : pl.BroadcastStart.ToString("HHmm");
			return Build(mm, date, fileName, data, broadcastTime);
		}

		protected abstract IList<ExportFile> Build(Massmedia mm, DateTime date, string fileName, DataTable data, string broadcastTime);

		protected void ExportBlocks(Stream file, IEnumerable<DataRow> rows, Massmedia mm, DateTime date)
		{
			PrintTitle(file, mm, date);

			DateTime? lastBlock = null;
			DateTime? lastBlockExtension = null;
			string lastTarrifID = string.Empty;
			string lastTarrifUnionID = string.Empty;
			bool isExtension = false;
			int lastType = 0;
			bool isPrintStart = false;
			// В окне есть ролики, кроме промо без спонсора (9); влёт окна был бы напечатан
			bool windowHasAdvert = false;
			bool windowInAllowed = false;
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
						if (!windowHasAdvert)
							EndWindowWithoutAdvert(file, mm, ref additional, windowInAllowed, beforeIsExtension || isExtension);
						
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
					windowHasAdvert = WindowHasAdvert(rowList, i);
					windowInAllowed = additional.NeedInJingle && string.IsNullOrEmpty(windowPrevId);
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

						// Окно без рекламы (только промо без спонсора) влёт не получает - вместо него метка в конце окна
                        Additional addStart = new Additional
						                      	{
						                      		NeedExt = bool.Parse(block.Key[ExportParams.needExt].ToString()),
						                      		NeedInJingle = bool.Parse(block.Key[ExportParams.needInJingle].ToString()) && string.IsNullOrEmpty(windowPrevId)
						                      			&& (block.Key != row || windowHasAdvert),
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
					lastType = type;
				}
			}
			if (lastType > 0)
			{
				if (!windowHasAdvert)
					EndWindowWithoutAdvert(file, mm, ref additional, windowInAllowed, isExtension);

				PrintBlockEnd(file, lastBlockExtension, mm, additional, false);
			}

			PrintFooter(file, mm, date);
		}

		/// <summary>
		/// Конец окна без рекламы - пустого или только с промо без спонсора (9) - в уже начатом блоке:
		/// своих влёта и аута у него нет. Если окну положен влёт (needInJingle, не продолжение склейки по
		/// windowPrevId), на его месте - метка места влёта; BlockManager оставляет её только в склеенном
		/// блоке без настоящего влёта и с рекламой.
		/// </summary>
		/// <param name="inChain">Окно склеено с соседним; добивку (Ext) одиночного окна не трогаем.</param>
		private void EndWindowWithoutAdvert(Stream file, Massmedia mm, ref Additional additional, bool inAllowed, bool inChain)
		{
			if (inAllowed)
				PrintJingleInPlace(file, mm, additional);

			if (additional.NeedInJingle && additional.NeedOutJingle)
			{
				additional.NeedOutJingle = false;
				if (inChain)
					additional.NeedExt = false;
			}
		}

		/// <summary>Есть ли в окне (строки с тем же временем, начиная с start) ролик, кроме промо без спонсора (9).</summary>
		private static bool WindowHasAdvert(List<DataRow> rowList, int start)
		{
			DateTime time = GetTime(rowList[start]);
			for (int k = start; k < rowList.Count && GetTime(rowList[k]) == time; k++)
			{
				string strType = rowList[k][ExportParams.rolActionTypeID].ToString();
				int type = string.IsNullOrEmpty(strType) ? 0 : int.Parse(strType);
				if (type > 0 && type != 9)
					return true;
			}
			return false;
		}

		private int GetNextWindowsDuration(List<DataRow> rowList, int currentIndex)
		{ 
			int j = 1;
			int duration = 0;
			int tariffId = 0;
			
			// there're 2 possibilities how to union windows - through tarriffы and through windows
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

		/// <summary>Метка места влёта (см. DJinParam.strJingleInPlace); другим форматам не нужна.</summary>
		protected virtual void PrintJingleInPlace(Stream file, Massmedia mm, Additional additional)
		{
		}
		
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