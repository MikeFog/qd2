using System;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.Controls;

namespace Merlin.Forms
{
	/// <summary>
	/// Предупреждение перед сохранением правки скидки (объёмной или пакетной): список акций, в которых
	/// эта скидка уже посчитана и которым правка может её поменять при следующем пересчёте.
	/// Что спросить, собирает ядро (DiscountChangeImpact → PresentationObject.GetSaveWarning/GetDeleteWarning,
	/// то же спрашивает веб); форма только показывает. Решает человек: «Сохранить» — правка пишется,
	/// акции не пересчитываются; «Отмена» — сохранение не происходит.
	/// </summary>
	public class DiscountAffectedActionsForm : Form
	{
		private const int VirtualEntityId = -5200;

		private readonly SmartGrid grid;

		/// <summary>
		/// Перед записью паспорта (AddItem/UpdateItem). false — пользователь отказался.
		/// Форма паспорта к этому моменту уже положила введённые значения в объект, а запись не
		/// состоится (переопределённый Update() вернёт false до base.Update(), чей catch делает
		/// Refresh) - поэтому у существующего объекта возвращаем значения из базы, иначе при
		/// повторном открытии свойств видна несохранённая правка.
		/// </summary>
		public static bool ConfirmSave(PresentationObject po)
		{
			bool confirmed = false;
			try
			{
				confirmed = Confirm(po.GetSaveWarning());
				return confirmed;
			}
			finally
			{
				if (!confirmed && !po.IsNew)
					po.Refresh();
			}
		}

		/// <summary>Перед удалением строки, которая входит в содержимое скидки (порог, станция пакета).</summary>
		public static bool ConfirmDelete(PresentationObject po)
		{
			return Confirm(po.GetDeleteWarning());
		}

		private static bool Confirm(SaveWarning warning)
		{
			if (warning == null)
				return true;

			using (DiscountAffectedActionsForm form = new DiscountAffectedActionsForm(warning))
				return form.ShowDialog(Form.ActiveForm) == DialogResult.OK;
		}

		private DiscountAffectedActionsForm(SaveWarning warning)
		{
			Entity entity = EntityManager.CreateVirtualEntity(
				VirtualEntityId, "Затронутые акции", "DiscountAffectedActions", "rowID", string.Empty,
				warning.Columns);

			grid = new SmartGrid { Dock = DockStyle.Fill, MenuEnabled = false };

			Label label = new Label
			{
				AutoSize = true,
				Dock = DockStyle.Fill,
				Padding = new Padding(0, 0, 0, 6),
				Text = string.Join(Environment.NewLine, warning.Paragraphs)
			};

			Button btnOk = new Button { Text = warning.OkText, Size = new Size(100, 33), DialogResult = DialogResult.OK, FlatStyle = FlatStyle.System };
			Button btnCancel = new Button { Text = "Отмена", Size = new Size(100, 33), DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.System };

			ToolStripButton tsbExcel = new ToolStripButton
			{
				DisplayStyle = ToolStripItemDisplayStyle.Image,
				ImageTransparentColor = Color.Magenta,
				Image = Globals.GetImage(Constants.ActionsImages.ExportExcel),
				Name = "tsbExcel",
				ToolTipText = "Экспорт таблицы"
			};
			tsbExcel.Click += (s, e) => ExportExcel();
			ToolStrip toolbar = new ToolStrip { Dock = DockStyle.Fill, ImageScalingSize = new Size(20, 20), GripStyle = ToolStripGripStyle.Hidden };
			toolbar.Items.Add(tsbExcel);

			FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
			buttons.Controls.Add(btnCancel);
			buttons.Controls.Add(btnOk);

			TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 4 };
			layout.RowStyles.Add(new RowStyle());
			layout.RowStyles.Add(new RowStyle());
			layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			layout.RowStyles.Add(new RowStyle());
			layout.Controls.Add(label, 0, 0);
			layout.Controls.Add(toolbar, 0, 1);
			layout.Controls.Add(grid, 0, 2);
			layout.Controls.Add(buttons, 0, 3);

			Text = warning.Title;
			Font = new Font("Segoe UI Variable Text", 9F, FontStyle.Regular, GraphicsUnit.Point, 204);
			ClientSize = new Size(1150, 520);
			StartPosition = FormStartPosition.CenterParent;
			ShowInTaskbar = false;
			MinimizeBox = false;
			AcceptButton = btnOk;
			CancelButton = btnCancel;
			Controls.Add(layout);

			grid.Entity = entity;
			grid.DataSource = warning.Table.DefaultView;
			Shown += (s, e) => grid.AdjustColumnsWidth();
		}

		private void ExportExcel()
		{
			try
			{
				Cursor = Cursors.WaitCursor;
				ExportManager.ExportExcel(grid.InternalGrid, grid.Entity);
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
	}
}
