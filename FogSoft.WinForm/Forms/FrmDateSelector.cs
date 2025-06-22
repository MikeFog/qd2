using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm
{
	public class FrmDateSelector : Form
	{
		public enum SelectorMode
		{
			SelectOne,
			SelectRange
		}

		private DateTime startDate, finishDate;
		private SelectorMode mode;

		#region Controls ----------------------------------------

		private IContainer components = null;
		private Label label1;
		private Label label2;
		private DateTimePicker dtStartDate;
		private DateTimePicker dtFinishDate;
		private Button btnCancel;
		private Button btnOk;

		#endregion

		#region Constructors ----------------------------------

		public FrmDateSelector()
		{
			InitializeComponent();
		}

		public FrmDateSelector(string caption)
			: this()
		{
			Text = caption;
			mode = SelectorMode.SelectRange;
		}

		public FrmDateSelector(DateTime startDate, DateTime finishDate, string caption)
			: this(caption)
		{
			this.startDate = startDate;
			this.finishDate = finishDate;
			dtStartDate.MinDate = dtStartDate.Value = startDate;
			dtFinishDate.MaxDate = dtFinishDate.Value = finishDate;
		}

		public FrmDateSelector(string caption, DateTime minDate, DateTime maxDate) : this(caption)
		{
			dtStartDate.MinDate = minDate;
			dtStartDate.Value = minDate.AddDays(10);
			dtStartDate.MaxDate = maxDate;
		}

        #endregion

        protected override void Dispose(bool disposing)
		{
			if(disposing)
				if(components != null) components.Dispose();
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
            this.dtStartDate = new System.Windows.Forms.DateTimePicker();
            this.dtFinishDate = new System.Windows.Forms.DateTimePicker();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(114, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "Дата начала:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(147, 25);
            this.label2.TabIndex = 1;
            this.label2.Text = "Дата окончания:";
            // 
            // dtStartDate
            // 
            this.dtStartDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtStartDate.Location = new System.Drawing.Point(160, 16);
            this.dtStartDate.Name = "dtStartDate";
            this.dtStartDate.Size = new System.Drawing.Size(200, 31);
            this.dtStartDate.TabIndex = 2;
            // 
            // dtFinishDate
            // 
            this.dtFinishDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtFinishDate.Location = new System.Drawing.Point(160, 40);
            this.dtFinishDate.Name = "dtFinishDate";
            this.dtFinishDate.Size = new System.Drawing.Size(200, 31);
            this.dtFinishDate.TabIndex = 3;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(189, 81);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 22);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Отмена";
            // 
            // btnOk
            // 
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOk.Location = new System.Drawing.Point(101, 81);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(80, 22);
            this.btnOk.TabIndex = 4;
            this.btnOk.Text = "Ок";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // FrmDateSelector
            // 
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(370, 110);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.dtFinishDate);
            this.Controls.Add(this.dtStartDate);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "FrmDateSelector";
            this.ShowInTaskbar = false;
            this.Text = "Даты начала и окончания";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private void btnOk_Click(object sender, EventArgs e)
		{
            if (dtStartDate.Value.Date <= dtFinishDate.Value.Date || mode == SelectorMode.SelectOne)
			{
				startDate = dtStartDate.Value.Date;
				finishDate = dtFinishDate.Value.Date;
				DialogResult = DialogResult.OK;
			}
			else
			{
				ErrorManager.ShowExclamation(MessageNames.StartFinishDateError.ToString());
			}
		}

		public SelectorMode Mode
		{
			get { return mode; }
			set
			{
				if(mode != value)
				{
					mode = value;
					label2.Visible = dtFinishDate.Visible = (mode == SelectorMode.SelectRange);
					label1.Text = (mode == SelectorMode.SelectOne) ? "Дата:" : "Дата начала:";
				}
			}
		}

		public DateTime StartDate
		{
			get { return startDate; }
			set { startDate = value.Date; }
		}

		public DateTime FinishDate
		{
			get { return finishDate; }
			set { finishDate = value.Date; }
		}
	}
}