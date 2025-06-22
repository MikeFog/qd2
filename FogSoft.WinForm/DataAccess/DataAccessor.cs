using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using FogSoft.WinForm.Classes;
using Microsoft.ApplicationBlocks.Data;
using Microsoft.Extensions.Caching.Memory;

namespace FogSoft.WinForm.DataAccess
{
	/// <summary>
	/// Data Accessor Layer class
	/// </summary>
	public static class DataAccessor
	{
		#region Internal Class --------------------------------

		private class ProcedureConfig
		{
			public readonly string ProcedureName;
			public readonly string ProcedureType;
			public readonly int ConnectionTimeout;
			public readonly bool IsTransactionRequired;
			public readonly int CachingTime;
			private readonly string[] tableAliases;

			public ProcedureConfig(DataRow row, ICollection<DataRow> tableAliaseRows)
			{
				ProcedureType = row["procedureType"].ToString();
				ProcedureName = row["name"].ToString();
				ConnectionTimeout = ParseHelper.ParseToInt32(row["connectionTimeout"].ToString());
				IsTransactionRequired = ParseHelper.ParseToBoolean(row["isTransactionRequired"].ToString());
				CachingTime = ParseHelper.ParseToInt32(row["cachingTime"].ToString());

                if (tableAliaseRows.Count == 0)
				{
					tableAliases = new string[1];
					tableAliases[0] = Constants.TableNames.Data;
				}
				else
				{
					tableAliases = new string[tableAliaseRows.Count];
					int i = 0;
					foreach (DataRow aliasRow in tableAliaseRows)
					{
						tableAliases[i++] = aliasRow["name"].ToString();
					}
				}
			}

			public void ProcessDataSet(DataSet ds)
			{
				for (int i = 0; i < ds.Tables.Count; i++)
				{
					ds.Tables[i].TableName = tableAliases.Length > i ? tableAliases[i] : i.ToString();
				}
			}
		}

		#endregion

		public const int DEFAULT_TIMEOUT = 30;
		private struct ParamNames
		{
			public const string LoggedUserID = "loggedUserId";
        }


		/// <summary>
		/// Main Connection String
		/// </summary>
		public static string ConnectionString
		{
			get
			{
				return ConfigurationUtil.ConnectionStringMain.ConnectionString;
			}
		}

		private static readonly Dictionary<string, ProcedureConfig> procedureConfigs = new Dictionary<string, ProcedureConfig>();
        private static readonly Dictionary<string, ProcedureConfig> procedureConfigsByName = new Dictionary<string, ProcedureConfig>();
        private static readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        [ThreadStatic] private static SqlTransaction _transaction;
        [ThreadStatic] private static SqlParameter[] _commandParameters;

        public static void ClearHash()
		{
			procedureConfigs.Clear();
            procedureConfigsByName.Clear();
            SqlHelperParameterCache.ClearParamCache();
		}

		public static void LoadProcedureConfig()
		{
			DataSet ds = LoadDataSet("ProcedureConfigurationRetrieve", null);
			// коллекция процедур с доступом по ключу
			foreach (DataRow row in ds.Tables[0].Rows)
			{
				string key =
					string.Format("{0}_{1}_{2}", row[Constants.ParamNames.EntityId],
					              row[Constants.ParamNames.ActionName],
					              row[Constants.ParamNames.ModuleId]);
				string storedProcedureId = row["storedProcedureID"].ToString();
				DataRow[] selectedRows = ds.Tables[1].Select("storedProcedureID=" + storedProcedureId);
				ProcedureConfig config = new ProcedureConfig(row, selectedRows);
				procedureConfigs.Add(key, config);
            }
            // коллекция процедур с доступом по имени процедуры
            foreach (DataRow row in ds.Tables[2].Rows)
            {
                string storedProcedureId = row["storedProcedureID"].ToString();
                DataRow[] selectedRows = ds.Tables[1].Select("storedProcedureID=" + storedProcedureId);
                ProcedureConfig config = new ProcedureConfig(row, selectedRows);
                procedureConfigsByName.Add(config.ProcedureName, config);
            }
        }
		
		public static void BeginTransaction()
		{
			SqlConnection connection = new SqlConnection(ConnectionString);
			connection.Open();
			_transaction = connection.BeginTransaction();
        }

