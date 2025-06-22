using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Classes;

namespace Merlin.Forms.CreateActionMaster
{
	public partial class SelectMassmediasStep : Form
	{
		private readonly DataSet _data;
	    private readonly Firm _firm;

        public SelectMassmediasStep(Firm firm)
		{
			InitializeComponent();

			Entity entity = (Entity)(EntityManager.GetEntity((int)Entities.MassMedia).Clone());
			entity.AttributeSelector = (int)Massmedia.AttributeSelectors.NameAndGroupOnly;
			grdMassmedia.Entity = entity;
            _firm = firm;
            _data = DataAccessor.LoadDataSet("CreateActionMasterPassport", DataAccessor.CreateParametersDictionary());
        }

		protected override void OnLoad(EventArgs e)
		{
            try
            {
                base.OnLoad(e);

                cmbRadioStationGroup.DataSource = _data.Tables["massmedia_group"].DefaultView;
                lookUpPaymentType.DataSource = _data.Tables["payment_type"].DefaultView;
                DisplayRadiostations();
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
        }

		private void grdMassmedia_ObjectChecked(PresentationObject presentationObject, bool state)
		{
			UpdateBtnOkEnabled();
		}

		private void UpdateBtnOkEnabled()
		{
			btnOk.Enabled = grdMassmedia.Added2Checked.Count > 1 && lookUpPaymentType.SelectedValue != null;
		}


        private void DisplayRadiostations()
        {
            int groupId = ParseHelper.GetInt32FromObject(cmbRadioStationGroup.SelectedValue, 0);

            DataTable dtMassmedia = _data.Tables["massmedia"];
            StringBuilder filter = new StringBuilder();

            if (groupId != 0)
                filter.Append("groupID = " + groupId);

            grdMassmedia.SelectedObject = null;
            dtMassmedia.DefaultView.RowFilter = filter.ToString();
            grdMassmedia.DataSource = dtMassmedia.DefaultView;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                string agenciesIDs = CreateAgencciesIDSString();
                if(agenciesIDs == null)
                {
                    DialogResult = DialogResult.None;
                    return;
                }

                Globals.SetWaitCursor(this);
                Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
                parameters["massmediaString"] = GetMassmediasIDsString();
                parameters["agencyString"] = agenciesIDs;
                parameters["firmID"] = _firm.FirmId;
                parameters["paymentTypeID"] = lookUpPaymentType.SelectedValue;
                DataAccessor.ExecuteNonQuery("CreateActionWithRange", parameters);
                ActionID = ParseHelper.GetInt32FromObject(parameters["actionID"], 0);
                Globals.SetDefaultCursor(this);
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
        }

        private string CreateAgencciesIDSString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Massmedia m in grdMassmedia.Added2Checked.Cast<Massmedia>())
            {
                DataTable agencies = m.Agencies;
                if (agencies.Rows.Count == 1)
                    sb.AppendFormat("{0},", agencies.Rows[0][Agency.ParamNames.AgencyId]);
                else
                {
                    SelectionForm selector = new SelectionForm(m, "Выбор агентства для радиостанции " + m.Name , false);
                    if (selector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
                    {
                        sb.AppendFormat("{0},", ((MassmediaAgency)selector.SelectedObject).AgencyId);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            return sb.ToString();
        }

        private string GetMassmediasIDsString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (PresentationObject o in grdMassmedia.Added2Checked)
            {
                sb.AppendFormat("{0},", o.IDs[0]);
            }
            return sb.ToString();
        }

	    public int MassmediasCount
	    {
            get { return grdMassmedia.Added2Checked.Count; }
	    }

        public int ActionID
        {
            get; private set;
        }

        private void SelectedItemChanged(object sender, EventArgs e)
        {
            try
            {
                DisplayRadiostations();
                UpdateBtnOkEnabled();
            }
            catch(Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
        }
    }
}
