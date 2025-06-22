using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Classes;
using Merlin.Controls;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace Merlin.Forms
{
	internal partial class CampaignForm : Form, IMediaControlContainer
	{
		private readonly Campaign _campaign;
		private readonly MediaControl mediaControl;
		protected TariffGrid tariffGrid;
		private bool changeFlag;
		private IssueTemplate template;
		private SmartGrid packDetails;
        protected Firm _firm;

        internal CampaignForm()
		{
			mediaControl = new MediaControl(this);
			InitializeComponent();
			toolStripButtonGrantor.Enabled = !(SecurityManager.LoggedUser != null && SecurityManager.LoggedUser.IsAdmin || IsSponsorCampaign);
			tbbPlay.Image = Globals.GetImage(Constants.ActionsImages.Play);
			tsbStop.Image = Globals.GetImage(Constants.ActionsImages.Stop);
			tbbExcel.Image = Globals.GetImage(Constants.ActionsImages.ExportExcel);
			tbbRefresh.Image = Globals.GetImage(Constants.ActionsImages.Refresh);
			toolStripButtonGrantor.Image = Globals.GetImage(Constants.ActionsImages.User);
			tbbStart.Image = Globals.GetImage(Constants.ActionsImages.Properties);
			Icon = Globals.MdiParent.Icon;
			toolStripButtonGrantor.Visible = false;
        }

		internal CampaignForm(Campaign campaign, TariffGrid tariffGrid)	: this()
		{
			_campaign = campaign;
			_campaign.Refresh();
			_firm = campaign.Action.Firm;
			SetTariffGrid(tariffGrid);

			tbbModules.Image = IsPackModuleCampaign ? Globals.GetImage(Constants.ActionsImages.PackModule) 
					: Globals.GetImage(Constants.ActionsImages.Module);
		}

		protected void SetTariffGrid(TariffGrid tariffGrid)
		{
			this.tariffGrid = tariffGrid;
		}

		protected virtual Firm Firm
		{
			get { return _firm; }
		}

		private bool IsSimplelCampaign
		{
			[DebuggerStepThrough]
			get 
			{ 
				// Это либо простая кампания либо спонсорская, но редактируем ролики
				return _campaign != null && (_campaign.CampaignType == Campaign.CampaignTypes.Simple 
					|| (_campaign.CampaignType == Campaign.CampaignTypes.Sponsor && tariffGrid is RollerIssuesGrid3)) ; 
			}
		}

		private bool IsSponsorCampaign
		{
			[DebuggerStepThrough]
			get { return _campaign != null && _campaign.CampaignType == Campaign.CampaignTypes.Sponsor; }
		}

		private bool IsPackModuleCampaign
		{
			[DebuggerStepThrough]
			get { return _campaign != null && _campaign.CampaignType == Campaign.CampaignTypes.PackModule; }
		}

		private bool IsModuleCampaign
		{
			[DebuggerStepThrough]
			get { return _campaign != null && _campaign.CampaignType == Campaign.CampaignTypes.Module; }
		}

		internal bool ChangeFlag
		{
			get { return changeFlag; }
		}

		private TariffGridWithCampaignIssues RollerIssuesGrid
		{
			[DebuggerStepThrough]
			get { return tariffGrid as TariffGridWithCampaignIssues; }
		}

		private ProgramIssuesGrid2 SponsorIssuesGrid
		{
			get { return tariffGrid as ProgramIssuesGrid2; }
		}

		#region IMediaControlContainer Members

		public bool IsPlaying
		{
			set { tsbStop.Enabled = value; }
		}

		#endregion

		private void tbbStart_CheckedChanged(object sender, EventArgs e)
		{
			// if editing program issies and action is confirmed - select advert type
			if (tbbStart.Checked)
			{
				if (SponsorIssuesGrid != null && _campaign.Action.IsConfirmed)
				{
					Entity entity = EntityManager.GetEntity((int)Entities.AdvertTypeChild);
					SelectionForm form = new SelectionForm(entity, entity.GetContent().DefaultView, "Выбор предмета рекламы");
					if (form.ShowDialog(this) == DialogResult.OK)
					{
						SponsorIssuesGrid.AdvertType = form.SelectedObject;
					}
					else
					{
						MessageBox.ShowExclamation(Properties.Resources.AdvertTypeMastBeSelected);
						tbbStart.Checked = false;
						return;
					}
				}
			}
			else
			{
				if (SponsorIssuesGrid != null)
					SponsorIssuesGrid.AdvertType = null;
            }

            SetEditMode();
        }

		private void SetEditMode()
		{
			tariffGrid.EditMode = tbbTemplate.Checked ? EditMode.Template : (tbbStart.Checked) ? EditMode.Edit : EditMode.View;
		}

		protected virtual void ProcessToolbar()
		{
			tsbMuteRoller.Enabled = IsSimplelCampaign;
			tsbMuteRoller.Visible = tbbPosition.Visible = tbbPlay.Visible = tsbStop.Visible = toolStripSeparator3.Visible = !(tariffGrid is ProgramIssuesGrid2);
            tbbAdvertType.Visible = !(tariffGrid is ProgramIssuesGrid2) && !(tariffGrid is PackModuleGrid);
            tbbModules.Visible = IsModuleCampaign || IsPackModuleCampaign;
			if (IsPackModuleCampaign)
				tbbModules.Text = "Выбор пакета";

            tbbTemplate.Visible = tbbTemplate2.Visible = !(IsModuleCampaign || IsPackModuleCampaign || tariffGrid is ProgramIssuesGrid2);
		}

		private void InitModulesList()
		{
			if(tbbModules.Visible)
			{
				ToolStripDropDown dropDown = new ToolStripDropDown();
				DateTime currentDate = tariffGrid.CurrentDate;
				bool isFind = false;
				_campaign.ClearModuleList();
				foreach(DataRow row in GetModuleList(currentDate))
				{
					PresentationObject module = CreateModule(row);
                    ToolStripButton item = new ToolStripButton(row["name"].ToString(), null, ModuleSelected, module.IDs[0].ToString())
                    {
                        Tag = module
                    };
                    if (_campaign.HasModuleIssue(module))
						item.Font = new Font(item.Font, FontStyle.Bold);
					dropDown.Items.Add(item);

					if (((IRollerGrid)tariffGrid).Module != null && ((IsModuleCampaign && ((IRollerGrid)tariffGrid).Module["moduleID"].ToString() == row["moduleID"].ToString()) 
						|| (IsPackModuleCampaign && ((IRollerGrid)tariffGrid).Module["packModuleID"].ToString() == row["packModuleID"].ToString())))
						isFind = true;
				}
				tbbModules.DropDown = dropDown;
				if (!isFind)
				{
					InitModule(null, tbbModules.Text, false);
				}
			}
		}

		private DataRowCollection GetModuleList(DateTime date)
		{
			if(IsModuleCampaign)
				return ((CampaignOnSingleMassmedia)_campaign).Massmedia.GetModules(date).Rows;
			if(IsPackModuleCampaign)
				return PackModule.LoadModules(date).Rows;
			return null;
		}

		private PresentationObject CreateModule(DataRow row)
		{
			if(IsPackModuleCampaign) return new PackModule(row);
			return new Module(row);
		}

		private void ModuleSelected(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				ToolStripButton button = (ToolStripButton)sender;
				InitModule(button.Tag, button.Text, true);
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

		private void InitModule(object module, string title, bool fNeedRefresh)
		{
			tariffGrid.Clear();
            IRollerGrid rollerGrid = (IRollerGrid)tariffGrid;

			if (rollerGrid.RollerPosition != RollerPositions.Undefined)
				tbbPosition.DropDownItems[0].PerformClick();

            ((IRollerGrid)tariffGrid).Module = (PresentationObject)module;
			if (fNeedRefresh)
				tariffGrid.RefreshGrid();

			tbbModules.Text = title;
			if(tariffGrid.Pricelist != null)
			{
                tbbPosition.Enabled = !tariffGrid.Pricelist.CheckTariffWithMaxCapacity();
				tsbMuteRoller.Enabled = !tariffGrid.Pricelist.HasRollerAssigned;
            }
        }

		protected virtual void SetFormCaption()
		{
            if (_campaign is CampaignOnSingleMassmedia smc)
                Text = string.Format("Просмотр информации на радиостанции {0}", smc.MassmediaNameWithGroup);
            else
                Text = "Пакетная модульная рекламная кампания";
        }

		private void SetTariffGrid()
		{
			tariffGrid.Dock = DockStyle.Fill;
			tariffGrid.Name = "tariffGrid";

			splitContainer5.Panel1.Controls.Add(tariffGrid);
			splitContainer5.Panel2Collapsed = !IsPackModuleCampaign;

            if (tariffGrid is TariffGridWithCampaignIssues grid)
            {
                grid.Campaign = _campaign;
				grid.ShowMessages = true;
                if (IsSimplelCampaign || IsSponsorCampaign)
                    grid.RefreshGrid();
            }

            SetEventHandlersFromGridEvents(tariffGrid);

			if (IsPackModuleCampaign)
			{
				InitDetailsGrid();
			}
		}

		private void InitDetailsGrid()
		{
			packDetails = new SmartGrid
			              	{
			              		Dock = DockStyle.Fill,
			              		Name = "packDetails",
			              		CaptionVisible = true,
			              		Caption = "Детальная информация по радиостанции",
			              		MenuEnabled = false,
			              		QuickSearchVisible = false
			              	};
			packDetails.InternalGrid.ReadOnly = true;
			splitContainer5.Panel2.Controls.Add(packDetails);
		}

		private void SetEventHandlersFromGridEvents(TariffGrid grid)
		{
			grid.CampaignStatusChanged += CampaignStatusChanged;
			grid.CellClicked += grid_CellClicked;
			grid.GridRefreshed += delegate
														{
															if (IsPackModuleCampaign || IsModuleCampaign)
															{
																InitModulesList();
																InitRollersList();
															}
															grdCurrentCampaignIssues.Clear();
															grdIssues.Clear();
															if (packDetails != null)
																packDetails.Clear();
															SetStatus(null);
														};
		}

		private void InitRollersList()
		{
			if(tariffGrid is ProgramIssuesGrid2)
			{
				grdRollers.Caption = "Программы";
				grdRollers.Entity = SponsorProgram.GetEntity();
				grdRollers.DataSource =	((CampaignOnSingleMassmedia)_campaign).Massmedia.GetSponsorPrograms(true).DefaultView;
			}
			else
			{
				//Entity rollerEntity = (Entity)Roller.GetEntity().Clone();
				//rollerEntity.AttributeSelector = (int)Roller.AttributeSelectors.NameOnly;
				grdRollers.Entity = EntityManager.GetEntity((int)Entities.ActionRollers); // rollerEntity;
				SetRollerDataSource();
				EnableEditButtons();
			}
		}

		private void EnableEditButtons()
		{
            tbbTemplate.Enabled = tbbTemplate2.Enabled = tbbPlay.Enabled = tbbStart.Enabled = grdRollers.InternalGrid.RowCount > 0;
        }

		private void SetRollerDataSource()
		{
			PresentationObject selected = grdRollers.SelectedObject;
			
			if ((IsPackModuleCampaign || IsModuleCampaign) && ((IRollerGrid)tariffGrid).Module != null)
			{
				Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
				if (tariffGrid.StartDate == DateTime.MinValue || tariffGrid.FinishDate == DateTime.MinValue)
					return;
				parameters["startDate"] = tariffGrid.StartDate;
				parameters["finishDate"] = tariffGrid.FinishDate;
                parameters[Campaign.ParamNames.CampaignId] = _campaign.CampaignId;
                if (IsPackModuleCampaign)
					parameters[PackModule.ParamNames.PackModuleId] = ((IRollerGrid) tariffGrid).Module.IDs[0];
				else if (IsModuleCampaign)
					parameters[Module.ParamNames.ModuleId] = ((IRollerGrid) tariffGrid).Module.IDs[0];
				DataSet data = DataAccessor.LoadDataSet("RollersForModule", parameters);
				if (data.Tables.Count > 0 && data.Tables[0].Rows.Count > 0)
				{
					// если нашли ролик, это значит для модуля или пакета назначен ролик "для всех"
					grdRollers.Entity = EntityManager.GetEntity((int)Entities.CommonRollers);
                    grdRollers.DataSource = data.Tables[0].DefaultView;
					return;
				}
			}

            grdRollers.DataSource = Firm.GetRollers().DefaultView;
            if (grdRollers.Contains(selected))
			{
				grdRollers.SelectedObject = selected;
				grdRollers.Select();
			}
		}

		private void CampaignStatusChanged()
		{
			try
			{
				//campaign.RecalculateAction();
				_campaign.Refresh();
				_campaign.DisplayCampaignData(lstStat);
				changeFlag = true;
				HighlightSelectedModule();
				RefreshDetails(tariffGrid.CurrentTariffWindow);
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void HighlightSelectedModule()
		{
			if(IsModuleCampaign || IsPackModuleCampaign)
			{
				ToolStripItem item = tbbModules.DropDown.Items[((IRollerGrid)tariffGrid).Module.IDs[0].ToString()];
				if(_campaign.HasModuleIssue(((IRollerGrid)tariffGrid).Module))
					item.Font = new Font(item.Font, FontStyle.Bold);
				else
					item.Font = new Font(item.Font, FontStyle.Regular);
			}
		}

		private void grid_CellClicked(ITariffWindow tariffWindow)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				if (tariffWindow != null && tariffGrid.EditMode == EditMode.Template)
				{
					template.SetTime(tariffWindow.WindowDate);
					template.Parameters["Window"] = tariffWindow.WindowDate.TimeOfDay;
					if(Globals.ShowQuestion("StartIssueGeneration", template.Parameters) == DialogResult.Yes)
					{
						FrmGenerator form;
						if (tariffGrid is ProgramIssuesGrid2)
						{
							form =
								new FrmGenerator(template, _campaign, SponsorIssuesGrid.SponsorProgram,
								                 tariffWindow.TariffId, tariffWindow.Price,
								                 ((SponsorPricelist) SponsorIssuesGrid.Pricelist).Bonus);
						}
						else
						{
							form =
								new FrmGenerator(template, ((IRollerGrid) tariffGrid).Roller, RollerIssuesGrid.RollerPosition,
								_campaign, RollerIssuesGrid.Pricelist, ((IRollerGrid)tariffGrid).Module, Grantor == null ? null : (int?)Grantor.Id);
						}

						form.ShowDialog(this);

						if(template.IsDateCovered(tariffGrid.StartDate, tariffGrid.FinishDate))
						{
							RefreshGrid();
						}
						CampaignStatusChanged();
					}
				}

				ShowWindowIssues(tariffWindow);
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		protected virtual void ShowWindowIssues(ITariffWindow tariffWindow)
		{
			grdIssues.Clear();
			grdCurrentCampaignIssues.Clear();

			if (tariffWindow != null && tariffGrid.IsActiveCellSelected && tariffGrid.IssueEntity != null)
			{
				Entity issueEntity = (Entity)tariffGrid.IssueEntity.Clone();
				if(tariffGrid is ProgramIssuesGrid2)
				{
					issueEntity.AttributeSelector = (int)ProgramIssue.AttributeSelectors.FirmNameOnly;
					grdIssues.Entity = issueEntity;
					grdIssues.DataSource =
						((TariffWindowWithProgramIssue)tariffWindow).LoadIssues(RollerIssuesGrid.ShowUnconfirmed, issueEntity).DefaultView;
				}
				else
				{
					Entity currentCampaignIssueEntity = (Entity)issueEntity.Clone();
					currentCampaignIssueEntity.AttributeSelector = Issue.AttributeSelectorShort;
					grdCurrentCampaignIssues.Entity = currentCampaignIssueEntity;
					DataTable dtIssues = tariffWindow.LoadIssues(RollerIssuesGrid.ShowUnconfirmed, currentCampaignIssueEntity);
					if (dtIssues.Rows.Count > 0)
						grdCurrentCampaignIssues.DataSource = GetCurrentCampaignIssues(dtIssues);

					grdIssues.Entity = (Entity)EntityManager.GetEntity((int)Entities.Issue).Clone();
					grdIssues.Entity.AttributeSelector = Issue.AttributeSelectorFull;

					if (issueEntity.Id == (int)Entities.ModuleIssue)
						dtIssues = tariffWindow.LoadIssues(RollerIssuesGrid.ShowUnconfirmed, grdIssues.Entity);
					if (dtIssues.Rows.Count > 0)
						grdIssues.DataSource = dtIssues.DefaultView;
				}
			}
			SetStatus(tariffWindow);

			RefreshDetails(tariffWindow);
		}

		private void SetStatus(ITariffWindow tariffWindow)
		{
			toolStripStatusLabelSelected.Text =
				(tariffWindow != null && tariffGrid.IsActiveCellSelected)
					? string.Format("Выбранное окно: '{0}'",
					                tariffWindow.WindowDate.ToString(IsPackModuleCampaign ? "dd.MM.yyyy" : "dd.MM.yyyy HH:mm"))
					: "Окно не выбрано";
		}

		private void RefreshDetails(ITariffWindow tariffWindow)
		{
			if (IsPackModuleCampaign && packDetails != null)
			{
				packDetails.Clear();
				if (tariffWindow != null)
				{
					packDetails.DataSource = ((TariffWindowPackModule) tariffWindow).GetDetails(RollerIssuesGrid.ShowUnconfirmed);
				}
			}
		}

		private DataView GetCurrentCampaignIssues(DataTable dtIssues)
		{
			DataTable dtCurrentIssues = dtIssues.Clone();
			foreach(DataRow row in dtIssues.Select(string.Format("campaignId = {0}", _campaign.CampaignId)))
				dtCurrentIssues.ImportRow(row);
			return dtCurrentIssues.DefaultView;
		}

		protected void RefreshGrid()
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				tariffGrid.RefreshGrid();
				//if(bDoClick) grid_CellClicked(tariffGrid.CurrentTariffWindow);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void grdRollers_ObjectSelected(PresentationObject presentationObject)
		{
			try
			{
				IRollerGrid rollerGrid = tariffGrid as IRollerGrid;
				if (rollerGrid != null)
				{
					rollerGrid.Roller = new Roller(((ActionRoller)presentationObject).RollerId);
				}
				else
				{
					SponsorIssuesGrid.SponsorProgram = (SponsorProgram)presentationObject;
					tariffGrid.RefreshGrid();
				}
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void tbbPosition_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				tbbPosition.Text = e.ClickedItem.Text;
				IRollerGrid rollerGrid = (IRollerGrid)tariffGrid;

				rollerGrid.RollerPosition =
						(RollerPositions)Enum.Parse(typeof(RollerPositions), e.ClickedItem.Tag.ToString());
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void tbbShowUnconfirmed_Click(object sender, EventArgs e)
		{
			try
			{
				tariffGrid.ShowUnconfirmed = tbbShowUnconfirmed.Checked;
                if (tariffGrid is IRollerGrid grid && (grid.Module == null && (IsPackModuleCampaign || IsModuleCampaign))) return;

                RefreshGrid();
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void tbbRefresh_Click(object sender, EventArgs e)
		{
			try
			{
				tbbStart.Checked = false;
                RefreshGrid();
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void grdCurrentCampaignIssues_ObjectDeleted(PresentationObject presentationObject)
		{
			try
			{
				_campaign.RecalculateAction();
				((IRollerGrid)tariffGrid).RefreshCurrentCell(grdCurrentCampaignIssues.ItemsCount > 0, TariffGridRefreshMode.WithDelete);
				ShowWindowIssues(tariffGrid.CurrentTariffWindow);

				CampaignStatusChanged();
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void tbbPlay_Click(object sender, EventArgs e)
		{
			mediaControl.Play(grdRollers.SelectedObject);
		}

		private void tsbStop_Click(object sender, EventArgs e)
		{
			mediaControl.Stop();
		}

		private void tbbJump_Click(object sender, EventArgs e)
		{
			try
			{
				if(!tariffGrid.Jump2Date()) return;

				IRollerGrid rollerGrid = tariffGrid as IRollerGrid;
				if (rollerGrid != null && rollerGrid.Module == null && (IsPackModuleCampaign || IsModuleCampaign))
				{
					InitModulesList();
					InitRollersList();
					return;
				}

				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				tariffGrid.RefreshGrid();
				if(IsPackModuleCampaign || IsModuleCampaign) InitRollersList();
				
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void tbbTemplate_Click(object sender, EventArgs e)
		{
			try
			{
				if(tbbTemplate.Checked)
				{
					FrmTemplate form = new FrmTemplate(template);
					if(form.ShowDialog(this) != DialogResult.OK)
						tbbTemplate.Checked = false;
					else
						template = form.Template;
				}
				SetEditMode();
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void tbbTemplate2_Click(object sender, EventArgs e)
		{
			try
			{
				Roller roller = ((IRollerGrid)tariffGrid).Roller;
				if(roller == null)
				{
                    FogSoft.WinForm.Forms.MessageBox.ShowExclamation(MessageAccessor.GetMessage("RollerNotSelected"));
                    return;
				}

                FrmTemplate2 formTemplate = new FrmTemplate2(roller.Name);

				if (formTemplate.ShowDialog(this) == DialogResult.OK)
				{
					template = formTemplate.Template;
	
					FrmGenerator form = new FrmGenerator(template, roller, RollerIssuesGrid.RollerPosition,
					_campaign, null, ((IRollerGrid)tariffGrid).Module, Grantor == null ? null : (int?)Grantor.Id);

					form.ShowDialog(this);
					Application.DoEvents();
					Cursor = Cursors.WaitCursor;

					if (template.IsDateCovered(tariffGrid.StartDate, tariffGrid.FinishDate))
						RefreshGrid();

					CampaignStatusChanged();
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
		
		private void tbbExcel_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				ExportManager.ExportExcel(tariffGrid.InternalGrid, null);
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void CampaignForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			mediaControl.Stop();
		}

		private void CampaignForm_Load(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

                ProcessToolbar();
                SetFormCaption();
				SetTariffGrid();

				if (!(tariffGrid is IRollerGrid))
					HideGridWithCurrentIssues();

				
				InitModulesList();
				if (!IsModuleCampaign && !IsPackModuleCampaign)
					InitRollersList();

				if (RollerIssuesGrid != null)
				{
					RollerIssuesGrid.IsPopUpMenuAllowed = false;
					RollerIssuesGrid.ExcludeSpecialTariffs = true;
					RollerIssuesGrid.ShowUnconfirmed = tbbShowUnconfirmed.Checked;
				}

				if (_campaign != null)
					_campaign.DisplayCampaignData(lstStat);

                grdRollers.ObjectDeleted += GrdRollers_ObjectDeleted;
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

        private void GrdRollers_ObjectDeleted(PresentationObject presentationObject)
        {
            EnableEditButtons();	
        }

        protected void HideGridWithCurrentIssues()
		{
			splitContainer4.Panel2Collapsed = true;			
		}

		private void toolStripButtonGrantor_Click(object sender, EventArgs e)
		{
			/*
			if (Grantor == null)
				Grantor = Utils.AskConfirmation(this, toolStripButtonGrantor.ToolTipText, "Введите логин и пароль привелигированного пользователя.");
			else
				Grantor = null;
			toolStripButtonGrantor.Checked = (Grantor != null);		
			*/
		}

		private SecurityManager.User Grantor
		{
			get { return ((IRollerGrid) tariffGrid).Grantor; }
			set { ((IRollerGrid) tariffGrid).Grantor = value; }
		}

		private void grdCurrentCampaignIssues_ObjectChanged(PresentationObject presentationObject)
		{
			try
			{
				_campaign.RecalculateAction();
				((IRollerGrid)tariffGrid).RefreshCurrentCell(true, TariffGridRefreshMode.None);
                CampaignStatusChanged();
				ShowWindowIssues(tariffGrid.CurrentTariffWindow);
            }
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void tsbMuteRoller_Click(object sender, EventArgs e)
		{
			CreateMuteRoller();
        }

		private void CreateMuteRoller()
		{
			try
			{
				RollerMuteSelect frm = new RollerMuteSelect();
				if (frm.ShowDialog(this) == DialogResult.OK)
				{
					if (frm.TimeDuration > 0)
					{
						Cursor.Current = Cursors.WaitCursor;
						PresentationObject newRoller = MuteRoller.GetRoller(frm.TimeDuration, Firm.FirmId, null);
						ActionRoller roller = new ActionRoller(newRoller);

                        grdRollers.AddRow(roller);
						grdRollers.SelectedObject = roller;
						EnableEditButtons();

                    }
				}
			}
			finally { Cursor.Current = Cursors.Default; }
        }

        private void tbbAdvertType_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            try
            {
                Application.DoEvents();
                Cursor = Cursors.WaitCursor;

                
                IRollerGrid rollerGrid = (IRollerGrid)tariffGrid;

				AdvertTypePresences presence = (AdvertTypePresences)Enum.Parse(typeof(AdvertTypePresences), e.ClickedItem.Tag.ToString());
				if (presence != AdvertTypePresences.Undefined)
				{
					TreeViewSelector tvSelector = new TreeViewSelector(RelationManager.GetScenario(RelationScenarios.AdvertTypes), "Предметы рекламы");
					if (tvSelector.ShowDialog(Parent) == DialogResult.OK)
					{
						Application.DoEvents();
						Cursor = Cursors.WaitCursor;
						rollerGrid.SetAdvertTypePresence(presence, tvSelector.SelectedObject);
						tbbAdvertType.Text = (presence == AdvertTypePresences.Exist ? "Есть: " : "Нет: ") + tvSelector.SelectedObject.Name;
					}
				}
				else
				{
					rollerGrid.SetAdvertTypePresence(presence, null);
                    tbbAdvertType.Text = e.ClickedItem.Text;
                }
            }
            finally
            {
               Cursor = Cursors.Default;
            }
        }
    }
}