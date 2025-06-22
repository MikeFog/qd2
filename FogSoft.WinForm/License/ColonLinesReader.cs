using System;
using System.Collections.Generic;
using System.IO;

namespace FogSoft.WinForm.License
{
	public class ColonLinesReader
	{
		public Dictionary<string, string> GetPropertiesFromFile(string path)
		{
			return GetProperties(File.ReadAllText(path, KeyManager.Encoding));
		}

		public Dictionary<string, string> GetProperties(string content)
		{
			string[] lines =
				content.Split(new char[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
			Dictionary<string, string> result =
				new Dictionary<string, string>(lines.Length, StringComparer.InvariantCultureIgnoreCase);
			
			foreach (string line in lines)
			{
				int colonIndex = line.IndexOf(':');
				if (colonIndex < 1 || colonIndex >= line.Length - 1)
					throw new ArgumentException(string.Format("Invalid line format ('{0}').", line), "content");
				result.Add(line.Substring(0, colonIndex), line.Substring(colonIndex + 1));
			}
			return result;
		}
	}
}