using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	public class Announcement : PresentationObject
	{
		public Announcement() : base(EntityManager.GetEntity((int) Entities.Announcement))
		{
		}

		protected Announcement(Entity entity)
			: base(entity)
		{
		}

		public Announcement(DataRow row)
			: base(EntityManager.GetEntity((int)Entities.Announcement), row)
		{
		}

		public Announcement(int announcementID)
			: base(EntityManager.GetEntity((int) Entities.Announcement))
		{
			parameters[ParamNames.AnnouncementId] = announcementID.ToString();
		}

		private bool IsConfirmed
		{
			get { return !StringUtil.IsDBNullOrNull(parameters[ParamNames.ConfirmationDate]); }
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (actionName == ActionNames.MarkAsRead)
				return base.IsActionEnabled(actionName, type) && !IsConfirmed;
			return base.IsActionEnabled(actionName, type);
		}

		public override void DoAction(string actionName, IWin32Window owner,
		                              InterfaceObjects interfaceObject)
		{
			if (actionName == ActionNames.MarkAsRead)
				SetReadMark(true);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		private void SetReadMark(bool mark)
		{
			parameters[ParamNames.ConfirmationDate] = mark ? (object)DateTime.Now : null;
			Update();
			Refresh();
		}

		public static bool HasNewEvents(DateTime date)
		{
			return HasNewEvents(date, SecurityManager.LoggedUser.Id);
		}

		private static bool HasNewEvents(DateTime date, int userID)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["date"] = date;
			parameters["userID"] = userID;

			object rc = DataAccessor.ExecuteScalar("AnnouncementCheckForNew", parameters, true);
			return (!StringUtil.IsDBNullOrNull(rc));
		}

		#region Nested type: ParamNames

		public struct ActionNames
		{
			public const string MarkAsRead = "MarkAsRead";
		}

		public struct ParamNames
		{
			public const string AnnouncementId = "announcementID";
			public const string ConfirmationDate = "dateConfirmed";
		}

		#endregion
	}
}