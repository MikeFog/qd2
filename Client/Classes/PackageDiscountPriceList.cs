using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using System;
using System.Collections.Generic;
using System.Data;

namespace Merlin.Classes
{
    // UI-часть (AssignNew, AssignMany, ValidatePassportData, ApplyPassportData —
    // делегаты контракта UniversalPassportForm) — в PackageDiscountPriceList.WinForms.cs.
    // Конвенция — docs/tasks/web-migration-dialogs.md.
    internal partial class PackageDiscountPriceList : ObjectContainer
    {
        private struct ParamNames
        {
            public const string isForType1 = "isForType1";
            public const string isForType2 = "isForType2";
            public const string isForType3 = "isForType3";
            public const string packageDiscountPriceListID = "packageDiscountPriceListID";
            public const string sourcePackageDiscountPriceListId = "sourcePackageDiscountPriceListId";
            public const string startDate = "startDate";
            public const string finishDate = "finishDate";
        }

        public PackageDiscountPriceList(int Id) : this()
        {
            this["packageDiscountPriceListID"] = Id;
            isNew = false;
            Refresh();
            iterator.ChildEntity = EntityManager.GetEntity((int)Entities.PackageDiscountMassmedia);
        }

        public PackageDiscountPriceList() : base(EntityManager.GetEntity((int)Entities.PackageDiscountPriceLists))
        {
        }

        public PackageDiscountPriceList(Entity entity, DataRow row) : base(entity, row)
        {
        }

        // AssignNew, AssignMany, ValidatePassportData переехали в
        // PackageDiscountPriceList.WinForms.cs. ValidatePassportData показывает
        // сообщение сама — так устроен делегатный контракт с UniversalPassportForm
        // (ValidateDataDelegate: bool(Dictionary<string,object>)), развести без
        // правки самой формы нельзя.

        /// <summary>
        /// Черновик копии прайс-листа: значения исходного, но без ключа, с пометкой Clone и ссылкой на
        /// источник. Записывается паспортом (Update -> PackageDiscountPriceListIUD 'Clone'); радиостанции
        /// копирует процедура. Период по умолчанию — следующий за исходным той же длины (см. CalcCloneFinishDate).
        /// </summary>
        public override PresentationObject CreateCloneDraft()
        {
            PackageDiscountPriceList draft = new PackageDiscountPriceList { parameters = Parameters };
            draft.parameters[Constants.ParamNames.ActionName] = Constants.Actions.Clone;
            draft.parameters[ParamNames.sourcePackageDiscountPriceListId] = this[ParamNames.packageDiscountPriceListID];
            draft.parameters.Remove(ParamNames.packageDiscountPriceListID);

            if (this[ParamNames.startDate] is DateTime start && this[ParamNames.finishDate] is DateTime finish)
            {
                DateTime newStart = finish.Date.AddDays(1);
                draft.parameters[ParamNames.startDate] = newStart;
                draft.parameters[ParamNames.finishDate] = CalcCloneFinishDate(start.Date, finish.Date, newStart);
            }
            return draft;
        }

        /// <summary>
        /// Окончание копии. Период из целых месяцев (с 1-го числа по последний день месяца) переносится
        /// на то же число месяцев — полный год даёт полный год, високосность не сбивает границу.
        /// Любой другой период — на то же число дней.
        /// </summary>
        internal static DateTime CalcCloneFinishDate(DateTime start, DateTime finish, DateTime newStart)
        {
            bool wholeMonths = start.Day == 1 && finish.AddDays(1).Day == 1;
            if (!wholeMonths)
                return newStart.AddDays((finish - start).TotalDays);

            int months = (finish.Year - start.Year) * 12 + finish.Month - start.Month + 1;
            return newStart.AddMonths(months).AddDays(-1);
        }

        /// <summary>Записывает выбранные радиостанции в пакетную скидку.</summary>
        internal void ApplyRadioStationsAssignment(Dictionary<string, object> parameters)
        {
            foreach (var rs in SelectedRadioStations)
            {
                PresentationObject po = new PresentationObject(EntityManager.GetEntity((int)Entities.PackageDiscountMassmedia))
                {
                    Parameters = parameters
                };
                po[Massmedia.ParamNames.MassmediaId] = rs.MassmediaId;
                po[ParamNames.packageDiscountPriceListID] = this[ParamNames.packageDiscountPriceListID];
                po.IsNew = true;
                po.Update();
            }
        }

        private List<Massmedia> SelectedRadioStations
        {
            get
            {
                List<Massmedia> radioStations = new List<Massmedia>();
                foreach (ChildrenChanges childrenChanges in childrenChangesList)
                {
                    foreach (PresentationObject po in childrenChanges.AddedObjects)
                    {
                        Massmedia rs = po as Massmedia;
                        if(po !=null)
                            radioStations.Add(rs);
                    }
                }

                return radioStations;
            }
        }
    }
}
