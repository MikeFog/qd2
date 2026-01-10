using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;

namespace FogSoft.WinForm.Passport.Classes
{
	internal abstract class PageFieldDateTime : PageField
	{
		private const string DefaultDateTime = "getdate()";

		protected PageFieldDateTime(
			XPathNavigator navigator, ColumnInfo columnInfo,
			DateTimePickerFormat format, PageTypes pageType)
			: base(navigator, columnInfo, new DateTimePicker(), CreateControlInLeftColumn(pageType, navigator), pageType)
		{
			Picker.Format = format;
			Picker.ValueChanged += OnPickerValueChanged;
		}

		public override void SetValue(Dictionary<string, object> parameters)
		{
			base.SetValue(parameters);
			if (parameters.ContainsKey(Name) && parameters[Name] != DBNull.Value)
			{
				DateTime now = DateTime.Now;
				Picker.Value = (parameters[Name].ToString().Equals(DefaultDateTime))
				               	? new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0)
				               	: DateTime.Parse(parameters[Name].ToString());
			}
		}

		public override void ApplyChanges(Dictionary<string, object> parameters)
		{
			if(ValueShouldBeSet)
				parameters[Name] = new DateTime(Picker.Value.Year, Picker.Value.Month, Picker.Value.Day, Picker.Value.Hour, Picker.Value.Minute, 0);
			else
				parameters.Remove(Name);
		}

		private void OnPickerValueChanged(object sender, EventArgs e)
		{
			FireValueChanged();
		}

		protected DateTimePicker Picker
		{
			get { return (DateTimePicker) controlInRightColumn; }
		}
	}

	internal class PageFieldDate : PageFieldDateTime
	{
		public PageFieldDate(XPathNavigator navigator, ColumnInfo columnInfo, PageTypes pageType)
			: base(navigator, columnInfo, DateTimePickerFormat.Short, pageType) {}

		public override void ApplyChanges(Dictionary<string, object> parameters)
		{
			if(ValueShouldBeSet)
				parameters[Name] = Picker.Value.Date;
			else
				parameters.Remove(Name);
		}
	}

	internal class PageFieldFullDateTime : PageFieldDateTime
	{
		public PageFieldFullDateTime(XPathNavigator navigator, ColumnInfo columnInfo, PageTypes pageType)
			: base(navigator, columnInfo, DateTimePickerFormat.Custom, pageType)
		{
			Picker.ShowUpDown = false;
			Picker.CustomFormat = "HH:mm dd.MM.yy";
		}
	}

	internal class PageFieldTime : PageFieldDateTime
	{
		public PageFieldTime(XPathNavigator navigator, ColumnInfo columnInfo, PageTypes pageType)
			: base(navigator, columnInfo, DateTimePickerFormat.Custom, pageType)
		{
			Picker.ShowUpDown = true;
			Picker.CustomFormat = "HH:mm";
		}

		public override void ApplyChanges(Dictionary<string, object> parameters)
		{
			if(ValueShouldBeSet)
				parameters[Name] = new DateTime(1900, 1, 1, Picker.Value.Hour, Picker.Value.Minute, 0);
			else
				parameters.Remove(Name);
		}
	}

	internal class PageFieldTimeDuration : PageField
	{
		public PageFieldTimeDuration(XPathNavigator navigator, ColumnInfo columnInfo, PageTypes pageType)
			: base(navigator, columnInfo, new TimeDuration(), CreateControlInLeftColumn(pageType, navigator), pageType)
		{
			((TimeDuration) controlInRightColumn).ValueChanged += FireValueChanged;
		}

		public override void SetValue(Dictionary<string, object> parameters)
		{
			if(parameters.ContainsKey(Name))
				TimeDurationControl.Value = ParseHelper.ParseToInt32(parameters[Name].ToString());
		}

		public override void ApplyChanges(Dictionary<string, object> parameters)
		{
			if(ValueShouldBeSet)
				parameters[Name] = TimeDurationControl.Value;
			else
				parameters.Remove(Name);
		}

		private TimeDuration TimeDurationControl
		{
			get { return controlInRightColumn as TimeDuration; }
		}
	}
}