        public static void CommitTransaction()
        {
            _transaction.Commit();
			_transaction = null;
        }

        public static void RollbackTransaction()
        {
            _transaction.Rollback();
            _transaction = null;
        }

        public static object DoAction(Dictionary<string, object> parameters, bool forceFlag = false)
		{
			parameters[ParamNames.LoggedUserID] = SecurityManager.LoggedUser.Id;
			MessageAccessor.Parameters = parameters;

			// find stored procedure by name
			string key = GetProcKey(parameters);
			ProcedureConfig config = procedureConfigs[key];
			if (config.ProcedureType == "RECORDSET")
			{
				DataSet ds = LoadDataSet(config.ProcedureName, parameters, config.ConnectionTimeout, config.IsTransactionRequired, config.CachingTime, forceFlag);
                config.ProcessDataSet(ds);
				return ds;
			}
			if (config.ProcedureType == "NO_DATA")
				ExecuteNonQuery(config, parameters);
			return null;
		}

		public static object DoAction(Dictionary<string, object> parameters, out Dictionary<string, object> outputValues)
		{
			object result = DoAction(parameters);
			outputValues = GetOutParameters();
			return result;
		}

		public static bool IsProcedureExist(Dictionary<string, object> parameters)
		{
			string key = GetProcKey(parameters);
			return procedureConfigs.ContainsKey(key);
		}

		public static XmlDocument LoadXml(string procedureName)
		{
			XmlDocument xmlDoc = new XmlDocument();
			using (SqlConnection connection = new SqlConnection(ConnectionString))
			{
				connection.Open();
				string xmlString = null;
				XmlReader xmlReader = SqlHelper.ExecuteXmlReader(connection, procedureName, DEFAULT_TIMEOUT);
				xmlReader.MoveToContent();
				while (!xmlReader.EOF)
				{
					xmlString += xmlReader.ReadOuterXml();
				}
				xmlReader.Close();
				connection.Close();
				xmlDoc.LoadXml("<root>" + xmlString + "</root>");
			}
			return xmlDoc;
		}

		public static DataSet LoadDataSet(string procedureName, Dictionary<string, object> parameters)
		{
			return LoadDataSet(procedureName, parameters, DEFAULT_TIMEOUT, false);
		}

		public static DataSet LoadDataSet(string procedureName, Dictionary<string, object> parameters, int timeout)
		{
			return LoadDataSet(procedureName, parameters, timeout, false);
		}

		public static object ExecuteScalar(string procedureName, Dictionary<string, object> parameters)
		{
			return ExecuteScalar(procedureName, parameters, false);
		}

		public static object ExecuteScalar(string procedureName, Dictionary<string, object> parameters, bool isTransactionRequired)
		{
			try
			{
                parameters[ParamNames.LoggedUserID] = SecurityManager.LoggedUser.Id;
                using (SqlConnection connection = new SqlConnection(ConnectionString))
				{
                    DateTime t = DateTime.Now;
                    connection.Open();
					_commandParameters = AssignSqlParameters(connection, procedureName, parameters);

					object res = null;
					if (isTransactionRequired)
					{
						if (ConfigurationUtil.IsUseCustomTransaction)
						{
							throw new NotImplementedException("Cannot ExecuteScalar with custom Transactions");
						}
						else
						{
							SqlTransaction transaction = connection.BeginTransaction();
							try
							{
								
								res = SqlHelper.ExecuteScalar(transaction, CommandType.StoredProcedure, procedureName, DEFAULT_TIMEOUT, _commandParameters);
								transaction.Commit();
                                
                            }
							catch
							{
								transaction.Rollback();
								throw;
							}
						}
					}
					else
					{
						res = SqlHelper.ExecuteScalar(connection, CommandType.StoredProcedure, procedureName, DEFAULT_TIMEOUT, _commandParameters);
					}
					connection.Close();
					WriteExecutionTime(t, procedureName);
                    return res;
				}
			}
			catch (Exception exp)
			{
				if (parameters != null && exp.Data != null)
				{
					foreach (SqlParameter parameter in _commandParameters)
						exp.Data.Add("Parameter: " + parameter.ParameterName, parameter.Value);
				}
				throw;
			}
		}

