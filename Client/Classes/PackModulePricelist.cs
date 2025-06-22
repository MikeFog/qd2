using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Forms;

namespace Merlin.Classes
{
	public class PackModulePricelist : Pricelist
	{
		private bool _isMaxCapacityChecked = false;
		private bool _maxCapacityCheckResult;

		public PackModulePricelist() : base(EntityManager.GetEntity((int) Entities.PackModulePricelist))
		{
		}

		public PackModulePricelist(Entity entity) : base(entity)
		{
		}

		public PackModulePricelist(Entity entity, DataRow row) : base(entity, row)
		{
		}

		public PackModulePricelist(int packModuleIssueID) : this()
		{
			this[ParamNames.PricelistId] = packModuleIssueID;
			isNew = false;
			Refresh();
		}

        public decimal Price
		{
			get { return (decimal)parameters["price"]; }
		}

		public int PackModuleId
		{
            get { return int.Parse(parameters[PackModule.ParamNames.PackModuleId].ToString()); }
        }

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch(actionName)
			{
				case Constants.EntityActions.Clone:
					CloneContent(owner);
					break;

				case Constants.EntityActions.Edit:
					EditContent((Form) owner);
					break;

				default:
					base.DoAction(actionName, owner, interfaceObject);
					break;
			}
		}

		private void CloneContent(IWin32Window owner)
		{
			PackModulePricelist lst = new PackModulePricelist {parameters = Parameters};
			lst.parameters["sourcePricelistID"] = parameters["pricelistID"];
			lst.parameters[Constants.ParamNames.ActionName] = Constants.EntityActions.Clone;

			if (lst.ShowPassport(owner))
			{
				FireContainerRefreshed();
				OnParentChanged(this, 1);
			}
		}

		public void EditContent(Form owner)
		{
			FrmPackModuleContent fContent = new FrmPackModuleContent(this);
			fContent.ShowDialog(owner);
		}

		protected override void AssignExisting(IWin32Window owner)
		{
			PresentationObject newObj =
				EntityManager.GetEntity((int) Entities.PackModuleContent).NewObject;
			newObj[ParamNames.PricelistId] = PricelistId;
			if (newObj.ShowPassport(owner))
			{
				OnObjectCreated(newObj);
			}
		}

		public override DataTable GetTariffList()
		{
			throw new NotImplementedException();
		}

		internal DataSet GetTariffWindows(DateTime startDate, DateTime finishDate, bool showUnconfirmed)
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters.Add("startDate", startDate);
			procParameters.Add("finishDate", finishDate);
			procParameters.Add("showUnconfirmed", showUnconfirmed);
			procParameters.Add(ParamNames.PricelistId, PricelistId);
			return DataAccessor.LoadDataSet("PackModuleTariffWindowsRetrieve", procParameters);
		}

        internal override bool HasRollerAssigned 
		{
			get { return this[Roller.ParamNames.RollerId] != DBNull.Value; }
		}

        internal override bool CheckTariffWithMaxCapacity(int level = 4)
        {
			if (_isMaxCapacityChecked) return _maxCapacityCheckResult;

            Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
            parameters[Pricelist.ParamNames.PricelistId] = PricelistId;
            parameters["level"] = level;

            object rc = DataAccessor.ExecuteScalar("CheckPackModuleTariffWithMaxCapacity", parameters, false);
            _maxCapacityCheckResult =  int.Parse(rc.ToString()) == 1;
			_isMaxCapacityChecked = true;
			return _maxCapacityCheckResult;
        }

        public override bool Refresh()
        {
			_isMaxCapacityChecked = false;
            return base.Refresh();
        }
    }
}