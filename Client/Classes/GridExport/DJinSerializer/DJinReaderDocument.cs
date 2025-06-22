using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Merlin.Classes.GridExport.DJinSerializer;

namespace Merlin.Classes.GridExport.DJinSerializer
{
	public class DJinReaderDocument
	{
		public enum Blocks  
		{
			block = 0,
			type = 1,
			time = 2,
			name = 3,
			path = 4,
			duration = 5
		}

		public static void Read(IList<string> files, string fileTo)
		{
			SortedList<DateTime, DJinBlock> list = new SortedList<DateTime, DJinBlock>();
			foreach (string file in files)
				ReadFile(file, list);
			Export(list, fileTo);
		}

		private static void Export(IEnumerable<KeyValuePair<DateTime, DJinBlock>> list, string fileTo)
		{
			FileStream file = new FileStream(fileTo, FileMode.Create);
			foreach (KeyValuePair<DateTime, DJinBlock> pair in list)
			{
				printLine(file, DJinParam.strBlockStart, string.Empty, pair.Key.ToString("T"), pair.Value.GetName(), string.Empty, pair.Value.GetDuration());
				if (pair.Value.JingleIn != null)
					printLine(file, string.Empty, DJinParam.strJingle, string.Empty, string.Empty, pair.Value.JingleIn.Path, string.Empty);

				ExportRollers(pair.Value.News, file, DJinParam.strRollerNews);
				ExportRollers(pair.Value.Programs, file, DJinParam.strRollerProgram);
				ExportRollers(pair.Value.Rollers, file, DJinParam.strRoller);
                
				if (pair.Value.JingleOut != null)
					printLine(file, string.Empty, DJinParam.strJingle, string.Empty, string.Empty, pair.Value.JingleOut.Path, string.Empty);
				if (pair.Value.Etc != null)
					printLine(file, string.Empty, DJinParam.strEtc, string.Empty, string.Empty, pair.Value.Etc.Path, string.Empty);
				printLine(file, DJinParam.strBlockEnd, string.Empty, pair.Key.ToString("T"), string.Empty, string.Empty, string.Empty);
			}
			file.Close();
		}

		private static void ExportRollers(IEnumerable<DJinRoller> rollers, Stream file, string preffix)
		{
			foreach (DJinRoller roller in rollers)
				printLine(file, string.Empty, preffix, string.Empty, string.Empty, roller.Path, roller.GetDuration());
		}

		private static void printLine(Stream file, string s0, string s1, string s2, string s3, string s4, string s5)
		{
			byte[] bytes = DJinParam.Encoding.GetBytes(string.Format(DJinParam.strLine, s0, s1, s2, s3, s4, s5));
			file.Write(bytes, 0, bytes.Length);
		}

		private static void ReadFile(string file, IDictionary<DateTime, DJinBlock> list)
		{
			if (File.Exists(file))
			{
				StreamReader stream = new StreamReader(file, DJinParam.Encoding);
				DJinBlock block = null;
				string lastType = string.Empty;
				while (!stream.EndOfStream)
				{
					string[] values = RemoveQuotes(stream.ReadLine()).Split(new string[] { "\",\"" }, StringSplitOptions.None);

					if (string.Compare(values[(int)Blocks.block], DJinParam.strBlockEnd) == 0)
					{
						lastType = values[(int)Blocks.type];
						continue;
					}

					if (string.Compare(values[(int)Blocks.block], DJinParam.strBlockStart) == 0)
					{
						DateTime time = GetTime(values[(int)Blocks.time]);
						if (!list.ContainsKey(time))
							list.Add(time, block = new DJinBlock(time, GetBlockName(values[(int)Blocks.name]), GetDuration(values[(int)Blocks.duration])));
						else
						{
							block = list[time];
							block.SetMaxDuration(GetDuration(values[(int)Blocks.duration]));
						}
						lastType = values[(int)Blocks.type];
						continue;
					}

					if (block != null)
					{
						ReadRoller(block, values, lastType);
					}
					lastType = values[(int)Blocks.type];
				}
				stream.Close();
			}
		}

