using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm.DataAccess
{
    public static class MessageAccessor
	{
		private class Message
		{
			public readonly string Text;
			public readonly string[] Parameters;

			public Message(DataRow message, DataRow[] parameters)
			{
				Text = message["message"].ToString();
				
				Parameters = new string[parameters.Length];
				int i = 0;
				foreach (DataRow row in parameters)
					Parameters[i++] = row["name"].ToString();
			}

			public Message(DataSet ds)
			{
				int i = 0;

				Text = ds.Tables[0].Rows[0]["message"].ToString();
				if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
				{
					Parameters = new string[ds.Tables[1].Rows.Count];
					foreach (DataRow row in ds.Tables[1].Rows)
						Parameters[i++] = row["name"].ToString();
				}
			}
		}
				
		// Кэши общие на процесс, а в вебе к ним обращаются запросы разных пользователей
		// одновременно: словарь потокобезопасный, загрузка справочника — под замком
		// (docs/tasks/side-defects.md, №5).
		private static readonly ConcurrentDictionary<string, Message> messages =
			new ConcurrentDictionary<string, Message>(StringComparer.InvariantCultureIgnoreCase);

		private static readonly object loadLock = new object();

		// Параметры подстановки — свои у каждого потока выполнения (в вебе — у запроса
		// пользователя), а не одно поле на процесс: иначе в текст ошибки попадали
		// значения чужого запроса. В десктопе поток один, поведение прежнее.
		private static readonly AsyncLocal<IDictionary<string, object>> parameters =
			new AsyncLocal<IDictionary<string, object>>();

		// Параметры упавшего вызова процедуры, привязанные к его исключению
		// (RememberFailedCall). Отдельная таблица, а не Exception.Data: Data выводится
		// в лог целиком (ErrorManager.PublishError, ErrorPresenter).
		private static readonly ConditionalWeakTable<Exception, IDictionary<string, object>> failedCallParameters =
			new ConditionalWeakTable<Exception, IDictionary<string, object>>();

		public static Dictionary<string, object> Parameters
		{
			set { parameters.Value = value; }
		}

		public static string GetMessage(string name)
		{
			return Format(name, parameters.Value);
		}

		/// <summary>
		/// Текст сообщения для ошибки <paramref name="source"/>: подстановка из параметров
		/// того вызова процедуры, который её бросил, — даже если после него были другие
		/// обращения к базе или исключение пришло из другого потока выполнения (веб:
		/// работа под BusyService.RunAsync). Исключение не из DataAccessor — как
		/// <see cref="GetMessage(string)"/>.
		/// </summary>
		public static string GetMessage(string name, Exception source)
		{
			IDictionary<string, object> callParameters;
			if (source == null || !failedCallParameters.TryGetValue(source, out callParameters))
				callParameters = parameters.Value;
			return Format(name, callParameters);
		}

		/// <summary>
		/// Вызов процедуры упал: его параметры становятся текущими (как DoAction до вызова)
		/// и запоминаются при исключении — снимком, вызывающий может переиспользовать словарь.
		/// </summary>
		internal static void RememberFailedCall(Exception ex, IDictionary<string, object> callParameters)
		{
			parameters.Value = callParameters;
			if (ex == null || callParameters == null || failedCallParameters.TryGetValue(ex, out _))
				return;

			Dictionary<string, object> snapshot = callParameters is Dictionary<string, object> dictionary
				? new Dictionary<string, object>(dictionary, dictionary.Comparer)
				: new Dictionary<string, object>(callParameters);
			failedCallParameters.GetValue(ex, _ => snapshot);
		}

		private static string Format(string name, IDictionary<string, object> values)
		{
			Message msg;
			if (!messages.TryGetValue(name, out msg))
			{
				msg = LoadMessage(name);
				if (msg == null) return null;
			}

			// Перевод шаблона — до подстановки параметров (веб, docs/tasks/web-i18n.md);
			// в десктопе переводчика нет, текст остаётся как в iMessage.
			string text = Tr.T(msg.Text);
			if (msg.Parameters != null && msg.Parameters.Length > 0)
			{
				object[] msgParameters = new object[msg.Parameters.Length];
				for(int i = 0; i < msgParameters.Length; i++)
				{
					object value;
					msgParameters[i] = values != null && values.TryGetValue(msg.Parameters[i], out value) ? value : null;
				}

				return string.Format(text, msgParameters);
			}

			return text;
		}

		private static Message LoadMessage(string name)
		{
			// Справочник грузится вне замка (сетевой вызов не должен держать ошибки всех
			// пользователей в очереди); FullLoadDictionaries публикует его под замком.
			if (_dsData == null)
				FullLoadDictionaries();

			lock (loadLock)
			{
				string escaped = name.Replace("'", "''");
				DataRow[] message = _dsData.Tables[0].Select(string.Format("[messageName] = '{0}'", escaped));
				DataRow[] rowsParams = _dsData.Tables[1].Select(string.Format("[messageName] = '{0}'", escaped));
				if (message.Length > 0)
				{
					Message msg = new Message(message[0], rowsParams);
					messages[name] = msg;
					return msg;
				}
				return null;
			}
            //else
                //throw new NullReferenceException(string.Format("Cannot find Message with Name: {0}", name));

			/*
            if (ConfigurationUtil.IsFullLoadDictionaries)
			{
				if (_dsData == null)
					FullLoadDictionaries();

				DataRow[] message = _dsData.Tables[0].Select(string.Format("[messageName] = '{0}'", name));
				DataRow[] rowsParams = _dsData.Tables[1].Select(string.Format("[messageName] = '{0}'", name));
				if (message.Length > 0)
					messages[name] = new Message(message[0], rowsParams);
				else
					throw new NullReferenceException(string.Format("Cannot find Message with Name: {0}", name));
			}
			else
			{
				Dictionary<string, object> cmdParameters =
					new Dictionary<string, object>(1, StringComparer.InvariantCultureIgnoreCase) {{"name", name}};

				DataSet ds = DataAccessor.LoadDataSet("MessageLoad", cmdParameters);
				if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
					messages[name] = new Message(ds);
				else 
					throw new NullReferenceException(string.Format("Cannot find Message with Name: {0}", name));
			}
			*/
		}

		#region Full Load

		private static DataSet _dsData = null;

		public static void FullLoadDictionaries()
		{
			DataSet ds = DataAccessor.LoadDataSet("MessageLoad", new Dictionary<string, object>());
			lock (loadLock)
				_dsData = ds;
		}

		#endregion

		public static void ClearHash()
		{
			messages.Clear();
		}
	}
}