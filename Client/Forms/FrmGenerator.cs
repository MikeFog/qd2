using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using log4net;
using Merlin.Classes;
using Merlin.Classes.Domain;
using Merlin.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using System.Linq;
using System.Diagnostics;

namespace Merlin.Forms
{
	public partial class FrmGenerator : Form
	{
        private static readonly ILog Log = LogManager.GetLogger(typeof(FrmGenerator));

		private readonly IssueTemplate _template;
		private readonly Campaign _campaign;
		private readonly PresentationObject _module;
		private readonly PresentationObject _roller;
		private readonly RollerPositions _position;
		private readonly Pricelist _pricelist;
		private DataTable _maxPrices;
		private readonly int _firmId = -1;

        private readonly SponsorProgram program;
		private readonly int tariffID;
		private readonly decimal price;
		private readonly int bonus;

		private readonly int? grantorID;
        internal delegate DataRow UpdateDBDelegate(DateTime windowDate);
		internal delegate List<PresentationObject> DeleteDBDelegate(DateTime windowDate);
        internal delegate TimePeriodAddResult UpdateDBTimePeriodDelegate(DateTime date);
        private readonly UpdateDBDelegate _updateDB;
		private readonly DeleteDBDelegate _deleteDB;
        private readonly UpdateDBTimePeriodDelegate _updateTimePeriodDB;
        private readonly ActionOnMassmedia _action;

        // Несколько роликов (Шаблон 3): очередь уже перемешанных случайно roller-слотов,
        // по одному на каждую единицу "Количество" из грида. День за днём забираем из начала очереди.
        private readonly LinkedList<Roller> _rollerQueue;

        // Выпуски, добавленные текущим запуском генератора — источник для кнопки "Отменить"
        // на CampaignForm/EditIssuesForm после закрытия диалога.
        internal List<PresentationObject> AddedObjects { get; } = new List<PresentationObject>();

        // For Simple Issue
        internal FrmGenerator(IssueTemplate template, PresentationObject roller, RollerPositions position,
			Campaign campaign, Pricelist pricelist, PresentationObject module, int? grantorID)
		{
			InitializeComponent();
			this._template = template;
			this._module = module;
			this._campaign = campaign;
			this._position = position;
			this._pricelist = pricelist;
			this._roller = roller;
			this.grantorID = grantorID;
            _firmId = _campaign.Action.FirmID;
        }

        // For multiple rollers with individual quantities (Шаблон 3, линейная кампания):
        // список "ролик x количество" разворачивается в очередь единичных слотов и перемешивается один раз.
        internal FrmGenerator(IssueTemplate template, IEnumerable<(Roller Roller, int Quantity)> rollerQuantities, RollerPositions position,
            Campaign campaign, Pricelist pricelist, PresentationObject module, int? grantorID)
        {
            InitializeComponent();
            this._template = template;
            this._module = module;
            this._campaign = campaign;
            this._position = position;
            this._pricelist = pricelist;
            this.grantorID = grantorID;
            _firmId = _campaign.Action.FirmID;

            var expanded = new List<Roller>();
            foreach (var (roller, quantity) in rollerQuantities)
                for (int i = 0; i < quantity; i++)
                    expanded.Add(roller);

            var rnd = new Random();
            for (int i = expanded.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                (expanded[i], expanded[j]) = (expanded[j], expanded[i]);
            }

            _rollerQueue = new LinkedList<Roller>(expanded);
        }

		// For Sponsors Programs
		internal FrmGenerator(IssueTemplate template, Campaign campaign, SponsorProgram program, int tariffID, decimal price, int bonus)
		{
			InitializeComponent();
			this._template = template;
			this._campaign = campaign;
			this.program = program;
			this.tariffID = tariffID;
			this.price = price;
			this.bonus = bonus;
            _firmId = _campaign.Action.FirmID;
        }

		internal FrmGenerator(IssueTemplate template, UpdateDBDelegate updateDB, DeleteDBDelegate deleteDB = null)
		{
            InitializeComponent();
            _template = template;
			_updateDB = updateDB;
			_deleteDB = deleteDB;
        }

