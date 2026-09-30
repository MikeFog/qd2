using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	public partial class FirmBalanceIssues : FirmBalance
	{
		public FirmBalanceIssues() : base(EntityManager.GetEntity((int) Entities.BalanceIssues))
		{
		}

		public FirmBalanceIssues(Entity entity) : base(entity)
		{
		}

		public FirmBalanceIssues(Entity entity, DataRow row)
			: base(entity, row)
		{
		}

		// Jump2FirmBalanceJournal переехал в FirmBalanceIssues.WinForms.cs.
	}

	/// <summary>
	/// «Баланс для конкретной фирмы-заказчика»: данные отбора и расчёт. Вынесено из
	/// FrmFirmIssuesBalance — десктопная форма и веб-экран считают одинаково.
	/// На окончание интервала = на начало + платежи за период − акции за период.
	/// </summary>
	public static class FirmBalanceReport
	{
		/// <summary>Отбор: имена полей веб-экрана, они же — параметры процедур.</summary>
		public struct ParamNames
		{
			public const string StartOfInterval = "startOfInterval";
			public const string EndOfInterval = "endOfInterval";
			public const string FirmId = "firmID";
			public const string ManagerId = "managerID";
			public const string Agencies = "agenciesIDString";
			public const string ShowWhite = "ShowWhite";
			public const string ShowBlack = "ShowBlack";
		}

		/// <summary>Отбор расчёта. Agencies — «12,15,»; пусто — все агентства.</summary>
		public sealed class Filter
		{
			public DateTime Start;
			public DateTime Finish;
			public object FirmId;
			public object ManagerId;
			public string Agencies = "";
			public bool ShowWhite = true;
			public bool ShowBlack = true;
		}

		public sealed class Result
		{
			public decimal StartBalance;
			public Entity ActionEntity;
			public DataTable Actions;
			public decimal ActionsTotal;
			public Entity PaymentEntity;
			public DataTable Payments;
			public decimal PaymentsTotal;
			public decimal EndBalance => StartBalance + PaymentsTotal - ActionsTotal;
		}

		/// <summary>Фирмы («firm») и агентства («agency») для отбора — FirmBalanceIssuesOnLoad.</summary>
		public static DataSet LoadFilterData()
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				EntityManager.GetEntity((int)Entities.BalanceIssues), InterfaceObjects.BalanceJournal, Constants.Actions.Load);
			return (DataSet)DataAccessor.DoAction(procParameters);
		}

		/// <summary>Менеджеры, работавшие с фирмой, — выбор менеджера в отборе.</summary>
		public static DataTable LoadManagers(object firmId)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["firmID"] = firmId;
			return DataAccessor.LoadDataSet("FirmManagers", parameters).Tables[0];
		}

		public static Result Load(Filter filter)
		{
			var result = new Result { StartBalance = StartBalance(filter) };
			LoadPayments(filter, result);
			LoadActions(filter, result);
			return result;
		}

		/// <summary>Остаток на начало интервала — stat_Balance на день раньше начала.</summary>
		internal static decimal StartBalance(Filter filter)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(EntityManager.GetEntity((int)Entities.BalanceIssues));
			procParameters["theDate"] = filter.Start.AddDays(-1);
			procParameters["FirmID"] = filter.FirmId;
			procParameters["ShowBlack"] = filter.ShowBlack;
			procParameters["ShowWhite"] = filter.ShowWhite;
			procParameters["agenciesIDString"] = filter.Agencies;
			if (filter.ManagerId != null)
				procParameters["ManagerID"] = filter.ManagerId;

			DataTable dt = ((DataSet)DataAccessor.DoAction(procParameters)).Tables[Constants.TableNames.Data];
			if (dt.Rows.Count == 0)
				return 0;
			return decimal.Parse(dt.Rows[0]["summaPositive"].ToString()) + decimal.Parse(dt.Rows[0]["summaNegative"].ToString());
		}

		/// <summary>Акции за период (ActionsForBalance), итог — по totalPrice.</summary>
		internal static void LoadActions(Filter filter, Result result)
		{
			Entity entity = EntityManager.GetEntity((int)Entities.Action);
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(entity,
				InterfaceObjects.BalanceJournal, Constants.Actions.Load);
			procParameters["startOfInterval"] = filter.Start;
			procParameters["endOfInterval"] = filter.Finish;
			procParameters["firmID"] = filter.FirmId;
			procParameters["ShowBlack"] = filter.ShowBlack;
			procParameters["ShowWhite"] = filter.ShowWhite;
			procParameters["agenciesIDString"] = filter.Agencies;
			if (filter.ManagerId != null)
				procParameters[SecurityManager.ParamNames.UserId] = filter.ManagerId;
			procParameters["isReadyOnly"] = 1;

			DataTable data = ((DataSet)DataAccessor.DoAction(procParameters)).Tables[Constants.TableNames.Data];
			decimal total = 0;
			foreach (DataRow row in data.Rows)
				if (decimal.TryParse(row["totalPrice"].ToString(), out decimal price))
					total += price;

			result.ActionEntity = entity;
			result.Actions = data;
			result.ActionsTotal = total;
		}

		/// <summary>
		/// Платежи за период. Выбран менеджер или у пользователя только групповые права —
		/// платежи по акциям (PaymentCommonAction), иначе — общие платежи (PaymentCommon).
		/// </summary>
		internal static void LoadPayments(Filter filter, Result result)
		{
			Entity entity = filter.ManagerId != null
				|| (!SecurityManager.LoggedUser.IsRightToViewForeignActions() && SecurityManager.LoggedUser.IsRightToViewGroupActions())
				? EntityManager.GetEntity((int)Entities.PaymentCommonAction)
				: EntityManager.GetEntity((int)Entities.PaymentCommon);

			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(entity);
			procParameters["startOfInterval"] = filter.Start;
			procParameters["endOfInterval"] = filter.Finish;
			procParameters["firmID"] = filter.FirmId;
			procParameters["ShowBlack"] = filter.ShowBlack;
			procParameters["ShowWhite"] = filter.ShowWhite;
			procParameters["agenciesIDString"] = filter.Agencies;
			if (filter.ManagerId != null)
				procParameters["managerID"] = filter.ManagerId;

			DataTable data = ((DataSet)DataAccessor.DoAction(procParameters)).Tables[Constants.TableNames.Data];
			decimal total = 0;
			foreach (DataRow row in data.Rows)
				total += decimal.Parse(row["summa"].ToString());

			result.PaymentEntity = entity;
			result.Payments = data;
			result.PaymentsTotal = total;
		}
	}
}
