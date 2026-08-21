using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	internal abstract partial class Issue : CampaignPart
	{
		internal struct ParamNames
		{
			public const string IssueId = "issueID";
			public const string TariffId = "tariffID";
			public const string TariffPrice = "tariffPrice";
			public const string IssueDate = "issueDate";
			public const string PositionId = "positionId";
            public const string PositionName = "issuePosition";
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

		// DoAction/UpdatePosition переехали в Issue.WinForms.cs (UpdatePosition
		// использует Application.DoEvents в catch-ветке).

		public RollerPositions Position
		{
			get { return (RollerPositions)Enum.Parse(typeof(RollerPositions), this[ParamNames.PositionId].ToString()); }
		}


		public void SetPosition(RollerPositions pos)
		{
            this[ParamNames.PositionId] = pos;
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
                new KeyValuePair<int, string>((int)RollerPositions.Undefined, "Не определен"),
                new KeyValuePair<int, string>((int)RollerPositions.First, "Первый"),
                new KeyValuePair<int, string>((int)RollerPositions.Second, "Второй"),
                new KeyValuePair<int, string>((int)RollerPositions.Last, "Последний"),
            };
        }

        public static string GetPositionDisplayName(RollerPositions position)
        {
            switch (position)
            {
                case RollerPositions.First:
                case RollerPositions.FirstTransferred:
                    return "Первый";
                case RollerPositions.Second:
                case RollerPositions.SecondTransferred:
                    return "Второй";
                case RollerPositions.Last:
                case RollerPositions.LastTransferred:
                    return "Последний";
                default:
                    return "Не опеределен";
            }
        }
    }
}
