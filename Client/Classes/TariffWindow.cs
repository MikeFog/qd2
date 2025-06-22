using System;
using System.Data;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public abstract class TariffWindow : PresentationObject, ITariffWindow
	{
		public struct ParamNames
		{
			public const string WindowDateActual = "windowDateActual";
			public const string Price = "price";
			public const string WindowId = "windowId";
			public const string OriginalWindowId = "originalWindowId";
			public const string WindowDateBroadcast = "windowDateBroadcast";
			public const string WindowDateOriginal = "windowDateOriginal";
			public const string MaxCapacity = "maxCapacity";
			public const string CapacityInUseConfirmed = "capacityInUseConfirmed";
			public const string Duration = "duration";
            public const string DurationTotal = "duration_total";
            public const string TimeInUseConfirmed = "timeInUseConfirmed";
			public const string TimeInUseUnconfirmed = "timeInUseUnconfirmed";
			public const string RollerID = "rollerID";
            public const string IsDisabled = "isDisabled";
            public const string IsMarked = "isMarked";
        }

		protected TariffWindow(Entity entity) : base(entity)
		{
		}

		protected TariffWindow(Entity entity, DataRow row) : base(entity, row)
		{
		}

		public DateTime WindowDate
		{
			get { return DateTime.Parse(this[ParamNames.WindowDateActual].ToString()); }
			set {this[ParamNames.WindowDateActual] = value; }
		}

		public DateTime WindowDateOriginal
		{
			get { return DateTime.Parse(this[ParamNames.WindowDateOriginal].ToString()); }
			set { this[ParamNames.WindowDateOriginal] = value; }
		}

		public decimal Price
		{
			get { return decimal.Parse(this[ParamNames.Price].ToString()); }
		}

		public int TariffId
		{
			get { return ParseHelper.ParseToInt32(this[Tariff.ParamNames.TariffId].ToString()); }
		}

		public abstract DataTable LoadIssues(bool showUnconfirmed, Entity issueEntity);

		public int WindowId
		{
			get { return ParseHelper.ParseToInt32(parameters[ParamNames.WindowId].ToString()); }
		}

		public DateTime WindowDateBroadcast
		{
			get { return DateTime.Parse(this[ParamNames.WindowDateBroadcast].ToString()); }
		}

		public int MaxCapacity
		{
			get { return ParseHelper.ParseToInt32(this[ParamNames.MaxCapacity].ToString()); }
		}

		public int CapacityInUseConfirmed
		{
			get { return ParseHelper.ParseToInt32(this[ParamNames.CapacityInUseConfirmed].ToString()); }
			set { this[ParamNames.CapacityInUseConfirmed] = value; }
		}

		public int Duration
		{
			get { return ParseHelper.ParseToInt32(this[ParamNames.Duration].ToString()); }
		}

        public int DurationTotal
        {
            get { return ParseHelper.ParseToInt32(this[ParamNames.DurationTotal].ToString()); }
        }

        public int TimeInUseConfirmed
		{
			get { return ParseHelper.ParseToInt32(this[ParamNames.TimeInUseConfirmed].ToString()); }
			set { this[ParamNames.TimeInUseConfirmed] = value; }
		}

		public int TimeInUseUnconfirmed
		{
			get { return ParseHelper.ParseToInt32(this[ParamNames.TimeInUseUnconfirmed].ToString()); }
			set { this[ParamNames.TimeInUseUnconfirmed] = value; }
		}

		public int TimeLeftWithUnconfirmed
        {
            get { return Duration - TimeInUseConfirmed - TimeInUseUnconfirmed; }
        }

        public bool IsDisabled
		{
			get { return (bool)this[ParamNames.IsDisabled]; }
		}

        public bool IsMarked 
		{
            get { return (bool)this[ParamNames.IsMarked]; }
        }
    }
}