using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Controls;
using Merlin.Forms;
using Merlin.Reports;
using unoidl.com.sun.star.sheet;
using Application=System.Windows.Forms.Application;
using Constants=FogSoft.WinForm.Constants;

namespace Merlin.Classes
{
	internal class CampaignPackModule : Campaign
	{
		internal struct PackActionNames
		{
			public const string ShowPackModules = "ShowPackModules";
		}

		public CampaignPackModule()
			: base(PackModuleCampaingEntity)
		{
            ChildEntity = EntityManager.GetEntity((int)Entities.PackCampaignDay);
		}

		public CampaignPackModule(int campaignID)
			: base(campaignID)
		{
            ChildEntity = EntityManager.GetEntity((int)Entities.PackCampaignDay);
		}

        public CampaignPackModule(int paymentTypeId, int agencyId) : base(PackModuleCampaingEntity)
        {
            this[ParamNames.AgencyID] = agencyId;
            this[ParamNames.CampaignTypeId] = (int)CampaignTypes.PackModule;
            this[ParamNames.PaymentTypeID] = paymentTypeId;
            ChildEntity = EntityManager.GetEntity((int)Entities.PackCampaignDay);
        }

        private static Entity PackModuleCampaingEntity
		{
			get { return EntityManager.GetEntity((int)Entities.PackModuleCampaign); }
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if(actionName == Constants.EntityActions.Edit)
				EditRollerIssues(owner, new PackModuleGrid());
			else if (actionName == ActionNames.ShowDays)
				ShowDays();
			else if (actionName == PackActionNames.ShowPackModules)
				ShowPackModules();
			else if (actionName == ActionNames.PrintOnAirInquire)
				PrintOnAirInquire((Form)owner);
            else if (actionName == Constants.Actions.ChangePositions)
                ChangePositions((Form)owner);
            else
				base.DoAction(actionName, owner, interfaceObject);
		}

		public override void PrintOnAirInquire(Form owner)
		{
			try
			{
				Refresh();
				Application.DoEvents();
				owner.Cursor = Cursors.WaitCursor;

				Dictionary<string, object> procParameters = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
				procParameters[ParamNames.CampaignId] = CampaignId;
				DataSet ds = DataAccessor.LoadDataSet("sl_Months", procParameters);

				Dictionary<object, object> dMonthsToShow = new Dictionary<object, object>();
				Dictionary<object, object> dMonths = new Dictionary<object, object>();

				foreach (DataRow row in ds.Tables[0].Rows)
				{
					int month = ParseHelper.ParseToInt32(row["MonthDate"].ToString(), -1);
					int year = ParseHelper.ParseToInt32(row["MonthYear"].ToString(), -1);
					if (month >= 0 && year >= 0)
					{
						DateTime date = new DateTime(year, month, 1);
						dMonthsToShow.Add(date, date.ToString("MMMM yyy"));
						dMonths.Add(date, date);
					}
				}

				FrmMonths f = new FrmMonths(dMonthsToShow);
				if (f.ShowDialog(owner) == DialogResult.Cancel) return;


				foreach (object dm in f.CheckedItems.Keys)
				{
					if (parameters.ContainsKey("packmodulemassmediaID") && parameters["packmodulemassmediaID"] != null)
					{
						int mmId = ParseHelper.ParseToInt32(parameters["packmodulemassmediaID"].ToString(), -1);
						if (mmId > 0)
						{
							Massmedia massmedia = Massmedia.GetMassmediaByID(mmId);
							DataSet rs = GetOnAirInquireReport(mmId, CampaignId, (DateTime)dMonths[dm], ((DateTime)dMonths[dm]).AddMonths(1).AddDays(-1));
							OnAirInquireReport report = new OnAirInquireReport(this, Agency, rs, f.IsOptionChecked, massmedia, (DateTime)dMonths[dm]);
							report.Show("Ёфирна€ справка");
						}
					}
					else
					{
						DataSet dsMM = Massmedias;
						if (dsMM != null)
						{
							DataTable table = dsMM.Tables[Constants.TableNames.Data];
							foreach (DataRow row in table.Rows)
							{
								int mmId = ParseHelper.ParseToInt32(row["packmodulemassmediaID"].ToString(), -1);
								if (mmId > 0)
								{
									Massmedia massmedia = Massmedia.GetMassmediaByID(mmId);
									DataSet rs = GetOnAirInquireReport(mmId, CampaignId, (DateTime)dMonths[dm], ((DateTime)dMonths[dm]).AddMonths(1).AddDays(-1));
									OnAirInquireReport report = new OnAirInquireReport(this, Agency, rs, f.IsOptionChecked, massmedia, (DateTime)dMonths[dm]);
									report.Show("Ёфирна€ справка");
								}
							}
						}
					}
				}
			}
			finally
			{
				owner.Cursor = Cursors.Default;
			}
		}

