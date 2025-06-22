using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm.DataAccess
{
	public static class SqlBulkCopyHelper
	{
		public class Options
		{
			internal static readonly Options Default = new Options();

			private readonly Dictionary<Type, string> _clrToSqlDataTypes = new Dictionary<Type, string>();
			private readonly List<Type> _ignoredDataTypes = new List<Type>();
			private readonly string _collation;

			public Options()
				: this(string.Empty)
			{
			}

			public Options(string collation)
			{
				_clrToSqlDataTypes[typeof(byte)] = "tinyint";
				_clrToSqlDataTypes[typeof(int)] = "int";
				_clrToSqlDataTypes[typeof(decimal)] = "money";
				_clrToSqlDataTypes[typeof(bool)] = "bit";
				_clrToSqlDataTypes[typeof(string)] = "varchar(4000)";
				_clrToSqlDataTypes[typeof(DateTime)] = "datetime";
				_collation = collation;
			}

			public List<Type> IgnoredDataTypes
			{
				get { return _ignoredDataTypes; }
			}

			public string Collation
			{
				get { return _collation; }
			}

			public string GetSqlDataTypeFor(Type type)
			{
				if (IgnoredDataTypes.Contains(type)) return null;

				string dataType;
				if (_clrToSqlDataTypes.TryGetValue(type, out dataType)) return dataType;

				return "sql_variant";
			}
		}

		public static Options ConstructOptions(SqlTransaction transaction)
		{
			using (SqlCommand command = new SqlCommand("SELECT DATABASEPROPERTYEX(db_name(), 'Collation')  SQLCollation", transaction.Connection, transaction))
			{
				object oCollation = command.ExecuteScalar();
				return new Options(ParseHelper.GetStringFromObject(oCollation, string.Empty));
			}
		}

		public static void CopyToSqlTempTable(SqlTransaction transaction, DataTable table)
		{
			CopyToSqlTempTable(transaction, table, null);
		}

		public static void CopyToSqlTempTable(SqlTransaction transaction, DataTable table, Options options)
		{
			CopyToSqlTempTable(transaction, table, options, SqlBulkCopyOptions.Default);
		}

		public static void CopyToSqlTempTable(SqlTransaction transaction, DataTable table, SqlBulkCopyOptions sbcOptions)
		{
			CopyToSqlTempTable(transaction, table, null, sbcOptions);
		}

		// TODO: not completed
		public static void CopyToSqlTempTable(SqlTransaction transaction, DataTable table, Options options, SqlBulkCopyOptions sbcOptions)
		{
			if (transaction == null) throw new ArgumentNullException("transaction");

			SqlConnection connection = transaction.Connection as SqlConnection;
			
			if (connection == null)
				throw new InvalidOperationException("CopyToSqlTempTable requires SqlConnection.");

			options = options ?? Options.Default;

			string collation = GetCollation(options);
			
			StringBuilder sbCommandText = new StringBuilder();

			sbCommandText.AppendFormat(
				@"if OBJECT_ID('tempdb..#{0}') is not null drop table #{0};
					create table #{0} (", table.TableName);

			using (SqlBulkCopy copy = new SqlBulkCopy(connection, sbcOptions, transaction))
			{
				string columnSeparator = string.Empty;
				foreach (DataColumn column in table.Columns)
				{
					string dataType = options.GetSqlDataTypeFor(column.DataType);

					if (StringUtil.IsNullOrEmptyTrimmed(dataType)) continue;


					sbCommandText.Append(string.Concat(columnSeparator, column.ColumnName, " ", dataType,
					                                         column.DataType == typeof (string) ? collation : string.Empty));
					copy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
					columnSeparator = ", ";
				}
				sbCommandText.Append(")");

				using (SqlCommand command = new SqlCommand(sbCommandText.ToString(), connection, transaction))
				{
					command.ExecuteNonQuery();
				}

				copy.DestinationTableName = "#" + table.TableName;
				
				copy.WriteToServer(table);
			}
		}

		private static string GetCollation(Options options)
		{
			string collation = (options == null) ? null : options.Collation;

			if (string.IsNullOrEmpty(collation))
			{
				collation = ConfigurationUtil.GetSettings("SqlBulkCopy.DefaultCollation", collation);
			}

			return string.IsNullOrEmpty(collation) ? null : " Collate " + collation;
		}
	}
}