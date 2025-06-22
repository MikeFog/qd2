using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm;
using Merlin.Controls;
using Merlin.Forms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace Merlin.Classes
{
    internal class PackModuleIssueInCampaignForm : PackModuleIssue
    {
        public PackModuleIssueInCampaignForm() : base(EntityManager.GetEntity((int)Entities.PackModuleIssueInCampaignForm))
        {
        }

        public PackModuleIssueInCampaignForm(DataRow row) : base(EntityManager.GetEntity((int)Entities.PackModuleIssueInCampaignForm), row)
        {
        }

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (actionName == Constants.Actions.Substitute)
                SubstituteRollerForSingleIssue(Roller);
            else
                base.DoAction(actionName, owner, interfaceObject);
        }
    }

    internal class PackModuleIssue : Issue
    {
        private PackModule _packModule;
        private Roller _roller;
        private PackModulePricelist _pricelist;

        public PackModuleIssue() : base(EntityManager.GetEntity((int)Entities.PackModuleIssue))
        {
        }

        public PackModuleIssue(DataRow row) : base(EntityManager.GetEntity((int)Entities.PackModuleIssue), row)
        {
        }

        protected PackModuleIssue(Entity entity) : base(entity)
        {
        }

        protected PackModuleIssue(Entity entity, DataRow row) : base(entity, row)
        {
        }

        private int RollerId
        {
            get { return int.Parse(this[Roller.ParamNames.RollerId].ToString()); }
        }

        internal Roller Roller
        {
            get
            {
                if (_roller == null)
                    _roller = new Roller(RollerId);
                return _roller;
            }
        }

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (actionName == Constants.Actions.Substitute)
            {
                if (entity.AttributeSelector == Issue.AttributeSelectorShort)
                    SubstituteRollerForSingleIssue(Roller);
                else
                    SubstituteRoller((Form)owner);
            }
            else if (actionName == Constants.Actions.PlayRoller)
                MediaControl.Current.Play(this);
            else if (string.Compare(actionName, Roller.ActionNames.ChangeAdvertType, StringComparison.OrdinalIgnoreCase) == 0)
                ChangeAdvertType((Form)owner);
            else
                base.DoAction(actionName, owner, interfaceObject);
        }

        private void SubstituteRoller(Form owner)
        {
            decimal price = decimal.Zero;

            if (Campaign != null && Campaign.Action != null)
            {
                Campaign.Action.Refresh();
                price = Campaign.Action.TotalPrice;
            }

            CampaignRoller.Substitute((Form)owner, Campaign, PackModuleID, null,
                       new Roller(int.Parse(this[Roller.ParamNames.RollerId].ToString())),
                       delegate
                       {
                           RecalculateAndShowPriceChange(price);
                           OnParentChanged(this, 1);
                       });
        }

        public override bool IsActionEnabled(string actionName, ViewType type)
        {
            if (actionName == Constants.Actions.Substitute)
                return !PackModulePricelist.HasRollerAssigned;
            if (actionName == ActionNames.SetFirst || actionName == ActionNames.SetSecond || actionName == ActionNames.SetLast || actionName == ActionNames.SetUnknow)
                return !PackModulePricelist.CheckTariffWithMaxCapacity() && base.IsActionEnabled(actionName, type);
            if (string.Compare(actionName, Roller.ActionNames.ChangeAdvertType, StringComparison.OrdinalIgnoreCase) == 0)
                return PackModulePricelist.HasRollerAssigned;
            return base.IsActionEnabled(actionName, type);
        }

        private void ChangeAdvertType(Form parentForm)
        {
            try
            {
                RollerChangeAdvertTypeForm form = new RollerChangeAdvertTypeForm(Roller, Campaign, null, PackModulePricelist.PackModuleId);
                if (form.ShowDialog(parentForm) == DialogResult.OK)
                {
                    Application.DoEvents();
                    Cursor.Current = Cursors.WaitCursor;
                    foreach (var date in form.SelectedDays)
                    {
                        Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();

                        procParameters[Roller.ParamNames.RollerId] = Roller.RollerId;
                        procParameters[Campaign.ParamNames.CampaignId] = Campaign.CampaignId;
                        procParameters[Pricelist.ParamNames.PricelistId] = PackModulePricelist.PricelistId;
                        procParameters[AdvertType.ParamNames.AdvertTypeId] = form.AdvertTypeId;
                        procParameters[Issue.ParamNames.IssueDate] = date;

                        DataAccessor.ExecuteNonQuery("SetAdvertTypeForCommmonRoller", procParameters);
                    }
                    Refresh();
                    OnObjectChanged(this);
                    OnParentChanged(this, 1);
                }
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
            finally
            {
                parentForm.Cursor = Cursors.Default;
            }
        }

        public int PackModuleID
        {
            get
            {
                //return (this["packmoduleID"] == null || this["packmoduleID"] == DBNull.Value) ? null : (int?)ParseHelper.ParseToInt32(this[PackModule.ParamNames.PackModuleId].ToString()); 
                return ParseHelper.ParseToInt32(this[PackModule.ParamNames.PackModuleId].ToString());
            }
        }

        public PackModule PackModule
        {
            get
            {
                if (_packModule == null)
                    _packModule = new PackModule(PackModuleID);
                return _packModule;
            }
        }

        public static Entity GetEntity()
        {
            return EntityManager.GetEntity((int)Entities.PackModuleIssue);
        }

        public override DateTime IssueDate
        {
            get { return ParseHelper.GetDateTimeFromObject(this[RollerIssue.ParamNames.IssueDate], DateTime.MinValue); }
        }

        public override bool Refresh()
        {
            _roller = null;
            return base.Refresh();
        }

        private PackModulePricelist PackModulePricelist
        {
            get
            {
                if (_pricelist == null)
                    _pricelist = new PackModulePricelist(int.Parse(this[Pricelist.ParamNames.PricelistId].ToString()));
                return _pricelist;
            }
        }

        protected override DataSet PrepareSubstitutionParametersAndExecute(Dictionary<string, object> procParameters)
        {
            procParameters[PackModule.ParamNames.PackModuleId] = PackModuleID;
            // Создадим специальную таблицу, небходимую для хранимой процедуры
            DataTable days = CreateTableWithDays(IssueDate);

            return DataAccessor.LoadDataSet("RollerSubstitute", procParameters, days);
        }
    }
}