using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Classes;

namespace FogSoft.WinForm.Forms
{
	public partial class FormGridHighlight : Passport.Forms.PassportForm
	{
		private readonly SmartGrid smartGrid;
		private LookUp lookUpColors;
		private CheckBox checkBox;
		private CheckBox checkBoxInvert;
		private LookUp lookUpColumns;

		public FormGridHighlight(SmartGrid smartGrid)
			: base(PassportLoader.Load("JounalHighlight"))
		{
			InitializeComponent();
			this.smartGrid = smartGrid;
			btnApply.Visible = false;
			pageContext = new PageContext(new DataSet(), DataAccessor.CreateParametersDictionary());
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			lookUpColors = FindControl("colorHighlight") as LookUp;
			if (lookUpColors != null)
			{
				lookUpColors.AddItem("Оттенки красного", ColorHighlight.Red);
				lookUpColors.AddItem("Оттенки синего", ColorHighlight.Blue);
				lookUpColors.AddItem("Оттенки зеленого", ColorHighlight.Green);
				lookUpColors.SelectedValue = smartGrid.ColorHighlight;
			}

			lookUpColumns = FindControl("columnNameHighlight") as LookUp;
			if (lookUpColumns != null)
			{
				foreach (KeyValuePair<string, string> column in smartGrid.Entity.GetColumnsForHighlight())
				{
					if (smartGrid.DataSource.Table.Columns.Contains(column.Key))
						lookUpColumns.AddItem(column.Value, column.Key);
				}

				if (string.IsNullOrEmpty(smartGrid.ColumnNameHighlight))
					lookUpColumns.SelectedIndex = 0;
				else
					lookUpColumns.SelectedValue = smartGrid.ColumnNameHighlight;
			}

			checkBoxInvert = FindControl("isInvertColor") as CheckBox;
			if (checkBoxInvert != null)
				checkBoxInvert.Checked = smartGrid.IsHighlightInvertColor;

			checkBox = FindControl("isUseHighlight") as CheckBox;
			if (checkBox != null)
			{
				checkBox.CheckedChanged += new EventHandler(checkBox_CheckedChanged);
				checkBox.Checked = smartGrid.IsNeedHighlight;
			}

			UpdateControlEnabled();
		}

		private void checkBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdateControlEnabled();
		}

		private void UpdateControlEnabled()
		{
			lookUpColumns.Enabled = checkBox.Checked;
			lookUpColors.Enabled = checkBox.Checked;
			checkBoxInvert.Enabled = checkBox.Checked;
		}

		protected override void ApplyChanges(Button clickedButton)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				smartGrid.IsNeedHighlight = checkBox.Checked;
				smartGrid.ColumnNameHighlight = lookUpColumns.SelectedValue.ToString();
				smartGrid.ColorHighlight = (ColorHighlight) lookUpColors.SelectedValue;
				smartGrid.IsHighlightInvertColor = checkBoxInvert.Checked;
				smartGrid.HighlightRows();
			}
			catch(Exception exp)
			{
				ErrorManager.PublishError(exp);
			}
		}
	}
}
