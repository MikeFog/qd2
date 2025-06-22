using System;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Forms;
using Merlin.Forms.FilterForm;

namespace Merlin.Classes.FakeContainers
{
	public class StudioOrderActionContainer : FakeContainer
	{
		private struct ActionNames
		{
			public const string ShowActions = "ShowActions";
			public const string ShowFirms = "ShowFirms";
		}

		private static readonly Entity.Action[] menu = new []
		                                               	{
		                                               		new Entity.Action(ActionNames.ShowFirms, "Акции с разбивкой на фирмы",
		                                               		                  Constants.ActionsImages.Firm),
		                                               		new Entity.Action(ActionNames.ShowActions,
		                                               		                  "Акции без разбивки на фирмы"),
															new Entity.Action(null, "-"),
															new Entity.Action(Constants.EntityActions.ShowFilters, "Установить фильтр", Constants.ActionsImages.Filter),
															new Entity.Action(Constants.EntityActions.Refresh, "Обновить", Constants.ActionsImages.Refresh)
		                                               	};

		public StudioOrderActionContainer()
			: base("Журнал заказов на ролики", menu, RelationManager.GetScenario(RelationScenarios.ProductionAction))
		{
			ChildEntity = EntityManager.GetEntity((int)Entities.FirmWithOrders);
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (actionName == ActionNames.ShowActions)
				return ChildEntity.Id != (int)Entities.StudioOrderAction;
			if (actionName == ActionNames.ShowFirms)
				return ChildEntity.Id != (int)Entities.FirmWithOrders;
			return true;
		}

		public override bool IsFilterable
		{
			get { return true; }
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == ActionNames.ShowActions)
			{
				ChildEntity = EntityManager.GetEntity((int)Entities.StudioOrderAction);
				FireContainerRefreshed();
			}
			else if (actionName == ActionNames.ShowFirms)
			{
				ChildEntity = EntityManager.GetEntity((int)Entities.FirmWithOrders);
				FireContainerRefreshed();
			}
			else if (actionName == Constants.EntityActions.ShowFilters)
			{
				ShowFilter(owner);
			}

			base.DoAction(actionName, owner, interfaceObject);
		}

		public override void ShowFilter(IWin32Window owner)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				FilterForm frm = XmlFilter != null ? new SOActionJournalFilter(RootEntity, XmlFilter, Globals.PrepareForFilter(RootEntity), _filter)
									: new SOActionJournalFilter(RootEntity, Globals.PrepareForFilter(RootEntity), _filter);

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
				return EntityManager.GetEntity((int)Entities.FirmWithOrders);
			}
		}
	}
}
