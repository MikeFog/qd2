using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть ActionOnMassmedia: диалоги и показ сообщений пользователю.
	// Бизнес-часть тех же операций — в ActionOnMassmedia.cs, она не знает про UI.
	// Эталон разреза, конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class ActionOnMassmedia
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			Application.DoEvents();

			if (actionName == Constants.EntityActions.Edit)
			{
				if (ShowPassport(owner))
				{
					//FireContainerRefreshed();
					OnParentChanged(this, 1);
				}
			}
			else if (actionName == ActionNames.Deactivate)
				DeactivateAction();
			else if (actionName == ActionNames.Activate|| string.Compare(actionName, ActionNames.ActivateTest) == 0)
				ActivateAction(string.Compare(actionName, ActionNames.ActivateTest) == 0);
			else if (string.Compare(actionName, ActionNames.Merge) == 0)
				Merge();
			else if (string.Compare(actionName, ActionNames.ActionRollers) == 0)
				ShowRollers();
			else if (string.Compare(actionName, ActionNames.Recalculate) == 0)
			{
				Recalculate(true);
				FireContainerRefreshed();
			}
			else if (actionName == ActionNames.Clone)
				Clone();
			else if (actionName == ActionNames.SplitCampaigns)
				SplitCampaign();
			else if (actionName == ActionNames.SplitAction)
				SplitAction();
			else if (actionName == ActionNames.ChangePaymentTypeMass)
				ChangePaymentTypeMass(owner);
			else if (actionName == ActionNames.Restore)
				Restore(owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		public override bool ShowPassport(IWin32Window owner)
		{
			ActionForm fAction = new ActionForm(this /*, false*/);
			fAction.ShowDialog(owner);
			return true;
		}

		private void ShowRollers()
		{
			Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ActionRollersStat),
				string.Format("Статистика по роликам для акции №{0}", ActionId),
				new Dictionary<string, object> { { "actionID", ActionId } });
		}

		private bool CheckActionRollersAndProgramIssues()
		{
			CheckAdvertTypes(out bool rollersWithout, out bool programIssuesWithout);
			if (rollersWithout)
			{
				// дать назначить предмет рекламы роликам без него и проверить ещё раз;
				// если такие ролики остались — не активировать
				SetAdvertTypeOrSubstituteRoller();
				CheckAdvertTypes(out rollersWithout, out _);
				if (rollersWithout)
				{
					UserMessage.ShowExclamation(MessageAccessor.GetMessage("ActivationWithRollersWithoutAdvType"));
					return false;
				}
			}

			if (programIssuesWithout)
				UserMessage.ShowExclamation(Properties.Resources.ActivationWithProgramIssuesWithoutAdvType);
			return true;
		}

		private void Restore(IWin32Window owner)
		{
			try
			{
				Globals.SetWaitCursor((Form)owner);

				ApplyRestore();
				UserMessage.ShowCompleted(MessageAccessor.GetMessage("ActionRestored"));
			}
			finally
			{
				Globals.SetDefaultCursor((Form)owner);
			}
		}

		public void Merge()
		{
			if (!IsSplitOrMergeEnabled(StartDate.Date)) return;

			DataTable table = GetActionsForMerge();
			if (table == null) return;

			Entity entityAction = EntityManager.GetEntity((int)Entities.Action);
			SelectionForm selection = new SelectionForm(entityAction, table.DefaultView, "Объдинить с ...");
			if (selection.ShowDialog() == DialogResult.OK && selection.SelectedObject != null && selection.SelectedObject is ActionOnMassmedia)
			{
				ActionOnMassmedia action2 = (ActionOnMassmedia)selection.SelectedObject;
				if (!IsSplitOrMergeEnabled(action2.StartDate.Date)) return;

				ApplyMerge(action2);
			}
		}

		private void DeactivateAction()
		{
			if (!CanDeactivate(out string errorMessage))
			{
				UserMessage.ShowExclamation(errorMessage);
				return;
			}

			MessageAccessor.Parameters = null;
			if (UserInteraction.Confirm(MessageAccessor.GetMessage("ConfirmActionDeactivate")))
			{
				try
				{
					Cursor.Current = Cursors.WaitCursor;
					ApplyDeactivate();
				}
				finally
				{
					Cursor.Current = Cursors.Default;
				}
			}
		}

		// Запись и разбор результата — ActionOnMassmedia.RunActivation (ядро); здесь окно
		// параметров и показ трёх журналов результата.
		private void ActivateAction(bool isTestActivation)
		{
			try
			{
				if (!isTestActivation && !CheckActionRollersAndProgramIssues()) return;

				ActivationSettings settings = ActivationSettings.NoTransfer;
				if (!isTestActivation)
				{
					using (ActionActivateSettingsForm form = new ActionActivateSettingsForm())
					{
						if (form.ShowDialog(Globals.MdiParent) != DialogResult.OK)
							return;

						settings = new ActivationSettings
						{
							TryTransferFailedIssues = form.TryTransferFailedIssues,
							AllowDifferentWindowPrice = form.AllowDifferentWindowPrice,
							AvoidFirmRollerWindows = form.AvoidFirmRollerWindows,
							TransferAttemptCount = form.TransferAttemptCount,
						};
					}
				}

				Cursor.Current = Cursors.WaitCursor;
				ActivationResult result = RunActivation(isTestActivation, settings);

				string caption = isTestActivation
					? "Предварительный просмотр результатов активации"
					: "Результаты активации";
				ShowActivationJournal(result.Activated, -5000, "Активированные выпуски", "ActivatedIssues", "Issue.png",
					caption + ": активированное", ActivationColumns.Issues());
				ShowActivationJournal(result.Transferred, -5002, "Перенесённые выпуски", "TransferredIssues", "issue_transferred.png",
					caption + ": перенесенное", ActivationColumns.Transferred());
				ShowActivationJournal(result.NotActivated, -5001, "Неактивированные выпуски", "NotActivatedIssues", "DeletedIssues.png",
					caption + ": неактивированное", ActivationColumns.Issues());

				if (result.FatalError != null)
					UserMessage.ShowExclamation(result.FatalError);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private static void ShowActivationJournal(DataTable table, int entityId, string entityName, string codeName,
			string iconName, string caption, Entity.Attribute[] columns)
		{
			if (table == null || table.Rows.Count == 0)
				return;
			Entity entity = EntityManager.CreateVirtualEntity(entityId, entityName, codeName, "issueID", iconName, columns);
			Globals.ShowSimpleJournal(entity, caption, table);
		}

		private bool IsSplitOrMergeEnabled(DateTime startDate)
		{
			if (CanSplitOrMerge(startDate, out string messageKey)) return true;

			UserMessage.ShowExclamation(MessageAccessor.GetMessage(messageKey));
			return false;
		}

		private bool CheckCampaignsSelectionResultForActionSplit(SelectionForm selectionForm)
		{
			if (IsSplitSelectionValid(selectionForm.AddedItems.Count, out string messageKey)) return true;

			UserMessage.ShowExclamation(MessageAccessor.GetMessage(messageKey));
			return false;
		}

		private void SplitAction()
		{
			try
			{
				if (!IsSplitOrMergeEnabled(StartDate.Date)) return;

				DataTable dt = GetCampaignsForSplit(out string messageKey);
				if (dt == null)
				{
					UserMessage.ShowInformation(MessageAccessor.GetMessage(messageKey));
					return;
				}

				SelectionForm fSelector = new SelectionForm(EntityManager.GetEntity((int)Entities.CampaignOnMassmedia),
						dt.DefaultView, "Выберите рекламные компании которые хотите перенести в новую акцию", true,
						CheckCampaignsSelectionResultForActionSplit);

				if (fSelector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
				{
					Cursor.Current = Cursors.WaitCursor;
					ApplySplitAction(fSelector.AddedItems);
					FireContainerRefreshed();
				}
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void SplitCampaign()
		{
			if (!IsSplitOrMergeEnabled(StartDate.Date)) return;

			if (!CanSplitCampaign(out string messageKey))
			{
				UserMessage.ShowInformation(MessageAccessor.GetMessage(messageKey));
				return;
			}

			SelectCampaignsForm fSelector = new SelectCampaignsForm(this, SelectionMode.Split);

			if (fSelector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
			{
				try
				{
					Cursor.Current = Cursors.WaitCursor;
					ApplySplitCampaign(fSelector.SplitRules);
				}
				finally
				{
					Cursor.Current = Cursors.Default;
				}
			}
		}

		private void ChangePaymentTypeMass(IWin32Window owner)
		{
			try
			{
				ChangePaymentTypeMassForm form = new ChangePaymentTypeMassForm(this);
				if (form.ShowDialog(owner) != DialogResult.OK) return;

				Cursor.Current = Cursors.WaitCursor;
				ApplyPaymentTypeChangeMass(form.SelectedPaymentTypeId, form.SelectedCampaigns, out DataTable tableErrors);

				if (tableErrors.Rows.Count > 0)
					Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ErrTmplGen), "Ошибки смены типа оплаты", tableErrors);
				else
					UserMessage.ShowInformation(Properties.Resources.PaymentTypeChangeSuccess);

				FireContainerRefreshed();
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void Clone()
		{
			try
			{
				SelectCampaignsForm form = new SelectCampaignsForm(this, SelectionMode.Clone);
				if (form.ShowDialog(Globals.MdiParent) == DialogResult.OK)
				{
					Cursor.Current = Cursors.WaitCursor;

					var items = new List<(DateTime, PresentationObject)>();
					foreach (var item in form.SelectedItems)
						items.Add((item.date, item.presentationObject));

					ActionOnMassmedia newAction = ApplyClone(items, out DataTable tableErrors);

					if (tableErrors.Rows.Count > 0)
						Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ErrTmplGen), "Ошибки клонирования", tableErrors);

					Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.Issue), string.Format("Клонированные выходы в эфир новой акции № {0}", newAction.ActionId), newAction.Issues);
				}
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		// Не диалог, но принимает UI-тип (ListBox) — поэтому здесь, иначе ядро
		// не собирается вне проекта Client (мост, §10 конвенции).
        internal void DisplayData(ListBox lstStat)
        {
            lstStat.Items.Clear();
            lstStat.Items.Add($"Начало: {(StartDate == DateTime.MinValue ? "" : StartDate.ToShortDateString())}");
            lstStat.Items.Add($"Окончание: {(FinishDate == DateTime.MinValue ? "" : FinishDate.ToShortDateString())}");
            lstStat.Items.Add($"Выпусков: {this["iCount"]}");
            lstStat.Items.Add($"Общее время: {this["duration"]}");
            lstStat.Items.Add($"Стоимость акции без скидок: {TariffPrice:c}");
            lstStat.Items.Add($"Стоимость акции со всеми скидками: {TotalPrice:c}");
            lstStat.Items.Add($"Пакетная скидка: {this[ParamNames.Discount]:F2}");
        }
	}
}