		private static void ReadRoller(DJinBlock block, string[] values, string lastType)
		{
			DJinRoller roller = new DJinRoller(values[(int)Blocks.path], GetDuration(values[(int)Blocks.duration]));
			switch(values[(int)Blocks.type])
			{
				case DJinParam.strEtc:
					{
						block.Etc = roller;
						return;
					}
				case DJinParam.strJingle:
					{
						if (string.IsNullOrEmpty(lastType))
							block.JingleIn = roller;
						else
							block.JingleOut = roller;
						return;
					}
				case DJinParam.strRoller:
					{
						block.AddRoller(roller);
						return;
					}
				case DJinParam.strRollerProgram:
					{
						block.AddProgram(roller);
						return;
					}
				case DJinParam.strRollerNews:
					{
						block.AddNews(roller);
						return;
					}
			}
		}

		private static int? GetDuration(string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;
			string[] strs = value.Split(':');
			return int.Parse(strs[0])*60 + int.Parse(strs[1]);
		}

		public static string GetDuration(int? duration)
		{
			if (duration == null)
				duration = 0;
			int? minutes = duration / 60;
			int? seconds = duration - 60 * minutes;
			string strZero = "0";
			return string.Format("{0}{1}:{2}{3}", minutes < 10 ? strZero : string.Empty, minutes, seconds < 10 ? strZero : string.Empty, seconds);
		}

		private static string GetBlockName(string strs)
		{
			// remove time in end of string " â HH:mm"
			return strs.Length > 8 ? strs.Substring(0, strs.Length - 8) : string.Empty;
		}

		private static DateTime GetTime(string value)
		{
			IFormatProvider culture = new CultureInfo("en-US", true);
			return (String.IsNullOrEmpty(value) ?
			                                    	DateTime.MinValue :
			                                    	                  	DateTime.ParseExact(value, "H:mm:ss", culture, DateTimeStyles.AllowWhiteSpaces));
		}

		private static string RemoveQuotes(string value)
		{
			return (!string.IsNullOrEmpty(value)) && value.Length > 2 ? value.Substring(1, value.Length - 2) : string.Empty;
		}
	}

	internal class DJinBlock
	{
		private DateTime time;
		private readonly string name;
		private int? duration;
		private readonly IList<DJinRoller> rollers = new List<DJinRoller>();
		private readonly IDictionary<string, DJinRoller> programs = new Dictionary<string, DJinRoller>();
		private readonly IDictionary<string, DJinRoller> news = new Dictionary<string, DJinRoller>();
		private DJinRoller etc;
		private DJinRoller jingleIn;
		private DJinRoller jingleOut;

		public DJinBlock(DateTime time, string name, int? duration)
		{
			this.time = time;
			this.name = name;
			this.duration = duration;
		}

		public void SetMaxDuration(int? _duration)
		{
			duration = Math.Max(_duration ?? 0, duration ?? 0);
		}
        
		public void AddRoller(DJinRoller roller)
		{
			rollers.Add(roller);
		}

		public void AddProgram(DJinRoller roller)
		{
			SetOnlyNew(programs, roller);
		}

		public void AddNews(DJinRoller roller)
		{
			SetOnlyNew(news, roller);
		}

		private static void SetOnlyNew(IDictionary<string, DJinRoller> rollers, DJinRoller roller)
		{
			if (!rollers.ContainsKey(roller.Path))
				rollers.Add(roller.Path, roller);
			else
				rollers[roller.Path].SetMaxDuration(roller.Duration);
		}

		public DJinRoller Etc
		{
			get { return etc; }
			set { if (etc == null) etc = value; }
		}

		public DJinRoller JingleIn
		{
			get { return jingleIn; }
			set { if (jingleIn == null) jingleIn = value; }
		}

		public DJinRoller JingleOut
		{
			get { return jingleOut; }
			set { if (jingleOut == null) jingleOut = value; }
		}

		public string GetName()
		{
			return string.Format("{0} â {1}", name, time.ToString("t"));
		}

		public string GetDuration()
		{
			return DJinReaderDocument.GetDuration(duration);
		}

		public IList<DJinRoller> Rollers
		{
			get { return rollers; }
		}

		public ICollection<DJinRoller> Programs
		{
			get { return programs.Values; }
		}

		public ICollection<DJinRoller> News
		{
			get { return news.Values; }
		}
	}

	internal class DJinRoller
	{
		private readonly string path;
		private int? duration;
        
		public DJinRoller(string path, int? duration)
		{
			this.path = path;
			this.duration = duration;
		}
        
		public string Path
		{
			get { return path; }
		}

		public int? Duration
		{
			get { return duration; }
		}

		public string GetDuration()
		{
			return DJinReaderDocument.GetDuration(duration);
		}

		public void SetMaxDuration(int? _duration)
		{
			duration = Math.Max(_duration ?? 0, duration ?? 0);
		}
	}
}