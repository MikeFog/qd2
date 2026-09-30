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

        // AssignNew и AssignMany (окна) — в PackageDiscountPriceList.WinForms.cs; данные для
        // карточки станций, проверка и запись — здесь, их зовут и десктоп, и веб.

        /// <summary>Имя именованного паспорта «Радиостанции» (iPassport).</summary>
        internal const string RadioStationsPassport = "PackDiscountRadiostations";

        /// <summary>
        /// «Добавить» открывает карточку станций галочками, пока у прайс-листа станций нет;
        /// когда уже есть — обычную карточку одной станции.
        /// </summary>
        internal bool NeedsRadioStationsAssignment => GetContent().Rows.Count == 0;

        /// <summary>
        /// Данные карточки станций: все радиостанции (набор «massmedia» для selector);
        /// накопленные прежде изменения набора сбрасываются.
        /// </summary>
        internal DataSet PrepareRadioStationsAssignment()
        {
            DataTable dt = EntityManager.GetEntity((int)Entities.MassMedia).GetContent();
            dt.TableName = "massmedia";
            DataSet ds = new DataSet();
            ds.Tables.Add(dt.Copy());
            childrenChangesList.Clear();
            return ds;
        }

        /// <summary>Проверка карточки станций: текст отказа или null.</summary>
        internal string ValidateRadioStationsAssignment(Dictionary<string, object> parameters)
        {
            if (!IsChecked(parameters, ParamNames.isForType1) && !IsChecked(parameters, ParamNames.isForType2)
                && !IsChecked(parameters, ParamNames.isForType3))
                return Tr.T(Properties.Resources.NoCampaignTypeSelected);
            if (SelectedRadioStations.Count == 0)
                return Tr.T(Properties.Resources.NoRadiostationSelected);
            return null;
        }

        private static bool IsChecked(Dictionary<string, object> parameters, string name) =>
            parameters.TryGetValue(name, out object value) && value is bool b && b;

        /// <summary>
        /// Черновик копии прайс-листа: значения исходного, но без ключа, с пометкой Clone и ссылкой на
        /// источник. Записывается паспортом (Update -> PackageDiscountPriceListIUD 'Clone'); радиостанции
        /// копирует процедура. Период по умолчанию — год, следующий за годом окончания исходного
        /// (Pricelist.GetClonePeriod).
        /// </summary>
        public override PresentationObject CreateCloneDraft()
        {
            PackageDiscountPriceList draft = new PackageDiscountPriceList { parameters = Parameters };
            draft.parameters[Constants.ParamNames.ActionName] = Constants.Actions.Clone;
            draft.parameters[ParamNames.sourcePackageDiscountPriceListId] = this[ParamNames.packageDiscountPriceListID];
            draft.parameters.Remove(ParamNames.packageDiscountPriceListID);

            if (this[ParamNames.finishDate] is DateTime finish)
            {
                Pricelist.GetClonePeriod(finish, out DateTime newStart, out DateTime newFinish);
                draft.parameters[ParamNames.startDate] = newStart;
                draft.parameters[ParamNames.finishDate] = newFinish;
            }
            return draft;
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

    /// <summary>
    /// «Добавить» у прайс-листа пакетной скидки снаружи сборки (веб):
    /// PackageDiscountPriceList internal.
    /// </summary>
    public static class PackageDiscountStations
    {
        public const string PassportName = PackageDiscountPriceList.RadioStationsPassport;

        public static bool NeedsAssignment(PresentationObject pricelist) =>
            ((PackageDiscountPriceList)pricelist).NeedsRadioStationsAssignment;

        public static DataSet Prepare(PresentationObject pricelist) =>
            ((PackageDiscountPriceList)pricelist).PrepareRadioStationsAssignment();

        public static string Validate(PresentationObject pricelist, Dictionary<string, object> parameters) =>
            ((PackageDiscountPriceList)pricelist).ValidateRadioStationsAssignment(parameters);

        public static void Apply(PresentationObject pricelist, Dictionary<string, object> parameters) =>
            ((PackageDiscountPriceList)pricelist).ApplyRadioStationsAssignment(parameters);
    }
}
