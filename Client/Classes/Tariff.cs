using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	public class ModuleTariff : Tariff
	{
		public ModuleTariff() : base(EntityManager.GetEntity((int) Entities.ModuleTariff))
		{
		}

		public ModuleTariff(DataRow row)
			: base(EntityManager.GetEntity((int)Entities.ModuleTariff), row)
		{
		}
	}

	// UI-часть (DoAction) — в Tariff.WinForms.cs.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class Tariff : PresentationObject
	{
		public struct ParamNames
		{
			public const string TariffId = "tariffID";
			public const string IsSpecial = "isSpecial";
			public const string Monday = "monday";
			public const string Tuesday = "tuesday";
			public const string Wednesday = "wednesday";
			public const string Thursday = "thursday";
			public const string Friday = "friday";
			public const string Saturday = "saturday";
			public const string Sunday = "sunday";
			public const string Time = "time";
			public const string Duration = "duration";
            public const string DurationTotal = "duration_total";
            public const string Price = "price";
			public const string TimeString = "timeString";
		}

		public Tariff() : base(EntityManager.GetEntity((int) Entities.Tariff))
		{
		}

		public Tariff(DataRow row) : base(EntityManager.GetEntity((int) Entities.Tariff), row)
		{
		}

		private Tariff(int tariffID) : base(EntityManager.GetEntity((int) Entities.Tariff))
		{
			this[ParamNames.TariffId] = tariffID;
			isNew = false;
		}
        
		public Tariff(Entity entity) : base(entity)
		{
		}

		public Tariff(Entity entity, DataRow row) : base(entity, row)
		{
		}

		internal bool IsSpecial
		{
			get { return bool.Parse(this[ParamNames.IsSpecial].ToString()); }
			set { this[ParamNames.IsSpecial] = value; }
		}

		internal bool Monday
		{
			get { return bool.Parse(this[ParamNames.Monday].ToString()); }
			set { this[ParamNames.Monday] = value; }
		}

		internal bool Tuesday
		{
			get { return bool.Parse(this[ParamNames.Tuesday].ToString()); }
			set { this[ParamNames.Tuesday] = value; }
		}

		internal bool Wednesday
		{
			get { return bool.Parse(this[ParamNames.Wednesday].ToString()); }
			set { this[ParamNames.Wednesday] = value; }
		}

		internal bool Thursday
		{
			get { return bool.Parse(this[ParamNames.Thursday].ToString()); }
			set { this[ParamNames.Thursday] = value; }
		}

		internal bool Friday
		{
			get { return bool.Parse(this[ParamNames.Friday].ToString()); }
			set { this[ParamNames.Friday] = value; }
		}

		internal bool Saturday
		{
			get { return bool.Parse(this[ParamNames.Saturday].ToString()); }
			set { this[ParamNames.Saturday] = value; }
		}

		internal bool Sunday
		{
			get { return bool.Parse(this[ParamNames.Sunday].ToString()); }
			set { this[ParamNames.Sunday] = value; }
		}

		internal int PricelistID
		{
			get { return int.Parse(this[Pricelist.ParamNames.PricelistId].ToString()); }
			set { this[Pricelist.ParamNames.PricelistId] = value; }
		}

		internal Pricelist Pricelist
		{
			get { return Pricelist.GetPricelistById(PricelistID, EntityManager.GetEntity((int) Entities.Pricelist)); }
		}

		internal DateTime Time
		{
			get { return DateTime.Parse(this[ParamNames.Time].ToString()); }
		}

		internal string TimeString
		{
			get { return this[ParamNames.TimeString].ToString(); }
		}

		internal decimal Price
		{
			get { return decimal.Parse(this[ParamNames.Price].ToString()); }
		}

		internal int Duration
		{
			get { return int.Parse(this[ParamNames.Duration].ToString()); }
		}

        internal int DurationTotal
        {
            get { return int.Parse(this[ParamNames.DurationTotal].ToString()); }
        }

        internal int TariffId
		{
			get { return int.Parse(IDs[0].ToString()); }
		}

		/// <summary>
		/// Черновик копии тарифа: значения исходного без ключа, обычное добавление
		/// (AddItem) — тариф копируется целиком карточкой, отдельной процедуры Clone
		/// у него нет. Сборка перенесена из ветки Clone в DoAction без изменений.
		/// </summary>
		public override PresentationObject CreateCloneDraft()
		{
			Tariff draft = new Tariff { parameters = Parameters };
			draft.parameters[ParamNames.TariffId] = null;
			draft.parameters[Constants.ParamNames.ActionName] = Constants.Actions.AddItem;
			return draft;
		}

		internal static Tariff GetTariffByID(int tariffID)
		{
			Tariff tariff = new Tariff(tariffID);
			tariff.Refresh();
			return tariff;
		}

		/// <summary>
		/// Массово создаёт тарифы: по одному в каждом часе от <paramref name="hourFrom"/> до
		/// <paramref name="hourTo"/> включительно, в минуту <paramref name="minute"/>. Остальные
		/// параметры берутся из <paramref name="template"/> (значения паспорта). Каждый тариф —
		/// отдельный вызов TariffIUD, best-effort: не создавшиеся (дубль, спонсорский тариф,
		/// разрыв цепочки) попадают в <paramref name="tableErrors"/>, остальные создаются.
		/// </summary>
		internal static int CreateMass(Dictionary<string, object> template, int hourFrom, int hourTo, int minute,
			out DataTable tableErrors)
		{
			tableErrors = ErrorManager.CreateErrorsTable();
			int created = 0;

			for (int hour = hourFrom; hour <= hourTo; hour++)
			{
				DateTime time = new DateTime(1900, 1, 1, hour, minute, 0);
				try
				{
					Tariff tariff = new Tariff();
					foreach (KeyValuePair<string, object> kvp in template)
						tariff[kvp.Key] = kvp.Value;
					tariff[ParamNames.Time] = time;
					tariff.Update();
					created++;
				}
				catch (Exception ex)
				{
					ErrorManager.AddErrorRow(tableErrors, DateTime.Now,
						string.Format("{0:HH:mm}: {1}", time, ErrorManager.GetErrorMessage(ex)));
				}
			}

			return created;
		}

		internal static readonly string[] DayNames =
			{ ParamNames.Monday, ParamNames.Tuesday, ParamNames.Wednesday, ParamNames.Thursday,
			  ParamNames.Friday, ParamNames.Saturday, ParamNames.Sunday };

		// Атрибуты, по которым проверяем, менял ли пользователь что-то в форме массового редактирования.
		private static readonly string[] MassEditAttributes =
			{ ParamNames.Price, ParamNames.Duration, ParamNames.DurationTotal, "maxCapacity", "isForModuleOnly",
			  "needInJingle", "needOutJingle", "blockTypeID", "notEarly", "notLater", "openBlock", "openPhonogram",
			  "comment", "suffix" };

		/// <summary>
		/// «Похожие» на этот тариф: тот же прайс-лист, та же минута выхода и совпадение всех остальных
		/// атрибутов (включая дни недели), т.е. отличаются только часом. Колонки: tariffID, hour,
		/// hasWindows (есть сгенерированные окна), inUnion (входит в цепочку объединения).
		/// Сам тариф входит в результат.
		/// </summary>
		internal DataTable LoadSimilarTariffs()
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[ParamNames.TariffId] = TariffId;
			return DataAccessor.LoadDataSet("TariffSimilar", procParameters).Tables[0];
		}

		/// <summary>Менял ли пользователь хоть один атрибут (кроме дней и интервала часов) или минуту.</summary>
		internal static bool HasMassEditChanges(Dictionary<string, object> original, Dictionary<string, object> edited, int newMinute)
		{
			if (Convert.ToDateTime(original[ParamNames.Time]).Minute != newMinute)
				return true;
			foreach (string name in MassEditAttributes)
			{
				object oldValue, newValue;
				original.TryGetValue(name, out oldValue);
				edited.TryGetValue(name, out newValue);
				if (!SameValue(oldValue, newValue))
					return true;
			}
			return false;
		}

		private static bool SameValue(object a, object b)
		{
			string sa = a == null || a == DBNull.Value ? string.Empty : a.ToString().Trim();
			string sb = b == null || b == DBNull.Value ? string.Empty : b.ToString().Trim();
			if (sa == sb)
				return true;
			decimal da, db;
			return decimal.TryParse(sa, out da) && decimal.TryParse(sb, out db) && da == db;
		}

		private static bool IsDayOn(Dictionary<string, object> values, string dayName)
		{
			return bool.Parse(values[dayName].ToString());
		}

		/// <summary>
		/// Массово правит «похожие» тарифы (см. <see cref="LoadSimilarTariffs"/>) значениями из
		/// <paramref name="edited"/>. Дни недели в <paramref name="edited"/> - «область применения»:
		/// если отмечены все дни исходного тарифа, тариф правится на месте; если часть дней, тариф делится -
		/// у существующего снимаются отмеченные дни (остальные атрибуты прежние), а на отмеченные дни
		/// создаётся новый тариф с новыми значениями. Деление - в одной транзакции. Минута выхода меняется
		/// у каждого тарифа при сохранении его часа; <paramref name="hourFrom"/>-<paramref name="hourTo"/>
		/// сужают набор. Тарифы с окнами и в цепочках объединения пропускаются, все сбои - в
		/// <paramref name="tableErrors"/>, остальные тарифы обрабатываются (best-effort).
		/// </summary>
		internal static void ApplyMassEdit(Dictionary<string, object> original, Dictionary<string, object> edited,
			DataTable similar, int hourFrom, int hourTo, int minute,
			out DataTable tableErrors, out List<Tariff> changed, out List<Tariff> added)
		{
			tableErrors = ErrorManager.CreateErrorsTable();
			changed = new List<Tariff>();
			added = new List<Tariff>();

			List<string> sourceDays = new List<string>();
			List<string> scopeDays = new List<string>();
			foreach (string day in DayNames)
			{
				if (IsDayOn(original, day)) sourceDays.Add(day);
				if (IsDayOn(edited, day)) scopeDays.Add(day);
			}
			bool wholeTariff = scopeDays.Count == sourceDays.Count;
			int oldMinute = Convert.ToDateTime(original[ParamNames.Time]).Minute;

			// значения для записи: всё из формы, кроме идентификатора исходного тарифа и связи с блоком
			Dictionary<string, object> values = new Dictionary<string, object>(edited, StringComparer.InvariantCultureIgnoreCase);
			values.Remove(ParamNames.TariffId);
			values["isUnionEnable"] = false;
			values["tariffUnionID"] = null;

			foreach (DataRow row in similar.Rows)
			{
				int tariffId = Convert.ToInt32(row["tariffID"]);
				int hour = Convert.ToInt32(row["hour"]);
				if (hour < hourFrom || hour > hourTo) continue;

				DateTime oldTime = new DateTime(1900, 1, 1, hour, oldMinute, 0);
				DateTime newTime = new DateTime(1900, 1, 1, hour, minute, 0);

				if (Convert.ToBoolean(row["hasWindows"]))
				{
					ErrorManager.AddErrorRow(tableErrors, DateTime.Now, string.Format(
						"{0:HH:mm}: у тарифа есть сгенерированные окна - сначала удалите их или правьте окна", oldTime));
					continue;
				}
				if (Convert.ToBoolean(row["inUnion"]))
				{
					ErrorManager.AddErrorRow(tableErrors, DateTime.Now, string.Format(
						"{0:HH:mm}: тариф входит в цепочку объединения - правьте его вручную", oldTime));
					continue;
				}

				bool inTransaction = false;
				try
				{
					if (wholeTariff)
					{
						Tariff tariff = GetTariffByID(tariffId);
						ApplyValues(tariff, values, newTime, scopeDays);
						tariff.Update();
						changed.Add(tariff);
					}
					else
					{
						// Существующий тариф теряет отмеченные дни, на них создаётся новый с новыми значениями.
						// Сначала снимаем дни: иначе новый тариф упрётся в проверку дубля по времени и дням.
						Tariff existing = GetTariffByID(tariffId);
						List<string> remainingDays = sourceDays.FindAll(d => !scopeDays.Contains(d));
						foreach (string day in DayNames)
							existing[day] = remainingDays.Contains(day);

						Tariff created = new Tariff();
						ApplyValues(created, values, newTime, scopeDays);

						DataAccessor.BeginTransaction();
						inTransaction = true;
						existing.Update();
						created.Update();
						DataAccessor.CommitTransaction();
						inTransaction = false;

						changed.Add(existing);
						added.Add(created);
					}
				}
				catch (Exception ex)
				{
					if (inTransaction)
						DataAccessor.RollbackTransaction();
					ErrorManager.AddErrorRow(tableErrors, DateTime.Now,
						string.Format("{0:HH:mm}: {1}", oldTime, ErrorManager.GetErrorMessage(ex)));
				}
			}
		}

		private static void ApplyValues(Tariff tariff, Dictionary<string, object> values, DateTime time, List<string> days)
		{
			foreach (KeyValuePair<string, object> kvp in values)
				tariff[kvp.Key] = kvp.Value;
			tariff[ParamNames.Time] = time;
			foreach (string day in DayNames)
				tariff[day] = days.Contains(day);
		}

		// DoAction переехал в Tariff.WinForms.cs (IWin32Window в сигнатуре).

		// GetPassportForm переехал в Tariff.WinForms.cs (возвращает UI-тип PassportForm).
	}
}