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
}