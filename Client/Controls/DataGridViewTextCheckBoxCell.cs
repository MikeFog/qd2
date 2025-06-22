using System.Drawing;
using System.Windows.Forms;

namespace Merlin.Controls
{
	internal class DataGridViewTextCheckBoxCell : DataGridViewCheckBoxCell
	{
		public DataGridViewTextCheckBoxCell(bool threeState) : base(threeState)
		{
		}

		protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
		                              DataGridViewElementStates elementState, object value, object formattedValue,
		                              string errorText, DataGridViewCellStyle cellStyle,
		                              DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
		{
			// the base Paint implementation paints the check box

			base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState, value, formattedValue, errorText, cellStyle,
			           advancedBorderStyle, paintParts);

			// now let's paint the text

			// Get the check box bounds: they are the content bounds

			Rectangle contentBounds = GetContentBounds(rowIndex);

			// Compute the location where we want to paint the string.

			Point stringLocation = new Point {Y = cellBounds.Y + 2, X = cellBounds.X + contentBounds.Right + 2};

			// Compute the Y.

			// NOTE: the current logic does not take into account padding.

			// Compute the X.

			// Content bounds are computed relative to the cell bounds

			// - not relative to the DataGridView control.

			// Paint the string.

			graphics.DrawString(ToolTipText, Control.DefaultFont, SystemBrushes.WindowText, stringLocation);
		}
	}
}