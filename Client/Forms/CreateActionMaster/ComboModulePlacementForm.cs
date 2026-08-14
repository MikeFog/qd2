using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using Merlin.Classes;
using Merlin.Controls;

namespace Merlin.Forms.CreateActionMaster
{
	/// <summary>
	/// Третий шаг мастера размещения комбо-модулями: слева ролики фирмы, статистика акции и
	/// добавленные выпуски, справа грид остатков по модулям комбо-модуля.
	///
	/// Форма самостоятельная, а не наследник CampaignForm: та завязана на одну кампанию с её
	/// прайс-листом и тарифной сеткой, а здесь строки - модули разных радиостанций, и кампаний
	/// столько же, сколько модулей.
	/// </summary>
	internal partial class ComboModulePlacementForm : Form
	{
		private const string SETTING_PERIOD_MODE = "ComboModulePlacementPeriodMode";

		private readonly Firm _firm;
		private readonly int _comboModuleID;
		private readonly string _comboModuleName;
		private readonly int _paymentTypeID;
		private readonly Dictionary<int, int> _agencyByMassmedia;

		private RollerPositions _position = RollerPositions.Undefined;

		private ComboModulePlacementForm()
		{
			InitializeComponent();
		}

		public ComboModulePlacementForm(Firm firm, SelectComboModuleStep step) : this()
		{
			_firm = firm;
			_comboModuleID = step.ComboModuleID;
			_comboModuleName = step.ComboModuleName;
			_paymentTypeID = step.PaymentTypeID;
			_agencyByMassmedia = step.AgencyByMassmedia;
		}

		protected override void OnLoad(EventArgs e)
		{
			try
			{
				base.OnLoad(e);

				Text = string.Format("Размещение комбо-модулями: {0} - {1}", _firm.Name, _comboModuleName);

				InitRollersList();
				InitComboModuleGrid();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void InitRollersList()
		{
			grdRollers.Entity = EntityManager.GetEntity((int) Entities.ActionRollers);
			grdRollers.DataSource = _firm.GetRollers().DefaultView;
		}

		private void InitComboModuleGrid()
		{
			comboModuleGrid.ComboModuleID = _comboModuleID;
			comboModuleGrid.PeriodMode = LoadPeriodMode();
			comboModuleGrid.ShowUnconfirmed = tbbShowUnconfirmed.Checked;
			UpdatePeriodModeCaption();
			comboModuleGrid.RefreshGrid();
		}

		#region Режим периода ---------------------------------

		private ComboModulePeriodMode LoadPeriodMode()
		{
			return UserSettings.Load(SETTING_PERIOD_MODE) == ComboModulePeriodMode.Month.ToString()
				? ComboModulePeriodMode.Month
				: ComboModulePeriodMode.Week;
		}

		private void UpdatePeriodModeCaption()
		{
			tbbPeriodMode.Text = comboModuleGrid.PeriodMode == ComboModulePeriodMode.Month ? "Месяц" : "Неделя";
		}

		private void tbbPeriodMode_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				ComboModulePeriodMode mode = (ComboModulePeriodMode)
					Enum.Parse(typeof(ComboModulePeriodMode), e.ClickedItem.Tag.ToString());
				if (mode == comboModuleGrid.PeriodMode) return;

				comboModuleGrid.PeriodMode = mode;
				UpdatePeriodModeCaption();
				UserSettings.Save(SETTING_PERIOD_MODE, mode.ToString());
				comboModuleGrid.RefreshGrid();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		#endregion

		private void tbbShowUnconfirmed_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				comboModuleGrid.ShowUnconfirmed = tbbShowUnconfirmed.Checked;
				comboModuleGrid.RefreshGrid();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void tbbPosition_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			try
			{
				tbbPosition.Text = e.ClickedItem.Text;
				_position = (RollerPositions) Enum.Parse(typeof(RollerPositions), e.ClickedItem.Tag.ToString());
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}
	}
}
