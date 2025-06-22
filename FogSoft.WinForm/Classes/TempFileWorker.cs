using System.Collections.Generic;
using System.IO;

namespace FogSoft.WinForm.Classes
{
	public static class TempFileWorker
	{
		private static readonly IList<string> fileNames = new List<string>();

		public static string GetTemplateFile()
		{
			string tempFileName = Path.GetTempFileName();
			fileNames.Add(tempFileName);
			return tempFileName;
		}

		public static void Clear()
		{
			IList<string> list = new List<string>(fileNames);
			foreach (string fileName in list)
			{
				try
				{
					File.Delete(fileName);
					fileNames.Remove(fileName);
				}
				catch
				{
				}
			}
		}
	}
}
