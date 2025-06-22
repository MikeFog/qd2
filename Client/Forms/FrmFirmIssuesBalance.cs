using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;

namespace Merlin.Forms
{
	public class FrmFirmIssuesBalance : FrmFirmBalance
	{
		public FrmFirmIssuesBalance()
		{
		}

		public FrmFirmIssuesBalance(FirmBalanceIssues balance, DateTime startDate)
			: base(balance, startDate)
		{
		}

		protected override void InitOnLoad()
		{
			base.InitOnLoad();

			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(EntityManager.GetEntity((int)Entities.BalanceIssues),
											   InterfaceObjects.BalanceJournal, Constants.Actions.Load);
			DataSet ds = DataAccessor.DoAction(procParameters) as DataSet;
			OpFirms.SetDataSource(EntityManager.GetEntity((int)Entities.Firm), ds.Tables["firm"].Copy());

			GrdAgency.Entity = EntityManager.GetEntity((int)Entities.Agency);
			GrdAgency.DataSource = ds.Tables["agency"].Copy().DefaultView;
		}

		protected override decimal RefreshBalanceOnStartOfInterval()
		{
			Entity entity = EntityManager.GetEntity((int)Entities.BalanceIssues);
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(entity);

			procParameters["theDate"] = DateStart.AddDays(-1);
			procParameters["FirmID"] = FirmID;
			procParameters["ShowBlack"] = ShowBlack;
			procParameters["ShowWhite"] = ShowWhite;
			procParameters["agenciesIDString"] = AgenciesIDString;
			if (UserID != null)
				procParameters["ManagerID"] = UserID;

			DataSet ds = DataAccessor.DoAction(procParameters) as DataSet;
			DataTable dt = ds.Tables[Constants.TableNames.Data];
			if (dt.Rows.Count == 0)
				return 0;
			return decimal.Parse(dt.Rows[0]["summaPositive"].ToString()) +
				   decimal.Parse(dt.Rows[0]["summaNegative"].ToString());
		}

		protected override decimal RefreshActionInfo(FogSoft.WinForm.Controls.SmartGrid grid)
		{
			Entity entity = EntityManager.GetEntity((int)Entities.Action);
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(entity,
											   InterfaceObjects.BalanceJournal, Constants.Actions.Load);

			procParameters["startOfInterval"] = DateStart;
			procParameters["endOfInterval"] = DateFinish;
			procParameters["firmID"] = FirmID;
			procParameters["ShowBlack"] = ShowBlack;
			procParameters["ShowWhite"] = ShowWhite;
			procParameters["agenciesIDString"] = AgenciesIDString;
			if (UserID != null)
				procParameters[SecurityManager.ParamNames.UserId] = UserID;
			procParameters["isReadyOnly"] = 1;

			DataSet ds = DataAccessor.DoAction(procParameters) as DataSet;

			grid.Entity = entity;
			grid.DataSource = ds.Tables[Constants.TableNames.Data].DefaultView;
			//Calculate total sum
			decimal totalSum = 0;
			foreach (DataRow row in ds.Tables[Constants.TableNames.Data].Rows)
			{
				decimal result;
				if (decimal.TryParse(row["totalPrice"].ToString(), out result))
					totalSum += result;
			}
			return totalSum;
		}

		protected override decimal RefreshPaymentInfo(FogSoft.WinForm.Controls.SmartGrid grid)
		{
			// Load Payments assigned to action
			Entity entity = UserID != null 
				|| (!SecurityManager.LoggedUser.IsRightToViewForeignActions() && SecurityManager.LoggedUser.IsRightToViewGroupActions()) ?
			                	EntityManager.GetEntity((int) Entities.PaymentCommonAction)
								: EntityManager.GetEntity((int)Entities.PaymentCommon);

			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(entity);
			procParameters["startOfInterval"] = DateStart;
			procParameters["endOfInterval"] = DateFinish;
			procParameters["firmID"] = FirmID;
			procParameters["ShowBlack"] = ShowBlack;
			procParameters["ShowWhite"] = ShowWhite;
			procParameters["agenciesIDString"] = AgenciesIDString;
			if (UserID != null)
				procParameters["managerID"] = UserID;

			DataSet ds = DataAccessor.DoAction(procParameters) as DataSet;

			grid.Entity = entity;
			grid.DataSource = ds.Tables[Constants.TableNames.Data].DefaultView;

			//Calculate total sum
			decimal totalSum = 0;
			foreach (DataRow row in ds.Tables[Constants.TableNames.Data].Rows)
				totalSum += Decimal.Parse(row["summa"].ToString());
			return totalSum;
		}

		protected override DataTable ReloadUsers(PresentationObject firm)
		{
			if (firm == null)
				return null;

			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["firmID"] = firm.IDs[0];
			DataSet ds = DataAccessor.LoadDataSet("FirmManagers", parameters);
			return ds.Tables[0];
		}
	}
}