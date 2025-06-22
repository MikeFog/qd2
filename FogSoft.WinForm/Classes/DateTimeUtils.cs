using System;
using System.Globalization;

namespace FogSoft.WinForm.Classes
{
	public static class DateTimeUtils
	{
		public static string[] WeekDayNames = {
		                                      	"Понедельник",
		                                      	"Вторник",
		                                      	"Среда",
		                                      	"Четверг",
		                                      	"Пятница",
		                                      	"Суббота",
		                                      	"Воскресенье"
		                                      };

		private static readonly string[] WeekDayNamesShort = {
		                                            	"Пн.",
		                                            	"Вт.",
		                                            	"Ср.",
		                                            	"Чт.",
		                                            	"Пт.",
		                                            	"Сб.",
		                                            	"Вс."
		                                            };

		public enum WeekDayNameFormat
		{
			Full,
			Short
		}

		public static int ResolveDayOfWeekNumber(DayOfWeek day)
		{
			switch(day)
			{
				case DayOfWeek.Monday:
					return 1;
				case DayOfWeek.Tuesday:
					return 2;
				case DayOfWeek.Wednesday:
					return 3;
				case DayOfWeek.Thursday:
					return 4;
				case DayOfWeek.Friday:
					return 5;
				case DayOfWeek.Saturday:
					return 6;
				case DayOfWeek.Sunday:
					return 7;
			}
			return 0;
		}

		public static string ResolveWeekDayName(DayOfWeek day)
		{
			return ResolveWeekDayName(day, WeekDayNameFormat.Full);
		}

		public static string ResolveWeekDayName(DayOfWeek day, WeekDayNameFormat format)
		{
			int index = ResolveDayOfWeekNumber(day) - 1;
			if(index >= 0)
				return format == WeekDayNameFormat.Full ? WeekDayNames[index] : WeekDayNamesShort[index];
			return string.Empty;
		}

		public static string ToDateTimeString(DateTime date)
		{
			return date.ToString("g", CultureInfo.CurrentCulture);
		}

		public static string Time2String(int time)
		{
			int min, hour;
			hour = Math.DivRem(Math.Abs(time), 60, out min);
			string res = Time2String(hour, min);

			return string.Format(time < 0 ? "-{0}" : "{0}", res);
		}

		public static string Time2String(int hour, int min)
		{
			return string.Format("{0:00}:{1:00}", hour, min);
		}

        public static string Time2StringHHMMSS(int totalSeconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
            return time.ToString(@"hh\:mm\:ss"); ;
        }
    }
}