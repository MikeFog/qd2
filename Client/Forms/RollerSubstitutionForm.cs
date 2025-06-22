using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Classes;
using FogSoft.WinForm.Passport.Forms;
using FogSoft.WinForm.Properties;
using Merlin.Classes;

namespace Merlin.Forms
{
	internal partial class RollerSubstitutionForm : PassportForm
	{
		private readonly Campaign campaign;
		private readonly Roller roller;
		private Roller newRoller;
		private DataTable selectedDays;
		private CheckBox cbSubtituteOnMute;
		private TimeDuration tdMuteRoller;
		private LookUp luRollers;
		private ObjectPicker2 opAdvertType;

		public RollerSubstitutionForm()
		{
			InitializeComponent();
		}

		public RollerSubstitutionForm(Roller roller, Campaign campaign, int? moduleID, int? packModuleID)
			: base(PassportLoader.Load("RollerSubstitute"))
		{
			this.roller = roller;
			this.campaign = campaign;
			btnApply.Visible = false;
			DataSet ds = LoadData(moduleID, packModuleID);
			pageContext = new PageContext(ds, CreateParameters(ds));
			Text = "Замена ролика";
		}

		public Roller NewRoller
		{
			get { return newRoller; }
		}

		public DataTable SelectedDays
		{
			get { return selectedDays; }
		}

		protected override void OnLoad(EventArgs e)
		{
			try
			{
				Cursor.Current = Cursors.WaitCursor;
				base.OnLoad(e);

				tdMuteRoller = FindControl("rollerMuteDuration") as TimeDuration;
				cbSubtituteOnMute = FindControl("subtituteMute") as CheckBox;
				luRollers = FindControl("rollerID") as LookUp;
				opAdvertType = FindControl("advertTypeID") as ObjectPicker2;
				cbSubtituteOnMute.CheckedChanged += cbSubtituteOnMute_CheckedChanged;
				cbSubtituteOnMute.Checked = false;
				UpdateControlsStatus();
			}
			finally { Cursor.Current = Cursors.Default; }
		}

		private void cbSubtituteOnMute_CheckedChanged(object sender, EventArgs e)
		{
			UpdateControlsStatus();
		}

		private void UpdateControlsStatus()
		{
			bool hasRollers = luRollers.SelectedValue != null;
			cbSubtituteOnMute.Enabled = hasRollers;
			if (!hasRollers)
				cbSubtituteOnMute.Checked = true;

			tdMuteRoller.Enabled = cbSubtituteOnMute.Checked;
			luRollers.Enabled = !cbSubtituteOnMute.Checked && hasRollers;
		}

		private Dictionary<string, object> CreateParameters(DataSet ds)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["rollerName"] = roller.Name;
			parameters["duration"] = roller.DurationString;
            parameters["issues"] = ds.Tables["quantity"].Rows[0]["issues"];
			return parameters;
		}

		private DataSet LoadData(int? moduleID, int? packModuleID)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				EntityManager.GetEntity((int) Entities.CampaignRoller),
				InterfaceObjects.PropertyPage, Constants.Actions.Substitute);

			procParameters[Roller.ParamNames.RollerId] = roller.RollerId;
			procParameters[Campaign.ParamNames.CampaignId] = campaign.CampaignId;
			procParameters[Campaign.ParamNames.CampaignTypeId] = (int)campaign.CampaignType;
			if (moduleID.HasValue)
				procParameters["moduleID"] = moduleID;
			if (packModuleID.HasValue)
				procParameters["packModuleID"] = packModuleID;
			return DataAccessor.DoAction(procParameters) as DataSet;
		}

		protected override void ApplyChanges(Button clickedButton)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

                TreeView2 treeDays = FindControl("days") as TreeView2;
                selectedDays = treeDays.DataSource.Clone();
                foreach (DataRow row in treeDays.DataSource.Rows)
                {
                    if (treeDays.AddedIDs.Contains(row["id"]))
                        selectedDays.Rows.Add(row.ItemArray);
                }

				if(selectedDays.Rows.Count == 0)
				{
                    DialogResult = DialogResult.None;
                    FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Properties.Resources.NoIssueSelected);
                    return;
                }

                newRoller = GetNewRoller();
				if (newRoller == null)
				{
                    DialogResult = DialogResult.None;
                    return;
				}

				if(!newRoller.HasAdvertType && campaign.Action.IsConfirmed) 
				{ 
					DialogResult = DialogResult.None;
                    FogSoft.WinForm.Forms.MessageBox.ShowExclamation(MessageAccessor.GetMessage("WrongRollerForSubstitution"));
                    return;
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

		private Roller GetNewRoller()
		{
			if (cbSubtituteOnMute.Checked)
			{
				// если это активированная акция, то для "пустышки" обязательно надо указать предмет рекламы
				if(opAdvertType.SelectedObject == null && campaign.Action.IsConfirmed)
				{
                    FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Properties.Resources.SubstitutionImpossibleForDummyRoller);
                    return null;
                }

				if(tdMuteRoller.Value == 0)
				{
                    FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Properties.Resources.DummyRollerWithZeroDuration);
                    return null;
                }

				return MuteRoller.GetRoller(tdMuteRoller.Value, campaign.Action.FirmID, 
					opAdvertType.SelectedObject == null ? null : (int?)int.Parse(opAdvertType.SelectedObject.IDs[0].ToString()));
			}
			else 
			{
				if (luRollers == null || luRollers.SelectedValue == null)
					return null;
				return new Roller(int.Parse(luRollers.SelectedValue.ToString()));
			}
		}
	}
}