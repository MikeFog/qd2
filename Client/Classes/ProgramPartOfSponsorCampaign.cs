using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices.ComTypes;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	// UI-часть (DoAction, SetAdvertType, EditProgramIssues) — в
	// ProgramPartOfSponsorCampaign.WinForms.cs. EditProgramIssues перенесён
	// целиком без разреза — форма 3.1 (модальная сессия CampaignForm),
	// docs/tasks/web-migration-dialogs.md, §8 п.3, отложено до этапа 3.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class ProgramPartOfSponsorCampaign : CampaignPart
	{
		public struct ActionNames
		{
			public const string ShowPrograms = "ShowPrograms";
			public const string ShowDays = "ShowDays";
			public const string ShowRollers = "ShowRollers";
			public const string EditIssues = "EditIssues";
		}

		public ProgramPartOfSponsorCampaign() : base(GetEntity())
		{
		}

		public ProgramPartOfSponsorCampaign(DataRow row) : base(GetEntity(), row)
		{
		}

        public ProgramPartOfSponsorCampaign(int campaignId) : this()
        {
			this[Campaign.ParamNames.CampaignId] = campaignId;
        }

        // DoAction, SetAdvertType и EditProgramIssues переехали в
        // ProgramPartOfSponsorCampaign.WinForms.cs.

        /// <summary>
        /// «Показать дни выхода» / «Показать программы» — смена дочерней сущности узла.
        /// false — это не переключатель.
        /// </summary>
        internal bool TrySwitchView(string actionName)
        {
            if (actionName == ActionNames.ShowDays)
                ChildEntity = EntityManager.GetEntity((int)Entities.SponsorCampaignDay);
            else if (actionName == ActionNames.ShowPrograms)
                ChildEntity = EntityManager.GetEntity((int)Entities.SponsorCampaignProgram);
            else
                return false;
            FireContainerRefreshed();
            return true;
        }

        /// <summary>Переключатель на текущий вид погашен — как у кампании.</summary>
        public override bool IsActionEnabled(string actionName, ViewType type)
        {
            if (actionName == ActionNames.ShowDays)
                return base.IsActionEnabled(actionName, type) && ChildEntity?.Id != (int)Entities.SponsorCampaignDay;
            if (actionName == ActionNames.ShowPrograms)
                return base.IsActionEnabled(actionName, type) && ChildEntity?.Id != (int)Entities.SponsorCampaignProgram;
            return base.IsActionEnabled(actionName, type);
        }

        /// <summary>Именованный паспорт окна «Назначить предмет рекламы» (iPassport).</summary>
        internal const string AdvertTypePassport = "ChangeAdvertTypeForSponsorIssues";

        internal struct AdvertTypeParams
        {
            public const string NameWithGroup = "nameWithGroup";
            public const string AdvertTypeId = "advertTypeID";
            public const string Days = "days";
        }

        /// <summary>
        /// Данные окна: дерево «день → выпуски программ» (набор «days» для treeselector).
        /// campaignTypeID = 100 — ветка процедуры для выпусков спонсорских программ.
        /// </summary>
        internal static DataSet LoadAdvertTypePassportData(Campaign campaign)
        {
            Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
            procParameters[Campaign.ParamNames.CampaignId] = campaign.CampaignId;
            procParameters[Campaign.ParamNames.CampaignTypeId] = 100;
            return DataAccessor.LoadDataSet("CampaignDaysTreePassport", procParameters);
        }

        internal static string AdvertTypePassportCaption(Campaign campaign) =>
            ((CampaignOnSingleMassmedia)campaign).MassmediaNameWithGroup;

        /// <summary>
        /// Проверка окна в порядке десктопа: предмет рекламы → хотя бы один выпуск. В
        /// отметках дерева есть и дни (id — дата строкой) — берутся только целые id выпусков.
        /// </summary>
        internal static string ValidateAdvertTypeAssignment(object advertTypeId, IEnumerable<object> checkedIds, out List<int> issueIds)
        {
            issueIds = new List<int>();
            if (advertTypeId == null || advertTypeId == DBNull.Value)
                return Tr.T(Properties.Resources.AdvertTypeNotSelected);

            foreach (object id in checkedIds)
                if (id != null && int.TryParse(id.ToString(), out int issueId))
                    issueIds.Add(issueId);
            if (issueIds.Count == 0)
                return MessageAccessor.GetMessage("NoIssuesSelected");
            return null;
        }

        /// <summary>Назначает предмет рекламы выбранным выпускам программы.</summary>
        internal void ApplyAdvertTypeToIssues(IEnumerable<int> selectedIds, int advertTypeId)
        {
            foreach (var id in selectedIds)
            {
                ProgramIssue issue = new ProgramIssue(id);
                issue.AdvertTypeId = advertTypeId;
                issue.Update();
            }
            FireContainerRefreshed();
        }

		private static Entity GetEntity()
		{
			return EntityManager.GetEntity((int)Entities.ProgramPart);
		}

		public DataTable GetProgramIssues()
		{
            Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(EntityManager.GetEntity((int)Entities.ProgramIssue));
            procParameters[Campaign.ParamNames.CampaignId] = this[Campaign.ParamNames.CampaignId];

            return ((DataSet)DataAccessor.DoAction(procParameters)).Tables[Constants.TableNames.Data];
        }
	}

	/// <summary>
	/// Вход для веба к узлам частей спонсорской кампании («Программы для спонсоров» /
	/// «Рекламные ролики»): классы частей internal.
	/// </summary>
	public static class SponsorCampaignPartView
	{
		public const string ShowDaysAction = ProgramPartOfSponsorCampaign.ActionNames.ShowDays;
		public const string ShowProgramsAction = ProgramPartOfSponsorCampaign.ActionNames.ShowPrograms;
		public const string ShowRollersAction = RollerPartOfSponsorCampaign.ActionNames.ShowRollers;

		public static void SwitchView(PresentationObject part, string actionName)
		{
			if (part is ProgramPartOfSponsorCampaign programs)
				programs.TrySwitchView(actionName);
			else
				((RollerPartOfSponsorCampaign)part).TrySwitchView(actionName);
		}

		// «Назначить предмет рекламы выпускам спонсорских программ» (узел «Программы для спонсоров»).
		public const string SetAdvertTypeAction = Action.ActionNames.SetAdvertType;
		public const string AdvertTypePassport = ProgramPartOfSponsorCampaign.AdvertTypePassport;
		public const string NameWithGroupField = ProgramPartOfSponsorCampaign.AdvertTypeParams.NameWithGroup;
		public const string AdvertTypeField = ProgramPartOfSponsorCampaign.AdvertTypeParams.AdvertTypeId;
		public const string DaysField = ProgramPartOfSponsorCampaign.AdvertTypeParams.Days;

		public static DataSet LoadAdvertTypeData(PresentationObject programs) =>
			ProgramPartOfSponsorCampaign.LoadAdvertTypePassportData(((ProgramPartOfSponsorCampaign)programs).Campaign);

		public static string AdvertTypeCaption(PresentationObject programs) =>
			ProgramPartOfSponsorCampaign.AdvertTypePassportCaption(((ProgramPartOfSponsorCampaign)programs).Campaign);

		public static string ValidateAdvertType(object advertTypeId, IEnumerable<object> checkedIds, out List<int> issueIds) =>
			ProgramPartOfSponsorCampaign.ValidateAdvertTypeAssignment(advertTypeId, checkedIds, out issueIds);

		public static void ApplyAdvertType(PresentationObject programs, IEnumerable<int> issueIds, int advertTypeId) =>
			((ProgramPartOfSponsorCampaign)programs).ApplyAdvertTypeToIssues(issueIds, advertTypeId);
	}
}