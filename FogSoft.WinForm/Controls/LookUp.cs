using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm
{
	public class LookUp : UserControl, IDataControl
	{
		public event EventHandler SelectedItemChanged;

		#region Members --------------------------------------- 

		private ComboBox cmbLookup;
		private IContainer components;
		private readonly ArrayList comboData = new ArrayList();
		private Button btnCreateNew;
		private bool isNullable;
		private ToolTip ttCreateNew;
		private readonly Entity entity;
		private string columnWithID = Constants.Parameters.Id;
		private string columnWithName = Constants.Parameters.Name;

		#endregion

		#region Constructors ----------------------------------

		public LookUp()
		{
			InitializeComponent();
			ttCreateNew.SetToolTip(btnCreateNew, "Create new object");
		}

		public LookUp(Entity entity) : this()
		{
			this.entity = entity;
			btnCreateNew.Visible = entity.IsActionEnabled(Constants.EntityActions.AddNew, ViewType.Journal);
		}

		#endregion

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            this.cmbLookup = new System.Windows.Forms.ComboBox();
            this.btnCreateNew = new System.Windows.Forms.Button();
            this.ttCreateNew = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // cmbLookup
            // 
            this.cmbLookup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbLookup.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLookup.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cmbLookup.Location = new System.Drawing.Point(0, 0);
            this.cmbLookup.Name = "cmbLookup";
            this.cmbLookup.Size = new System.Drawing.Size(320, 23);
            this.cmbLookup.TabIndex = 0;
            this.cmbLookup.SelectionChangeCommitted += new System.EventHandler(this.cmbLookup_SelectedIndexChanged);
            // 
            // btnCreateNew
            // 
            this.btnCreateNew.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnCreateNew.Image = global::FogSoft.WinForm.Properties.Resources.NewItem;
            this.btnCreateNew.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCreateNew.Location = new System.Drawing.Point(320, 0);
            this.btnCreateNew.Name = "btnCreateNew";
            this.btnCreateNew.Size = new System.Drawing.Size(24, 32);
            this.btnCreateNew.TabIndex = 1;
            this.btnCreateNew.Visible = false;
            this.btnCreateNew.Click += new System.EventHandler(this.btnCreateNew_Click);
            // 
            // LookUp
            // 
            this.Controls.Add(this.cmbLookup);
            this.Controls.Add(this.btnCreateNew);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Name = "LookUp";
            this.Size = new System.Drawing.Size(344, 32);
            this.Resize += new System.EventHandler(this.LookUp_Resize);
            this.ResumeLayout(false);

		}

		#endregion

		private void LookUp_Resize(object sender, EventArgs e)
		{
			Height = cmbLookup.Bounds.Height;
		}

		/// <summary>
		/// Populates combo from DataView
		/// </summary>
		[Browsable(false)]
		public DataView DataSource
		{
			set
			{
				if(value == null)
					cmbLookup.DataSource = null;
				else
				{
					//this.cmbLookup.SelectedIndexChanged -= new System.EventHandler(this.cmbLookup_SelectedIndexChanged);
					DataView view = value;
					// if this isn't mandatory combo we have to add empty row
					// to allow user to leave this combo withour selected item
					cmbLookup.DisplayMember = columnWithName;
					cmbLookup.ValueMember = columnWithID;
					cmbLookup.DataSource = view;

					if(isNullable && view.Table.Rows.Count > 0)
					{
						// Just check that empty row hasn't already added
						if(view.Table.Rows[0][Constants.Parameters.Id] != DBNull.Value)
						{
							DataRow row = view.Table.NewRow();
							view.Table.Rows.InsertAt(row, 0);
						}
						cmbLookup.SelectedValue = DBNull.Value;
					}
					//this.cmbLookup.SelectedIndexChanged += new System.EventHandler(this.cmbLookup_SelectedIndexChanged);
				}
				FireSelectedIndexChanged();
			}
		}

		[Browsable(false)]
		public object SelectedValue
		{
			get
			{
				if(cmbLookup.DataSource != null)
					return cmbLookup.SelectedValue;
				return cmbLookup.SelectedIndex >= 0 ? comboData[cmbLookup.SelectedIndex] : null;
			}
			set
			{
                int currentIndex = cmbLookup.SelectedIndex;
                if (cmbLookup.DataSource != null && value != null)
					cmbLookup.SelectedValue = value;
				else
				{
					
					for(int i = 0; i < comboData.Count; i++)
						if(comboData[i].ToString() == ParseHelper.GetStringFromObject(value, null))
						{
							cmbLookup.SelectedIndex = i;
							break;
						}

                }
                if (currentIndex != cmbLookup.SelectedIndex)
                    FireSelectedIndexChanged();
            }
		}

		public void ClearItems()
		{
			cmbLookup.Items.Clear();
		}

		[Browsable(false)]
		public int SelectedIndex
		{
			get { return cmbLookup.SelectedIndex; }
			set { cmbLookup.SelectedIndex = value; }
		}

		public string ColumnWithID
		{
			set { columnWithID = value; }
		}

		public string ColumnWithName
		{
			set { columnWithName = value; }
		}

		public bool IsNullable
		{
			get { return isNullable; }
			set { isNullable = value; }
		}

		/// <summary>
		/// Return value for given column name from current row in the DataSource
		/// </summary>
		/// <returns></returns>
		public object GetValue(string name)
		{
			if(cmbLookup.SelectedIndex == -1) return null;
			
			DataView dv = (DataView)cmbLookup.DataSource;
			return dv[cmbLookup.SelectedIndex][name];			
		}

		/// <summary>
		/// Adds item to the combo. This method is used when combo is being populated item by item
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public void AddItem(string name, object value)
		{
			cmbLookup.Items.Add(name);
			comboData.Add(value);
		}

		/// <summary>
		/// Adds object to the DataView forcing it to be displayed in the Combo.
		/// Can work for the objects with Id only
		/// </summary>
		/// <param name="presentationObject"></param>
		/// <returns></returns>
		private void AddObject(PresentationObject presentationObject)
		{
			DataView view = (DataView) cmbLookup.DataSource;
			DataRow row = view.Table.NewRow();
			row[columnWithName] = presentationObject.Name;
			row[columnWithID] = presentationObject.IDs[0];
			view.Table.Rows.Add(row);
			SelectedValue = presentationObject.IDs[0];
		}

		public void FireSelectedIndexChanged()
		{
			cmbLookup_SelectedIndexChanged(null, null);
		}

		// Fire an event that selected item is changed
		private void cmbLookup_SelectedIndexChanged(object sender, EventArgs e)
		{
			if(SelectedItemChanged != null) SelectedItemChanged(this, e);
		}

		/// <summary>
		/// Show properties page to create new object
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnCreateNew_Click(object sender, EventArgs e)
		{
			try
			{
				PresentationObject presentationObject = entity.NewObject;
				if(presentationObject.ShowPassport(ParentForm))
				{
					// If new object has been created add it to the ComboBox
					// and this object will be selected one
					AddObject(presentationObject);
				}
			}
			catch(Exception ex)
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