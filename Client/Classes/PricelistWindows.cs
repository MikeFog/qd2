using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// Операции прайс-листа над его рекламными окнами — без UI, для веба (десктопные
	/// аналоги — ветки MassmediaPricelist.WinForms.DoAction и контекстное меню строки
	/// времени в TariffWindowGrid). Публичный фасад: MassmediaPricelist internal.
	///
	/// Долгие операции (генерация, удаление за период) отдаются вызывающему порциями —
	/// тот показывает прогресс и может остановиться между ними, как ProgressForm десктопа.
	/// Порции — по неделе и без перекрытий. Десктоп режет генерацию неделями, удаление —
	/// днями, но с перекрытием границ (последний день порции идёт первым днём следующей),
	/// а интервал из одного дня у него не обрабатывается вовсе (цикл while start &lt; finish).
	/// </summary>
	public static class PricelistWindows
	{
		/// <summary>Имена действий прайс-листа (iEntityAction, сущность 80) — работа с окнами.</summary>
		public static class ActionNames
		{
			public const string GenerateWindows = "GenerateWindows";
			public const string DeleteGeneratedWindows = "DeleteGeneratedWindows";
			public const string DisabledTariffWindows = "DisabledTariffWindows";
			public const string EnabledTariffWindows = "EnabledTariffWindows";
			public const string ShowDisabledWindows = "ShowDisabledWindows";
			public const string MarkWindows = "MarkWindows";
			public const string UnmarkWindows = "UnmarkWindows";

			public static readonly string[] All =
			{
				GenerateWindows, DeleteGeneratedWindows, DisabledTariffWindows, EnabledTariffWindows,
				ShowDisabledWindows, MarkWindows, UnmarkWindows
			};
		}

		/// <summary>
		/// Копия прайс-листа, у которой дочерняя сущность — рекламные окна. Действия выше
		/// MassmediaPricelist.IsActionEnabled разрешает только в таком виде (дерево «Генерация
		/// рекламных окон» десктопа); узел дерева «Рекламные тарифы» — с тарифами, и трогать
		/// его не нужно: копия несёт те же параметры.
		/// </summary>
		public static PresentationObject ForWindows(object pricelist)
		{
			MassmediaPricelist source = (MassmediaPricelist)pricelist;
			MassmediaPricelist copy = new MassmediaPricelist { Parameters = source.Parameters };
			copy.ChildEntity = EntityManager.GetEntity((int)Entities.TariffWindow);
			return copy;
		}

		public static DateTime StartDate(object pricelist) => ((Pricelist)pricelist).StartDate.Date;
		public static DateTime FinishDate(object pricelist) => ((Pricelist)pricelist).FinishDate.Date;

		/// <summary>Интервал внутри срока прайс-листа и не пустой; текст ошибки или null.</summary>
		public static string ValidatePeriod(object pricelist, DateTime start, DateTime finish)
		{
			if (start > finish)
				return MessageAccessor.GetMessage("StartFinishWindowTimeError");
			if (start < StartDate(pricelist) || finish > FinishDate(pricelist))
				return Tr.Format("Интервал должен быть внутри срока прайс-листа: {0:dd.MM.yyyy} – {1:dd.MM.yyyy}.",
					StartDate(pricelist), FinishDate(pricelist));
			return null;
		}

		/// <summary>Интервал по неделям, границы включительно, без перекрытий.</summary>
		public static IReadOnlyList<Tuple<DateTime, DateTime>> Weeks(DateTime start, DateTime finish)
		{
			List<Tuple<DateTime, DateTime>> weeks = new List<Tuple<DateTime, DateTime>>();
			for (DateTime from = start.Date; from <= finish.Date; from = from.AddDays(7))
			{
				DateTime to = from.AddDays(6);
				weeks.Add(Tuple.Create(from, to > finish.Date ? finish.Date : to));
			}
			return weeks;
		}

		// ---------- Генерация ----------

		/// <summary>
		/// Порция генерации — GenerateTariffWindows (ключ TariffWindow/Generate, модуль 0, в
		/// транзакции), как MassmediaPricelist.WinForms.GenerateTariffWindows. Уже
		/// сгенерированные окна процедура не дублирует.
		/// </summary>
		public static void Generate(object pricelist, DateTime start, DateTime finish)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				EntityManager.GetEntity((int)Entities.TariffWindow), InterfaceObjects.FakeModule, Constants.Actions.Generate);
			procParameters.Add(Pricelist.ParamNames.PricelistId, ((Pricelist)pricelist).PricelistId);
			procParameters.Add(Pricelist.ParamNames.StartDate, start);
			procParameters.Add(Pricelist.ParamNames.FinishDate, finish);
			DataAccessor.DoAction(procParameters);
		}

		/// <summary>
		/// После генерации всего интервала — проверка склеенных окон (CheckLinkedWindows) и
		/// перечитывание прайс-листа: в его имени срок сгенерированных окон.
		/// </summary>
		public static void AfterGenerate(object pricelist, DateTime start, DateTime finish)
		{
			MassmediaPricelist p = (MassmediaPricelist)pricelist;
			p.CheckLinkedWindows(start, finish);
			p.Refresh();
		}

		// ---------- Удаление сгенерированных окон ----------

		/// <summary>
		/// Порция удаления — TariffWindowMassDelete: окна прайс-листа за интервал, у которых
		/// нет выпусков. <paramref name="time"/> — только окна этого тарифного времени (строка
		/// сетки), null — все.
		/// </summary>
		public static void DeleteGenerated(object pricelist, DateTime start, DateTime finish, TimeSpan? time)
		{
			MassmediaPricelist p = (MassmediaPricelist)pricelist;
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters.Add(Pricelist.ParamNames.PricelistId, p.PricelistId);
			procParameters.Add(Pricelist.ParamNames.StartDate, start);
			procParameters.Add(Pricelist.ParamNames.FinishDate, finish);
			procParameters.Add(Massmedia.ParamNames.MassmediaId, p.MassmediaId);
			if (time.HasValue)
				procParameters.Add("time", string.Format("{0}:{1}", time.Value.Hours, time.Value.Minutes));
			DataAccessor.ExecuteNonQuery("TariffWindowMassDelete", procParameters);
		}

		/// <summary>После удаления — перечитать прайс-лист (срок сгенерированных окон в имени).</summary>
		public static void Refresh(object pricelist)
		{
			((PresentationObject)pricelist).Refresh();
		}

		// ---------- Запрет внесения и пометка окон «по шаблону» ----------

		/// <summary>Именованный паспорт формы TariffWindowsDisabledStatusForm.</summary>
		public const string StatusChangePassport = "TariffWindowsStatusChange";

		/// <summary>Черновик паспорта: интервал — срок прайс-листа (как в десктопе), все дни отмечены.</summary>
		public static PresentationObject CreateStatusChangeDraft(object pricelist)
		{
			PresentationObject draft = EntityManager.GetEntity((int)Entities.TariffWindow).NewObject;
			draft["time"] = new DateTime(1900, 1, 1);
			draft["startDate"] = StartDate(pricelist);
			draft["endDate"] = FinishDate(pricelist);
			foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
				draft[DayParam(day)] = true;
			return draft;
		}

		public static string ValidateStatusChange(object pricelist, Dictionary<string, object> values)
		{
			string error = ValidatePeriod(pricelist,
				Convert.ToDateTime(values["startDate"]), Convert.ToDateTime(values["endDate"]));
			if (error != null)
				return error;

			foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
				if (values.TryGetValue(DayParam(day), out object v) && v is bool b && b)
					return null;
			return Tr.T("Отметьте хотя бы один день недели.");
		}

		/// <summary>
		/// Запретить/разрешить внесение (sp_ChangeTariffWindowDisabledStatus) или пометить/снять
		/// пометку (sp_ChangeTariffWindowMarkedStatus) окнам этого времени в интервале по дням
		/// недели — дословно TariffWindowsDisabledStatusForm.ApplyChanges.
		/// </summary>
		public static void ChangeStatus(object pricelist, string actionName, Dictionary<string, object> values)
		{
			string procedure;
			bool flag;
			switch (actionName)
			{
				case ActionNames.DisabledTariffWindows: procedure = "sp_ChangeTariffWindowDisabledStatus"; flag = true; break;
				case ActionNames.EnabledTariffWindows: procedure = "sp_ChangeTariffWindowDisabledStatus"; flag = false; break;
				case ActionNames.MarkWindows: procedure = "sp_ChangeTariffWindowMarkedStatus"; flag = true; break;
				case ActionNames.UnmarkWindows: procedure = "sp_ChangeTariffWindowMarkedStatus"; flag = false; break;
				default: throw new ArgumentOutOfRangeException(nameof(actionName), actionName, null);
			}

			DateTime time = Convert.ToDateTime(values["time"]);
			Dictionary<string, object> parameters = new Dictionary<string, object>
			{
				["startDate"] = Convert.ToDateTime(values["startDate"]),
				["endDate"] = Convert.ToDateTime(values["endDate"]),
				["time"] = new DateTime(1900, 1, 1, time.Hour, time.Minute, 0),
				["flag"] = flag,
				["priceListID"] = ((Pricelist)pricelist).PricelistId,
			};
			foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
				parameters[DayParam(day)] = values.TryGetValue(DayParam(day), out object v) && v is bool b && b;

			DataAccessor.ExecuteNonQuery(procedure, parameters);
		}

		private static string DayParam(DayOfWeek day) => day.ToString("g").ToLower();

		// ---------- Заблокированные окна ----------

		/// <summary>
		/// Недоступные для внесения окна прайс-листа за период — ShowDisabledWindows, как
		/// MassmediaPricelist.WinForms.ShowDisabledWindows. Добавлена колонка с длительностью
		/// в виде «мм:сс».
		/// </summary>
		public static DataTable DisabledWindows(object pricelist, DateTime start, DateTime finish)
		{
			DataTable table = DataAccessor.LoadDataSet("ShowDisabledWindows", new Dictionary<string, object>
			{
				{ "priceListID", ((Pricelist)pricelist).PricelistId },
				{ "startDate", start },
				{ "finishDate", finish }
			}).Tables[0];

			table.Columns.Add("durationString", typeof(string));
			foreach (DataRow row in table.Rows)
				row["durationString"] = row[TariffWindow.ParamNames.Duration] == DBNull.Value
					? string.Empty
					: DateTimeUtils.Time2String(Convert.ToInt32(row[TariffWindow.ParamNames.Duration]));
			return table;
		}
	}
}
