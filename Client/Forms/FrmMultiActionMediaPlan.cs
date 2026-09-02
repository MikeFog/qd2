using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using FogSoft.WinForm.Forms;

namespace Merlin.Forms
{
	// Ввод номеров рекламных акций для сводного «Графика размещения по
	// нескольким акциям». Раскладка — по образцу FrmDateSelector
	// (TableLayoutPanel + FlowLayoutPanel RightToLeft, кнопки 100x33).
	public class FrmMultiActionMediaPlan : Form
	{
		private IContainer components = null;
		private Label lblPrompt;
		private TextBox txtActionIds;
		private TableLayoutPanel tableLayoutPanel1;
		private FlowLayoutPanel flowLayoutPanel1;
		private Button btnOk;
		private Button btnCancel;

		private readonly List<int> _actionIds = new List<int>();

		public FrmMultiActionMediaPlan()
		{
			InitializeComponent();
		}

		/// <summary>Разобранные номера акций: без повторов, по возрастанию.</summary>
		public IList<int> ActionIds => _actionIds;

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null) components.Dispose();
			base.Dispose(disposing);
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			_actionIds.Clear();
			foreach (string token in txtActionIds.Text.Split(new[] { ',', ';', ' ', '\t', '\r', '\n' },
				StringSplitOptions.RemoveEmptyEntries))
			{
				if (int.TryParse(token.Trim(), out int id) && id > 0 && !_actionIds.Contains(id))
					_actionIds.Add(id);
			}

			if (_actionIds.Count == 0)
			{
				UserMessage.ShowExclamation("Введите хотя бы один номер рекламной акции.");
				return;
			}

			_actionIds.Sort();
			DialogResult = DialogResult.OK;
		}

		private void InitializeComponent()
		{
			this.lblPrompt = new System.Windows.Forms.Label();
			this.txtActionIds = new System.Windows.Forms.TextBox();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.tableLayoutPanel1.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			//
			// lblPrompt
			//
			this.lblPrompt.AutoSize = true;
			this.lblPrompt.Location = new System.Drawing.Point(3, 6);
			this.lblPrompt.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
			this.lblPrompt.Name = "lblPrompt";
			this.lblPrompt.TabIndex = 0;
			this.lblPrompt.Text = "Номера рекламных акций через запятую:";
			//
			// txtActionIds
			//
			this.txtActionIds.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtActionIds.Location = new System.Drawing.Point(3, 30);
			this.txtActionIds.Name = "txtActionIds";
			this.txtActionIds.Size = new System.Drawing.Size(414, 31);
			this.txtActionIds.TabIndex = 1;
			//
			// tableLayoutPanel1
			//
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.lblPrompt, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.txtActionIds, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 2);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(6);
			this.tableLayoutPanel1.RowCount = 3;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(432, 110);
			this.tableLayoutPanel1.TabIndex = 0;
			//
			// flowLayoutPanel1
			//
			this.flowLayoutPanel1.AutoSize = true;
			this.flowLayoutPanel1.Controls.Add(this.btnCancel);
			this.flowLayoutPanel1.Controls.Add(this.btnOk);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 67);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(414, 39);
			this.flowLayoutPanel1.TabIndex = 2;
			//
			// btnOk
			//
			this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(100, 33);
			this.btnOk.TabIndex = 0;
			this.btnOk.Text = "Ок";
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(100, 33);
			this.btnCancel.TabIndex = 1;
			this.btnCancel.Text = "Отмена";
			//
			// FrmMultiActionMediaPlan
			//
			this.AcceptButton = this.btnOk;
			this.AutoSize = true;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(432, 110);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Font = new System.Drawing.Font("Segoe UI Variable Text", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MinimumSize = new System.Drawing.Size(360, 0);
			this.Name = "FrmMultiActionMediaPlan";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "График размещения по нескольким акциям";
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.ResumeLayout(false);
		}
	}
}
