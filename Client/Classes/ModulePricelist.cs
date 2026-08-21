using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	// UI-часть (DoAction, CloneTariffList, EditTariffList) — в
	// ModulePricelist.WinForms.cs. Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class ModulePricelist : MassmediaPricelist
	{
		#region Constants -------------------------------------

		public new struct ParamNames
		{
			public const string TariffID = "tariffID";
			public const string ModulePriceListID = "modulePriceListID";
			public const string Price = "price";
		}

		#endregion

		private DataTable dtTariffs;

		#region Constructor -----------------------------------

		public ModulePricelist() : base(GetModulePricelistEntity())
		{
		}


		public ModulePricelist(DataRow row) : base(GetModulePricelistEntity(), row)
		{
		}

        public ModulePricelist(int modulePricelistID) : this()
        {
            this[ParamNames.ModulePriceListID] = modulePricelistID;
            isNew = false;
			Refresh();
        }

        #endregion

		internal int ModuleID
		{
            get { return int.Parse(parameters[Module.ParamNames.ModuleId].ToString()); }
        }

        internal decimal Price
		{
			get { return decimal.Parse(parameters[ParamNames.Price].ToString()); }
		}

		internal int ModulePriceListID
		{
			get { return int.Parse(parameters[ParamNames.ModulePriceListID].ToString()); }
		}

		internal override bool HasRollerAssigned
		{
			get { return this[Roller.ParamNames.RollerId] != DBNull.Value; }
		}

        internal override bool CheckTariffWithMaxCapacity(int level = 4)
        {
            TafiffList.DefaultView.RowFilter = string.Format("maxCapacity > 0 and maxCapacity < {0}", level);
            return TafiffList.DefaultView.Count > 0;
        }

        // DoAction, CloneTariffList и EditTariffList переехали в ModulePricelist.WinForms.cs.



		private DataTable TafiffList
		{
			get
			{
				if(dtTariffs == null)
					dtTariffs = GetTariffList();
				return dtTariffs;
			}
		}

		// CloneTariffList переехал в ModulePricelist.WinForms.cs.

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (string.Compare(Constants.EntityActions.Clone, actionName) == 0)
				return base.IsActionEnabled(actionName, type) || SecurityManager.LoggedUser.IsAdmin;
			return base.IsActionEnabled(actionName, type);
		}

		/// <summary>Применяет добавленные/удалённые тарифы модуля.</summary>
		internal void ApplyTariffListChanges(IEnumerable<PresentationObject> addedTariffs, IEnumerable<PresentationObject> deletedTariffs)
		{
			Entity moduleTariffEntity = EntityManager.GetEntity((int) Entities.ModuleTariff);
			Dictionary<string, object> procParameters;

			foreach (PresentationObject po in addedTariffs)
			{
				procParameters = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase)
                {
                    [ParamNames.ModulePriceListID] = ModulePriceListID,
                    [ParamNames.TariffID] = ((Tariff)po).TariffId,
                    ["isEditTarrifs"] = true
                };
				PresentationObject moduleTariff = moduleTariffEntity.CreateObject(procParameters);
				moduleTariff.Update();
			}

			foreach (PresentationObject po in deletedTariffs)
			{
				procParameters = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

				procParameters[ParamNames.ModulePriceListID] = ModulePriceListID;
				procParameters[ParamNames.TariffID] = ((Tariff) po).TariffId;
				PresentationObject moduleTariff = moduleTariffEntity.CreateObject(procParameters);
				moduleTariff.Delete(true);
			}
			FireContainerRefreshed();
		}

		/// <summary>
		/// Loads module tariffes for this price list
		/// </summary>
		/// <returns></returns>
		private DataTable LoadTariffList()
		{
			DataAccessor.PrepareParameters(parameters, EntityManager.GetEntity((int) Entities.Tariff),
			                               InterfaceObjects.Selector, Constants.Actions.LoadForSelection);

			DataSet ds = (DataSet)DataAccessor.DoAction(parameters);
			return ds.Tables[Constants.TableNames.Data];
		}

		public override DataTable GetTariffList()
		{
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(EntityManager.GetEntity((int) Entities.ModuleTariff));
			procParameters[Module.ParamNames.ModuleId] = this[Module.ParamNames.ModuleId];
			procParameters[Pricelist.ParamNames.PricelistId] = PricelistId;

			return ((DataSet) DataAccessor.DoAction(procParameters)).Tables[Constants.TableNames.Data];
		}

        public override bool Refresh()
        {
			dtTariffs = null;
            return base.Refresh();
        }

        private static Entity GetModulePricelistEntity()
		{
			return EntityManager.GetEntity((int) Entities.ModulePricelist);
		}
	}
}