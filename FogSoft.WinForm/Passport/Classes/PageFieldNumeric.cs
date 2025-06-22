using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm.Passport.Classes
{
	internal class PageFieldNumeric : PageField
	{
		public PageFieldNumeric(
			XPathNavigator navigator, ColumnInfo columnInfo,
			int decimalPlaces, PageTypes pageType)
			: base(navigator, columnInfo, new NumericUpDown(), CreateControlInLeftColumn(pageType, navigator), pageType
				)
		{
			NumericControl.Minimum = ParseHelper.GetAttributeIntValue(navigator, Attributes.MinValue, int.MinValue);
			if (columnInfo != null)
			{
				NumericControl.Maximum = ParseHelper.GetAttributeIntValue(navigator, Attributes.MaxValue, (int)Math.Min(int.MaxValue, columnInfo.MaxValue));
				NumericControl.Minimum = ParseHelper.GetAttributeIntValue(navigator, Attributes.MinValue, (int)Math.Max(int.MinValue, columnInfo.MinValue));
			}
			else
				NumericControl.Maximum = ParseHelper.GetAttributeIntValue(navigator, Attributes.MaxValue, int.MaxValue);
			NumericControl.DecimalPlaces = ParseHelper.GetAttributeIntValue(navigator, Attributes.DecimalPlaces, decimalPlaces);
			NumericControl.ValueChanged += new EventHandler(numericControl_ValueChanged);
		}

		private void numericControl_ValueChanged(object sender, EventArgs e)
		{
			FireValueChanged();
		}

		public override void SetValue(Dictionary<string, object> parameters)
		{
			base.SetValue(parameters);
			if (parameters.ContainsKey(Name) && !string.IsNullOrEmpty(parameters[Name].ToString()))
				NumericControl.Value = Math.Min(NumericControl.Maximum, Math.Max(NumericControl.Minimum, decimal.Parse(parameters[Name].ToString())));
		}

		public override void ApplyChanges(Dictionary<string, object> parameters)
		{
			if(ValueShouldBeSet)
				parameters[Name] = NumericControl.Value;
			else
				parameters.Remove(Name);
		}

		private NumericUpDown NumericControl
		{
			get { return controlInRightColumn as NumericUpDown; }
		}

		public override void Clear()
		{
			base.Clear();
			NumericControl.Value = 0;
		}
	}
}