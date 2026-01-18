using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	internal abstract class Issue : CampaignPart
	{
		internal struct ParamNames
		{
			public const string IssueId = "issueID";
			public const string TariffId = "tariffID";
			public const string TariffPrice = "tariffPrice";
			public const string IssueDate = "issueDate";
			public const string Position = "positionId";
            public const string ModuleIssueId = "moduleIssueID";
            public const string PackModuleIssueID = "packModuleIssueID";
        }

		internal struct ActionNames
		{
			public const string SetFirst = "SetFirst";
			public const string SetSecond = "SetSecond";
			public const string SetLast = "SetLast";
			public const string SetUnknow = "SetUnknow";
		}

		protected Issue(Entity entity) : base(entity)
		{
		}

		protected Issue(Entity entity, DataRow row) : base(entity, row)
		{
		}

		protected int TariffId
		{
			get { return Int32.Parse(this[Tariff.ParamNames.TariffId].ToString()); }
		}

		internal decimal TariffPrice
		{
			set { this[ParamNames.TariffPrice] = value; }
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (string.Compare(actionName, ActionNames.SetFirst) == 0)
				UpdatePosition(RollerPositions.First);
			else if (string.Compare(actionName, ActionNames.SetSecond) == 0)
				UpdatePosition(RollerPositions.Second);
			else if (string.Compare(actionName, ActionNames.SetLast) == 0)
				UpdatePosition(RollerPositions.Last);
			else if (string.Compare(actionName, ActionNames.SetUnknow) == 0)
				UpdatePosition(RollerPositions.Undefined);
			else base.DoAction(actionName, owner, interfaceObject);
		}

		public RollerPositions Position
		{
			get { return (RollerPositions)Enum.Parse(typeof(RollerPositions), this[ParamNames.Position].ToString()); }
		}

		private void UpdatePosition(RollerPositions pos)
		{
			decimal price = decimal.Zero;
            if(Campaign != null && Campaign.Action != null)
			{
				Campaign.Action.Refresh();
                price = Campaign.Action.TotalPrice;
            }
			
			try
			{
				SetPosition(pos);
				Refresh();
				OnParentChanged(this, 1);
				RecalculateAndShowPriceChange(price);
			}
			catch (Exception exp)
			{
				MessageAccessor.Parameters = parameters;
				ErrorManager.PublishError(exp);
				Application.DoEvents();
				Refresh();
			}
		}

		public void SetPosition(RollerPositions pos)
		{
            this[ParamNames.Position] = pos;
            Update();
        }

		public override bool IsActionHidden(string actionName, ViewType type)
		{
			if (!ActionOnMassmedia.CheckLoggedUserRight(actionName, Campaign.Action))
				return true;

			return base.IsActionHidden(actionName, type);
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (!ActionOnMassmedia.CheckLoggedUserRight(actionName, Campaign.Action))
				return false;

			if (string.Compare(actionName, ActionNames.SetFirst) == 0)
				return base.IsActionEnabled(actionName, type) && (Position != RollerPositions.First && Position != RollerPositions.FirstTransferred);
			else if (string.Compare(actionName, ActionNames.SetSecond) == 0)
				return base.IsActionEnabled(actionName, type) && (Position != RollerPositions.Second && Position != RollerPositions.SecondTransferred);
			else if (string.Compare(actionName, ActionNames.SetLast) == 0)
				return base.IsActionEnabled(actionName, type) && (Position != RollerPositions.Last && Position != RollerPositions.LastTransferred);
			else if (string.Compare(actionName, ActionNames.SetUnknow) == 0)
				return base.IsActionEnabled(actionName, type) && (Position != RollerPositions.Undefined);
			return base.IsActionEnabled(actionName, type);
		}

		public abstract DateTime IssueDate { get; }

		public const int AttributeSelectorShort = 1;
		public const int AttributeSelectorFull = 2;

        public static List<KeyValuePair<int, string>> GetRollerPositionItems()
        {
            return new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>((int)RollerPositions.Undefined, "без позиции"),
                new KeyValuePair<int, string>((int)RollerPositions.First, "первый"),
                new KeyValuePair<int, string>((int)RollerPositions.Second, "второй"),
                new KeyValuePair<int, string>((int)RollerPositions.Last, "последний"),
            };
        }
	}
}