using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

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
				 
				WindowDate = WindowDate.AddHours(time.Hour).AddMinutes(time.Minute);
				if (time < broadcastStart) WindowDate = WindowDate.AddDays(1);
                WindowDateOriginal = WindowDate;
			}

			SpecialTariffWindow specialTariffWindow = GetSpecialTariffWindow();
			if (specialTariffWindow != null)
			{
				parameters = specialTariffWindow.parameters;
				isNew = false;
			}
			return base.Update();
		}

		private SpecialTariffWindow GetSpecialTariffWindow()
		{
			Entity specialTariffWindow = EntityManager.GetEntity((int)Entities.SpecialTariffWindow);
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(specialTariffWindow);
			procParameters[TariffWindow.ParamNames.WindowDateActual] = WindowDate;
			procParameters[TariffWindow.ParamNames.WindowDateOriginal] = WindowDateOriginal;
			procParameters[Massmedia.ParamNames.MassmediaId] = MassmediaID;

			DataSet ds = DataAccessor.DoAction(procParameters) as DataSet;
			if (ds == null || ds.Tables[Constants.TableNames.Data].Rows.Count == 0)
				return null;
			return (SpecialTariffWindow)specialTariffWindow.CreateObject(ds.Tables[Constants.TableNames.Data].Rows[0]);
		}
	}
}