        // For Range TimePeriod (FrmTemplate2 in fan-out campaign)
        internal FrmGenerator(IssueTemplate template,
            UpdateDBTimePeriodDelegate updateTimePeriodDB,
            ActionOnMassmedia action)
        {
            InitializeComponent();
            _template = template;
            _updateTimePeriodDB = updateTimePeriodDB;
            _action = action;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            grdSuccess.Entity = ResolveEntity();
            grdFail.Entity = EntityManager.GetEntity((int)Entities.ErrTmplGen);
            tbbExcel.Image = Globals.GetImage(Constants.ActionsImages.ExportExcel);
			if (!_template.IsModeAdd)
			{
                this.Text = "Удаление рекламных выпусков по шаблону";
                grdFail.Caption = "Ошибки удаления";
                grdSuccess.Caption = "Удаленные выпуски";
            }
        }

        private Entity ResolveEntity()
		{
			if (_module == null && program == null)
				return EntityManager.GetEntity((int)Entities.Issue);
			else if (_module == null)
				return ProgramIssue.GetEntity();
			else if (_module is Module)
				return ModuleIssue.GetEntity();
			else if (_module is PackModule)
				return PackModuleIssue.GetEntity();
			return null;
		}

		private void tbbExcel_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				DataGridView grid = (ActiveControl == grdFail) ? grdFail.InternalGrid : grdSuccess.InternalGrid;
				Entity entity = (ActiveControl == grdFail) ? grdFail.Entity : grdSuccess.Entity;
				if (grid.RowCount > 0)
					ExportManager.ExportExcel(grid, entity);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			try
			{
				Cursor.Current = Cursors.WaitCursor;
				Generate();
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			e.Cancel = pbProgress.Visible;
			base.OnClosing(e);
		}

		public void Generate()
		{
			Stopwatch sw = Stopwatch.StartNew();

			try
			{
				if (_template == null)
					throw new NullReferenceException("Template was not initialized!");
				_template.Reset();
				pbProgress.Visible = true;
				pbProgress.Maximum = _template.DaysCount;
				Application.DoEvents();
                _template.Reset();
                while (_template.MoveNext())
				{
					try
					{
						List<PresentationObject> pos = _template.IsModeAdd ? AddIssues() : DeleteIssues();
						if (pos != null)
						{
							foreach (PresentationObject po in pos)
								if (po != null/* && po.Refresh()*/)
									grdSuccess.AddRow(po);
						}
					}
					catch (Exception ex)
					{
						Dictionary<string, object> parameters = CreateMessageParameters();
						parameters["description"] = Globals.GetMessage(ex.Message, parameters);
						AddErrorInfo(parameters);
					}
                    pbProgress.Value++;
                    Application.DoEvents();
                }

                // За весь период не хватило окон, чтобы разместить всех оставшихся в очереди роликов
                if (_rollerQueue != null && _rollerQueue.Count > 0)
                {
                    Dictionary<string, object> parameters = CreateMessageParameters();
                    parameters["description"] = $"Недостаточно рекламных окон за весь период шаблона — не удалось разместить {_rollerQueue.Count} выход(ов) из выбранных роликов.";
                    AddErrorInfo(parameters);
                }
			}
			finally
			{
				sw.Stop();
                Log.Info($"Generate completed in {sw.ElapsedMilliseconds}ms" +
                    $" success={grdSuccess.InternalGrid.RowCount} fail={grdFail.InternalGrid.RowCount}");

				pbProgress.Visible = false;
                // Гарантированный вызов RecalculateAction в конце, независимо от результата
                if (_campaign != null)
					_campaign.RecalculateAction();
                // Range TimePeriod: single recalculate after all slots are generated
                else if (_action != null)
                    _action.Recalculate();
			}
		}

		Dictionary<string, object> CreateMessageParameters()
        {
			Dictionary<string, object> parameters = new Dictionary<string, object>
			{
				["issueDate"] = _template.CurrentDate.ToString()
			};

			return parameters;
		}

		private void AddErrorInfo(Dictionary<string, object> parameters)
		{
			PresentationObject po = grdFail.Entity.CreateObject(parameters);
			grdFail.AddRow(po);
		}

        private List<PresentationObject> DeleteIssues()
		{
			if (_deleteDB != null)
				return _deleteDB(_template.CurrentDate);

			var issues = ((CampaignOnSingleMassmedia)_campaign).GetIssuesForDate(_template.CurrentDate);
			foreach (var issue in issues) 
				issue.Delete(true);

            return issues;
        }

