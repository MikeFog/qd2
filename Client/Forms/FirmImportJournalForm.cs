using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using System.Data.SqlClient;
using Microsoft.ApplicationBlocks.Data;
using FogSoft.WinForm;
using System.Threading;
using FogSoft.WinForm.Forms;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using System.Globalization;

namespace Merlin.Forms
{
    public partial class FirmImportJournalForm : JournalForm
    {
        private ToolStripButton tsbImport;
        private DataTable dtImportFirmBrands;
        private DataTable dtExistingFirmBrands;
        private DataTable dtExistingBrands;

        public FirmImportJournalForm()
            : base(EntityManager.GetEntity((int)Entities.ImportFirms), "Импортирование фирм")
        {
            InitializeComponent();
            
            Grid.CheckBoxes = true;

            tsbImport = new ToolStripButton();

            TsJournal.Items.Insert(0, tsbImport);

            tsbImport.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            tsbImport.Image = Globals.GetImage("Icons.Import.png");
            tsbImport.ImageTransparentColor = Color.Magenta;
            tsbImport.Name = "tsbImport";
            tsbImport.Size = new Size(41, 22);
            tsbImport.Text = "Импортировать выбранное";
            tsbImport.ToolTipText = "Импортировать выбранные фирмы";
            tsbImport.Click += new EventHandler(tsbImport_Click);

            TsJournal.Items[1].Visible = false; // New
            TsJournal.Items[2].Visible = false; // Edit
            TsJournal.Items[3].Visible = false; // Delete
            TsJournal.Items[8].Visible = false; // Sum
        }

