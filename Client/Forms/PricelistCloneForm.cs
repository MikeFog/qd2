using System;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Classes;

namespace Merlin.Forms
{
	/// <summary>Параметры клонирования прайс-листа: даты нового прайс-листа и режим клонирования тарифов.</summary>
	public class PricelistCloneForm : Form
	{
		private TableLayoutPanel tableLayoutPanel1;
		private Label lblStartDate;
		private Label lblFinishDate;
		private DateTimePicker dtStartDate;
		private DateTimePicker dtFinishDate;
		private Label lblQuestion;
		private FlowLayoutPanel flpModes;
		private RadioButton rbExact;
		private RadioButton rbWithWindowChanges;
		private RadioButton rbHybrid;
		private FlowLayoutPanel flowLayoutPanel1;
		private Button btnOk;
		private Button btnCancel;

		public PricelistCloneForm()
		{
			InitializeComponent();
		}

		public DateTime StartDate { get; private set; }

		public DateTime FinishDate { get; private set; }

		public PricelistCloneMode SelectedMode
		{
			get
			{
				if (rbExact.Checked)
					return PricelistCloneMode.Exact;
				if (rbHybrid.Checked)
					return PricelistCloneMode.Hybrid;
				return PricelistCloneMode.WithWindowChanges;
			}
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if (dtStartDate.Value.Date > dtFinishDate.Value.Date)
			{
				ErrorManager.ShowExclamation(MessageNames.StartFinishDateError.ToString());
				return;
			}
			StartDate = dtStartDate.Value.Date;
			FinishDate = dtFinishDate.Value.Date;
			DialogResult = DialogResult.OK;
		}

		private RadioButton CreateModeButton(string name, string text, int tabIndex)
		{
			return new RadioButton
			{
				AutoSize = true,
				Margin = new Padding(3, 3, 3, 9),
				Name = name,
				TabIndex = tabIndex,
				Text = text,
				UseVisualStyleBackColor = true
			};
		}

		private void InitializeComponent()
		{
			this.tableLayoutPanel1 = new TableLayoutPanel();
			this.lblStartDate = new Label();
			this.lblFinishDate = new Label();
			this.dtStartDate = new DateTimePicker();
			this.dtFinishDate = new DateTimePicker();
			this.lblQuestion = new Label();
			this.flpModes = new FlowLayoutPanel();
			this.rbExact = CreateModeButton("rbExact",
				"Один в один — тарифы копируются без изменений.", 3);
			this.rbWithWindowChanges = CreateModeButton("rbWithWindowChanges",
				"С учётом правок предыдущих тарифов — цена, длительность и время берутся из последних 7 дней", 4);
			this.rbHybrid = CreateModeButton("rbHybrid",
				"Гибрид — модульные тарифы один в один, остальные с учётом правок окон.", 5);
			this.flowLayoutPanel1 = new FlowLayoutPanel();
			this.btnOk = new Button();
			this.btnCancel = new Button();
			this.tableLayoutPanel1.SuspendLayout();
			this.flpModes.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			//
			// lblStartDate
			//
			this.lblStartDate.Anchor = AnchorStyles.Left;
			this.lblStartDate.AutoSize = true;
			this.lblStartDate.Name = "lblStartDate";
			this.lblStartDate.Text = "Дата начала:";
			//
			// lblFinishDate
			//
			this.lblFinishDate.Anchor = AnchorStyles.Left;
			this.lblFinishDate.AutoSize = true;
			this.lblFinishDate.Name = "lblFinishDate";
			this.lblFinishDate.Text = "Дата окончания:";
			//
			// dtStartDate
			//
			this.dtStartDate.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.dtStartDate.Format = DateTimePickerFormat.Short;
			this.dtStartDate.Name = "dtStartDate";
			this.dtStartDate.TabIndex = 0;
			//
			// dtFinishDate
			//
			this.dtFinishDate.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.dtFinishDate.Format = DateTimePickerFormat.Short;
			this.dtFinishDate.Name = "dtFinishDate";
			this.dtFinishDate.TabIndex = 1;
			//
			// lblQuestion
			//
			this.lblQuestion.AutoSize = true;
			this.lblQuestion.Margin = new Padding(3, 12, 3, 9);
			this.lblQuestion.Name = "lblQuestion";
			this.lblQuestion.TabIndex = 2;
			this.lblQuestion.Text = "Как клонировать тарифы прайс-листа?";
			//
			// flpModes
			//
			this.flpModes.AutoSize = true;
			this.flpModes.Controls.Add(this.rbExact);
			this.flpModes.Controls.Add(this.rbWithWindowChanges);
			this.flpModes.Controls.Add(this.rbHybrid);
			this.flpModes.Dock = DockStyle.Fill;
			this.flpModes.FlowDirection = FlowDirection.TopDown;
			this.flpModes.Name = "flpModes";
			this.flpModes.WrapContents = false;
			this.rbWithWindowChanges.Checked = true;
			this.rbWithWindowChanges.TabStop = true;
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new Size(100, 33);
			this.btnCancel.TabIndex = 7;
			this.btnCancel.Text = "Отмена";
			this.btnCancel.UseVisualStyleBackColor = true;
			//
			// btnOk
			//
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new Size(100, 33);
			this.btnOk.TabIndex = 6;
			this.btnOk.Text = "Ок";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new EventHandler(this.btnOk_Click);
			//
			// flowLayoutPanel1
			//
			this.flowLayoutPanel1.AutoSize = true;
			this.flowLayoutPanel1.Controls.Add(this.btnCancel);
			this.flowLayoutPanel1.Controls.Add(this.btnOk);
			this.flowLayoutPanel1.Dock = DockStyle.Fill;
			this.flowLayoutPanel1.FlowDirection = FlowDirection.RightToLeft;
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			//
			// tableLayoutPanel1
			//
			this.tableLayoutPanel1.AutoSize = true;
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.lblStartDate, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.dtStartDate, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.lblFinishDate, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.dtFinishDate, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.lblQuestion, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.flpModes, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 4);
			this.tableLayoutPanel1.SetColumnSpan(this.lblQuestion, 2);
			this.tableLayoutPanel1.SetColumnSpan(this.flpModes, 2);
			this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel1, 2);
			this.tableLayoutPanel1.Dock = DockStyle.Fill;
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.Padding = new Padding(12);
			this.tableLayoutPanel1.RowCount = 5;
			for (int i = 0; i < 5; i++)
				this.tableLayoutPanel1.RowStyles.Add(new RowStyle());
			//
			// PricelistCloneForm
			//
			this.AcceptButton = this.btnOk;
			this.AutoSize = true;
			this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.tableLayoutPanel1);
			this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(204)));
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PricelistCloneForm";
			this.ShowInTaskbar = false;
			this.StartPosition = FormStartPosition.CenterParent;
			this.Text = "Клонирование прайс-листа";
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.flpModes.ResumeLayout(false);
			this.flpModes.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();
		}
	}
}
