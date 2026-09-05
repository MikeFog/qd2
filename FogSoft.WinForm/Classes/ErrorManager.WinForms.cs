using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows.Forms;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using FogSoft.WinForm.Properties;

namespace FogSoft.WinForm.Classes
{
	// Показ ошибок пользователю. Основная часть ErrorManager (логирование, разбор
	// имени ограничения SQL) — в ErrorManager.cs, она компилируется также в сборку
	// без UI. Логика не менялась, код перенесён как есть.
	// См. docs/tasks/web-migration.md, этап 0.
	public static partial class ErrorManager
	{
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

						// Нарушение ограничения показывается пользователю и дальше не всплывает,
						// но без записи в лог жалобу («падает на комбо-модуле пустой акции»)
						// нечем воспроизвести. Фиксируем всегда — см. docs/LOGGING.md.
						Log.Warn(string.Format("Нарушение ограничения (SQL {0}) в процедуре {1}: {2}",
							sqlEx.Number, GetProcedureName(sqlEx), sqlEx.Message));
						if (ex.Data != null)
							Log.Warn(ex.Data);

						try
						{
							ShowExclamation(msg);
						}
						catch (Exception exc)
						{
							Log.Error(string.Format("Ошибка в процедуре {0}", GetProcedureName(sqlEx)));
							Log.Error(sqlEx);
							Log.Error(ex.Data);

							// ── Заменяем locale-зависимую проверку Contains("REFERENCE")/Contains("DELETE")
							//    на прямую проверку по Number — единственный надёжный признак.
							if (sqlEx.Number == 547)
							{
								// Сообщение о невозможности удаления/изменения объекта
								UserMessage.ShowExclamation(Resources.DefaultCannotDeleteObject);
							}
							else
							{
								Log.Error(string.Format("Error {0} in {1}", sqlEx.Message, GetProcedureName(sqlEx)), sqlEx);
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
								Log.Error(string.Format("Ошибка в процедуре {0}", GetProcedureName(sqlEx)));
								Log.Error(sqlEx);
								Log.Error(ex.Data);
							}
							ShowExclamation(ex.Message);
						}
						catch (Exception exc)
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

		public static void ShowExclamation(string msgName)
		{
			Globals.ShowExclamation(msgName, null);
		}

		public static void ShowExclamation(string msgName, Dictionary<string, object> parameters)
		{
			Globals.ShowExclamation(msgName, parameters);
		}
	}
}
