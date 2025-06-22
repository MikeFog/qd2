using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Forms;

namespace Merlin.Classes
{
	internal class CampaignDayForRoller : CampaignPart
	{
        public CampaignDayForRoller() : base(EntityManager.GetEntity((int) Entities.CampaignDayForRoller))
		{
		}

		protected CampaignDayForRoller(Entity entity)
			: base(entity)
		{
		}
	}

	internal class CampaignDay : CampaignPart
	{
        public struct ParamNames
        {
            public const string IssueDate = "issueDate";
        }

        public CampaignDay() : base(EntityManager.GetEntity((int) Entities.CampaignDay))
		{
		}

		protected CampaignDay(Entity entity) : base(entity)
		{
		}


        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == Constants.EntityActions.Transfer)
				TransferDay((Form) owner);
			base.DoAction(actionName, owner, interfaceObject);
		}

        public DateTime Day
		{
			get { return DateTime.Parse(parameters[RollerIssue.ParamNames.IssueDate].ToString()); }
		}

		protected virtual Pricelist GetPriceList(DateTime date)
		{
			Massmedia massmedia = Massmedia.
				GetMassmediaByID(int.Parse(this[Massmedia.ParamNames.MassmediaId].ToString()));
			
			return massmedia.GetPriceList(date);
		}

		private void TransferDay(Form form)
		{
			decimal price = decimal.Zero;

			if (Campaign != null && Campaign.Action != null)
			{
				Campaign.Action.Refresh();
				price = Campaign.Action.TotalPrice;
			}

			DateTime date = DateTime.Parse(this[RollerIssue.ParamNames.IssueDate].ToString());
			TransferDayForm fTransfer = new TransferDayForm(date, GetPriceList(date));
			if (fTransfer.ShowDialog(form) == DialogResult.OK)
			{
				try
				{
                    Cursor.Current = Cursors.WaitCursor;
					Application.DoEvents();
                    Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
						entity, InterfaceObjects.FakeModule, Constants.Actions.Transfer);

					procParameters[Campaign.ParamNames.CampaignId] = this[Campaign.ParamNames.CampaignId];
					procParameters["oldDate"] = this[RollerIssue.ParamNames.IssueDate];
					procParameters["newDate"] = fTransfer.TargetDate;
					// этот параметр для показа возможного сообщения об ошибке
                    procParameters[Issue.ParamNames.IssueDate] = fTransfer.TargetDate;
                    procParameters[Massmedia.ParamNames.MassmediaId] = this[Massmedia.ParamNames.MassmediaId];
					DataAccessor.DoAction(procParameters);
					this[RollerIssue.ParamNames.IssueDate] = fTransfer.TargetDate;
					RecalculateAndShowPriceChange(price);
					OnParentChanged(this, 1);
				}
				finally { Cursor.Current = Cursors.Default; }
			}
		}
	}
}