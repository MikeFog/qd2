using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Classes;
using Merlin.Forms.FilterForm;

namespace Merlin.Forms
{
	// Выбор рекламных акций для сводного «Графика размещения по нескольким
	// акциям» (Рекламный отдел). Список — Actions1 по фильтру сущности
	// «Рекламная акция» (iEntity.filter, entityID 77): подтверждённые и
	// макеты вместе, права менеджера проверяет процедура по @loggedUserID.
	// Номера вручную не вводятся — в медиаплан попадают только акции,
	// которые пользователю разрешено видеть.
	//
	// Отмеченные акции копятся в _selectedIds и переживают смену фильтра:
	// SmartGrid при перепривязке помнит отметки только у строк, оставшихся
	// в новой выборке.
	public class FrmActionsSelector : Form
	{
		private IContainer components = null;
		private ToolStrip tsMain;
		private ToolStripButton tsbRefresh;
		private ToolStripButton tsbFilter;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripButton tsbClearSelection;
		private TableLayoutPanel tableLayoutPanel1;
		private SmartGrid grid;
		private Label lblSelected;
		private FlowLayoutPanel flpButtons;
		private Button btnOk;
		private Button btnCancel;

		private readonly Entity _entity = EntityManager.GetEntity((int)Entities.Action);
		private readonly Dictionary<string, object> _filter = DataAccessor.CreateParametersDictionary();
		private readonly SortedSet<int> _selectedIds = new SortedSet<int>();

		public FrmActionsSelector()
		{
			InitializeComponent();
			grid.Entity = _entity;
			Globals.ResolveFilterInitialValues(_filter, _entity.XmlFilter);
		}

