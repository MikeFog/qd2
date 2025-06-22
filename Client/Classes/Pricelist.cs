using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Forms;
using FogSoft.WinForm.Classes;
using CrystalDecisions.CrystalReports.Engine;
using FogSoft.WinForm.DataAccess;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace Merlin.Classes
{
	public abstract class Pricelist : ObjectContainer
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
                    Dictionary<string, object> newParameters = Parameters;
                    newParameters[ParamNames.FinishDate] = fSelector.FinishDate.Date;
                    newParameters[ParamNames.StartDate] = fSelector.StartDate.Date;
					if (!massFlag)
						Clone(newParameters);
					else
					{
						SelectionForm selector = new SelectionForm(EntityManager.GetEntity((int)Entities.MassMedia), "Радиостанции", true, CheckSelectionResult);

						if (selector.ShowDialog(owner) == DialogResult.OK)
						{
							Application.DoEvents();
							Cursor.Current = Cursors.WaitCursor;

							DataTable tableErrors = CreateErrorTable();

							foreach (var radioStation in selector.AddedItems)
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

		private DataTable CreateErrorTable()
		{
            DataTable tableErrors = new DataTable();

            DataColumn column = new DataColumn("description", System.Type.GetType("System.String"));
            tableErrors.Columns.Add(column);
			return tableErrors;
        }

        private bool CheckSelectionResult(SelectionForm selectionForm)
        {
            if (selectionForm.AddedItems.Count == 0)
            {
                MessageBox.ShowExclamation(Properties.Resources.NoRadiostationSelected);
                return false;
            }
            return true;
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