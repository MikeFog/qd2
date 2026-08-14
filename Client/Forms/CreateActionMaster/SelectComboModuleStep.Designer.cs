namespace Merlin.Forms.CreateActionMaster
{
	partial class SelectComboModuleStep
	{
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
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
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.lblPaymentType = new System.Windows.Forms.Label();
			this.lookUpPaymentType = new FogSoft.WinForm.LookUp();
			this.lblComboModule = new System.Windows.Forms.Label();
			this.grdComboModules = new FogSoft.WinForm.Controls.SmartGrid();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.tableLayoutPanel1.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			//
			// lblPaymentType
			//
			this.lblPaymentType.AutoSize = true;
			this.lblPaymentType.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lblPaymentType.Location = new System.Drawing.Point(3, 0);
			this.lblPaymentType.Name = "lblPaymentType";
			this.lblPaymentType.Size = new System.Drawing.Size(130, 20);
			this.lblPaymentType.TabIndex = 0;
			this.lblPaymentType.Text = "Выберите тип оплаты:";
			this.lblPaymentType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// lookUpPaymentType
			//
			this.lookUpPaymentType.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lookUpPaymentType.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.lookUpPaymentType.IsNullable = false;
			this.lookUpPaymentType.Location = new System.Drawing.Point(3, 23);
			this.lookUpPaymentType.Name = "lookUpPaymentType";
			this.lookUpPaymentType.SelectedIndex = -1;
			this.lookUpPaymentType.SelectedValue = null;
			this.lookUpPaymentType.Size = new System.Drawing.Size(478, 33);
			this.lookUpPaymentType.TabIndex = 1;
			this.lookUpPaymentType.SelectedItemChanged += new System.EventHandler(this.SelectedItemChanged);
			//
			// lblComboModule
			//
			this.lblComboModule.AutoSize = true;
			this.lblComboModule.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lblComboModule.Location = new System.Drawing.Point(3, 59);
			this.lblComboModule.Name = "lblComboModule";
			this.lblComboModule.Size = new System.Drawing.Size(150, 20);
			this.lblComboModule.TabIndex = 2;
			this.lblComboModule.Text = "Выберите комбо-модуль:";
			this.lblComboModule.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// grdComboModules
			//
			this.grdComboModules.Caption = "Комбо-модули";
			this.grdComboModules.CaptionVisible = true;
			this.grdComboModules.ColumnNameHighlight = null;
			this.grdComboModules.DataSource = null;
			this.grdComboModules.DependantGrid = null;
			this.grdComboModules.Dock = System.Windows.Forms.DockStyle.Fill;
			this.grdComboModules.Entity = null;
			this.grdComboModules.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.grdComboModules.IsHighlightInvertColor = false;
			this.grdComboModules.IsNeedHighlight = false;
			this.grdComboModules.Location = new System.Drawing.Point(3, 82);
			this.grdComboModules.MenuEnabled = false;
			this.grdComboModules.Name = "grdComboModules";
			this.grdComboModules.QuickSearchVisible = false;
			this.grdComboModules.SelectedObject = null;
			this.grdComboModules.Size = new System.Drawing.Size(478, 300);
			this.grdComboModules.TabIndex = 3;
			this.grdComboModules.ObjectSelected += new FogSoft.WinForm.ObjectDelegate(this.grdComboModules_ObjectSelected);
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnCancel.Location = new System.Drawing.Point(378, 3);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(100, 33);
			this.btnCancel.TabIndex = 5;
			this.btnCancel.Text = "Отмена";
			//
			// btnOk
			//
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.Enabled = false;
			this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnOk.Location = new System.Drawing.Point(272, 3);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(100, 33);
			this.btnOk.TabIndex = 4;
			this.btnOk.Text = "Ок";
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			//
			// flowLayoutPanel1
			//
			this.flowLayoutPanel1.Controls.Add(this.btnCancel);
			this.flowLayoutPanel1.Controls.Add(this.btnOk);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 388);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(478, 39);
			this.flowLayoutPanel1.TabIndex = 6;
			//
			// tableLayoutPanel1
			//
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.lblPaymentType, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.lookUpPaymentType, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.lblComboModule, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.grdComboModules, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 4);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(12);
			this.tableLayoutPanel1.RowCount = 5;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(508, 451);
			this.tableLayoutPanel1.TabIndex = 0;
			//
			// SelectComboModuleStep
			//
			this.AcceptButton = this.btnOk;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(508, 451);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.MinimizeBox = false;
			this.MaximizeBox = false;
			this.Name = "SelectComboModuleStep";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Размещение комбо-модулями: выбор комбо-модуля";
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.Label lblPaymentType;
		private FogSoft.WinForm.LookUp lookUpPaymentType;
		private System.Windows.Forms.Label lblComboModule;
		private FogSoft.WinForm.Controls.SmartGrid grdComboModules;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
	}
}
