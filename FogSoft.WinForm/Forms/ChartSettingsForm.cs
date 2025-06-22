using System;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Classes;

namespace FogSoft.WinForm.Forms
{
	public partial class ChartSettingsForm : Passport.Forms.PassportForm
	{
		public ChartSettingsForm()
			: base(PassportLoader.Load("ChartSettings"))
		{
			InitializeComponent();
			btnApply.Visible = false;
			pageContext = new PageContext(new DataSet(), DataAccessor.CreateParametersDictionary());
		}

		public bool UseLegend { get; set; }

		public bool CollectedThreshold { get; set; }

		public string CollectedLabel { get; set; }

		public int CollectedThresholdValue { get; set; }

		public bool Is3D { get; set; }

		public PieType PieType { get; set; }

		public LabelType LabelType { get; set; }

		public DrawningStyle DrawningStyle { get; set; }
        
		private CheckBox cbShowLegend;
		private CheckBox cbCollectedThreshold;
		private CheckBox cbIs3D;
		private TextBox tbCollectedLabel;
		private NumericUpDown numCollectedValue;
		private LookUp lookUpTypes;
		private LookUp lookupLabels;
		private LookUp lookupDrawningStyle;

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			cbShowLegend = FindControl("isShowLegend") as CheckBox;
			cbShowLegend.Checked = UseLegend;

			tbCollectedLabel = FindControl("сollectedLabel") as TextBox;
			tbCollectedLabel.Text = CollectedLabel;

			numCollectedValue = FindControl("сollectedThresholdValue") as NumericUpDown;
			numCollectedValue.Value = CollectedThresholdValue;

			cbCollectedThreshold = FindControl("isCollectedThreshold") as CheckBox;
			cbCollectedThreshold.CheckedChanged += new EventHandler(cbCollectedThreshold_CheckedChanged);
			cbCollectedThreshold.Checked = CollectedThreshold;

			lookUpTypes = FindControl("lookupTypes") as LookUp;
			if (lookUpTypes != null)
			{
				lookUpTypes.ClearItems();
				lookUpTypes.AddItem("Пирог", PieType.Pie);
				lookUpTypes.AddItem("Бублик", PieType.Doughnut);
				lookUpTypes.SelectedValue = PieType;
			}

			lookupLabels = FindControl("lookupLabels") as LookUp;
			if (lookupLabels != null)
			{
				lookupLabels.ClearItems();
				lookupLabels.AddItem("Внутри", LabelType.Inside);
				lookupLabels.AddItem("Вне", LabelType.Outside);
				lookupLabels.AddItem("Не отображать", LabelType.Disabled);
				lookupLabels.SelectedValue = LabelType;
			}

			lookupDrawningStyle = FindControl("lookupDrawningStyle") as LookUp;
			if (lookupDrawningStyle != null)
			{
				lookupDrawningStyle.ClearItems();
				lookupDrawningStyle.AddItem("Обычный", DrawningStyle.Default);
				lookupDrawningStyle.AddItem("Плавная заливка", DrawningStyle.SoftEdge);
				lookupDrawningStyle.AddItem("Вогнутый", DrawningStyle.Concave);
				lookupDrawningStyle.SelectedValue = DrawningStyle;
			}

			cbIs3D = FindControl("is3D") as CheckBox;
			cbIs3D.CheckedChanged += new EventHandler(cbIs3D_CheckedChanged);
			cbIs3D.Checked = Is3D;
		}

		void cbIs3D_CheckedChanged(object sender, EventArgs e)
		{
			lookupDrawningStyle.Enabled = !cbIs3D.Checked;
		}

		void cbCollectedThreshold_CheckedChanged(object sender, EventArgs e)
		{
			tbCollectedLabel.Enabled = cbCollectedThreshold.Checked;
			numCollectedValue.Enabled = cbCollectedThreshold.Checked;
		}

		protected override void ApplyChanges(Button clickedButton)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				UseLegend = cbShowLegend.Checked;
				CollectedThreshold = cbCollectedThreshold.Checked;
				CollectedLabel = tbCollectedLabel.Text;
				CollectedThresholdValue = Decimal.ToInt32(numCollectedValue.Value);
				Is3D = cbIs3D.Checked;
				DrawningStyle = ParseHelper.GetEnumValue(lookupDrawningStyle.SelectedValue.ToString(), DrawningStyle.Default);
				LabelType = ParseHelper.GetEnumValue(lookupLabels.SelectedValue.ToString(), LabelType.Outside);
				PieType = ParseHelper.GetEnumValue(lookUpTypes.SelectedValue.ToString(), PieType.Pie);
			}
			catch (Exception exp)
			{
				ErrorManager.PublishError(exp);
			}
		}
	}
}
