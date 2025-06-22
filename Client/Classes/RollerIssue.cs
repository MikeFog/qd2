using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Properties;

namespace Merlin.Classes
{
	internal class RollerIssue : Issue
	{
		private Roller roller;

		internal enum AttributeSelectors
		{
			TrafficManager = 1
		}

		internal new struct ParamNames
		{
			public const string IssueDate = "issueDate";
			public const string RollerDuration = "rollerDuration";
			public const string RollerName = "rollerName";
			public const string StartDate = "startDate";
			public const string FinishDate = "finishDate";
			public const string NewPosition = "newPosition";
		}

		#region Constructors ----------------------------------

		public RollerIssue() : base(GetEntity())
		{
		}

		public RollerIssue(DataRow row)
			: base(GetEntity(), row)
		{
		}

		public RollerIssue(Campaign campaign, PresentationObject roller, TariffWindowWithRollerIssues tariffWindow,
		                   RollerPositions position,
		                   bool isConfirmed, int? grantorID)
			: this()
		{
			this[Roller.ParamNames.RollerId] = roller.IDs[0];
			this[Campaign.ParamNames.CampaignId] = campaign.CampaignId;
			this[TariffWindow.ParamNames.WindowId] = tariffWindow.WindowId;
			this["tariffWindowPrice"] = tariffWindow.Price;
			this[ParamNames.IssueDate] = tariffWindow.WindowDate;
			this[ParamNames.RollerDuration] = roller[Roller.ParamNames.Duration];
			this[Action.ParamNames.IsConfirmed] = isConfirmed;
			this[Issue.ParamNames.Position] = (int)position;
			this["grantorID"] = (grantorID ?? (object)DBNull.Value);
		}

		#endregion

		public override DateTime IssueDate
		{
			get { return DateTime.Parse(parameters[ParamNames.IssueDate].ToString()); }
		}

		public int Duration
		{
			get { return int.Parse(parameters[Roller.ParamNames.Duration].ToString()); }
		}

		private int RollerId
		{
			get { return int.Parse(this[Roller.ParamNames.RollerId].ToString()); }
		}

		internal Roller Roller
		{
			get
			{
				if (roller == null)
					roller = new Roller(RollerId);
				return roller;
			}
		}

		internal void Transfer(ITariffWindow destinationWindow, Massmedia massmedia)
		{
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(entity, InterfaceObjects.FakeModule, Constants.Actions.Transfer);

			procParameters[Issue.ParamNames.IssueId] = this[Issue.ParamNames.IssueId];
			procParameters[Campaign.ParamNames.CampaignId] = this[Campaign.ParamNames.CampaignId];
			procParameters[Massmedia.ParamNames.MassmediaId] = massmedia.MassmediaId;

			procParameters[Action.ParamNames.IsConfirmed] = this[Action.ParamNames.IsConfirmed];
			procParameters["newWindowID"] = ((TariffWindow)destinationWindow).WindowId;
			procParameters["newDate"] = destinationWindow.WindowDate;
			SetNewPosition((TariffWindowWithRollerIssues)destinationWindow, procParameters);

			DataAccessor.DoAction(procParameters);
			Refresh();
		}

		private void SetNewPosition(TariffWindowWithRollerIssues destinationWindow,
		                            Dictionary<string, object> procParameters)
		{
			if (Position == RollerPositions.Undefined)
				procParameters[ParamNames.NewPosition] = (int) RollerPositions.Undefined;
			else if (Position == RollerPositions.First || Position == RollerPositions.FirstTransferred)
			{
				procParameters[ParamNames.NewPosition] = destinationWindow.IsFirstPositionOccupied
				                                         	?
				                                         (int) RollerPositions.FirstTransferred
				                                         	: (int) RollerPositions.First;
			}
			else if (Position == RollerPositions.Second || Position == RollerPositions.SecondTransferred)
			{
				procParameters[ParamNames.NewPosition] = destinationWindow.IsSecondPositionOccupied
				                                         	?
				                                         (int) RollerPositions.SecondTransferred
				                                         	: (int) RollerPositions.Second;
			}
			else if (Position == RollerPositions.Last || Position == RollerPositions.LastTransferred)
			{
				procParameters[ParamNames.NewPosition] = destinationWindow.IsLastPositionOccupied
				                                         	?
				                                         (int) RollerPositions.LastTransferred
				                                         	: (int) RollerPositions.Last;
			}
		}

		public static Entity GetEntity()
		{
			return EntityManager.GetEntity((int) Entities.Issue);
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (string.Compare(actionName, Constants.Actions.Substitute) == 0)
				SubstituteRollerForSingleIssue(Roller);
			else base.DoAction(actionName, owner, interfaceObject);
		}
	}
}