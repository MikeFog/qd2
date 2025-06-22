using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	internal class TariffWindowWithProgramIssue : ITariffWindow
	{
		private readonly DateTime windowDate;
		private readonly decimal price;
		private readonly int tariffId;
		private readonly int sponsorProgramId;
		public readonly int IssuesCount;
		public readonly int IssuesCountConfirmed;

		internal TariffWindowWithProgramIssue(Tariff tariff, DateTime windowDate, DataTable dtIssue, SponsorProgram program, int campaignID)
		{
			this.windowDate = windowDate.AddHours(tariff.Time.Hour).AddMinutes(tariff.Time.Minute);
			price = tariff.Price;
			tariffId = tariff.TariffId;
			Issue = FindIssue(dtIssue, campaignID);
			IssuesCount = CalculateIssues(dtIssue, false);
			IssuesCountConfirmed = CalculateIssues(dtIssue, true);
			sponsorProgramId = program.SponsorProgramId;
		}

		internal ProgramIssue Issue { get; set; }

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

		private int CalculateIssues(DataTable dtIssue, bool onlyConfirmed)
		{
			string strWhere = string.Format("tariffId = {0} And issueDate = '{1}' ", TariffId, WindowDate);
			if (onlyConfirmed)
				strWhere += "and IsConfirmed = 1 ";
			return dtIssue.Select(strWhere).Length;			
		}

		private ProgramIssue FindIssue(DataTable dtIssue, int campaignID)
		{
			DataRow[] issues = dtIssue.Select(
				string.Format("tariffId = {0} And issueDate = '{1}' and (isConfirmed = 1 or campaignID = {2})", TariffId, WindowDate, campaignID));

			if (issues.Length > 0)
				return new ProgramIssue(issues[0]);
			return null;
		}

		public virtual DataTable LoadIssues(bool showUnconfirmed, Entity issueEntity)
		{
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(EntityManager.GetEntity((int) Entities.ProgramIssue));
			procParameters["programId"] = sponsorProgramId;
			procParameters[Tariff.ParamNames.TariffId] = tariffId;
			procParameters["windowDate"] = WindowDate;
			procParameters["showUncorfirmed"] = showUnconfirmed;

			DataSet ds = (DataSet) DataAccessor.DoAction(procParameters);
			return ds.Tables[Constants.TableNames.Data];
		}

        public bool IsDisabled
        {
            get { return false; }
        }

        public bool IsMarked
        {
            get { return false; }
        }
    }
}