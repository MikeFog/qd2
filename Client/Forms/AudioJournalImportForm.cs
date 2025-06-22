using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm;
using System.Data.SqlClient;
using Microsoft.ApplicationBlocks.Data;
using System.Threading;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;

namespace Merlin.Forms
{
    public partial class AudioJournalImportForm : FogSoft.WinForm.Forms.JournalForm 
    {
        private ToolStripButton tsbImport;

        public AudioJournalImportForm()
            : base(EntityManager.GetEntity((int)Entities.ImportRollers), "Импортирование роликов")
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
            tsbImport.ToolTipText = "Импортировать выбранные ролики";
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

                SqlParameter[] cmdParametersWithValues = new SqlParameter[Filters.Count];

                using (SqlConnection connection = new SqlConnection(ConfigurationUtil.ConnectionStringLegacyDB.ConnectionString))
                {
                    connection.Open();
                    dtData = SqlHelper.ExecuteDataset(connection, CommandType.StoredProcedure, "Rollers", 300, DataAccessor.AssignSqlParameters(connection, "Rollers", Filters)).Tables[0];
                    dtData.Columns.Add("isNew", typeof(bool));
                    connection.Close();
                }

                DataTable existingRollers;
                using (SqlConnection connection = new SqlConnection(ConfigurationUtil.ConnectionStringMain.ConnectionString))
                {
                    connection.Open();
                    existingRollers = SqlHelper.ExecuteDataset(connection, CommandType.StoredProcedure, @"Rollers", 300, DataAccessor.AssignSqlParameters(connection, "Rollers", Filters)).Tables[0];
                    connection.Close();
                }

                foreach (DataRow row in existingRollers.Rows)
                {
                    string rollerName = (string)row["name"];

                    foreach (DataRow rowImport in dtData.Rows)
                    {
                        string importRollerName = (string)rowImport["name"];
                        int compare = string.Compare(importRollerName, rollerName, StringComparison.CurrentCulture);
                        if (compare == 0)
                        {
                            if (showOnlyNew)
                            {
                                dtData.Rows.Remove(rowImport);
                            }
                            else
                            {
                                rowImport["rollerID"] = row["rollerID"];
                                rowImport["isNew"] = false;
                            }

                            break;
                        }
                        else if (compare > 0)
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
            bool cancelledByUser = false;

            try
            {
                if (!IsDisposed && IsHandleCreated)
                    Globals.SetWaitCursor(this);

                Enabled = false;

                foreach (PresentationObject obj in Grid.Added2Checked)
                {
                    Roller roller = new Roller(obj.Parameters);

                    int firmID = ParseHelper.GetInt32FromObject(obj["firmID"], -1);
                    if (firmID >= 0)
                    {
                        Dictionary<string, object> parameters = new Dictionary<string, object>
					                                        	    {
                                                                        {"firmID", firmID}
					                                        	    };

                        bool hasFirm = DataAccessor.LoadDataSet("firms", parameters).Tables[0].Rows.Count > 0;

                        if (!hasFirm)
                        {
                            if (FogSoft.WinForm.Forms.MessageBox.ShowQuestion(
                                string.Format("Ролик '{0}' не может быть проимпортирован, так как Фирма-заказчик ролика не существует в текущей базе данных. Продолжить импортировать оставшиеся ролики?", roller.Name)) == DialogResult.No)
                            {
                                cancelledByUser = true;
                                break;
                            }

                            continue;
                        }
                    }

                    roller.IsNew = ParseHelper.GetBooleanFromObject(roller["isNew"], true);
                    if (roller.IsNew)
                    {
                        roller["rollerID"] = null;
                    }

                    roller.Update();
                }

                if (!cancelledByUser)
                {
                    FogSoft.WinForm.Forms.MessageBox.ShowCompleted("Выбранные ролики сохранены в текущую Базу Данных");
                }
            }
            catch (Exception e)
            {
                hasErrors = true;
                ErrorManager.PublishError(e);
            }
            finally
            {
                Enabled = true;

                Grid.Entity.ClearCache();
                EntityManager.GetEntity((int)Entities.Roller).ClearCache();

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
