using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Forms;
using Merlin.Classes;

namespace Merlin.Forms
{
	/// <summary>
	/// «Сменить цену» прайс-листа: слева все разные цены тарифов (PricelistPrices.Load), справа —
	/// новая цена, предзаполнена старой. Отдаёт NewPrices — все пары «старая → новая»; пары без
	/// изменения отбрасывает сам PricelistPrices.Apply.
	/// </summary>
	internal sealed class ChangeTariffPricesForm : Form
	{
		private const string NewPriceColumn = "newPrice";

		// Набираемый текст цены: до 16 цифр, один разделитель, до двух знаков после него (decimal(18,2)).
		private static readonly Regex PriceInput = new Regex(@"^\d{0,16}([.,]\d{0,2})?$");

		private readonly DataTable _prices;
		private readonly DataGridView _grid = new DataGridView();

		public ChangeTariffPricesForm(DataTable prices)
		{
			_prices = prices;
			_prices.Columns.Add(NewPriceColumn, typeof(decimal));
			foreach (DataRow row in _prices.Rows)
				row[NewPriceColumn] = row[PricelistPrices.Columns.Price];
			_prices.AcceptChanges();

			InitializeComponent();
		}

		public IDictionary<decimal, decimal> NewPrices { get; private set; }

		private void InitializeComponent()
		{
			Label hint = new Label
			{
				AutoSize = true,
				Anchor = AnchorStyles.Left,
				Margin = new Padding(3, 0, 3, 6),
				Text = "Новая цена заменит старую во всех тарифах прайс-листа.\r\n" +
					"Тарифы со сгенерированными окнами не меняются."
			};

			_grid.Dock = DockStyle.Fill;
			_grid.AutoGenerateColumns = false;
			_grid.AllowUserToAddRows = false;
			_grid.AllowUserToDeleteRows = false;
			_grid.AllowUserToResizeRows = false;
			_grid.RowHeadersVisible = false;
			_grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			_grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
			_grid.EditMode = DataGridViewEditMode.EditOnEnter;
			_grid.BackgroundColor = SystemColors.Window;
			_grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			_grid.Columns.Add(TextColumn(PricelistPrices.Columns.Price, "Цена", "N2", true));
			_grid.Columns.Add(TextColumn(PricelistPrices.Columns.TariffsCount, "Тарифов", null, true));
			_grid.Columns.Add(TextColumn(PricelistPrices.Columns.WithWindowsCount, "Из них с окнами", null, true));
			// Без разделителя тысяч: при правке в ячейке тот же текст, что и при показе, — иначе PriceInput мешал бы.
			_grid.Columns.Add(TextColumn(NewPriceColumn, "Новая цена", "0.00", false));
			_grid.DataSource = _prices;
			_grid.DataError += Grid_DataError;
			_grid.EditingControlShowing += Grid_EditingControlShowing;

			Button btnOk = new Button { Text = "Ок", Size = new Size(100, 33) };
			btnOk.Click += BtnOk_Click;
			Button btnCancel = new Button { Text = "Отмена", Size = new Size(100, 33), DialogResult = DialogResult.Cancel };

			FlowLayoutPanel buttons = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.RightToLeft,
				AutoSize = true
			};
			buttons.Controls.Add(btnCancel);
			buttons.Controls.Add(btnOk);

			TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1 };
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.Controls.Add(hint, 0, 0);
			layout.Controls.Add(_grid, 0, 1);
			layout.Controls.Add(buttons, 0, 2);

			AcceptButton = btnOk;
			CancelButton = btnCancel;
			ClientSize = new Size(520, 360);
			Controls.Add(layout);
			Font = new Font("Segoe UI Variable Text", 9F, FontStyle.Regular, GraphicsUnit.Point, 204);
			FormBorderStyle = FormBorderStyle.SizableToolWindow;
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.CenterParent;
			Text = "Сменить цену";
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			if (_grid.Rows.Count > 0)
				_grid.CurrentCell = _grid.Rows[0].Cells[NewPriceColumn];
		}

		private static DataGridViewTextBoxColumn TextColumn(string name, string header, string format, bool readOnly)
		{
			DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn
			{
				Name = name,
				DataPropertyName = name,
				HeaderText = header,
				ReadOnly = readOnly,
				SortMode = DataGridViewColumnSortMode.NotSortable
			};
			column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			if (format != null)
				column.DefaultCellStyle.Format = format;
			if (readOnly)
				column.DefaultCellStyle.BackColor = SystemColors.Control;
			return column;
		}

		// Редактируется только «Новая цена»; поле ввода грид переиспользует между ячейками — подписка одна.
		private static void Grid_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
		{
			TextBox box = e.Control as TextBox;
			if (box == null) return;
			box.KeyPress -= PriceBox_KeyPress;
			box.KeyPress += PriceBox_KeyPress;
		}

		// Не пускает символ, после которого текст перестанет быть ценой; точка и запятая — разделитель
		// культуры. Вставку (Ctrl+V) сюда не видно — её ловят Grid_DataError и проверка на «Ок».
		private static void PriceBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (char.IsControl(e.KeyChar)) return;
			if (e.KeyChar == '.' || e.KeyChar == ',')
				e.KeyChar = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];

			TextBox box = (TextBox)sender;
			string text = box.Text.Remove(box.SelectionStart, box.SelectionLength)
				.Insert(box.SelectionStart, e.KeyChar.ToString());
			if (!PriceInput.IsMatch(text))
				e.Handled = true;
		}

		private void Grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
		{
			UserMessage.ShowExclamation("Введите число.");
			e.Cancel = true;
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			if (!_grid.EndEdit())
				return;

			Dictionary<decimal, decimal> newPrices = new Dictionary<decimal, decimal>();
			foreach (DataRow row in _prices.Rows)
			{
				if (row[NewPriceColumn] == DBNull.Value)
				{
					UserMessage.ShowExclamation("Введите новую цену.");
					return;
				}
				decimal newPrice = Convert.ToDecimal(row[NewPriceColumn]);
				string error = PricelistPrices.ValidateNewPrice(newPrice);
				if (error != null)
				{
					UserMessage.ShowExclamation(error);
					return;
				}
				newPrices.Add(Convert.ToDecimal(row[PricelistPrices.Columns.Price]), newPrice);
			}

			NewPrices = newPrices;
			DialogResult = DialogResult.OK;
		}
	}
}
