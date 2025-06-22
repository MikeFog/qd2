using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using Merlin.Classes;

namespace Merlin.Forms
{
	public abstract class FrmFirmBalance : Form
	{
		#region Members ---------------------------------------

		private Panel panel2;
		private Panel panel1;
		private ObjectPicker2 opUsers;
		private ObjectPicker2 opFirms;
		private DateTimePicker dtFinish;
		private DateTimePicker dtStart;
		private Label label3;
		private Label label2;
		private Label label1;
		private Splitter splitter1;
		private SmartGrid grdAction;
		private Splitter splitter2;
		private SmartGrid grdPayment;
		private SmartGrid grdAgency;
		private ListBox lstResult;
		private Splitter splitter3;
		private ImageList imageList1;
		private ToolBar tlbJournal;
		private ToolBarButton tbRefresh;
		private Label label4;
		private CheckBox checkBoxShowBlack;
		private CheckBox checkBoxShowWhite;
		private IContainer components;

		#endregion

		public SmartGrid GrdAgency
		{
			get { return grdAgency; }
		}

		public ObjectPicker2 OpFirms
		{
			get { return opFirms; }
		}

		public ObjectPicker2 OpUsers
		{
			get { return opUsers; }
		}

		public bool ShowBlack
		{
			get { return checkBoxShowBlack.Checked; }
		}

		public bool ShowWhite
		{
			get { return checkBoxShowWhite.Checked; }
		}

		public FrmFirmBalance()
		{
			InitializeComponent();
			InitOnLoad();
		}
				
		public FrmFirmBalance(FirmBalance balance, DateTime startDate)
			: this()
		{
			dtFinish.Value = startDate;
			dtStart.Value = startDate.AddMonths(-6);
			opFirms.SelectObject(balance.FirmID);
			opFirms_ObjectSelected(Firm.GetFirmById(balance.FirmID));
		}

