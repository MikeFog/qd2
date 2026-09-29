using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Classes.FakeContainers;
using System;
using System.Collections.Generic;

namespace Merlin.Classes
{
    internal partial class BalanceStatRow : PresentationObject
    {
        public BalanceStatRow() : base(EntityManager.GetEntity((int)Entities.StatBonuses))
        {

        }

        // DoAction (открытие окна журнала) — в BalanceStatRow.WinForms.cs; отбор — здесь.

        public const string OpenActionJournalAction = "OpenActionJournal";

        /// <summary>
        /// «Открыть журнал акций»: отбор журнала подтверждённых акций по строке бонусов —
        /// фирма (или группа компаний), группа станций, менеджер и период строки; период по
        /// дате создания или по датам акции — как выбрано в отборе бонусов. DBNull — поле
        /// отбора не используется.
        /// </summary>
        internal Dictionary<string, object> ActionJournalFilter()
        {
            var filter = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            if (parameters.ContainsKey("FirmId"))
                filter["firmID2"] = parameters["FirmId"];
            else
                filter["headCompanyID"] = parameters["headCompanyID"];
            filter["massmediaGroupID"] = parameters["massmediaGroupID"];
            filter["userID"] = parameters["userID"];
            var startDate = parameters["periodStartDate"];
            var finishDate = parameters["periodFinishDate"];
            if ((bool)parameters["selectByCreateDate"])
            {
                filter["createDateBegin"] = startDate;
                filter["createDateEnd"] = finishDate;
                filter["startOfInterval"] = DBNull.Value;
            }
            else
            {
                filter["startOfInterval"] = startDate;
                filter["endOfInterval"] = finishDate;
            }
            return filter;
        }
    }

    /// <summary>Строка журнала бонусов снаружи сборки (веб): BalanceStatRow internal.</summary>
    public static class BonusStatRow
    {
        public const string OpenActionJournalAction = BalanceStatRow.OpenActionJournalAction;

        /// <summary>Отбор журнала подтверждённых акций по строке; DBNull — поле не используется.</summary>
        public static Dictionary<string, object> ActionJournalFilter(PresentationObject row) =>
            ((BalanceStatRow)row).ActionJournalFilter();
    }
}
