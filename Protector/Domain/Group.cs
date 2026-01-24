using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using FogSoft.WinForm.Passport.Forms;
using Protector.Forms;

namespace Protector.Domain
{
	public class Group : ObjectContainer
	{
		public struct ParamNames
		{
			public const string MenuID = "menuID";
			public const string GroupID = "groupID";
			public const string EntityActionID = "entityActionID";
		}

		#region Constructor -----------------------------------

		public Group() : base(EntityManager.GetEntity((int) Entities.Group))
		{
			base.ChildEntity = EntityManager.GetEntity((int) Entities.GroupMember);
		}

		public Group(DataRow row) : base(EntityManager.GetEntity((int) Entities.Group), row)
		{
			base.ChildEntity = EntityManager.GetEntity((int) Entities.GroupMember);
		}

		//public Group(Entity entity, DataRow row) : this(row) {}		

		#endregion

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch (actionName)
			{
				case "EditMenuAccess":
					EditMenuAccess(owner);
					break;

				case "EditGroupRights":
					EditGroupRights(owner);
					break;

				case Constants.EntityActions.AssignExisting:
					AssignExistingUser(owner);
					break;

				case "ShowGroupMembers":
					base.ChildEntity = EntityManager.GetEntity((int) Entities.GroupMember);
					FireContainerRefreshed();
					break;

				case "ShowGroupRights":
					base.ChildEntity = EntityManager.GetEntity((int) Entities.GroupRightsAssigned);
					FireContainerRefreshed();
					break;

				case Constants.EntityActions.AssignNew:
					AssignNewUser(actionName, owner, interfaceObject);
					break;


				case "SetManagerDiscount":
					SetManagerDiscount(owner);
                    break;


                default:
					base.DoAction(actionName, owner, interfaceObject);
					break;
			}
		}

		private void SetManagerDiscount(IWin32Window owner)
		{
			UserDiscount userDiscount = new UserDiscount();
			PassportForm passport = userDiscount.GetPassportForm(null);
			passport.ShowDialog(owner);
        }

        private void AssignExistingUser(IWin32Window owner)
		{
			SelectionForm selector =
				new SelectionForm(EntityManager.GetEntity((int) Entities.User), "Выбор пользователя");
			if (selector.ShowDialog(owner) == DialogResult.OK)
			{
				Dictionary<string, object> newObjParameters = selector.SelectedObject.Parameters;
				newObjParameters[ParamNames.GroupID] = parameters[ParamNames.GroupID];

				PresentationObject presentationObject =
					EntityManager.GetEntity((int) Entities.GroupMember).CreateObject(newObjParameters);

				presentationObject.IsNew = true;
				presentationObject.Update();
				OnObjectCreated(presentationObject);
				FireContainerRefreshed();
			}
		}

		private void AssignNewUser(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			Entity oldEntity = base.ChildEntity;
			base.ChildEntity = EntityManager.GetEntity((int) Entities.User);
			base.DoAction(actionName, owner, interfaceObject);
			base.ChildEntity = oldEntity;
			FireContainerRefreshed();
		}

		private void EditMenuAccess(IWin32Window owner)
		{
			Application.DoEvents();
			Cursor.Current = Cursors.WaitCursor;

			// Load menu items allowed for this group
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(EntityManager.GetEntity((int) Entities.GroupMenu),
				                               InterfaceObjects.SimpleJournal, Constants.Actions.Load);
			procParameters[ParamNames.GroupID] = parameters[ParamNames.GroupID];

			DataSet ds = (DataSet) DataAccessor.DoAction(procParameters);

			TreeViewSelector selector =
				new TreeViewSelector(RelationManager.GetScenario(FrmMain.RelationScenarios.MenuItem),
				                     "Пункты меню", true, ds.Tables[Constants.TableNames.Data]);
			selector.SelectedItemsImageColumn = "img";
			if (selector.ShowDialog(owner) == DialogResult.OK)
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				ProcessMenu(selector.AddedItems, true);
				ProcessMenu(selector.DeletedItems, false);

				Cursor.Current = Cursors.Default;
			}
		}

		private void ProcessMenu(IEnumerable<PresentationObject> menuList, bool updateFlag)
		{
			Entity entityGroup = EntityManager.GetEntity((int) Entities.GroupMenu);

			foreach (PresentationObject menu in menuList)
			{
				Dictionary<string, object> procParameters =
					new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

				procParameters[ParamNames.GroupID] = parameters[ParamNames.GroupID];
				procParameters[ParamNames.MenuID] = menu.IDs[0];
				PresentationObject presentationObject = entityGroup.CreateObject(procParameters);

				if (updateFlag) presentationObject.Update();
				else presentationObject.Delete(true);
			}
		}

		private void EditGroupRights(IWin32Window owner)
		{
			Entity oldChild = ChildEntity;
			ChildEntity = EntityManager.GetEntity((int) Entities.GroupRight);
			SelectionForm selector = new SelectionForm(ChildEntity, GetContent().DefaultView, "Права для группы", true);
			if (selector.ShowDialog(owner) == DialogResult.OK)
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				ProcessGroupRights(selector.AddedItems, true);
				ProcessGroupRights(selector.DeletedItems, false);

				Cursor.Current = Cursors.Default;
			}
			ChildEntity = oldChild;
		}

		private void ProcessGroupRights(IEnumerable<PresentationObject> groupRightsList, bool updateFlag)
		{
			Entity oldChild = ChildEntity;
			ChildEntity = EntityManager.GetEntity((int) Entities.GroupRight);
			foreach (PresentationObject entityAction in groupRightsList)
			{
				Dictionary<string, object> newObjParameters =
					new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

				newObjParameters[ParamNames.GroupID] = parameters[ParamNames.GroupID];
				newObjParameters[ParamNames.EntityActionID] = entityAction.IDs[0];

				PresentationObject presentationObject = ChildEntity.CreateObject(newObjParameters);
				if (updateFlag) presentationObject.Update();
				else presentationObject.Delete(true);
			}
			ChildEntity = oldChild;
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (string.Compare(actionName, "ShowGroupMembers") == 0)
				return ChildEntity.Id != (int) Entities.GroupMember;
			if (string.Compare(actionName, "ShowGroupRights") == 0)
				return ChildEntity.Id != (int) Entities.GroupRightsAssigned;
			return base.IsActionEnabled(actionName, type);
		}
	}
}