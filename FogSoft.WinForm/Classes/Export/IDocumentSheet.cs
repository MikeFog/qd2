using System;

namespace FogSoft.WinForm.Classes.Export
{
	public struct CustomType
	{
		public const string Time = "time";
	}

	public interface IDocumentSheet
	{
		void SetValuesForRange(int top, int left, int bottom, int right, object[,] data);
		void SetBordersStyles(int top, int left, int bottom, int right, bool fNeedInsideVertical);
		void SetBoldForRange(int top, int left, int bottom, int right);
		void SetCellValue(int y, int x, object val);
		void SetAutoFitCells();
        void SetAutoFitCells(int left, int right);
		void SetFormatForCell(int top, int left, int bottom, int right, Type type);
        void SetFormatForCell(int top, int left, int bottom, int right, string type);
        void SetOrientationForCells(int x, int y, int gr);
		void SetLandscapeOrientation();
		void SetStyleForRange(int top, int left, int bottom, int right, bool fBold, bool fItalic, int fontSize);
		void SetBackground(int top, int left, int bottom, int right, int r, int g, int b);
		void InsertImage(int top, int left, byte[] image);
		void SetColumnWidth(int columnIndex, double width);
        double GetColumnWidth(int columnIndex);
		void SetColumnNumberFormat(int columnIndex, string format);
        void SetWrapText(int top, int left, int bottom, int right, bool wrap);
    }
}