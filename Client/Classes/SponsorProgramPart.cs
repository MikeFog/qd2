using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	internal class SponsorProgramPart : CampaignPart
	{
		private ObjectContainer behindClass;

		public SponsorProgramPart() : base(EntityManager.GetEntity((int) Entities.ProgramPart))
		{
		}

		private static Entity DefineEntity(DataRow row)
		{
			if (int.Parse(row[Constants.Parameters.Id].ToString()) ==
			    (int) CampaignOnSingleMassmedia.CampaignParts.ProgramPart)
				return EntityManager.GetEntity((int) Entities.ProgramPart);
			return EntityManager.GetEntity((int) Entities.RollerPart);
		}

		public override void Init(DataRow row)
		{
			entity = DefineEntity(row);

			if (entity.Id == (int) Entities.ProgramPart)
				behindClass = new ProgramPartOfSponsorCampaign(row);
			else
				behindClass = new RollerPartOfSponsorCampaign(row);

			base.Init(row);
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if(actionName != Constants.EntityActions.Refresh)
				behindClass.DoAction(actionName, owner, interfaceObject);

			if (actionName == ProgramPartOfSponsorCampaign.ActionNames.ShowDays ||
				actionName == ProgramPartOfSponsorCampaign.ActionNames.ShowPrograms ||
				 actionName == ProgramPartOfSponsorCampaign.ActionNames.ShowRollers ||
					actionName == Constants.EntityActions.Refresh)
			{
				ChildEntity = behindClass.ChildEntity;
				FireContainerRefreshed();
			}
		}

		public override bool IsActionHidden(string actionName, ViewType type)
		{
			if (actionName == ProgramPartOfSponsorCampaign.ActionNames.ShowDays)
				return type != ViewType.Tree;
			if (actionName == ProgramPartOfSponsorCampaign.ActionNames.ShowPrograms)
				return type != ViewType.Tree;
			if (actionName == ProgramPartOfSponsorCampaign.ActionNames.ShowRollers)
				return type != ViewType.Tree;

			return base.IsActionHidden(actionName, type);
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (actionName == ProgramPartOfSponsorCampaign.ActionNames.ShowDays)
				return type == ViewType.Tree && base.IsActionEnabled(actionName, type) && (ChildEntity.Id == (int)Entities.SponsorCampaignProgram || ChildEntity.Id == (int)Entities.CampaignRoller);
			else if (actionName == ProgramPartOfSponsorCampaign.ActionNames.ShowPrograms)
				return type == ViewType.Tree && base.IsActionEnabled(actionName, type) && ChildEntity.Id == (int)Entities.SponsorCampaignDay;
			else if (actionName == ProgramPartOfSponsorCampaign.ActionNames.ShowRollers)
				return type == ViewType.Tree && base.IsActionEnabled(actionName, type) && ChildEntity.Id == (int)Entities.CampaignDay;
			return base.IsActionEnabled(actionName, type);
		}

		public override RelationScenario RelationScenario
		{
			get { return behindClass.RelationScenario; }
			set { base.RelationScenario = behindClass.RelationScenario = value; }
		}

		public override DataTable GetContent()
		{
			return behindClass.GetContent();
		}

		public override Entity ChildEntity
		{
			get { return behindClass.ChildEntity; }
		}

		public override bool IsChildNodeExpandable
		{
			get { return behindClass.IsChildNodeExpandable; }
		}
	}
}