using System;
using System.Data;
using System.IO;
using System.Text;
using FogSoft.WinForm;

namespace Merlin.Classes.GridExport.VectorBoxSerializer
{
	internal class VectorBoxExportDocument : ExportDocument
	{
		protected override void Export(Massmedia mm, DateTime date, string fileName, DataTable data,
		                               string broadcastTime)
		{
			DataRow[] rows = data.Select();
			if (rows.Length > 0)
			{
				if (!string.IsNullOrEmpty(mm[Massmedia.ParamNames.ExportName].ToString()))
					fileName = fileName.Substring(0, fileName.Length - ExportHelper.RemoveInvalidFileNameChars(mm.Name).Length)
							   + ExportHelper.RemoveInvalidFileNameChars(mm[Massmedia.ParamNames.ExportName].ToString());

				string fileFirst = string.Format("{0}.PLX", fileName);
				FileStream file = new FileStream(fileFirst, FileMode.Create);
				ExportBlocks(file, data.Select(), mm, date);
				file.Close();
			}
		}

		protected override void PrintTitle(Stream file, Massmedia mm, DateTime date)
		{
			base.PrintTitle(file, mm, date);

			PrintString(file, string.Format(@"<?xml version=""1.0"" encoding=""UTF-16""?>
<PlayList>
	<ExportedBy>{0}</ExportedBy>
	<StudioId>0</StudioId>
	<Id>0</Id>", Globals.ApplicationName));
		}

		protected override void PrintFooter(Stream file, Massmedia mm, DateTime date)
		{
			base.PrintTitle(file, mm, date);

			PrintString(file, string.Format(@"</PlayList>"));
		}

		protected override void PrintRoller(Stream file, DataRow row, Massmedia mm, DateTime date,
		                                    int type, Additional additional)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat(@"	<Item>
		<ItemId>{0}</ItemId>", id);

			if (!isBlockStarted)
			{
				sb.AppendFormat(@"
		<FixedTime>TRUE</FixedTime>
		<StartTime>{0}:00:00</StartTime>", row[ExportParams.tariffTime]);
				isBlockStarted = true;
			}

			sb.AppendFormat(@"
		<Title>
			<TitleId>{0}</TitleId>
			<FilePath>{1}</FilePath>
			<TitleType>Video</TitleType>
		</Title>
	</Item>", row[ExportParams.name], row[ExportParams.path]);

			PrintString(file, sb.ToString());
		}

		private bool isBlockStarted;
		private readonly int id = new Random().Next(int.MinValue, int.MaxValue);

		protected override void PrintBlockStart(Stream file, DateTime? lastBlock, string fullDuration,
		                                        string description, Massmedia mm, Additional additional,
												int type, bool isExtension, DataRow block)
		{
			
		}

		protected override void PrintBlockEnd(Stream file, DateTime? lastBlock, Massmedia mm,
		                                      Additional additional, bool isExtension)
		{
			isBlockStarted = false;
		}

		private static void PrintString(Stream file, string str)
		{
			byte[] bytes = VectorBoxParam.Encoding.GetBytes(string.Format("{0}\r\n", str));
			file.Write(bytes, 0, bytes.Length);
		}
	}
}