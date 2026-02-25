using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Classes;
using Merlin.Forms.CreateActionMaster;
using Merlin.Forms.CreateCampaign;
using System;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;
using Action = Merlin.Classes.Action;

namespace Merlin.Forms
{
    public partial class ActionForm : Form
	{
		private readonly ActionOnMassmedia _action;

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
			this._action = action;
			action.Refresh();
		}

		private void EnableToolbarButtons(Campaign campaign)
		{
			tsbSetActionPrice.Enabled = tsbSetDiscount.Enabled = tsbDelete.Enabled = tsbEditRollerIssues.Enabled = 
				tsbEditProgIssues.Enabled = tsbPrintMediaPlan.Enabled = campaign != null;

            if (campaign != null)
			{
				Campaign massmediaCampaign = campaign;
				tsbEditProgIssues.Enabled = (massmediaCampaign.CampaignType == Campaign.CampaignTypes.Sponsor);
			}
		}

		private void SetFormCaption()
		{
			Text = _action.IsConfirmed ?	"Подтвержденная рекламная акция" : "Неподтвержденная рекламная акция";
		}

		private void InitFormFromClass()
		{
			lblFirmName.Text = _action.FirmName;
			lblName.Text = _action.Name;

			LoadCampaigns();
			RefreshActionStats(false);
		}

		private void LoadCampaigns()
		{
			Entity entityCampaign = EntityManager.GetEntity((int) Entities.CampaignOnMassmedia);
			entityCampaign.AttributeSelector = Campaign.ShortAttributesList;
			grdCampaign.Entity = entityCampaign;
			_action.ClearCache();
			grdCampaign.DataSource = _action.GetContent().DefaultView;
		}

		private void RefreshActionStats(bool refreshFlag)
		{
			if (refreshFlag) _action.Refresh();

			lblTariffSum.Text = _action.TariffPrice.ToString("c");
			lblPackDiscount.Text = _action.Discount.ToString("f");
			lblTotalPrice.Text = _action.TotalPrice.ToString("c");
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
					
					this.UseWaitCursor = true;
                    Application.DoEvents();

                    if (_action.IsNew) _action.Update();

					Campaign campaign = fNewCampaign.Campaign;
					campaign.Action = _action;
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
				this.UseWaitCursor = false;
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
					this.UseWaitCursor = true;	
                    Application.DoEvents();

                    _action.Recalculate();
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
				this.UseWaitCursor = false;	
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
				var currentCampaign = grdCampaign.SelectedObject;
				LoadCampaigns();
				grdCampaign.SelectedObject = currentCampaign;
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
					UseWaitCursor = true;
                    Application.DoEvents();

                    Campaign campaign = SelectedCampaign;
					try
					{
						DataAccessor.BeginTransaction();
						campaign.SetFinalPrice(fDiscount.FinalPrice, fDiscount.CurrentDate, fDiscount.Grantor == null ? null : (int?)fDiscount.Grantor.Id);
						_action.Recalculate(refreshFlag: true, todayDate: fDiscount.CurrentDate);
                        DataAccessor.CommitTransaction();
                    }
					catch
					{
						DataAccessor.RollbackTransaction();
						throw;
                    }

                    campaign.Refresh();
					grdCampaign.UpdateRow(campaign);			
					RefreshActionStats(false);
				}
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				UseWaitCursor = false;
			}
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				if (chkPrintBill.Checked)
					_action.PrintBills(this, false, false);
				if (chkPrintContract.Checked)
					_action.PrintContracts(this, false);
                if (chkPrintSponsorContract.Checked)
                    _action.PrintSponsorContracts(this, false);
				if (chkPrintMediaPlan.Checked)
					_action.PrintMediaPlan(Action.ActionMediaPlanType.Massmedias, false);
                if (chkPrintBillContract.Checked)
                    _action.PrintBillContracts(this, false);
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
				Application.UseWaitCursor = true;	
                Application.DoEvents();

                SetFormCaption();
				InitFormFromClass();
			}	
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
                Application.UseWaitCursor = false;
            }
		}

		private void tsbPrintMediaPlan_Click(object sender, EventArgs e)
		{
			if (SelectedCampaign != null)
				SelectedCampaign.PrintMediaPlan(false, false, false, false);
		}

        private void tsbSetActionPrice_Click(object sender, EventArgs e)
        {
            try
            {
				//if (_action.TotalPrice == 0) return;

                ActionFinalPriceForm form = new ActionFinalPriceForm(_action);
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    UseWaitCursor = true;
                    Application.DoEvents();

					DataTable dataTable = _action.Campaigns();
                    try
                    {
                        DataAccessor.BeginTransaction();
                        decimal distributedSoFar = 0m;
                        int rowCount = dataTable.Rows.Count;

                        for (int i = 0; i < rowCount; i++)
                        {
                            DataRow row = dataTable.Rows[i];
                            Campaign campaign = new Campaign(row);
                            decimal newP;

                            if (i == rowCount - 1)
                            {
                                // ПОСЛЕДНЯЯ СТРОКА: забирает всё, что осталось от целевой суммы
                                newP = form.FinalPrice - distributedSoFar;
                            }
                            else
                            {
                                // ОБЫЧНАЯ СТРОКА: считаем долю и жестко округляем до копеек
                                decimal rawP = form.IsManagerDiscount
                                    ? form.ManagerDiscount * campaign.Discount * campaign.PackDiscount * campaign.TariffPrice
                                    : form.FinalPrice * campaign.FullPrice / _action.TotalPrice;

                                newP = Math.Round(rawP, 2, MidpointRounding.AwayFromZero);
                                distributedSoFar += newP;
                            }

                            Debug.WriteLine($"Campaign {i}: {newP}");
                            campaign.SetFinalPrice(newP, form.SelectedDate, SecurityManager.LoggedUser.Id);
                        }
                        _action.Recalculate(refreshFlag: true, todayDate: form.SelectedDate	);
                        DataAccessor.CommitTransaction();
                    }
					catch  
					{
                        DataAccessor.RollbackTransaction();
						throw;
                    }
                    //action.Refresh();
                    grdCampaign.DataSource = _action.Campaigns(true).DefaultView;
                    RefreshActionStats(true);
                }
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private void MassEdit(object sender, EventArgs e)
        {
			try
			{
                EditIssuesForm form = new EditIssuesForm(_action.Firm, _action, grdCampaign.ItemsCount);
                form.ShowDialog(this);
                LoadCampaigns();
                RefreshActionStats(false);
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }
    }
}