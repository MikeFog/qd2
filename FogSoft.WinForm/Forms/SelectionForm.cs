using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.WinForm.Forms
{
	public partial class SelectionForm : Form
	{
        public delegate bool IsSelectionCorrect(SelectionForm selectionForm);
		private readonly IsSelectionCorrect validateSelection;

        private PresentationObject selectedObject;

		public SelectionForm()
		{
			InitializeComponent();
		}

		public SelectionForm(IObjectContainer container, string caption, bool showCheckboxes = false, IsSelectionCorrect validateSelection = null) : this()
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				Text = caption;
                //container.LoadContent(InterfaceObjects.SimpleJournal);
                grid.CheckBoxes = showCheckboxes;
                grid.Entity = container.ChildEntity;
				grid.DataSource = container.GetContent().DefaultView;
                this.validateSelection = validateSelection;

                //container.Dispose();
            }
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

        public SelectionForm(Entity entity, DataView dataView, string caption)	: this()
		{
			InitForm(entity, dataView, caption);
		}

		public SelectionForm(Entity entity, DataView dataView, string caption, bool showCheckboxes, IsSelectionCorrect validateSelection = null)
			: this()
		{
			grid.CheckBoxes = showCheckboxes;
			InitForm(entity, dataView, caption);
			this.validateSelection = validateSelection;
		}

        private void InitForm(Entity entity, DataView dataView, string caption)
		{
			Text = caption;
			grid.Entity = entity;
			grid.DataSource = dataView;
		}

		[Browsable(false)]
		public PresentationObject SelectedObject
		{
			get { return selectedObject; }
		}

		[Browsable(false)]
		public List<PresentationObject> AddedItems
		{
			get { return grid.Added2Checked; }
		}

		[Browsable(false)]
		public List<PresentationObject> DeletedItems
		{
			get { return grid.RemovedFromChecked; }
		}
        [Browsable(false)]
        public int ItemsCount
        {
            get { return grid.ItemsCount; }
        }

        private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				selectedObject = grid.SelectedObject;
				DialogResult = DialogResult.OK;
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

		private void grid_DblClick()
		{
			btnOk_Click(null, null);
		}

		private void SelectionForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (DialogResult == DialogResult.OK && validateSelection != null)
				e.Cancel = !validateSelection(this);
		}
    }
}