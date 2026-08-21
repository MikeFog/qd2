using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using static Merlin.Classes.Campaign;

namespace Merlin.Classes
{
    // UI-часть (DoAction, SubstituteRoller, DeleteIssues, диалог Substitute) —
	// в CampaignRoller.WinForms.cs. Здесь остаётся точка записи в БД
	// ApplyRollerSubstitutionForDays: она возвращает таблицу незаменённых
	// роликов, а показывает её вызывающий UI-код.
	// Конвенция — docs/tasks/web-migration-dialogs.md, §8 п.4.
	internal partial class CampaignRoller : CampaignPart
	{
		private Roller roller;

		public CampaignRoller() : base(EntityManager.GetEntity((int) Entities.CampaignRoller))
		{
		}

		protected CampaignRoller(Entity entity) : base(entity)
		{
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (actionName == Constants.Actions.PlayRoller)
				return base.IsActionEnabled(actionName, type) && !IsMute;
			return base.IsActionEnabled(actionName, type);
		}

		// DoAction, SubstituteRoller и DeleteIssues переехали в CampaignRoller.WinForms.cs.

        public Roller Roller
		{
			get
			{
				if (roller == null)
					roller = new Roller(int.Parse(this[Roller.ParamNames.RollerId].ToString()));

                return roller;
            }
		}

        public int? ModuleID
		{
			get {return (this["moduleID"] == null || this["moduleID"] == DBNull.Value) ? null : (int?)ParseHelper.ParseToInt32(this["moduleID"].ToString()); }
		}
        
		public bool IsMute
		{
			get { return ParseHelper.GetBooleanFromObject(this["isMute"], false); }
		}

		// Substitute (диалог выбора ролика и дней) переехал в CampaignRoller.WinForms.cs.

		/// <summary>
		/// Заменяет <paramref name="oldRoller"/> на <paramref name="newRoller"/> в
		/// кампании <paramref name="campaign"/> по набору дней <paramref name="days"/>,
		/// опционально в рамках модуля или пакетного модуля.
		/// Возвращает таблицу незаменённых роликов (null, если процедура ничего не
		/// вернула) — показать её пользователю решает вызывающий UI-код.
		///
		/// Имя отличается от <see cref="CampaignPart.ApplyRollerSubstitution"/> не
		/// случайно: тот заменяет ролик в одном выпуске, этот — по набору дней.
		/// </summary>
		public static DataTable ApplyRollerSubstitutionForDays(Campaign campaign, Roller oldRoller, Roller newRoller,
								  DataTable days, object moduleID, object packModuleID)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				EntityManager.GetEntity((int)Entities.CampaignRoller),
				InterfaceObjects.FakeModule, Constants.Actions.Substitute);
			procParameters["oldRollerId"] = oldRoller.RollerId;
			procParameters["oldDuration"] = oldRoller.Duration;
			procParameters["newRollerId"] = newRoller.RollerId;
			procParameters["newDuration"] = newRoller.Duration;
			if (moduleID != null)
				procParameters["moduleID"] = moduleID;
			if (packModuleID != null)
				procParameters["packModuleID"] = packModuleID;
			procParameters[Campaign.ParamNames.CampaignId] = campaign.CampaignId;
			procParameters[Campaign.ParamNames.CampaignTypeId] = (int)campaign.CampaignType;

			DataSet ds = DataAccessor.LoadDataSet("RollerSubstitute", procParameters, days);

			return (ds != null && ds.Tables.Count > 0) ? ds.Tables[0] : null;
		}
	}

	internal class CampaignRollerInsideDay : CampaignRoller
	{
		public CampaignRollerInsideDay()
			:base(EntityManager.GetEntity((int)Entities.CampaignRollerInsideDay))
		{
		}
	}
}
