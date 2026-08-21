using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Constants = FogSoft.WinForm.Constants;

namespace Merlin.Classes
{
	// UI-часть (DoAction, PrintOnAirInquire) — в CampaignPackModule.WinForms.cs.
	// PrintOnAirInquire перенесён целиком: генерация отчёта — отдельная область
	// (docs/tasks/web-migration.md, этап 4), не диалоговый паттерн этого документа.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class CampaignPackModule : Campaign
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

		// DoAction и PrintOnAirInquire переехали в CampaignPackModule.WinForms.cs.

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
			if (_modules == null)
			{
				ChildEntity = EntityManager.GetEntity((int)Entities.PackModuleInCampaign);
				_modules = base.GetContent();
			}
			return _modules.Select(string.Format("packModuleID={0}", module.IDs[0])).Length > 0;
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
			issue[Issue.ParamNames.PositionId] = (int)position;
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

	// UI-часть (DoAction) — в CampaignPackModule.WinForms.cs.
	internal partial class CampaignPartPackModule : CampaignPart
	{
		public CampaignPartPackModule()
			: base(EntityManager.GetEntity((int)Entities.PackModuleInCampaign))
		{
		}

        // DoAction и DeleteIssues переехали в CampaignPackModule.WinForms.cs.
    }
}
