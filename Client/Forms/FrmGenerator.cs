using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Threading;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;

namespace Merlin.Forms
{
	public partial class FrmGenerator : Form
	{
		private IssueTemplate template;
		private readonly Campaign campaign;
		private readonly PresentationObject module;
		private readonly PresentationObject roller;
		private readonly RollerPositions position;
		private readonly Pricelist pricelist;

		private readonly SponsorProgram program;
		private readonly int tariffID;
		private readonly decimal price;
		private readonly int bonus;

		private readonly int? grantorID;

		// For Simple Issue
		internal FrmGenerator(IssueTemplate template, PresentationObject roller, RollerPositions position,
			Campaign campaign, Pricelist pricelist, PresentationObject module, int? grantorID)
		{
			InitializeComponent();
			this.template = template;
			this.module = module;
			this.campaign = campaign;
			this.position = position;
			this.pricelist = pricelist;
			this.roller = roller;
			this.grantorID = grantorID;
			grdSuccess.Entity = ResolveEntity();
			grdFail.Entity = EntityManager.GetEntity((int)Entities.ErrTmplGen);
			tbbExcel.Image = Globals.GetImage(Constants.ActionsImages.ExportExcel);
		}

		// For Sponsors Programs
		internal FrmGenerator(IssueTemplate template,
			Campaign campaign, SponsorProgram program, int tariffID, decimal price, int bonus)
		{
			InitializeComponent();
			this.template = template;
			this.campaign = campaign;
			this.program = program;
			this.tariffID = tariffID;
			this.price = price;
			this.bonus = bonus;
			grdSuccess.Entity = ResolveEntity();
			grdFail.Entity = EntityManager.GetEntity((int)Entities.ErrTmplGen);
			tbbExcel.Image = Globals.GetImage(Constants.ActionsImages.ExportExcel);
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
				if (template == null)
					throw new NullReferenceException("Template was not initialized!");
				template.Reset();
				pbProgress.Visible = true;
				pbProgress.Maximum = template.DaysCount;
				Application.DoEvents();

				while (template.MoveNext())
				{
					try
					{
						pbProgress.Value++;
						Application.DoEvents();
						List<PresentationObject> pos = AddIssues();
						if (pos != null)
						{
							foreach (PresentationObject po in pos)
								if (po != null && po.Refresh())
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
				//campaign.RecalculateAction();
			}
			finally
			{
				pbProgress.Visible = false;
			}
		}

		Dictionary<string, object> CreateMessageParameters()
        {
			Dictionary<string, object> parameters = new Dictionary<string, object>
			{
				["issueDate"] = template.CurrentDate.ToString()
			};

			return parameters;
		}

		private void AddErrorInfo(Dictionary<string, object> parameters)
		{
			PresentationObject po = grdFail.Entity.CreateObject(parameters);
			grdFail.AddRow(po);
		}

		private List<PresentationObject> AddIssues() 
		{
			if (module == null && program == null)
			{
				if (template.Mode == IssueTemplateMode.TimePeriod)
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
			ITariffWindow tariffWindow = TariffWindowWithRollerIssues.GetWindowByDate(template.CurrentDate, ((CampaignOnSingleMassmedia)campaign).Massmedia) ?? throw new NullReferenceException("TariffWindowNotFound");
            Issue issue;
            try
            {
				DataAccessor.BeginTransaction();
				issue = campaign.AddIssue(roller, tariffWindow, position, grantorID);
                campaign.RecalculateAction(false);
                DataAccessor.CommitTransaction();
            }
			catch 
			{
                DataAccessor.RollbackTransaction();
                throw; 
			}

			if (campaign.CampaignType == Campaign.CampaignTypes.Sponsor)
			{
				campaign.RecalculateAction();
				Thread.Sleep(100);
			}
			return new List<PresentationObject> { issue };
		}

		private List<PresentationObject> AddSimpleIssues()
        {
			MassmediaPricelist pricelist = ((CampaignOnSingleMassmedia)campaign).Massmedia.GetPriceList(template.CurrentDate) as MassmediaPricelist ?? throw new Exception("PriceListDoesntExist");
            DataSet dsWindows = pricelist.GetTariffWindows(template.CurrentDate, template.CurrentDate, null, false);
			DataTable dtTariffWindow = dsWindows.Tables[Constants.TableNames.Data];
			List<PresentationObject> issues = new List<PresentationObject>();
			List<TariffWindowWithRollerIssues> windows = new List<TariffWindowWithRollerIssues>();

			// выберем блоки, относящиеся к временному интервалу шаблона
			string filter = string.Format("(hour > {0} And hour < {1}) OR (hour = {0} and min >= {2}) OR (hour = {1} and min <= {3})",
	template.StartTime.Hour, template.FinishTime.Hour, template.StartTime.Minute, template.FinishTime.Minute);
			foreach (DataRow row in dtTariffWindow.Select(filter))
				windows.Add(new TariffWindowWithRollerIssues(row, Entities.TariffWindow));
			
			// сортируем блоки по количеству свободного времени
			windows.Sort();

			for(int i = 0; i < template.Quantity && windows.Count > 0; i++)
            {
                try
                {
                    DataAccessor.BeginTransaction();
                    Issue issue = campaign.AddIssue(roller, windows[0], position, grantorID);
                    campaign.RecalculateAction(false);
                    DataAccessor.CommitTransaction();
                    issues.Add(issue);
                }
                catch(Exception ex)
                {
                    DataAccessor.RollbackTransaction();
                    Dictionary<string, object> parameters = CreateMessageParameters();
                    parameters["description"] = Globals.GetMessage(ex.Message, parameters);
                    AddErrorInfo(parameters);
                }
                
				windows.RemoveAt(0);
			}

			if (issues.Count < template.Quantity)
			{
				Dictionary<string, object> parameters = CreateMessageParameters();
				parameters["windowsQuantity"] = issues.Count;
				parameters["requiredQuantity"] = template.Quantity;
				parameters["description"] = Globals.GetMessage("NotEnoughWindows", parameters);
				AddErrorInfo(parameters);
			}

			return issues;	
        }

		private List<PresentationObject> AddModuleIssue()
		{
			return new List<PresentationObject> { campaign.AddModuleIssue((Module)module, roller, (ModulePricelist)pricelist,
																	 template.CurrentDate, position, grantorID)};			
		}

		private List<PresentationObject> AddPackModuleIssue()
		{
			return new List<PresentationObject> {
				((CampaignPackModule)campaign).AddPackModuleIssue((PackModulePricelist)pricelist, (Roller)roller, position,
																  template.CurrentDate, grantorID) };
		}

		private List<PresentationObject> AddProgramIssue()
		{
			return new List<PresentationObject> { campaign.AddProgramIssue(program, tariffID, template.CurrentDate, price, bonus, campaign.Action.IsConfirmed) }; 
		}
	}
}