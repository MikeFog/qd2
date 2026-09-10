using System.Data.SqlClient;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Веб-аналог <c>ErrorManager.PublishError</c> (<c>ErrorManager.WinForms.cs</c>) —
/// та же семантика разбора исключения и те же уровни логирования, только вместо
/// <c>MessageBox</c> возвращает готовый текст: страница сама решает, как его
/// показать (сейчас — <c>&lt;div class="alert"&gt;</c>, см. <c>Journal.razor</c>).
///
/// Само распознавание "какое сообщение показать" уже перенесено в ядро и не
/// дублируется — <see cref="ErrorManager.GetErrorMessage"/> (компилируется в
/// FogSoft.Core) делает и разбор бизнес-ключа через MessageAccessor, и разбор
/// имени нарушенного ограничения для SQL 547/2627/2601. Здесь воспроизведена
/// только вторая половина <c>PublishError</c> — какой уровень лога использовать —
/// потому что она осталась в UI-половине класса (<c>GetProcedureName</c> и
/// <c>ExtractConstraintName</c> там private, а сам метод завязан на
/// <c>Globals.ShowExclamation</c>/<c>Cursor.Current</c> — не годится для веба).
/// </summary>
public static class ErrorPresenter
{
	public static string Describe(Exception ex)
	{
		// Отказ по правам — нормальный отказ пользователю, а не сбой: он уже
		// записан одной строкой WARN в WebActionAuthorization, повторять его
		// здесь ERROR-ом со стеком незачем. Та же логика, что и для отказов по
		// бизнес-правилам (docs/LOGGING.md).
		if (ex is ActionNotAllowedException)
			return ex.Message;

		if (ex is SqlException sqlEx)
		{
			if (sqlEx.Number == 547 || sqlEx.Number == 2627 || sqlEx.Number == 2601)
			{
				// Нарушение ограничения — ожидаемый отказ пользователю, не сбой
				// приложения, но без записи в лог жалобу нечем воспроизвести
				// (тот же довод, что и в ErrorManager.WinForms.cs).
				ErrorManager.Log.Warn(string.Format(
					"Нарушение ограничения (SQL {0}) в процедуре {1}: {2}",
					sqlEx.Number, GetProcedureName(sqlEx), sqlEx.Message));
				if (ex.Data != null)
					ErrorManager.Log.Warn(ex.Data);
			}
			else if (MessageAccessor.GetMessage(ex.Message) == null)
			{
				// Не нарушение ограничения и не известный бизнес-ключ —
				// настоящая ошибка, логируем со стеком.
				ErrorManager.Log.Error(string.Format("Ошибка в процедуре {0}", GetProcedureName(sqlEx)));
				ErrorManager.Log.Error(sqlEx);
				if (ex.Data != null)
					ErrorManager.Log.Error(ex.Data);
			}
			// Известный бизнес-ключ логировать не нужно: для write-пути это уже
			// сделал DataAccessor.ExecuteNonQuery (WARN, см. IsHandledBusinessMessage).
		}
		else
		{
			ErrorManager.Log.Error(ex);
		}

		return ErrorManager.GetErrorMessage(ex);
	}

	/// <summary>
	/// Тот же приём, что и приватный ErrorManager.GetProcedureName: SqlException.Procedure
	/// пуст при ошибке внутри sp_executesql, тогда имя лежит в Exception.Data —
	/// его туда кладёт DataAccessor (ExecuteNonQuery/LoadDataSet, см. DataAccessor.cs).
	/// </summary>
	private static string GetProcedureName(SqlException ex)
	{
		if (!string.IsNullOrEmpty(ex.Procedure))
			return ex.Procedure;

		object? fromData = ex.Data?["Procedure"];
		return fromData?.ToString() ?? "<неизвестна>";
	}
}
