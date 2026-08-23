using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using static Merlin.Classes.Campaign;

namespace Merlin.Classes
{
	// UI-часть (диалоги смены позиционирования и замены ролика) — в
	// CampaignPart.WinForms.cs. RecalculateAndShowPriceChange остаётся здесь:
	// уведомление идёт через UserInteraction.Notify, не напрямую через WinForms.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class CampaignPart : ObjectContainer
	{
        public const string OBJECT_ID = "objectID";
        private Campaign _campaign;

        protected CampaignPart(Entity entity, DataRow row) : base(entity, row)
		{
		}

		protected CampaignPart(Entity entity) : base(entity)
		{
		}

		// DoAction переехал в CampaignPart.WinForms.cs (параметр owner — IWin32Window).

		public void RecalculateAndShowPriceChange(decimal price)
		{
			Campaign?.RecalculateAction();

			decimal newPrice = ((Campaign != null && Campaign.Action != null) ? Campaign.Action.TotalPrice : decimal.Zero);
            ShowPriceChangeMessage(price, newPrice);
        }

        public static void ShowPriceChangeMessage(decimal price, decimal newPrice)
        {
            Dictionary<string, object> msgParameters =
                new Dictionary<string, object>(2, StringComparer.InvariantCultureIgnoreCase)
                {
                    ["oldPrice"] = price.ToString("c"),
                    ["newPrice"] = newPrice.ToString("c")
                };
            UserInteraction.Notify((newPrice == price) ? "CampaignPriceWithoutChanged" : "CampaignPriceChanged", msgParameters);
        }

		public override bool IsActionHidden(string actionName, ViewType type)
		{
			if (!ActionOnMassmedia.CheckLoggedUserRight(actionName, Campaign.Action))
				return true;
            if (actionName != Constants.EntityActions.Refresh)
                return IsMarkedAsDeleted || base.IsActionHidden(actionName, type);
            return base.IsActionHidden(actionName, type);
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (!ActionOnMassmedia.CheckLoggedUserRight(actionName, Campaign.Action))
				return false;

			return base.IsActionEnabled(actionName, type);
		}

        public bool IsMarkedAsDeleted
        {
            get { return parameters[ActionOnMassmedia.ParamNames.DeleteDate] != DBNull.Value; }
        }

		public Campaign Campaign
		{
			get
			{
				if (_campaign == null)
				{
					RefreshCampaign();
				}
				return _campaign;
			}
		}

		private void RefreshCampaign()
		{
			_campaign = Campaign.GetCampaignById(CampaignId);
		}

		public int CampaignId
		{
			get { return int.Parse(this[Campaign.ParamNames.CampaignId].ToString()); }
		}





        /// <summary>
        /// Кандидаты на замену ролика для одиночного выпуска: активные ролики
        /// фирмы, кроме текущего. null, если менять не на что.
        /// </summary>
        internal DataTable GetRollersForSubstitution(Roller currentRoller)
        {
            Entity eRoller = EntityManager.GetEntity((int)Entities.Roller);
            Dictionary<string, object> filter = DataAccessor.CreateParametersDictionary();
            filter["isActiveOnly"] = true;
            filter["withoutID"] = currentRoller.RollerId;
            filter[Action.ParamNames.FirmId] = Campaign.Action.FirmID;
            DataTable dt = eRoller.GetContent(filter);
            return dt.Rows.Count == 0 ? null : dt;
        }

        /// <summary>
        /// Меняет позиционирование у выбранных выпусков. Возвращает таблицу
        /// ошибок (пустую, если ошибок не было).
        /// </summary>
        internal DataTable ApplyPositionChanges(System.Collections.IEnumerable selectedIds, RollerPositions newPosition)
        {
            DataTable tableErrors = ErrorManager.CreateErrorsTable();

            Campaign.Action.Refresh();
            decimal price = Campaign.Action.TotalPrice;
            foreach (var id in selectedIds)
            {
                PresentationObject item = null;
                try
                {
                    Entity itemEntity = null;
                    Dictionary<string, object> parameters = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

                    if (Campaign.CampaignType == CampaignTypes.Simple || Campaign.CampaignType == CampaignTypes.Sponsor)
                    {
                        itemEntity = EntityManager.GetEntity((int)Entities.Issue);
                        parameters[Issue.ParamNames.IssueId] = id;
                    }
                    else if (Campaign.CampaignType == CampaignTypes.Module)
                    {
                        itemEntity = EntityManager.GetEntity((int)Entities.ModuleIssue);
                        parameters[ModuleIssue.ParamNames.ModuleIssueId] = id;
                    }
                    else if (Campaign.CampaignType == CampaignTypes.PackModule)
                    {
                        itemEntity = EntityManager.GetEntity((int)Entities.PackModuleIssue);
                        parameters[ModuleIssue.ParamNames.PackModuleIssueID] = id;
                    }
                    item = itemEntity.CreateObject(parameters);

                    item.Refresh();
                    // по любому это будет кто-то из наследников Issue, или сам Issue
                    ((Issue)item).SetPosition(newPosition);
                }
                catch (Exception ex)
                {

                    item.Refresh();
                    ErrorManager.AddErrorRow(tableErrors, DateTime.Parse(item[CampaignDay.ParamNames.IssueDate].ToString()), MessageAccessor.GetMessage(ex.Message));
                }
            }
            //OnParentChanged(this, 2);
            FireContainerRefreshed();
            RecalculateAndShowPriceChange(price);
            return tableErrors;
        }

        /// <summary>
        /// Заменяет ролик <paramref name="currentRoller"/> на <paramref name="newRoller"/>
        /// в текущем выпуске. Возвращает текст предупреждения от процедуры
        /// (например, о незаменённых роликах), если он есть.
        /// </summary>
        internal string ApplyRollerSubstitution(Roller currentRoller, Roller newRoller)
        {
            Campaign.Action.Refresh();
            decimal price = Campaign.Action.TotalPrice;

            Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
                Entity, InterfaceObjects.FakeModule, Constants.Actions.Substitute);
            procParameters["oldRollerId"] = currentRoller.RollerId;
            procParameters["oldDuration"] = currentRoller.Duration;
            procParameters["newRollerId"] = newRoller.RollerId;
            procParameters["newDuration"] = newRoller.Duration;
            procParameters[Campaign.ParamNames.CampaignId] = Campaign.CampaignId;
            procParameters[Campaign.ParamNames.CampaignTypeId] = (int)Campaign.CampaignType;
            procParameters[Issue.ParamNames.IssueId] = this[Issue.ParamNames.IssueId];

            DataSet ds = PrepareSubstitutionParametersAndExecute(procParameters);

            string warning = (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                ? ds.Tables[0].Rows[0]["message"].ToString()
                : null;
            //Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.RollerUnSubtitude), "Незамененные ролики", ds.Tables[0]);
            RecalculateAndShowPriceChange(price);
            //Refresh();
            OnParentChanged(this, EntityManager.GetEntity((int)Entities.GeneralCampaign));
            return warning;
        }

        protected virtual DataSet PrepareSubstitutionParametersAndExecute(Dictionary<string, object> procParameters)
        {
            procParameters[TariffWindow.ParamNames.OriginalWindowId] = this[TariffWindow.ParamNames.OriginalWindowId];
            return DataAccessor.DoAction(procParameters) as DataSet;
        }

        protected DataTable CreateTableWithDays(DateTime issueDate)
        {
            DataTable days = new DataTable("days");
            days.Columns.Add(TariffWindow.ParamNames.WindowId, Type.GetType("System.Int32"));
            days.Columns.Add(Issue.ParamNames.IssueDate, Type.GetType("System.DateTime"));
            object[] rowVals = new object[days.Columns.Count];
            rowVals[0] = DBNull.Value;
            rowVals[1] = issueDate;
            days.Rows.Add(rowVals);
            return days;
        }
    }
}
