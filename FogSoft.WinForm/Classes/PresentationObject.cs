using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows.Forms;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Forms;

namespace FogSoft.WinForm.Classes
{
	public class PresentationObject : IActionHandler
	{
		public event ObjectDelegate ObjectCreated;
		public event ObjectDelegate ObjectDeleted;
		public event ObjectDelegate ObjectChanged;
		public event ObjectDelegate ObjectCloned;
		public event ObjectParentChange ParentChanged;
        public event ObjectParentChange2 ParentChanged2;
		public event EmptyDelegate RefreshAllData;

        #region Constants -------------------------------------

        private const string DELETE_PROMPT = "Эта операция приведет к удалению объекта '{0}' из системы. Продолжить?";
		private const string DETACH_PROMPT = "Вы действительно хотите отсоединить объект '{0}'?";

		#endregion

		#region Members ---------------------------------------

		protected Dictionary<string, object> parameters =
			new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

		protected Dictionary<string, object> PKparameters =
			new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

		protected Entity entity;
		protected bool isNew = true;

		#endregion

		#region Constructors ----------------------------------

		public PresentationObject(int entityId, DataRow row)
			: this(EntityManager.GetEntity(entityId), row) {}

		public PresentationObject(int entityId, Dictionary<string, object> parameters)
			: this(EntityManager.GetEntity(entityId))
		{
			this.parameters = parameters;
		}

        public PresentationObject(Entity entity, Dictionary<string, object> parameters)
            : this(entity)
        {
            this.parameters = parameters;
        }

		public PresentationObject(Entity entity, DataRow row)
			: this(entity)
		{
			Init(row);
		}

		public PresentationObject(Entity entity)
		{
			this.entity = entity;
			SetDefaultValues();
		}

		private void SetDefaultValues()
		{
			foreach(KeyValuePair<string, ColumnInfo> kvp in entity.ColumnsInfo)
				if(kvp.Value.ColumnDefault != null)
					this[kvp.Key] = kvp.Value.ColumnDefault;
		}

		public object this[string key]
		{
			get
			{
				if (parameters.ContainsKey(key))
					return parameters[key];
				return null;
			}
			set { parameters[key] = value; }
		}

		#endregion

		public Dictionary<string, object> Parameters
		{
			get { return ClonePatameters(); }
			set
			{
				parameters = value;
				isNew = false;
			}
		}

		protected Dictionary<string, object> ClonePatameters()
		{
			Dictionary<string, object> res =
				new Dictionary<string, object>(parameters.Count, StringComparer.InvariantCultureIgnoreCase);
			foreach(KeyValuePair<string, object> kvp in parameters)
				res.Add(kvp.Key, kvp.Value);
			return res;
		}

