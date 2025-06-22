using System;

namespace Merlin.Classes.Exchange.MediaPlus
{
	internal class SimpleIssue
	{
		public SimpleIssue(int duration, DateTime date, byte hour, bool isFirstHalf)
		{
			Duration = duration;
			Date = date.Date;
			Hour = hour;
			IsFirstHalf = isFirstHalf;
		}

		public int Duration { get; private set; }
		public DateTime Date { get; private set; }
		public byte Hour { get; private set; }
		public bool IsFirstHalf { get; private set; }
	}
}