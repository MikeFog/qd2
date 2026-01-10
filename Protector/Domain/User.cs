using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Protector.Forms;
using Protector.License;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows.Forms;

namespace Protector.Domain
{
	class UserMassmedia : PresentationObject
	{
		public UserMassmedia(int massmediaID, int userID)
			: base(EntityManager.GetEntity((int)Entities.UserMassmedia))
		{
			parameters[User.ParamNames.UserID] = userID;
			parameters["massmediaID"] = massmediaID;
		}
	}

	class UserMassmediaAdd : PresentationObject
	{
		public UserMassmediaAdd(int massmediaID, int userID)
			: base(EntityManager.GetEntity((int)Entities.UserMassmediaAdd))
		{
			parameters[User.ParamNames.UserID] = userID;
			parameters["massmediaID"] = massmediaID;
		}
	}

	public class User : ObjectContainer 
	{
		public struct ParamNames
		{
			public const string UserID = "userID";
			public const string MenuID = "menuID";
		}

		public User()
			: base(EntityManager.GetEntity((int)Entities.User))
		{
		}

		public User(DataRow row)
			: base(EntityManager.GetEntity((int)Entities.User), row)
		{
		}
		
		public User(Entity entity) : base(entity)
		{
		}

		public User(Entity entity, DataRow row) : base(entity, row)
		{
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (string.Compare(actionName, "EditUserAdditionalRights") == 0)
				EditUserAdditionalRights(owner);
			if (string.Compare(actionName, "EditUserAdditionalMenus") == 0)
				EditUserAdditionalMenus(owner);
			if (string.Compare(actionName, "Groups") == 0)
				ShowUserGroups();
			if (string.Compare(actionName, "EditDiscount") == 0)
				EditUserDiscount();
			base.DoAction(actionName, owner, interfaceObject);
		}

		private void EditUserDiscount()
		{
			Entity ugEntity = EntityManager.GetEntity((int)Entities.UserDiscount);
			Dictionary<string, object> dictionary = DataAccessor.CreateParametersDictionary();
			dictionary[ParamNames.UserID] = UserID;
			Globals.ShowSimpleJournal(ugEntity, string.Format("{0} - {1}", ugEntity.Name, Name), dictionary);
		}

		private void ShowUserGroups()
		{
			Entity ugEntity = EntityManager.GetEntity((int)Entities.UserGroup);
			Dictionary<string, object> dictionary = DataAccessor.CreateParametersDictionary();
			dictionary[ParamNames.UserID] = UserID;
			Globals.ShowSimpleJournal(ugEntity, string.Format("{0} - {1}", ugEntity.Name, Name), dictionary);
		}

		private void EditUserAdditionalMenus(IWin32Window owner)
		{
			Application.DoEvents();
			Cursor.Current = Cursors.WaitCursor;

			// Load menu items allowed for this user
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(EntityManager.GetEntity((int)Entities.UserAdditionalMenu),
				                               InterfaceObjects.SimpleJournal, Constants.Actions.Load);
			procParameters[ParamNames.UserID] = parameters[ParamNames.UserID];
            procParameters.Add("languageCode", ConfigurationManager.AppSettings["Language"] ?? "ru");
            DataSet ds = (DataSet)DataAccessor.DoAction(procParameters);

			TreeViewSelector selector =
				new TreeViewSelector(RelationManager.GetScenario(FrmMain.RelationScenarios.MenuItem),
				                     "Пункты меню пользователя", true, ds.Tables[Constants.TableNames.Data]) { SelectedItemsImageColumn = "img", SelectedItemsBitColumn = "isObjectSelected" };
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
			Entity entityGroup = EntityManager.GetEntity((int)Entities.UserAdditionalMenu);

			foreach (PresentationObject menu in menuList)
			{
				Dictionary<string, object> procParameters =
					new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

				procParameters[ParamNames.UserID] = parameters[ParamNames.UserID];
				procParameters[ParamNames.MenuID] = menu.IDs[0];
				PresentationObject presentationObject = entityGroup.CreateObject(procParameters);

				if (updateFlag) presentationObject.Update();
				else presentationObject.Delete(true);
			}
		}

		private void EditUserAdditionalRights(IWin32Window owner)
		{
			ChildEntity = EntityManager.GetEntity((int)Entities.UserAdditionalRight);
			SelectionForm selector = new SelectionForm(ChildEntity, GetContent().DefaultView, "Права для пользователя", true);
			if (selector.ShowDialog(owner) == DialogResult.OK)
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				ProcessGroupRights(selector.AddedItems, true);
				ProcessGroupRights(selector.DeletedItems, false);

				Cursor.Current = Cursors.Default;
			}
		}

		private void ProcessGroupRights(List<PresentationObject> groupRightsList, bool updateFlag)
		{
			ChildEntity = EntityManager.GetEntity((int)Entities.UserAdditionalRight);
			foreach (PresentationObject entityAction in groupRightsList)
			{
				Dictionary<string, object> newObjParameters =
					new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

				newObjParameters[ParamNames.UserID] = UserID;
				newObjParameters[Group.ParamNames.EntityActionID] = entityAction.IDs[0];

				PresentationObject presentationObject = ChildEntity.CreateObject(newObjParameters);
				if (updateFlag) presentationObject.Update();
				else presentationObject.Delete(true);
			}
		}

		public override bool Update()
		{
			if (IsNew && !AdvertAgLicence.CheckLicenseUsersCountForAdd())
				return false;

			return base.Update();
		}
        
		public int UserID
		{
			get { return ParseHelper.GetInt32FromObject(this[ParamNames.UserID], 0); }
		}

		public override bool ShowPassport(IWin32Window owner)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				// load data to display Passport
				DataAccessor.PrepareParameters(parameters, entity, InterfaceObjects.PropertyPage,
				                               Constants.Actions.Load);

				DataSet ds = null;
				if (DataAccessor.IsProcedureExist(parameters))
				{
					ds = DataAccessor.DoAction(parameters) as DataSet;
				}

				bool isNewObject = IsNew;
				UserPassport passport = new UserPassport(this, ds);
				//TODO: !passport.ApplyClicked
				bool res = (passport.ShowDialog(owner) == DialogResult.OK) /*|| passport.ApplyClicked*/;

				// Fire event only if existing object was changed
				if (res && !isNewObject) OnObjectChanged(this);
				return res;
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
				return false;
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}
	}
}