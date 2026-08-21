using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using System;
using System.Collections.Generic;
using System.Data;

namespace Merlin.Classes
{
    // UI-часть (DoAction, SetAdvertType-диалог) — в ActionRoller.WinForms.cs.
    // SubstituteRoller (в DoAction) — часть кластера замены ролика,
    // docs/tasks/web-migration-dialogs.md, §8 п.4, не тронут.
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

        // DoAction, SetAdvertType (диалог) и SubstituteRoller (кластер замены
        // ролика, §8 п.4, не разрезан) переехали в ActionRoller.WinForms.cs.

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
}
