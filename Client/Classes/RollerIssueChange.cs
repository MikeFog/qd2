using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	/// <summary>
	/// Действия над одним выпуском ролика (сущность 98, «Выходы в эфир этой кампании» в панели окна)
	/// — веб-аналог Issue.DoAction и CampaignPart.DoAction: «Сделать первым / вторым / последним»,
	/// «Убрать порядок», «Заменить рекламный ролик», «Удалить». RollerIssue — internal, снаружи
	/// выпуск — PresentationObject строки списка.
	///
	/// Как в десктопе, после действия — пересчёт акции и текст о смене её цены. Отличия: запись и
	/// пересчёт — одной транзакцией (десктоп пересчитывал дважды, Д-16), и отказ процедуры откатывает
	/// всё (IssueIUD UpdateItem освобождает занятость окна ещё до проверок). Права проверяются здесь
	/// же: смена позиции (UpdateItem) и замена по имени процедуры мимо WebActionAuthorization идут.
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public static class RollerIssueChange
	{
		public const string SetFirstAction = Issue.ActionNames.SetFirst;
		public const string SetSecondAction = Issue.ActionNames.SetSecond;
		public const string SetLastAction = Issue.ActionNames.SetLast;
		public const string SetUnknownAction = Issue.ActionNames.SetUnknow;
		public const string SubstituteAction = Constants.Actions.Substitute;

		/// <summary>Позиция в блоке по действию меню; возвращает текст о цене акции.</summary>
		public static string SetPosition(PresentationObject issue, string actionName)
		{
			Issue i = Ensure(issue, actionName);
			RollerPositions position;
			switch (actionName)
			{
				case SetFirstAction: position = RollerPositions.First; break;
				case SetSecondAction: position = RollerPositions.Second; break;
				case SetLastAction: position = RollerPositions.Last; break;
				default: position = RollerPositions.Undefined; break;
			}

			// Issue.SetPosition передаёт фактическое окно — позиция меняется там, где выпуск стоит.
			return WithRecalculation(i, campaign => i.SetPosition(position));
		}

		/// <summary>Удалить выпуск; возвращает текст о цене акции.</summary>
		public static string Delete(PresentationObject issue)
		{
			Issue i = Ensure(issue, Constants.EntityActions.Delete);
			return WithRecalculation(i, campaign => i.Delete(silenceFlag: true));
		}

		/// <summary>
		/// Кандидаты на замену ролика выпуска — активные ролики фирмы, кроме текущего
		/// (CampaignPart.GetRollersForSubstitution); null — менять не на что.
		/// </summary>
		public static DataTable SubstituteCandidates(PresentationObject issue)
		{
			Issue i = Ensure(issue, SubstituteAction);
			return i.GetRollersForSubstitution(new Roller(RollerIdOf(i)));
		}

		/// <summary>
		/// «Заменить рекламный ролик» в одном выпуске — RollerSubstitute с @issueID и
		/// @originalWindowID, как CampaignPart.ApplyRollerSubstitution.
		/// </summary>
		/// <param name="warning">Почему не заменено (первая строка процедуры, как в десктопе); null — заменено.</param>
		/// <returns>Текст о цене акции.</returns>
		public static string Substitute(PresentationObject issue, int newRollerId, out string warning)
		{
			Issue i = Ensure(issue, SubstituteAction);
			Roller oldRoller = new Roller(RollerIdOf(i));
			Roller newRoller = new Roller(newRollerId);
			string refusal = null;
			string priceText = WithRecalculation(i, campaign =>
			{
				DataTable unsubstituted = CampaignRoller.ApplyRollerSubstitutionForIssue(campaign, oldRoller, newRoller,
					Convert.ToInt32(i[Issue.ParamNames.IssueId]), Convert.ToInt32(i[TariffWindow.ParamNames.OriginalWindowId]));
				if (unsubstituted != null && unsubstituted.Rows.Count > 0)
					refusal = Convert.ToString(unsubstituted.Rows[0]["message"]);
			});
			warning = refusal;
			return priceText;
		}

		public static string RollerNameOf(PresentationObject issue) => Convert.ToString(issue[Constants.Parameters.Name]);

		private static int RollerIdOf(Issue issue) => Convert.ToInt32(issue[Roller.ParamNames.RollerId]);

		/// <summary>Выпуск ролика и право на действие — как пункт меню (CheckLoggedUserRight, права группы, позиция).</summary>
		private static Issue Ensure(PresentationObject issue, string actionName)
		{
			if (!(issue is Issue i) || !i.IsActionEnabled(actionName, ViewType.Journal))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));
			return i;
		}

		/// <summary>Запись и пересчёт акции одной транзакцией; текст о цене — как после действия в десктопе.</summary>
		private static string WithRecalculation(Issue issue, System.Action<Campaign> write)
		{
			Campaign campaign = Campaign.GetCampaignById(issue.CampaignId);
			if (campaign == null)
				throw new InvalidOperationException(Tr.T("Рекламная кампания удалена. Обновите страницу."));
			decimal oldPrice = campaign.Action.TotalPrice;

			WindowsSource.RunInTransaction(() =>
			{
				write(campaign);
				campaign.RecalculateAction(false);
			});
			return CampaignPart.PriceChangeText(oldPrice, campaign.Action.TotalPrice);
		}
	}
}
