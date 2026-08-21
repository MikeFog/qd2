using FogSoft.WinForm.Classes;
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