		public static object ExecuteSQLScalar(string sqltext)
		{
			using (SqlConnection connection = new SqlConnection(ConnectionString))
			{
				connection.Open();

				object res = SqlHelper.ExecuteScalar(connection, CommandType.Text, sqltext, DEFAULT_TIMEOUT);

				connection.Close();
				return res;
			}
		}

		public static void ExecuteNonQuery(string procedureName, Dictionary<string, object> parameters)
		{
			ExecuteNonQuery(procedureName, parameters, DEFAULT_TIMEOUT, true);
		}

		private static string GetProcKey(IDictionary<string, object> parameters)
		{
			return string.Format("{0}_{1}_{2}",
			                     parameters[Constants.ParamNames.EntityId],
			                     parameters[Constants.ParamNames.ActionName],
			                     parameters[Constants.ParamNames.ModuleId]);
		}

		private static Dictionary<string, object> GetOutParameters()
		{
			Dictionary<string, object> retOutParameters =
				new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

			for (int index = 0; index < _commandParameters.Length; index++)
			{
				if (_commandParameters[index].Direction != ParameterDirection.Input)
				{
					string paramName = _commandParameters[index].ParameterName.Substring(1);
					retOutParameters.Add(paramName, _commandParameters[index].Value);
				}
			}
			return retOutParameters;
		}

		private static DataSet LoadDataSet(
			string procedureName, IDictionary<string, object> parameters,
			int connectionTimeout, bool isTransactionRequired, int cachingTime = 0, bool forceFlag = false)
		{
			try
			{
				if(parameters != null && SecurityManager.LoggedUser != null)
					parameters[ParamNames.LoggedUserID] = SecurityManager.LoggedUser.Id;
                DataSet ds;

				using (SqlConnection connection = new SqlConnection(ConnectionString))
				{
					connection.Open();
					_commandParameters = AssignSqlParameters(connection, procedureName, parameters);
					if (isTransactionRequired)
					{
                        DateTime t = DateTime.Now;
                        if (ConfigurationUtil.IsUseCustomTransaction)
						{
							ds = SqlHelper.ExecuteDataset(connection, CommandType.Text, CreateSQLwithTransaction(procedureName),
														  connectionTimeout, _commandParameters);
                        }
						else
						{
							SqlTransaction transaction = null;
							if (_transaction == null)
								transaction = connection.BeginTransaction();
							try
							{
								ds = SqlHelper.ExecuteDataset(_transaction ?? transaction, CommandType.StoredProcedure, procedureName, connectionTimeout, _commandParameters);
								transaction?.Commit();
							}
							catch
							{
								transaction?.Rollback();
								throw;
							}
						}
                        WriteExecutionTime(t, procedureName);
                    }
					else
					{
						DateTime t = DateTime.Now;
						
						var key = procedureName + GetKeyFromParameters(_commandParameters);
						if (!forceFlag && _cache.TryGetValue(key, out DataSet value))
							ds = value;
						else
						{
                            ds = SqlHelper.ExecuteDataset(connection, CommandType.StoredProcedure, procedureName, connectionTimeout, _commandParameters);
							if (cachingTime > 0)
								_cache.Set(key, ds, TimeSpan.FromSeconds(cachingTime));
						}
                        WriteExecutionTime(t, procedureName);
                    }
					connection.Close();
				}
                procedureConfigsByName.TryGetValue(procedureName, out ProcedureConfig config);
                config?.ProcessDataSet(ds);
                return ds;
			}
			catch(Exception exp)
			{
				if (parameters != null && exp.Data != null)
				{
					foreach (KeyValuePair<string, object> parameter in parameters)
						exp.Data.Add("Parameter: " + parameter.Key, parameter.Value);
				}
				throw;
			}
		}

		private static void ExecuteNonQuery(ProcedureConfig config, Dictionary<string, object> parameters)
		{
			ExecuteNonQuery(
				config.ProcedureName, parameters, config.ConnectionTimeout, config.IsTransactionRequired);
		}

