using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Remoting.Messaging;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	class TariffWindowPackModule : ITariffWindow
	{
		private readonly DateTime windowDate;
		private readonly decimal price;
		private readonly int tariffId = 0;
		private readonly int packModuleId = 0;
		public readonly bool isFirstBusy;
		public readonly bool isSecondBusy;
		public readonly bool isLastBusy;

		public TariffWindowPackModule(DataRow row, decimal price, int packModuleId)
		{
			this.price = price;
			this.packModuleId = packModuleId;
			windowDate = (DateTime) row["date"];
			isFirstBusy = (bool)row["isFirstBusy"];
			isSecondBusy = (bool)row["isSecondBusy"];
			isLastBusy = (bool)row["isLastBusy"];
		}

		public DateTime WindowDate
		{
			get { return windowDate; }
		}

		public decimal Price
		{
			get { return price; }
		}

		public int TariffId
		{
			get { return tariffId; }
		}

        public bool IsDisabled
		{
			get { return false; }
		}

        public bool IsMarked
        {
            get { return false; }
        }

        public DataTable LoadIssues(bool showUnconfirmed, Entity issueEntity)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(PackModuleIssue.GetEntity());
			procParameters[PackModule.ParamNames.PackModuleId] = packModuleId;
			procParameters["startDate"] = windowDate;
			procParameters["finishDate"] = windowDate;
			procParameters["showUnconfirmed"] = showUnconfirmed;

			//if (position != RollerPositions.Undefined)
			//  procParameters[RollerIssue.ParamNames.Position] = ((int)position).ToString();

			DataSet ds = (DataSet)DataAccessor.DoAction(procParameters);
			return ds.Tables[Constants.TableNames.Data];
		}

		public void AddIssue(PackModulePricelist pricelist, Roller roller, CampaignPackModule campaign, RollerPositions position, int? grantorID)
		{
			campaign.AddPackModuleIssue(pricelist, roller, position, windowDate, grantorID);
		}

		public DataView GetDetails(bool showUnconfirmed)
		{
			Dictionary<string, object> procParameters = new Dictionary<string, object>();
			procParameters["windowDate"] = windowDate;
			procParameters["packModuleId"] = packModuleId;
			procParameters["showUnconfirmed"] = showUnconfirmed;
			return DataAccessor.LoadDataSet("GetPackModuleIssueDetails", procParameters).Tables[0].DefaultView;
		}
	}
}