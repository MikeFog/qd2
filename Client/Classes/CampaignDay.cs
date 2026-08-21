using System;
using System.Collections.Generic;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

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

	// UI-часть (DoAction, TransferDay) — в CampaignDay.WinForms.cs.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class CampaignDay : CampaignPart
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


        // DoAction и TransferDay переехали в CampaignDay.WinForms.cs.

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

		/// <summary>Переносит выпуск на новую дату <paramref name="targetDate"/>.</summary>
		internal void ApplyDayTransfer(DateTime targetDate, decimal priceBeforeTransfer)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				entity, InterfaceObjects.FakeModule, Constants.Actions.Transfer);

			procParameters[Campaign.ParamNames.CampaignId] = this[Campaign.ParamNames.CampaignId];
			procParameters["oldDate"] = this[RollerIssue.ParamNames.IssueDate];
			procParameters["newDate"] = targetDate;
			// этот параметр для показа возможного сообщения об ошибке
			procParameters[Issue.ParamNames.IssueDate] = targetDate;
			procParameters[Massmedia.ParamNames.MassmediaId] = this[Massmedia.ParamNames.MassmediaId];
			DataAccessor.DoAction(procParameters);
			this[RollerIssue.ParamNames.IssueDate] = targetDate;
			RecalculateAndShowPriceChange(priceBeforeTransfer);
			OnParentChanged(this, 1);
		}
	}
}