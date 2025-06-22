using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace Merlin.Classes
{
	public class MediaPlanCampaignGroups
	{
		private readonly IList<MediaPlanMassmedia> massmedias = new List<MediaPlanMassmedia>();
		private readonly SortedList<int, string> uniquesList = new SortedList<int, string>();

		public void AddMassmedia(int massmediaID, String massmediaName, int rollerID, DateTime day)
		{
			foreach (MediaPlanMassmedia massmedia in massmedias)
			{
				if (massmedia.CompareTo(massmediaID) == 0)
				{
					massmedia.Add(rollerID, day);
					return;
				}
			}
			MediaPlanMassmedia mm = new MediaPlanMassmedia(massmediaID, massmediaName, rollerID, day);
			massmedias.Add(mm);
		}

		public void InitUniquesList(DataTable dt)
		{
			if (dt != null)
			{
				foreach (DataRow row in dt.Rows)
				{
					int mmID = int.Parse(row[Massmedia.ParamNames.MassmediaId].ToString());
					string name = row[Massmedia.ParamNames.Name].ToString();
					uniquesList.Add(mmID, name);
				}
			}
		}

		public IDictionary<String, String> GetUniqueMassmedias()
		{
			SortedDictionary<MediaPlanMassmedia, KeyValuePair<String, String>> mms = new SortedDictionary<MediaPlanMassmedia, KeyValuePair<string, string>>();
			foreach (MediaPlanMassmedia massmedia in massmedias)
			{
				if (!uniquesList.ContainsKey(massmedia.MassmediaID) && mms.ContainsKey(massmedia))
				{
					KeyValuePair<string, string> oldPair = mms[massmedia];
					mms[massmedia] = new KeyValuePair<string, string>(
						String.Format("{0}{1},", oldPair.Key, massmedia.MassmediaID)
						, String.Format("{0}, {1}", oldPair.Value, massmedia.MassmediaName));
				}
				else
				{
					mms.Add(massmedia, new KeyValuePair<string, string>(
						String.Format("{0},", massmedia.MassmediaID)
						, String.Format("{0}", massmedia.MassmediaName)));
				}
			}
			IDictionary<String, String> uniques = new Dictionary<String, String>();
			foreach (KeyValuePair<int, string> pair in uniquesList)
			{
				bool isContains = false;
				foreach (MediaPlanMassmedia massmedia in massmedias)
				{
					if (massmedia.MassmediaID == pair.Key)
					{
						isContains = true;	
					}
				}
				if (!isContains)
				{
					mms.Add(new MediaPlanMassmedia(pair.Key, pair.Value), 
							new KeyValuePair<string, string>(String.Format("{0},", pair.Key), pair.Value));
				}
			}
			foreach (KeyValuePair<string, string> pair in mms.Values)
			{
				uniques.Add(pair);
			}
			
			return uniques;
		}

		#region Helpers Classes

		private class MediaPlanMassmedia : IComparable<MediaPlanMassmedia>, IComparable<int>
		{
			private readonly int massmediaID;
			private readonly String massmediaName;
			private readonly MediaPlanRollers rollers = new MediaPlanRollers();
			private readonly MediaPlanDays days = new MediaPlanDays();

			public MediaPlanMassmedia(int massmediaID, String massmediaName)
			{
				this.massmediaID = massmediaID;
				this.massmediaName = massmediaName;
			}

			public MediaPlanMassmedia(int massmediaID, String massmediaName, int rollerID, DateTime day)
			{
				this.massmediaID = massmediaID;
				this.massmediaName = massmediaName;
				Add(rollerID, day);
			}

			public int MassmediaID
			{
				get { return massmediaID; }
			}

			public string MassmediaName
			{
				get { return massmediaName; }
			}

			public void Add(int rollerID, DateTime day)
			{
				AddRoller(rollerID);
				AddDay(day);
			}

			private void AddRoller(int rollerID)
			{
				MediaPlanRoller roller = new MediaPlanRoller(rollerID);
				rollers.AddRoller(roller);
			}

			private void AddDay(DateTime day)
			{
				days.AddDay(day);
			}

			#region IComparable<MediaPlanMassmedia> Members

			public int CompareTo(MediaPlanMassmedia other)
			{
				return 1;
				/*
				return Math.Abs(rollers.CompareTo(other.rollers))
				       + Math.Abs(days.CompareTo(other.days));
				*/
			}

			#endregion

			#region IComparable<int> Members

			public int CompareTo(int other)
			{
				return MassmediaID - other;
			}

			#endregion
		}

		private class MediaPlanRoller : IComparable<MediaPlanRoller>
		{
			private readonly int rollerID;

			public MediaPlanRoller(int rollerID)
			{
				this.rollerID = rollerID;
			}

			#region IComparable<MediaPlanRoller> Members

			public int CompareTo(MediaPlanRoller other)
			{
				return rollerID - other.rollerID;
			}

			#endregion
		}

		private class MediaPlanRollers : IComparable<MediaPlanRollers>
		{
			private readonly SortedList<MediaPlanRoller, MediaPlanRoller> rollers = new SortedList<MediaPlanRoller, MediaPlanRoller>();

			public void AddRoller(MediaPlanRoller roller)
			{
				if (!rollers.ContainsKey(roller))
					rollers.Add(roller, roller);
			}

			#region IComparable<MediaPlanRoller> Members

			public int CompareTo(MediaPlanRollers other)
			{
				foreach (MediaPlanRoller roller in rollers.Keys)
				{
					if (rollers.Keys.Count == 0)
						return 1;
					if (!other.rollers.ContainsKey(roller))
						return roller.CompareTo(rollers.Keys[0]);
				}
				foreach (MediaPlanRoller roller in other.rollers.Keys)
				{
					if (rollers.Keys.Count == 0)
						return 1;
					if (!rollers.ContainsKey(roller))
						return rollers.Keys[0].CompareTo(roller);
				}
				return 0;
			}

			#endregion
		}

		private class MediaPlanDays : IComparable<MediaPlanDays>
		{
			private readonly SortedList<MediaPlanDay, MediaPlanDay> days = new SortedList<MediaPlanDay, MediaPlanDay>();

			public void AddDay(DateTime time)
			{
				foreach (MediaPlanDay day in days.Keys)
				{
					if (day.CompareTo(time) == 0)
					{
						day.AddDay(time);
						return;
					}
				}
				MediaPlanDay mpDay = new MediaPlanDay(time);
				days.Add(mpDay, mpDay);
			}

			#region IComparable<MediaPlanDays> Members

			public int CompareTo(MediaPlanDays other)
			{
				foreach (MediaPlanDay day in days.Keys)
				{
					if (other.days.Keys.Count == 0)
						return 1;
					if (!other.days.ContainsKey(day))
						return day.CompareTo(other.days.Keys[0]);
				}
				foreach (MediaPlanDay day in other.days.Keys)
				{
					if (other.days.Keys.Count == 0)
						return 1;
					if (!days.ContainsKey(day))
						return other.days.Keys[0].CompareTo(day);
				}
				return 0;
			}

			#endregion
		}

		private class MediaPlanDay : IComparable<MediaPlanDay>, IComparable<DateTime>
		{
			private readonly MediaPlanTimes times = new MediaPlanTimes();

			public MediaPlanDay(DateTime time)
			{
				AddDay(time);
			}

			public void AddDay(MediaPlanTime time)
			{
				times.AddTime(time);
			}

			public void AddDay(DateTime time)
			{
				MediaPlanTime _time = new MediaPlanTime(time);
				AddDay(_time);
			}

			#region IComparable<MediaPlanDay> Members

			public int CompareTo(MediaPlanDay other)
			{
				return times.CompareTo(other.times);
			}

			#endregion

			#region IComparable<DateTime> Members

			/// <summary>
			/// Сравнение по дате, время не учитывается
			/// </summary>
			/// <param name="other"></param>
			/// <returns></returns>
			public int CompareTo(DateTime other)
			{
				return times.CompareTo(other);
			}

			#endregion
		}

		private class MediaPlanTimes : IComparable<MediaPlanTimes>, IComparable<DateTime>
		{
			private readonly SortedList<MediaPlanTime, MediaPlanTime> times = new SortedList<MediaPlanTime, MediaPlanTime>();

			public void AddTime(MediaPlanTime time)
			{
				if (!times.ContainsKey(time))
					times.Add(time, time);
				else
					times[time].Count++;
			}

			#region IComparable<MediaPlanTimes> Members

			public int CompareTo(MediaPlanTimes other)
			{
				foreach (MediaPlanTime time in times.Keys)
				{
					if (!other.times.ContainsKey(time))
						return time.CompareTo(other.times.Keys[0]);
				}
				foreach (MediaPlanTime time in other.times.Keys)
				{
					if (!times.ContainsKey(time))
						return other.times.Keys[0].CompareTo(time);
				}
				return 0;
			}

			#endregion

			#region IComparable<DateTime> Members

			/// <summary>
			/// Сравнение по дате, время не учитывается
			/// </summary>
			/// <param name="other"></param>
			/// <returns></returns>
			public int CompareTo(DateTime other)
			{
				return times.Keys[0].CompareTo(other);
			}

			#endregion
		}

		private class MediaPlanTime : IComparable<MediaPlanTime>, IComparable<DateTime>
		{
			private readonly DateTime time;
			private int count = 1;

			public MediaPlanTime(DateTime time)
			{
				this.time = time;
			}

			#region IComparable<MediaPlanTime> Members

			public int CompareTo(MediaPlanTime other)
			{
				if (DateTime.Compare(time, other.time) == 0)
					return Count - other.Count;
				else return DateTime.Compare(time, other.time);
			}

			#endregion

			#region IComparable<DateTime> Members

			/// <summary>
			/// Сравненени по дате, время не учитывается
			/// </summary>
			/// <param name="other"></param>
			/// <returns></returns>
			public int CompareTo(DateTime other)
			{
				return DateTime.Compare(time.Date, other.Date);
			}

			#endregion

			public int Count
			{
				get { return count; }
				set { count = value; }
			}
		}

		#endregion
	}
}
