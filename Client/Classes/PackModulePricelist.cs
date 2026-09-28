using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	// EditContent переехал в PackModulePricelist.WinForms.cs. Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class PackModulePricelist : Pricelist
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



		// EditContent переехал в PackModulePricelist.WinForms.cs.

		/// <summary>
		/// Черновик копии прайс-листа пакетных модулей: значения исходного, с пометкой
		/// Clone и ссылкой на источник. Записывается паспортом (Update -> Clone);
		/// содержимое пакета копирует процедура. Ключ pricelistID остаётся в параметрах —
		/// как было в CloneContent. Период по умолчанию — год по Pricelist.GetClonePeriod,
		/// суженный до общего периода модульных прайс-листов всех модулей пакета: процедура
		/// требует, чтобы каждый покрывал период пакета целиком (CannotClonePackModulePriceList).
		/// Если у какого-то модуля на 1 января модульного прайс-листа нет — остаётся целый год.
		/// </summary>
		public override PresentationObject CreateCloneDraft()
		{
			PackModulePricelist draft = new PackModulePricelist { parameters = Parameters };
			draft.parameters["sourcePricelistID"] = parameters["pricelistID"];
			draft.parameters[Constants.ParamNames.ActionName] = Constants.EntityActions.Clone;

			GetClonePeriod(out DateTime start, out DateTime finish);
			DateTime? common = CommonModulesFinish(start);
			if (common < finish)
				finish = common.Value;
			draft.parameters[ParamNames.StartDate] = start;
			draft.parameters[ParamNames.FinishDate] = finish;
			return draft;
		}

		/// <summary>
		/// Самое раннее окончание модульных прайс-листов модулей пакета, действующих на <paramref name="date"/>;
		/// null — у какого-то модуля такого нет.
		/// </summary>
		private DateTime? CommonModulesFinish(DateTime date)
		{
			DataTable content = DataAccessor.LoadDataSet("PackModuleContentRetrieve",
				new Dictionary<string, object> { { ParamNames.PricelistId, PricelistId } }).Tables[0];

			DateTime? result = null;
			foreach (DataRow row in content.Rows)
			{
				DataTable modulePricelists = DataAccessor.LoadDataSet("ModulePricelistByDate", new Dictionary<string, object>
				{
					{ Massmedia.ParamNames.MassmediaId, row[Massmedia.ParamNames.MassmediaId] },
					{ "theDate", date },
					{ Module.ParamNames.ModuleId, row[Module.ParamNames.ModuleId] }
				}).Tables[0];
				if (modulePricelists.Rows.Count == 0)
					return null;

				DateTime finish = ((DateTime)modulePricelists.Rows[0][ParamNames.FinishDate]).Date;
				if (result == null || finish < result)
					result = finish;
			}
			return result;
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