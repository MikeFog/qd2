using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Windows.Forms;

using Debug = System.Diagnostics.Debug;
namespace MetadataEditor
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class MainForm : Form
	{
		#region Members
		private TabControl tabControl1;
		private TabPage tabPage1;
		private GroupBox groupBox1;
		private RadioButton rbWizardStep;
		private RadioButton rbEntity;
		private ComboBox cmbSource;
		private MetadataEditor.IdName idNameCombo;
		private System.Windows.Forms.RichTextBox tbPassport;
		private System.Windows.Forms.Button btPassportSave;
		private System.Windows.Forms.Panel panel2;
		private Label statusLabel;
		private RadioButton rbPassports;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;
		#endregion // Members

		public MainForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			SelectSource();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.panel2 = new System.Windows.Forms.Panel();
			this.statusLabel = new System.Windows.Forms.Label();
			this.btPassportSave = new System.Windows.Forms.Button();
			this.tbPassport = new System.Windows.Forms.RichTextBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.cmbSource = new System.Windows.Forms.ComboBox();
			this.idNameCombo = new MetadataEditor.IdName();
			this.rbWizardStep = new System.Windows.Forms.RadioButton();
			this.rbEntity = new System.Windows.Forms.RadioButton();
			this.rbPassports = new System.Windows.Forms.RadioButton();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.idNameCombo)).BeginInit();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(736, 623);
			this.tabControl1.TabIndex = 0;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.panel2);
			this.tabPage1.Controls.Add(this.tbPassport);
			this.tabPage1.Controls.Add(this.groupBox1);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Size = new System.Drawing.Size(728, 597);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Passport";
			// 
			// panel2
			// 
			this.panel2.Controls.Add(this.statusLabel);
			this.panel2.Controls.Add(this.btPassportSave);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel2.Location = new System.Drawing.Point(0, 563);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(728, 34);
			this.panel2.TabIndex = 4;
			// 
			// statusLabel
			// 
			this.statusLabel.AutoSize = true;
			this.statusLabel.Location = new System.Drawing.Point(7, 7);
			this.statusLabel.Name = "statusLabel";
			this.statusLabel.Size = new System.Drawing.Size(35, 13);
			this.statusLabel.TabIndex = 5;
			this.statusLabel.Text = "label1";
			// 
			// btPassportSave
			// 
			this.btPassportSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btPassportSave.Location = new System.Drawing.Point(658, 7);
			this.btPassportSave.Name = "btPassportSave";
			this.btPassportSave.Size = new System.Drawing.Size(63, 20);
			this.btPassportSave.TabIndex = 4;
			this.btPassportSave.Text = "Save";
			this.btPassportSave.Click += new System.EventHandler(this.btPassportSave_Click);
			// 
			// tbPassport
			// 
			this.tbPassport.AcceptsTab = true;
			this.tbPassport.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tbPassport.DetectUrls = false;
			this.tbPassport.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.tbPassport.Location = new System.Drawing.Point(7, 55);
			this.tbPassport.Name = "tbPassport";
			this.tbPassport.Size = new System.Drawing.Size(717, 509);
			this.tbPassport.TabIndex = 3;
			this.tbPassport.Text = "";
			this.tbPassport.WordWrap = false;
			this.tbPassport.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbPassport_KeyDown);
			this.tbPassport.TextChanged += new System.EventHandler(this.tbPassport_TextChanged);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.rbPassports);
			this.groupBox1.Controls.Add(this.cmbSource);
			this.groupBox1.Controls.Add(this.rbWizardStep);
			this.groupBox1.Controls.Add(this.rbEntity);
			this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
			this.groupBox1.Location = new System.Drawing.Point(0, 0);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(728, 49);
			this.groupBox1.TabIndex = 1;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Select source";
			// 
			// cmbSource
			// 
			this.cmbSource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.cmbSource.DataSource = this.idNameCombo.Source;
			this.cmbSource.DisplayMember = "displayName";
			this.cmbSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbSource.Location = new System.Drawing.Point(308, 21);
			this.cmbSource.MaxDropDownItems = 16;
			this.cmbSource.Name = "cmbSource";
			this.cmbSource.Size = new System.Drawing.Size(408, 21);
			this.cmbSource.TabIndex = 2;
			this.cmbSource.ValueMember = "id";
			this.cmbSource.SelectedIndexChanged += new System.EventHandler(this.cmbSource_SelectedIndexChanged);
			// 
			// idNameCombo
			// 
			this.idNameCombo.DataSetName = "IdName";
			this.idNameCombo.Locale = new System.Globalization.CultureInfo("en-US");
			this.idNameCombo.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
			// 
			// rbWizardStep
			// 
			this.rbWizardStep.Location = new System.Drawing.Point(80, 21);
			this.rbWizardStep.Name = "rbWizardStep";
			this.rbWizardStep.Size = new System.Drawing.Size(87, 21);
			this.rbWizardStep.TabIndex = 1;
			this.rbWizardStep.Text = "Filter";
			this.rbWizardStep.CheckedChanged += new System.EventHandler(this.rbWizardStep_CheckedChanged);
			// 
			// rbEntity
			// 
			this.rbEntity.Checked = true;
			this.rbEntity.Location = new System.Drawing.Point(13, 21);
			this.rbEntity.Name = "rbEntity";
			this.rbEntity.Size = new System.Drawing.Size(60, 21);
			this.rbEntity.TabIndex = 0;
			this.rbEntity.TabStop = true;
			this.rbEntity.Text = "Entity";
			this.rbEntity.CheckedChanged += new System.EventHandler(this.rbEntity_CheckedChanged);
			// 
			// rbPassports
			// 
			this.rbPassports.AutoSize = true;
			this.rbPassports.Location = new System.Drawing.Point(139, 23);
			this.rbPassports.Name = "rbPassports";
			this.rbPassports.Size = new System.Drawing.Size(67, 17);
			this.rbPassports.TabIndex = 3;
			this.rbPassports.TabStop = true;
			this.rbPassports.Text = "Passport";
			this.rbPassports.UseVisualStyleBackColor = true;
			this.rbPassports.CheckedChanged += new System.EventHandler(this.rbPassports_CheckedChanged);
			// 
			// MainForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
			this.ClientSize = new System.Drawing.Size(736, 623);
			this.Controls.Add(this.tabControl1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.MinimumSize = new System.Drawing.Size(744, 650);
			this.Name = "MainForm";
			this.Text = "Metadata Editor";
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.idNameCombo)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new MainForm());
		}

		private string FieldId
		{
			get{ return rbPassports.Checked ? "codeName" : "entityID"; }
		}
		private string FieldNameAddition
		{
			get { return string.Format("CASE WHEN {0} IS NULL THEN '*' ELSE '' END", GetPassportField()); }
		}
		private string Table
		{
			get{ return rbPassports.Checked ? "iPassport" : "iEntity"; }
		}
		private string GetPassportField()
		{
			return rbEntity.Checked ? "passport" : rbPassports.Checked ? "passport" : "filter";
		}
		private void Clear()
		{
			tbPassport.Text = "";
			cmbSource.DataSource = null;
			idNameCombo.Clear();
		}
		private void SetDataSource()
		{
			cmbSource.DisplayMember = "displayName";
			cmbSource.ValueMember = "id";
			cmbSource.DataSource = idNameCombo.Source;
		}
		private string GetID()
		{
			DataRowView sourceRow = cmbSource.SelectedItem as DataRowView;
			if( sourceRow == null )
			{
				return "";
			}
			IdName.SourceRow source = sourceRow.Row as IdName.SourceRow;
			if( source == null )
			{
				Debug.Assert(false);
				return "";
			}
			return source.id.ToString();
		}
		private void GetPassport()
		{
			string id = GetID();
			if( id.Length == 0 )
				return;
			string select = "SELECT " + GetPassportField() + " AS passport FROM " + Table
				+ " WHERE " + FieldId + " = '" + id + "'";
			using(SqlConnection connection = new SqlConnection(Settings.ConnectionString))
			{
				SqlCommand command = new SqlCommand(select, connection);
				connection.Open();
				try
				{
					string text = command.ExecuteScalar() as string;
					tbPassport.Text = text;
				}
				catch(SqlException e)
				{
					MessageBox.Show(e.Errors[0].Message);
				}
				finally
				{
					command.Dispose();
				}
			}
		}
		private void SavePassport()
		{
			string id = GetID();
			if( id.Length == 0 )
				return;
			string update = "UPDATE " + Table + " SET " + GetPassportField() + " = @passport "
				+ " WHERE " + FieldId + " = '" + id + "'";
			using(SqlConnection connection = new SqlConnection(Settings.ConnectionString))
			{
				SqlCommand command = new SqlCommand(update, connection);
				command.Parameters.Add("@passport", SqlDbType.Text).Value = tbPassport.Text;
				connection.Open();
				try
				{
					command.ExecuteNonQuery();
					statusLabel.Text = "Изменения сохранены.";
					Thread thread = new Thread(ClearStatusLabel) {Priority = ThreadPriority.Lowest};
					thread.Start();
				}
				catch(SqlException e)
				{
					MessageBox.Show(e.Errors[0].Message);
				}
				finally
				{
					command.Dispose();
				}
			}
			Application.DoEvents();
		}

		public delegate void VoidCallback();

		private void ClearStatusLabel()
		{
			Application.DoEvents();
			Thread.Sleep(2000);
			Invoke(new VoidCallback(DoClearStatusLabel));
		}

		private void DoClearStatusLabel()
		{
			statusLabel.Text = string.Empty;
		}

		private void SelectSource()
		{
			Clear();

			if (rbPassports.Checked)
				SelectPassports();
			else
				SelectEntities();

			SetDataSource();
		}

		private void SelectEntities()
		{
			string select = "SELECT " + FieldId + " AS id, str([entityID], 4, 0) + ' - ' + name + " + FieldNameAddition
			                + " AS displayName FROM " + Table + " ORDER BY id, " + FieldNameAddition;
			using(SqlConnection connection = new SqlConnection(Settings.ConnectionString))
			{
				SqlDataAdapter adapter = new SqlDataAdapter(select, connection);
				adapter.TableMappings.Add("Table", "Source");
				try
				{
					adapter.Fill(idNameCombo);
				}
				catch(SqlException e)
				{
					MessageBox.Show(e.Errors[0].Message);
				}
			}
		}

		private void SelectPassports()
		{
			using (SqlConnection connection = new SqlConnection(Settings.ConnectionString))
			{
				SqlDataAdapter adapter = new SqlDataAdapter("SELECT codeName AS id, codeName AS displayName FROM iPassport ORDER BY codeName ", connection);
				adapter.TableMappings.Add("Table", "Source");
				try
				{
					adapter.Fill(idNameCombo);
				}
				catch (SqlException e)
				{
					MessageBox.Show(e.Errors[0].Message);
				}
			}
		}

		#region Handlers
		private void rbEntity_CheckedChanged(object sender, EventArgs e)
		{
			if(rbEntity.Checked)
				SelectSource();
		}

		private void rbWizardStep_CheckedChanged(object sender, EventArgs e)
		{
			if(rbWizardStep.Checked)
				SelectSource();
		}

		private void cmbSource_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			GetPassport();
			statusLabel.Text = "";
		}

		private void btPassportSave_Click(object sender, System.EventArgs e)
		{
			SavePassport();
		}
		#endregion // Handlers		

		private void tbPassport_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.S)
				SavePassport();
		}

		private void tbPassport_TextChanged(object sender, EventArgs e)
		{
			statusLabel.Text = "Текст изменён.";
		}

		private void rbPassports_CheckedChanged(object sender, EventArgs e)
		{
			if (rbPassports.Checked)
				SelectSource();
		}
	}
}
