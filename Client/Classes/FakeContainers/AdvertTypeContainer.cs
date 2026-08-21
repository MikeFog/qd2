using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes.FakeContainers
{
    internal partial class AdvertTypeContainer : FakeContainer
    {
        private struct ActionNames
        {
            public const string ShowTree = "ShowTree";
            public const string ShowFlat = "ShowFlat";
        }
        private static readonly Entity.Action[] menu = new[]
        {
            new Entity.Action(ActionNames.ShowTree, "Показать с группировакой"),
            new Entity.Action(ActionNames.ShowFlat, "Показать без группировки"),
            new Entity.Action(Constants.EntityActions.AddNew, "Создать новый предмет рекламы"),
            new Entity.Action(null, "-"),
            new Entity.Action(Constants.EntityActions.Refresh, "Обновить", Constants.ActionsImages.Refresh)
        };

        public AdvertTypeContainer() : base("Предметы рекламы", menu, RelationManager.GetScenario(RelationScenarios.AdvertTypes))
        {

        }

        public override bool IsActionEnabled(string actionName, ViewType type)
        {
            if (actionName == ActionNames.ShowTree)
                return childEntity.Id != (int)Entities.AdvertType;
            if (actionName == ActionNames.ShowFlat)
                return childEntity.Id != (int)Entities.AdvertTypeChild;
            if (actionName == Constants.EntityActions.AddNew)
                return childEntity.IsActionEnabled(actionName, type);
            return base.IsActionEnabled(actionName, type);
        }

        // DoAction переехал в AdvertTypeContainer.WinForms.cs.
    }
}
