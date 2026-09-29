using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using System;
using System.Collections.Generic;
using System.Data;

namespace Merlin.Classes
{
    // UI-часть (DoAction, диалоги SetAdvertType и SubstituteRoller) — в
    // ActionRoller.WinForms.cs; запись замены по всей акции — здесь
    // (ApplyActionSubstitution), вход для веба — ActionRollerChange.
    // Конвенция — docs/tasks/web-migration-dialogs.md.
    internal partial class ActionRoller : Roller
    {
        public ActionRoller() : base(EntityManager.GetEntity((int)Entities.ActionRollers))
        {
        }

        protected ActionRoller(Entity entity) : base(entity) { }

        public ActionRoller(PresentationObject roller) : this()
        {
            parameters = roller.Parameters;
            isNew = false;
        }

        // DoAction и диалоги SetAdvertType / SubstituteRoller — в ActionRoller.WinForms.cs.

        internal ActionOnMassmedia ActionOfRoller() => new ActionOnMassmedia((int)this[Action.ParamNames.ActionId]);

        /// <summary>
        /// Замена этого ролика на <paramref name="newRoller"/> во всех кампаниях акции:
        /// у линейной и спонсорской — по дням кампании, у модульной — по каждому модулю,
        /// у пакетной — по каждому пакетному модулю. Возвращает таблицы незаменённых
        /// роликов (по одной на кампанию/модуль/пакет, только непустые). Пересчёт акции —
        /// за вызывающим.
        /// </summary>
        internal List<DataTable> ApplyActionSubstitution(ActionOnMassmedia action, Roller newRoller)
        {
            var result = new List<DataTable>();
            void Add(DataTable table)
            {
                if (table != null && table.Rows.Count > 0)
                    result.Add(table);
            }

            foreach (DataRow campaignRow in action.Campaigns().Rows)
            {
                CampaignOnSingleMassmedia campaign = new CampaignOnSingleMassmedia(campaignRow);
                if (campaign.CampaignType == Campaign.CampaignTypes.Simple ||
                    campaign.CampaignType == Campaign.CampaignTypes.Sponsor)
                    Add(CampaignRoller.ApplyRollerSubstitutionForDays(campaign, this, newRoller, campaign.Days(this), null, null));
                else if (campaign.CampaignType == Campaign.CampaignTypes.Module)
                {
                    CampaignModule campaignModule = new CampaignModule(campaign.CampaignId)
                    {
                        ChildEntity = EntityManager.GetEntity((int)Entities.CampaignModule)
                    };
                    foreach (DataRow moduleRow in campaignModule.GetContent().Rows)
                    {
                        Module module = new Module(moduleRow);
                        Add(CampaignRoller.ApplyRollerSubstitutionForDays(campaign, this, newRoller, campaign.Days(this), module.ModuleId, null));
                    }
                }
                else if (campaign.CampaignType == Campaign.CampaignTypes.PackModule)
                {
                    CampaignPackModule campaignPackModule = new CampaignPackModule(campaign.CampaignId)
                    {
                        ChildEntity = EntityManager.GetEntity((int)Entities.PackModuleInCampaign)
                    };
                    foreach (DataRow packModuleRow in campaignPackModule.GetContent().Rows)
                    {
                        PackModule packModule = new PackModule(packModuleRow);
                        Add(CampaignRoller.ApplyRollerSubstitutionForDays(campaign, this, newRoller, campaign.Days(this), null, packModule.PackModuleId));
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, "Unknown campaign type");
                }
            }
            return result;
        }

        public override bool IsActionEnabled(string actionName, ViewType type)
        {
            if (string.Compare(actionName, Constants.Actions.Substitute, StringComparison.OrdinalIgnoreCase) == 0)
                return !IsCommon && this[Action.ParamNames.ActionId] != null && StringUtil.IsDBNullOrEmpty(this[ParamNames.ParentId]);
            return base.IsActionEnabled(actionName, type);
        }