        private List<PresentationObject> AddIssues()
		{
            // Range + TimePeriod: delegate handles multiple slots per day
            if (_updateTimePeriodDB != null)
            {
                TimePeriodAddResult result = _updateTimePeriodDB(_template.CurrentDate);

                // Individual slot errors → grdFail, generation of remaining slots continues
                foreach (Exception ex in result.Errors)
                {
                    Dictionary<string, object> p = CreateMessageParameters();
                    p["description"] = Globals.GetMessage(ex.Message, p);
                    AddErrorInfo(p);
                }

                // Summary error when fewer slots were placed than requested
                if (result.Rows.Count < result.ExpectedCount)
                {
                    Dictionary<string, object> p = CreateMessageParameters();
                    p["description"] = string.Format(
                        "Недостаточно слотов для размещения. Добавлено: {0}, требовалось: {1}.",
                        result.Rows.Count, result.ExpectedCount);
                    AddErrorInfo(p);
                }

                // MasterIssue — реальный удаляемый объект (Delete -> MasterIssueDelete),
                // в отличие от RollerIssue ниже, который несёт фейковый IssueId и годится
                // только для отображения в grdSuccess.
                foreach (DataRow r in result.Rows)
                    AddedObjects.Add(new MasterIssue(r));

                return result.Rows
                    .Select(r => (PresentationObject)new RollerIssue(r))
                    .ToList();
            }

            // Range + Simple: single-slot delegate
            if (_updateDB != null)
			{
				DataRow row = _updateDB(_template.CurrentDate);
				AddedObjects.Add(new MasterIssue(row));
				return new List<PresentationObject> { new RollerIssue(row) };
			}
			// Несколько роликов с разным количеством выходов (Шаблон 3, линейная кампания, TimePeriod)
			else if (_rollerQueue != null)
			{
				if (_maxPrices == null)
					_maxPrices = ((CampaignOnSingleMassmedia)_campaign).Massmedia.GetMaxPriceByDay(_template.StartDate, _template.FinishDate);
				return AddMultiRollerIssues();
			}
			else if (_module == null && program == null)
			{
				if (_template.TemplateType == IssueTemplateType.TimePeriod)
				{
					if (_maxPrices == null)
						_maxPrices = ((CampaignOnSingleMassmedia)_campaign).Massmedia.GetMaxPriceByDay(_template.StartDate, _template.FinishDate);
					return AddSimpleIssues();
				}
				return AddSimpleIssue();
			}
			else if (_module == null)
				return AddProgramIssue();
			else if (_module is Module)
				return AddModuleIssue();
			else if (_module is PackModule)
				return AddPackModuleIssue();
			return null;
		}

		private List<PresentationObject> AddSimpleIssue()
		{
			Issue issue = null;
            TariffWindowWithRollerIssues window = (TariffWindowWithRollerIssues)TariffWindowWithRollerIssues.GetWindowByDate(
				_template.CurrentDate, ((CampaignOnSingleMassmedia)_campaign).Massmedia)
				?? throw new NullReferenceException("TariffWindowNotFound");

            if (!_template.IgnoreWindowsWithTheSameFirmIssue || !window.IsRollerOfTheFirmExist(_firmId, true))
			{
				if (_position != RollerPositions.Undefined && IsPositionOccupied(window, _position))
				{
					Log.Info($"Position {_position} already occupied in window {window.WindowDate}, skipping");
					throw new Exception("FirstLastIssueError");
				}
                issue = _campaign.AddIssue(_roller, window, _position, grantorID);
			}
            else
                throw new Exception("IssueWithTheSameFirmExists");
            issue.Refresh();
            AddedObjects.Add(issue);
			return new List<PresentationObject> { issue };
		}