		public virtual bool ShowPassport(IWin32Window parentForm)
		{
			try
			{
				if(!entity.HasPassport) return false;

				// load data to display Passport
				Dictionary<string, object> procParameters = Parameters;
				DataAccessor.PrepareParameters(
					procParameters, entity, InterfaceObjects.PropertyPage, Constants.Actions.Load);

				DataSet ds = null;
				if(DataAccessor.IsProcedureExist(procParameters))
				{
					ds = DataAccessor.DoAction(procParameters) as DataSet;
				}

				bool isNewObject = IsNew;

				PassportForm passport = GetPassportForm(ds);
				bool res = (passport.ShowDialog(parentForm) == DialogResult.OK || passport.IsApplyClicked);

				// Fire event only if existing object was changed
				if(res && !isNewObject) OnObjectChanged(this);
				return res;
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
				return false;
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		public virtual PassportForm GetPassportForm(DataSet ds)
		{
			return new PassportForm(this, ds);
		}

		public virtual bool Update()
		{
			Dictionary<string, object> procParameters = Parameters;

			string actionName;

			if (procParameters.ContainsKey(Constants.ParamNames.ActionName) && 
				(string)procParameters[Constants.ParamNames.ActionName] == Constants.Actions.Clone)
				actionName = Constants.Actions.Clone;
			else
				actionName = (IsNew ? Constants.Actions.AddItem : Constants.Actions.Update);

			DataAccessor.PrepareParameters(procParameters, entity,
			                               InterfaceObjects.FakeModule,
			                               actionName);

			// Add initial values for PK columns
			foreach(KeyValuePair<string, object> kvp in PKparameters)
				procParameters[kvp.Key] = kvp.Value;

			try
			{
                DataSet ds = (DataSet)DataAccessor.DoAction(procParameters, out Dictionary<string, object> outParams);
                foreach (KeyValuePair<string, object> kvp in outParams)
					this[kvp.Key] = kvp.Value;

				if (ds != null) Init(ds.Tables[0].Rows[0]);
				isNew = false;
				return true;
			}
			catch
			{
				// Если Update не состоится, тогда в объекте будет неверная информация.
				if (string.Compare(actionName, Constants.Actions.Update) == 0)
					Refresh();
				throw;
			}
		}

		public virtual PresentationObject Clone(Dictionary<string, object> newParameters)
		{			
			DataAccessor.PrepareParameters(newParameters, entity, InterfaceObjects.FakeModule, Constants.Actions.Clone);
            DataSet ds = (DataSet)DataAccessor.DoAction(newParameters, out Dictionary<string, object> outParams);

            foreach (KeyValuePair<string, object> kvp in outParams)
				newParameters[kvp.Key] = kvp.Value;

			PresentationObject presentationObject = entity.NewObject;
			if(ds != null)
				presentationObject.Init(ds.Tables[0].Rows[0]);
			else
				presentationObject.Parameters = newParameters;

			OnObjectCloned(presentationObject);
			return presentationObject;
		}

		public virtual void Detach(bool silenceFlag)
		{
			if(silenceFlag || ConfirmDetach())
			{
				ExecuteAction(Constants.Actions.Detach);
				OnObjectDeleted(this);
			}
		}

		public virtual void Detach()
		{
			Detach(false);
		}

		/// <summary>
		/// Delete object from the DB with confirmation
		/// </summary>
		public virtual bool Delete()
		{
			return Delete(false);
		}

		/// <summary>
		/// Delete object from the DB
		/// </summary>
		/// <param name="silenceFlag">This flag indicates is user will be asked to confirm</param>
		public virtual bool Delete(bool silenceFlag)
		{
			if(silenceFlag || ConfirmDelete())
			{
				ExecuteAction(Constants.Actions.Delete);
				isNew = true;
				OnObjectDeleted(this);
				return true;
			}
			return false;
		}

		private void ExecuteAction(string actionName)
		{
			Dictionary<string, object> procParameters = Parameters;

			DataAccessor.PrepareParameters(procParameters, entity, InterfaceObjects.FakeModule, actionName);
			foreach(KeyValuePair<string, object> kvp in PKparameters)
				procParameters[kvp.Key] = kvp.Value;
			DataAccessor.DoAction(procParameters);
		}

		public virtual bool Refresh(InterfaceObjects interfaceObject)
		{
			if(!IsNew)
			{
				DataAccessor.PrepareParameters(parameters, entity, interfaceObject, Constants.Actions.Load);
				DataSet ds = (DataSet) DataAccessor.DoAction(parameters, true);
				if (ds.Tables[Constants.TableNames.Data].Rows.Count <= 0)
					return false;
				Init(ds.Tables[Constants.TableNames.Data].Rows[0]);
				OnObjectChanged(this);
			}
			return true;
		}

		public virtual bool Refresh()
		{
			return Refresh(InterfaceObjects.SimpleJournal);
		}

		protected virtual bool ConfirmDelete()
		{
			bool result = MessageBox.ShowQuestion(DeleteConfirmationText) == DialogResult.Yes;
			Application.DoEvents();
			return result;
		}

		protected virtual bool ConfirmDetach()
		{
			bool result = MessageBox.ShowQuestion(string.Format(DETACH_PROMPT, Name)) == DialogResult.Yes;
			Application.DoEvents();
			return result;
		}

		protected virtual string DeleteConfirmationText
		{
			get { return string.Format(DELETE_PROMPT, Name); }
		}

		public Entity Entity
		{
			get { return entity; }
		}

		public bool IsNew
		{
			get { return isNew; }
			set { isNew = value; }
		}

		public virtual string Name
		{
			get
			{
				if(parameters.ContainsKey(Constants.Parameters.Name))
					return parameters[Constants.Parameters.Name].ToString();
				else
					return string.Empty;
			}
		}

		public object[] IDs
		{
			get
			{
				object[] res = new object[entity.PKColumns.Length];
				for (int i = 0; i < res.Length; i++)
				{
					if (parameters.ContainsKey(entity.PKColumns[i]))
						res[i] = parameters[entity.PKColumns[i]];
					else
						res[i] = null;
				}
				return res;
			}
		}

		public string Key
		{
			get
			{
				string key = "";
				foreach(object id in IDs)
					key += id + "_";
				if(key.Length > 0)
					key = key.Remove(key.Length - 1, 1);
				return key;
			}
		}

		public virtual void Init(DataRow row)
		{
			DataTable dataTable = row.Table;
			parameters = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
			PKparameters = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

			// Copy object's attributes from the data row into Hashtable
			foreach(DataColumn column in dataTable.Columns)
			{
				parameters.Add(column.ColumnName, row[column]);
				// Save copy of PK in the dedicated container
				if(IsPKColumn(column.ColumnName))
					PKparameters.Add(column.ColumnName + "Old", row[column]);
			}
			isNew = false;
		}

		/// <summary>
		/// Returns boolean flag is given column is PK column
		/// </summary>
		/// <param name="columnName"></param>
		/// <returns></returns>
		private bool IsPKColumn(string columnName)
		{
			for(int i = 0; i < entity.PKColumns.Length; i++)
				if(columnName == entity.PKColumns[i]) return true;
			return false;
		}

		#region IActionHandler Members ------------------------

		public Entity.Action[] ActionList
		{
			get { return entity.ActionList; }
		}

		public virtual void DoAction(
			string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch(actionName)
			{
				case Constants.EntityActions.Delete:
					Delete();
					break;
				case Constants.EntityActions.ShowPassport:
					ShowPassport(owner);
					break;
				case Constants.EntityActions.Refresh:
					Refresh(interfaceObject);
					break;
				case Constants.EntityActions.Detach:
					Detach();
					break;
			}
		}

		public virtual bool IsActionEnabled(string actionName, ViewType type)
		{
			return entity.IsActionEnabled(actionName, type);
		}

		public virtual bool IsActionHidden(string actionName, ViewType type)
		{
			return false;
		}

		#endregion

		protected void OnParentChanged(PresentationObject presentationObject, int parentDepth)
		{
            ParentChanged?.Invoke(presentationObject, parentDepth);
        }

        protected void OnParentChanged(PresentationObject presentationObject, Entity parentEntity)
        {
            ParentChanged2?.Invoke(presentationObject, parentEntity);
        }

        protected void OnObjectCloned(PresentationObject presentationObject)
		{
			ObjectCloned?.Invoke(presentationObject);
		}

		protected void OnObjectCreated(PresentationObject presentationObject)
		{
			ObjectCreated?.Invoke(presentationObject);
		}

		protected void OnObjectDeleted(PresentationObject presentationObject)
		{
			ObjectDeleted?.Invoke(presentationObject);
		}

		protected void OnDataNeedRefresh()
		{
			RefreshAllData?.Invoke();
        }

		protected void OnObjectChanged(PresentationObject presentationObject)
		{
			ObjectChanged?.Invoke(presentationObject);
		}

		public override bool Equals(object obj)
		{
            // Two presentation objects are equal if both have the same Id and 
            // belong to the same Entity
            if (!(obj is PresentationObject presentationObject)) return false;
            return (GetHashCode() == presentationObject.GetHashCode());
		}

		public override int GetHashCode()
		{
			int hash = 0;
			for(int i = 0; i < IDs.Length; i++)
				if (IDs[i] != null)
					hash += IDs[i].GetHashCode();

			return entity.GetHashCode() + hash;
		}

		public string PKWhereClause
		{
			get
			{
				StringBuilder clause = new StringBuilder();
				for(int i = 0; i < entity.PKColumns.Length; i++)
				{
					string columnName = entity.PKColumns[i];
					if (!string.IsNullOrEmpty(columnName))
					{
						if (i > 0) clause.Append(" And ");
						clause.AppendFormat("{0}='{1}'", columnName, this[columnName]);
					}
				}
				return clause.ToString();
			}
		}

		public bool Equal(PresentationObject po)
		{
			if (po == null ||  this.entity.Id != po.entity.Id) return false;
			for (int i = 0; i < IDs.Length; i++)
				if (IDs[i] != po.IDs[i]) return false;
			return true;
		}

		protected bool IsRefreshAllSet 
		{ 			
			get
			{
				return RefreshAllData != null;
			}
        }

        /// <summary>
        /// Load single object from database using primary key values
        /// </summary>
        /// <param name="pkValues">Primary key values dictionary</param>
        /// <returns>Loaded presentation object or null if not found</returns>
        public virtual PresentationObject LoadSingleObject(Dictionary<string, object> pkValues)
        {
            if (pkValues == null)
                throw new ArgumentNullException(nameof(pkValues));

            // Set PK values to current object
            foreach (KeyValuePair<string, object> kvp in pkValues)
                this[kvp.Key] = kvp.Value;

            // Load data from database
            DataTable dataTable = entity.LoadSingleObject(this);

            if (dataTable == null || dataTable.Rows.Count == 0)
                return null;

            // Create and initialize object with correct type through Entity
            return entity.CreateObject(dataTable.Rows[0]);
        }
    }
}