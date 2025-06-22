using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Forms;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using static Merlin.Forms.UniversalPassportForm;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace Merlin.Classes
{
    internal class PackageDiscountPriceList : ObjectContainer
    {
        private struct ParamNames
        {
            public const string isForType1 = "isForType1";
            public const string isForType2 = "isForType2";
            public const string isForType3 = "isForType3";
            public const string packageDiscountPriceListID = "packageDiscountPriceListID";
        }

        public PackageDiscountPriceList(int Id) : this()
        {
            this["packageDiscountPriceListID"] = Id;
            isNew = false;
            Refresh();
            iterator.ChildEntity = EntityManager.GetEntity((int)Entities.PackageDiscountMassmedia);
        }

        public PackageDiscountPriceList() : base(EntityManager.GetEntity((int)Entities.PackageDiscountPriceLists))
        {
        }

        public PackageDiscountPriceList(Entity entity, DataRow row) : base(entity, row)
        {
        }

        protected override void AssignNew(IWin32Window owner)
        {
            if (GetContent().Rows.Count == 0)
                AssignMany((Form)owner);
            else
                base.AssignNew(owner);
        }

        private void AssignMany(Form owner)
        {
            DataTable dt = EntityManager.GetEntity((int)Entities.MassMedia).GetContent();
            dt.TableName = "massmedia";
            DataSet ds = new DataSet();
            ds.Tables.Add(dt.Copy());
            childrenChangesList.Clear();
            UniversalPassportForm frm = new UniversalPassportForm(this, "PackDiscountRadiostations", "Радиостанции", EntityManager.GetEntity((int)Entities.PackageDiscountMassmedia), 
                ds, ValidatePassportData, ApplyPassportData);
            if(frm.ShowDialog(owner) == DialogResult.OK)
                FireContainerRefreshed();
        }

        private bool ValidatePassportData(Dictionary<string, object> parameters)
        {
            
            if (!(bool)parameters[ParamNames.isForType1] && !(bool)parameters[ParamNames.isForType2] && !(bool)parameters[ParamNames.isForType3])
            {
                MessageBox.ShowExclamation(Properties.Resources.NoCampaignTypeSelected);
                return false;
            }

            if(SelectedRadioStations.Count == 0)
            {
                MessageBox.ShowExclamation(Properties.Resources.NoRadiostationSelected);
                return false;
            }

            return true;
        }

        private void ApplyPassportData(Dictionary<string, object> parameters)
        {
            try
            {
                Application.DoEvents();
                Cursor.Current = Cursors.WaitCursor;
                 
                foreach (var rs in SelectedRadioStations)
                {
                    PresentationObject po = new PresentationObject(EntityManager.GetEntity((int)Entities.PackageDiscountMassmedia))
                    {
                        Parameters = parameters
                    };
                    po[Massmedia.ParamNames.MassmediaId] = rs.MassmediaId;
                    po[ParamNames.packageDiscountPriceListID] = this[ParamNames.packageDiscountPriceListID];
                    po.IsNew = true;
                    po.Update();
                }
            }
            finally 
            {
                Cursor.Current = Cursors.Default; 
            }
        }

        private List<Massmedia> SelectedRadioStations
        {
            get
            {
                List<Massmedia> radioStations = new List<Massmedia>();
                foreach (ChildrenChanges childrenChanges in childrenChangesList)
                {
                    foreach (PresentationObject po in childrenChanges.AddedObjects)
                    {
                        Massmedia rs = po as Massmedia;
                        if(po !=null)
                            radioStations.Add(rs);
                    }
                }

                return radioStations;
            }
        }
    }
}
