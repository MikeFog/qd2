using FogSoft.WinForm.Classes;
using System;
using Microsoft.Office.Interop.Excel;
using System.Windows.Forms;
using Application = Microsoft.Office.Interop.Excel.Application;
using System.Data;
using DataTable = System.Data.DataTable;
using FogSoft.WinForm;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Classes;
using System.IO;

namespace Merlin.Classes.Import
{
    internal class FirmImporter
    {
        private const int COL_PREFIX = 1;
        private const int COL_NAME = 2;
        private const int COL_DIRECTOR = 3;
        private const int COL_REPORT_STRING = 4;
        private const int COL_REGISTRATION = 5;
        private const int COL_ADDRESS = 6;
        private const int COL_PHONE = 7;
        private const int COL_EMAIL = 8;
        private const int COL_BIK = 9;
        private const int COL_ACCOUNT = 10;
        private const int COL_INN = 11;
        private const int COL_KPP = 12;
        private const int COL_OGRN = 13;
        private DataTable _tableErrors;

        public void Import()
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = "Файлы Excel|*.xls;*.xlsx;"
            };

            DialogResult result = fileDialog.ShowDialog(Globals.MdiParent);
            if (result != DialogResult.OK) return;

            System.Windows.Forms.Application.DoEvents();
            Cursor.Current = Cursors.WaitCursor;

            Application excel = new Application();
            Workbook book = null;
            _tableErrors = CreateErrorTable();
            int successCounter = 0;

            try
            {
                book = excel.Workbooks.Open(fileDialog.FileName, ReadOnly: true);
                foreach (Worksheet worksheet in book.Worksheets)
                {
                    successCounter += ImportWorkSheet(worksheet);
                }

                if (_tableErrors.Rows.Count > 0)
                    Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ErrTmplGen), "Ошибки импорта", _tableErrors);
                FogSoft.WinForm.Forms.MessageBox.ShowInformation(string.Format(Properties.Resources.FirmImportResult, successCounter));
            }
            catch(Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
            finally
            {
                book?.Close(false);
                excel?.Quit();
                Cursor.Current = Cursors.Default;
            }
        }

        private int ImportWorkSheet(Worksheet worksheet)
        {
            int successCounter = 0;
            int i = 2;
            while (((Range)worksheet.Cells[i, COL_NAME]).Value != null)
            {
                if(SaveFirm(worksheet, i++))
                    successCounter++;
            }
            return successCounter;
        }

        private DataTable CreateErrorTable()
        {
            DataTable tableErrors = new DataTable();

            DataColumn column = new DataColumn("description", System.Type.GetType("System.String"));
            tableErrors.Columns.Add(column);
            return tableErrors;
        }

        private bool SaveFirm(Worksheet worksheet, int rowNum)
        {
            try
            {
                Firm firm = new Firm();
                firm[Firm.ParamNames.Name] = ((Range)worksheet.Cells[rowNum, COL_NAME]).Value;
                firm[Firm.ParamNames.Prefix] = ((Range)worksheet.Cells[rowNum, COL_PREFIX]).Value;
                firm[Firm.ParamNames.Director] = ((Range)worksheet.Cells[rowNum, COL_DIRECTOR]).Value;
                firm[Firm.ParamNames.ReportString] = ((Range)worksheet.Cells[rowNum, COL_REPORT_STRING]).Value;
                firm[Firm.ParamNames.Registration] = ((Range)worksheet.Cells[rowNum, COL_REGISTRATION]).Value;
                firm[Firm.ParamNames.Address] = ((Range)worksheet.Cells[rowNum, COL_ADDRESS]).Value;
                firm[Firm.ParamNames.Email] = ((Range)worksheet.Cells[rowNum, COL_EMAIL]).Value;
                firm[Firm.ParamNames.INN] = ((Range)worksheet.Cells[rowNum, COL_INN]).Value;
                firm[Firm.ParamNames.KPP] = ((Range)worksheet.Cells[rowNum, COL_KPP]).Value;
                firm[Firm.ParamNames.EGRN] = ((Range)worksheet.Cells[rowNum, COL_OGRN]).Value;
                firm[Firm.ParamNames.Account] = ((Range)worksheet.Cells[rowNum, COL_ACCOUNT]).Value;
                firm[Firm.ParamNames.Phone] = ((Range)worksheet.Cells[rowNum, COL_PHONE]).Value;
                firm[Firm.ParamNames.IsActive] = 1;

                var bik = ((Range)worksheet.Cells[rowNum, COL_BIK]).Value;
                if (bik != null)
                {
                    PresentationObject bank = Bank.Find(bik.ToString());
                    if (bank != null)
                        firm[Firm.ParamNames.BankId] = bank[Firm.ParamNames.BankId];
                }
                firm.Update();
                
                return true;
            }
            catch (Exception ex)
            {
                DataRow row = _tableErrors.NewRow();
                row["description"] = string.Format("Ошибка импорта в строке {0}: {1} ", rowNum, ErrorManager.GetErrorMessage(ex));
                _tableErrors.Rows.Add(row);
                return false;
            }
        }
    }
}