		protected virtual void InitOnLoad()
		{
			dtStart.Value = DateTime.Today.AddMonths(-6);
			dtFinish.Value = DateTime.Today;
			opUsers.IsCreateNewAllowed = false;
			opFirms.IsCreateNewAllowed = false;
			grdAgency.ObjectChecked += grdAgency_ObjectChecked;
			grdPayment.ObjectChecked += grdPayment_ObjectChecked;
			imageList1.Images.Add("Refresh", Globals.GetImage(Constants.ActionsImages.Refresh));
			tbRefresh.ImageKey = "Refresh";
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmFirmBalance));
			this.panel2 = new System.Windows.Forms.Panel();
			this.grdAction = new FogSoft.WinForm.Controls.SmartGrid();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this.panel1 = new System.Windows.Forms.Panel();
			this.checkBoxShowBlack = new System.Windows.Forms.CheckBox();
			this.checkBoxShowWhite = new System.Windows.Forms.CheckBox();
			this.label4 = new System.Windows.Forms.Label();
			this.opUsers = new FogSoft.WinForm.Controls.ObjectPicker2();
			this.opFirms = new FogSoft.WinForm.Controls.ObjectPicker2();
			this.dtFinish = new System.Windows.Forms.DateTimePicker();
			this.dtStart = new System.Windows.Forms.DateTimePicker();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.splitter2 = new System.Windows.Forms.Splitter();
			this.grdPayment = new FogSoft.WinForm.Controls.SmartGrid();
			this.grdAgency = new FogSoft.WinForm.Controls.SmartGrid();
			this.lstResult = new System.Windows.Forms.ListBox();
			this.splitter3 = new System.Windows.Forms.Splitter();
			this.tlbJournal = new System.Windows.Forms.ToolBar();
			this.tbRefresh = new System.Windows.Forms.ToolBarButton();
			this.panel2.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel2
			// 
			this.panel2.Controls.Add(this.grdAction);
			this.panel2.Controls.Add(this.splitter1);
			this.panel2.Controls.Add(this.panel1);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Left;
			this.panel2.Location = new System.Drawing.Point(0, 26);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(384, 503);
			this.panel2.TabIndex = 11;
			// 
			// grdAction
			// 
			this.grdAction.Caption = "Акции";
			this.grdAction.CaptionVisible = true;
			this.grdAction.CheckBoxes = false;
			this.grdAction.DataSource = null;
			this.grdAction.DependantGrid = null;
			this.grdAction.Dock = System.Windows.Forms.DockStyle.Fill;
			this.grdAction.Entity = null;
			this.grdAction.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.grdAction.InterfaceObject = FogSoft.WinForm.InterfaceObjects.SimpleJournal;
			this.grdAction.Location = new System.Drawing.Point(0, 147);
			this.grdAction.MenuEnabled = true;
			this.grdAction.Name = "grdAction";
			this.grdAction.QuickSearchVisible = false;
			this.grdAction.SelectedObject = null;
			this.grdAction.Size = new System.Drawing.Size(384, 356);
			this.grdAction.TabIndex = 14;
			// 
			// splitter1
			// 
			this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
			this.splitter1.Location = new System.Drawing.Point(0, 144);
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size(384, 3);
			this.splitter1.TabIndex = 13;
			this.splitter1.TabStop = false;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.checkBoxShowBlack);
			this.panel1.Controls.Add(this.checkBoxShowWhite);
			this.panel1.Controls.Add(this.label4);
			this.panel1.Controls.Add(this.opUsers);
			this.panel1.Controls.Add(this.opFirms);
			this.panel1.Controls.Add(this.dtFinish);
			this.panel1.Controls.Add(this.dtStart);
			this.panel1.Controls.Add(this.label3);
			this.panel1.Controls.Add(this.label2);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(384, 144);
			this.panel1.TabIndex = 11;
			// 
			// checkBoxShowBlack
			// 
			this.checkBoxShowBlack.AutoSize = true;
			this.checkBoxShowBlack.Checked = true;
			this.checkBoxShowBlack.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxShowBlack.Location = new System.Drawing.Point(0, 121);
			this.checkBoxShowBlack.Name = "checkBoxShowBlack";
			this.checkBoxShowBlack.Size = new System.Drawing.Size(127, 17);
			this.checkBoxShowBlack.TabIndex = 19;
			this.checkBoxShowBlack.Text = "Показывать без оплаты";
			this.checkBoxShowBlack.UseVisualStyleBackColor = true;
			this.checkBoxShowBlack.CheckedChanged += new System.EventHandler(this.checkBoxShowBlack_CheckedChanged);
			// 
			// checkBoxShowWhite
			// 
			this.checkBoxShowWhite.AutoSize = true;
			this.checkBoxShowWhite.Checked = true;
			this.checkBoxShowWhite.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxShowWhite.Location = new System.Drawing.Point(0, 99);
			this.checkBoxShowWhite.Name = "checkBoxShowWhite";
			this.checkBoxShowWhite.Size = new System.Drawing.Size(127, 17);
			this.checkBoxShowWhite.TabIndex = 18;
			this.checkBoxShowWhite.Text = "Показывать с оплатой";
			this.checkBoxShowWhite.UseVisualStyleBackColor = true;
			this.checkBoxShowWhite.CheckedChanged += new System.EventHandler(this.checkBoxShowWhite_CheckedChanged);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label4.Location = new System.Drawing.Point(0, 72);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(64, 13);
			this.label4.TabIndex = 17;
			this.label4.Text = "Менеджер:";
			// 
			// opUsers
			// 
			this.opUsers.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.opUsers.Enabled = false;
			this.opUsers.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.opUsers.Location = new System.Drawing.Point(144, 72);
			this.opUsers.Name = "opUsers";
			this.opUsers.Size = new System.Drawing.Size(240, 21);
			this.opUsers.TabIndex = 16;
			this.opUsers.ObjectSelected += new FogSoft.WinForm.ObjectDelegate(this.opUsers_ObjectSelected);
			this.opUsers.SelectionCleared += new FogSoft.WinForm.EmptyDelegate(this.opUsers_SelectionCleared);
			// 
			// opFirms
			// 
			this.opFirms.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.opFirms.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.opFirms.Location = new System.Drawing.Point(144, 48);
			this.opFirms.Name = "opFirms";
			this.opFirms.Size = new System.Drawing.Size(240, 21);
			this.opFirms.TabIndex = 14;
			this.opFirms.ObjectSelected += new FogSoft.WinForm.ObjectDelegate(this.opFirms_ObjectSelected);
			this.opFirms.SelectionCleared += new FogSoft.WinForm.EmptyDelegate(this.opFirms_SelectionCleared);
			// 
			// dtFinish
			// 
			this.dtFinish.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.dtFinish.CalendarFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.dtFinish.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.dtFinish.Location = new System.Drawing.Point(144, 24);
			this.dtFinish.Name = "dtFinish";
			this.dtFinish.Size = new System.Drawing.Size(240, 21);
			this.dtFinish.TabIndex = 13;
			this.dtFinish.Value = new System.DateTime(2005, 2, 26, 11, 14, 13, 687);
			this.dtFinish.TextChanged += new System.EventHandler(this.filter_Changed);
			// 
			// dtStart
			// 
			this.dtStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.dtStart.CalendarFont = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.dtStart.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.dtStart.Location = new System.Drawing.Point(144, 0);
			this.dtStart.Name = "dtStart";
			this.dtStart.Size = new System.Drawing.Size(240, 21);
			this.dtStart.TabIndex = 12;
			this.dtStart.Value = new System.DateTime(2005, 2, 26, 11, 14, 13, 718);
			this.dtStart.TextChanged += new System.EventHandler(this.filter_Changed);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label3.Location = new System.Drawing.Point(0, 48);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(93, 13);
			this.label3.TabIndex = 11;
			this.label3.Text = "Фирма-заказчик:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label2.Location = new System.Drawing.Point(0, 24);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(124, 13);
			this.label2.TabIndex = 10;
			this.label2.Text = "Окончание интервала:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label1.Location = new System.Drawing.Point(0, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(105, 13);
			this.label1.TabIndex = 9;
			this.label1.Text = "Начало интервала:";
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
			// 
			// splitter2
			// 
			this.splitter2.Location = new System.Drawing.Point(384, 26);
			this.splitter2.Name = "splitter2";
			this.splitter2.Size = new System.Drawing.Size(3, 503);
			this.splitter2.TabIndex = 16;
			this.splitter2.TabStop = false;
			// 
			// grdPayment
			// 
			this.grdPayment.Caption = "Платежи";
			this.grdPayment.CaptionVisible = true;
			this.grdPayment.CheckBoxes = false;
			this.grdPayment.Cursor = System.Windows.Forms.Cursors.Default;
			this.grdPayment.DataSource = null;
			this.grdPayment.DependantGrid = null;
			this.grdPayment.Dock = System.Windows.Forms.DockStyle.Fill;
			this.grdPayment.Entity = null;
			this.grdPayment.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.grdPayment.InterfaceObject = FogSoft.WinForm.InterfaceObjects.SimpleJournal;
			this.grdPayment.Location = new System.Drawing.Point(387, 298);
			this.grdPayment.MenuEnabled = false;
			this.grdPayment.Name = "grdPayment";
			this.grdPayment.QuickSearchVisible = false;
			this.grdPayment.SelectedObject = null;
			this.grdPayment.Size = new System.Drawing.Size(389, 231);
			this.grdPayment.TabIndex = 19;
			// 
			// grdAgency
			// 
			this.grdAgency.Caption = "Агентство";
			this.grdAgency.CaptionVisible = true;
			this.grdAgency.CheckBoxes = true;
			this.grdAgency.Cursor = System.Windows.Forms.Cursors.Default;
			this.grdAgency.DataSource = null;
			this.grdAgency.DependantGrid = null;
			this.grdAgency.Dock = System.Windows.Forms.DockStyle.Top;
			this.grdAgency.Entity = null;
			this.grdAgency.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.grdAgency.InterfaceObject = FogSoft.WinForm.InterfaceObjects.FakeModule;
			this.grdAgency.Location = new System.Drawing.Point(387, 122);
			this.grdAgency.MenuEnabled = false;
			this.grdAgency.Name = "grdAgency";
			this.grdAgency.QuickSearchVisible = false;
			this.grdAgency.SelectedObject = null;
			this.grdAgency.Size = new System.Drawing.Size(389, 176);
			this.grdAgency.TabIndex = 18;
			// 
			// lstResult
			// 
			this.lstResult.Dock = System.Windows.Forms.DockStyle.Top;
			this.lstResult.IntegralHeight = false;
			this.lstResult.Location = new System.Drawing.Point(387, 26);
			this.lstResult.Name = "lstResult";
			this.lstResult.Size = new System.Drawing.Size(389, 96);
			this.lstResult.TabIndex = 17;
			// 
			// splitter3
			// 
			this.splitter3.Dock = System.Windows.Forms.DockStyle.Top;
			this.splitter3.Location = new System.Drawing.Point(387, 298);
			this.splitter3.Name = "splitter3";
			this.splitter3.Size = new System.Drawing.Size(389, 3);
			this.splitter3.TabIndex = 20;
			this.splitter3.TabStop = false;
			// 
			// tlbJournal
			// 
			this.tlbJournal.Appearance = System.Windows.Forms.ToolBarAppearance.Flat;
			this.tlbJournal.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
            this.tbRefresh});
			this.tlbJournal.Divider = false;
			this.tlbJournal.DropDownArrows = true;
			this.tlbJournal.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.tlbJournal.ImageList = this.imageList1;
			this.tlbJournal.Location = new System.Drawing.Point(0, 0);
			this.tlbJournal.Name = "tlbJournal";
			this.tlbJournal.ShowToolTips = true;
			this.tlbJournal.Size = new System.Drawing.Size(776, 26);
			this.tlbJournal.TabIndex = 21;
			this.tlbJournal.TextAlign = System.Windows.Forms.ToolBarTextAlign.Right;
			this.tlbJournal.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(this.tlbJournal_ButtonClick);
			// 
			// tbRefresh
			// 
			this.tbRefresh.Enabled = false;
			this.tbRefresh.ImageIndex = 3;
			this.tbRefresh.Name = "tbRefresh";
			this.tbRefresh.Tag = "REFRESH";
			this.tbRefresh.Text = "Обновить";
			this.tbRefresh.ToolTipText = "Обновить журнал";
			// 
			// FrmFirmBalance
			// 
			this.ClientSize = new System.Drawing.Size(776, 529);
			this.Controls.Add(this.splitter3);
			this.Controls.Add(this.grdPayment);
			this.Controls.Add(this.grdAgency);
			this.Controls.Add(this.lstResult);
			this.Controls.Add(this.splitter2);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.tlbJournal);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Name = "FrmFirmBalance";
			this.Text = "Баланс для фирмы";
			this.panel2.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private void grdAgency_ObjectChecked(PresentationObject presentationObject, bool state)
		{
			ClearData();
		}

		private void grdPayment_ObjectChecked(PresentationObject presentationObject, bool state)
		{
			ClearData();
		}

		private void opUsers_SelectionCleared()
		{
			ClearData();
		}

		private void opUsers_ObjectSelected(PresentationObject presentationObject)
		{
			ClearData();
		}

		private void filter_Changed(object sender, EventArgs e)
		{
			ClearData();
		}

		private void ClearData()
		{
			grdPayment.Clear();
			grdAction.Clear();
			lstResult.Items.Clear();
		}

		private void tlbJournal_ButtonClick(object sender, ToolBarButtonClickEventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				lstResult.Items.Clear();
				decimal balanceOnStartOfInterval = RefreshBalanceOnStartOfInterval();
				string text = string.Format("На начало интервала: {0}", balanceOnStartOfInterval.ToString("c"));
				lstResult.Items.Add(text);

				decimal totalSumPayment = RefreshPaymentInfo(grdPayment);
				text = string.Format("Платежей за период: {0}", totalSumPayment.ToString("c"));
				lstResult.Items.Add(text);

				decimal totalSumAction = RefreshActionInfo(grdAction);
				text = string.Format("Акций за период: {0}", totalSumAction.ToString("c"));
				lstResult.Items.Add(text);

				text = string.Format("На окончание интервала: {0}",
				                     (balanceOnStartOfInterval + totalSumPayment - totalSumAction).ToString("c"));
				lstResult.Items.Add(text);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			//finally{this.Cursor = Cursors.Default;}
		}

		protected abstract decimal RefreshBalanceOnStartOfInterval();

		protected abstract decimal RefreshActionInfo(SmartGrid grid);

		protected abstract decimal RefreshPaymentInfo(SmartGrid grid);
		
		protected DateTime DateStart
		{
			get { return dtStart.Value.Date; }
		}

		protected DateTime DateFinish
		{
			get { return dtFinish.Value.Date; }
		}

		protected object FirmID
		{
			get { return opFirms.SelectedObject.IDs[0]; }
		}

		protected object UserID
		{
			get { return (opUsers.SelectedObject == null) ? null : opUsers.SelectedObject.IDs[0]; }
		}

		private void opFirms_SelectionCleared()
		{
			tbRefresh.Enabled = false;
			ClearData();
			opUsers.ClearSelected();
			OpUsers.SetDataSource(EntityManager.GetEntity((int)Entities.User), null);
			opUsers.Enabled = false;
		}

		private void opFirms_ObjectSelected(PresentationObject presentationObject)
		{
			tbRefresh.Enabled = presentationObject != null;
			ClearData();
			opUsers.ClearSelected();
			OpUsers.SetDataSource(EntityManager.GetEntity((int)Entities.User), ReloadUsers(presentationObject));
			opUsers.Enabled = presentationObject != null;
		}

		protected abstract DataTable ReloadUsers(PresentationObject firm);

		protected string AgenciesIDString
		{
			get
			{
				StringBuilder builder = new StringBuilder();

				foreach (PresentationObject presentationObject in grdAgency.Added2Checked)
					builder.Append(string.Format("{0},", int.Parse(presentationObject.IDs[0].ToString())));
				return builder.ToString();
			}
		}

		private void checkBoxShowWhite_CheckedChanged(object sender, EventArgs e)
		{
			ClearData();
		}

		private void checkBoxShowBlack_CheckedChanged(object sender, EventArgs e)
		{
			ClearData();
		}
	}
}