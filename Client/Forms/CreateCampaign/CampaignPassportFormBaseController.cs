using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using Merlin.Properties;

namespace Merlin.Forms.CreateCampaign
{
	internal class CampaignPassportFormBaseController
	{
		private DataSet Data { get; set;}
		private Form Form { get; set; }

		private bool IsPackModuleCampaignTypeSelected
		{
			get
			{
				return _cmbCampaignType != null && (Campaign.CampaignTypes)
				                                   Enum.Parse(typeof(Campaign.CampaignTypes),
				                                              _cmbCampaignType.SelectedValue.ToString()) ==
				                                   Campaign.CampaignTypes.PackModule;
			}
		}

		public int PaymentTypeID
		{
			get
			{
				return _cmbPaymentType == null ? 0 : ParseHelper.GetInt32FromObject(_cmbPaymentType.SelectedValue, 0);
			}
		}

		public int CampaignTypeID
		{
			get
			{
				return _cmbCampaignType == null ? 0 : ParseHelper.GetInt32FromObject(_cmbCampaignType.SelectedValue, 0);
			}
		}

		public Agency Agency
		{
			get
			{
				if (_grdAgency == null)
					return null;
				return (Agency)_grdAgency.SelectedObject;
			}
		}

		public Massmedia Massmedia
		{
			get
			{
				if (_grdMassmedia == null || !_grdMassmedia.Enabled)
					return null;
				return (Massmedia)_grdMassmedia.SelectedObject;
			}
		}

		private SmartGrid _grdAgency;
		private SmartGrid _grdMassmedia;
		private LookUp _cmbPaymentType;
		private LookUp _cmbCampaignType;
		private LookUp _cmbMassmediaGroup;

		public CampaignPassportFormBaseController(Form form)
		{
			Form = form;
		}

		public void Init(LookUp cmbCampaignType, LookUp cmbPaymentType, LookUp cmbMassmediaGroup, SmartGrid grdAgency, SmartGrid grdMassmedia)
		{
			_grdAgency = grdAgency;
			_grdMassmedia = grdMassmedia;
			_cmbCampaignType = cmbCampaignType;
			_cmbPaymentType = cmbPaymentType;
			_cmbMassmediaGroup = cmbMassmediaGroup;

			if (grdMassmedia != null)
				grdMassmedia.ObjectSelected += new ObjectDelegate(grdMassmedia_ObjectSelected);

			if (grdAgency != null)
				grdAgency.ObjectSelected += new ObjectDelegate(grdAgency_ObjectSelected);

			if (cmbCampaignType != null)
				cmbCampaignType.SelectedItemChanged += new System.EventHandler(cmbCampaignType_SelectedItemChanged);

            Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(EntityManager.GetEntity((int)Entities.CampaignOnMassmedia),
				                               InterfaceObjects.PropertyPage, Constants.Actions.Load);

			Data = (DataSet)DataAccessor.DoAction(procParameters);

			if (cmbCampaignType != null)
				cmbCampaignType.DataSource = Data.Tables["campaign_type"].DefaultView;

			if (cmbMassmediaGroup != null)
			{
				cmbMassmediaGroup.DataSource = Data.Tables["massmedia_group"].DefaultView;
				//cmbMassmediaGroup.SelectedValue = (int)Settings.Default.CreateCampaign_SelectedRollerType;
				cmbMassmediaGroup.SelectedItemChanged += new EventHandler(cmbMassmediaGroup_SelectedItemChanged);
			}

			if (cmbPaymentType != null)
				cmbPaymentType.DataSource = Data.Tables["payment_type"].DefaultView;

			if (grdAgency != null)
			{
				Entity entityAgency = (Entity) (EntityManager.GetEntity((int)Entities.Agency).Clone());
				entityAgency.AttributeSelector = (int) Agency.AttributeSelectors.NameOnly;
				grdAgency.Entity = entityAgency;
			}

			LoadMassmediaData();
		}

		private void LoadMassmediaData()
		{
			if (_grdMassmedia != null && _cmbMassmediaGroup != null)
			{
				Entity entity = (Entity) (EntityManager.GetEntity((int) Entities.MassMedia).Clone());
				entity.AttributeSelector = (int) Massmedia.AttributeSelectors.NameAndGroupOnly;
				_grdMassmedia.Entity = entity;
				int groupId = ParseHelper.GetInt32FromObject(_cmbMassmediaGroup.SelectedValue, 0);
				DataTable dtMassmedia = Data.Tables["massmedia"];
				if (groupId == 0)
					dtMassmedia.DefaultView.RowFilter = null;				
				else			
					dtMassmedia.DefaultView.RowFilter = "groupID = " + groupId;					
			
				_grdMassmedia.DataSource = Data.Tables["massmedia"].DefaultView;
			}
		}

		void cmbMassmediaGroup_SelectedItemChanged(object sender, EventArgs e)
		{
			//Settings.Default.CreateCampaign_SelectedRollerType = ParseHelper.GetEnumValue(_cmbMassmediaGroup.SelectedValue.ToString(), RolType.Radi);
			_grdAgency.Clear();	
			LoadMassmediaData();
			CheckOkButton();
		}

		void cmbCampaignType_SelectedItemChanged(object sender, EventArgs e)
		{
			if (_grdMassmedia != null && _grdAgency != null)
			{
				_grdMassmedia.Enabled = !IsPackModuleCampaignTypeSelected;
				if (_cmbMassmediaGroup != null)
					_cmbMassmediaGroup.Enabled = !IsPackModuleCampaignTypeSelected;

				if (IsPackModuleCampaignTypeSelected)
					_grdAgency.DataSource = SecurityManager.LoggedUser.Agencies.DefaultView; //Agency.LoadAgencies(true).Tables[Constants.TableNames.Data].DefaultView;
			}
			CheckOkButton();
		}
        
		void grdAgency_ObjectSelected(PresentationObject presentationObject)
		{
			CheckOkButton();
		}

		void grdMassmedia_ObjectSelected(PresentationObject presentationObject)
		{
			if (_grdAgency == null)
				return;

			try
			{
				Application.DoEvents();
				Form.Cursor = Cursors.WaitCursor;
				Massmedia massmedia = (Massmedia)presentationObject;
				_grdAgency.DataSource = massmedia.Agencies.DefaultView;
				CheckOkButton();
			}
			finally
			{
				Form.Cursor = Cursors.Default;
			}
		}

		public event Globals.VoidCallback OnCheckOkButton;

		public void CheckOkButton()
		{
			if (OnCheckOkButton != null)
				OnCheckOkButton();
		}
	}
}