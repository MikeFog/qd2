using System;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	internal class SpecialTariffWindow : TariffWindowWithRollerIssues
	{
		private readonly DateTime broadcastStart;

        public SpecialTariffWindow(DateTime broadcastStart) 
			: base(EntityManager.GetEntity((int) Entities.SpecialTariffWindow))
		{
			this[TariffWindow.ParamNames.Price] = decimal.Zero;
			this.broadcastStart = broadcastStart;
		}

		public int MassmediaID
		{
			get { return int.Parse(parameters[Massmedia.ParamNames.MassmediaId].ToString()); }
			set { parameters[Massmedia.ParamNames.MassmediaId] = value; }
		}

		public override bool Update()
		{
			if (isNew && parameters.ContainsKey("time"))
			{
				DateTime time = DateTime.Parse(parameters["time"].ToString());
				 
				// от начала дня: после отказа (например, «окно уже существует») повторный «ОК» не прибавляет время второй раз
				WindowDate = WindowDate.Date.AddHours(time.Hour).AddMinutes(time.Minute);
				if (time < broadcastStart) WindowDate = WindowDate.AddDays(1);
                WindowDateOriginal = WindowDate;
			}

			// Окно в занятую минуту или слот SQL отклоняет (TariffWindowIUD, UIX_TariffWindow_Tariff_Date, этап 7). Раньше здесь
			// искали окно с теми же двумя временами: найденное давало техническую ошибку (MissingMethodException в CreateObject),
			// а у сдвинутого окна не находилось ничего — и создавалось второе окно в ту же минуту или дубль слота.
			return base.Update();
		}
	}
}
