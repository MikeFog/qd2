using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	// UI-часть (DoAction, ClonePriceList, CheckSelectionResult) — в
	// Pricelist.WinForms.cs. Конвенция — docs/tasks/web-migration-dialogs.md.
	public abstract partial class Pricelist : ObjectContainer
	{
		public struct ParamNames
		{
			public const string StartDate = "startDate";
			public const string FinishDate = "finishDate";
			public const string PricelistId = "pricelistID";
		}

		private struct ActionNames
		{
            public const string MassClone = "MassClone";
        }

		protected Pricelist(Entity entity) : base(entity)
		{
		}

		protected Pricelist(Entity entity, DataRow row) : base(entity, row)
		{
		}

		public abstract DataTable GetTariffList();

		public DateTime StartDate
		{
			get { return DateTime.Parse(this[ParamNames.StartDate].ToString()); }
		}

		public DateTime FinishDate
		{
			get { return DateTime.Parse(this[ParamNames.FinishDate].ToString()); }
		}

		public int PricelistId
		{
			get { return int.Parse(this[ParamNames.PricelistId].ToString()); }
		}

		// DoAction, ClonePriceList и CheckSelectionResult переехали в Pricelist.WinForms.cs.

		/// <summary>Клонирует прайс-лист на новый период.</summary>
		internal void ApplyClone(DateTime startDate, DateTime finishDate)
		{
			Dictionary<string, object> newParameters = Parameters;
			newParameters[ParamNames.FinishDate] = finishDate;
			newParameters[ParamNames.StartDate] = startDate;
			Clone(newParameters);
		}

		/// <summary>
		/// Клонирует прайс-лист на новый период для каждой из выбранных
		/// радиостанций. Возвращает таблицу ошибок (пустую, если ошибок не было).
		/// </summary>
		internal DataTable ApplyMassClone(DateTime startDate, DateTime finishDate, IEnumerable<PresentationObject> radioStations)
		{
			Dictionary<string, object> newParameters = Parameters;
			newParameters[ParamNames.FinishDate] = finishDate;
			newParameters[ParamNames.StartDate] = startDate;

			DataTable tableErrors = CreateErrorTable();
			foreach (var radioStation in radioStations)
			{
				newParameters[Massmedia.ParamNames.MassmediaId] = radioStation[Massmedia.ParamNames.MassmediaId];
				try
				{
					Clone(newParameters);
				}
				catch (Exception ex)
				{
					DataRow row = tableErrors.NewRow();
					row["description"] = string.Format("{0}: {1} ", radioStation.Name, MessageAccessor.GetMessage(ex.Message));
					tableErrors.Rows.Add(row);
				}
			}
			return tableErrors;
		}

		/// <summary>Проверяет выбор радиостанций для массового клонирования.</summary>
		internal bool IsMassCloneSelectionValid(int selectedCount)
		{
			return selectedCount != 0;
		}

		private DataTable CreateErrorTable()
		{
            DataTable tableErrors = new DataTable();

            DataColumn column = new DataColumn("description", System.Type.GetType("System.String"));
            tableErrors.Columns.Add(column);
			return tableErrors;
        }

        internal static Pricelist GetPricelistById(int pricelistId, Entity entity)
		{
			Pricelist pricelist = (Pricelist)entity.NewObject;
			pricelist[ParamNames.PricelistId] = pricelistId;
			pricelist.isNew = false;

			pricelist.Refresh();
			return pricelist;
		}

        internal virtual bool HasRollerAssigned
        {
            get { return false; }
        }

        internal virtual bool CheckTariffWithMaxCapacity(int level = 3)
        {
			return false;
        }
    }
}