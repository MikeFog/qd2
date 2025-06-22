using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;

namespace Merlin.Forms
{
	public class FrmFirmStudioOrderBalance : FrmFirmBalance
	{
		public FrmFirmStudioOrderBalance()
		{
		}

		public FrmFirmStudioOrderBalance(FirmBalanceStudioOrder balance, DateTime startDate) 
			: base(balance, startDate)
		{
		}

		protected override void InitOnLoad()
		{
			base.InitOnLoad();

			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(EntityManager.GetEntity((int)Entities.BalanceStudioOrder),
											   InterfaceObjects.BalanceJournal, Constants.Actions.Load);

			DataSet ds = DataAccessor.DoAction(procParameters) as DataSet;
			OpFirms.SetDataSource(EntityManager.GetEntity((int)Entities.Firm), ds.Tables["firm"].Copy());

			GrdAgency.Entity = EntityManager.GetEntity((int)Entities.Agency);
			GrdAgency.DataSource = ds.Tables["agency"].Copy().DefaultView;
		}

		protected override decimal RefreshBalanceOnStartOfInterval()
		{
			Entity entity = EntityManager.GetEntity((int)Entities.BalanceStudioOrder);
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(entity);

			procParameters["theDate"] = DateStart.AddDays(-1);
			procParameters["firmID"] = FirmID;
			procParameters["ShowWhite"] = ShowWhite;
			procParameters["ShowBlack"] = ShowBlack;
			procParameters["agenciesIDString"] = AgenciesIDString;
			if (UserID != null)
				procParameters["managerID"] = UserID;

			DataSet ds = DataAccessor.DoAction(procParameters) as DataSet;
			DataTable dt = ds.Tables[Constants.TableNames.Data];
			if (dt.Rows.Count == 0)
				return 0;
			return decimal.Parse(dt.Rows[0]["summaPositive"].ToString()) +
				   decimal.Parse(dt.Rows[0]["summaNegative"].ToString());
		}

		protected override decimal RefreshActionInfo(SmartGrid grid)
		{
			Entity entity = EntityManager.GetEntity((int)Entities.StudioOrder);
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(entity);

			procParameters["startOfInterval"] = DateStart;
			procParameters["endOfInterval"] = DateFinish;
			procParameters["firmID"] = FirmID;
			procParameters["ShowWhite"] = ShowWhite;
			procParameters["ShowBlack"] = ShowBlack;
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
				totalSum += Decimal.Parse(row["finalPrice"].ToString());
			return totalSum;
		}

		protected override decimal RefreshPaymentInfo(SmartGrid grid)
		{
			// Load Payments assigned to action
			Entity entity = UserID != null
								?
							EntityManager.GetEntity((int)Entities.PaymentStudioOrderAction)
								:
							EntityManager.GetEntity((int)Entities.PaymentStudioOrder);

			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(entity);
			procParameters["startOfInterval"] = DateStart;
			procParameters["endOfInterval"] = DateFinish;
			procParameters["firmID"] = FirmID;
			procParameters["ShowWhite"] = ShowWhite;
			procParameters["ShowBlack"] = ShowBlack;
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

			Dictionary<string, object> parameters = new Dictionary<string, object>();
			parameters["firmID"] = firm.IDs[0];
			DataSet ds = DataAccessor.LoadDataSet("FirmStudioOrderManagers", parameters);
			return ds.Tables[0];
		}
	}
}