using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Classes;

namespace Merlin.Forms
{
	public partial class UniversalPassportForm : FogSoft.WinForm.Passport.Forms.PassportForm
	{
		public delegate void ApplyChangesDelegate(Dictionary<string, object> parameters);
        public delegate bool ValidateDataDelegate(Dictionary<string, object> parameters);

        public struct PassportNames
		{
            public const string MoveTime = "MoveTime";
            public const string ChangeDuration = "TrafficChangeDuration";
            public const string DeleteWindows = "TrafficDeleteTariffWindows";
            public const string ChangeDurationInDay = "TrafficChangeDurationInDay";
            public const string ChangeTariffWindowsPrice = "ChangeTariffWindowsPrice";
        }

        public struct ProcedureNames
        {
            public const string MoveTime = "TariffWindowMoveTime";
			public const string ChangeDuration = "TariffWindowChangeDuration";
            public const string ChangeDurationInDay = "TariffWindowChangeDurationInDay";
			public const string ChangePrice = "TariffWindowChangePrice";
        }

		private readonly string procedureName;
        private readonly ApplyChangesDelegate applyChanges;
		private readonly ValidateDataDelegate validateData;	

        public UniversalPassportForm(Dictionary<string, object> parameters, string passportName, string procedureName, 
			string caption, ValidateDataDelegate validateData, DataSet ds = null)
			: base(PassportLoader.Load(passportName))
		{
			InitializeComponent();
			btnApply.Visible = false;
			pageContext = new PageContext(ds, parameters);
			this.procedureName = procedureName;
			Text = caption;
            this.validateData = validateData;
        }

        public UniversalPassportForm(Dictionary<string, object> parameters, string passportName, ApplyChangesDelegate applyChanges, string caption, ValidateDataDelegate validateData)
			: base(PassportLoader.Load(passportName))
        {
            InitializeComponent();
            btnApply.Visible = false;
            pageContext = new PageContext(null, parameters);
            Text = caption;            
			this.validateData = validateData;
            this.applyChanges = applyChanges;
        }

		public UniversalPassportForm(PresentationObject po,  string passportName, string caption, Entity entity, DataSet ds, ValidateDataDelegate validateData, ApplyChangesDelegate applyChanges) 
			: base(PassportLoader.Load(passportName))
        {
            InitializeComponent();
            btnApply.Visible = false;
            pageContext = new PageContext(ds, po)
            {
                Entity = entity
            };

            Text = caption;
            this.validateData = validateData;
            this.applyChanges = applyChanges;
        }

        protected override void ApplyChanges(Button clickedButton)
		{
			try
			{
				Application.DoEvents();

				foreach(PassportPage page in pages)
					page.ApplyChanges();

				if (validateData != null && !validateData(pageContext.Parameters))
				{
					DialogResult = DialogResult.None;
					return;
				}

				if (applyChanges != null)
					applyChanges(pageContext.Parameters);
				else
					DataAccessor.ExecuteNonQuery(procedureName, pageContext.Parameters);
			}
			catch(Exception exp)
			{
				ErrorManager.PublishError(exp);
			}
		}
	}
}
