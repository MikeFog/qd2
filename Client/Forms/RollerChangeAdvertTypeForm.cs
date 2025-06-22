using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
	internal partial class RollerChangeAdvertTypeForm : PassportForm
	{
		private readonly Campaign campaign;
		private readonly Roller roller;
		private List<DateTime> selectedDays;
		private ObjectPicker2 opAdvertType;
		private int _advertTypeId;

		public RollerChangeAdvertTypeForm()
		{
			InitializeComponent();
		}

		public RollerChangeAdvertTypeForm(Roller roller, Campaign campaign, int? moduleID, int? packModuleID)
			: base(PassportLoader.Load("ChangeAdvertType"))
		{
			this.roller = roller;
			this.campaign = campaign;
			btnApply.Visible = false;
			DataSet ds = LoadData(moduleID, packModuleID);
			pageContext = new PageContext(ds, CreateParameters(ds));
			Text = "Замена ролика";
		}

		public List<DateTime> SelectedDays
		{
			get { return selectedDays; }
		}

		public int AdvertTypeId
		{
			get { return _advertTypeId; }
		}

		protected override void OnLoad(EventArgs e)
		{
			try
			{
				Cursor.Current = Cursors.WaitCursor;
				base.OnLoad(e);

				opAdvertType = FindControl("advertTypeID") as ObjectPicker2;
			}
			finally { Cursor.Current = Cursors.Default; }
		}

		private Dictionary<string, object> CreateParameters(DataSet ds)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["rollerName"] = roller.Name;
			parameters["duration"] = roller.DurationString;
            parameters["advertTypeName"] = roller.AdvertTypeName;
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
                if (opAdvertType.SelectedObject == null)
                {
                    DialogResult = DialogResult.None;
                    FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Properties.Resources.AdvertTypeNotSelected);
					return;
                }

				_advertTypeId = int.Parse(opAdvertType.SelectedObject.IDs[0].ToString());

                Application.DoEvents();
				Cursor = Cursors.WaitCursor;

                TreeView2 treeDays = FindControl("days") as TreeView2;
				selectedDays = new List<DateTime>();

                foreach (object id in treeDays.AddedIDs)
                {
					if (id != null)
						selectedDays.Add(DateTime.Parse(id.ToString()));
                }

                if (selectedDays.Count == 0)
				{
                    DialogResult = DialogResult.None;
                    FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Properties.Resources.NoIssueSelected);
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
	}
}