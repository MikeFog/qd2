using System;
using System.Drawing;
using System.Windows.Forms;

namespace Merlin.Forms.CreateActionMaster
{
	// Немодальная справка по цветам сетки веера — открывается кнопкой "Легенда" на
	// тулбаре EditIssuesForm (только там: смысл цветов завязан на чек-лист кампаний,
	// которого у линейной кампании нет). Немодальная специально: удобно держать открытой
	// рядом с сеткой, а не закрывать перед каждым сравнением с цветом в ячейке.
	// Цвета здесь — те же константы, что красят ячейки в TariffGrid.cs
	// (MarkCellAsHavingCurrentCampaignIssues и соседние методы, RefreshWindowsColors) —
	// при правке цвета в гриде поправить и здесь, чтобы легенда не разъехалась с фактом.
	internal partial class VeerColorLegendForm : Form
	{
		public VeerColorLegendForm()
		{
			InitializeComponent();
			BuildContent();
		}

		private void BuildContent()
		{
			AppendHeader("Цвет текста в ячейке");

			AppendColorLine("Синий", Color.Blue, null,
				"выпуск акции есть у всех отмеченных галочкой кампаний.");
			AppendColorLine("Красный", Color.Red, null,
				"выпуск акции есть, но не у всех отмеченных кампаний. Наведите мышью на " +
				"такую ячейку — подсказка покажет, каких станций не хватает.");
			AppendColorLine("Бирюзовый", Color.LightSeaGreen, null,
				"подтверждённый выпуск этой же фирмы из другой акции — на всех отмеченных станциях.");
			AppendColorLine("Оранжевый", Color.Orange, null,
				"то же самое, но только на части отмеченных станций.");

			AppendNote(
				"Учитываются только станции отмеченных галочкой кампаний — невыбранная кампания " +
				"как будто не существует. Если сузить выбор, синяя или бирюзовая ячейка может " +
				"остаться такой же, хотя в снятой с галочки кампании уже стоит выпуск — цвет об " +
				"этом не сообщит.");
			AppendNote(
				"Кнопка «Учитывать неподтверждённые» добавляет в бирюзовый/оранжевый ещё и " +
				"неподтверждённые чужие выпуски. На синий и красный она не влияет.");

			AppendHeader("Фон ячейки");
			AppendColorLine("Розовый", null, Color.FromArgb(255, 231, 234),
				"окно отключено (при «Показывать отключённые»).");
			AppendColorLine("Голубой", null, Color.LightSteelBlue,
				"помеченное окно.");
			AppendColorLine("Сиреневый", null, Color.FromArgb(223, 211, 238),
				"прайм-тайм (при «Видеть прайм»).");

			AppendHeader("Жирный шрифт", isLast: true);
			AppendPlain(
				"При выбранной позиции ролика (первый / второй / последний) жирным выделены " +
				"ячейки, где эта позиция ещё свободна.");

			rtbLegend.Select(0, 0);
		}

		private void AppendHeader(string text, bool isLast = false)
		{
			if (rtbLegend.TextLength > 0)
				AppendPlain(Environment.NewLine);

			int start = rtbLegend.TextLength;
			rtbLegend.AppendText(text + Environment.NewLine);
			rtbLegend.Select(start, text.Length);
			rtbLegend.SelectionFont = new Font(rtbLegend.Font, FontStyle.Bold);
		}

		// "Слово" цветным текстом или цветной заливкой (что именно красит соответствующий
		// цвет в реальной сетке) + обычное описание после него.
		private void AppendColorLine(string word, Color? foreColor, Color? backColor, string description)
		{
			int wordStart = rtbLegend.TextLength;
			rtbLegend.AppendText(word);
			rtbLegend.Select(wordStart, word.Length);
			rtbLegend.SelectionFont = new Font(rtbLegend.Font, FontStyle.Bold);
			if (foreColor.HasValue)
				rtbLegend.SelectionColor = foreColor.Value;
			if (backColor.HasValue)
				rtbLegend.SelectionBackColor = backColor.Value;

			int restStart = rtbLegend.TextLength;
			rtbLegend.AppendText(" — " + description + Environment.NewLine);
			rtbLegend.Select(restStart, rtbLegend.TextLength - restStart);
			rtbLegend.SelectionColor = rtbLegend.ForeColor;
			rtbLegend.SelectionBackColor = rtbLegend.BackColor;
			rtbLegend.SelectionFont = rtbLegend.Font;
		}

		private void AppendNote(string text)
		{
			int start = rtbLegend.TextLength;
			rtbLegend.AppendText(text + Environment.NewLine);
			rtbLegend.Select(start, rtbLegend.TextLength - start);
			rtbLegend.SelectionColor = Color.DimGray;
			rtbLegend.SelectionFont = new Font(rtbLegend.Font, FontStyle.Italic);
		}

		private void AppendPlain(string text)
		{
			int start = rtbLegend.TextLength;
			rtbLegend.AppendText(text + Environment.NewLine);
			rtbLegend.Select(start, rtbLegend.TextLength - start);
			rtbLegend.SelectionColor = rtbLegend.ForeColor;
			rtbLegend.SelectionFont = rtbLegend.Font;
		}
	}
}
