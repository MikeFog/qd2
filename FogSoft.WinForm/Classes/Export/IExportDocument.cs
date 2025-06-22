namespace FogSoft.WinForm.Classes.Export
{
	public interface IExportDocument
	{
		IDocumentSheet GetNewSheet(string name, string fontName, int fontSize);
		void StartExport();
		void FinishExport();
		void OnAppQuit();
		bool Visible();
	}
}