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