		private List<PresentationObject> AddSimpleIssues()
		{
			Massmedia radioStation = ((CampaignOnSingleMassmedia)_campaign).Massmedia;

			MassmediaPricelist pricelist = radioStation.GetPriceList(_template.CurrentDate) as MassmediaPricelist ?? throw new Exception("PriceListDoesntExist");
			DataSet dsWindows = pricelist.GetTariffWindows(_template.CurrentDate, _template.CurrentDate, null, false, false);
			DataTable dtTariffWindow = dsWindows.Tables[Constants.TableNames.Data];
			List<PresentationObject> issues = new List<PresentationObject>();

            // Фильтруем окна, попадающие в указанный временной диапазон
            int startTotal = _template.StartTime.Hour * 60 + _template.StartTime.Minute;
            int finishTotal = _template.FinishTime.Hour * 60 + _template.FinishTime.Minute;
            string filter = $"(hour * 60 + min) >= {startTotal} AND (hour * 60 + min) <= {finishTotal}";

			var allWindows = new List<TariffWindowWithRollerIssues>();
			foreach (DataRow row in dtTariffWindow.Select(filter))
			{
				var window = new TariffWindowWithRollerIssues(row, Entities.TariffWindow);
				if ((!_template.IgnoreWindowsWithTheSameFirmIssue || !window.IsRollerOfTheFirmExist(_firmId, true))
				    && (_position == RollerPositions.Undefined || !IsPositionOccupied(window, _position)))
				{
					allWindows.Add(window);
				}
			}

			var rnd = new Random();

			if (_template.Quantity != 0)
			{
                // Режим: использовать все окна вместе, quantity = _template.Quantity
                var windows = allWindows
					.Select(w => new { w, rand = rnd.Next() })
					.OrderBy(x => x.w)
					.ThenBy(x => x.rand)
					.Select(x => x.w)
					.ToList();

                issues.AddRange(AddIssuesFromWindows(windows, _template.Quantity, "Недостаточно рекламных окон для размещения всех выпусков для добавления. Окон: {0}, выпусков {1}."));
            }
			else
			{
				// ?????: ????????? ?? prime (???? ??? ?? _maxPrices) ? non-prime (?????????)
				decimal? dayPrimePrice = GetPrimePriceForDate(_template.CurrentDate);

				List<TariffWindowWithRollerIssues> windowsPrime;
				List<TariffWindowWithRollerIssues> windowsNonPrime;

				if (dayPrimePrice.HasValue)
				{
					windowsPrime = allWindows.Where(w => w.Price == dayPrimePrice.Value).ToList();
					windowsNonPrime = allWindows.Where(w => w.Price != dayPrimePrice.Value).ToList();
				}
				else
				{
                    // Нет прайм-цены на день: прайм-окон нет, остальные считаем non-prime
                    windowsPrime = new List<TariffWindowWithRollerIssues>();
					windowsNonPrime = new List<TariffWindowWithRollerIssues>(allWindows);
				}

				windowsPrime = windowsPrime
					.Select(w => new { w, rand = rnd.Next() })
					.OrderBy(x => x.w)
					.ThenBy(x => x.rand)
					.Select(x => x.w)
					.ToList();

				windowsNonPrime = windowsNonPrime
					.Select(w => new { w, rand = rnd.Next() })
					.OrderBy(x => x.w)
					.ThenBy(x => x.rand)
					.Select(x => x.w)
					.ToList();

                // Всегда: prime -> только prime, non-prime -> только non-prime
                issues.AddRange(AddIssuesFromWindows(windowsPrime, _template.QuantityPrime, "Недостаточно рекламных окон прайм для размещения всех выпусков. Окон: {0}, выпусков {1}."));
                issues.AddRange(AddIssuesFromWindows(windowsNonPrime, _template.QuantityNonPrime, "Недостаточно рекламных окон офф прайм для размещения всех выпусков. Окон: {0}, выпусков для добавления {1}."));
            }

            AddedObjects.AddRange(issues);
            return issues;
		}

