using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using CrystalDecisions.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;

namespace Merlin.Forms.GridReport
{
	public class FrmGridReport : Form
	{
		private ImageList imageList1;
		private ToolBar tlbJournal;
		private ToolBarButton tbRefresh;
		private CrystalReportViewer viewer;
		private Panel panel1;
		private Label label1;
		private Label label2;
		private DateTimePicker dtDate;
		private Label label3;
		private ObjectPicker2 userPicker;
		private ObjectPicker2 massmediaPicker;
		private IContainer components;
		private ToolBarButton tbExport;

		public FrmGridReport()
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				InitializeComponent();
				InitializeControls();
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

		private void InitializeControls()
		{
			Entity entity = EntityManager.GetEntity((int) Entities.User);
			Dictionary<string, object> procParams = DataAccessor.CreateParametersDictionary();
			DataSet ds = DataAccessor.LoadDataSet("UserListByRights", procParams);
			userPicker.SetDataSource(entity, ds.Tables[0]);

			entity = EntityManager.GetEntity((int) Entities.MassMedia);
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters = DataAccessor.PrepareParameters(entity);
			ds = DataAccessor.DoAction(procParameters) as DataSet;
			massmediaPicker.SetDataSource(entity, ds.Tables[Constants.TableNames.Data]);
			
			userPicker.IsCreateNewAllowed = false;
			massmediaPicker.IsCreateNewAllowed = false;

			bool hasRight = SecurityManager.LoggedUser.IsRightToViewForeignActions()
			                || SecurityManager.LoggedUser.IsRightToViewGroupActions();
			if (!hasRight)
				userPicker.SelectObject(SecurityManager.LoggedUser.Id);
			userPicker.Enabled = hasRight;

			imageList1.Images.Add(Globals.GetImage(Constants.ActionsImages.Refresh));
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmGridReport));
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.tlbJournal = new System.Windows.Forms.ToolBar();
            this.tbRefresh = new System.Windows.Forms.ToolBarButton();
            this.tbExport = new System.Windows.Forms.ToolBarButton();
            this.viewer = new CrystalDecisions.Windows.Forms.CrystalReportViewer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.massmediaPicker = new FogSoft.WinForm.Controls.ObjectPicker2();
            this.userPicker = new FogSoft.WinForm.Controls.ObjectPicker2();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.dtDate = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "");
            this.imageList1.Images.SetKeyName(1, "");
            this.imageList1.Images.SetKeyName(2, "");
            this.imageList1.Images.SetKeyName(3, "");
            this.imageList1.Images.SetKeyName(4, "");
            this.imageList1.Images.SetKeyName(5, "");
            this.imageList1.Images.SetKeyName(6, "");
            this.imageList1.Images.SetKeyName(7, "");
            this.imageList1.Images.SetKeyName(8, "new.ico");
            // 
            // tlbJournal
            // 
            this.tlbJournal.Appearance = System.Windows.Forms.ToolBarAppearance.Flat;
            this.tlbJournal.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
            this.tbRefresh,
            this.tbExport});
            this.tlbJournal.Divider = false;
            this.tlbJournal.DropDownArrows = true;
            this.tlbJournal.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tlbJournal.ImageList = this.imageList1;
            this.tlbJournal.Location = new System.Drawing.Point(0, 0);
            this.tlbJournal.Name = "tlbJournal";
            this.tlbJournal.ShowToolTips = true;
            this.tlbJournal.Size = new System.Drawing.Size(882, 27);
            this.tlbJournal.TabIndex = 2;
            this.tlbJournal.TextAlign = System.Windows.Forms.ToolBarTextAlign.Right;
            this.tlbJournal.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(this.tlbJournal_ButtonClick);
            // 
            // tbRefresh
            // 
            this.tbRefresh.Enabled = false;
            this.tbRefresh.ImageIndex = 9;
            this.tbRefresh.Name = "tbRefresh";
            this.tbRefresh.Tag = "REFRESH";
            this.tbRefresh.Text = "Обновить";
            this.tbRefresh.ToolTipText = "Обновить журнал";
            // 
            // tbExport
            // 
            this.tbExport.Enabled = false;
            this.tbExport.ImageIndex = 8;
            this.tbExport.Name = "tbExport";
            this.tbExport.Text = "Экспорт...";
            this.tbExport.ToolTipText = "Экспортировать плей-лист...";
            // 
            // viewer
            // 
            this.viewer.ActiveViewIndex = -1;
            this.viewer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.viewer.Cursor = System.Windows.Forms.Cursors.Default;
            this.viewer.DisplayBackgroundEdge = false;
            this.viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.viewer.Location = new System.Drawing.Point(0, 89);
            this.viewer.Name = "viewer";
            this.viewer.SelectionFormula = "";
            this.viewer.ShowCloseButton = false;
            this.viewer.ShowGotoPageButton = false;
            this.viewer.ShowGroupTreeButton = false;
            this.viewer.ShowRefreshButton = false;
            this.viewer.ShowTextSearchButton = false;
            this.viewer.Size = new System.Drawing.Size(882, 639);
            this.viewer.TabIndex = 3;
            this.viewer.ViewTimeSelectionFormula = "";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.massmediaPicker);
            this.panel1.Controls.Add(this.userPicker);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.dtDate);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 27);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(882, 62);
            this.panel1.TabIndex = 4;
            // 
            // massmediaPicker
            // 
            this.massmediaPicker.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.massmediaPicker.Location = new System.Drawing.Point(124, 8);
            this.massmediaPicker.Name = "massmediaPicker";
            this.massmediaPicker.Size = new System.Drawing.Size(580, 24);
            this.massmediaPicker.TabIndex = 8;
            this.massmediaPicker.ObjectSelected += new FogSoft.WinForm.ObjectDelegate(this.massmediaPicker_ObjectSelected);
            this.massmediaPicker.SelectionCleared += new FogSoft.WinForm.EmptyDelegate(this.massmediaPicker_SelectionCleared);
            // 
            // userPicker
            // 
            this.userPicker.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.userPicker.Location = new System.Drawing.Point(456, 32);
            this.userPicker.Name = "userPicker";
            this.userPicker.Size = new System.Drawing.Size(248, 24);
            this.userPicker.TabIndex = 5;
            this.userPicker.ObjectSelected += new FogSoft.WinForm.ObjectDelegate(this.userPicker_ObjectSelected);
            this.userPicker.SelectionCleared += new FogSoft.WinForm.EmptyDelegate(this.userPicker_SelectionCleared);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(373, 34);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(78, 17);
            this.label3.TabIndex = 4;
            this.label3.Text = "Менеджер:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(45, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "Дата:";
            // 
            // dtDate
            // 
            this.dtDate.Location = new System.Drawing.Point(124, 32);
            this.dtDate.Name = "dtDate";
            this.dtDate.Size = new System.Drawing.Size(224, 24);
            this.dtDate.TabIndex = 2;
            this.dtDate.ValueChanged += new System.EventHandler(this.ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(131, 21);
            this.label1.TabIndex = 0;
            this.label1.Text = "Радиостанция:";
            // 
            // FrmGridReport
            // 
            this.ClientSize = new System.Drawing.Size(882, 728);
            this.Controls.Add(this.viewer);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.tlbJournal);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.MinimumSize = new System.Drawing.Size(648, 400);
            this.Name = "FrmGridReport";
            this.ShowInTaskbar = false;
            this.Text = "Сетка вещания";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private void massmediaPicker_ObjectSelected(PresentationObject presentationObject)
		{
			tbRefresh.Enabled = (presentationObject != null);
			tbExport.Enabled = (presentationObject != null);
			viewer.ReportSource = null;
		}

		private void massmediaPicker_SelectionCleared()
		{
			tbRefresh.Enabled = false;
			tbExport.Enabled = false;
			viewer.ReportSource = null;
		}

		private void tlbJournal_ButtonClick(object sender, ToolBarButtonClickEventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				GridReportCreator creater = new GridReportCreator(massmediaPicker.SelectedObject as Massmedia, dtDate.Value.Date, userPicker.SelectedObject);
				if (e.Button == tbRefresh)
				{
					viewer.ReportSource = creater.GetReport();
				}
				if (e.Button == tbExport)
				{
					creater.ExportDocument();
				}
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

		private void ValueChanged(object sender, EventArgs e)
		{
			viewer.ReportSource = null;
		}

		private void userPicker_ObjectSelected(PresentationObject presentationObject)
		{
			viewer.ReportSource = null;
		}

		private void userPicker_SelectionCleared()
		{
			viewer.ReportSource = null;
		}
	}
}