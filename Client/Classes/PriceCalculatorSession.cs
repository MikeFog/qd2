using System;
using System.Collections.Generic;
using System.Linq;
using FogSoft.WinForm.Classes;
using log4net;

namespace Merlin.Classes
{
	/// <summary>
	/// Экран «Калькулятор цены» веба без разметки (docs/tasks/web-price-calculator.md, этап К.2): поля панели параметров,
	/// модель цен, строки таблицы и результат; что при каком изменении перечитывается, пересчитывается и сбрасывается —
	/// как в десктопе после К.0 (§4.5):
	/// <list type="bullet">
	/// <item>период, группа, менеджер — модель перечитывается, отметки и позиции строк остаются, выходы и длительность
	/// строк — по шаблону (у десктопа так же);</item>
	/// <item>шаблон выходов, «Ролик (сек.)», дни, режим коэффициента — пересчёт со сбросом выходов и длительности строк;</item>
	/// <item>«Сезонный коэфф.:», «Позиция:», правка строки, отметки — пересчёт, правки строк остаются.</item>
	/// </list>
	/// Что перечитать, решает не то, какое поле изменили, а расхождение: модель не на тот период, группу или менеджера —
	/// перечитать, коэффициент поставлен не для этого менеджера и периода — поставить заново. Поэтому сбой базы посреди
	/// изменения не оставляет расчёт наполовину старым: результат сбрасывается, следующее изменение доделывает недоделанное.
	/// Правка — один запрос к базе (скидки), смена группы — два (модель и скидки), периода или менеджера — ещё коэффициент
	/// менеджера по умолчанию. Сохранённые варианты (этап К.3) — здесь же: список, отметки карточек, редактируемый вариант.
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public sealed class PriceCalculatorSession
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(PriceCalculatorSession));

		private SecurityManager.User _manager;
		private Func<PcRowResult, PcStation, IComparable> _sortKey;
		// коэффициент по умолчанию поставлен для этого менеджера и периода
		private int? _ratioManagerId;
		private DateTime _ratioFrom, _ratioTo;
		// шаблон выходов изменён при неверном периоде — строки по шаблону, когда период исправят
		private bool _rowsToTemplate;
		// отметки и позиции станций, выпавших из модели, и их названия: вернутся, когда станция снова появится (группа 7 → 5 → 7)
		private readonly Dictionary<int, PcRowInput> _absent = new Dictionary<int, PcRowInput>();
		private readonly Dictionary<int, string> _absentNames = new Dictionary<int, string>();
		// «Редактировать», ещё не доведённый до расчёта (база не ответила): доделывают «Повторить» и возврат на экран, а явное
		// изменение параметров его отменяет (CancelRestore) — иначе восстановление легло бы поверх изменения
		private PriceCalculatorVariant _restoring;

		public PriceCalculatorSession(int managerId)
		{
			Template.ManagerId = managerId;
		}

		public PcTemplate Template { get; } = new PcTemplate();
		public PcModel Model { get; private set; }
		/// <summary>Строки таблицы в показанном порядке: от него зависят снимок варианта и Excel (§12).</summary>
		public List<PcRowInput> Rows { get; } = new List<PcRowInput>();
		/// <summary>Расчёт по <see cref="Rows"/> в том же порядке; null — нет (неверный период, сбой базы).</summary>
		public PcResult Result { get; private set; }
		/// <summary>Нижняя граница «Сезонный коэфф.:»: у не-администратора — коэффициент по умолчанию, у администратора — 0.</summary>
		public decimal RatioMinimum { get; private set; }
		/// <summary>
		/// Отмеченные станции, которых нет в модели (нет прайс-листа на период, другая группа), — плашка; строится заново при
		/// перечитывании и снятии всех отметок, «закрыть» плашки её очищает.
		/// </summary>
		public List<string> DroppedStations { get; } = new List<string>();
		/// <summary>Колонка сортировки (её имя задаёт экран) и направление; null — порядок модели (по названию станции).</summary>
		public string SortColumn { get; private set; }
		public bool SortDescending { get; private set; }

		/// <summary>«Сохранённые варианты» в порядке добавления (перезапись оставляет вариант на месте) — и для КП (К.4).</summary>
		public List<PriceCalculatorVariant> Variants { get; } = new List<PriceCalculatorVariant>();
		/// <summary>
		/// Вариант, открытый «Редактировать»; null — нет. Снимают «Сохранить» (перезапись или копия), удаление варианта и выход
		/// из режима; смена параметров — нет (у десктопа режим снимала «Рассчитать», в вебе её нет).
		/// </summary>
		public PriceCalculatorVariant Editing { get; private set; }

		/// <summary>
		/// «Сохранить» доступна: расчёт есть и он по текущим параметрам, есть выбранные дни и отмеченные станции (десктоп —
		/// К-9, К-25: иначе снимок без станций или со старым итогом).
		/// </summary>
		public bool CanSave => Result != null && !PeriodInvalid && ModelFits() && _restoring == null
			&& Result.Rows.Any(r => r.Input.Selected) && SelectedDates().Count > 0;

		/// <summary>Расчёта нет: модель не перечитывается, строки не пересчитываются, экран показывает причину вместо таблицы.</summary>
		public bool PeriodInvalid => !IsValidPeriod(Template.PeriodFrom, Template.PeriodTo);

		/// <summary>
		/// Период, по которому считается: год 1900…9998 (браузер присылает дату на каждой цифре года — 0002, 0020, 0202, 2026, —
		/// такие не уходят ни в базу, ни в перебор дней), начало не позже конца, не длиннее десяти лет, как у шаблонов
		/// (<see cref="PlacementTemplate.MaxPeriodDays"/>).
		/// </summary>
		public static bool IsValidPeriod(DateTime from, DateTime to)
		{
			return from.Year >= 1900 && to.Year >= 1900 && from.Year <= 9998 && to.Year <= 9998
				&& from.Date <= to.Date && (to.Date - from.Date).TotalDays <= PlacementTemplate.MaxPeriodDays;
		}

		/// <summary>Почему по периоду не считается — текст для экрана; null — считается.</summary>
		public string PeriodProblem()
		{
			DateTime from = Template.PeriodFrom.Date, to = Template.PeriodTo.Date;
			if (from.Year < 1900)
				return Tr.T("Дата начала периода слишком ранняя.");
			if (to.Year > 9998)
				return Tr.T("Дата окончания периода слишком большая.");
			if (from > to)
				return Tr.T("Дата начала периода акции не может быть больше даты окончания.");
			if ((to - from).TotalDays > PlacementTemplate.MaxPeriodDays)
				return Tr.T("Период акции слишком длинный: не больше 10 лет.");
			return null;
		}

		public List<DateTime> SelectedDates()
		{
			return PeriodInvalid ? new List<DateTime>() : Template.SelectedDates();
		}

		/// <summary>Первое открытие: коэффициент менеджера по умолчанию, модель, строки по шаблону без отметок, расчёт.</summary>
		public void Start()
		{
			Refresh(true);
		}

		/// <summary>Как десктоп: новый период ставит в «Сезонный коэфф.:» значение менеджера на этот период.</summary>
		public void SetPeriod(DateTime from, DateTime to)
		{
			CancelRestore();
			Template.PeriodFrom = from.Date;
			Template.PeriodTo = to.Date;
			Refresh(true);
		}

		public void SetGroup(int massmediaGroupId)
		{
			CancelRestore();
			Template.MassmediaGroupId = massmediaGroupId;
			Refresh(true);
		}

		public void SetManager(int userId)
		{
			CancelRestore();
			Template.ManagerId = userId;
			Refresh(true);
		}

		/// <summary>
		/// Шаблон выходов, «Ролик (сек.)», дни недели, чётность, режим коэффициента: строки — по шаблону (отметки и позиции
		/// остаются). Если поле по сути не изменилось (прижатое к границе значение то же, повторный щелчок), правки строк
		/// остаются — десктопный NumericUpDown в этом случае тоже молчит.
		/// </summary>
		public void SetTemplate(Action<PcTemplate> change)
		{
			CancelRestore();
			string before = TemplateKey();
			change(Template);
			// без изменения — ничего; но после сбоя (результата нет) то же значение повторяет расчёт
			if (TemplateKey() == before && Result != null)
				return;
			_rowsToTemplate = true;
			Refresh(true);
		}

		/// <summary>«Позиция:» — всем строкам, и выпавшим станциям тоже; остальные правки строк остаются.</summary>
		public void SetPosition(int position)
		{
			CancelRestore();
			if (Template.Position == position && Result != null)
				return;
			Template.Position = position;
			foreach (var row in Rows.Concat(_absent.Values))
				row.Position = position;
			Refresh(true);
		}

		/// <summary>«Сезонный коэфф.:» одним числом, не ниже <see cref="RatioMinimum"/>; правки строк остаются.</summary>
		public void SetRatio(decimal ratio)
		{
			CancelRestore();
			// сброс коэффициента, не доделанный из-за сбоя, — до ручного значения, иначе следующий расчёт его затрёт
			if (!PeriodInvalid && RatioPending())
				ResetRatio();
			decimal value = Math.Max(ratio, RatioMinimum);
			if (Template.SingleRatio == value && Result != null)
				return;
			Template.SingleRatio = value;
			Refresh(true);
		}

		/// <summary>Правка строки — без перестановки строк: строка не уезжает из-под курсора.</summary>
		public void EditRow(int massmediaId, Action<PcRowInput> change)
		{
			var row = Rows.FirstOrDefault(r => r.MassmediaId == massmediaId);
			if (row == null) return;
			change(row);
			Refresh(false);
		}

		public void SetSelected(int massmediaId, bool selected)
		{
			var row = Rows.FirstOrDefault(r => r.MassmediaId == massmediaId);
			if (row == null || row.Selected == selected) return;
			row.Selected = selected;
			Refresh(false);
		}

		/// <summary>«Выбрать все»: снятие снимает и выпавшие станции — вернувшись, они придут без отметки.</summary>
		public void SetAllSelected(bool selected)
		{
			foreach (var row in Rows)
				row.Selected = selected;
			if (!selected)
				foreach (var row in _absent.Values)
					row.Selected = false;
			UpdateDroppedStations();
			Refresh(true);
		}

		/// <summary>
		/// Порядок строк по колонке. Строки переставляются сейчас и после общих изменений (параметры, «Выбрать все»), но не
		/// после правки ячейки и отметки строки.
		/// </summary>
		public void SortBy(string column, bool descending, Func<PcRowResult, PcStation, IComparable> key)
		{
			SortColumn = column;
			SortDescending = descending;
			_sortKey = key;
			ApplySort();
		}

		/// <summary>
		/// «Сохранить», как десктоп (<c>PriceCalculatorForm.SaveVariant</c>): снимок текущего расчёта, строки — в показанном
		/// порядке. <paramref name="replaceEditing"/> — перезаписать редактируемый вариант на его месте (выход из режима, без
		/// проверки на дубль, как десктоп); иначе — новый вариант в конец, если такого ещё нет (сравнение снимков
		/// <see cref="CampaignCalcSnapshot.Equals(object)"/>: первый и последний день и строки по порядку). Копия
		/// редактируемого («Клонировать») снимает режим, только если сохранилась.
		/// </summary>
		public PriceCalculatorSaveOutcome Save(bool replaceEditing)
		{
			if (!CanSave)
				throw new InvalidOperationException("Расчёт не готов к сохранению."); // i18n-ok: экран закрывает «Сохранить» раньше
			var snapshot = PriceCalculatorEngine.BuildSnapshot(Template, Model, Result);
			Log.Info($"PriceCalculator save variant stations={snapshot.Rows.Count} total={snapshot.GrandTotal:0.00} editing={Editing != null} replace={replaceEditing}");

			if (replaceEditing && Editing != null)
			{
				Editing.Update(snapshot, Rows, SortColumn, SortDescending, _sortKey);
				Editing = null;
				return PriceCalculatorSaveOutcome.Replaced;
			}
			if (Variants.Any(v => v.Snapshot.Equals(snapshot)))
				return PriceCalculatorSaveOutcome.Duplicate;
			var variant = new PriceCalculatorVariant();
			variant.Update(snapshot, Rows, SortColumn, SortDescending, _sortKey);
			Variants.Add(variant);
			Editing = null;
			return PriceCalculatorSaveOutcome.Added;
		}

		/// <summary>
		/// «Редактировать», как десктоп (<c>LoadVariant</c>): параметры варианта с введённым периодом (К-26), цены — заново из
		/// базы, коэффициент — сохранённый (К-14), строки — отмеченные станции с правками, остальные по шаблону варианта,
		/// порядок и сортировка — как при сохранении. Станции варианта, которых нет в модели, — в плашку выпавших.
		/// </summary>
		public void Edit(PriceCalculatorVariant variant)
		{
			if (!Variants.Contains(variant))
				return;
			Log.Info($"PriceCalculator edit variant stations={variant.Snapshot.Rows.Count} total={variant.Snapshot.GrandTotal:0.00}");
			Editing = variant;
			_restoring = variant;

			PcTemplate t = PriceCalculatorEngine.TemplateFromSnapshot(variant.Snapshot);
			Template.ManagerId = t.ManagerId;
			Template.MassmediaGroupId = t.MassmediaGroupId;
			Template.PeriodFrom = t.PeriodFrom;
			Template.PeriodTo = t.PeriodTo;
			Template.UseDaysOfWeek = t.UseDaysOfWeek;
			Template.DaysOfWeek = t.DaysOfWeek;
			Template.EvenDays = t.EvenDays;
			Template.Duration = t.Duration;
			Template.PrimeWeekdayPerDay = t.PrimeWeekdayPerDay;
			Template.NonPrimeWeekdayPerDay = t.NonPrimeWeekdayPerDay;
			Template.PrimeWeekendPerDay = t.PrimeWeekendPerDay;
			Template.NonPrimeWeekendPerDay = t.NonPrimeWeekendPerDay;
			Template.Position = t.Position;
			Template.RatioSingle = t.RatioSingle;
			Template.SingleRatio = t.SingleRatio;

			_rowsToTemplate = false;
			// цены — по базе на сейчас, как десктоп (LoadData), даже если модель на те же параметры уже загружена
			Model = null;
			Refresh(false);
		}

		/// <summary>«Удалить» и «Удалить отмеченные»; удалили редактируемый — режим снимается. Отметки остальных остаются (К-16).</summary>
		public void Delete(IEnumerable<PriceCalculatorVariant> variants)
		{
			var list = variants.ToList();
			foreach (var variant in list)
				Variants.Remove(variant);
			if (Editing != null && !Variants.Contains(Editing))
				Editing = null;
			Log.Info($"PriceCalculator delete variants count={list.Count} left={Variants.Count}");
		}

		/// <summary>Выйти из режима редактирования: расчёт остаётся, «Сохранить» добавит его новым вариантом.</summary>
		public void FinishEditing()
		{
			Editing = null;
		}

		/// <summary>
		/// Довести расчёт до параметров: коэффициент по умолчанию, если сменились менеджер или период; модель, если она не на
		/// эти период, группу и менеджера; строки по шаблону, если шаблон менялся при неверном периоде; затем расчёт. При
		/// неверном периоде и при сбое результат сбрасывается (исключение уходит экрану).
		/// </summary>
		private void Refresh(bool resort)
		{
			try
			{
				if (PeriodInvalid)
				{
					Result = null;
					return;
				}
				if (RatioPending())
				{
					ResetRatio();
					// К-14: коэффициент варианта — после коэффициента менеджера по умолчанию, иначе тот его затрёт
					if (_restoring != null)
						Template.SingleRatio = _restoring.Snapshot.ManagerDiscountValue;
				}
				if (!ModelFits())
					Reload();
				else if (_rowsToTemplate)
					ResetRowsToTemplate();
				_rowsToTemplate = false;

				// строки варианта — заново из снимка и модели при каждом повторе, вариант снимается только после удачного расчёта:
				// иначе повтор после сбоя скидок пересортировал бы строки, и порядок варианта потерялся бы
				bool restoring = _restoring != null;
				if (restoring)
					RestoreRows(_restoring);

				Result = PriceCalculatorEngine.Calculate(Model, Rows, SelectedDates(), Template.RatioRule(Model), PriceCalculatorData.Discounts);
				// у восстановленного варианта — порядок строк, как при сохранении
				if (restoring)
					_restoring = null;
				else if (resort)
					ApplySort();
			}
			catch
			{
				Result = null;
				throw;
			}
		}

		private bool RatioPending()
		{
			return _ratioManagerId != Template.ManagerId || _ratioFrom != Template.PeriodFrom.Date || _ratioTo != Template.PeriodTo.Date;
		}

		private bool ModelFits()
		{
			return Model != null && Model.DateFrom == Template.PeriodFrom.Date && Model.DateTo == Template.PeriodTo.Date
				&& Model.MassmediaGroupId == Template.MassmediaGroupId && Model.UserId == Template.ManagerId;
		}

		// Коэффициент по умолчанию и нижняя граница — по выбранному менеджеру, как TemplateEditorControl.SetManagerDiscount;
		// пользователь читается один раз на менеджера.
		private void ResetRatio()
		{
			if (_manager == null || _manager.Id != Template.ManagerId)
				_manager = SecurityManager.GetUser(Template.ManagerId);
			decimal value = PriceCalculatorData.DefaultManagerRatio(_manager, Template.PeriodFrom, Template.PeriodTo);
			Template.SingleRatio = value;
			RatioMinimum = _manager != null && _manager.IsAdmin ? 0m : value;
			_ratioManagerId = Template.ManagerId;
			_ratioFrom = Template.PeriodFrom.Date;
			_ratioTo = Template.PeriodTo.Date;
		}

		// Модель на период, группу и менеджера; строки заново по новой модели: отметки и позиции — по станции (и у станций,
		// выпадавших раньше), выходы и длительность — по шаблону, новым станциям — позиция шаблона.
		private void Reload()
		{
			PcModel model = PriceCalculatorData.LoadModel(Template.MassmediaGroupId, Template.PeriodFrom, Template.PeriodTo, Template.ManagerId);
			var previous = Rows.ToDictionary(r => r.MassmediaId);
			var previousModel = Model;
			Model = model;

			var dates = SelectedDates();
			var present = new HashSet<int>();
			Rows.Clear();
			foreach (var station in Model.Stations)
			{
				present.Add(station.MassmediaId);
				PcRowInput old;
				if (!previous.TryGetValue(station.MassmediaId, out old))
					_absent.TryGetValue(station.MassmediaId, out old);
				_absent.Remove(station.MassmediaId);
				_absentNames.Remove(station.MassmediaId);
				var row = Template.TemplateRow(station.MassmediaId, dates, old != null && old.Selected);
				if (old != null)
					row.Position = old.Position;
				Rows.Add(row);
			}

			foreach (var old in previous.Values.Where(r => !present.Contains(r.MassmediaId)))
			{
				_absent[old.MassmediaId] = old;
				var station = previousModel?.Stations.FirstOrDefault(s => s.MassmediaId == old.MassmediaId);
				_absentNames[old.MassmediaId] = station?.Name ?? old.MassmediaId.ToString();
			}
			UpdateDroppedStations();
		}

		// Плашка — все отмеченные выпавшие, а не только выпавшие на последнем шаге (7 → 5 → 3 не теряет станцию из группы 7).
		private void UpdateDroppedStations()
		{
			DroppedStations.Clear();
			DroppedStations.AddRange(_absent.Values.Where(r => r.Selected).Select(r => _absentNames[r.MassmediaId])
				.OrderBy(n => n, StringComparer.CurrentCulture));
		}

		// Изменение параметров после неудачного «Редактировать»: восстановление отменяется. Строки варианта ещё не поставлены
		// (модели нет — «Редактировать» её сбросил) — его станции с отметками и позициями ждут, как выпавшие, и следующее
		// перечитывание перенесёт их по обычным правилам; прежние строки забываются. Поставлены (упал только расчёт) — остаются.
		private void CancelRestore()
		{
			if (_restoring == null)
				return;
			if (Model == null)
			{
				Rows.Clear();
				_absent.Clear();
				_absentNames.Clear();
				foreach (var r in _restoring.Snapshot.Rows)
				{
					_absent[r.MassmediaId] = new PcRowInput { MassmediaId = r.MassmediaId, Selected = true, Position = r.Position };
					_absentNames[r.MassmediaId] = r.StationName ?? r.MassmediaId.ToString();
				}
			}
			_restoring = null;
		}

		// Строки варианта по свежей модели: порядок и сортировка — как при сохранении (новые станции — в конце), станции
		// варианта без прайс-листа на период — в выпавшие (плашка), прежние выпавшие забываются.
		private void RestoreRows(PriceCalculatorVariant variant)
		{
			var rows = PriceCalculatorEngine.RowsFromSnapshot(Model, variant.Snapshot, SelectedDates());
			var place = new Dictionary<int, int>();
			for (int i = 0; i < variant.Order.Count; i++)
				place[variant.Order[i]] = i;
			int Place(PcRowInput r) => place.TryGetValue(r.MassmediaId, out int i) ? i : int.MaxValue;
			Rows.Clear();
			Rows.AddRange(rows.OrderBy(Place));
			SortColumn = variant.SortColumn;
			SortDescending = variant.SortDescending;
			_sortKey = variant.SortKey;

			_absent.Clear();
			_absentNames.Clear();
			var present = new HashSet<int>(Model.Stations.Select(st => st.MassmediaId));
			foreach (var r in variant.Snapshot.Rows.Where(r => !present.Contains(r.MassmediaId)))
			{
				_absent[r.MassmediaId] = new PcRowInput
				{
					MassmediaId = r.MassmediaId,
					Selected = true,
					Duration = r.RollerDuration,
					Position = r.Position,
					PrimeWeekday = r.PrimeTotalSpotsWeekday,
					NonPrimeWeekday = r.NonPrimeTotalSpotsWeekday,
					PrimeWeekend = r.PrimeTotalSpotsWeekend,
					NonPrimeWeekend = r.NonPrimeTotalSpotsWeekend,
				};
				_absentNames[r.MassmediaId] = r.StationName ?? r.MassmediaId.ToString();
			}
			UpdateDroppedStations();
		}

		private void ResetRowsToTemplate()
		{
			var dates = SelectedDates();
			for (int i = 0; i < Rows.Count; i++)
			{
				var old = Rows[i];
				var row = Template.TemplateRow(old.MassmediaId, dates, old.Selected);
				row.Position = old.Position;
				Rows[i] = row;
			}
		}

		// Поля шаблона, от которых строятся строки и правило коэффициента.
		private string TemplateKey()
		{
			return string.Join("|", Template.UseDaysOfWeek, string.Concat(Template.DaysOfWeek.Select(d => d ? '1' : '0')),
				Template.EvenDays, Template.Duration, Template.PrimeWeekdayPerDay, Template.NonPrimeWeekdayPerDay,
				Template.PrimeWeekendPerDay, Template.NonPrimeWeekendPerDay, Template.RatioSingle);
		}

		// Переставить строки результата и строки ввода без пересчёта: цифры от порядка не зависят. Результат есть только
		// после удачного расчёта по Rows — его строки ссылаются на те же объекты.
		private void ApplySort()
		{
			if (_sortKey == null || Result == null) return;
			var stations = Model.Stations.ToDictionary(s => s.MassmediaId);
			Func<PcRowResult, IComparable> key = r =>
			{
				PcStation station;
				stations.TryGetValue(r.Input.MassmediaId, out station);
				return _sortKey(r, station);
			};
			var ordered = SortDescending
				? Result.Rows.OrderByDescending(key).ToList()
				: Result.Rows.OrderBy(key).ToList();
			Result.Rows.Clear();
			Result.Rows.AddRange(ordered);
			Rows.Clear();
			Rows.AddRange(ordered.Select(r => r.Input));
		}
	}

	public enum PriceCalculatorSaveOutcome
	{
		Added,
		Replaced,
		/// <summary>«Данный вариант уже добавлен.» — ничего не сохранено, режим редактирования остаётся.</summary>
		Duplicate,
	}

	/// <summary>
	/// Сохранённый вариант калькулятора: снимок (<see cref="CampaignCalcSnapshot"/>, его же берёт КП) и отметка карточки.
	/// Обёртка нужна, потому что снимки равны по значению, а перезапись редактируемого может дать две равные карточки:
	/// карточка, отметка и «редактируется» — по ссылке на обёртку. Порядок строк таблицы и сортировка при сохранении — чтобы
	/// «Редактировать» вернул тот же порядок (от него зависят итог до последнего знака и проверка на дубль).
	/// </summary>
	public sealed class PriceCalculatorVariant
	{
		public CampaignCalcSnapshot Snapshot { get; private set; }
		/// <summary>Отметка карточки — для «Удалить отмеченные» и КП; новая карточка отмечена, как в десктопе.</summary>
		public bool Checked { get; set; } = true;
		internal List<int> Order { get; private set; }
		internal string SortColumn { get; private set; }
		internal bool SortDescending { get; private set; }
		internal Func<PcRowResult, PcStation, IComparable> SortKey { get; private set; }

		internal void Update(CampaignCalcSnapshot snapshot, IEnumerable<PcRowInput> rows, string sortColumn, bool sortDescending,
			Func<PcRowResult, PcStation, IComparable> sortKey)
		{
			Snapshot = snapshot;
			Order = rows.Select(r => r.MassmediaId).ToList();
			SortColumn = sortColumn;
			SortDescending = sortDescending;
			SortKey = sortKey;
		}
	}
}
