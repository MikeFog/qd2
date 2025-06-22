using System;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Forms;
using Merlin.Forms;

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

	public class Tariff : PresentationObject
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

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch(actionName)
			{
				case Constants.Actions.Clone:
                    Tariff tariff = new Tariff
                    {
                        parameters = Parameters
                    };
                    tariff.parameters[ParamNames.TariffId] = null;
					tariff.parameters[Constants.ParamNames.ActionName] = Constants.Actions.AddItem;

					if (tariff.ShowPassport(owner))
						//OnObjectCreated(tariff);
						OnObjectCloned(tariff);
					break;
				
				default:
					base.DoAction(actionName, owner, interfaceObject);
					break;
			}
		}

		public override PassportForm GetPassportForm(DataSet ds)
		{
			return new TariffPassport(this, ds);
		}
	}
}