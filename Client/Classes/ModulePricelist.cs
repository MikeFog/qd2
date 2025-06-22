using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;

namespace Merlin.Classes
{
	internal class ModulePricelist : MassmediaPricelist
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

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch (actionName)
			{
				case Constants.EntityActions.Clone:
					CloneTariffList(owner);
					break;

				case "EditTariffList":
					EditTariffList(owner);
					break;

				default:
					base.DoAction(actionName, owner, interfaceObject);
					break;
			}
		}



		private DataTable TafiffList
		{
			get
			{
				if(dtTariffs == null)
					dtTariffs = GetTariffList();
				return dtTariffs;
			}
		}

		private void CloneTariffList(IWin32Window owner)
		{
            ModulePricelist lst = new ModulePricelist
            {
                parameters = Parameters
            };

            lst.parameters[Constants.ParamNames.ActionName] = Constants.Actions.Clone;
            lst.parameters["sourceModulePriceListID"] = this["modulePriceListID"];
			lst.parameters.Remove(ModulePricelist.ParamNames.ModulePriceListID);

            if (lst.ShowPassport(owner))
			{
				FireContainerRefreshed();
				OnParentChanged(this, 1);
			}
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (string.Compare(Constants.EntityActions.Clone, actionName) == 0)
				return base.IsActionEnabled(actionName, type) || SecurityManager.LoggedUser.IsAdmin;
			return base.IsActionEnabled(actionName, type);
		}

		private void EditTariffList(IWin32Window owner)
		{
			SelectionForm selector = new SelectionForm(
				EntityManager.GetEntity((int) Entities.Tariff), LoadTariffList().DefaultView, "Тарифы для модуля", true);
			if (selector.ShowDialog(owner) == DialogResult.OK)
			{
				Application.DoEvents();
				//Form.ActiveForm.Cursor = Cursors.WaitCursor;

				Entity moduleTariffEntity = EntityManager.GetEntity((int) Entities.ModuleTariff);
				Dictionary<string, object> procParameters;

				foreach (PresentationObject po in selector.AddedItems)
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

				foreach (PresentationObject po in selector.DeletedItems)
				{
					procParameters = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

					procParameters[ParamNames.ModulePriceListID] = ModulePriceListID;
					procParameters[ParamNames.TariffID] = ((Tariff) po).TariffId;
					PresentationObject moduleTariff = moduleTariffEntity.CreateObject(procParameters);
					moduleTariff.Delete(true);
				}
				FireContainerRefreshed();
			}
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