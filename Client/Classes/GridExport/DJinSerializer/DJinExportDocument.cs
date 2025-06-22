using System;
using System.Data;
using System.IO;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes.GridExport.DJinSerializer
{
	internal class DJinExportDocument : ExportDocument
	{
		protected override void Export(Massmedia mm, DateTime date, string fileName, DataTable data, string broadcastTime)
		{
			if (data.Select("isToday = 1 and rolActionTypeID is not null").Length > 0)
			{
				DataRow[] rows = data.Select("isToday = 1");
				if (rows.Length > 0)
				{
					string fileFirst = string.Format("{0}_{1}_{2}-2359.txt", fileName, date.ToString("dd_MM_yyyy"), broadcastTime);
					FileStream file = new FileStream(fileFirst, FileMode.Create);
					ExportBlocks(file, rows, mm, date);
					file.Close();
				}
			}

			if (data.Select("isToday = 0 and rolActionTypeID is not null").Length > 0)
			{
				DataRow[] rows = data.Select("isToday = 0");
				if (rows.Length > 0)
				{
					string fileSecond =
						string.Format("{0}_{1}_0000-{2}.txt", fileName, date.AddDays(1).ToString("dd_MM_yyyy"), broadcastTime);
					FileStream file = new FileStream(fileSecond, FileMode.Create);
					ExportBlocks(file, rows, mm, date);
					file.Close();
				}
			}
		}

		private readonly Random random = new Random(unchecked((int) DateTime.Now.Ticks));
		private readonly string strAddedPath = string.Format("{{0}}{0}{{1}}.mp3", Path.DirectorySeparatorChar);
				
		protected override void PrintRoller(Stream file, DataRow row, Massmedia mm, DateTime date, int type, Additional additional)
		{
			if (string.IsNullOrEmpty(row[ExportParams.name].ToString()) && !additional.IsAlive)
				return;

			string typePreffix;
			string volume;

			if(	type == 3)
			{
				typePreffix = DJinParam.strRollerProgram;
                volume = mm.VolumePStr;
            }
			else if(type == 2)
			{
				typePreffix = DJinParam.strRollerNews;
                volume = mm.VolumeNStr;
            }
			else
			{
				typePreffix = DJinParam.strRoller;
				volume = mm.VolumeCStr;
            }

			PrintLine(file, string.Empty, typePreffix, string.Empty, string.Empty, GetFullPath(row, mm, date, type),
					  row[ExportParams.rollerDurationString].ToString(), volume);
		}

		private static string GetFullPath(DataRow row, Massmedia mm, DateTime date, int type)
		{
			string strFileName = GetFileName(row, date, type);
			string strPath = string.IsNullOrEmpty(row[ExportParams.currentPath].ToString())
			                 	? mm.RollersPath
			                 	: row[ExportParams.currentPath].ToString();
			return Path.GetFullPath(string.Format("{0}{1}{2}", strPath, Path.DirectorySeparatorChar, ExportHelper.RemoveInvalidFileNameChars(strFileName)));
		}

		private static string GetFileName(DataRow row, DateTime date, int type)
		{
			string fileName;
			if (string.IsNullOrEmpty(row[ExportParams.path].ToString()))
				fileName = string.Format("{0}.mp3", row[ExportParams.name]);
			else
				fileName = Path.GetFileName(row[ExportParams.path].ToString());

			if (type != 1)
			{
				fileName = string.Format("{0}{1}{2}{3}"
				                         , Path.GetFileNameWithoutExtension(fileName)
				                         , date.ToString("yyMMdd")
				                         , row[ExportParams.suffix]
				                         ,
				                         string.IsNullOrEmpty(Path.GetExtension(fileName))
				                         	? DJinParam.strDefaultExt
				                         	: Path.GetExtension(fileName));
			}
			return fileName;
		}

		protected override void PrintBlockStart(Stream file, DateTime? lastBlock, string fullDuration, string description, Massmedia mm,
									 Additional additional, int type, bool isExtension, DataRow block)
		{
			if (!isExtension)
			{
				string blockName =
					string.Format(DJinParam.strBlockComment, type > 1 ? description : DJinParam.strAdvert,
					              ((DateTime) lastBlock).ToString("t"));
				string blockStart = string.Format("B{0}{1}{2}{3}{4}T", block["blockType"], block["notEarly"], block["notLater"], block["openBlock"], block["openPhonogram"]);
				PrintLine(file, blockStart, string.Empty, ((DateTime) lastBlock).ToString("T"),
				          blockName, string.Empty, fullDuration);
			}

			if ((!additional.NeedInJingle || !additional.NeedOutJingle) || !StringUtil.IsDBNullOrNull(block[ExportParams.description]))
			{
				if (additional.NeedInJingle && !additional.IsAlive)
					PrintLine(file, string.Empty, DJinParam.strJingle, string.Empty, string.Empty,
					          Path.GetFullPath(string.Format(strAddedPath, mm.EnterPath, random.Next(mm.EnterMin, mm.EnterMax))),
					          string.Empty, mm.VolumeJStr);
			}
		}

		protected override void PrintBlockEnd(Stream file, DateTime? lastBlock, Massmedia mm, Additional additional, bool isExtension)
		{
			if (lastBlock != null)
			{
				if (additional.NeedOutJingle && !additional.IsAlive)
					PrintLine(file, string.Empty, DJinParam.strJingle, string.Empty, string.Empty,
							  Path.GetFullPath(string.Format(strAddedPath, mm.ExitPath, random.Next(mm.ExitMin, mm.ExitMax))),
					          string.Empty, mm.VolumeJStr);
				if (additional.NeedExt && !additional.IsAlive)
					PrintLine(file, string.Empty, DJinParam.strEtc, string.Empty, string.Empty,
							  Path.GetFullPath(string.Format(strAddedPath, mm.EtcPath, random.Next(mm.EtcMin, mm.EtcMax))),
					          string.Empty, mm.VolumeMStr);
				if (!isExtension)
				{
					PrintLine(file, DJinParam.strBlockEnd, string.Empty, ((DateTime) lastBlock).ToString("T"), string.Empty,
					          string.Empty,
					          string.Empty);
				}
			}
		}

		private static void PrintLine(Stream file, string s0, string s1, string s2, string s3, string s4, string s5, string s6 = "")
		{
			byte[] bytes = DJinParam.Encoding.GetBytes(string.Format(DJinParam.strLine, s0, s1, s2, s3, s4, s5, s6));
			file.Write(bytes, 0, bytes.Length);
		}
	}
}