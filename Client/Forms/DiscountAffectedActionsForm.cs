using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Forms
{
	/// <summary>
	/// Предупреждение перед сохранением правки скидки (объёмной или пакетной): список акций, в которых
	/// эта скидка уже посчитана и которым правка может её поменять при следующем пересчёте
	/// (процедура DiscountChangeAffectedActions). Решает человек: «Сохранить» — правка пишется,
	/// акции не пересчитываются; «Отмена» — сохранение не происходит.
	/// </summary>
	public class DiscountAffectedActionsForm : Form
	{
		private const int VirtualEntityId = -5200;
		private const string ProcedureName = "DiscountChangeAffectedActions";

		private readonly SmartGrid grid;

		/// <summary>Перед записью паспорта (AddItem/UpdateItem). false — пользователь отказался.</summary>
		public static bool ConfirmSave(PresentationObject po)
		{
			// Копия исходную скидку не меняет
			if (po.Parameters.TryGetValue(Constants.ParamNames.ActionName, out object action)
				&& action as string == Constants.Actions.Clone)
				return true;
			return Confirm(po, po.IsNew ? Constants.Actions.AddItem : Constants.Actions.Update);
		}

		/// <summary>Перед удалением строки, которая входит в содержимое скидки (порог, станция пакета).</summary>
		public static bool ConfirmDelete(PresentationObject po)
		{
			return Confirm(po, Constants.Actions.Delete);
		}

		private static bool Confirm(PresentationObject po, string actionName)
		{
			Dictionary<string, object> parameters = po.Parameters;
			parameters["entityID"] = po.Entity.Id;
			parameters[Constants.ParamNames.ActionName] = actionName;

			DataTable affected = DataAccessor.LoadDataSet(ProcedureName, parameters).Tables[0];
			if (affected.Rows.Count == 0)
				return true;

			bool byCampaign = po.Entity.Id == (int)Entities.DiscountRelease || po.Entity.Id == (int)Entities.DiscountValue;
			using (DiscountAffectedActionsForm form = new DiscountAffectedActionsForm(affected, byCampaign))
				return form.ShowDialog(Form.ActiveForm) == DialogResult.OK;
		}

		private DiscountAffectedActionsForm(DataTable affected, bool byCampaign)
		{
			List<Entity.Attribute> attributes = new List<Entity.Attribute>
			{
				new Entity.Attribute("actionID", "Акция", "int"),
				new Entity.Attribute("firm", "Фирма", "nvarchar"),
				new Entity.Attribute("manager", "Менеджер", "nvarchar"),
				new Entity.Attribute("startDate", "Начало акции", "datetime"),
				new Entity.Attribute("finishDate", "Окончание акции", "datetime"),
				new Entity.Attribute("status", "Статус", "nvarchar"),
			};
			if (byCampaign)
			{
				attributes.Add(new Entity.Attribute("massmedia", "Радиостанция", "nvarchar"));
				attributes.Add(new Entity.Attribute("campaignID", "Кампания", "int"));
			}
			attributes.Add(new Entity.Attribute("discount", byCampaign ? "Скидка кампании" : "Пакетная скидка", "float"));

			Entity entity = EntityManager.CreateVirtualEntity(
				VirtualEntityId, "Затронутые акции", "DiscountAffectedActions", "rowID", string.Empty,
				attributes.ToArray());

			grid = new SmartGrid { Dock = DockStyle.Fill, MenuEnabled = false };

			Label label = new Label
			{
				AutoSize = true,
				Dock = DockStyle.Fill,
				Padding = new Padding(0, 0, 0, 6),
				Text = string.Format(
					"Скидка уже посчитана в {0} ({1}), и изменение может её поменять.{2}" +
					"Сами акции сейчас не пересчитываются: скидка у них может измениться при следующем пересчёте " +
					"(любая правка выпусков).{2}Сохранить изменения?",
					byCampaign ? "кампаниях" : "акциях", affected.Rows.Count, Environment.NewLine)
			};

			Button btnOk = new Button { Text = "Сохранить", Size = new Size(100, 33), DialogResult = DialogResult.OK, FlatStyle = FlatStyle.System };
			Button btnCancel = new Button { Text = "Отмена", Size = new Size(100, 33), DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.System };
			Button btnExcel = new Button { Text = "В Excel", Size = new Size(100, 33), FlatStyle = FlatStyle.System };
			btnExcel.Click += (s, e) => ExportExcel();

			FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
			buttons.Controls.Add(btnCancel);
			buttons.Controls.Add(btnOk);
			buttons.Controls.Add(btnExcel);

			TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 3 };
			layout.RowStyles.Add(new RowStyle());
			layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			layout.RowStyles.Add(new RowStyle());
			layout.Controls.Add(label, 0, 0);
			layout.Controls.Add(grid, 0, 1);
			layout.Controls.Add(buttons, 0, 2);

			Text = "Изменение затронет акции";
			Font = new Font("Segoe UI Variable Text", 9F, FontStyle.Regular, GraphicsUnit.Point, 204);
			ClientSize = new Size(1150, 520);
			StartPosition = FormStartPosition.CenterParent;
			ShowInTaskbar = false;
			MinimizeBox = false;
			AcceptButton = btnOk;
			CancelButton = btnCancel;
			Controls.Add(layout);

			grid.Entity = entity;
			grid.DataSource = affected.DefaultView;
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
