using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Merlin.Classes
{
    // DoAction переехал в PackModuleIssue.WinForms.cs (форма 3.1, структурно —
    // IWin32Window в сигнатуре, см. docs/tasks/web-migration-dialogs.md, §8, п.3).
    internal partial class PackModuleIssueInCampaignForm : PackModuleIssue
    {
        public PackModuleIssueInCampaignForm() : base(EntityManager.GetEntity((int)Entities.PackModuleIssueInCampaignForm))
        {
        }

        public PackModuleIssueInCampaignForm(DataRow row) : base(EntityManager.GetEntity((int)Entities.PackModuleIssueInCampaignForm), row)
        {
        }
    }

    // UI-часть (DoAction, ChangeAdvertType) — в PackModuleIssue.WinForms.cs.
    // SubstituteRoller (замена ролика) пока не разрезан — общий с
    // CampaignRoller.cs/ActionRoller.cs/CampaignModule.cs кластер, см.
    // docs/tasks/web-migration-dialogs.md, §8, п.4.
    // Конвенция — docs/tasks/web-migration-dialogs.md.
    internal partial class PackModuleIssue : Issue
    {
        private PackModule _packModule;
        private Roller _roller;
        private PackModulePricelist _pricelist;

        public PackModuleIssue() : base(EntityManager.GetEntity((int)Entities.PackModuleIssue))
        {
        }

        public PackModuleIssue(DataRow row) : base(EntityManager.GetEntity((int)Entities.PackModuleIssue), row)
        {
        }

        protected PackModuleIssue(Entity entity) : base(entity)
        {
        }

        protected PackModuleIssue(Entity entity, DataRow row) : base(entity, row)
        {
        }

        private int RollerId
        {
            get { return int.Parse(this[Roller.ParamNames.RollerId].ToString()); }
        }

        internal Roller Roller
        {
            get
            {
                if (_roller == null)
                    _roller = new Roller(RollerId);
                return _roller;
            }
        }

        // DoAction и SubstituteRoller (кластер замены ролика, §8 п.4) переехали
        // в PackModuleIssue.WinForms.cs целиком, без разреза.

        public override bool IsActionEnabled(string actionName, ViewType type)
        {
            if (actionName == Constants.Actions.Substitute)
                return !PackModulePricelist.HasRollerAssigned;
            if (actionName == ActionNames.SetFirst || actionName == ActionNames.SetSecond || actionName == ActionNames.SetLast || actionName == ActionNames.SetUnknow)
                return !PackModulePricelist.CheckTariffWithMaxCapacity() && base.IsActionEnabled(actionName, type);
            if (string.Compare(actionName, Roller.ActionNames.ChangeAdvertType, StringComparison.OrdinalIgnoreCase) == 0)
                return PackModulePricelist.HasRollerAssigned;
            return base.IsActionEnabled(actionName, type);
        }

        /// <summary>Меняет предмет рекламы у ролика на выбранные даты.</summary>
        internal void ApplyAdvertTypeChange(System.Collections.IEnumerable selectedDays, int advertTypeId)
        {
            foreach (var date in selectedDays)
            {
                Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();

                procParameters[Roller.ParamNames.RollerId] = Roller.RollerId;
                procParameters[Campaign.ParamNames.CampaignId] = Campaign.CampaignId;
                procParameters[Pricelist.ParamNames.PricelistId] = PackModulePricelist.PricelistId;
                procParameters[AdvertType.ParamNames.AdvertTypeId] = advertTypeId;
                procParameters[Issue.ParamNames.IssueDate] = date;

                DataAccessor.ExecuteNonQuery("SetAdvertTypeForCommmonRoller", procParameters);
            }
            Refresh();
            OnObjectChanged(this);
            OnParentChanged(this, 1);
        }

        public int PackModuleID
        {
            get
            {
                //return (this["packmoduleID"] == null || this["packmoduleID"] == DBNull.Value) ? null : (int?)ParseHelper.ParseToInt32(this[PackModule.ParamNames.PackModuleId].ToString()); 
                return ParseHelper.ParseToInt32(this[PackModule.ParamNames.PackModuleId].ToString());
            }
        }

        public PackModule PackModule
        {
            get
            {
                if (_packModule == null)
                    _packModule = new PackModule(PackModuleID);
                return _packModule;
            }
        }

        public static Entity GetEntity()
        {
            return EntityManager.GetEntity((int)Entities.PackModuleIssue);
        }

        public override DateTime IssueDate
        {
            get { return ParseHelper.GetDateTimeFromObject(this[RollerIssue.ParamNames.IssueDate], DateTime.MinValue); }
        }

        public override bool Refresh()
        {
            _roller = null;
            return base.Refresh();
        }

        private PackModulePricelist PackModulePricelist
        {
            get
            {
                if (_pricelist == null)
                    _pricelist = new PackModulePricelist(int.Parse(this[Pricelist.ParamNames.PricelistId].ToString()));
                return _pricelist;
            }
        }

        protected override DataSet PrepareSubstitutionParametersAndExecute(Dictionary<string, object> procParameters)
        {
            procParameters[PackModule.ParamNames.PackModuleId] = PackModuleID;
            // Создадим специальную таблицу, небходимую для хранимой процедуры
            DataTable days = CreateTableWithDays(IssueDate);

            return DataAccessor.LoadDataSet("RollerSubstitute", procParameters, days);
        }
    }
}