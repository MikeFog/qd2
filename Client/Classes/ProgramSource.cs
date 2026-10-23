using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using log4net;

namespace Merlin.Classes
{
	/// <summary>
	/// Полоса «Программы» вкладки «Спонсорство» страницы акции (docs/tasks/web-action-forms.md, этап 7) — то, что в десктопе
	/// делает ProgramIssuesGrid2 в режиме программ CampaignForm: выпуски спонсорских программ одной спонсорской кампании за
	/// неделю. Строка — тариф программы («Программа | Цена | Время»), колонка — день, место — выпуск программы в этот день во
	/// время тарифа. Бонусные ролики той же кампании — сетка окон WindowsSource (OpenCampaign.IsSponsor).
	///
	/// Чтение: действующие программы станции кампании (SponsorPrograms @activeOnly = 1), их прайс-листы, пересекающие
	/// неделю, — SponsorPricelists, а не SponsorPricelistByDate (тот берёт один прайс-лист по дате, а без него отдаёт
	/// ближайший будущий, и десктоп обрезал неделю одним прайс-листом; здесь на стыке прайс-листов видны дни обоих, В-28),
	/// тарифы прайс-листов (sponsorTariffList) и выпуски программ станции за неделю одним вызовом (ProgramIssues @massmediaID
	/// с макетами; десктопное «только подтверждённые» до процедуры не доходило, Д-29). Место есть, только если день в сроке
	/// прайс-листа тарифа и отмечен у тарифа. Надпись места — по старшинству (В-31): свой выпуск, подтверждённый чужой,
	/// чужие макеты (только при «Учитывать макеты»).
	///
	/// Запись — щелчок (<see cref="Toggle"/>): ProgramIssueIUD AddItem или DeleteItem и пересчёт акции одной транзакцией, как
	/// ProgramIssuesGrid2.UpdateDB; «Отменить последнее» — <see cref="Undo"/>. Права — «Редактировать» акции (В-7) и
	/// «Редактировать выпуски программ для спонсоров» (86/EditIssues, 151), как кнопка ActionForm. Место берётся только из
	/// модели недели: программа — станции кампании (ProgramIssueIUD станцию не проверяет, Д-30), тариф — своего
	/// прайс-листа; срок прайс-листа и цену выпуска проверяет процедура (Deploy/68, В-34).
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public static class ProgramSource
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ProgramSource));

		/// <summary>Спонсорская кампания — её программы и бонусные ролики показывает вкладка «Спонсорство».</summary>
		public static bool IsSponsor(DataRow campaign) =>
			ParseHelper.GetInt32FromObject(campaign[Campaign.ParamNames.CampaignTypeId], 0) == (int)Campaign.CampaignTypes.Sponsor;

		// ---------- Кампания ----------

		/// <summary>
		/// Спонсорская кампания вкладки — для полосы программ. Кисть и окна бонусных роликов — у WindowsSource.OpenCampaign
		/// той же кампании.
		/// </summary>
		public sealed class ProgramScope
		{
			internal ProgramScope() { }

			public int CampaignId { get; internal set; }
			public int ActionId { get; internal set; }
			public int MassmediaId { get; internal set; }

			/// <summary>Можно ли ставить и снимать программы: «Редактировать» акции и право на выпуски программ (151).</summary>
			public bool CanEdit { get; internal set; }

			/// <summary>День первого выпуска программы; null — выпусков нет.</summary>
			public DateTime? FirstIssueDate { get; internal set; }

			private readonly Dictionary<int, int> _rowNumbers = new Dictionary<int, int>();

			/// <summary>
			/// Номер строки тарифа — выдаётся раз за жизнь набора: ключ места (день × строка) не сдвигается, когда на
			/// другой неделе строк больше или меньше.
			/// </summary>
			internal int RowNumber(int tariffId)
			{
				if (!_rowNumbers.TryGetValue(tariffId, out int number))
					_rowNumbers[tariffId] = number = _rowNumbers.Count + 1;
				return number;
			}
		}

		/// <summary>Спонсорская кампания этой акции; null — нет такой (удалена, другая акция, не спонсорская).</summary>
		public static ProgramScope Open(ActionOnMassmedia action, int campaignId)
		{
			DataRow row = SponsorCampaignRow(action, campaignId);
			if (row == null)
				return null;

			DateTime? first = null;
			foreach (DataRow issue in CampaignIssues(campaignId).Rows)
			{
				DateTime day = IssueDateOf(issue).Date;
				if (first == null || day < first)
					first = day;
			}

			return new ProgramScope
			{
				CampaignId = campaignId,
				ActionId = action.ActionId,
				MassmediaId = ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.MassmediaId], 0),
				CanEdit = CanEdit(action),
				FirstIssueDate = first,
			};
		}

		/// <summary>
		/// Править программы — «Редактировать» акции (права группы и правило своих / чужих акций, В-7) и право сущности
		/// «Часть программ спонсорской кампании» на выпуски программ, как ActionForm («Редактировать выходы спонсорских
		/// программ»): у спонсорской кампании (93) своего «Редактировать» нет.
		/// </summary>
		private static bool CanEdit(ActionOnMassmedia action) =>
			ActionWorkspace.CanEdit(action)
			&& EntityManager.GetEntity((int)Entities.ProgramPart).IsActionEnabled(ProgramPartOfSponsorCampaign.ActionNames.EditIssues, ViewType.Journal);

		/// <summary>
		/// Право на размещение спонсорской кампании — на выпуски программ (86/EditIssues, 151) или на бонусные ролики
		/// (87/EditIssues, 481), как кнопки ActionForm: по нему виден вход «Редактировать» у строки спонсорской кампании (В-37).
		/// </summary>
		public static bool HasPlacementRight() =>
			EntityManager.GetEntity((int)Entities.ProgramPart).IsActionEnabled(ProgramPartOfSponsorCampaign.ActionNames.EditIssues, ViewType.Journal)
			|| EntityManager.GetEntity((int)Entities.RollerPart).IsActionEnabled(RollerPartOfSponsorCampaign.ActionNames.EditIssues, ViewType.Journal);

		/// <summary>Строка спонсорской кампании акции по свежему списку (Campaigns @actionID); null — такой нет.</summary>
		private static DataRow SponsorCampaignRow(ActionOnMassmedia action, int campaignId)
		{
			foreach (DataRow row in ActionWorkspace.LoadCampaigns(action).Rows)
				if (IsSponsor(row) && WindowsSource.CampaignIdOf(row) == campaignId)
					return row;
			return null;
		}

		/// <summary>
		/// Свежая акция для записи: удалённая — отказ; без права (на момент открытия и на сейчас) — отказ; кампании в акции
		/// больше нет — отказ.
		/// </summary>
		internal static ActionOnMassmedia LoadForWrite(ProgramScope s)
		{
			ActionOnMassmedia action = ActionWorkspace.Load(s.ActionId);
			if (action == null || ActionWorkspace.IsDeleted(action))
				throw new InvalidOperationException(Tr.T("Рекламная акция удалена. Обновите страницу."));
			if (!s.CanEdit || !CanEdit(action))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));
			if (SponsorCampaignRow(action, s.CampaignId) == null)
				throw new InvalidOperationException(Tr.T("Рекламная кампания удалена. Обновите страницу."));
			return action;
		}

		// ---------- Неделя ----------

		/// <summary>Тариф программы за день — то, что нужно записи и панели (у PlacementCell — internal).</summary>
		internal sealed class ProgramDay
		{
			internal int CampaignId { get; set; }
			internal int ProgramId { get; set; }
			internal string ProgramName { get; set; }
			internal int PricelistId { get; set; }

			/// <summary>Бонус прайс-листа, секунды: столько времени бонусных роликов даёт выпуск.</summary>
			internal int Bonus { get; set; }

			internal int TariffId { get; set; }
			internal decimal Price { get; set; }

			/// <summary>Свой выпуск в месте на момент чтения недели; null — своего нет.</summary>
			internal int? OwnIssueId { get; set; }
		}

		/// <summary>
		/// Неделя с понедельника недели <paramref name="anyDate"/>, все семь дней (у программ свои прайс-листы, срок прайс-листа
		/// станции полосу не обрезает). Строки — тарифы действующих программ станции по прайс-листам, срок которых
		/// пересекает неделю: по программам (как SponsorPrograms — по имени), внутри — по началу прайс-листа и времени
		/// тарифа. <paramref name="showUnconfirmed"/> — «Учитывать макеты»: без неё чужие макеты не видны нигде.
		/// Итог дня — свои выпуски программ.
		/// </summary>
		public static PlacementWeek LoadWeek(ProgramScope s, DateTime anyDate, bool showUnconfirmed)
		{
			DateTime monday = anyDate.Date.AddDays(-(((int)anyDate.DayOfWeek + 6) % 7));
			DateTime sunday = monday.AddDays(PlacementWeek.DaysInWeek - 1);
			PlacementWeek week = new PlacementWeek
			{
				FirstDay = monday,
				StartDate = monday,
				FinishDate = sunday,
				HasPrices = false,
				HasPricelist = false,
				HeaderColumns = new[] { Tr.T("Программа"), Tr.T("Цена"), Tr.T("Время") },
				EmptyTitle = Tr.T("На эту неделю у радиостанции нет спонсорских программ."),
				EmptyHint = Tr.T("Программы и их прайс-листы ведутся в справочнике радиостанции."),
			};

			Dictionary<(int TariffId, DateTime Day), List<DataRow>> slots = new Dictionary<(int, DateTime), List<DataRow>>();
			foreach (DataRow issue in StationIssues(s.MassmediaId, monday, sunday).Rows)
			{
				(int, DateTime) key = (Convert.ToInt32(issue[Issue.ParamNames.TariffId]), IssueDateOf(issue).Date);
				if (!slots.TryGetValue(key, out List<DataRow> list))
					slots[key] = list = new List<DataRow>();
				list.Add(issue);
			}

			List<PlacementRow> rows = new List<PlacementRow>();
			Massmedia massmedia = Massmedia.GetMassmediaByID(s.MassmediaId);
			foreach (DataRow program in massmedia.GetSponsorPrograms(true).Rows)
			{
				int programId = Convert.ToInt32(program[SponsorProgram.ParamNames.SponsorProgramId]);
				string programName = Convert.ToString(program[Constants.Parameters.Name]).Trim();
				foreach (DataRow pricelistRow in ProgramPricelists(programId)
					.Where(p => StartOf(p) <= sunday && FinishOf(p) >= monday)
					.OrderBy(StartOf))
				{
					SponsorPricelist pricelist = new SponsorPricelist(pricelistRow);
					DateTime start = StartOf(pricelistRow), finish = FinishOf(pricelistRow);
					foreach (DataRow tariff in pricelist.GetTariffList().Rows)
					{
						int tariffId = Convert.ToInt32(tariff[Tariff.ParamNames.TariffId]);
						decimal price = Convert.ToDecimal(tariff[Tariff.ParamNames.Price]);
						TimeSpan time = Convert.ToDateTime(tariff[Tariff.ParamNames.Time]).TimeOfDay;
						PlacementRow row = new PlacementRow(new[] { programName, price.ToString("C"), DateTimeUtils.Time2String(time.Hours, time.Minutes) },
							PlacementWeek.DaysInWeek);
						for (int d = 0; d < PlacementWeek.DaysInWeek; d++)
						{
							DateTime day = monday.AddDays(d);
							if (day < start || day > finish || !IsTariffDay(tariff, day))
								continue;
							slots.TryGetValue((tariffId, day), out List<DataRow> issues);
							row.Cells[d] = Cell(s, week, d, day + time, issues, showUnconfirmed, new ProgramDay
							{
								CampaignId = s.CampaignId,
								ProgramId = programId,
								ProgramName = programName,
								PricelistId = pricelist.PricelistId,
								Bonus = pricelist.Bonus,
								TariffId = tariffId,
								Price = price,
							});
						}
						rows.Add(row);
					}
				}
			}
			week.Rows = rows;

			for (int d = 0; d < PlacementWeek.DaysInWeek; d++)
				week.DayTotals[d] = slots.Where(x => x.Key.Day == monday.AddDays(d))
					.Sum(x => x.Value.Count(i => CampaignIdOf(i) == s.CampaignId));
			return week;
		}

		private static readonly DateTime KeyEpoch = new DateTime(2000, 1, 1);

		/// <summary>Ключ места: номер дня от 2000-01-01 × 100 000 + номер строки тарифа, как у модулей.</summary>
		private static int CellKey(DateTime day, int rowNumber) => (day - KeyEpoch).Days * 100000 + rowNumber;

		/// <summary>
		/// Место тарифа за день и его надпись по старшинству (В-31): «✓» — свой выпуск (макет или подтверждённый), иначе имя
		/// фирмы подтверждённого чужого, иначе чужие макеты («Макет: фирма» или «Макеты: N», <see cref="PlacementFlags.Tentative"/>
		/// — курсив); при «✓» или фирме и чужих макетах — приписка «+N макет(а, ов)» (<see cref="PlacementCell.Note"/>).
		/// Подсказка — все выпуски слота: «Спонсирует: …» (подтверждённый), «Макет: …». Без «Учитывать макеты» чужих макетов
		/// нет ни в надписи, ни в подсказке. Второго подтверждённого в слот не ставит отказ ProgramIssueIUD
		/// UIX_ProgramIssue_program_issueDate — это проверка без блокировки, а не ограничение базы: при одновременной постановке
		/// слот может получить двоих (на данных — ни разу), надпись покажет первого. За всю историю в слоте — до трёх выпусков.
		/// </summary>
		private static PlacementCell Cell(ProgramScope s, PlacementWeek week, int dayIndex, DateTime moment, List<DataRow> issues,
			bool showUnconfirmed, ProgramDay programDay)
		{
			List<DataRow> slot = issues ?? new List<DataRow>();
			DataRow own = slot.FirstOrDefault(i => CampaignIdOf(i) == s.CampaignId);
			List<DataRow> confirmed = slot.Where(i => CampaignIdOf(i) != s.CampaignId && IsConfirmed(i)).ToList();
			List<DataRow> tentative = showUnconfirmed ? slot.Where(i => CampaignIdOf(i) != s.CampaignId && !IsConfirmed(i)).ToList() : new List<DataRow>();
			programDay.OwnIssueId = own == null ? (int?)null : Convert.ToInt32(own[Issue.ParamNames.IssueId]);

			PlacementCell cell = new PlacementCell
			{
				Key = CellKey(moment.Date, s.RowNumber(programDay.TariffId)),
				Date = moment,
				DayIndex = dayIndex,
				ProgramDay = programDay,
			};
			if (own != null || confirmed.Count > 0)
			{
				cell.Text = own != null ? "✓" : FirmOf(confirmed[0]); // i18n-ok: значок
				cell.Flags = own != null ? PlacementFlags.Mine : PlacementFlags.None;
				cell.Note = tentative.Count > 0 ? TentativeNote(tentative.Count) : null;
			}
			else if (tentative.Count == 1)
			{
				cell.Text = Tr.Format("Макет: {0}", FirmOf(tentative[0]));
				cell.Flags = PlacementFlags.Tentative;
			}
			else if (tentative.Count > 1)
			{
				cell.Text = Tr.Format("Макеты: {0}", tentative.Count);
				cell.Flags = PlacementFlags.Tentative;
			}
			else
				cell.Text = string.Empty;

			List<string> details = new List<string>();
			foreach (DataRow i in slot.Where(i => i == own || IsConfirmed(i) || tentative.Contains(i)))
				details.Add(IsConfirmed(i) ? Tr.Format("Спонсирует: {0}", FirmOf(i)) : Tr.Format("Макет: {0}", FirmOf(i)));
			cell.Details = details.Count > 0 ? string.Join(Environment.NewLine, details) : null;
			return cell;
		}

		/// <summary>«+1 макет», «+2 макета», «+5 макетов» — по русскому правилу числа.</summary>
		private static string TentativeNote(int count)
		{
			int mod10 = count % 10, mod100 = count % 100;
			if (mod10 == 1 && mod100 != 11)
				return Tr.Format("+{0} макет", count);
			if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14))
				return Tr.Format("+{0} макета", count);
			return Tr.Format("+{0} макетов", count);
		}

		/// <summary>Отмечен ли у тарифа день недели <paramref name="day"/>.</summary>
		private static bool IsTariffDay(DataRow tariff, DateTime day)
		{
			string column;
			switch (day.DayOfWeek)
			{
				case DayOfWeek.Monday: column = Tariff.ParamNames.Monday; break;
				case DayOfWeek.Tuesday: column = Tariff.ParamNames.Tuesday; break;
				case DayOfWeek.Wednesday: column = Tariff.ParamNames.Wednesday; break;
				case DayOfWeek.Thursday: column = Tariff.ParamNames.Thursday; break;
				case DayOfWeek.Friday: column = Tariff.ParamNames.Friday; break;
				case DayOfWeek.Saturday: column = Tariff.ParamNames.Saturday; break;
				default: column = Tariff.ParamNames.Sunday; break;
			}
			return ParseHelper.GetBooleanFromObject(tariff[column], false);
		}

		/// <summary>Прайс-листы программы (SponsorPricelists @sponsorProgramID) — все, отбор по сроку — у вызывающего.</summary>
		private static IEnumerable<DataRow> ProgramPricelists(int programId)
		{
			Dictionary<string, object> parameters = DataAccessor.PrepareParameters(EntityManager.GetEntity((int)Entities.SponsorPricelist));
			parameters[SponsorProgram.ParamNames.SponsorProgramId] = programId;
			return ((DataSet)DataAccessor.DoAction(parameters)).Tables[Constants.TableNames.Data].Rows.Cast<DataRow>();
		}

		/// <summary>Срок прайс-листа программы — дни, без времени (startDate…finishDate включительно, как в ProgramIssueIUD).</summary>
		private static DateTime StartOf(DataRow pricelist) => Convert.ToDateTime(pricelist[Pricelist.ParamNames.StartDate]).Date;

		private static DateTime FinishOf(DataRow pricelist) => Convert.ToDateTime(pricelist[Pricelist.ParamNames.FinishDate]).Date;

		/// <summary>Выпуски программ станции за дни <paramref name="start"/>…<paramref name="finish"/>, с макетами, без удалённых акций.</summary>
		private static DataTable StationIssues(int massmediaId, DateTime start, DateTime finish)
		{
			Dictionary<string, object> parameters = DataAccessor.PrepareParameters(ProgramIssue.GetEntity());
			parameters[Massmedia.ParamNames.MassmediaId] = massmediaId;
			parameters[RollerIssue.ParamNames.StartDate] = start;
			parameters[RollerIssue.ParamNames.FinishDate] = finish;
			parameters[ShowUnconfirmedParam] = true;
			return ((DataSet)DataAccessor.DoAction(parameters)).Tables[Constants.TableNames.Data];
		}

		/// <summary>Выпуски программ кампании (ProgramIssues @campaignID).</summary>
		private static DataTable CampaignIssues(int campaignId)
		{
			Dictionary<string, object> parameters = DataAccessor.PrepareParameters(ProgramIssue.GetEntity());
			parameters[Campaign.ParamNames.CampaignId] = campaignId;
			return ((DataSet)DataAccessor.DoAction(parameters)).Tables[Constants.TableNames.Data];
		}

		/// <summary>Строка выпуска программы из базы (ProgramIssues @issueID); null — выпуска нет (или его акция удалена).</summary>
		private static DataRow LoadIssue(int issueId)
		{
			Dictionary<string, object> parameters = DataAccessor.PrepareParameters(ProgramIssue.GetEntity());
			parameters[Issue.ParamNames.IssueId] = issueId;
			DataTable data = ((DataSet)DataAccessor.DoAction(parameters)).Tables[Constants.TableNames.Data];
			return data.Rows.Count > 0 ? data.Rows[0] : null;
		}

		/// <summary>Параметр процедуры ProgramIssues — с опечаткой, как в базе.</summary>
		private const string ShowUnconfirmedParam = "showUncorfirmed";

		private static int CampaignIdOf(DataRow issue) => Convert.ToInt32(issue[Campaign.ParamNames.CampaignId]);

		private static DateTime IssueDateOf(DataRow issue) => Convert.ToDateTime(issue[Issue.ParamNames.IssueDate]);

		private static bool IsConfirmed(DataRow issue) => ParseHelper.GetBooleanFromObject(issue[Action.ParamNames.IsConfirmed], false);

		private static string FirmOf(DataRow issue) => Convert.ToString(issue["firmName"]).Trim();

		// ---------- Щелчок ----------

		/// <summary>Выпуск программы, поставленный щелчком, — номер из ответа ProgramIssueIUD, захваченный в момент записи.</summary>
		public sealed class PlacedProgramIssue
		{
			internal PlacedProgramIssue() { }

			public int IssueId { get; internal set; }
			public int CampaignId { get; internal set; }
			public int ProgramId { get; internal set; }
			public int TariffId { get; internal set; }

			/// <summary>День и время выпуска (ProgramIssue.issueDate).</summary>
			public DateTime Date { get; internal set; }

			/// <summary>Для строки отказа: «дата время — программа».</summary>
			public string Name { get; internal set; }
		}

		/// <summary>Итог щелчка: поставленный выпуск (для «Отменить последнее») или номер снятого.</summary>
		public sealed class ToggleResult
		{
			internal ToggleResult() { }

			/// <summary>Поставленный выпуск; null — щелчок снял выпуск.</summary>
			public PlacedProgramIssue Placed { get; internal set; }

			/// <summary>Снятый выпуск; null — щелчок поставил выпуск.</summary>
			public int? RemovedIssueId { get; internal set; }
		}

		/// <summary>
		/// Щелчок «Режима добавления» по месту полосы — как ProgramIssuesGrid2.UpdateDB: своего выпуска в месте нет — поставить
		/// программу (ProgramIssueIUD AddItem), есть — снять его (DeleteItem), затем пересчёт акции, всё одной транзакцией;
		/// без вопроса и без сообщения о цене, как в десктопе. Что делать, решает модель недели (то, что видел пользователь):
		/// если между чтением и щелчком выпуск в месте поставили или сняли в другом окне, щелчок отказывает «Обновите
		/// страницу» — иначе он сделал бы противоположное увиденному.
		///
		/// Постановка: у подтверждённой акции без предмета рекламы — отказ текстом десктопа (AdvertTypeMastBeSelected) до
		/// записи; у макета предмет необязателен (В-30). Выпуск — в день места во время тарифа, признак подтверждения — как у
		/// акции. Цену и срок прайс-листа берёт и проверяет процедура (Deploy/68); занятое подтверждённым место — её же отказ
		/// UIX_ProgramIssue_program_issueDate. Снятие — свежей строкой выпуска с датой (без даты процедура пропускает запрет
		/// прошлого PastIssue) и без бонуса: бонус прайс-листа тарифа процедура находит сама; снять программу, бонус которой
		/// уже занят роликами, процедура не даёт (ProgramIssueDeleteBonusError).
		///
		/// Отказ по бизнес-правилу (RAISERROR с ключом iMessage) откатывает всё и уходит наверх как есть: текст —
		/// ErrorManager.GetErrorMessage(ex). Неделю и строку кампании после щелчка перечитывает экран.
		/// </summary>
		public static ToggleResult Toggle(ProgramScope s, PlacementCell cell, int? advertTypeId)
		{
			ProgramDay p = cell?.ProgramDay;
			if (p == null || p.CampaignId != s.CampaignId)
				throw new InvalidOperationException(Tr.T("Место не относится к спонсорской кампании вкладки. Обновите страницу."));

			ActionOnMassmedia action = LoadForWrite(s);
			ToggleResult result = new ToggleResult();
			if (p.OwnIssueId.HasValue)
			{
				int issueId = p.OwnIssueId.Value;
				WindowsSource.RunInTransaction(() =>
				{
					DataRow row = LoadIssue(issueId);
					if (row == null || CampaignIdOf(row) != s.CampaignId)
						throw new InvalidOperationException(Tr.T("Выпуска программы уже нет. Обновите страницу."));
					new ProgramIssue(row).Delete(silenceFlag: true);
					action.Recalculate(false);
				});
				result.RemovedIssueId = issueId;
				return result;
			}

			bool confirmed = action.IsConfirmed;
			if (confirmed && advertTypeId == null)
				throw new InvalidOperationException(Tr.T(Properties.Resources.AdvertTypeMastBeSelected));

			ProgramIssue placed = null;
			WindowsSource.RunInTransaction(() =>
			{
				// Свой выпуск в месте уже стоит (поставлен в другом окне после чтения недели) — второй не ставится.
				if (StationIssues(s.MassmediaId, cell.Date.Date, cell.Date.Date).Rows.Cast<DataRow>()
					.Any(i => CampaignIdOf(i) == s.CampaignId && Convert.ToInt32(i[Issue.ParamNames.TariffId]) == p.TariffId))
					throw new InvalidOperationException(Tr.T("Выпуск программы в этом месте уже стоит. Обновите страницу."));
				placed = CampaignRef(s).AddProgramIssue(ProgramRef(p.ProgramId), p.TariffId, cell.Date, p.Price, p.Bonus, confirmed,
					advertTypeId.HasValue ? AdvertTypeRef(advertTypeId.Value) : null);
				action.Recalculate(false);
			});
			result.Placed = new PlacedProgramIssue
			{
				IssueId = Convert.ToInt32(placed[Issue.ParamNames.IssueId]),
				CampaignId = s.CampaignId,
				ProgramId = p.ProgramId,
				TariffId = p.TariffId,
				Date = cell.Date,
				Name = NameOf(cell.Date, p.ProgramName),
			};
			return result;
		}

		private static string NameOf(DateTime date, string programName) => string.Format("{0:dd.MM.yyyy HH:mm} — {1}", date, programName);

		/// <summary>Кампания для записи выпуска — нужен только её номер (Campaign.AddProgramIssue), читать её незачем.</summary>
		private static Campaign CampaignRef(ProgramScope s)
		{
			Campaign campaign = new Campaign();
			campaign[Campaign.ParamNames.CampaignId] = s.CampaignId;
			return campaign;
		}

		/// <summary>Программа и предмет рекламы для AddProgramIssue — нужны только номера (IDs[0]).</summary>
		private static PresentationObject ProgramRef(int programId)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[SponsorProgram.ParamNames.SponsorProgramId] = programId;
			return new PresentationObject(SponsorProgram.GetEntity(), parameters);
		}

		private static PresentationObject AdvertTypeRef(int advertTypeId)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[AdvertType.ParamNames.AdvertTypeId] = advertTypeId;
			return new PresentationObject(EntityManager.GetEntity((int)Entities.AdvertType), parameters);
		}

		// ---------- «Отменить последнее» ----------

		/// <summary>
		/// «Отменить последнее» для программ: снимает выпуски, которые поставили щелчки, одной транзакцией. Каждый выпуск
		/// читается заново: нет — уже снят, другая кампания, тариф или время — такой не трогается; и то и другое — строкой
		/// отказа. Права и запрет прошлого — те же, что у щелчка. После каждого снятия — пересчёт акции: бонус кампании,
		/// с которым процедура сверяет занятое роликами (ProgramIssueDeleteBonusError), ведёт только пересчёт, и без него
		/// второе снятие сверилось бы с бонусом до первого (В-33). Небизнес-ошибка откатывает всё.
		/// </summary>
		public static WindowsSource.BatchResult Undo(ProgramScope s, IReadOnlyList<PlacedProgramIssue> placed)
		{
			if (placed.Any(p => p.CampaignId != s.CampaignId))
				throw new InvalidOperationException(Tr.T("Отменять можно только выпуски этой кампании."));
			ActionOnMassmedia action = LoadForWrite(s);
			WindowsSource.BatchResult result = new WindowsSource.BatchResult();
			using (OperationScope.Start($"ProgramsUndo campaign={s.CampaignId} issues={placed.Count}"))
				WindowsSource.RunInTransaction(() =>
				{
					foreach (PlacedProgramIssue p in placed.OrderBy(x => x.Date).ThenBy(x => x.IssueId))
					{
						DataRow row = LoadIssue(p.IssueId);
						if (row == null)
							result.Refuse(p.Name, Tr.T("Выпуска уже нет."));
						else if (CampaignIdOf(row) != p.CampaignId || Convert.ToInt32(row[Issue.ParamNames.TariffId]) != p.TariffId
							|| IssueDateOf(row) != p.Date)
							result.Refuse(p.Name, Tr.T("Выпуск изменили после размещения: другая программа или время."));
						else if (WindowsSource.Step(result, p.Name, () =>
							{
								new ProgramIssue(row).Delete(silenceFlag: true);
								return null;
							}))
							action.Recalculate(false);
					}
				});
			Log.Info($"Programs undo campaign={s.CampaignId} issues={placed.Count} removed={result.Done} notRemoved={result.Failed}");
			return result;
		}
	}
}
