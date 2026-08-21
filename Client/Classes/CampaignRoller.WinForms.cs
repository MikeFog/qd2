using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Controls;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть CampaignRoller: диспетчеризация, диалог замены ролика и удаление
	// выпусков. Запись в БД (ApplyRollerSubstitutionForDays) — в CampaignRoller.cs,
	// она возвращает таблицу незаменённых роликов, показывает её здесь.
	// Конвенция — docs/tasks/web-migration-dialogs.md, §8 п.4.
	internal partial class CampaignRoller
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == Constants.Actions.Substitute)
				SubstituteRoller(owner);
			else if (actionName == Constants.Actions.PlayRoller)
				MediaControl.Current.Play(this);
			else if (actionName == Constants.Actions.ChangePositions)
				ChangePositions((Form)owner);
			else if (actionName == Campaign.ActionNames.DeleteIssues)
				DeleteIssues((Form)owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		protected void SubstituteRoller(IWin32Window owner, int refreshLevel = 1)
		{
			Campaign.Action.Refresh();
			Substitute((Form)owner, Campaign, null, ModuleID, Roller,
					   delegate
					   {
						   RecalculateAndShowPriceChange(Campaign.Action.TotalPrice);
						   //OnParentChanged(this, refreshLevel);
						   OnParentChanged(this, EntityManager.GetEntity((int)Entities.GeneralCampaign));
					   });
		}

		private void DeleteIssues(Form owner)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[CampaignPart.OBJECT_ID] = Roller.RollerId;
			if (Campaign.DeleteIssues(owner, false, parameters, isFireEvent: false))
				FireContainerRefreshed();
		}

		/// <summary>
		/// Показывает диалог выбора нового ролика и дней, применяет замену и
		/// показывает журнал незаменённых роликов, если они есть.
		/// </summary>
		public static void Substitute(Form parentForm, Campaign campaign, int? packModuleId, int? moduleID, Roller roller, Globals.VoidCallback onEnd)
		{
			try
			{
				RollerSubstitutionForm fSubstitute = new RollerSubstitutionForm(roller, campaign, moduleID, packModuleId);
				if (fSubstitute.ShowDialog(parentForm) == DialogResult.OK)
				{
					Cursor.Current = Cursors.WaitCursor;
					Application.DoEvents();

					DataTable unsubstituted = ApplyRollerSubstitutionForDays(
						campaign, roller, fSubstitute.NewRoller, fSubstitute.SelectedDays, moduleID, packModuleId);
					ShowUnsubstitutedRollers(unsubstituted);

					onEnd?.Invoke();
				}
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				parentForm.Cursor = Cursors.Default;
			}
		}

		/// <summary>
		/// Показывает журнал незаменённых роликов, если процедура их вернула.
		/// Раньше этот показ был внутри самой записи в БД; вынесен сюда, чтобы
		/// запись не зависела от UI. Условие показа сохранено дословно.
		/// </summary>
		internal static void ShowUnsubstitutedRollers(DataTable unsubstituted)
		{
			if (unsubstituted != null && unsubstituted.Rows.Count > 0)
				Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.RollerUnSubtitude), "Незамененные ролики",
										  unsubstituted);
		}
	}
}
