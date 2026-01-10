using FogSoft.WinForm.Classes;
using Merlin.Classes;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Merlin.Controls
{
    public partial class RadiostationListWithGroup : UserControl
    {
        public RadiostationListWithGroup()
        {
            InitializeComponent();
        }

        public void LoadData()
        {
            grdRadiostations.Entity = EntityManager.GetEntity((int)Entities.MassMedia);
            grdRadiostations.Entity.AttributeSelector = (int)Massmedia.AttributeSelectors.TrafficDeadLine;

            cmbRadioStationGroup.ColumnWithID = "massmediaGroupID";
            cmbRadioStationGroup.DataSource = Massmedia.LoadGroupsWithShowAllOption();
        }

        public List<PresentationObject> SelectedItems
        {
            get
            {
                return grdRadiostations.Added2Checked;
            }
        }

        private void CmbGroup_SelectedItemChanged(object sender, System.EventArgs e)
        {
            Massmedia.LoadRadiostationsByGroup(cmbRadioStationGroup, grdRadiostations);
        }
    }
}
