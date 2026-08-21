using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	// UI-часть (DoAction, ShowDisabledWindows, ChangeTariffWindowsMarkedStatus,
	// ChangeTariffWindowsDisabedStatus, диалоговые обёртки GenerateTariffWindows/
	// DeleteGeneratedWindows) — в MassmediaPricelist.WinForms.cs.
	// GenerateTariffWindows(object,DoWorkEventArgs)/DeleteGeneratedTariffWindows/
	// CheckLinkedWindows остаются здесь: BackgroundWorker/DoWorkEventArgs — это
	// System.ComponentModel, не WinForms, и сами эти методы не показывают диалог.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class MassmediaPricelist : Pricelist
	{
		public new struct ParamNames
		{
			public const string ExcludeModuleTariffs = "excludeModuleTariffs";
			public const string ExcludeSpecialWindows = "excludeSpecialWindows";
			public const string BroadcastStart = "broadcastStart";
            public const string ShowTrafficWindows = "showTrafficWindows";
            public const string ShowDisabledWindows = "showDisabledWindows";
            public const string UseActualTime = "useActualTime";
        }

		private struct Actions
		{
			public const string GenerateWindows = "GenerateWindows";
			public const string DeleteGeneratedWindows = "DeleteGeneratedWindows";
			public const string DisabledTariffWindows = "DisabledTariffWindows";
			public const string EnabledTariffWindows = "EnabledTariffWindows";
			public const string ShowDisabledWindows = "ShowDisabledWindows";
            public const string MarkWindows = "MarkWindows";
            public const string UnmarkWindows = "UnmarkWindows";
        }

		private bool excludeModuleTariffs = true;
		private bool excludeSpecialWindows = true;

		public MassmediaPricelist() : base(GetPriceListEntity())
		{
		}

		public MassmediaPricelist(DataRow row) : base(GetPriceListEntity(), row)
		{
		}

		protected MassmediaPricelist(Entity entity) : base(entity)
		{
		}

		protected MassmediaPricelist(Entity entity, DataRow row) : base(entity, row)
		{
		}

		public bool ExcludeModuleTariffs
		{
			set { excludeModuleTariffs = value; }
		}

		public bool ExcludeSpecialWindows
		{
			set { excludeSpecialWindows = value; }
		}

		public DateTime BroadcastStart
		{
			get
			{
				if (!parameters.ContainsKey(ParamNames.BroadcastStart)) Refresh();
				return DateTime.Parse(this[ParamNames.BroadcastStart].ToString());
			}
		}

		public int MassmediaId
		{
			get { return int.Parse(this[Massmedia.ParamNames.MassmediaId].ToString()); }
		}

		// DoAction и ShowDisabledWindows переехали в MassmediaPricelist.WinForms.cs.

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if(actionName == Constants.EntityActions.AssignNew)
				return ChildEntity != null && (ChildEntity.Id == (int)Entities.Tariff || ChildEntity.Id == (int)Entities.SponsorTariff);
			else if(actionName == Actions.GenerateWindows 
				|| actionName == Actions.DeleteGeneratedWindows
				|| actionName == Actions.EnabledTariffWindows
				|| actionName == Actions.ShowDisabledWindows
				|| actionName == Actions.DisabledTariffWindows)
				return ChildEntity != null && ChildEntity.Id == (int)Entities.TariffWindow;
			else if(actionName == Constants.EntityActions.Clone)
				return ChildEntity != null && (ChildEntity.Id == (int)Entities.Tariff 
						|| (Entity.Id == (int)Entities.SponsorPricelist && ChildEntity.Id == (int)Entities.SponsorTariff));

			return base.IsActionEnabled(actionName, type);
		}

		public override DataTable GetTariffList()
		{
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(EntityManager.GetEntity((int) Entities.Tariff));
			procParameters[Pricelist.ParamNames.PricelistId] = PricelistId;
			procParameters[ParamNames.ExcludeModuleTariffs] = excludeModuleTariffs;
			
			return ((DataSet) DataAccessor.DoAction(procParameters)).Tables[Constants.TableNames.Data];
		}

        public DataSet GetTariffWindows(DateTime startDate, DateTime finishDate, Module module, bool showTrafficWindows, bool showDisabledWindows = true, bool useActualTime = false)
		{
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(EntityManager.GetEntity((int) Entities.TariffWindow));
			procParameters[Pricelist.ParamNames.StartDate] = startDate;
			procParameters[Pricelist.ParamNames.FinishDate] = finishDate;
			procParameters[Pricelist.ParamNames.PricelistId] = PricelistId;
			procParameters[ParamNames.BroadcastStart] = BroadcastStart;
			procParameters[ParamNames.ExcludeSpecialWindows] = excludeSpecialWindows;
			procParameters[ParamNames.ExcludeModuleTariffs] = excludeModuleTariffs;
            procParameters[ParamNames.ShowTrafficWindows] = showTrafficWindows;
            procParameters[ParamNames.ShowDisabledWindows] = showDisabledWindows;
            procParameters[ParamNames.UseActualTime] = useActualTime;
			if (module != null)
				procParameters[Module.ParamNames.ModuleId] = module.ModuleId;

			return (DataSet) DataAccessor.DoAction(procParameters);
		}

        public DataSet GetTariffWindowsWithAdvertType(DateTime startDate, DateTime finishDate, PresentationObject advertType, bool showUnconfirmed, TariffWindow window = null)
		{
            Dictionary<string, object> procParameters =
                new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase)
                {
                    [AdvertType.ParamNames.AdvertTypeId] = advertType.IDs[0],
                    ["showUnconfirmed"] = showUnconfirmed
                };
			if (window != null)
				procParameters.Add(TariffWindow.ParamNames.WindowId, window.WindowId);
			else
			{
				procParameters.Add(Pricelist.ParamNames.StartDate, startDate);
				procParameters.Add(Pricelist.ParamNames.FinishDate, finishDate);
				procParameters.Add(Pricelist.ParamNames.PricelistId, PricelistId);
			}

            return DataAccessor.LoadDataSet("TariffWindowWithAdvertTypeRetrieve", procParameters);
        }

        internal SpecialTariffWindow CreateSpecialTariffWindow(DateTime date, Form parentForm)
		{
			SpecialTariffWindow tariffwindow = new SpecialTariffWindow(BroadcastStart)
			{
				MassmediaID = MassmediaId,
				WindowDate = date.Date,
				WindowDateOriginal = date.Date
			};
			if (tariffwindow.ShowPassport(parentForm))
				return tariffwindow;
			return null;
		}


		// ChangeTariffWindowsMarkedStatus, ChangeTariffWindowsDisabedStatus,
		// GenerateTariffWindows(IWin32Window) переехали в MassmediaPricelist.WinForms.cs.

        private void CheckLinkedWindows(DateTime startDate, DateTime finishDate)
        {
			Dictionary<string, object> procParameters = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase)
			{
				["startDate"] = startDate.AddHours(BroadcastStart.Hour).AddMinutes(BroadcastStart.Minute),
				["finishDate"] = finishDate.AddDays(1).AddHours(BroadcastStart.Hour).AddMinutes(BroadcastStart.Minute),
				["massmediaId"] = MassmediaId,

			};
            DataAccessor.ExecuteNonQuery("CheckLinkedWindows", procParameters);
        }

        public void GenerateTariffWindows(object sender, DoWorkEventArgs e)
		{
			List<object> list = e.Argument as List<object>;
			DateTime startDate = (DateTime)list[0];
			DateTime finishDate = (DateTime)list[1];

			BackgroundWorker worker = sender as BackgroundWorker;

			int i = 0;

			while (startDate < finishDate)
			{
				if (worker.CancellationPending)
				{
					e.Cancel = true;
					return;
				}

				DateTime fDate =finishDate > startDate.AddDays(7) ? startDate.AddDays(7) : finishDate;

				Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
						EntityManager.GetEntity((int)Entities.TariffWindow),
						InterfaceObjects.FakeModule, Constants.Actions.Generate);
				procParameters.Add(Pricelist.ParamNames.PricelistId, PricelistId);
				procParameters.Add(Pricelist.ParamNames.StartDate, startDate);
				procParameters.Add(Pricelist.ParamNames.FinishDate, fDate);
				DataAccessor.DoAction(procParameters);

				startDate = fDate; // One week

				worker.ReportProgress(0, i++);

				Application.DoEvents();
			}
			
		}

		// DeleteGeneratedWindows(IWin32Window) переехал в MassmediaPricelist.WinForms.cs.

		public void DeleteGeneratedTariffWindows(object sender, DoWorkEventArgs e)
		{
			List<object> list = e.Argument as List<object>;
			DateTime startDate = (DateTime)list[0];
			DateTime finishDate = (DateTime)list[1];

			BackgroundWorker worker = sender as BackgroundWorker;

			int i = 0;

			while (startDate < finishDate)
			{
				if (worker.CancellationPending)
				{
					e.Cancel = true;
					return;
				}

				DateTime fDate = finishDate > startDate.AddDays(1) ? startDate.AddDays(1) : finishDate;

				Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
				procParameters.Add(Pricelist.ParamNames.PricelistId, PricelistId);
				procParameters.Add(Pricelist.ParamNames.StartDate, startDate);
				procParameters.Add(Pricelist.ParamNames.FinishDate, fDate);
				procParameters.Add(Massmedia.ParamNames.MassmediaId, MassmediaId);
				DataAccessor.ExecuteNonQuery("TariffWindowMassDelete", procParameters);
				
				startDate = fDate; // One day

				worker.ReportProgress(0, i++);
				Application.DoEvents();
			}
		}

		private static Entity GetPriceListEntity()
		{
			return EntityManager.GetEntity((int) Entities.Pricelist);
		}
	}
}