using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using System;
using System.Collections.Generic;

namespace Merlin.Classes
{
    /// <summary>
    /// Строка «Реестра счетов» (сущность 1272 BillRegister): счёт = акция + агентство. Нужна ради действия
    /// «Открыть журнал акций» (ПКМ): переход к акции строки в журнал активированных акций или в журнал
    /// макетов — по статусу акции. DoAction (окно журнала) — в BillRegisterRow.WinForms.cs; отбор — здесь.
    /// По образцу BalanceStatRow (журнал бонусов, сущность 1269).
    /// </summary>
    internal partial class BillRegisterRow : PresentationObject
    {
        public BillRegisterRow() : base(EntityManager.GetEntity((int)Entities.BillRegister))
        {
        }

        public const string OpenActionJournalAction = "OpenActionJournal";

        /// <summary>Акция активирована (isConfirmed в результате BillRegister), иначе макет.</summary>
        internal bool IsActionConfirmed => parameters.ContainsKey("isConfirmed")
            && parameters["isConfirmed"] != DBNull.Value && Convert.ToBoolean(parameters["isConfirmed"]);

        /// <summary>
        /// Отбор журнала акций по строке: только № акции. Даты и менеджер не задаём — журнал не должен
        /// спрятать акцию периодом по умолчанию; права на акции процедуры журнала проверяют сами.
        /// </summary>
        internal Dictionary<string, object> ActionJournalFilter()
        {
            return new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase)
            {
                ["actionID"] = parameters["actionID"]
            };
        }
    }

    /// <summary>Строка реестра счетов снаружи сборки (веб): BillRegisterRow internal.</summary>
    public static class BillRegisterRowActions
    {
        public const string OpenActionJournalAction = BillRegisterRow.OpenActionJournalAction;

        /// <summary>Акция строки активирована (иначе макет).</summary>
        public static bool IsActionConfirmed(PresentationObject row) => ((BillRegisterRow)row).IsActionConfirmed;

        /// <summary>Отбор журнала акций по строке.</summary>
        public static Dictionary<string, object> ActionJournalFilter(PresentationObject row) =>
            ((BillRegisterRow)row).ActionJournalFilter();
    }
}