		public static void ExecuteNonQuery(string procedureName, Dictionary<string, object> parameters, int connectionTimeout, bool isTransactionRequired)
		{
			try
			{
				parameters[ParamNames.LoggedUserID] = SecurityManager.LoggedUser.Id;

                MessageAccessor.Parameters = parameters;
                using (SqlConnection connection = new SqlConnection(ConnectionString))
				{
                    DateTime t = DateTime.Now;
                    connection.Open();
					_commandParameters = AssignSqlParameters(connection, procedureName, parameters);

					if (isTransactionRequired)
					{
						
						if (ConfigurationUtil.IsUseCustomTransaction)
						{
							SqlHelper.ExecuteNonQuery(connection, CommandType.Text, CreateSQLwithTransaction(procedureName),
							                          connectionTimeout,
							                          _commandParameters);
						}
						else
						{
							SqlTransaction transaction = null;

							if (_transaction == null)
								transaction = connection.BeginTransaction();
							try
							{
								SqlHelper.ExecuteNonQuery(_transaction ?? transaction, CommandType.StoredProcedure, procedureName, connectionTimeout, _commandParameters);
								transaction?.Commit();
							}
							catch
							{
								transaction?.Rollback();
								throw;
							}
						}
                    }
					else
					{
						SqlHelper.ExecuteNonQuery(connection, CommandType.StoredProcedure, procedureName, connectionTimeout, _commandParameters);
					}
					connection.Close();
					WriteExecutionTime(t, procedureName);
                }

                foreach (KeyValuePair<string, object> kvp in GetOutParameters())
					parameters[kvp.Key] = kvp.Value;
			}
			catch(Exception exp)
			{
				if (parameters != null && exp.Data != null)
				{
					exp.Data.Add("Procedure", procedureName);
                    foreach (SqlParameter parameter in _commandParameters)
						exp.Data.Add("Parameter: " + parameter.ParameterName, parameter.Value);
				}
				throw;
			}
		}

