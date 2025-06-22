using System;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using Merlin.Classes;

namespace Merlin.Forms
{
	internal class FrmPackModuleContent : Form
	{
		private Panel panel1;
		private SmartGrid grdMassmedia;
		private SmartGrid grdModules;
		private Button btnCancel;
		private Button btnOk;
		private Splitter splitter1;
		private Pricelist pricelist;

		#region Constructors ----------------------------------

		public FrmPackModuleContent()
		{
			InitializeComponent();
		}

		public FrmPackModuleContent(PackModulePricelist pricelist) : this()
		{
			this.pricelist = pricelist;
			Entity entityMassmedia = EntityManager.GetEntity((int) Entities.MassMedia);
			grdMassmedia.Entity = entityMassmedia;
			grdMassmedia.DataSource = entityMassmedia.GetContent().DefaultView;
		}

		#endregion

		#region Windows Form Designer generated code ----------

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.panel1 = new System.Windows.Forms.Panel();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.grdModules = new FogSoft.WinForm.Controls.SmartGrid();
            this.grdMassmedia = new FogSoft.WinForm.Controls.SmartGrid();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.splitter1);
            this.panel1.Controls.Add(this.grdModules);
            this.panel1.Controls.Add(this.grdMassmedia);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(584, 320);
            this.panel1.TabIndex = 2;
            // 
            // splitter1
            // 
            this.splitter1.Location = new System.Drawing.Point(296, 0);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(3, 320);
            this.splitter1.TabIndex = 5;
            this.splitter1.TabStop = false;
            // 
            // grdModules
            // 
            this.grdModules.Caption = "Модули";
            this.grdModules.CaptionVisible = true;
            this.grdModules.CheckBoxes = true;
            this.grdModules.ColumnNameHighlight = null;
            this.grdModules.DataSource = null;
            this.grdModules.DependantGrid = null;
            this.grdModules.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdModules.Entity = null;
            this.grdModules.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdModules.IsHighlightInvertColor = false;
            this.grdModules.IsNeedHighlight = false;
            this.grdModules.Location = new System.Drawing.Point(296, 0);
            this.grdModules.MenuEnabled = false;
            this.grdModules.Name = "grdModules";
            this.grdModules.QuickSearchVisible = false;
            this.grdModules.SelectedObject = null;
            this.grdModules.Size = new System.Drawing.Size(288, 320);
            this.grdModules.TabIndex = 4;
            // 
            // grdMassmedia
            // 
            this.grdMassmedia.Caption = "Радиостанция";
            this.grdMassmedia.CaptionVisible = true;
            this.grdMassmedia.CheckBoxes = true;
            this.grdMassmedia.ColumnNameHighlight = null;
            this.grdMassmedia.Cursor = System.Windows.Forms.Cursors.Default;
            this.grdMassmedia.DataSource = null;
            this.grdMassmedia.DependantGrid = null;
            this.grdMassmedia.Dock = System.Windows.Forms.DockStyle.Left;
            this.grdMassmedia.Entity = null;
            this.grdMassmedia.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdMassmedia.IsHighlightInvertColor = false;
            this.grdMassmedia.IsNeedHighlight = false;
            this.grdMassmedia.Location = new System.Drawing.Point(0, 0);
            this.grdMassmedia.MenuEnabled = false;
            this.grdMassmedia.Name = "grdMassmedia";
            this.grdMassmedia.QuickSearchVisible = false;
            this.grdMassmedia.SelectedObject = null;
            this.grdMassmedia.Size = new System.Drawing.Size(296, 320);
            this.grdMassmedia.TabIndex = 2;
            this.grdMassmedia.ObjectChecked += new FogSoft.WinForm.ObjectCheckedDelegate(this.lvwMassmedia_ObjectChecked);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnCancel.Location = new System.Drawing.Point(295, 328);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(83, 22);
            this.btnCancel.TabIndex = 17;
            this.btnCancel.Text = "Отмена";
            // 
            // btnOk
            // 
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOk.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnOk.Location = new System.Drawing.Point(207, 328);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(84, 22);
            this.btnOk.TabIndex = 16;
            this.btnOk.Text = "Ок";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // FrmPackModuleContent
            // 
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(584, 359);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Name = "FrmPackModuleContent";
            this.Text = "Пакетный модуль";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

		private void lvwMassmedia_ObjectChecked(PresentationObject presentationObject, bool state)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.Default;
				if (state)
					AddModules(presentationObject as Massmedia);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Application.DoEvents();
				Cursor = Cursors.Default;
			}
		}

		public void AddModules(Massmedia massmedia)
		{
			grdModules.Entity = EntityManager.GetEntity((int) Entities.Module);
			grdModules.DataSource = massmedia.GetModules(pricelist.StartDate).DefaultView;
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				// Delete 'deleted' objects
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
	}
}