		// Аналог AddSimpleIssues, но роликов несколько — на каждый день забираем нужное количество
		// слотов из перемешанной очереди _rollerQueue вместо одного фиксированного _roller.
		private List<PresentationObject> AddMultiRollerIssues()
		{
			Massmedia radioStation = ((CampaignOnSingleMassmedia)_campaign).Massmedia;

			MassmediaPricelist pricelist = radioStation.GetPriceList(_template.CurrentDate) as MassmediaPricelist ?? throw new Exception("PriceListDoesntExist");
			DataSet dsWindows = pricelist.GetTariffWindows(_template.CurrentDate, _template.CurrentDate, null, false, false);
			DataTable dtTariffWindow = dsWindows.Tables[Constants.TableNames.Data];

			int startTotal = _template.StartTime.Hour * 60 + _template.StartTime.Minute;
			int finishTotal = _template.FinishTime.Hour * 60 + _template.FinishTime.Minute;
			string filter = $"(hour * 60 + min) >= {startTotal} AND (hour * 60 + min) <= {finishTotal}";

			var allWindows = new List<TariffWindowWithRollerIssues>();
			foreach (DataRow row in dtTariffWindow.Select(filter))
			{
				var window = new TariffWindowWithRollerIssues(row, Entities.TariffWindow);
				if ((!_template.IgnoreWindowsWithTheSameFirmIssue || !window.IsRollerOfTheFirmExist(_firmId, true))
				    && (_position == RollerPositions.Undefined || !IsPositionOccupied(window, _position)))
				{
					allWindows.Add(window);
				}
			}

			var rnd = new Random();
			var issues = new List<PresentationObject>();

			if (_template.Quantity != 0)
			{
				var windows = OrderWindowsRandomly(allWindows, rnd);
				issues.AddRange(AddIssuesForRollers(_template.Quantity, windows,
					"Недостаточно рекламных окон для размещения всех выпусков. Окон: {0}, выпусков: {1}."));
			}
			else
			{
				decimal? dayPrimePrice = GetPrimePriceForDate(_template.CurrentDate);

				List<TariffWindowWithRollerIssues> windowsPrime;
				List<TariffWindowWithRollerIssues> windowsNonPrime;

				if (dayPrimePrice.HasValue)
				{
					windowsPrime = allWindows.Where(w => w.Price == dayPrimePrice.Value).ToList();
					windowsNonPrime = allWindows.Where(w => w.Price != dayPrimePrice.Value).ToList();
				}
				else
				{
					windowsPrime = new List<TariffWindowWithRollerIssues>();
					windowsNonPrime = new List<TariffWindowWithRollerIssues>(allWindows);
				}

				windowsPrime = OrderWindowsRandomly(windowsPrime, rnd);
				windowsNonPrime = OrderWindowsRandomly(windowsNonPrime, rnd);

				issues.AddRange(AddIssuesForRollers(_template.QuantityPrime, windowsPrime,
					"Недостаточно рекламных окон прайм для размещения всех выпусков. Окон: {0}, выпусков: {1}."));
				issues.AddRange(AddIssuesForRollers(_template.QuantityNonPrime, windowsNonPrime,
					"Недостаточно рекламных окон офф прайм для размещения всех выпусков. Окон: {0}, выпусков: {1}."));
			}

			AddedObjects.AddRange(issues);
			return issues;
		}

		private static List<TariffWindowWithRollerIssues> OrderWindowsRandomly(List<TariffWindowWithRollerIssues> windows, Random rnd)
		{
			return windows
				.Select(w => new { w, rand = rnd.Next() })
				.OrderBy(x => x.w)
				.ThenBy(x => x.rand)
				.Select(x => x.w)
				.ToList();
		}

		// Забирает с начала общей очереди до count роликов на сегодня и сортирует их по убыванию
		// длительности — длинные ролики размещаются первыми, пока в дне больше свободных окон.
		private List<Roller> TakeRollersForToday(int count)
		{
			var batch = new List<Roller>();
			for (int i = 0; i < count && _rollerQueue.Count > 0; i++)
			{
				batch.Add(_rollerQueue.First.Value);
				_rollerQueue.RemoveFirst();
			}
			return batch.OrderByDescending(r => r.Duration).ToList();
		}

		private List<PresentationObject> AddIssuesForRollers(int targetCount, List<TariffWindowWithRollerIssues> windows, string errorTemplate)
		{
			List<Roller> rollers = TakeRollersForToday(targetCount);
			var issues = new List<PresentationObject>();

			while (rollers.Count > 0 && windows.Count > 0)
			{
				Roller roller = rollers[0];
				rollers.RemoveAt(0);
				try
				{
					Issue issue = _campaign.AddIssue(roller, windows[0], _position, grantorID);
					issue.Refresh();
					issues.Add(issue);
				}
				catch (Exception ex)
				{
					Dictionary<string, object> parameters = CreateMessageParameters();
					parameters["description"] = Globals.GetMessage(ex.Message, parameters);
					AddErrorInfo(parameters);
				}
				windows.RemoveAt(0);
			}

			// Не хватило окон сегодня — недостающие ролики возвращаются в начало очереди, попробуем в другой день
			for (int i = rollers.Count - 1; i >= 0; i--)
				_rollerQueue.AddFirst(rollers[i]);

			if (issues.Count < targetCount)
			{
				Dictionary<string, object> parameters = CreateMessageParameters();
				parameters["description"] = string.Format(errorTemplate, issues.Count, targetCount);
				AddErrorInfo(parameters);
			}

			return issues;
		}

