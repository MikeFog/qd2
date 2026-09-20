using System;
using System.Drawing;
using System.Windows.Forms;
using Merlin.Classes;

namespace Merlin.Forms
{
	/// <summary>Выбор режима клонирования тарифов прайс-листа.</summary>
	public class PricelistCloneModeForm : Form
	{
		private TableLayoutPanel tableLayoutPanel1;
		private Label lblQuestion;
		private FlowLayoutPanel flpModes;
		private RadioButton rbExact;
		private RadioButton rbWithWindowChanges;
		private RadioButton rbWithoutModuleOnly;
		private FlowLayoutPanel flowLayoutPanel1;
		private Button btnOk;
		private Button btnCancel;

		public PricelistCloneModeForm()
		{
			InitializeComponent();
		}

		public PricelistCloneMode SelectedMode
		{
			get
			{
				if (rbExact.Checked)
					return PricelistCloneMode.Exact;
				if (rbWithoutModuleOnly.Checked)
					return PricelistCloneMode.WithoutModuleOnlyTariffs;
				return PricelistCloneMode.WithWindowChanges;
			}
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
			this.lblQuestion = new Label();
			this.flpModes = new FlowLayoutPanel();
			this.rbExact = CreateModeButton("rbExact",
				"Один в один — тарифы копируются без изменений.", 0);
			this.rbWithWindowChanges = CreateModeButton("rbWithWindowChanges",
				"С учётом правок окон — цена, длительность и время из последних окон.", 1);
			this.rbWithoutModuleOnly = CreateModeButton("rbWithoutModuleOnly",
				"С учётом правок окон, без тарифов «только для модулей».", 2);
			this.flowLayoutPanel1 = new FlowLayoutPanel();
			this.btnOk = new Button();
			this.btnCancel = new Button();
			this.tableLayoutPanel1.SuspendLayout();
			this.flpModes.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// lblQuestion
			// 
			this.lblQuestion.AutoSize = true;
			this.lblQuestion.Margin = new Padding(3, 3, 3, 9);
			this.lblQuestion.Name = "lblQuestion";
			this.lblQuestion.Text = "Как клонировать тарифы прайс-листа?";
			// 
			// flpModes
			// 
			this.flpModes.AutoSize = true;
			this.flpModes.Controls.Add(this.rbExact);
			this.flpModes.Controls.Add(this.rbWithWindowChanges);
			this.flpModes.Controls.Add(this.rbWithoutModuleOnly);
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
			this.btnCancel.TabIndex = 4;
			this.btnCancel.Text = "Отмена";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnOk
			// 
			this.btnOk.DialogResult = DialogResult.OK;
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new Size(100, 33);
			this.btnOk.TabIndex = 3;
			this.btnOk.Text = "Ок";
			this.btnOk.UseVisualStyleBackColor = true;
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
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.lblQuestion, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.flpModes, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 2);
			this.tableLayoutPanel1.Dock = DockStyle.Fill;
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.Padding = new Padding(12);
			this.tableLayoutPanel1.RowCount = 3;
			this.tableLayoutPanel1.RowStyles.Add(new RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new RowStyle());
			// 
			// PricelistCloneModeForm
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
			this.Name = "PricelistCloneModeForm";
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
