using System;
using System.Data;
using System.IO;

namespace Merlin.Classes.GridExport.VideoDJSerializer
{
	internal class VideoDJExportDocument : ExportDocument
	{
		protected override void Export(Massmedia mm, DateTime date, string fileName, DataTable data, string broadcastTime)
		{
			DataRow[] rows = data.Select();
			if (rows.Length > 0)
			{
				if (!string.IsNullOrEmpty(mm[Massmedia.ParamNames.ExportName].ToString()))
					fileName = fileName.Substring(0, fileName.Length - ExportHelper.RemoveInvalidFileNameChars(mm.Name).Length)
						+ ExportHelper.RemoveInvalidFileNameChars(mm[Massmedia.ParamNames.ExportName].ToString());

				string fileFirst = string.Format("{0}.txt", fileName);
				FileStream file = new FileStream(fileFirst, FileMode.Create);
				ExportBlocks(file, data.Select(), mm, date);
				file.Close();
			}
		}

		protected override void PrintRoller(Stream file, DataRow row, Massmedia mm, DateTime date, int type, Additional additional)
		{
			string name = row[ExportParams.name].ToString();
			if (row[ExportParams.position] != DBNull.Value && !string.IsNullOrEmpty(row[ExportParams.position].ToString()))
			{
				if (string.Compare(row[ExportParams.position].ToString(), "!!") == 0)
					name = string.Format("{0}", row[ExportParams.position]) + name;
				else
					name += string.Format(" {0}", row[ExportParams.position]);
			}
			PrintString(file, string.Format(VideoDJParam.RollerFormat, name, row[ExportParams.rollerDuration]));
		}

		protected override void PrintTitle(Stream file, Massmedia mm, DateTime date)
		{
			base.PrintTitle(file, mm, date);

			PrintString(file, string.Format("//{0}//", date.ToString("dd.MM.yy")));
		}

		protected override void PrintBlockStart(Stream file, DateTime? lastBlock, string fullDuration, string description, Massmedia mm, Additional additional, int type, bool isExtension, DataRow block)
		{
			if (lastBlock.HasValue)
			{
				PrintString(file, string.Empty);
				PrintString(file, string.Format("[{0}]", lastBlock.Value.ToString("HH:mm")));
				PrintString(file, VideoDJParam.JingleStart);
			}
		}

		protected override void PrintBlockEnd(Stream file, DateTime? lastBlock, Massmedia mm, Additional additional, bool isExtension)
		{
			if (lastBlock.HasValue)
			{
				PrintString(file, VideoDJParam.JingleEnd);
			}
		}

		private static void PrintString(Stream file, string str)
		{
			byte[] bytes = VideoDJParam.Encoding.GetBytes(string.Format("{0}\r\n",str));
			file.Write(bytes, 0, bytes.Length);
		}
	}
}