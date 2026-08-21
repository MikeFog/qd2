using FogSoft.WinForm.DataAccess;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FogSoft.WinForm.Classes
{
    // Показ ошибок пользователю (PublishError, ShowExclamation) вынесен
    // в ErrorManager.WinForms.cs — см. docs/tasks/web-migration.md, этап 0.
    public static partial class ErrorManager
    {
        // ── Удалены 8 locale-зависимых строковых констант (EN+RU ключевые слова).  ──
        // ── Единственная точка знания о "правильных" именах — префиксы ниже.       ──
        private static readonly string[] _knownConstraintPrefixes =
            { "FK_", "PK_", "CK_", "UQ_", "UIX_", "IX_" };

        public static readonly ILog Log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Возвращает имя ограничения/индекса для использования как ключ в MessageAccessor.
        /// Опирается на <see cref="SqlException.Number"/> и кавычки в тексте — не зависит от локали SQL Server.
        /// </summary>
        private static string ExtractConstraintName(SqlException ex)
        {
            // Универсальный поиск: ищем имя объекта в кавычках по известным префиксам.
            if (TryExtractQuotedObjectName(ex.Message, out string name))
                return name; // например "FK_Orders_Customers" → ключ в MessageAccessor

            // Fallback по Number, когда имя объекта в тексте не найдено.
            if (ex.Number == 547)
                return "DefaultCannotDeleteObject"; // ключ в MessageAccessor / Resources

            // 2627 / 2601: отдаём сырой текст — Globals.ShowExclamation покажет
            // его через GetMessage, а при null — Resources.ApplicationError.
            return ex.Message;
        }

        /// <summary>
        /// Ищет все подстроки в одинарных или двойных кавычках в <paramref name="message"/>
        /// и возвращает первый кандидат, начинающийся с одного из известных префиксов
        /// (FK_, PK_, CK_, UQ_, UIX_, IX_).
        /// Не содержит ни одного EN/RU ключевого слова — работает на любой локали SQL Server.
        /// </summary>
        private static bool TryExtractQuotedObjectName(string message, out string name)
        {
            // SQL Server всегда заключает имена объектов в кавычки, независимо от локали.
            // Пример (EN): ...constraint "FK_Orders_Customers"...
            // Пример (RU): ...ограничением REFERENCE "FK_Orders_Customers"...
            var matches = Regex.Matches(message, @"['""]([^'""]+)['""]");
            foreach (Match m in matches)
            {
                string candidate = m.Groups[1].Value;
                foreach (string prefix in _knownConstraintPrefixes)
                {
                    if (candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        name = candidate;
                        return true;
                    }
                }
            }
            name = null;
            return false;
        }

        /// <summary>
        /// Оставлен для обратной совместимости (публичный метод).
        /// Теперь делегирует в общий helper <see cref="TryExtractQuotedObjectName"/>.
        /// </summary>
        public static string GetSqlIndexName(SqlException ex)
        {
            TryExtractQuotedObjectName(ex.Message, out string name);
            return name ?? string.Empty;
        }

        public static void LogError(string messageError, Exception e)
        {
            Log.Error(messageError, e);
        }

        public static string GetErrorMessage(Exception ex)
        {   
            string message = MessageAccessor.GetMessage(ex.Message);
            if(message != null)
                return message;
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
