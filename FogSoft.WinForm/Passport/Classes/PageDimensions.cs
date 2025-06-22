using System.Windows.Forms;

namespace FogSoft.WinForm.Passport.Classes
{
	public class PageDimensions
	{
		public struct Offsets
		{
			public const int LeftMargin = 5;
			public const int RightMargin = 5;
			public const int TopMargin = 40;
			public const int LineSpacing = 3;
			public const int BottomMargin = 5;
		}

		public readonly int RightColumnX;
		public readonly int ControlWidthInLeftColumn;
		public readonly int ControlWidthInRightColumn;
		public readonly int MaximumControlWidth;

		public PageDimensions(TabPage tabPage)
		{
			RightColumnX = tabPage.Width/2 - 30;
			ControlWidthInLeftColumn = RightColumnX - Offsets.LeftMargin;
			ControlWidthInRightColumn = tabPage.Width - RightColumnX - Offsets.RightMargin;
			MaximumControlWidth = tabPage.Width - Offsets.RightMargin - Offsets.LeftMargin;
		}
	}
}