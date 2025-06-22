using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;

namespace Merlin.Forms
{
	public class FrmManagerSelector : Form
	{
		#region Members ---------------------------------------

		private Label label1;
		private Label label2;
		private DateTimePicker dtStart;
		private DateTimePicker dtFinish;
		private Button btnRefresh;
		private SmartGrid grdUsers;
		private Button btnCancel;
		private Button btnOk;
		private readonly Container components = null;
		private List<PresentationObject> selectedUsers;
		private DateTime startDate, finishDate;
		private ObjectPicker2 objectPickerAgency;
		private Label label3;

		private readonly InterfaceObjects interfaceObj;

		#endregion



		public FrmManagerSelector(InterfaceObjects interfaceObj)
		{
			InitializeComponent();
			dtStart.Value = DateTime.Now.AddDays(-7);
			InitAgencies();
			this.interfaceObj = interfaceObj;
		}

		private void InitAgencies()
		{
			objectPickerAgency.IsCreateNewAllowed = false;
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(EntityManager.GetEntity((int)Entities.Agency),
				                               InterfaceObjects.SimpleJournal, Constants.Actions.Load);
			DataSet ds = DataAccessor.DoAction(procParameters) as DataSet;
			objectPickerAgency.SetDataSource(EntityManager.GetEntity((int)Entities.Agency), ds.Tables[Constants.TableNames.Data]);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.dtStart = new System.Windows.Forms.DateTimePicker();
			this.dtFinish = new System.Windows.Forms.DateTimePicker();
			this.btnRefresh = new System.Windows.Forms.Button();
			this.grdUsers = new FogSoft.WinForm.Controls.SmartGrid();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.objectPickerAgency = new FogSoft.WinForm.Controls.ObjectPicker2();
			this.label3 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(8, 10);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(105, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Начало интервала:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(8, 34);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(124, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Окончание интервала:";
			// 
			// dtStart
			// 
			this.dtStart.Location = new System.Drawing.Point(152, 8);
			this.dtStart.Name = "dtStart";
			this.dtStart.Size = new System.Drawing.Size(200, 21);
			this.dtStart.TabIndex = 2;
			// 
			// dtFinish
			// 
			this.dtFinish.Location = new System.Drawing.Point(152, 32);
			this.dtFinish.Name = "dtFinish";
			this.dtFinish.Size = new System.Drawing.Size(200, 21);
			this.dtFinish.TabIndex = 3;
			// 
			// btnRefresh
			// 
			this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnRefresh.Location = new System.Drawing.Point(8, 80);
			this.btnRefresh.Name = "btnRefresh";
			this.btnRefresh.Size = new System.Drawing.Size(344, 24);
			this.btnRefresh.TabIndex = 4;
			this.btnRefresh.Text = "Обновить информацию";
			this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
			// 
			// grdUsers
			// 
			this.grdUsers.Caption = "Менеджеры";
			this.grdUsers.CaptionVisible = true;
			this.grdUsers.CheckBoxes = true;
			this.grdUsers.DataSource = null;
			this.grdUsers.DependantGrid = null;
			this.grdUsers.Entity = null;
			this.grdUsers.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.grdUsers.InterfaceObject = FogSoft.WinForm.InterfaceObjects.FakeModule;
			this.grdUsers.Location = new System.Drawing.Point(8, 110);
			this.grdUsers.MenuEnabled = false;
			this.grdUsers.Name = "grdUsers";
			this.grdUsers.QuickSearchVisible = false;
			this.grdUsers.SelectedObject = null;
			this.grdUsers.Size = new System.Drawing.Size(344, 258);
			this.grdUsers.TabIndex = 5;
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnCancel.Location = new System.Drawing.Point(183, 376);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 9;
			this.btnCancel.Text = "Отмена";
			// 
			// btnOk
			// 
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnOk.Location = new System.Drawing.Point(103, 376);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 8;
			this.btnOk.Text = "Ок";
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// objectPickerAgency
			// 
			this.objectPickerAgency.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.objectPickerAgency.Location = new System.Drawing.Point(152, 56);
			this.objectPickerAgency.Name = "objectPickerAgency";
			this.objectPickerAgency.Size = new System.Drawing.Size(200, 21);
			this.objectPickerAgency.TabIndex = 10;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(8, 58);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(58, 13);
			this.label3.TabIndex = 11;
			this.label3.Text = "Агенство:";
			// 
			// FrmManagerSelector
			// 
			this.AcceptButton = this.btnOk;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(360, 408);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.objectPickerAgency);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.grdUsers);
			this.Controls.Add(this.btnRefresh);
			this.Controls.Add(this.dtFinish);
			this.Controls.Add(this.dtStart);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FrmManagerSelector";
			this.ShowInTaskbar = false;
			this.Text = "Выбор менеджеров";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private void LoadData()
		{
			Entity entity = EntityManager.GetEntity((int) Entities.User);
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				entity, interfaceObj, Constants.Actions.Load);
			procParameters["startDate"] = dtStart.Value.Date;
			procParameters["finishDate"] = dtFinish.Value.Date;
			if (objectPickerAgency.SelectedObject != null)
				procParameters["agencyID"] = objectPickerAgency.SelectedObject.IDs[0];

			DataSet ds = DataAccessor.DoAction(procParameters) as DataSet;
			grdUsers.Entity = entity;
			grdUsers.DataSource = ds.Tables[Constants.TableNames.Data].DefaultView;
		}

		private void btnRefresh_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;
				LoadData();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			selectedUsers = grdUsers.Added2Checked;
			startDate = dtStart.Value;
			finishDate = dtFinish.Value;
		}

		public List<PresentationObject> SelectedUsers
		{
			get { return selectedUsers; }
		}

		public DateTime StartDate
		{
			get { return startDate; }
		}

		public DateTime FinishDate
		{
			get { return finishDate; }
		}

		public Agency SelectedAgency
		{
			get { return objectPickerAgency.SelectedObject as Agency; }
		}
	}
}