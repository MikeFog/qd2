using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;

namespace FogSoft.WinForm.Classes
{
	public enum ReportExportFormat
	{
		CrystalReport = ExportFormatType.CrystalReport,
		RichText = ExportFormatType.RichText,
		WordForWindows = ExportFormatType.WordForWindows,
		Excel = ExportFormatType.Excel,
		PortableDocFormat = ExportFormatType.PortableDocFormat,
		HTML32 = ExportFormatType.HTML32,
		HTML40 = ExportFormatType.HTML40,
		Unknown = ExportFormatType.NoFormat
	}

	public sealed class ReportExporter
	{
		private struct ReportFileInfo
		{
			public string Path;
			public string FileName;
		}

		private const string HTMLFILENAME = "index.htm";

		private string exportPath;

		internal ReportExporter()
		{
			SetExportPath(Environment.GetEnvironmentVariable("TEMP") + "\\" + Application.ProductName + "\\");
		}

		private void ClearTemporaryDiriectory()
		{
			foreach(string path in Directory.GetFileSystemEntries(exportPath))
			{
				try
				{
					if(File.Exists(path))
						File.Delete(path);
					else if(Directory.Exists(path))
						Directory.Delete(path, true);
				}
				catch
				{
					continue;
				}
			}
		}

		private void SetExportPath(string path)
		{
			if(!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			exportPath = path;
			ClearTemporaryDiriectory();
		}

		public string Export(ReportClass report, ReportExportFormat exportFormatType, bool bShellExecute)
		{
			ExportOptions exportOptions = report.ExportOptions;
			exportOptions.ExportFormatType = (ExportFormatType) exportFormatType;
			exportOptions.ExportDestinationType = ExportDestinationType.DiskFile;
			exportOptions.FormatOptions = null;
			DiskFileDestinationOptions diskFileDestinationOptions = new DiskFileDestinationOptions();
			string fileName = string.Empty;

			try
			{
				Application.DoEvents();
				ReportFileInfo exportFileInfo = PrepareExportOptions(report, diskFileDestinationOptions);

				DirectoryInfo di = new DirectoryInfo(exportFileInfo.Path);
				if(!di.Exists)
				{
					di.Create();
					di.Refresh();
				}

				report.Export();

				FileInfo file = FindExportResultFile(di, exportFileInfo.FileName);

				if(file != null && bShellExecute)
				{
					ProcessStartInfo pi = new ProcessStartInfo(file.FullName);
					pi.UseShellExecute = true;
					Process.Start(pi);
				}
				return fileName;
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}

		private FileInfo FindExportResultFile(DirectoryInfo dir, string mask)
		{
			if(dir.Exists)
			{
				FileInfo[] files = dir.GetFiles(mask);
				if(files.Length > 0) return files[0];
				foreach(DirectoryInfo di in dir.GetDirectories())
				{
					return FindExportResultFile(di, mask);
				}
			}
			return null;
		}

		private ReportFileInfo PrepareExportOptions(
			ReportClass report, DiskFileDestinationOptions diskFileDestinationOptions)
		{
			ReportFileInfo reportFile = new ReportFileInfo();
			reportFile.FileName = Guid.NewGuid().ToString();
			reportFile.Path = exportPath + report.ResourceName;
			switch(report.ExportOptions.ExportFormatType)
			{
				case ExportFormatType.CrystalReport:
				case ExportFormatType.RichText:
				case ExportFormatType.WordForWindows:
				case ExportFormatType.Excel:
				case ExportFormatType.PortableDocFormat:
					reportFile.FileName += "." + ExportFileExtension(report.ExportOptions.ExportFormatType);
					diskFileDestinationOptions.DiskFileName = reportFile.Path + "\\" + reportFile.FileName;
					report.ExportOptions.DestinationOptions = diskFileDestinationOptions;
					break;
				case ExportFormatType.HTML32:
				case ExportFormatType.HTML40:
					reportFile.Path = reportFile.Path + "\\" + reportFile.FileName;
					if(report.ExportOptions.ExportFormatType == ExportFormatType.HTML40)
						ConfigureExportToHtml40(report, reportFile);
					else
						ConfigureExportToHtml32(report, reportFile);
					reportFile.FileName = HTMLFILENAME;
					break;
				case ExportFormatType.NoFormat:
				default:
					throw new Exception(
						string.Format("Export format '{0}' is not supported.", report.ExportOptions.ExportFormatType));
			}
			return reportFile;
		}

		private string ExportFileExtension(ExportFormatType exportFormatType)
		{
			string extension = string.Empty;
			switch(exportFormatType)
			{
				case ExportFormatType.CrystalReport:
					extension = "rpt";
					break;
				case ExportFormatType.RichText:
					extension = "rtf";
					break;
				case ExportFormatType.WordForWindows:
					extension = "doc";
					break;
				case ExportFormatType.Excel:
					extension = "xls";
					break;
				case ExportFormatType.PortableDocFormat:
					extension = "pdf";
					break;
				case ExportFormatType.HTML32:
					extension = "htm";
					break;
				case ExportFormatType.HTML40:
					extension = "htm";
					break;
				case ExportFormatType.NoFormat:
				default:
					return "unknown";
			}
			return extension;
		}

		private void ConfigureExportToHtml32(ReportClass report, ReportFileInfo reportFile)
		{
			HTMLFormatOptions html32FormatOptions = new HTMLFormatOptions();
			html32FormatOptions.HTMLBaseFolderName = reportFile.Path;
			html32FormatOptions.HTMLFileName = HTMLFILENAME;
			html32FormatOptions.HTMLEnableSeparatedPages = false;
			html32FormatOptions.HTMLHasPageNavigator = false;
			report.ExportOptions.FormatOptions = html32FormatOptions;
		}

		private void ConfigureExportToHtml40(ReportClass report, ReportFileInfo reportFile)
		{
			HTMLFormatOptions html40FormatOptions = new HTMLFormatOptions();
			html40FormatOptions.HTMLBaseFolderName = reportFile.Path + "\\" + reportFile.FileName;
			html40FormatOptions.HTMLFileName = HTMLFILENAME;
			html40FormatOptions.HTMLEnableSeparatedPages = true;
			html40FormatOptions.HTMLHasPageNavigator = true;
			report.ExportOptions.FormatOptions = html40FormatOptions;
		}
	}
}