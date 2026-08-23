using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Classes.FakeContainers;
using System;

namespace Merlin.Classes
{
    internal partial class BalanceStatRow : PresentationObject
    {
        public BalanceStatRow() : base(EntityManager.GetEntity((int)Entities.StatBonuses))
        {

        }

        // DoAction переехал в BalanceStatRow.WinForms.cs.
    }
}
