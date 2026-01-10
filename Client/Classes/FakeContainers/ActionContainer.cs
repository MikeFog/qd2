using System;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Forms;
using Merlin.Forms.FilterForm;

namespace Merlin.Classes.FakeContainers
{
	public class ActionContainer : FakeContainer
	{
		#region Constants -------------------------------------

		private struct ActionNames
		{
			public const string ShowActions = "ShowActions";
			public const string ShowFirms = "ShowFirms";
            public const string ShowHeadCompanies = "ShowHeadCompanies";
        }

		#endregion

		#region Members ---------------------------------------
		private readonly Entity _firmEntity;
        private readonly Entity _actionEntity;
        private readonly Entity _headCompanyEntity;

        private static readonly Entity.Action[] menu = new[]
		{
		new Entity.Action(ActionNames.ShowHeadCompanies, "Акции c разбивкой по группам компаний"),	
		new Entity.Action(ActionNames.ShowFirms, "Акции с разбивкой на фирмы", Constants.ActionsImages.Firm),
		new Entity.Action(ActionNames.ShowActions, "Акции без разбивки на фирмы"),
		new Entity.Action(null, "-"),
		new Entity.Action(Constants.EntityActions.ShowFilters, "Установить фильтр", Constants.ActionsImages.Filter),
		new Entity.Action(Constants.EntityActions.Refresh, "Обновить", Constants.ActionsImages.Refresh)
		};

		#endregion

		#region Constructors ----------------------------------

		public ActionContainer(RelationScenario relationScenario, string caption, Entities firmEntity, Entities actionEntity, Entities headCompanyEntity)
			: base(caption, menu, relationScenario)
		{
			ChildEntity = RootEntity;
			ResolveFilterInitialValues();

			_firmEntity = EntityManager.GetEntity((int)firmEntity);
            _actionEntity = EntityManager.GetEntity((int)actionEntity);
			_headCompanyEntity = EntityManager.GetEntity((int)headCompanyEntity);	
        }

		#endregion

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (actionName == ActionNames.ShowActions)
				return ChildEntity.Id != (int) Entities.Action && ChildEntity.Id != (int)Entities.ActionDeleted;
			if (actionName == ActionNames.ShowFirms)
				return ChildEntity.Id != (int)Entities.FirmWithConfirmedActions && ChildEntity.Id != (int)Entities.FirmWithUnconfirmedActions && ChildEntity.Id != (int)Entities.FirmWithDeletedActions;
			if (actionName == ActionNames.ShowHeadCompanies)
				return ChildEntity.Id != (int)Entities.HeadCompanyWithConfirmedActions && ChildEntity.Id != (int)Entities.HeadCompanyWithUnconfirmedActions && ChildEntity.Id != (int)Entities.HeadCompanyWithDeletedActions;
            return true;
		}

		public override bool IsFilterable
		{
			get { return true; }
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			try
			{
				if (actionName == ActionNames.ShowActions)
				{
					ChildEntity = _actionEntity;
					FireContainerRefreshed();
				}
				else if (actionName == ActionNames.ShowFirms)
				{
					ChildEntity = _firmEntity;
					FireContainerRefreshed();
				}
                else if (actionName == ActionNames.ShowHeadCompanies)
                {
                    ChildEntity = _headCompanyEntity;
                    FireContainerRefreshed();
                }
                else if (actionName == Constants.EntityActions.ShowFilters)
				{
					ShowFilter(owner);
				}

				base.DoAction(actionName, owner, interfaceObject);
			}
            catch (Exception e)
            {
                ErrorManager.PublishError(e);
            }
        }

		public override void ShowFilter(IWin32Window owner)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				FilterForm frm = XmlFilter != null ? new ActionJournalFilter(RootEntity, XmlFilter, Globals.PrepareForFilter(RootEntity), _filter)
				                 	: new ActionJournalFilter(RootEntity, Globals.PrepareForFilter(RootEntity), _filter);

				if (frm.ShowDialog(owner) == DialogResult.OK)
					FireContainerRefreshed();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		public XPathNavigator XmlFilter { get; set; }

		public Entity RootEntity
		{
			get
			{
				return relationScenario.StartingEntity;
			}
		}
	}
}