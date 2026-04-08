using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using System.Linq;

namespace Merlin.Forms
{
	public partial class FrmGenerator : Form
	{
		private readonly IssueTemplate _template;
		private readonly Campaign _campaign;
		private readonly PresentationObject module;
		private readonly PresentationObject roller;
		private readonly RollerPositions position;
		private readonly Pricelist pricelist;

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
			this.module = module;
			this._campaign = campaign;
			this.position = position;
			this.pricelist = pricelist;
			this.roller = roller;
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
			if (module == null && program == null)
				return EntityManager.GetEntity((int)Entities.Issue);
			else if (module == null)
				return ProgramIssue.GetEntity();
			else if (module is Module)
				return ModuleIssue.GetEntity();
			else if (module is PackModule)
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
				pbProgress.Visible = false;
				// Гарантированный вызов RecalculateAction в конце, независимо от результата
				if (_campaign != null)
					_campaign.RecalculateAction();
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
			else if (module == null && program == null)
			{
				if (_template.Mode == IssueTemplateMode.TimePeriod)
					return AddSimpleIssues();
				return AddSimpleIssue();
			}
			else if (module == null)
				return AddProgramIssue();
			else if (module is Module)
				return AddModuleIssue();
			else if (module is PackModule)
				return AddPackModuleIssue();
			return null;
		}

		private List<PresentationObject> AddSimpleIssue()
		{
			ITariffWindow tariffWindow = TariffWindowWithRollerIssues.GetWindowByDate(
				_template.CurrentDate, ((CampaignOnSingleMassmedia)_campaign).Massmedia)
				?? throw new NullReferenceException("TariffWindowNotFound");

			Issue issue = _campaign.AddIssue(roller, tariffWindow, position, grantorID);
			issue.Refresh();
			return new List<PresentationObject> { issue };
		}

		private List<PresentationObject> AddSimpleIssues()
        {
			MassmediaPricelist pricelist = ((CampaignOnSingleMassmedia)_campaign).Massmedia.GetPriceList(_template.CurrentDate) as MassmediaPricelist ?? throw new Exception("PriceListDoesntExist");
            DataSet dsWindows = pricelist.GetTariffWindows(_template.CurrentDate, _template.CurrentDate, null, false);
			DataTable dtTariffWindow = dsWindows.Tables[Constants.TableNames.Data];
			List<PresentationObject> issues = new List<PresentationObject>();

			// Фильтруем окна, попадающие в указанный временной диапазон
			string filter = string.Format("(hour > {0} And hour < {1}) OR (hour = {0} and min >= {2}) OR (hour = {1} and min <= {3})",
							_template.StartTime.Hour, _template.FinishTime.Hour, _template.StartTime.Minute, _template.FinishTime.Minute);

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

                issues.AddRange(AddIssuesFromWindows(windows, _template.Quantity));
            }
            else
            {
                // Режим: разделяем на prime (макс. цена) и non-prime (остальные)
                List<TariffWindowWithRollerIssues> windowsPrime;
                List<TariffWindowWithRollerIssues> windowsNonPrime;

                if (allWindows.Count > 0)
                {
                    decimal maxPrice = allWindows.Max(w => w.Price);
                    windowsPrime = allWindows.Where(w => w.Price == maxPrice).ToList();

                    // Вторая по величине цена — наибольшая среди оставшихся
                    var nonPrimePrices = allWindows
                        .Select(w => w.Price)
                        .Where(p => p != maxPrice)
                        .Distinct()
                        .OrderByDescending(p => p)
                        .ToList();

                    decimal? secondPrice = nonPrimePrices.Count > 0 ? nonPrimePrices[0] : (decimal?)null;

                    windowsNonPrime = secondPrice.HasValue
                        ? allWindows.Where(w => w.Price == secondPrice.Value).ToList()
                        : new List<TariffWindowWithRollerIssues>();
                }
                else
                {
                    windowsPrime    = new List<TariffWindowWithRollerIssues>();
                    windowsNonPrime = new List<TariffWindowWithRollerIssues>();
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

                issues.AddRange(AddIssuesFromWindows(windowsPrime,    _template.QuantityPrime));
                issues.AddRange(AddIssuesFromWindows(windowsNonPrime, _template.QuantityNonPrime));
            }

            return issues;
        }

        private List<PresentationObject> AddIssuesFromWindows(List<TariffWindowWithRollerIssues> windows, int quantity)
        {
			var issues = new List<PresentationObject>();

			for (int i = 0; i < quantity && windows.Count > 0; i++)
			{
				try
				{
					Issue issue = _campaign.AddIssue(roller, windows[0], position, grantorID);
					issue.Refresh();
					issues.Add(issue);
				}
				catch (Exception ex)
				{
					// DataAccessor.RollbackTransaction(); // убрать: транзакция здесь не открывается
					Dictionary<string, object> parameters = CreateMessageParameters();
					parameters["description"] = Globals.GetMessage(ex.Message, parameters);
					AddErrorInfo(parameters);
				}

				windows.RemoveAt(0);
			}

			if (issues.Count + grdFail.ItemsCount < quantity)
			{
				Dictionary<string, object> parameters = CreateMessageParameters();
				parameters["windowsQuantity"] = issues.Count;
				parameters["requiredQuantity"] = quantity;
				parameters["description"] = Globals.GetMessage("NotEnoughWindows", parameters);
				AddErrorInfo(parameters);
			}

			return issues;
		}

		private List<PresentationObject> AddModuleIssue()
		{
            ModuleIssue moduleIssue = _campaign.AddModuleIssue((Module)module, roller, (ModulePricelist)pricelist,
                                                                     _template.CurrentDate, position, grantorID);
			moduleIssue.Refresh();
            return new List<PresentationObject> { moduleIssue};			
		}

		private List<PresentationObject> AddPackModuleIssue()
		{
            PackModuleIssue packModuleIssue = ((CampaignPackModule)_campaign).AddPackModuleIssue((PackModulePricelist)pricelist, (Roller)roller, position,
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
	}
}