using System;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public class ModuleTariff : Tariff
	{
		public ModuleTariff() : base(EntityManager.GetEntity((int) Entities.ModuleTariff))
		{
		}

		public ModuleTariff(DataRow row)
			: base(EntityManager.GetEntity((int)Entities.ModuleTariff), row)
		{
		}
	}

	// UI-часть (DoAction) — в Tariff.WinForms.cs.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class Tariff : PresentationObject
	{
		public struct ParamNames
		{
			public const string TariffId = "tariffID";
			public const string IsSpecial = "isSpecial";
			public const string Monday = "monday";
			public const string Tuesday = "tuesday";
			public const string Wednesday = "wednesday";
			public const string Thursday = "thursday";
			public const string Friday = "friday";
			public const string Saturday = "saturday";
			public const string Sunday = "sunday";
			public const string Time = "time";
			public const string Duration = "duration";
            public const string DurationTotal = "duration_total";
            public const string Price = "price";
			public const string TimeString = "timeString";
		}

		public Tariff() : base(EntityManager.GetEntity((int) Entities.Tariff))
		{
		}

		public Tariff(DataRow row) : base(EntityManager.GetEntity((int) Entities.Tariff), row)
		{
		}

		private Tariff(int tariffID) : base(EntityManager.GetEntity((int) Entities.Tariff))
		{
			this[ParamNames.TariffId] = tariffID;
			isNew = false;
		}
        
		public Tariff(Entity entity) : base(entity)
		{
		}

		public Tariff(Entity entity, DataRow row) : base(entity, row)
		{
		}

		internal bool IsSpecial
		{
			get { return bool.Parse(this[ParamNames.IsSpecial].ToString()); }
			set { this[ParamNames.IsSpecial] = value; }
		}

		internal bool Monday
		{
			get { return bool.Parse(this[ParamNames.Monday].ToString()); }
			set { this[ParamNames.Monday] = value; }
		}

		internal bool Tuesday
		{
			get { return bool.Parse(this[ParamNames.Tuesday].ToString()); }
			set { this[ParamNames.Tuesday] = value; }
		}

		internal bool Wednesday
		{
			get { return bool.Parse(this[ParamNames.Wednesday].ToString()); }
			set { this[ParamNames.Wednesday] = value; }
		}

		internal bool Thursday
		{
			get { return bool.Parse(this[ParamNames.Thursday].ToString()); }
			set { this[ParamNames.Thursday] = value; }
		}

		internal bool Friday
		{
			get { return bool.Parse(this[ParamNames.Friday].ToString()); }
			set { this[ParamNames.Friday] = value; }
		}

		internal bool Saturday
		{
			get { return bool.Parse(this[ParamNames.Saturday].ToString()); }
			set { this[ParamNames.Saturday] = value; }
		}

		internal bool Sunday
		{
			get { return bool.Parse(this[ParamNames.Sunday].ToString()); }
			set { this[ParamNames.Sunday] = value; }
		}

		internal int PricelistID
		{
			get { return int.Parse(this[Pricelist.ParamNames.PricelistId].ToString()); }
			set { this[Pricelist.ParamNames.PricelistId] = value; }
		}

		internal Pricelist Pricelist
		{
			get { return Pricelist.GetPricelistById(PricelistID, EntityManager.GetEntity((int) Entities.Pricelist)); }
		}

		internal DateTime Time
		{
			get { return DateTime.Parse(this[ParamNames.Time].ToString()); }
		}

		internal string TimeString
		{
			get { return this[ParamNames.TimeString].ToString(); }
		}

		internal decimal Price
		{
			get { return decimal.Parse(this[ParamNames.Price].ToString()); }
		}

		internal int Duration
		{
			get { return int.Parse(this[ParamNames.Duration].ToString()); }
		}

        internal int DurationTotal
        {
            get { return int.Parse(this[ParamNames.DurationTotal].ToString()); }
        }

        internal int TariffId
		{
			get { return int.Parse(IDs[0].ToString()); }
		}

		internal static Tariff GetTariffByID(int tariffID)
		{
			Tariff tariff = new Tariff(tariffID);
			tariff.Refresh();
			return tariff;
		}

		// DoAction переехал в Tariff.WinForms.cs (IWin32Window в сигнатуре).

		// GetPassportForm переехал в Tariff.WinForms.cs (возвращает UI-тип PassportForm).
	}
}