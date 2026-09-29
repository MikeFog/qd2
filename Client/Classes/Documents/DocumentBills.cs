using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes.Documents
{
	/// <summary>Счёт акции у агентства — номер и дата, под которыми печатаются счёт и договор.</summary>
	public sealed class DocumentBill
	{
		public DocumentBill(int number, DateTime date)
		{
			Number = number;
			Date = date;
		}

		public int Number { get; private set; }
		public DateTime Date { get; private set; }
	}

	/// <summary>
	/// Номер и дата счёта перед печатью документов — то, что в десктопе делает окно «Дата и номер
	/// счёта» (<c>FrmBill</c>, <c>Action.CreateBill</c>): счёт уже есть — его номер; нет — следующий
	/// номер агентства на год даты. Счёт записывается в базу до печати (docs/tasks/web-reports.md §2).
	/// </summary>
	public static class DocumentBills
	{
		private static Entity BillEntity
		{
			get { return EntityManager.GetEntity((int)Entities.GeneralBill); }
		}

		/// <summary>Счёт акции у агентства; null — ещё не выставлялся.</summary>
		public static DocumentBill Find(Action action, int agencyId)
		{
			// Как Action.GetBill(int, Entity).
			Dictionary<string, object> parameters = DataAccessor.PrepareParameters(BillEntity);
			parameters[Action.ParamNames.ActionId] = action.ActionId;
			parameters[Agency.ParamNames.AgencyId] = agencyId;
			DataTable table = ((DataSet)DataAccessor.DoAction(parameters)).Tables[Constants.TableNames.Data];
			if (table.Rows.Count == 0)
				return null;
			DataRow row = table.Rows[0];
			return new DocumentBill(Convert.ToInt32(row[TableColumns.Bill.BillNo]),
				Convert.ToDateTime(row[TableColumns.Bill.BillDate]));
		}

		/// <summary>
		/// Следующий номер счёта агентства на год. Как и в десктопе, номер расходуется сразу —
		/// отказ от печати оставляет пропуск в нумерации (<c>FrmBill.GetBillNo</c>).
		/// </summary>
		public static int NextNumber(int agencyId, int year)
		{
			Dictionary<string, object> parameters = DataAccessor.PrepareParameters(
				BillEntity, InterfaceObjects.FakeModule, Constants.Actions.LoadNo);
			parameters[Agency.ParamNames.AgencyId] = agencyId;
			parameters["year"] = year;
			DataAccessor.DoAction(parameters);
			return Convert.ToInt32(parameters["nextValue"]);
		}

		/// <summary>Записывает номер и дату счёта (как «Ок» в <c>FrmBill</c>).</summary>
		public static void Save(Action action, int agencyId, int number, DateTime date)
		{
			var parameters = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase)
			{
				{ Action.ParamNames.ActionId, action.ActionId },
				{ Agency.ParamNames.AgencyId, agencyId },
				{ TableColumns.Bill.BillNo, number },
				{ TableColumns.Bill.BillDate, date.Date }
			};
			BillEntity.CreateObject(parameters).Update();
		}
	}
}
