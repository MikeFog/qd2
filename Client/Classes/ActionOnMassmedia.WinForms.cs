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
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[ParamNames.ActionId] = ActionId;
			DataSet dataSet = DataAccessor.LoadDataSet("RollersWithoutAdvertype", procParameters);
			DataTable dtRollers = dataSet.Tables[0];
			DataTable dtProgramIssues = dataSet.Tables[1];

			if (dtRollers.Rows.Count > 0)
			{
				// allow rhe user assign advert type for rollers without it and then try to activate again without test flag.
				// If there are still rollers without advert type - show message and do not activate
				SetAdvertTypeOrSubstituteRoller();
				dataSet = DataAccessor.LoadDataSet("RollersWithoutAdvertype", procParameters);
				dtRollers = dataSet.Tables[0];
				if (dtRollers.Rows.Count > 0)
				{
					UserMessage.ShowExclamation(MessageAccessor.GetMessage("ActivationWithRollersWithoutAdvType"));
					return false;
				}
			}

			if (dtProgramIssues.Rows.Count > 0)
			{
				// the same for program issues without advert type
				UserMessage.ShowExclamation(Properties.Resources.ActivationWithProgramIssuesWithoutAdvType);
			}
			return true;
		}

		private void Restore(IWin32Window owner)
		{
			try
			{
				Globals.SetWaitCursor((Form)owner);

				Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
				procParameters.Add("actionID", ActionId);
				DataAccessor.ExecuteNonQuery("ActionRestore", procParameters);
				OnObjectDeleted(this);
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

		// ActivateAction перенесён целиком, без разреза — деловая логика
		// активации и подготовка трёх виртуальных сущностей для отображения
		// результата переплетены; см. комментарий у места переноса в
		// ActionOnMassmedia.cs.
		private void ActivateAction(bool isTestActivation)
		{
			bool tryTransferFailedIssues = false;
			bool allowDifferentWindowPrice = false;
			bool avoidFirmRollerWindows = true;
			int transferAttemptCount = 0;

			try
			{
				if (!isTestActivation && !CheckActionRollersAndProgramIssues()) return;
				if (!isTestActivation)
				{
					using (ActionActivateSettingsForm form = new ActionActivateSettingsForm())
					{
						if (form.ShowDialog(Globals.MdiParent) != DialogResult.OK)
							return;

						tryTransferFailedIssues = form.TryTransferFailedIssues;
						allowDifferentWindowPrice = form.AllowDifferentWindowPrice;
						avoidFirmRollerWindows = form.AvoidFirmRollerWindows;
						transferAttemptCount = form.TransferAttemptCount;
					}
				}

				Cursor.Current = Cursors.WaitCursor;
				parameters["isTestActivate"] = isTestActivation;
				parameters["tryTransferFailedIssues"] = tryTransferFailedIssues;
				parameters["allowDifferentWindowPrice"] = allowDifferentWindowPrice;
				parameters["avoidFirmRollerWindows"] = avoidFirmRollerWindows;
				parameters["transferAttemptCount"] = transferAttemptCount;

				DataAccessor.PrepareParameters(
					parameters, entity, InterfaceObjects.FakeModule, Constants.Actions.Activate);
				DataSet ds = (DataSet)DataAccessor.DoAction(parameters);

				if (ds.Tables["activated"].Rows.Count > 0)
				{
					Entity activatedEntity = EntityManager.CreateVirtualEntity(
						-5000,
						"Активированные выпуски",
						"ActivatedIssues",
						"issueID",
						"Issue.png",
						new Entity.Attribute("radiostationName", "Радиостанция", "nvarchar"),
						new Entity.Attribute("groupName", "Группа", "nvarchar"),
						new Entity.Attribute("name", "Ролик/Программа", "nvarchar"),
						new Entity.Attribute("advertTypeName", "Предмет рекламы", "nvarchar"),
						new Entity.Attribute("issueDate", "Дата", "datetime"),
						new Entity.Attribute("duration", "Пр-ть", "nvarchar"),
						new Entity.Attribute("issuePosition", "Порядок", "nvarchar"),
						new Entity.Attribute("statusDescription", "Статус", "nvarchar"));
					Globals.ShowSimpleJournal(
						activatedEntity,
						(isTestActivation
							? "Предварительный просмотр результатов активации"
							: "Результаты активации") + ": активированное"
						, ds.Tables["activated"]);
				}

				DataTable transferred = ds.Tables.Contains("transferred")
					? ds.Tables["transferred"]
					: (ds.Tables.Count > 3 ? ds.Tables[3] : null);
				if (transferred != null && transferred.Rows.Count > 0)
				{
					Entity transferredEntity = EntityManager.CreateVirtualEntity(
						-5002,
						"Перенесённые выпуски",
						"TransferredIssues",
						"issueID",
						"issue_transferred.png",
						new Entity.Attribute("radiostationName", "Радиостанция", "nvarchar"),
						new Entity.Attribute("groupName", "Группа", "nvarchar"),
						new Entity.Attribute("name", "Ролик/Программа", "nvarchar"),
						new Entity.Attribute("advertTypeName", "Предмет рекламы", "nvarchar"),
						new Entity.Attribute("oldIssueDate", "Дата (исходная)", "datetime"),
						new Entity.Attribute("issueDate", "Дата (новая)", "datetime"),
						new Entity.Attribute("duration", "Пр-ть", "nvarchar"),
						new Entity.Attribute("issuePosition", "Порядок", "nvarchar"),
						new Entity.Attribute("statusDescription", "Статус", "nvarchar"));

					Globals.ShowSimpleJournal(
						transferredEntity,
						(isTestActivation
							? "Предварительный просмотр результатов активации"
							: "Результаты активации") + ": перенесенное"
						, transferred);
				}

				if (ds.Tables["notactivated"].Rows.Count > 0)
				{
					Entity notActivatedEntity = EntityManager.CreateVirtualEntity(
						-5001,
						"Неактивированные выпуски",
						"NotActivatedIssues",
						"issueID",
						"DeletedIssues.png",
						new Entity.Attribute("radiostationName", "Радиостанция", "nvarchar"),
						new Entity.Attribute("groupName", "Группа", "nvarchar"),
						new Entity.Attribute("name", "Ролик/Программа", "nvarchar"),
						new Entity.Attribute("advertTypeName", "Предмет рекламы", "nvarchar"),
						new Entity.Attribute("issueDate", "Дата", "datetime"),
						new Entity.Attribute("duration", "Пр-ть", "nvarchar"),
						new Entity.Attribute("issuePosition", "Порядок", "nvarchar"),
						new Entity.Attribute("statusDescription", "Статус", "nvarchar"));
					Globals.ShowSimpleJournal(
						notActivatedEntity,
						(isTestActivation
							? "Предварительный просмотр результатов активации"
							: "Результаты активации") + ": неактивированное"
						, ds.Tables["notactivated"]);
				}

				bool errorFlag = false;
				if (ds.Tables["fatal_errors"].Rows.Count > 0)
				{
					UserMessage.ShowExclamation(ds.Tables["fatal_errors"].Rows[0]["errorMessage"].ToString());
					errorFlag = true;
				}

				if (!isTestActivation && !errorFlag)
				{
					Refresh();
					Recalculate();
					OnObjectDeleted(this);
				}
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
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
	}
}
