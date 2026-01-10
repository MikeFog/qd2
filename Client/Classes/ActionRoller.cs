using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;

namespace Merlin.Classes
{
    internal class ActionRoller : Roller
    {
        public ActionRoller() : base(EntityManager.GetEntity((int)Entities.ActionRollers))
        {
        }

        protected ActionRoller(Entity entity) : base(entity) { }

        public ActionRoller(PresentationObject roller) : this()
        {
            parameters = roller.Parameters;
            isNew = false;
        }

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (actionName.Equals(Action.ActionNames.SetAdvertType, System.StringComparison.OrdinalIgnoreCase))
                SetAdvertType((Form)owner, true);
            else if (string.Compare(actionName, Constants.Actions.Substitute, StringComparison.OrdinalIgnoreCase) == 0)
                SubstituteRoller((Form)owner);
            else
                base.DoAction(actionName, owner, interfaceObject);
        }

        public override bool IsActionEnabled(string actionName, ViewType type)
        {
            if (string.Compare(actionName, Constants.Actions.Substitute, StringComparison.OrdinalIgnoreCase) == 0)
                return !IsCommon && this[Action.ParamNames.ActionId] != null && StringUtil.IsDBNullOrEmpty(this[ParamNames.ParentId]);
            return base.IsActionEnabled(actionName, type);
        }

        private void SubstituteRoller(Form owner)
        {
            try
            {
                Entity entity = EntityManager.GetEntity((int)Entities.Roller);
                ActionOnMassmedia action = new ActionOnMassmedia((int)this[Action.ParamNames.ActionId]);
                
                SelectionForm form = new SelectionForm(entity, action.Firm.GetRollers().DefaultView, "Замена ролика");
                if (form.ShowDialog(owner) == DialogResult.OK)
                {
                    owner.UseWaitCursor = true;
                    Application.DoEvents();

                    var newRollerId = (int)form.SelectedObject.IDs[0];
                    decimal price = action.TotalPrice;

                    foreach (DataRow campaignrRow in action.Campaigns().Rows)
                    {
                        CampaignOnSingleMassmedia campaign = new CampaignOnSingleMassmedia(campaignrRow);
                        if (campaign.CampaignType == Campaign.CampaignTypes.Simple ||
                            campaign.CampaignType == Campaign.CampaignTypes.Sponsor)
                            CampaignRoller.Subtitute(campaign, this, new Roller(newRollerId), campaign.Days(this), null, null);
                        else if (campaign.CampaignType == Campaign.CampaignTypes.Module)
                        {
                            CampaignModule campaignModule = new CampaignModule(campaign.CampaignId)
                            {
                                ChildEntity = EntityManager.GetEntity((int)Entities.CampaignModule)
                            };
                            foreach (DataRow moduleRow in campaignModule.GetContent().Rows)
                            {
                                Module module = new Module(moduleRow);
                                CampaignRoller.Subtitute(campaign, this, new Roller(newRollerId), campaign.Days(this), module.ModuleId, null);
                            }

                        }
                        else if (campaign.CampaignType == Campaign.CampaignTypes.PackModule)
                        {
                            CampaignPackModule campaignPackModule = new CampaignPackModule(campaign.CampaignId)
                            {
                                ChildEntity = EntityManager.GetEntity((int)Entities.PackModuleInCampaign)
                            };
                            foreach (DataRow packModuleRow in campaignPackModule.GetContent().Rows)
                            {
                                PackModule packModule = new PackModule(packModuleRow);
                                CampaignRoller.Subtitute(campaign, this, new Roller(newRollerId), campaign.Days(this), null, packModule.PackModuleId);
                            }
                        }
                        else
                        {
                            Debug.Assert(false, "Unknown campaign type");
                        }
                    }
                    action.Recalculate();
                    OnDataNeedRefresh();
                    CampaignPart.ShowPriceChangeMessage(price, action.TotalPrice);
                }
            }
            finally { owner.UseWaitCursor = false; }
        }

        protected void SetAdvertType(Form owner, bool changeFlag)
        {
            try
            {
                Entity entity = EntityManager.GetEntity((int)Entities.AdvertTypeChild);
                SelectionForm form = new SelectionForm(entity, entity.GetContent().DefaultView, "Выбор предмета рекламы");
                if (form.ShowDialog(owner) == DialogResult.OK)
                {
                    owner.UseWaitCursor = true;
                    Application.DoEvents();

                    Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
                    procParameters[Roller.ParamNames.RollerId] = this[Roller.ParamNames.RollerId];
                    procParameters[Action.ParamNames.ActionId] = this[Action.ParamNames.ActionId];
                    procParameters[Firm.ParamNames.FirmId] = this[Firm.ParamNames.FirmId];
                    procParameters[AdvertType.ParamNames.AdvertTypeId] = form.SelectedObject.IDs[0];
                    procParameters[Roller.ParamNames.IsCommon] = this[Roller.ParamNames.IsCommon];
                    procParameters[Roller.ParamNames.IsMute] = this[Roller.ParamNames.IsMute];
                    procParameters[Roller.ParamNames.Duration] = this[Roller.ParamNames.Duration];
                    procParameters["changeFlag"] = changeFlag;

                    DataAccessor.ExecuteNonQuery("ActionRollerSetAdvertType", procParameters);
                    if (IsRefreshAllSet)
                        OnDataNeedRefresh();
                    else
                    {
                        int newRollerId = int.Parse(procParameters["newRollerID"].ToString());

                        // если была информация о том сколько раз использовался этот ролик, надо ее сохранить и добавить в новый объект
                        int count = -1;
                        if (parameters.ContainsKey("count"))
                            count = (int)parameters["count"];

                        // если назначили предмет рекламы ролику "для всех фирм", то создастся его "клон" и вернется ID нового ролика
                        if (RollerId == newRollerId)
                        {
                            Refresh();
                            if (count >= 0) this["count"] = count;
                            OnObjectChanged(this);
                        }
                        else
                        {
                            ReplaceRoller(newRollerId, count);
                        }
                    }
                }
            }
            finally { owner.UseWaitCursor = false; }
        }
        private void ReplaceRoller(int newRollerId, int count)
        {
            Roller roller = new Roller(newRollerId);
            // скопируем из старого ролика количество выходов
            if (count >= 0) roller["count"] = count;

            OnObjectCloned(CreateNewRoller(roller));
            OnObjectDeleted(this);
        }

        protected virtual ActionRoller CreateNewRoller(Roller roller)
        {
            var actionRoller = new ActionRoller
            {
                parameters = roller.Parameters,
                isNew = false
            };
            actionRoller[Action.ParamNames.ActionId] = this[Action.ParamNames.ActionId];
            actionRoller[Firm.ParamNames.FirmId] = this[Firm.ParamNames.FirmId];
            return actionRoller;
        }
    }
}
