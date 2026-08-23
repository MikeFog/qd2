using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using System.Collections.Generic;

namespace Merlin.Classes
{
    // UI-часть (DoAction, SetAdvertType) — в ActionRollerInStatJournal.WinForms.cs.
    // Конвенция — docs/tasks/web-migration-dialogs.md.
    internal partial class ActionRollerInStatJournal : PresentationObject
    {
        public ActionRollerInStatJournal() : base(EntityManager.GetEntity((int)Entities.RollerStatistic))
        {
        }

        // DoAction и SetAdvertType переехали в ActionRollerInStatJournal.WinForms.cs.

        /// <summary>Можно ли назначить предмет рекламы. false — <paramref name="errorMessage"/> заполнен.</summary>
        internal bool CanSetAdvertType(out string errorMessage)
        {
            if (bool.Parse(parameters[Roller.ParamNames.IsCommon].ToString()) || parameters[Roller.ParamNames.ParentId] != System.DBNull.Value)
            {
                errorMessage = Properties.Resources.ImpossibleSetAdvertType;
                return false;
            }
            errorMessage = null;
            return true;
        }

        /// <summary>Назначает предмет рекламы выбранному ролику.</summary>
        internal void ApplyAdvertTypeChange(object advertTypeId, string advertTypeName)
        {
            Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
            procParameters[Roller.ParamNames.RollerId] = parameters[Roller.ParamNames.RollerId];
            procParameters[Firm.ParamNames.FirmId] = parameters[Firm.ParamNames.FirmId];
            procParameters[AdvertType.ParamNames.AdvertTypeId] = advertTypeId;
            procParameters[Roller.ParamNames.IsCommon] = parameters[Roller.ParamNames.IsCommon];
            procParameters[Roller.ParamNames.IsMute] = parameters[Roller.ParamNames.IsMute];
            procParameters[Roller.ParamNames.Duration] = parameters[Roller.ParamNames.Duration];

            DataAccessor.ExecuteNonQuery("ActionRollerSetAdvertType", procParameters);
            this[Roller.ParamNames.AdvertTypeName] = advertTypeName;
            OnObjectChanged(this);
        }
    }
}
