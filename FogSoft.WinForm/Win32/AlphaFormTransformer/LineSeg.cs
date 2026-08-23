namespace FogSoft.WinForm.Win32.AlphaFormTransformer
{
	/// <summary>
	/// Defines a line segment. Used by AlphaFormTransformer.UpdateRectListFromAlpha()
	/// </summary>
	internal struct LineSeg
	{
		public int x1;
		public int x2;
		public int y;
		public int dy;

		public LineSeg(int x1, int x2, int y, int dy)
		{
			this.x1 = x1;
			this.x2 = x2;
			this.y = y;
			this.dy = dy;
		}
	}
}
