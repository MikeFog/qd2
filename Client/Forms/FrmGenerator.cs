using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using Merlin.Classes;
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
		private readonly IssueTemplate _template;
		private readonly Campaign _campaign;
		private readonly PresentationObject _module;
		private readonly PresentationObject _roller;
		private readonly RollerPositions _position;
		private readonly Pricelist _pricelist;
		private DataTable _maxPrices;

		private readonly SponsorProgram program;
		private readonly int tariffID;
		private readonly decimal price;
		private readonly int bonus;

		private readonly int? grantorID;
        internal delegate DataRow UpdateDBDelegate(DateTime windowDate);
        private readonly UpdateDBDelegate _updateDB;

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
		}

		internal FrmGenerator(IssueTemplate template, UpdateDBDelegate updateDB) 
		{
            InitializeComponent();
            _template = template; 
			_updateDB = updateDB;
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

				while (_template.MoveNext())
				{
					try
					{
						pbProgress.Value++;
						Application.DoEvents();

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
				}
			}
			finally
			{
				sw.Stop();

				pbProgress.Visible = false;
				// Гарантированный вызов RecalculateAction в конце, независимо от результата
				if (_campaign != null)
					_campaign.RecalculateAction();
				/*
				MessageBox.Show(
					string.Format("Генерация выполнена за {0:F2} сек.", sw.Elapsed.TotalSeconds),
					"Время выполнения",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				*/
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
			var issues = ((CampaignOnSingleMassmedia)_campaign).GetIssuesForDate(_template.CurrentDate);
			foreach (var issue in issues) 
				issue.Delete(true);

            return issues;
        }

        private List<PresentationObject> AddIssues() 
		{
			if (_updateDB != null)
			{
				DataRow row = _updateDB(_template.CurrentDate);
				return new List<PresentationObject> { new RollerIssue(row) };
			}
			else if (_module == null && program == null)
			{
				if (_template.Mode == IssueTemplateMode.TimePeriod)
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
			ITariffWindow tariffWindow = TariffWindowWithRollerIssues.GetWindowByDate(
				_template.CurrentDate, ((CampaignOnSingleMassmedia)_campaign).Massmedia)
				?? throw new NullReferenceException("TariffWindowNotFound");

			Issue issue = _campaign.AddIssue(_roller, tariffWindow, _position, grantorID);
			issue.Refresh();
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

            int firmId = _campaign.Action.FirmID;
			var allWindows = new List<TariffWindowWithRollerIssues>();
			foreach (DataRow row in dtTariffWindow.Select(filter))
			{
				var window = new TariffWindowWithRollerIssues(row, Entities.TariffWindow);
				if (!_template.IgnoreWindowsWithTheSameFirmIssue || !window.IsRollerOfTheFirmExist(firmId, true))
					allWindows.Add(window);
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
				// Режим: разделяем на prime (цена дня из _maxPrices) и non-prime (остальные)
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
				issues.AddRange(AddIssuesFromWindows(windowsNonPrime, _template.QuantityNonPrime, "Недостаточно рекламных окон не прайм для размещения всех выпусков. Окон: {0}, выпусков для добавления {1}."));
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

			if (issues.Count + grdFail.ItemsCount < quantity)
			{
				Dictionary<string, object> parameters = CreateMessageParameters();
				parameters["description"] = string.Format(errorTemplate, issues.Count, quantity);// Globals.GetMessage("NotEnoughWindows", parameters);
				AddErrorInfo(parameters);
			}

			return issues;
		}
	}
}