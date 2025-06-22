using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using Merlin.Controls;

namespace Merlin.Forms
{
	public partial class RollerStatisticForm : Form, IMediaControlContainer
	{
		private readonly Entity entityAction;
		private readonly MediaControl mediaControl;

		public RollerStatisticForm(bool setuser)
		{
			try
			{
                mediaControl = new MediaControl(this);
				InitializeComponent();
                Cursor.Current = Cursors.WaitCursor;

                grid.Entity = EntityManager.GetEntity((int)Entities.RollerStatistic).Clone() as Entity;
				entityAction = EntityManager.GetEntity((int)Entities.Action);
				grdActions.Entity = entityAction;
				grdMassmedia.Entity = EntityManager.GetEntity((int)Entities.MassMedia);
				grdMassmedia.Entity.AttributeSelector = (int)Massmedia.AttributeSelectors.NameAndGroupOnly;
				objectPickerUser.IsCreateNewAllowed = false;
				objectPickerFirm.IsCreateNewAllowed = false;
				opAdvertType.IsCreateNewAllowed = false;

				opAdvertType.Scenario = RelationManager.GetScenario(RelationScenarios.AdvertTypes);

                objectPickerFirm.SetEntity(EntityManager.GetEntity((int)Entities.Firm));

				InitMassmediaGroups();

				Dictionary<string, object> procParams = DataAccessor.CreateParametersDictionary();
				DataSet ds = DataAccessor.LoadDataSet("UserListByRights", procParams);
				objectPickerUser.SetDataSource(EntityManager.GetEntity((int)Entities.User), ds.Tables[0]);
				bool hasRight = SecurityManager.LoggedUser.IsRightToViewForeignActions()
							   || SecurityManager.LoggedUser.IsRightToViewGroupActions();
				if (setuser || !hasRight)
					objectPickerUser.SelectObject(SecurityManager.LoggedUser.Id);
				objectPickerUser.Enabled = hasRight;

				tbbPlay.Image = Globals.GetImage(Constants.ActionsImages.Play);
				tsbStop.Image = Globals.GetImage(Constants.ActionsImages.Stop);
				tsbExcel.Image = Globals.GetImage(Constants.ActionsImages.ExportExcel);
				tsbRefresh.Image = Globals.GetImage(Constants.ActionsImages.Refresh);
				tsbSplitByManager.Image = Globals.GetImage(Constants.ActionsImages.User);
				tsbSplitByDays.Image = Globals.GetImage(Constants.ActionsImages.Day);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void PopulateRadiostationsList()
		{
			if (grdMassmedia.Entity != null)
				Massmedia.LoadRadiostationsByGroup(cmbRadioStationGroup, grdMassmedia);
		}

		private void InitMassmediaGroups()
		{
			cmbRadioStationGroup.ColumnWithID = "massmediaGroupID";
			cmbRadioStationGroup.DataSource = Massmedia.LoadGroupsWithShowAllOption();		
		}

		private void tsbRefresh_Click(object sender, EventArgs e)
		{
			RefreshData();
		}

		private void RefreshData()
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				grdActions.Clear();
				grid.Clear();

				
				Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(grid.Entity);
				procParameters["massmediaString"] = BuildMassmediaString();
				if (StringUtil.IsNullOrEmpty(procParameters["massmediaString"].ToString())) return;

				procParameters["startDate"] = dtStart.Value.Date;
				procParameters["finishDate"] = dtFinish.Value.Date;
				procParameters["ShowWhite"] = checkBoxShowWhite.Checked;
				procParameters["ShowBlack"] = checkBoxShowBlack.Checked;
				procParameters["splitByManager"] = tsbSplitByManager.Checked;
				procParameters["splitByDays"] = tsbSplitByDays.Checked;
				if (objectPickerUser.SelectedObject != null)
					procParameters[SecurityManager.ParamNames.UserId] = objectPickerUser.SelectedObject.IDs[0];
				if (objectPickerFirm.SelectedObject != null)
					procParameters["firmID"] = objectPickerFirm.SelectedObject.IDs[0];
                if (opAdvertType.SelectedObject != null)
                    procParameters["advertTypeID"] = opAdvertType.SelectedObject.IDs[0];

                DataSet ds = DataAccessor.DoAction(procParameters) as DataSet;

				if (!tsbSplitByDays.Checked)
					grid.DataSource = ds.Tables[0].DefaultView;
				else
					grid.DataSource = CreateDataTableWithDates(ds);
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

		private DataView CreateDataTableWithDates(DataSet ds)
		{
			DataTable dtData = ds.Tables[0];
			DataTable dtRawData = ds.Tables[1];
			DataTable dtDays = ds.Tables[2];

			// Modify DataTable with data and entity attrivutes
			foreach (DataRow rowDay in dtDays.Rows)
			{
				DateTime date = DateTime.Parse(rowDay["date"].ToString());
				DataColumn column = dtData.Columns.Add(date.ToShortDateString(), Type.GetType("System.Int32"));
				PopulateDayColumn(column, date, dtData, dtRawData);
				grid.Entity.SortedAttributes.Add(
					new Entity.Attribute(date.ToShortDateString(), date.ToShortDateString(), "System.Int32"));
			}

			return dtData.DefaultView;
		}

		private void PopulateDayColumn(DataColumn column, DateTime date, DataTable dtData, DataTable dtRawData)
		{
			string filter;

			foreach (DataRow row in dtData.Rows)
			{
				// Find Count for given roller and given date
				int rollerID = int.Parse(row["rollerID"].ToString());
				if (!tsbSplitByManager.Checked)
					filter = string.Format("rollerID = {0} and date='{1}'", rollerID, date.ToShortDateString());
				else
				{
					int userID = int.Parse(row[SecurityManager.ParamNames.UserId].ToString());
					filter =
						string.Format("rollerID = {0} and date='{1}' and userID = {2}", rollerID, date.ToShortDateString(), userID);
				}

				row[column] = dtRawData.Compute("Count(rollerID)", filter);
			}
		}

		private string BuildMassmediaString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (PresentationObject po in grdMassmedia.Added2Checked)
				sb.AppendFormat("{0},", po.IDs[0]);
			return sb.ToString();
		}

		private void tsbSetting_Click(object sender, EventArgs e)
		{
			grid.Clear();
			grdActions.Clear();
		}

		private void tsbExcel_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				ExportManager.ExportExcel(grid.InternalGrid, grid.Entity);
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

		private void grid_ObjectSelected(PresentationObject presentationObject)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				// Display actions where selected Roller is used
				Dictionary<string, object> procParameters = DataAccessor.
					PrepareParameters(entityAction, InterfaceObjects.SimpleJournal, "LoadForRollerStatistic");

				procParameters["startDate"] = dtStart.Value.Date;
				procParameters["finishDate"] = dtFinish.Value.Date;
				procParameters["massmediaString"] = BuildMassmediaString();
				procParameters["rollerID"] = presentationObject[Roller.ParamNames.RollerId];
				if (tsbSplitByManager.Checked)
					procParameters[SecurityManager.ParamNames.UserId] = presentationObject[SecurityManager.ParamNames.UserId];
				DataSet ds = DataAccessor.DoAction(procParameters) as DataSet;
				grdActions.DataSource = ds.Tables[Constants.TableNames.Data].DefaultView;
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

		private void tbbPlay_Click(object sender, EventArgs e)
		{
			if (grid.SelectedObject != null && grid.SelectedObject["rollerID"] != null)
			{
				Dictionary<string, object> parametrs = DataAccessor.PrepareParameters(EntityManager.GetEntity((int) Entities.Roller));
				parametrs["rollerID"] = grid.SelectedObject["rollerID"];
				
				DataSet ds = DataAccessor.DoAction(parametrs) as DataSet;
				if (ds != null)
				{
					DataTable dt = ds.Tables[Constants.TableNames.Data];
					if (dt != null && dt.Rows.Count == 1) 
					{
						mediaControl.Play(new Roller(dt.Rows[0]));
					}
				}
			}
		}

		private void tsbStop_Click(object sender, EventArgs e)
		{
			mediaControl.Stop();
		}

		#region IMediaControlContainer Members

		public bool IsPlaying
		{
			set { tsbStop.Enabled = value; }
		}

		#endregion

		private void lookUpGroups_SelectedItemChanged(object sender, EventArgs e)
		{
			PopulateRadiostationsList();
			//grdMassmedia.MultiSelectCheckAll(true);
			RefreshData();
		}

        private void RollerStatisticForm_Load(object sender, EventArgs e)
        {
			//grdMassmedia.MultiSelectCheckAll(true);
		}
    }
}