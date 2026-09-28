using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Controls;
using Merlin.Forms;
using Merlin.Properties;

namespace Merlin.Classes
{
	// UI-часть Campaign: диспетчеризация, диалог удаления выпусков, отчёты
	// (перенесены целиком — отдельная область, docs/tasks/web-migration.md,
	// этап 4), модальная сессия CampaignForm (форма 3.1, docs/tasks/
	// web-migration-dialogs.md, §8 п.3), смена агентства/типа оплаты.
	// Бизнес-часть — в Campaign.cs. Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class Campaign
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == ActionNames.ChangePaymentType)
				ChangePaymentType(owner);
			else if (actionName == ActionNames.ChangeAgency)
				ChangeAgency(owner);
			else if (actionName == ActionNames.ShowRollers)
				ShowRollers();
			else if (actionName == ActionNames.ShowDays)
			{
				ChildEntity = EntityManager.GetEntity((int)Entities.CampaignDay);
				FireContainerRefreshed();
			}
			else if (actionName == Constants.EntityActions.Edit)
				EditRollerIssues(owner, new RollerIssuesGrid3());
			// Медиаплан всегда по фактическим окнам, поэтому пары «X» / «XFact» —
			// одно и то же. В iEntityAction у кампаний (Artvis/ArtvisDev) есть
			// только Fact-имена; имена без Fact оставлены на случай других баз.
			else if (actionName == ActionNames.PrintMediaPlan || actionName == ActionNames.PrintMediaPlanFact)
				PrintMediaPlan(false, false, false);
			else if (actionName == ActionNames.PrintMediaPlanMonth || actionName == ActionNames.PrintMediaPlanFactMonth)
				PrintMediaPlan(true, false, false);
			else if (actionName == ActionNames.PrintMediaPlanByPeriod || actionName == ActionNames.PrintMediaPlanFactByPeriod)
				PrintMediaPlan(false, true, false);
			else if (actionName == ActionNames.PrintSelectivelyMediaPlan || actionName == ActionNames.PrintSelectivelyMediaPlanFact)
				PrintMediaPlan(false, false, true);
			else if (actionName == ActionNames.PrintSelectivelyMediaPlanMonth || actionName == ActionNames.PrintSelectivelyMediaPlanFactMonth)
				PrintMediaPlan(true, false, true);
			else if (actionName == ActionNames.PrintSelectivelyMediaPlanByPeriod || actionName == ActionNames.PrintSelectivelyMediaPlanFactByPeriod)
				PrintMediaPlan(false, true, true);
			else if (actionName == ActionNames.PrintTransfers)
				PrintTransfers();
			else if (actionName == ActionNames.DeleteIssues)
				DeleteIssues(owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		public bool DeleteIssues(IWin32Window owner, bool isSponsorProgram = false, Dictionary<string, object> extraParameters = null, bool isFireEvent = true)
		{
			bool resFlag = false;
			try
			{
				Entity currentChild = null;

				if (ChildEntity != null)
					currentChild = ChildEntity;

				ChildEntity = EntityManager.GetEntity((int)Entities.CampaignDay);
				CampaignDaysForm selector = new CampaignDaysForm(this, isSponsorProgram, extraParameters);

				if (selector.ShowDialog(owner) == DialogResult.OK)
				{
					Application.DoEvents();
					Cursor.Current = Cursors.WaitCursor;

					Action.Refresh();
					decimal price = Action.TotalPrice;

					DataTable tableErrors = ApplyIssuesDelete(isSponsorProgram, selector.SelectedIDs, out resFlag);

					if (tableErrors.Rows.Count > 0)
					{
						Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ErrTmplGen), "Ошибки удаления", tableErrors);
					}
					RecalculateAndShowPriceChange(price);
					if (currentChild != null)
						ChildEntity = currentChild;
					if (isFireEvent)
						FireContainerRefreshed();
				}
				return resFlag;
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
				return resFlag;
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void PrintTransfers()
		{
			DataSet ds = DataAccessor.LoadDataSet("CampaignIssuesTransfers", new Dictionary<string, object> { {ParamNames.CampaignId, CampaignId } });
			if (ds == null  || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
				Globals.ShowInfo("CampaignHaveNotTransfers");
			else
				Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.CampaignIssuesTransfers), Resources.CampaignIssuesTransfersTitle, ds.Tables[0]);
		}

		public void PrintMediaPlan(bool isByMonth, bool isByPeriod, bool selectively)
		{
			Application.DoEvents();
			Refresh();

			if (isByMonth)
			{
				Dictionary<string, object> procParameters = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
				procParameters[ParamNames.CampaignId] = CampaignId;
				procParameters["isFact"] = true;
				DataSet ds = DataAccessor.LoadDataSet("GetMonthes", procParameters);

				Dictionary<object, object> dMonthsToShow = new Dictionary<object, object>();

				foreach (DataRow row in ds.Tables[0].Rows)
				{
					int month = ParseHelper.ParseToInt32(row["MonthDate"].ToString(), -1);
					int year = ParseHelper.ParseToInt32(row["MonthYear"].ToString(), -1);
					if (month >= 0 && year >= 0)
					{
						DateTime date = new DateTime(year, month, 1);
						dMonthsToShow.Add(date, date.ToString("MMMM yyy"));
					}
				}

				FrmMonths f = new FrmMonths(dMonthsToShow, false);
				if (f.ShowDialog(Globals.MdiParent) == DialogResult.Cancel) return;
				IList<DateTime> months = new List<DateTime>();
				foreach (KeyValuePair<object, object> item in f.CheckedItems)
					months.Add((DateTime)item.Key);
				MediaPlan.CreateInstance(this, months, selectively).Show();
			}
			else if (isByPeriod)
			{
				FrmDateSelector selector = new FrmDateSelector(StartDate, FinishDate, "Выбор периода");
				if (selector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
					MediaPlan.CreateInstance(this, selector.StartDate, selector.FinishDate, selectively).Show();
			}
			else
				MediaPlan.CreateInstance(this, selectively).Show();
		}

		protected void EditRollerIssues(IWin32Window owner, TariffGrid tariffGrid)
		{
			CampaignForm campaign = new CampaignForm(this, tariffGrid);
			campaign.ShowDialog(owner);
			Application.DoEvents();
			if (campaign.ChangeFlag)
			{
				//Action.Recalculate();
				Refresh();
				FireContainerRefreshed();
			}
		}

		public void EditProgramIssues(IWin32Window owner)
		{
			CampaignForm campaign = new CampaignForm(this, new ProgramIssuesGrid2());
			campaign.ShowDialog(owner);
			Application.DoEvents();
			if (campaign.ChangeFlag)
			{
				Action.Recalculate();
				Refresh();
			}
		}

		private void ChangeAgency(IWin32Window owner)
		{
			if (IsChangePossible)
			{
				DataView candidates = AgencyCandidates;
				SelectionForm selector = candidates == null
					? new SelectionForm(EntityManager.GetEntity((int)Entities.Agency), "Рекламное агентство")
					: new SelectionForm(EntityManager.GetEntity((int)Entities.Agency), candidates, "Рекламное агентство");
				if (selector.ShowDialog(owner) == DialogResult.OK)
				{
					ApplyAgencyChange(int.Parse(selector.SelectedObject.IDs[0].ToString()));
					UserMessage.ShowInformation(Resources.AgencyChangeSuccess);
				}
			}
			else
				UserMessage.ShowExclamation(Resources.ChangeAgencyIsForbidden);
		}

		private void ChangePaymentType(IWin32Window owner)
		{
			if (IsChangePossible)
			{
				SelectionForm selector = new SelectionForm(EntityManager.GetEntity((int)Entities.PaymentType), "Типы оплаты");
				if (selector.ShowDialog(owner) == DialogResult.OK)
				{
					ApplyPaymentTypeChange(int.Parse(selector.SelectedObject.IDs[0].ToString()));
					UserMessage.ShowInformation(Resources.PaymentTypeChangeSuccess);
				}
			}
			else
				UserMessage.ShowExclamation(Resources.ChangePaymentTypeIsForbidden);
		}

		// Не диалоги, но принимают UI-типы (ListBox / Form) — поэтому здесь,
		// иначе ядро не собирается вне проекта Client (мост, §10 конвенции).
		internal void DisplayCampaignData(ListBox lstStat)
		{
			string text;
			lstStat.Items.Clear();
			if (StartDate == DateTime.MinValue)
				text = string.Empty;
			else
				text = StartDate.ToShortDateString();
			lstStat.Items.Add("Начало: " + text);

			if (FinishDate == DateTime.MinValue)
				text = string.Empty;
			else
				text = FinishDate.ToShortDateString();
			lstStat.Items.Add("Окончание: " + text);
			lstStat.Items.Add("Выпусков: " + IssuesCount);
			lstStat.Items.Add("Общее время: " + DateTimeUtils.Time2String(IssuesDuration));
			lstStat.Items.Add("Цена по тарифам: " + TariffPrice.ToString("c"));
			lstStat.Items.Add("Объёмная скидка: " + Discount.ToString("0.00"));
			lstStat.Items.Add("Цена с учётом объёмной скидки: " + Price.ToString("c"));

			if (CampaignType == CampaignTypes.Sponsor)
			{
				lstStat.Items.Add("");
				lstStat.Items.Add("Программ: " + ProgramIssuesCount);
				lstStat.Items.Add("Бонус: " + DateTimeUtils.Time2String(Bonus - IssuesDuration));
			}
		}

		public virtual void PrintOnAirInquire(Form owner) {}
	}
}
