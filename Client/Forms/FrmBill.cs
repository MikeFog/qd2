using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;

namespace Merlin.Forms
{
	public class FrmBill : Form
	{
		private Label label1;
		private Label label2;
		private DateTimePicker dtDate;
		private Button btnCancel;
		private Button btnOk;
		private TextBox txtNumber;
		private PresentationObject bill;
		private readonly Entity entityBill;
		private readonly Dictionary<string, object> parameters;
		private int currentYear;

		public FrmBill()
		{
			InitializeComponent();
		}

		public FrmBill(Entity entityBill, DateTime billDate, Dictionary<string, object> parameters)
			: this()
		{
			this.entityBill = entityBill;
			this.parameters = parameters;
			currentYear = dtDate.Value.Year;
			dtDate.Value = billDate;

			if (parameters.ContainsKey(TableColumns.Bill.BillNo))
				txtNumber.Text = parameters[TableColumns.Bill.BillNo].ToString();
			else
				GetBillNo();
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
			this.txtNumber = new System.Windows.Forms.TextBox();
			this.dtDate = new System.Windows.Forms.DateTimePicker();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(8, 10);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(74, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Номер счёта:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(8, 34);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(37, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Дата:";
			// 
			// txtNumber
			// 
			this.txtNumber.Location = new System.Drawing.Point(96, 8);
			this.txtNumber.Name = "txtNumber";
			this.txtNumber.ReadOnly = true;
			this.txtNumber.Size = new System.Drawing.Size(240, 21);
			this.txtNumber.TabIndex = 2;
			// 
			// dtDate
			// 
			this.dtDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
			this.dtDate.Location = new System.Drawing.Point(96, 32);
			this.dtDate.Name = "dtDate";
			this.dtDate.Size = new System.Drawing.Size(240, 21);
			this.dtDate.TabIndex = 3;
			this.dtDate.ValueChanged += new System.EventHandler(this.dtDate_ValueChanged);
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnCancel.Font =
				new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point,
				                        ((byte) (204)));
			this.btnCancel.Location = new System.Drawing.Point(176, 72);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(83, 22);
			this.btnCancel.TabIndex = 15;
			this.btnCancel.Text = "Отмена";
			// 
			// btnOk
			// 
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnOk.Font =
				new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point,
				                        ((byte) (204)));
			this.btnOk.Location = new System.Drawing.Point(88, 72);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(84, 22);
			this.btnOk.TabIndex = 14;
			this.btnOk.Text = "Ок";
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// FrmBill
			// 
			this.AcceptButton = this.btnOk;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(346, 105);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.dtDate);
			this.Controls.Add(this.txtNumber);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Font =
				new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point,
				                        ((byte) (204)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FrmBill";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Дата и номер счёта";
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				parameters[TableColumns.Bill.BillNo] = txtNumber.Text;
				parameters[TableColumns.Bill.BillDate] = dtDate.Value.Date;
				bill = entityBill.CreateObject(parameters);
				bill.Update();
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

		private void dtDate_ValueChanged(object sender, EventArgs e)
		{
			try
			{
				//Application.DoEvents();
				if (currentYear != dtDate.Value.Year)
				{
					currentYear = dtDate.Value.Year;
					GetBillNo();
				}
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

		// Get next bill number for selected Year
		private void GetBillNo()
		{
			Application.DoEvents();
			Cursor.Current = Cursors.WaitCursor;

			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				entityBill, InterfaceObjects.FakeModule, Constants.Actions.LoadNo);

			procParameters[Agency.ParamNames.AgencyId] = parameters[Agency.ParamNames.AgencyId];
			procParameters["year"] = currentYear;
			DataAccessor.DoAction(procParameters);

			txtNumber.Text = procParameters["nextValue"].ToString();
		}

		public PresentationObject Bill
		{
			get { return bill; }
		}
	}
}