        protected override void LoadData(object stateinfo)
        {
            bool showOnlyNew = ParseHelper.GetBooleanFromObject(Filters["showOnlyNew"], true);

            try
            {
                Application.DoEvents();

                if (!IsDisposed && IsHandleCreated)
                    Globals.SetWaitCursor(this);

                Grid.Entity.ClearCache();

                using (SqlConnection connection = new SqlConnection(ConfigurationUtil.ConnectionStringLegacyDB.ConnectionString))
                {
                    connection.Open();
                    dtData = SqlHelper.ExecuteDataset(connection, CommandType.Text, @"select 
	f.*,
	b.bik as bank_bik,
	b.corAccount as bank_corAccount,
	b.name as bank_name,
    cast(1 as bit) as isNew
from Firm f
 left join Bank b on f.bankID = b.bankId
order by f.firmID ", 300, null).Tables[0];
                    dtImportFirmBrands = SqlHelper.ExecuteDataset(connection, CommandType.Text, @"select fb.firmID, b.name
from FirmBrand fb
	inner join Brand b on fb.brandID = b.brandID order by fb.firmId", 300, null).Tables[0];
                    connection.Close();
                }

                DataTable existingFirms;
                using (SqlConnection connection = new SqlConnection(ConfigurationUtil.ConnectionStringMain.ConnectionString))
                {
                    connection.Open();
                    existingFirms = SqlHelper.ExecuteDataset(connection, CommandType.Text, @"select firmID from Firm f order by f.firmID", 300, null).Tables[0];
                    dtExistingFirmBrands = SqlHelper.ExecuteDataset(connection, CommandType.Text, @"select fb.firmID, fb.brandID, b.name from FirmBrand fb inner join Brand b on fb.brandID = b.brandID order by fb.firmId", 300, null).Tables[0];
                    dtExistingBrands = SqlHelper.ExecuteDataset(connection, CommandType.Text, @"select brandID, name from Brand order by brandID", 300, null).Tables[0];
                    connection.Close();
                }

                foreach (DataRow row in existingFirms.Rows)
                {
                    int firmID = (short)row["firmID"];

                    foreach (DataRow rowImport in dtData.Rows)
                    {
                        int importFirmID = (short)rowImport["firmID"];

                        if (firmID == importFirmID)
                        {
                            if (showOnlyNew)
                            {
                                dtData.Rows.Remove(rowImport);
                            }
                            else
                            {
                                rowImport["isNew"] = false;
                            }

                            break;
                        }
                        else if (importFirmID > firmID)
                        {
                            break;
                        }
                    }
                }

                if (!IsHandleCreated)
                    Thread.Sleep(500);

                if (!IsDisposed && IsHandleCreated)
                    Invoke(new Globals.VoidCallback(PopulateDataGrid));
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
            finally
            {
                if (!IsDisposed && IsHandleCreated)
                    Globals.SetDefaultCursor(this);
            }
        }

        private void tsbImport_Click(object sender, EventArgs eventArgs)
        {
            if (Grid.Added2Checked.Count == 0)
                return;

            bool hasErrors = false;

            try
            {
                if (!IsDisposed && IsHandleCreated)
                    Globals.SetWaitCursor(this);

                Enabled = false;

                foreach (PresentationObject obj in Grid.Added2Checked)
                {
                    Firm firm = new Firm(obj.Parameters);
                    firm["bankId"] = null;

                    if (!string.IsNullOrEmpty(ParseHelper.GetStringFromObject(obj["bank_name"], null)))
                    {
                        Dictionary<string, object> parameters = new Dictionary<string, object>
					                                        	    {
                                                                        {"bankId", null},
					                                        		    {"bik", obj["bank_bik"] },
					                                        		    {"name", obj["bank_name"] },
					                                        		    {"corAccount", obj["bank_corAccount"] }
					                                        	    };

                        DataAccessor.ExecuteNonQuery("bankImport", parameters);

                        obj["bankId"] = parameters["bankId"];
                    }

                    firm.IsNew = ParseHelper.GetBooleanFromObject(firm["isNew"], true);
                    firm.Update();

                    DataRow[] importFirmBrands = dtImportFirmBrands.Select(string.Format(CultureInfo.InvariantCulture, "firmID = {0}", firm["firmID"]));
                    DataRow[] existingFirmBrands = dtExistingFirmBrands.Select(string.Format(CultureInfo.InvariantCulture, "firmID = {0}", firm["firmID"]));

                    foreach (DataRow importBrand in importFirmBrands)
                    {
                        bool contains = false;
                        string importBrandName = ParseHelper.GetStringFromObject(importBrand["name"], null);
                        foreach (DataRow existingBrand in existingFirmBrands)
                        {
                            string existingBrandName = ParseHelper.GetStringFromObject(existingBrand["name"], null);
                            if (string.Equals(importBrandName, existingBrandName, StringComparison.CurrentCulture))
                            {
                                contains = true;
                                break;
                            }
                        }

                        if (!contains)
                        {
                            Brand brand = null;

                            DataRow[] existingBrands = dtExistingBrands.Select(string.Format(CultureInfo.InvariantCulture, "name = '{0}'", importBrandName));
                            if (existingBrands.Length > 0)
                            {
                                brand = new Brand(existingBrands[0]);
                            }
                            else
                            {
                                brand = new Brand();
                                brand["name"] = importBrandName;
                                brand.IsNew = true;
                                brand.Update();
                            }

                            brand.AssignFirm(firm);
                        }
                    }
                }

                FogSoft.WinForm.Forms.MessageBox.ShowCompleted("Выбранные фирмы сохранены в текущую Базу Данных");
            }
            catch (Exception e)
            {
                hasErrors = true;
                ErrorManager.PublishError(e);
            }
            finally
            {
                Grid.Entity.ClearCache();

                EntityManager.GetEntity((int)Entities.Brand).ClearCache();
                EntityManager.GetEntity((int)Entities.FirmBrand).ClearCache();
                EntityManager.GetEntity((int)Entities.Firm).ClearCache();

                Enabled = true;

                if (!IsDisposed && IsHandleCreated)
                    Globals.SetDefaultCursor(this);
            }

            if (!hasErrors)
            {
                dtData.Rows.Clear();
                PopulateDataGrid();
                Application.DoEvents();
                RefreshJournal();
            }
        }
    }
}
