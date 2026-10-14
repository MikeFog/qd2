using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using log4net;

namespace Merlin.Classes
{
	/// <summary>
	/// Запуск Шаблонов 1–3 на вкладке «Рекламные окна» (docs/tasks/web-action-forms.md, этап 5) — то, что в
	/// десктопе делают FrmGenerator и FrmTemplate/2/3. Здесь — линейная кампания (одна станция); веер — 5.5.
	///
	/// Как устроен запуск (решения этапа 5): окна периода читаются заранее, одним вызовом на прайс-лист;
	/// выбор окон каждого дня — чистый <see cref="TemplatePlanner"/> с теми же правилами, что у десктопа;
	/// запись — по дням, каждый день в своей транзакции (дни независимы, как в десктопе, но блокировки
	/// держатся доли секунды). Бизнес-отказ шага — строка таблицы, запуск идёт дальше; любая другая
	/// ошибка (взаимоблокировка, таймаут) откатывает день и прерывает запуск: выпуски прежних дней остаются.
	/// Пересчёт акции — один, в конце. Права — как у остальных записей вкладки (LoadForWrite).
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public static class TemplateSource
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(TemplateSource));

		/// <summary>Итог запуска: поставленные выпуски (для «Отменить последнее»), счётчики и таблица замечаний.</summary>
		public sealed class TemplateResult
		{
			internal TemplateResult()
			{
				Errors = new DataTable();
				Errors.Columns.Add("name", typeof(string));
				Errors.Columns.Add("description", typeof(string));
			}

			/// <summary>Поставлено выпусков.</summary>
			public int Done { get; internal set; }

			/// <summary>Не поставлено: отказы записи плюс нехватка окон (сколько выпусков не нашло окна).</summary>
			public int NotAdded { get; internal set; }

			/// <summary>Сколько дней периода обработано (при сбое — до дня, на котором запуск прерван).</summary>
			public int Days { get; internal set; }

			public List<WindowsSource.PlacedIssue> Created { get; } = new List<WindowsSource.PlacedIssue>();

			/// <summary>name — день или окно, description — отказ, нехватка окон, прерывание запуска.</summary>
			public DataTable Errors { get; }

			internal void Note(string name, string description, int notAdded = 0)
			{
				Errors.Rows.Add(name, description);
				NotAdded += notAdded;
			}
		}

		/// <summary>
		/// Шаблон 2 («Шаблон для внесения роликов на интервале времени»): ролик кисти с позицией «Позиционирования»
		/// в окна интервала каждого дня периода — «в день» или отдельно в прайм и офф-прайм, самые свободные
		/// окна первыми (десктоп — FrmGenerator.AddSimpleIssues).
		/// </summary>
		public static TemplateResult RunInterval(WindowsSource.OpenCampaign c, PlacementTemplate template, int rollerId, int position)
		{
			Campaign campaign = WindowsSource.LoadForWrite(c);
			Roller roller = WindowsSource.BrushRoller(c, rollerId, campaign.Action.FirmID);
			List<DateTime> dates = template.Dates();
			TemplateResult result = new TemplateResult();
			if (dates.Count == 0)
				return result;

			using (OperationScope.Start($"TemplateInterval campaign={c.CampaignId} days={dates.Count}"))
			{
				Stopwatch sw = Stopwatch.StartNew();
				Random rnd = new Random();
				Period period = LoadPeriod(c, dates, template);
				int startMinute = (int)template.StartTime.TotalMinutes;
				int finishMinute = (int)template.FinishTime.TotalMinutes;

				foreach (DateTime day in dates)
				{
					string dayText = day.ToString("dd.MM.yyyy");
					if (!period.Rows.TryGetValue(day, out List<DataRow> rows))
					{
						// День вне прайс-листов станции (GetPriceList дал ближайший будущий или ничего).
						result.Note(dayText, MessageAccessor.GetMessage("PriceListDoesntExist"), WantedPerDay(template));
						continue;
					}

					// Те же отборы, что у десктопа: интервал (у линейки конец включается), занятая подтверждённым выпуском
					// позиция (макеты не смотрятся), ролики этой фирмы в окне (с макетами, любой акции фирмы).
					List<TemplateWindow> candidates = TemplatePlanner.InTimeRange(
							rows.Select(r => ToWindow(r, day, period)), startMinute, finishMinute, finishInclusive: true)
						.Where(w => !(position != (int)RollerPositions.Undefined && PositionOccupied(period.RowOf(w.Key), (RollerPositions)position))
							&& !(template.IgnoreFirmWindows && period.FirmWindows.Contains(w.Key)))
						.ToList();

					List<TemplatePick> picks = TemplatePlanner.PickDay(candidates, template.Quantity, template.QuantityPrime, template.QuantityOffPrime, rnd);
					foreach (TemplatePick pick in picks.Where(p => p.IsShort))
						result.Note(dayText, ShortNote(pick), pick.Wanted - pick.Windows.Count);

					// По возрастанию номера окна — один порядок блокировок у всех пачек.
					List<TemplateWindow> toWrite = picks.SelectMany(p => p.Windows).OrderBy(w => w.Key).ToList();
					if (toWrite.Count == 0)
					{
						result.Days++;
						continue;
					}

					WindowsSource.BatchResult dayResult = new WindowsSource.BatchResult();
					try
					{
						WindowsSource.RunInTransaction(() =>
						{
							foreach (TemplateWindow w in toWrite)
								WindowsSource.Step(dayResult, w.Start.ToString("dd.MM.yyyy HH:mm"), () =>
								{
									TariffWindowWithRollerIssues window = new TariffWindowWithRollerIssues(period.RowOf(w.Key), Entities.TariffWindow);
									dayResult.Created.Add(WindowsSource.Capture(c, campaign.AddIssue(roller, window, (RollerPositions)position, null), w.Key, w.Start, roller));
									return null;
								});
						});
					}
					catch (Exception ex)
					{
						// День откатан целиком; прежние дни остаются. Запуск прерывается — следующие дни, скорее всего,
						// упрутся в то же (сеть, взаимоблокировка, таймаут), и ждать их по таймауту каждый нельзя.
						Log.Error($"Template interval interrupted on {dayText} (campaign {c.CampaignId})", ex);
						result.Note(dayText, Tr.Format("Запуск прерван: {0} Выпуски этого дня не добавлены, добавленные ранее — сохранены.", ErrorManager.GetErrorMessage(ex)),
							toWrite.Count);
						break;
					}

					result.Days++;
					result.Done += dayResult.Done;
					result.NotAdded += dayResult.Failed;
					result.Created.AddRange(dayResult.Created);
					foreach (DataRow refused in dayResult.Errors.Rows)
						result.Errors.Rows.Add(refused["name"], refused["description"]);
				}

				if (result.Done > 0)
				{
					try
					{
						WindowsSource.RunInTransaction(() => campaign.RecalculateAction(false));
					}
					catch (Exception ex)
					{
						Log.Error($"Template interval: recalculation failed (campaign {c.CampaignId})", ex);
						result.Note(Tr.T("Пересчёт акции"), Tr.Format("Пересчёт не выполнен: {0} Выпуски добавлены; цена акции обновится при следующем изменении размещения.", ErrorManager.GetErrorMessage(ex)));
					}
				}

				Log.Info($"Template interval completed in {sw.ElapsedMilliseconds}ms campaign={c.CampaignId} days={result.Days}/{dates.Count} added={result.Done} notAdded={result.NotAdded}");
			}
			return result;
		}

		/// <summary>Сколько выпусков в день просит шаблон.</summary>
		private static int WantedPerDay(PlacementTemplate t) => t.Quantity > 0 ? t.Quantity : t.QuantityPrime + t.QuantityOffPrime;

		/// <summary>Тексты десктопа (FrmGenerator.AddSimpleIssues): «Окон» — сколько окон нашлось, «выпусков» — сколько просили.</summary>
		private static string ShortNote(TemplatePick pick)
		{
			switch (pick.Group)
			{
				case TemplateGroup.Prime:
					return Tr.Format("Недостаточно рекламных окон прайм для размещения всех выпусков. Окон: {0}, выпусков {1}.", pick.Windows.Count, pick.Wanted);
				case TemplateGroup.OffPrime:
					return Tr.Format("Недостаточно рекламных окон офф прайм для размещения всех выпусков. Окон: {0}, выпусков для добавления {1}.", pick.Windows.Count, pick.Wanted);
				default:
					return Tr.Format("Недостаточно рекламных окон для размещения всех выпусков для добавления. Окон: {0}, выпусков {1}.", pick.Windows.Count, pick.Wanted);
			}
		}

		// ---------- Чтение периода ----------

		/// <summary>Окна периода по дням (TariffWindowRetrieve), окна фирмы и максимальные цены дней — всё, что нужно планировщику.</summary>
		private sealed class Period
		{
			/// <summary>День (dayOriginal) → окна дня, как их вернул TariffWindowRetrieve; только дни с прайс-листом.</summary>
			public readonly Dictionary<DateTime, List<DataRow>> Rows = new Dictionary<DateTime, List<DataRow>>();
			public readonly HashSet<int> FirmWindows = new HashSet<int>();
			public readonly Dictionary<DateTime, decimal> MaxPrice = new Dictionary<DateTime, decimal>();
			private readonly Dictionary<int, DataRow> _byId = new Dictionary<int, DataRow>();

			public void Add(DateTime day, DataRow row)
			{
				if (!Rows.TryGetValue(day, out List<DataRow> list))
					Rows[day] = list = new List<DataRow>();
				list.Add(row);
				_byId[Convert.ToInt32(row[TariffWindow.ParamNames.WindowId])] = row;
			}

			public DataRow RowOf(int windowId) => _byId[windowId];
		}

		private static Period LoadPeriod(WindowsSource.OpenCampaign c, List<DateTime> dates, PlacementTemplate template)
		{
			Massmedia massmedia = Massmedia.GetMassmediaByID(c.MassmediaId);
			Period period = new Period();

			// Даты по прайс-листам: GetPriceList даёт покрывающий прайс-лист или ближайший будущий — такой дату не покрывает
			// (окон на неё нет), день уйдёт отказом «На указанную дату отсутствует прайс-лист».
			Dictionary<int, (MassmediaPricelist List, List<DateTime> Days)> byList = new Dictionary<int, (MassmediaPricelist, List<DateTime>)>();
			MassmediaPricelist current = null;
			foreach (DateTime day in dates)
			{
				if (current == null || day < current.StartDate.Date || day > current.FinishDate.Date)
					current = massmedia.GetPriceList(day) as MassmediaPricelist;
				if (current == null || day < current.StartDate.Date || day > current.FinishDate.Date)
					continue;
				if (!byList.TryGetValue(current.PricelistId, out var entry))
					byList[current.PricelistId] = entry = (current, new List<DateTime>());
				entry.Days.Add(day);
			}

			foreach ((MassmediaPricelist list, List<DateTime> days) in byList.Values)
			{
				DateTime first = days[0], last = days[days.Count - 1];

				// Как RollerIssuesGrid3 и десктопный генератор: без особых окон и тарифов «только для модулей», без
				// заблокированных окон; время окна — фактическое.
				list.ExcludeSpecialWindows = true;
				list.ExcludeModuleTariffs = true;
				DataTable windows = list.GetTariffWindows(first, last, null, false, false, useActualTime: true).Tables[Constants.TableNames.Data];
				HashSet<DateTime> wanted = new HashSet<DateTime>(days);
				foreach (DataRow row in windows.Rows)
				{
					DateTime day = ((DateTime)row[TariffWindow.ParamNames.WindowDateOriginal]).Date;
					if (wanted.Contains(day))
						period.Add(day, row);
				}
				foreach (DateTime day in days)
					if (!period.Rows.ContainsKey(day))
						period.Rows[day] = new List<DataRow>();

				// «Ролики этой фирмы в окне»: окна, где у фирмы есть выпуски любой её акции, подтверждённые и макеты, включая эту
				// (десктоп — IsRollerOfTheFirmExist(firm, true) на каждое окно). Процедура Grid отдаёт их набором; месяцами —
				// как просит процедура на длинных периодах.
				if (template.IgnoreFirmWindows)
					for (DateTime from = first; from <= last; from = from.AddDays(31))
					{
						DateTime to = from.AddDays(30) < last ? from.AddDays(30) : last;
						DataSet cells = massmedia.GetRollerCells(list, from, to, null, true, WindowsSource.CampaignRef(c), RollerPositions.Undefined);
						DataTable firm = cells.Tables[Constants.TableNames.WindowsWithThisFirmIssue];
						if (firm != null)
							foreach (DataRow row in firm.Rows)
								period.FirmWindows.Add(ParseHelper.GetInt32FromObject(row[TariffWindow.ParamNames.WindowId], 0));
					}
			}

			// Прайм линейки — окно с максимальной ценой дня по ВСЕМ окнам станции (GetMaxPriceByDay), нужно только при делении на прайм.
			if (template.Quantity == 0 && dates.Count > 0)
				foreach (DataRow row in massmedia.GetMaxPriceByDay(dates[0], dates[dates.Count - 1]).Rows)
				{
					DateTime day = ParseHelper.GetDateTimeFromObject(row["Date"], DateTime.MinValue);
					if (day != DateTime.MinValue)
						period.MaxPrice[day.Date] = ParseHelper.GetDecimalFromObject(row["Price"], 0m);
				}
			return period;
		}

		private static TemplateWindow ToWindow(DataRow row, DateTime day, Period period) => new TemplateWindow
		{
			Key = Convert.ToInt32(row[TariffWindow.ParamNames.WindowId]),
			Start = (DateTime)row[TariffWindow.ParamNames.WindowDateActual],
			// Остаток с неподтверждёнными (TariffWindow.TimeLeftWithUnconfirmed) — независимо от «Учитывать макеты».
			FreeSeconds = WindowsSource.IntOrZero(row[TariffWindow.ParamNames.Duration])
				- WindowsSource.IntOrZero(row[TariffWindow.ParamNames.TimeInUseConfirmed])
				- WindowsSource.IntOrZero(row[TariffWindow.ParamNames.TimeInUseUnconfirmed]),
			IsPrime = period.MaxPrice.TryGetValue(day, out decimal max) && Convert.ToDecimal(row[TariffWindow.ParamNames.Price]) == max,
		};

		/// <summary>Позиция занята подтверждённым выпуском — флаги окна, как FrmGenerator.IsPositionOccupied.</summary>
		private static bool PositionOccupied(DataRow row, RollerPositions position)
		{
			switch (position)
			{
				case RollerPositions.First: return ParseHelper.GetBooleanFromObject(row["isFirstPositionOccupied"], false);
				case RollerPositions.Second: return ParseHelper.GetBooleanFromObject(row["isSecondPositionOccupied"], false);
				case RollerPositions.Last: return ParseHelper.GetBooleanFromObject(row["isLastPositionOccupied"], false);
				default: return false;
			}
		}
	}
}