		private static string CreateSQLwithTransaction(string procedureName)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendFormat("begin tran {0}", Environment.NewLine);
			builder.AppendFormat("set nocount on {0}", Environment.NewLine);
			builder.Append(
				@"
if @@trancount <= 0 
begin 
	raiserror('TransactionError', 16, 1)
	return
end
if @@error != 0
begin 
	rollback tran
	return
end 
		");
			builder.AppendFormat("exec {0} {1}", procedureName, Environment.NewLine);
			for (int i = 0; i < _commandParameters.Length; i++)
			{
				builder.AppendFormat("{0} = {0} {1} {2} {3}", _commandParameters[i].ParameterName
				                     , (_commandParameters[i].Direction == ParameterDirection.Output
				                        || _commandParameters[i].Direction == ParameterDirection.InputOutput)
				                       	? "output"
				                       	: string.Empty
				                     , i == (_commandParameters.Length - 1) ? string.Empty : ",", Environment.NewLine);
			}
			builder.Append(
				@"
	if @@trancount > 0 
	begin 
		if @@error = 0 commit tran else 	rollback tran
	end 
	else 
		raiserror('TransactionError', 16, 1)");
			return builder.ToString();
		}

		/// <summary>
		/// Load DataSet, execute in transaction with copy table by bulk copy
		/// </summary>
		/// <param name="procedureName"></param>
		/// <param name="parameters"></param>
		/// <param name="table"></param>
		/// <returns></returns>
		public static DataSet LoadDataSet(
			string procedureName, Dictionary<string, object> parameters, DataTable table)
		{
			DataSet ds = new DataSet();
			ds.Tables.Add(table);
			return LoadDataSet(procedureName, parameters, DEFAULT_TIMEOUT, ds);
		}

		/// <summary>
		/// Load DataSet, execute in transaction with copy table by bulk copy
		/// </summary>
		/// <param name="procedureName"></param>
		/// <param name="parameters"></param>
		/// <param name="connectionTimeout"></param>
		/// <param name="dataSet">Таблица загруженная на сервер при помощи bulk - copy</param>
		/// <returns></returns>
		public static DataSet LoadDataSet(
			string procedureName, IDictionary<string, object> parameters, int connectionTimeout, DataSet dataSet)
		{
			try
			{
				DataSet ds;
                if (parameters != null && SecurityManager.LoggedUser != null)
                    parameters[ParamNames.LoggedUserID] = SecurityManager.LoggedUser.Id;

                using (SqlConnection connection = new SqlConnection(ConnectionString))
				{
					connection.Open();
					_commandParameters = AssignSqlParameters(connection, procedureName, parameters);
					if (ConfigurationUtil.IsUseCustomTransaction)
						throw new NotImplementedException();

					using (SqlTransaction transaction = connection.BeginTransaction())
					{
						DateTime t = DateTime.Now;
						try
						{
							foreach (DataTable table in dataSet.Tables)
							{
								SqlBulkCopyHelper.CopyToSqlTempTable(transaction, table, SqlBulkCopyHelper.ConstructOptions(transaction));
							}
							ds = SqlHelper.ExecuteDataset(
								transaction, CommandType.StoredProcedure, procedureName, connectionTimeout, _commandParameters);
							transaction.Commit();
						}
						catch
						{
							transaction.Rollback();
							throw;
						}
                        WriteExecutionTime(t, procedureName);
                    }

					connection.Close();
				}
                procedureConfigsByName.TryGetValue(procedureName, out ProcedureConfig config);
                config?.ProcessDataSet(ds);
                return ds;
			}
			catch(Exception exp)
			{
				if (parameters != null && exp.Data != null)
				{
					foreach (KeyValuePair<string, object> parameter in parameters)
						exp.Data.Add("Parameter: " + parameter.Key, parameter.Value);
				}
				throw;
			}
		}

		// Creates Sql Parameters array from Collection with parameters
		public static SqlParameter[] AssignSqlParameters(SqlConnection connection, string procedureName,
		                                                  IDictionary<string, object> parameterValues)
		{
			SqlParameter[] cmdParameters =
				SqlHelperParameterCache.GetSpParameterSet(connection, procedureName);

			SqlParameter[] cmdParametersWithValues = new SqlParameter[parameterValues == null
			                                                          	? 0
			                                                          	: Math.Max(parameterValues.Count, cmdParameters.Length)];
			int j = 0;
			for (int i = 0; i < cmdParameters.Length; i++)
			{
				string parameterName = cmdParameters[i].ParameterName.Substring(1);
				if (parameterValues != null)
				{
					if (parameterValues.ContainsKey(parameterName))
					{
						cmdParameters[i].Value = parameterValues[parameterName];
					}
					if (parameterValues.ContainsKey(parameterName)
					    || cmdParameters[i].Direction != ParameterDirection.Input)
					{
						cmdParametersWithValues[j++] = cmdParameters[i];
					}
				}
			}

			SqlParameter[] cmdParametersWithValuesToReturn = new SqlParameter[j];
			for (int i = 0; i < j; i++)
				cmdParametersWithValuesToReturn[i] = cmdParametersWithValues[i];

			return cmdParametersWithValuesToReturn;
		}

		public static Dictionary<string, object> CreateParametersDictionary()
		{
			return new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
		}

		public static Dictionary<string, object> PrepareParameters(Entity entity)
		{
			return PrepareParameters(entity, InterfaceObjects.SimpleJournal, Constants.Actions.Load);
		}

		public static Dictionary<string, object> PrepareParameters(
			Entity entity,
			InterfaceObjects interfaceObject, string action)
		{
			Dictionary<string, object> parameters = CreateParametersDictionary();
			PrepareParameters(parameters, entity, interfaceObject, action);
			return parameters;
		}

		public static void PrepareParameters(
			Dictionary<string, object> parameters, Entity entity,
			InterfaceObjects interfaceObject, string action)
		{
			parameters[Constants.ParamNames.EntityId] = entity.Id.ToString();
			parameters[Constants.ParamNames.ModuleId] = ((int) interfaceObject).ToString();
			parameters[Constants.ParamNames.ActionName] = action;
		}

        public static string GetKeyFromParameters(SqlParameter[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return string.Empty;

            // Сортируем параметры по имени, чтобы одинаковые массивы давали одинаковый результат
            var sortedParameters = parameters.OrderBy(p => p.ParameterName).ToArray();

            // Создаем строку, объединяя свойства каждого параметра
            var concatenated = string.Join(";", sortedParameters.Select(p =>
                $"{p.ParameterName}:{p.SqlDbType}:{p.Size}:{p.Value}"));

            // Вычисляем хэш (MD5, SHA256 или другой алгоритм)
            using (var hashAlgorithm = SHA256.Create())
            {
                var hashBytes = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(concatenated));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

		private static void WriteExecutionTime(DateTime startTime, string procedureName)
		{
            var diff = (DateTime.Now - startTime).TotalMilliseconds;
            if (diff > 1000)
                ErrorManager.Log.Info(string.Format("{0} {1}", procedureName, diff));
        }
    }
}