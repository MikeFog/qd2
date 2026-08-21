using System;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;

namespace Merlin.Classes
{
	// UI-часть Pricelist: диспетчеризация и диалог клонирования (одиночного и
	// массового). Бизнес-часть (ApplyClone, ApplyMassClone,
	// IsMassCloneSelectionValid) — в Pricelist.cs.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public abstract partial class Pricelist
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName.Equals(Constants.EntityActions.Clone, StringComparison.InvariantCultureIgnoreCase))
					ClonePriceList(owner, false);
			else if (actionName.Equals(ActionNames.MassClone, StringComparison.InvariantCultureIgnoreCase))
				ClonePriceList(owner, true);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		private void ClonePriceList(IWin32Window owner, bool massFlag)
		{
			try
			{
				FrmDateSelector fSelector = new FrmDateSelector("Даты начала и окончания");
				if (fSelector.ShowDialog(owner) == DialogResult.OK)
				{
					if (!massFlag)
						ApplyClone(fSelector.StartDate.Date, fSelector.FinishDate.Date);
					else
					{
						SelectionForm selector = new SelectionForm(EntityManager.GetEntity((int)Entities.MassMedia), "Радиостанции", true, CheckSelectionResult);

						if (selector.ShowDialog(owner) == DialogResult.OK)
						{
							Application.DoEvents();
							Cursor.Current = Cursors.WaitCursor;

							DataTable tableErrors = ApplyMassClone(fSelector.StartDate.Date, fSelector.FinishDate.Date, selector.AddedItems);
							if (tableErrors.Rows.Count > 0)
								Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ErrTmplGen), "Ошибки клонирования", tableErrors);
						}
					}
				}
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private bool CheckSelectionResult(SelectionForm selectionForm)
		{
			if (!IsMassCloneSelectionValid(selectionForm.AddedItems.Count))
			{
				UserMessage.ShowExclamation(Properties.Resources.NoRadiostationSelected);
				return false;
			}
			return true;
		}
	}
}
