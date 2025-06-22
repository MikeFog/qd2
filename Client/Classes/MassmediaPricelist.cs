using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Forms;
using unoidl.com.sun.star.ucb;

namespace Merlin.Classes
{
	internal class MassmediaPricelist : Pricelist
	{
		public new struct ParamNames
		{
			public const string ExcludeModuleTariffs = "excludeModuleTariffs";
			public const string ExcludeSpecialWindows = "excludeSpecialWindows";
			public const string BroadcastStart = "broadcastStart";
            public const string ShowTrafficWindows = "showTrafficWindows";
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

		private int MassmediaId
		{
			get { return int.Parse(this[Massmedia.ParamNames.MassmediaId].ToString()); }
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == Actions.GenerateWindows)
				GenerateTariffWindows(owner);
			else if(actionName == Actions.DeleteGeneratedWindows)
				DeleteGeneratedWindows(owner);
			else if (actionName == Actions.EnabledTariffWindows)
				ChangeTariffWindowsDisabedStatus(owner, false);
			else if (actionName == Actions.DisabledTariffWindows)
				ChangeTariffWindowsDisabedStatus(owner, true);
			else if (actionName == Actions.ShowDisabledWindows)
				ShowDisabledWindows();
            else if (actionName == Actions.MarkWindows)
                ChangeTariffWindowsMarkedStatus(owner, true);
            else if (actionName == Actions.UnmarkWindows)
                ChangeTariffWindowsMarkedStatus(owner, false);
            else
				base.DoAction(actionName, owner, interfaceObject);
		}

		private void ShowDisabledWindows()
		{
			FrmDateSelector selector = new FrmDateSelector(StartDate, FinishDate, "Выбрать период отчета");
			if (selector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
			{
				DataSet ds = DataAccessor.LoadDataSet("ShowDisabledWindows",
				                         new Dictionary<string, object>
				                         	{
				                         		{"priceListID", PricelistId},
				                         		{"startDate", selector.StartDate},
				                         		{"finishDate", selector.FinishDate}
				                         	});
				Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.TariffWindow), "Заблокированные выпуски", ds.Tables[0]);
			}
		}

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

        public DataSet GetTariffWindows(DateTime startDate, DateTime finishDate, Module module, bool showTrafficWindows)
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


        private void ChangeTariffWindowsMarkedStatus(IWin32Window owner, bool isSpecial)
		{
            TariffWindowsDisabledStatusForm frm = 
				new TariffWindowsDisabledStatusForm(this, isSpecial, TariffWindowsDisabledStatusForm.Procedures.MarkWindows)
            {
                Text = isSpecial ? "Пометить окна цветом" : "Снять пометку окон цветом"
            };
            if (frm.ShowDialog(owner) == DialogResult.OK)
            {
                try
                {
                    Cursor.Current = Cursors.WaitCursor;
                    FireContainerRefreshed();
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }
            }
        }

        private void ChangeTariffWindowsDisabedStatus(IWin32Window owner, bool isDisabled)
		{
            TariffWindowsDisabledStatusForm frm = 
				new TariffWindowsDisabledStatusForm(this, isDisabled, TariffWindowsDisabledStatusForm.Procedures.DisableWindows)
            {
                Text = isDisabled ? "Запретить вносить выпуски в окна" : "Разрешить вносить выпуски в окна"
            };
            if (frm.ShowDialog(owner) == DialogResult.OK)
			{
				try
				{
					Cursor.Current = Cursors.WaitCursor;
					FireContainerRefreshed();
				}
				finally
				{
					Cursor.Current = Cursors.Default;
				}
			}
		}

		private void GenerateTariffWindows(IWin32Window owner)
		{
			try
			{
				FrmDateSelector selector = new FrmDateSelector(StartDate, FinishDate, "Интервал генерации окон");
				if (selector.ShowDialog(owner) == DialogResult.OK)
				{
					Application.DoEvents();
					Cursor.Current = Cursors.WaitCursor;

					List<object> list = new List<object> {selector.StartDate, selector.FinishDate};

					int count = (selector.FinishDate - selector.StartDate).Days/7 + 1; // count in weeks

					ProgressForm.Show(owner, GenerateTariffWindows, 0, count, 1, "Генерирование рекламных окон...", list);
					CheckLinkedWindows(selector.StartDate, selector.FinishDate);
					Refresh();
					FireContainerRefreshed();
				}
			}
			catch(Exception e)
			{
				ErrorManager.PublishError(e);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

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

		private void DeleteGeneratedWindows(IWin32Window owner)
		{
			try
			{
				FrmDateSelector selector =
					new FrmDateSelector(StartDate, FinishDate, "Интервал удаления сгенерированных окон");
				if (selector.ShowDialog(owner) == DialogResult.OK)
				{
					Application.DoEvents();
					Cursor.Current = Cursors.WaitCursor;

					List<object> list = new List<object> { selector.StartDate, selector.FinishDate };

					int count = (selector.FinishDate - selector.StartDate).Days + 1; // count in days

					ProgressForm.Show(owner, DeleteGeneratedTariffWindows, 0, count, 1, "Удаление сгенерированных рекламных окон...", list);

					Refresh();
					FireContainerRefreshed();
				}
			}
			catch (Exception e)
			{
				ErrorManager.PublishError(e);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

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