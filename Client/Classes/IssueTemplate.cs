using System;
using System.Collections;
using System.Collections.Generic;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public enum IssueTemplateMode
	{
		WeekDays,
		OddEvenDays,
		TimePeriod
	}

	public class IssueTemplate : IEnumerator
	{
		private IssueTemplateMode mode;
		private DateTime currentDate;
		private bool bFirstMove;

		private bool isOdd;
		private bool[] weekDays;
		private readonly Dictionary<string, object> parameters;

		public bool[] WeekDays
		{
			get { return weekDays; }
			set { weekDays = value; }
		}

		public IssueTemplate()
		{
			mode = IssueTemplateMode.WeekDays;
			weekDays = new bool[DateTimeUtils.WeekDayNames.Length];
			StartDate = FinishDate = DateTime.MinValue.Date;
			parameters = new Dictionary<string, object>();
		}

        public IssueTemplate(DateTime startDate, DateTime finishDate, DateTime startTime, DateTime finishTime, int quantity)
        {
            StartDate = startDate;
            FinishDate = finishDate;
            StartTime = startTime;
            FinishTime = finishTime;
            Quantity = quantity;
			mode = IssueTemplateMode.TimePeriod;
            weekDays = new bool[DateTimeUtils.WeekDayNames.Length];
            parameters = new Dictionary<string, object>();
        }
        public int DaysCount
        {
            get 
			{
				int count = 0;
				while (MoveNext()) count++;
				Reset();
				return count;
			}
        }

        public Dictionary<string, object> Parameters
		{
			get
			{
				parameters["StartDate"] = StartDate;
				parameters["EndDate"] = FinishDate;
				parameters["Template"] = (Mode == IssueTemplateMode.OddEvenDays) ? "Чётный/Нечётный" : "Дни недели";
				return parameters;
			}
		}

		public bool IsOdd
		{
			get { return isOdd; }
			set
			{
				Reset();
				isOdd = value;
			}
		}

		public IssueTemplateMode Mode
		{
			get { return mode; }
			set
			{
				Reset();
				mode = value;
			}
		}

		public void SetTime(DateTime time)
		{
			StartDate = StartDate.Date + time.TimeOfDay;
			FinishDate = FinishDate.Date + time.TimeOfDay;
		}

		public DateTime CurrentDate
		{
			get { return currentDate; }
		}

		public DateTime StartDate { get; internal set; }

		public DateTime FinishDate { get; internal set; }

		public int Quantity { get; internal set; }

		public DateTime StartTime { get; internal set; }

		public DateTime FinishTime { get; internal set; }

		public bool IsDateCovered(DateTime startOfInterval, DateTime endOfInterval)
		{
			return StartDate <= endOfInterval && FinishDate >= startOfInterval;
		}

		public bool MoveNext()
		{
			if (StartDate > FinishDate) return false;
			if (mode == IssueTemplateMode.OddEvenDays)
			{
				bool bIsOdd = currentDate.Day%2 == 1;
				if (!bFirstMove || bIsOdd != isOdd)
				{
					int day2add = (bIsOdd == isOdd) ? 2 : 1;
					int daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
					if (day2add + currentDate.Day > daysInMonth)
					{
						day2add = daysInMonth - currentDate.Day + ((isOdd) ? 1 : 2);
					}
					currentDate = currentDate.AddDays(day2add);
				}
			}
			else
			{
				int day = DateTimeUtils.ResolveDayOfWeekNumber(currentDate.DayOfWeek) - 1;
				if (day < 0) return false;
				int days2add = -1;

				for (int i = day + ((bFirstMove) ? 0 : 1); i < weekDays.Length; i++)
				{
					if (weekDays[i])
					{
						days2add = i - day;
						break;
					}
				}
				if (days2add < 0)
				{
					for (int i = 0; i <= day; i++)
					{
						if (weekDays[i])
						{
							days2add = i + weekDays.Length - day;
							break;
						}
					}
				}
				if (days2add < 0) return false;
				currentDate = currentDate.AddDays(days2add);
			}
			bFirstMove = false;
			return currentDate.Date <= FinishDate.Date;
		}

		public void Reset()
		{
			currentDate = StartDate;
			bFirstMove = true;
		}
		
		public object Current
		{
			get { return currentDate; }
		}
    }
}