        /// <summary>
        /// Применяет назначение предмета рекламы (процедура ActionRollerSetAdvertType)
        /// и разбирает результат: либо простое обновление текущего объекта, либо
        /// замена на "клон" ролика (когда предмет назначен ролику "для всех фирм").
        /// </summary>
        internal void ApplyAdvertTypeChange(object advertTypeId, bool changeFlag)
        {
            Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
            procParameters[Roller.ParamNames.RollerId] = this[Roller.ParamNames.RollerId];
            procParameters[Action.ParamNames.ActionId] = this[Action.ParamNames.ActionId];
            procParameters[Firm.ParamNames.FirmId] = this[Firm.ParamNames.FirmId];
            procParameters[AdvertType.ParamNames.AdvertTypeId] = advertTypeId;
            procParameters[Roller.ParamNames.IsCommon] = this[Roller.ParamNames.IsCommon];
            procParameters[Roller.ParamNames.IsMute] = this[Roller.ParamNames.IsMute];
            procParameters[Roller.ParamNames.Duration] = this[Roller.ParamNames.Duration];
            procParameters["changeFlag"] = changeFlag;

            DataAccessor.ExecuteNonQuery("ActionRollerSetAdvertType", procParameters);
            if (IsRefreshAllSet)
                OnDataNeedRefresh();
            else
            {
                int newRollerId = int.Parse(procParameters["newRollerID"].ToString());

                // если была информация о том сколько раз использовался этот ролик, надо ее сохранить и добавить в новый объект
                int count = -1;
                if (parameters.ContainsKey("count"))
                    count = (int)parameters["count"];

                // если назначили предмет рекламы ролику "для всех фирм", то создастся его "клон" и вернется ID нового ролика
                if (RollerId == newRollerId)
                {
                    Refresh();
                    if (count >= 0) this["count"] = count;
                    OnObjectChanged(this);
                }
                else
                {
                    ReplaceRoller(newRollerId, count);
                }
            }
        }

        private void ReplaceRoller(int newRollerId, int count)
        {
            Roller roller = new Roller(newRollerId);
            // скопируем из старого ролика количество выходов
            if (count >= 0) roller["count"] = count;

            OnObjectCloned(CreateNewRoller(roller));
            OnObjectDeleted(this);
        }

        protected virtual ActionRoller CreateNewRoller(Roller roller)
        {
            var actionRoller = new ActionRoller
            {
                parameters = roller.Parameters,
                isNew = false
            };
            actionRoller[Action.ParamNames.ActionId] = this[Action.ParamNames.ActionId];
            actionRoller[Firm.ParamNames.FirmId] = this[Firm.ParamNames.FirmId];
            return actionRoller;
        }
    }

    /// <summary>
    /// Ролики акции (окно «Назначить предмет рекламы или заменить ролик») снаружи
    /// сборки (веб): ActionRoller и CommonRoller internal.
    /// </summary>
    public static class ActionRollerChange
    {
        public const string SetAdvertTypeAction = Action.ActionNames.SetAdvertType;
        public const string SubstituteAction = Constants.Actions.Substitute;

        /// <summary>Ролики акции с числом выпусков — то, что десктоп показывает в журнале 1244.</summary>
        public static DataTable Load(int actionId)
        {
            var filter = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase)
            {
                [Action.ParamNames.ActionId] = actionId,
            };
            return EntityManager.GetEntity((int)Entities.ActionRollers).GetContent(filter);
        }

        /// <summary>
        /// Назначение предмета рекламы. Ролику «для всех фирм» (CommonRoller) — без
        /// changeFlag, как в десктопе (CommonRoller.DoAction).
        /// </summary>
        public static void SetAdvertType(PresentationObject roller, object advertTypeId) =>
            ((ActionRoller)roller).ApplyAdvertTypeChange(advertTypeId, !(roller is CommonRoller));

        /// <summary>Кандидаты на замену — ролики фирмы акции.</summary>
        public static DataTable SubstituteCandidates(PresentationObject roller) =>
            ((ActionRoller)roller).ActionOfRoller().Firm.GetRollers();

        /// <summary>
        /// Замена во всей акции и пересчёт. Возвращает незаменённые ролики одной
        /// таблицей (null — всё заменено) и текст сообщения о цене акции.
        /// </summary>
        public static (DataTable Unsubstituted, string PriceMessage) Substitute(PresentationObject roller, int newRollerId)
        {
            ActionRoller actionRoller = (ActionRoller)roller;
            ActionOnMassmedia action = actionRoller.ActionOfRoller();
            // В десктопе цену подтягивает Firm того же объекта (выбор кандидатов); здесь
            // объект свежий — без Refresh TotalPrice был бы 0.
            action.Refresh();
            decimal price = action.TotalPrice;

            DataTable merged = null;
            foreach (DataTable table in actionRoller.ApplyActionSubstitution(action, new Roller(newRollerId)))
            {
                if (merged == null)
                    merged = table.Copy();
                else
                    merged.Merge(table);
            }

            action.Recalculate();
            return (merged, CampaignPart.PriceChangeText(price, action.TotalPrice));
        }
    }
}