		public DataSet Massmedias
		{
			get
			{
				Dictionary<string, object> procMMParameters = DataAccessor.PrepareParameters(EntityManager.GetEntity((int)Entities.MassMedia),
											   InterfaceObjects.SelectMMForPMCampaign, Constants.Actions.Load);
				procMMParameters["campaignID"] = CampaignId;
				return DataAccessor.DoAction(procMMParameters) as DataSet;
			}
		}

		private void ShowPackModules()
		{
			ChildEntity = EntityManager.GetEntity((int)Entities.PackModuleInCampaign);
			FireContainerRefreshed();
		}

		private void ShowDays()
		{
			ChildEntity = EntityManager.GetEntity((int)Entities.PackCampaignDay);
			FireContainerRefreshed();
		}

		public override bool HasModuleIssue(PresentationObject module)
		{
			if(isNew) return false;
			if (modules == null)
			{
				ChildEntity = EntityManager.GetEntity((int)Entities.PackModuleInCampaign);
				modules = base.GetContent();
			}
			return modules.Select(string.Format("packModuleID={0}", module.IDs[0])).Length > 0;
		}

		public override bool IsActionHidden(string actionName, ViewType type)
		{
			if (actionName == ActionNames.ShowDays)
                return type != ViewType.Tree;
            if (actionName == PackActionNames.ShowPackModules)
            	return type != ViewType.Tree;

			return base.IsActionHidden(actionName, type);
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
            if (actionName == ActionNames.ShowDays)
                return type == ViewType.Tree && ChildEntity != null && ChildEntity.Id != (int)Entities.PackCampaignDay;
            else if (actionName == PackActionNames.ShowPackModules)
				return type == ViewType.Tree && ChildEntity != null && ChildEntity.Id != (int)Entities.PackModuleInCampaign;
            else if (actionName == ActionNames.PrintOnAirInquire)
                return Action.IsConfirmed;
            else
                return base.IsActionEnabled(actionName, type);
		}

		public PackModuleIssue AddPackModuleIssue(PackModulePricelist pricelist, Roller roller, RollerPositions position, DateTime windowDate, int? grantorID)
		{
			PackModuleIssue issue = new PackModuleIssue();
				
			issue[ParamNames.CampaignId] = CampaignId;
			issue[Pricelist.ParamNames.PricelistId] = pricelist.PricelistId;
			issue[Roller.ParamNames.RollerId] = roller.RollerId;
			issue[RollerIssue.ParamNames.RollerDuration] = roller.Duration;
			issue[RollerIssue.ParamNames.IssueDate] = windowDate;
			issue[ParamNames.TariffPrice] = pricelist.Price;
			issue[Issue.ParamNames.Position] = (int)position;
			issue["grantorID"] = (grantorID ?? (object)DBNull.Value);
			issue.Update();

			int id = ParseHelper.ParseToInt32(issue[Issue.ParamNames.PackModuleIssueID].ToString(), -1);
			return id > 0 ? issue : null;
		}

		public IDictionary<string, string> GetUniqueMassmedias(bool isFact)
		{
			Dictionary<string, object> parametersMM = new Dictionary<string, object>();
			parametersMM["campaignID"] = CampaignId;
			parametersMM["isFact"] = isFact;
			DataSet ds = DataAccessor.LoadDataSet("GetUniqueMMsForPackModuleCampaign", parametersMM);
			if (ds.Tables.Count > 0)
			{
				MediaPlanCampaignGroups mp = new MediaPlanCampaignGroups();
				DataTable dt = ds.Tables[0];
				foreach (DataRow dataRow in dt.Rows)
					mp.AddMassmedia(int.Parse(dataRow["massmediaID"].ToString())
						, dataRow["name"].ToString()
						, int.Parse(dataRow["rollerID"].ToString())
						, DateTime.Parse(dataRow["date"].ToString()));
				return mp.GetUniqueMassmedias();
			}
			return null;
		}
	}

	internal class CampaignPartPackModule : CampaignPart
	{
		public CampaignPartPackModule()
			: base(EntityManager.GetEntity((int)Entities.PackModuleInCampaign))
		{
		}

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (actionName == Campaign.ActionNames.DeleteIssues)
                DeleteIssues((Form)owner);
            else if (actionName == Constants.Actions.ChangePositions)
                ChangePositions((Form)owner);
            else
                base.DoAction(actionName, owner, interfaceObject);
        }

        private void DeleteIssues(Form owner)
        {
            Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
            parameters[CampaignPart.OBJECT_ID] = this[PackModule.ParamNames.PackModuleId];
            if(Campaign.DeleteIssues(owner, false, parameters, isFireEvent: false))
				FireContainerRefreshed();
        }
    }
}