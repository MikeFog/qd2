using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// «Журнал использования роликов» без UI — для веб-экрана (десктоп — RollerStatisticForm).
	/// Те же процедуры и параметры, что у формы: stat_RollerStatistic (сущность 139, Load) и
	/// ActionsForRollerStatistic (сущность 77, LoadForRollerStatistic).
	/// </summary>
	public static class RollerStatisticQuery
	{
		/// <summary>Условия отбора — поля левой панели десктопной формы.</summary>
		public sealed class Filter
		{
			public DateTime Start = DateTime.Today;
			public DateTime Finish = DateTime.Today;
			public readonly HashSet<int> MassmediaIds = new HashSet<int>();
			public int? UserId;
			public int? FirmId;
			public int? AdvertTypeId;
			public int? HeadCompanyId;
			public bool ShowWhite = true;
			public bool ShowBlack = true;
			public bool SplitByManager;
			public bool SplitByDays;
		}

		/// <summary>Строки журнала и сущность колонок: у разбивки по дням к ней добавлены колонки дат.</summary>
		public sealed class Result
		{
			public Entity Entity;
			public DataTable Data;
		}

		/// <summary>Журнал по отбору (RollerStatisticForm.RefreshData).</summary>
		public static Result Load(Filter filter)
		{
			Entity entity = (Entity)EntityManager.GetEntity((int)Entities.RollerStatistic).Clone();
			Dictionary<string, object> parameters = DataAccessor.PrepareParameters(entity);
			parameters["massmediaString"] = MassmediaString(filter);
			parameters["startDate"] = filter.Start.Date;
			parameters["finishDate"] = filter.Finish.Date;
			parameters["showWhite"] = filter.ShowWhite;
			parameters["showBlack"] = filter.ShowBlack;
			parameters["splitByManager"] = filter.SplitByManager;
			parameters["splitByDays"] = filter.SplitByDays;
			if (filter.UserId != null)
				parameters[SecurityManager.ParamNames.UserId] = filter.UserId.Value;
			if (filter.FirmId != null)
				parameters[Firm.ParamNames.FirmId] = filter.FirmId.Value;
			if (filter.AdvertTypeId != null)
				parameters["advertTypeID"] = filter.AdvertTypeId.Value;
			if (filter.HeadCompanyId != null)
				parameters["headCompanyID"] = filter.HeadCompanyId.Value;

			DataSet ds = (DataSet)DataAccessor.DoAction(parameters);
			DataTable data = ds.Tables[0];
			if (filter.SplitByDays)
				AddDayColumns(entity, data, ds.Tables[1], ds.Tables[2], filter.SplitByManager);

			return new Result { Entity = entity, Data = data };
		}

		/// <summary>
		/// Колонка на каждый день периода с количеством выпусков ролика в этот день
		/// (RollerStatisticForm.CreateDataTableWithDates). Процедура отдаёт сырые строки
		/// (ролик, дата, менеджер, фирма) и список дат; раскладка — здесь.
		///
		/// Отличие от десктопа: строка журнала — ролик у фирмы (а при разбивке — ещё и у
		/// менеджера), и день считается по тем же признакам. Десктоп считал день только по
		/// ролику (и менеджеру), поэтому у ролика «для всех фирм» в каждой строке фирмы
		/// стояла сумма по всем фирмам. Дни без выпусков пустые, а не 0 — так таблица читается.
		/// </summary>
		private static void AddDayColumns(Entity entity, DataTable data, DataTable raw, DataTable days, bool byManager)
		{
			Dictionary<string, int> counts = new Dictionary<string, int>();
			foreach (DataRow row in raw.Rows)
			{
				string key = DayKey(row, byManager, (DateTime)row["date"]);
				int count;
				counts.TryGetValue(key, out count);
				counts[key] = count + 1;
			}

			foreach (DataRow day in days.Rows)
			{
				DateTime date = (DateTime)day["date"];
				string column = "day" + date.ToString("yyyyMMdd"); // i18n-ok: имя колонки данных
				data.Columns.Add(column, typeof(int));
				entity.SortedAttributes.Add(new Entity.Attribute(column, date.ToShortDateString(), "int"));

				foreach (DataRow row in data.Rows)
				{
					int count;
					if (counts.TryGetValue(DayKey(row, byManager, date), out count))
						row[column] = count;
				}
			}
		}

		private static string DayKey(DataRow row, bool byManager, DateTime date)
		{
			return row[Roller.ParamNames.RollerId] + "|" + row[Firm.ParamNames.FirmId] + "|"
				+ (byManager ? row[SecurityManager.ParamNames.UserId].ToString() : "") + "|" + date.Ticks;
		}

		/// <summary>
		/// Рекламные акции, в которых ролик выходил на отмеченных станциях за период
		/// (RollerStatisticForm.grid_ObjectSelected). При разбивке по менеджерам — только
		/// акции менеджера этой строки.
		/// </summary>
		public static DataTable LoadActions(Filter filter, DataRow roller)
		{
			Entity entity = EntityManager.GetEntity((int)Entities.Action);
			Dictionary<string, object> parameters =
				DataAccessor.PrepareParameters(entity, InterfaceObjects.SimpleJournal, "LoadForRollerStatistic");
			parameters["startDate"] = filter.Start.Date;
			parameters["finishDate"] = filter.Finish.Date;
			parameters["massmediaString"] = MassmediaString(filter);
			parameters[Roller.ParamNames.RollerId] = roller[Roller.ParamNames.RollerId];
			if (filter.SplitByManager)
				parameters[SecurityManager.ParamNames.UserId] = roller[SecurityManager.ParamNames.UserId];

			DataSet ds = (DataSet)DataAccessor.DoAction(parameters);
			return ds.Tables[Constants.TableNames.Data];
		}

		private static string MassmediaString(Filter filter)
		{
			StringBuilder sb = new StringBuilder();
			foreach (int id in filter.MassmediaIds)
				sb.Append(id).Append(',');
			return sb.ToString();
		}

		// ---------- «Назначить предмет рекламы» ----------

		/// <summary>
		/// Почему ролику строки нельзя назначить предмет рекламы (ролик «для всех фирм» или
		/// копия), null — можно. Строка журнала — ActionRollerInStatJournal.
		/// </summary>
		public static string CannotSetAdvertType(PresentationObject roller)
		{
			string message;
			return ((ActionRollerInStatJournal)roller).CanSetAdvertType(out message) ? null : message;
		}

		/// <summary>Назначить ролику строки предмет рекламы (ActionRollerSetAdvertType).</summary>
		public static void SetAdvertType(PresentationObject roller, PresentationObject advertType)
		{
			((ActionRollerInStatJournal)roller).ApplyAdvertTypeChange(advertType.IDs[0], advertType.Name);
		}
	}
}