		private decimal? GetPrimePriceForDate(DateTime date)
		{
			if (_maxPrices == null || _maxPrices.Rows.Count == 0)
				return null;

			DataColumn dateColumn = _maxPrices.Columns.Cast<DataColumn>()
				.FirstOrDefault(c => string.Equals(c.ColumnName, "Date", StringComparison.OrdinalIgnoreCase));
			DataColumn priceColumn = _maxPrices.Columns.Cast<DataColumn>()
				.FirstOrDefault(c => string.Equals(c.ColumnName, "Price", StringComparison.OrdinalIgnoreCase));

			if (dateColumn == null || priceColumn == null)
				return null;

			foreach (DataRow row in _maxPrices.Rows)
			{
				DateTime rowDate = ParseHelper.GetDateTimeFromObject(row[dateColumn], DateTime.MinValue);
				if (rowDate != DateTime.MinValue && rowDate.Date == date.Date)
					return ParseHelper.GetDecimalFromObject(row[priceColumn], 0m);
			}

			return null;
		}
		
		private List<PresentationObject> AddModuleIssue()
		{
            ModuleIssue moduleIssue = _campaign.AddModuleIssue((Module)_module, _roller, (ModulePricelist)_pricelist,
                                                                     _template.CurrentDate, _position, grantorID);
			moduleIssue.Refresh();
            return new List<PresentationObject> { moduleIssue};			
		}

		private List<PresentationObject> AddPackModuleIssue()
		{
            PackModuleIssue packModuleIssue = ((CampaignPackModule)_campaign).AddPackModuleIssue((PackModulePricelist)_pricelist, (Roller)_roller, _position,
                                                                  _template.CurrentDate, grantorID);
			packModuleIssue.Refresh();
			return new List<PresentationObject> { packModuleIssue };
		}

		private List<PresentationObject> AddProgramIssue()
		{
            ProgramIssue programIssue = _campaign.AddProgramIssue(program, tariffID, _template.CurrentDate, price, bonus, _campaign.Action.IsConfirmed);
			programIssue.Refresh();
            return new List<PresentationObject> { programIssue }; 
		}

		/// <summary>
		/// Проверяет, занята ли указанная позиция ролика в окне.
		/// </summary>
		private static bool IsPositionOccupied(TariffWindowWithRollerIssues window, RollerPositions position)
		{
			switch (position)
			{
				case RollerPositions.First:
					return window.IsFirstPositionOccupied;
				case RollerPositions.Second:
					return window.IsSecondPositionOccupied;
				case RollerPositions.Last:
					return window.IsLastPositionOccupied;
				default:
					return false;
			}
		}

		private List<PresentationObject> AddIssuesFromWindows(List<TariffWindowWithRollerIssues> windows, int quantity, string errorTemplate)
		{
			var issues = new List<PresentationObject>();

			for (int i = 0; i < quantity && windows.Count > 0; i++)
			{
				try
				{
					Issue issue = _campaign.AddIssue(_roller, windows[0], _position, grantorID);
					issue.Refresh();
					issues.Add(issue);
				}
				catch (Exception ex)
				{
					Dictionary<string, object> parameters = CreateMessageParameters();
					parameters["description"] = Globals.GetMessage(ex.Message, parameters);
					AddErrorInfo(parameters);
				}

				windows.RemoveAt(0);
			}

			if (issues.Count < quantity)
			{
				Dictionary<string, object> parameters = CreateMessageParameters();
				parameters["description"] = string.Format(errorTemplate, issues.Count, quantity);// Globals.GetMessage("NotEnoughWindows", parameters);
				AddErrorInfo(parameters);
			}

			return issues;
		}
	}
}