		/// <summary>Отмеченные акции по возрастанию номера.</summary>
		public IList<int> ActionIds => new List<int>(_selectedIds);

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null) components.Dispose();
			base.Dispose(disposing);
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			LoadActions();
			UpdateSelectedLabel();
		}

		private void LoadActions()
		{
			try
			{
				Cursor = Cursors.WaitCursor;
				Dictionary<string, object> ps = DataAccessor.PrepareParameters(_entity);
				foreach (KeyValuePair<string, object> kvp in _filter)
					ps[kvp.Key] = kvp.Value;
				ps["isShowActivate"] = true;
				ps["isShowNotActivate"] = true;

				// Значок — как в журналах (JournalForm.RefreshJournal): задан ли фильтр.
				tsbFilter.Image = _filter.Count > 0
					? FogSoft.WinForm.Properties.Resources.FilterSet
					: FogSoft.WinForm.Properties.Resources.Filter;

				DataTable dt = ((DataSet)DataAccessor.DoAction(ps)).Tables[Constants.TableNames.Data];
				grid.DataSource = dt.DefaultView;

				// Отметки из других выборок: колонка чекбоксов уже добавлена гридом.
				foreach (DataRow row in dt.Rows)
					if (_selectedIds.Contains(ActionIdOf(row)))
						row[SmartGrid.COL_IsSelected] = true;
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private static int ActionIdOf(DataRow row)
		{
			return ParseHelper.ParseToInt32(row[Merlin.Classes.Action.ParamNames.ActionId].ToString());
		}

		private void grid_ObjectChecked(PresentationObject po, bool state)
		{
			int id = ParseHelper.ParseToInt32(po.IDs[0].ToString());
			if (state) _selectedIds.Add(id);
			else _selectedIds.Remove(id);
			UpdateSelectedLabel();
		}

		private void UpdateSelectedLabel()
		{
			lblSelected.Text = _selectedIds.Count == 0
				? "Акции не выбраны"
				: string.Format("Выбрано ({0}): {1}", _selectedIds.Count, string.Join(", ", _selectedIds));
			tsbClearSelection.Enabled = _selectedIds.Count > 0;
		}

		private void tsbRefresh_Click(object sender, EventArgs e)
		{
			LoadActions();
		}

		private void tsbFilter_Click(object sender, EventArgs e)
		{
			try
			{
				Cursor = Cursors.WaitCursor;
				using (ActionJournalFilter frm = new ActionJournalFilter(
					_entity, Globals.PrepareForFilter(_entity), _filter, _entity.XmlFilter))
				{
					Cursor = Cursors.Default;
					if (frm.ShowDialog(this) == DialogResult.OK)
						LoadActions();
				}
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void tsbClearSelection_Click(object sender, EventArgs e)
		{
			// Без перезагрузки: при перепривязке SmartGrid вернул бы отметки
			// из своего Added2Checked.
			// Кнопка тулбара не забирает фокус у грида: только что отмеченный чекбокс
			// остаётся в режиме редактирования и рисует своё значение, а не строки,
			// пока фокус не уйдёт. Завершаем редактирование сами, до сброса данных.
			DataGridView dgv = grid.InternalGrid;
			dgv.EndEdit();

			_selectedIds.Clear();
			grid.Added2Checked.Clear();
			grid.RemovedFromChecked.Clear();
			if (grid.DataSource != null)
				foreach (DataRow row in grid.DataSource.Table.Rows)
					row[SmartGrid.COL_IsSelected] = false;
			if (dgv.Controls.Find("checkboxHeader", false).FirstOrDefault() is CheckBox header)
				header.Checked = false;
			dgv.Invalidate();
			UpdateSelectedLabel();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if (_selectedIds.Count == 0)
			{
				UserMessage.ShowExclamation("Отметьте хотя бы одну рекламную акцию.");
				return;
			}
			DialogResult = DialogResult.OK;
		}

		private void InitializeComponent()
		{
			this.tsMain = new System.Windows.Forms.ToolStrip();
			this.tsbRefresh = new System.Windows.Forms.ToolStripButton();
			this.tsbFilter = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.tsbClearSelection = new System.Windows.Forms.ToolStripButton();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.grid = new FogSoft.WinForm.Controls.SmartGrid();
			this.lblSelected = new System.Windows.Forms.Label();
			this.flpButtons = new System.Windows.Forms.FlowLayoutPanel();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.tsMain.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.flpButtons.SuspendLayout();
			this.SuspendLayout();
			//
			// tsMain
			//
			this.tsMain.Font = new System.Drawing.Font("Segoe UI Variable Text", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.tsMain.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.tsMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.tsbRefresh,
			this.tsbFilter,
			this.toolStripSeparator1,
			this.tsbClearSelection});
			this.tsMain.Name = "tsMain";
			this.tsMain.TabIndex = 1;
			//
			// tsbRefresh
			//
			this.tsbRefresh.Image = global::FogSoft.WinForm.Properties.Resources.Refresh;
			this.tsbRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbRefresh.Name = "tsbRefresh";
			this.tsbRefresh.Text = "Обновить";
			this.tsbRefresh.Click += new System.EventHandler(this.tsbRefresh_Click);
			//
			// tsbFilter
			//
			this.tsbFilter.Image = global::FogSoft.WinForm.Properties.Resources.Filter;
			this.tsbFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbFilter.Name = "tsbFilter";
			this.tsbFilter.Text = "Фильтр";
			this.tsbFilter.Click += new System.EventHandler(this.tsbFilter_Click);
			//
			// toolStripSeparator1
			//
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			//
			// tsbClearSelection
			//
			this.tsbClearSelection.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tsbClearSelection.Name = "tsbClearSelection";
			this.tsbClearSelection.Text = "Снять отметки";
			this.tsbClearSelection.Click += new System.EventHandler(this.tsbClearSelection_Click);
			//
			// tableLayoutPanel1
			//
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.grid, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.lblSelected, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.flpButtons, 0, 2);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(6);
			this.tableLayoutPanel1.RowCount = 3;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.TabIndex = 0;
			//
			// grid
			//
			this.grid.Caption = "";
			this.grid.CaptionVisible = false;
			this.grid.CheckBoxes = true;
			this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.grid.Font = new System.Drawing.Font("Segoe UI Variable Text", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.grid.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.grid.MenuEnabled = false;
			this.grid.Name = "grid";
			this.grid.QuickSearchVisible = true;
			this.grid.ShowMultiselectColumn = true;
			this.grid.TabIndex = 0;
			this.grid.ObjectChecked += new FogSoft.WinForm.ObjectCheckedDelegate(this.grid_ObjectChecked);
			//
			// lblSelected
			//
			this.lblSelected.AutoEllipsis = true;
			this.lblSelected.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lblSelected.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.lblSelected.Name = "lblSelected";
			this.lblSelected.Size = new System.Drawing.Size(100, 26);
			this.lblSelected.TabIndex = 1;
			this.lblSelected.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// flpButtons
			//
			this.flpButtons.AutoSize = true;
			this.flpButtons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.flpButtons.Controls.Add(this.btnCancel);
			this.flpButtons.Controls.Add(this.btnOk);
			this.flpButtons.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flpButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.flpButtons.Margin = new System.Windows.Forms.Padding(0);
			this.flpButtons.Name = "flpButtons";
			this.flpButtons.TabIndex = 2;
			//
			// btnOk
			//
			this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnOk.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(100, 33);
			this.btnOk.TabIndex = 0;
			this.btnOk.Text = "Ок";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(100, 33);
			this.btnCancel.TabIndex = 1;
			this.btnCancel.Text = "Отмена";
			this.btnCancel.UseVisualStyleBackColor = true;
			//
			// FrmActionsSelector
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 24F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(1000, 640);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Controls.Add(this.tsMain);
			this.Font = new System.Drawing.Font("Segoe UI Variable Display", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(600, 400);
			this.Name = "FrmActionsSelector";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "График размещения по нескольким акциям";
			this.tsMain.ResumeLayout(false);
			this.tsMain.PerformLayout();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.flpButtons.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();
		}
	}
}
