using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using System;
using System.Data;

namespace Merlin.Classes
{
    // Набор объёмных скидок радиостанции на период («Дата принятия скидок», сущность 22).
    // До появления клонирования жил как обычный ObjectContainer.
    // UI-часть (DoAction) — в DiscountRelease.WinForms.cs.
    // Конвенция — docs/tasks/web-migration-dialogs.md.
    internal partial class DiscountRelease : ObjectContainer
    {
        private struct ParamNames
        {
            public const string discountReleaseID = "discountReleaseID";
            public const string sourceDiscountReleaseID = "sourceDiscountReleaseID";
            public const string startDate = "startDate";
            public const string finishDate = "finishDate";
        }

        public DiscountRelease() : base(EntityManager.GetEntity((int)Entities.DiscountRelease))
        {
        }

        public DiscountRelease(Entity entity, DataRow row) : base(entity, row)
        {
        }

        /// <summary>
        /// Черновик копии набора скидок: значения исходного, но без ключа, с пометкой Clone и ссылкой
        /// на источник. Записывается паспортом (Update -> DiscountReleaseIUD 'Clone'); суммы и проценты
        /// копирует процедура. Период по умолчанию — год, следующий за годом окончания исходного
        /// (Pricelist.GetClonePeriod).
        /// </summary>
        public override PresentationObject CreateCloneDraft()
        {
            DiscountRelease draft = new DiscountRelease { parameters = Parameters };
            draft.parameters[Constants.ParamNames.ActionName] = Constants.Actions.Clone;
            draft.parameters[ParamNames.sourceDiscountReleaseID] = this[ParamNames.discountReleaseID];
            draft.parameters.Remove(ParamNames.discountReleaseID);
            if (this[ParamNames.finishDate] is DateTime finish)
            {
                Pricelist.GetClonePeriod(finish, out DateTime newStart, out DateTime newFinish);
                draft.parameters[ParamNames.startDate] = newStart;
                draft.parameters[ParamNames.finishDate] = newFinish;
            }
            return draft;
        }
    }
}
