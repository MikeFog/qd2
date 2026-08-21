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
	// UI-часть MassmediaPricelist: диспетчеризация и диалоги генерации/удаления
	// окон, смены статуса, отчёт по заблокированным выпускам. Бизнес-часть
	// длительных операций (GenerateTariffWindows(object,DoWorkEventArgs),
	// DeleteGeneratedTariffWindows, CheckLinkedWindows) — в
	// MassmediaPricelist.cs, они не показывают диалог сами и не нуждаются в UI
	// для компиляции (BackgroundWorker/DoWorkEventArgs — System.ComponentModel).
	// ChangeTariffWindowsMarkedStatus/ChangeTariffWindowsDisabedStatus перенесены
	// целиком без разреза: вся бизнес-логика уже внутри
	// TariffWindowsDisabledStatusForm, на этой стороне разрезать нечего.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class MassmediaPricelist
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == Actions.GenerateWindows)
				GenerateTariffWindows(owner);
			else if(actionName == Actions.DeleteGeneratedWindows)
				DeleteGeneratedWindows(owner);
			else if (actionName == Actions.EnabledTariffWindows)
				ChangeTariffWindowsDisabedStatus(owner, false);
			else if (actionName == Actions.DisabledTariffWindows)
				ChangeTariffWindowsDisabedStatus(owner, true);
			else if (actionName == Actions.ShowDisabledWindows)
				ShowDisabledWindows();
			else if (actionName == Actions.MarkWindows)
				ChangeTariffWindowsMarkedStatus(owner, true);
			else if (actionName == Actions.UnmarkWindows)
				ChangeTariffWindowsMarkedStatus(owner, false);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		private void ShowDisabledWindows()
		{
			FrmDateSelector selector = new FrmDateSelector(StartDate, FinishDate, "Выбрать период отчета");
			if (selector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
			{
				DataSet ds = DataAccessor.LoadDataSet("ShowDisabledWindows",
										 new Dictionary<string, object>
										 	{
										 		{"priceListID", PricelistId},
										 		{"startDate", selector.StartDate},
										 		{"finishDate", selector.FinishDate}
										 	});
				Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.TariffWindow), "Заблокированные выпуски", ds.Tables[0]);
			}
		}

		private void ChangeTariffWindowsMarkedStatus(IWin32Window owner, bool isSpecial)
		{
			TariffWindowsDisabledStatusForm frm =
				new TariffWindowsDisabledStatusForm(this, isSpecial, TariffWindowsDisabledStatusForm.Procedures.MarkWindows)
			{
				Text = isSpecial ? "Пометить окна цветом" : "Снять пометку окон цветом"
			};
			if (frm.ShowDialog(owner) == DialogResult.OK)
			{
				try
				{
					Cursor.Current = Cursors.WaitCursor;
					FireContainerRefreshed();
				}
				finally
				{
					Cursor.Current = Cursors.Default;
				}
			}
		}

		private void ChangeTariffWindowsDisabedStatus(IWin32Window owner, bool isDisabled)
		{
			TariffWindowsDisabledStatusForm frm =
				new TariffWindowsDisabledStatusForm(this, isDisabled, TariffWindowsDisabledStatusForm.Procedures.DisableWindows)
			{
				Text = isDisabled ? "Запретить вносить выпуски в окна" : "Разрешить вносить выпуски в окна"
			};
			if (frm.ShowDialog(owner) == DialogResult.OK)
			{
				try
				{
					Cursor.Current = Cursors.WaitCursor;
					FireContainerRefreshed();
				}
				finally
				{
					Cursor.Current = Cursors.Default;
				}
			}
		}

		private void GenerateTariffWindows(IWin32Window owner)
		{
			try
			{
				FrmDateSelector selector = new FrmDateSelector(StartDate, FinishDate, "Интервал генерации окон");
				if (selector.ShowDialog(owner) == DialogResult.OK)
				{
					Application.DoEvents();
					Cursor.Current = Cursors.WaitCursor;

					List<object> list = new List<object> {selector.StartDate, selector.FinishDate};

					int count = (selector.FinishDate - selector.StartDate).Days/7 + 1; // count in weeks

					ProgressForm.Show(owner, GenerateTariffWindows, 0, count, 1, "Генерирование рекламных окон...", list);
					CheckLinkedWindows(selector.StartDate, selector.FinishDate);
					Refresh();
					FireContainerRefreshed();
				}
			}
			catch(Exception e)
			{
				ErrorManager.PublishError(e);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void DeleteGeneratedWindows(IWin32Window owner)
		{
			try
			{
				FrmDateSelector selector =
					new FrmDateSelector(StartDate, FinishDate, "Интервал удаления сгенерированных окон");
				if (selector.ShowDialog(owner) == DialogResult.OK)
				{
					Application.DoEvents();
					Cursor.Current = Cursors.WaitCursor;

					List<object> list = new List<object> { selector.StartDate, selector.FinishDate };

					int count = (selector.FinishDate - selector.StartDate).Days + 1; // count in days

					ProgressForm.Show(owner, DeleteGeneratedTariffWindows, 0, count, 1, "Удаление сгенерированных рекламных окон...", list);

					Refresh();
					FireContainerRefreshed();
				}
			}
			catch (Exception e)
			{
				ErrorManager.PublishError(e);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}
	}
}
