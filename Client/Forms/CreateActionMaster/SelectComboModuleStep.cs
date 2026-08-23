using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Classes;

namespace Merlin.Forms.CreateActionMaster
{
	/// <summary>
	/// Второй шаг мастера размещения комбо-модулями: комбо-модуль, тип оплаты и агентства.
	///
	/// Кампании здесь не создаются - они появятся лениво, по первому клику в форме
	/// размещения. Но агентства разрешаются уже сейчас: у станции их может быть несколько,
	/// и спрашивать об этом посреди расстановки роликов неудобно.
	/// </summary>
	public partial class SelectComboModuleStep : Form
	{
		private readonly Dictionary<int, int> _agencyByMassmedia = new Dictionary<int, int>();

		public SelectComboModuleStep()
		{
			InitializeComponent();
		}

		#region Результаты шага -------------------------------

		public int ComboModuleID { get; private set; }

		public string ComboModuleName { get; private set; }

		public int PaymentTypeID { get; private set; }

		/// <summary>Агентство для каждой радиостанции комбо-модуля.</summary>
		public Dictionary<int, int> AgencyByMassmedia
		{
			get { return _agencyByMassmedia; }
		}

		#endregion

		protected override void OnLoad(EventArgs e)
		{
			try
			{
				base.OnLoad(e);
				DisplayPaymentTypes();
				DisplayComboModules();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void DisplayPaymentTypes()
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters["ShowActive"] = true;
			DataTable paymentTypes = DataAccessor.LoadDataSet("PaymentTypesLoad", procParameters).Tables[0];

			lookUpPaymentType.ColumnWithID = Campaign.ParamNames.PaymentTypeID;
			lookUpPaymentType.DataSource = paymentTypes.DefaultView;
		}

		private void DisplayComboModules()
		{
			Entity entity = EntityManager.GetEntity((int) Entities.ComboModule);
			grdComboModules.Entity = entity;
			grdComboModules.DataSource = entity.GetContent().DefaultView;
		}

		private void grdComboModules_ObjectSelected(PresentationObject presentationObject)
		{
			UpdateBtnOkEnabled();
		}

		private void SelectedItemChanged(object sender, EventArgs e)
		{
			UpdateBtnOkEnabled();
		}

		private void UpdateBtnOkEnabled()
		{
			btnOk.Enabled = grdComboModules.SelectedObject != null && lookUpPaymentType.SelectedValue != null;
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				Globals.SetWaitCursor(this);

				PresentationObject comboModule = grdComboModules.SelectedObject;
				if (!ResolveAgencies(int.Parse(comboModule.IDs[0].ToString())))
				{
					DialogResult = DialogResult.None;
					return;
				}

				ComboModuleID = int.Parse(comboModule.IDs[0].ToString());
				ComboModuleName = comboModule.Name;
				PaymentTypeID = int.Parse(lookUpPaymentType.SelectedValue.ToString());
			}
			catch (Exception ex)
			{
				DialogResult = DialogResult.None;
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Globals.SetDefaultCursor(this);
			}
		}

		/// <summary>
		/// Агентство берётся у радиостанции модуля: если оно одно - молча, если несколько -
		/// спрашиваем. Так же это делает мастер веерного размещения.
		/// </summary>
		private bool ResolveAgencies(int comboModuleID)
		{
			_agencyByMassmedia.Clear();

			foreach (DataRow row in ComboModule.LoadContent(comboModuleID).Rows)
			{
				int massmediaID = Convert.ToInt32(row[Massmedia.ParamNames.MassmediaId]);
				if (_agencyByMassmedia.ContainsKey(massmediaID)) continue;

				Massmedia massmedia = new Massmedia();
				massmedia[Massmedia.ParamNames.MassmediaId] = massmediaID;
				massmedia.IsNew = false;
				massmedia.Refresh();

				DataTable agencies = massmedia.Agencies;
				if (agencies.Rows.Count == 0)
				{
					UserMessage.ShowExclamation(string.Format(
						"У радиостанции «{0}» нет ни одного агентства, разместить комбо-модуль нельзя.",
						massmedia.Name));
					return false;
				}

				if (agencies.Rows.Count == 1)
				{
					_agencyByMassmedia[massmediaID] =
						Convert.ToInt32(agencies.Rows[0][Agency.ParamNames.AgencyId]);
					continue;
				}

				SelectionForm selector = new SelectionForm(
					massmedia, "Выбор агентства для радиостанции " + massmedia.Name, false, CheckAgencySelection);
				if (selector.ShowDialog(this) != DialogResult.OK)
					return false;

				_agencyByMassmedia[massmediaID] = ((MassmediaAgency) selector.SelectedObject).AgencyId;
			}

			return true;
		}

		private bool CheckAgencySelection(SelectionForm selectionForm)
		{
			if (selectionForm.SelectedObject == null)
			{
				UserMessage.ShowExclamation(Properties.Resources.AgencyIsRequied);
				return false;
			}

			return true;
		}
	}
}
