using System.Data;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	internal class MuteRoller : PresentationObject
	{
		public struct ParamNames
		{
			public const string RollerId = "rollerID";
			public const string Duration = "duration";
		}

		public MuteRoller() : base(GetEntity())
		{
		}

		public MuteRoller(DataRow row) : base(GetEntity(), row)
		{
		}

		private MuteRoller(int duration, int firmId, int? advertTypeId) : this()
		{
			this[ParamNames.Duration] = duration;
            this[Firm.ParamNames.FirmId] = firmId;
            if (advertTypeId.HasValue)
                this[AdvertType.ParamNames.AdvertTypeId] = advertTypeId.Value;
            this["withShow"] = 1;
            isNew = false;
		}

		public int RollerID
		{
			get { return ParseHelper.GetInt32FromObject(this[ParamNames.RollerId], 0); }
		}

		public static Roller GetRoller(int duration, int firmId, int? advertTypeId)
		{
			MuteRoller roller = new MuteRoller(duration, firmId, advertTypeId);
			roller.Refresh();
			Roller r = new Roller(roller.RollerID);
			return r;
		}

		public static Entity GetEntity()
		{
			return EntityManager.GetEntity((int) Entities.MuteRoller);
		}
	}
}