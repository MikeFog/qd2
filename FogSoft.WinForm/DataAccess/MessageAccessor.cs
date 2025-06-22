using System;
using System.Collections.Generic;
using System.Data;

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
				
		private static readonly Dictionary<string, Message> messages =
			new Dictionary<string, Message>(StringComparer.InvariantCultureIgnoreCase);

		private static Dictionary<string, object> parameters;

		public static Dictionary<string, object> Parameters
		{
			set { parameters = value; }
		}

		public static string GetMessage(string name)
		{
			if(!messages.ContainsKey(name))
				if(!LoadMessage(name)) return null;

			Message msg = messages[name];
			string text = msg.Text;
			if (msg.Parameters != null && msg.Parameters.Length > 0)
			{
				object[] msgParameters = new object[msg.Parameters.Length];
				for(int i = 0; i < msgParameters.Length; i++)
					msgParameters[i] = parameters.ContainsKey(msg.Parameters[i]) ? parameters[msg.Parameters[i]] : null;

				return string.Format(text, msgParameters);
			}

			return msg.Text;
		}

		private static bool LoadMessage(string name)
		{

            if (_dsData == null)
                FullLoadDictionaries();

			name = name.Replace("'", "''");
            DataRow[] message = _dsData.Tables[0].Select(string.Format("[messageName] = '{0}'", name));
            DataRow[] rowsParams = _dsData.Tables[1].Select(string.Format("[messageName] = '{0}'", name));
			if (message.Length > 0)
			{
				messages[name] = new Message(message[0], rowsParams);
				return true;
			}
			return false;
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
			_dsData = DataAccessor.LoadDataSet("MessageLoad", new Dictionary<string, object>());
		}

		#endregion

		public static void ClearHash()
		{
			messages.Clear();
		}
	}
}