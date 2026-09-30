using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	internal partial class RollerPartOfSponsorCampaign : CampaignPart
	{
		public RollerPartOfSponsorCampaign()
			: base(GetEntity())
		{
		}

		public RollerPartOfSponsorCampaign(DataRow row)
			: base(GetEntity(), row)
		{
		}

		private static Entity GetEntity()
		{
			return EntityManager.GetEntity((int) Entities.RollerPart);
		}

		// DoAction и EditRollerIssues (форма 3.1, модальная сессия CampaignForm)
		// переехали в RollerPartOfSponsorCampaign.WinForms.cs целиком, без разреза
		// — тот же случай, что ProgramPartOfSponsorCampaign (§8 п.3 конвенции).

		/// <summary>
		/// «Показать дни выхода» / «Показать рекламные ролики» — смена дочерней сущности
		/// узла. false — это не переключатель.
		/// </summary>
		internal bool TrySwitchView(string actionName)
		{
			if (actionName == ActionNames.ShowDays)
				ChildEntity = EntityManager.GetEntity((int)Entities.CampaignDay);
			else if (actionName == ActionNames.ShowRollers)
				ChildEntity = EntityManager.GetEntity((int)Entities.CampaignRoller);
			else
				return false;
			FireContainerRefreshed();
			return true;
		}

		/// <summary>Переключатель на текущий вид погашен — как у кампании.</summary>
		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (actionName == ActionNames.ShowDays)
				return base.IsActionEnabled(actionName, type) && ChildEntity?.Id != (int)Entities.CampaignDay;
			if (actionName == ActionNames.ShowRollers)
				return base.IsActionEnabled(actionName, type) && ChildEntity?.Id != (int)Entities.CampaignRoller;
			return base.IsActionEnabled(actionName, type);
		}


		#region Nested type: ActionNames

		public struct ActionNames
		{
			public const string EditIssues = "EditIssues";
			public const string ShowRollers = "ShowRollers";
			public const string ShowDays = "ShowDays";
		}

		#endregion
	}
}