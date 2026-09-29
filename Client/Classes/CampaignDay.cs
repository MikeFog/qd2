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

		/// <summary>Прайс-лист на исходный день — его срок показывается рядом с выбором даты.</summary>
		internal Pricelist PricelistOfDay => GetPriceList(Day);

		/// <summary>Переносит выпуск на новую дату <paramref name="targetDate"/>.</summary>
		internal void ApplyDayTransfer(DateTime targetDate, decimal priceBeforeTransfer)
		{
			TransferTo(targetDate);
			RecalculateAndShowPriceChange(priceBeforeTransfer);
			OnParentChanged(this, 1);
		}

		/// <summary>Сам перенос (процедура), без пересчёта акции.</summary>
		internal void TransferTo(DateTime targetDate)
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
		}
	}

	/// <summary>
	/// «Перенос дня» снаружи сборки (веб): CampaignDay internal. Дни линейной,
	/// модульной и пакетной кампании (94, 154, 180) — один класс с наследниками.
	/// </summary>
	public static class CampaignDayTransfer
	{
		public const string TransferAction = Constants.EntityActions.Transfer;

		public static DateTime Day(PresentationObject day) => ((CampaignDay)day).Day;

		/// <summary>Срок прайс-листа на исходный день; null — прайс-листа нет.</summary>
		public static (DateTime Start, DateTime Finish)? PricelistPeriod(PresentationObject day)
		{
			Pricelist pricelist = ((CampaignDay)day).PricelistOfDay;
			return pricelist == null ? ((DateTime, DateTime)?)null : (pricelist.StartDate, pricelist.FinishDate);
		}

		/// <summary>
		/// Перенос и пересчёт акции. Возвращает текст сообщения о цене — в десктопе его
		/// показывает RecalculateAndShowPriceChange, в вебе UserInteraction.Notify не назначен.
		/// </summary>
		public static string Apply(PresentationObject day, DateTime targetDate)
		{
			CampaignDay campaignDay = (CampaignDay)day;
			Campaign campaign = campaignDay.Campaign;
			campaign?.Action?.Refresh();
			decimal price = campaign?.Action?.TotalPrice ?? decimal.Zero;

			campaignDay.TransferTo(targetDate);
			return campaignDay.RecalculateWithPriceMessage(price);
		}
	}
}