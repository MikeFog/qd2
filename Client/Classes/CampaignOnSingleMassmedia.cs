using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Forms;
using Merlin.Reports;

namespace Merlin.Classes
{
	class CampaignOnSingleMassmedia : Campaign
	{
		public CampaignOnSingleMassmedia() : base()
		{
		}

		public CampaignOnSingleMassmedia(int campaignID) : base(campaignID)
		{
		}

		public CampaignOnSingleMassmedia(DataRow row) : base(row)
		{
        }

        public enum CampaignParts
		{
			ProgramPart = 1,
			RollerPart = 2
		}

		private Massmedia _massmedia;

		public override DataTable GetContent()
		{
            if(entity.Id == (int)Entities.SponsorCampaign)
            //if(ChildEntity.Id == (int)Entities.ProgramPart || ChildEntity.Id == (int)Entities.CampaignPart || ChildEntity.Id == (int)Entities.RollerPart)
				return GetSponsorCampaignParts();
			return base.GetContent();
		}

		public Massmedia Massmedia
		{
			get
			{
				if(!parameters.ContainsKey(ParamNames.MassmediaId))
					Refresh(InterfaceObjects.SimpleJournal);
				if(_massmedia == null)
					_massmedia = Massmedia.GetMassmediaByID(int.Parse(this[ParamNames.MassmediaId].ToString()));
				return _massmedia;
			}
		}

		private int MassmediaId
		{
			get { return int.Parse(this[ParamNames.MassmediaId].ToString()); }
		}

		public string MassmediaName
		{
			get { return this[ParamNames.MassmediaName].ToString(); }
		}

		public string MassmediaNameWithGroup
		{
			get { return string.Format("{0} ({1})", MassmediaName, this[ParamNames.GroupName]); }
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if(actionName == ActionNames.PrintOnAirInquire)
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

				foreach(DataRow row in ds.Tables[0].Rows)
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
				if(f.ShowDialog(owner) == DialogResult.Cancel) return;


				foreach (object dm in f.CheckedItems.Keys)
				{
					DataSet rs = GetOnAirInquireReport(MassmediaId, CampaignId, (DateTime)dMonths[dm], ((DateTime)dMonths[dm]).AddMonths(1).AddDays(-1));
					OnAirInquireReport report = new OnAirInquireReport(this, Agency, rs, f.IsOptionChecked, Massmedia, (DateTime)dMonths[dm]);
					report.Show("Эфирная справка");
				}
			}
			finally
			{
				owner.Cursor = Cursors.Default;
			}
		}

		private DataTable GetSponsorCampaignParts()
		{
			ChildEntity = EntityManager.GetEntity((int)Entities.CampaignPart);
			DataTable dt = new DataTable(Constants.TableNames.Data);
			dt.Columns.Add(Constants.Parameters.Id, Type.GetType("System.Int32"));
			dt.Columns.Add(Constants.Parameters.Name, Type.GetType("System.String"));
			dt.Columns.Add(ParamNames.CampaignId, Type.GetType("System.Int32"));
			dt.Columns.Add(Massmedia.ParamNames.MassmediaId, Type.GetType("System.Int32"));
            dt.Columns.Add(Classes.Action.ParamNames.DeleteDate, Type.GetType("System.DateTime"));
            dt.Columns.Add(Constants.ParamNames.EntityId, Type.GetType("System.Int32"));

            object[] rowVals = new object[dt.Columns.Count];
			rowVals[0] = CampaignParts.ProgramPart;
			rowVals[1] = "Программы для спонсоров";
			rowVals[2] = this[ParamNames.CampaignId];
			rowVals[3] = MassmediaId;
            rowVals[4] = Action.DeleteDate;
            rowVals[5] = (int)Entities.ProgramPart;
            dt.Rows.Add(rowVals);

			rowVals[0] = CampaignParts.RollerPart;
			rowVals[1] = "Рекламные ролики";
			rowVals[2] = this[ParamNames.CampaignId];
			rowVals[3] = MassmediaId;
            rowVals[4] = Action.DeleteDate;
            rowVals[5] = (int)Entities.RollerPart;
            dt.Rows.Add(rowVals);

			return dt;
		}

        public override bool IsActionEnabled(string actionName, ViewType type)
        {
            if (actionName == ActionNames.PrintOnAirInquire)
                return Action.IsConfirmed;
            else return base.IsActionEnabled(actionName, type);
        }


        public DataTable Days(PresentationObject roller)
        {
            Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
            procParameters.Add(ParamNames.CampaignId, CampaignId);
            procParameters.Add(ParamNames.CampaignTypeId, (int)CampaignType);
            procParameters.Add(Roller.ParamNames.RollerId, roller.IDs[0]);

            return DataAccessor.LoadDataSet("RollerSubstitutionPassport", procParameters).Tables[2].Copy();
        }
    }
}