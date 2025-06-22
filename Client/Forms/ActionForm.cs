using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Classes;
using Merlin.Forms.CreateCampaign;
using Action = Merlin.Classes.Action;

namespace Merlin.Forms
{
    public partial class ActionForm : Form
	{
		private readonly ActionOnMassmedia action;

		public ActionForm()
		{
			InitializeComponent();
			tsbAdd.Image = Globals.GetImage(Constants.ActionsImages.Add);
			tsbSetDiscount.Image = Globals.GetImage(Constants.ActionsImages.User);
			tsbDelete.Image = Globals.GetImage(Constants.ActionsImages.Delete);
			tsbEditRollerIssues.Image = Globals.GetImage(Constants.ActionsImages.Issue);
			tsbEditProgIssues.Image = Globals.GetImage(Constants.ActionsImages.SponsorProgram);
		}

		internal ActionForm(ActionOnMassmedia action)
			: this()
		{
			this.action = action;
			action.Refresh();
		}

		private void EnableToolbarButtons(Campaign campaign)
		{
			if (campaign == null)
				tsbSetDiscount.Enabled = tsbDelete.Enabled =
				                         tsbEditRollerIssues.Enabled = tsbEditProgIssues.Enabled = tsbPrintMediaPlan.Enabled = false;
			else
			{
				tsbSetDiscount.Enabled = tsbDelete.Enabled = tsbEditRollerIssues.Enabled = tsbPrintMediaPlan.Enabled = true;
				Campaign massmediaCampaign = campaign;
				tsbEditProgIssues.Enabled = (massmediaCampaign.CampaignType == Campaign.CampaignTypes.Sponsor);
			}
		}

		private void SetFormCaption()
		{
			Text = action.IsConfirmed ?	"Подтвержденная рекламная акция" : "Неподтвержденная рекламная акция";
		}

		private void InitFormFromClass()
		{
			lblFirmName.Text = action.FirmName;
			lblName.Text = action.Name;

			LoadCampaigns();
			RefreshActionStats(false);
		}

		private void LoadCampaigns()
		{
			Entity entityCampaign = EntityManager.GetEntity((int) Entities.CampaignOnMassmedia);
			entityCampaign.AttributeSelector = Campaign.ShortAttributesList;
			grdCampaign.Entity = entityCampaign;
			action.ClearCache();
			grdCampaign.DataSource = action.GetContent().DefaultView;
		}

		private void RefreshActionStats(bool refreshFlag)
		{
			if (refreshFlag) action.Refresh();

			lblTariffSum.Text = action.TariffPrice.ToString("c");
			lblPackDiscount.Text = action.Discount.ToString("f");
			lblTotalPrice.Text = action.TotalPrice.ToString("c");
		}

		private void grdCampaign_ObjectSelected(PresentationObject presentationObject)
		{
			try
			{
				EnableToolbarButtons(presentationObject as Campaign);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void AddCampaign(object sender, EventArgs e)
		{
			try
			{
				// будем считать, что если есть права редактировать обычную компанию, то есть и права создавать компании
				if(!EntityManager.GetEntity((int)Entities.GeneralCampaign).IsActionEnabled(Constants.EntityActions.Edit, ViewType.Journal))
				{
                    FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Properties.Resources.OperationNotAllowed);
                    return;
                }

				CampaignNewForm fNewCampaign = new CampaignNewForm();
				if (fNewCampaign.ShowDialog(this) == DialogResult.OK)
				{
					Application.DoEvents();
					Cursor = Cursors.WaitCursor;

					if (action.IsNew) action.Update();

					Campaign campaign = fNewCampaign.Campaign;
					campaign.Action = action;
					campaign.Update();

					grdCampaign.AddRow(campaign);
				}
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

		private void DeleteCampaign(object sender, EventArgs e)
		{
			try
			{
				PresentationObject presentationObject = SelectedCampaign;
				if (!presentationObject.IsActionEnabled(Constants.EntityActions.Delete, ViewType.Journal))
				{
					FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Properties.Resources.OperationNotAllowed); 
					return;		
				}

				if (presentationObject.Delete())
				{
					Cursor.Current = Cursors.WaitCursor;
                    action.Recalculate();
                    grdCampaign.DeleteRow(presentationObject);
					RefreshActionStats(true);
                    LoadCampaigns();

                    EnableToolbarButtons(grdCampaign.SelectedObject as Campaign);
				}
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

		private void EditRollerIssues(object sender, EventArgs e)
		{
			try
			{
				Campaign campaign = SelectedCampaign;
				if (
					(campaign.CampaignType != Campaign.CampaignTypes.Sponsor && !campaign.IsActionEnabled(Constants.EntityActions.Edit, ViewType.Journal))
					||
					(campaign.CampaignType == Campaign.CampaignTypes.Sponsor && !EntityManager.GetEntity((int)Entities.RollerPart).IsActionEnabled(RollerPartOfSponsorCampaign.ActionNames.EditIssues, ViewType.Journal))
					)
				{
					FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Properties.Resources.OperationNotAllowed);
					return;
				}
                campaign.DoAction(Constants.EntityActions.Edit, this, InterfaceObjects.SimpleJournal);
				RefreshActionStats(true);
				LoadCampaigns();
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

		private Campaign SelectedCampaign
		{
			get { return (Campaign)grdCampaign.SelectedObject; }
		}

		private void EditProgIssues(object sender, EventArgs e)
		{
			try
			{
                if (!EntityManager.GetEntity((int)Entities.ProgramPart).IsActionEnabled(ProgramPartOfSponsorCampaign.ActionNames.EditIssues, ViewType.Journal))
                {
                    FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Properties.Resources.OperationNotAllowed);
                    return;
                }
                Campaign campaign = SelectedCampaign;
                campaign.EditProgramIssues(this);
				RefreshActionStats(true);
				LoadCampaigns();
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

		private void SetDiscount(object sender, EventArgs e)
		{
			try
			{
				if (SelectedCampaign == null || SelectedCampaign.TariffPrice == 0) return;

				ManagerDiscountForm fDiscount = new ManagerDiscountForm(SelectedCampaign);
				if (fDiscount.ShowDialog(this) == DialogResult.OK)
				{
					Application.DoEvents();
					Cursor = Cursors.WaitCursor;
					Campaign campaign = SelectedCampaign;
					campaign.SetFinalPrice(fDiscount.FinalPrice, fDiscount.CurrentDate, fDiscount.Grantor == null ? null : (int?)fDiscount.Grantor.Id);
					//campaign.RecalculateAction();
					campaign.Refresh();
					grdCampaign.UpdateRow(campaign);			
					RefreshActionStats(true);
				}
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

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				if (chkPrintBill.Checked)
					action.PrintBills(this, false, false);
				if (chkPrintContract.Checked)
					action.PrintContracts(this, false);
                if (chkPrintSponsorContract.Checked)
                    action.PrintSponsorContracts(this, false);
				if (chkPrintMediaPlan.Checked)
					action.PrintMediaPlan(Action.ActionMediaPlanType.Massmedias, false);
                if (chkPrintBillContract.Checked)
                    action.PrintBillContracts(this, false);
            }
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void ActionForm_Load(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				SetFormCaption();
				InitFormFromClass();
			}	
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void tsbPrintMediaPlan_Click(object sender, EventArgs e)
		{
			if (SelectedCampaign != null)
				SelectedCampaign.PrintMediaPlan(false, false, false, false);
		}
	}
}