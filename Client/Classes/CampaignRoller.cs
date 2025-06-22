using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Controls;
using Merlin.Forms;
using static Merlin.Classes.Campaign;

namespace Merlin.Classes
{
    internal class CampaignRoller : CampaignPart
	{
		private Roller roller;

		public CampaignRoller() : base(EntityManager.GetEntity((int) Entities.CampaignRoller))
		{
		}

		protected CampaignRoller(Entity entity) : base(entity)
		{
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (actionName == Constants.Actions.PlayRoller)
				return base.IsActionEnabled(actionName, type) && !IsMute;
			return base.IsActionEnabled(actionName, type);
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == Constants.Actions.Substitute)
				SubstituteRoller(owner);
			else if (actionName == Constants.Actions.PlayRoller)
				MediaControl.Current.Play(this);
            else if (actionName == Constants.Actions.ChangePositions)
                ChangePositions((Form)owner);
            else if (actionName == Campaign.ActionNames.DeleteIssues)
                DeleteIssues((Form)owner);
            else
                base.DoAction(actionName, owner, interfaceObject);
		}

		protected void SubstituteRoller(IWin32Window owner, int refreshLevel = 1)
		{
            Campaign.Action.Refresh();
            Substitute((Form)owner, Campaign, null, ModuleID, Roller,
                       delegate
                       {
                           RecalculateAndShowPriceChange(Campaign.Action.TotalPrice);
                           //OnParentChanged(this, refreshLevel);
                           OnParentChanged(this, EntityManager.GetEntity((int)Entities.GeneralCampaign));
                       });
        }

        private void DeleteIssues(Form owner)
        {
            Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
            parameters[CampaignPart.OBJECT_ID] = Roller.RollerId;
			if (Campaign.DeleteIssues(owner, false, parameters, isFireEvent: false))
				FireContainerRefreshed();
        }

        public Roller Roller
		{
			get
			{
				if (roller == null)
					roller = new Roller(int.Parse(this[Roller.ParamNames.RollerId].ToString()));

                return roller;
            }
		}

        public int? ModuleID
		{
			get {return (this["moduleID"] == null || this["moduleID"] == DBNull.Value) ? null : (int?)ParseHelper.ParseToInt32(this["moduleID"].ToString()); }
		}
        
		public bool IsMute
		{
			get { return ParseHelper.GetBooleanFromObject(this["isMute"], false); }
		}

		public static void Substitute(Form parentForm, Campaign campaign, int? packModuleId, int? moduleID, Roller roller, Globals.VoidCallback onEnd)
		{
			try
			{
				RollerSubstitutionForm fSubstitute = new RollerSubstitutionForm(roller, campaign, moduleID, packModuleId);
				if (fSubstitute.ShowDialog(parentForm) == DialogResult.OK)
				{
					Cursor.Current = Cursors.WaitCursor;
					Subtitute(campaign, roller, fSubstitute.NewRoller, fSubstitute.SelectedDays, moduleID, packModuleId);
					onEnd?.Invoke();
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

		private static void Subtitute(Campaign campaign, Roller oldRoller, Roller newRoller,
								  DataTable days, object moduleID, object packModuleID)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				EntityManager.GetEntity((int)Entities.CampaignRoller),
				InterfaceObjects.FakeModule, Constants.Actions.Substitute);
			procParameters["oldRollerId"] = oldRoller.RollerId;
			procParameters["oldDuration"] = oldRoller.Duration;
			procParameters["newRollerId"] = newRoller.RollerId;
			procParameters["newDuration"] = newRoller.Duration;
			if (moduleID != null)
				procParameters["moduleID"] = moduleID;
			if (packModuleID != null)
				procParameters["packModuleID"] = packModuleID;
			procParameters[Campaign.ParamNames.CampaignId] = campaign.CampaignId;
			procParameters[Campaign.ParamNames.CampaignTypeId] = (int)campaign.CampaignType;

			DataSet ds = DataAccessor.LoadDataSet("RollerSubstitute", procParameters, days);

			if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
				Globals.ShowSimpleJournal(EntityManager.GetEntity((int) Entities.RollerUnSubtitude), "Незамененные ролики",
				                          ds.Tables[0]);
		}

		public DataTable Issues
		{
			get
			{
				Dictionary<string, object> procParameters =
					new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase)
					{
                        [Campaign.ParamNames.MassmediaId] = ((CampaignOnSingleMassmedia)Campaign).Massmedia.MassmediaId,
                        [Campaign.ParamNames.CampaignId] = Campaign.CampaignId,
						[Roller.ParamNames.RollerId] = this[Roller.ParamNames.RollerId]
                    };

				return DataAccessor.LoadDataSet("IssuesByDate", procParameters).Tables[0];
			}
		}
	}

	internal class CampaignRollerInsideDay : CampaignRoller
	{
		public CampaignRollerInsideDay()
			:base(EntityManager.GetEntity((int)Entities.CampaignRollerInsideDay))
		{
		}
	}
}
