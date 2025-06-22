using System;
using System.Data;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	internal class ProgramIssue : CampaignPart
	{
		private SponsorProgram _program;

		public enum AttributeSelectors
		{
			FirmNameOnly = 2
		}

		public ProgramIssue() : base(GetEntity())
		{
		}

		public ProgramIssue(DataRow row) : base(GetEntity(), row)
		{
		}

		public ProgramIssue(int issueId) : this()
		{
			this[Issue.ParamNames.IssueId] = issueId;
			isNew = false;
			Refresh();
		}

        public SponsorProgram SponsorProgram
		{
			get 
			{ 
				if(_program == null)
					_program = new SponsorProgram(int.Parse(this[SponsorCampaignProgram.ParamNames.ProgramId].ToString()));
				return _program;
			}
		}

        public DateTime IssueDate
		{
			get { return DateTime.Parse(parameters["IssueDate"].ToString()); }
		}

		public int Bonus
		{
			set { this["bonus"] = value; }
		}

		internal decimal TariffPrice
		{
			set { this[Issue.ParamNames.TariffPrice] = value; }
		}

		internal int AdvertTypeId
		{
			set { this[AdvertType.ParamNames.AdvertTypeId] = value; }
		}

		public static Entity GetEntity()
		{
			return EntityManager.GetEntity((int) Entities.ProgramIssue);
		}
	}
}