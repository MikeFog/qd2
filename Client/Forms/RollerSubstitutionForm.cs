using System;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.Passport.Classes;
using FogSoft.WinForm.Passport.Forms;
using FogSoft.WinForm.Properties;
using Merlin.Classes;
using FogSoft.WinForm.Forms;

namespace Merlin.Forms
{
	internal partial class RollerSubstitutionForm : PassportForm
	{
		private readonly RollerSubstitution substitution;
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
			substitution = new RollerSubstitution(campaign, roller, moduleID, packModuleID);
			btnApply.Visible = false;
			DataSet ds = substitution.LoadPassportData();
			pageContext = new PageContext(ds, substitution.CreatePassportParameters(ds));
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

		protected override void ApplyChanges(Button clickedButton)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

                TreeView2 treeDays = FindControl("days") as TreeView2;
                selectedDays = RollerSubstitution.SelectDays(treeDays.DataSource, treeDays.AddedIDs);

				if(selectedDays.Rows.Count == 0)
				{
                    DialogResult = DialogResult.None;
                    UserMessage.ShowExclamation(Properties.Resources.NoIssueSelected);
                    return;
                }

                newRoller = GetNewRoller();
				if (newRoller == null)
				{
                    DialogResult = DialogResult.None;
                    return;
				}

				string message = substitution.ValidateNewRoller(newRoller);
				if (message != null)
				{ 
					DialogResult = DialogResult.None;
                    UserMessage.ShowExclamation(message);
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
				int? advertTypeId = opAdvertType.SelectedObject == null
					? null : (int?)int.Parse(opAdvertType.SelectedObject.IDs[0].ToString());

				string message = substitution.ValidateMuteRoller(advertTypeId, tdMuteRoller.Value);
				if (message != null)
				{
                    UserMessage.ShowExclamation(message);
                    return null;
                }

				return substitution.CreateMuteRoller(tdMuteRoller.Value, advertTypeId);
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