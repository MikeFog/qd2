using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Windows.Forms;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Properties;
using log4net;

namespace FogSoft.WinForm.Classes
{
	public static class ErrorManager
	{
	    private const string str_dbexceptonuniqueindex = "unique index";
        private const string str_dbexceptonconstraint = "constraint";
        private const string str_dbexceptonREFERENCE = "REFERENCE";
	    private const string str_dbexceptonuniqueindexRUS = "уникальным индексом";
        private const string str_dbexceptonconstraintRUS = "Нарушение";
        private const char char_dbexceptonquote = '"';
        private const char char_dbexceptonquoteone = '\'';

		private const string str_dbexceptonDELETE = "DELETE";

	    public static readonly ILog Log =
			LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public static void PublishError(Exception ex)
		{
			try
			{
                SqlException sqlEx = ex as SqlException;
				if (sqlEx != null)
				{
                    // This is an Sql Exception. 
                    if (sqlEx.Number == 547 || sqlEx.Number == 2627 || sqlEx.Number == 2601)
					{
						string msg = ExtractConstraintName(sqlEx);
						try
						{
							ShowExclamation(msg);
						}
						catch (Exception exc)
						{
                            Log.Error(string.Format("Ошибка в процедуре {0}", sqlEx.Procedure));
                            Log.Error(sqlEx);
                            Log.Error(ex.Data);
                            
							if (!string.IsNullOrEmpty(sqlEx.Message)
								&& sqlEx.Message.Contains(str_dbexceptonREFERENCE) && sqlEx.Message.Contains(str_dbexceptonDELETE)
								&& sqlEx.Message.IndexOf(str_dbexceptonDELETE) < sqlEx.Message.IndexOf(str_dbexceptonREFERENCE))
							{
								// Сообщение о невозможности удаления объекта
								Forms.MessageBox.ShowExclamation(Resources.DefaultCannotDeleteObject);
							}
							else
							{
								Log.Error(string.Format("Error {0} in {1}", sqlEx.Message, sqlEx.Procedure), sqlEx);
								Log.Error(exc);
								Globals.ShowMessageError(Resources.ApplicationError, ex);
							}
						}
					}
					else
					{
						try
						{
							if (MessageAccessor.GetMessage(ex.Message) == null)
							{
                                Log.Error(string.Format("Ошибка в процедуре {0}", sqlEx.Procedure));
                                Log.Error(sqlEx);
                                Log.Error(ex.Data);
                            }
                            ShowExclamation(ex.Message);
						}
						catch(Exception exc)
						{
							Log.Error(exc);
							Globals.ShowMessageError(Resources.ApplicationError, ex);
						}
					}
				}
				else
				{
					Log.Error(ex);
					Globals.ShowMessageError(Resources.ApplicationError, ex);
				}
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}
        
		private static string ExtractConstraintName(SqlException ex)
		{
            if (ex.Message.Contains(str_dbexceptonuniqueindex))
                return GetConstantName(ex.Message, str_dbexceptonuniqueindex);
            else if (ex.Message.Contains(str_dbexceptonconstraint))
                return GetConstantName(ex.Message, str_dbexceptonconstraint);
			else if (ex.Message.Contains(str_dbexceptonREFERENCE) && ex.Message.Contains(str_dbexceptonDELETE)
					&& ex.Message.IndexOf(str_dbexceptonDELETE) < ex.Message.IndexOf(str_dbexceptonREFERENCE))
                return GetConstantName(ex.Message, str_dbexceptonREFERENCE);
            else if (ex.Message.Contains(str_dbexceptonuniqueindexRUS))
                return GetConstantName(ex.Message, str_dbexceptonuniqueindexRUS);
            else if (ex.Message.Contains(str_dbexceptonconstraintRUS))
                return GetConstantName(ex.Message, str_dbexceptonconstraintRUS);
            return ex.Message;
		}

	    private static string GetConstantName(string message, string whatfind)
	    {
            int x = message.IndexOf(whatfind);
            int indexStart1 = message.IndexOf(char_dbexceptonquote, x + 1);
            int indexStart2 = message.IndexOf(char_dbexceptonquoteone, x + 1);
            int start = Math.Min(indexStart1 < 0 ? int.MaxValue : indexStart1, indexStart2 < 0 ? int.MaxValue : indexStart2);
            int indexLast1 = message.IndexOf(char_dbexceptonquote, start + 1);
            int indexLast2 = message.IndexOf(char_dbexceptonquoteone, start + 1);
            int finish = Math.Min(indexLast1 < 0 ? int.MaxValue : indexLast1, indexLast2 < 0 ? int.MaxValue : indexLast2);
            if (start > message.Length)
                start = -1;
            if (finish > message.Length)
                finish = message.Length;
            return message.Substring(start + 1, finish - start - 1);
	    }

		public static void ShowExclamation(string msgName)
		{
			Globals.ShowExclamation(msgName, null);
		}

		public static void ShowExclamation(string msgName, Dictionary<string, object> parameters)
		{
			Globals.ShowExclamation(msgName, parameters);
		}

		public static void LogError(string messageError, Exception e)
		{
			Log.Error(messageError, e);
		}

		public static string GetErrorMessage(Exception ex)
		{
            if (ex is SqlException sqlEx && (sqlEx.Number == 547 || sqlEx.Number == 2627 || sqlEx.Number == 2601))
                return MessageAccessor.GetMessage(ExtractConstraintName(sqlEx));
			return ex.Message;
        }

        public static DataTable CreateErrorsTable()
        {
            DataTable tableErrors = new DataTable();

            DataColumn column = new DataColumn("issueDate", Type.GetType("System.DateTime"));
            tableErrors.Columns.Add(column);
            column = new DataColumn("description", System.Type.GetType("System.String"));
            tableErrors.Columns.Add(column);
            return tableErrors;
        }

        public static void AddErrorRow(DataTable table, DateTime date, string description)
        {
            DataRow row = table.NewRow();
            row["issueDate"] = date;
            row["description"] = description;
            table.Rows.Add(row);
        }
    }
}