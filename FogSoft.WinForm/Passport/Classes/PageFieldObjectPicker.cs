using System;
using System.Collections.Generic;
using System.Data;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;

namespace FogSoft.WinForm.Passport.Classes
{
	internal class PageFieldObjectPicker : PageFieldSelector
	{
		public PageFieldObjectPicker(XPathNavigator navigator, PageContext context)
			: base(navigator, context, CreateObjectPicker(navigator), context.PageType)
		{
			DataTable dtData = GetSourceDataTable(navigator, context);
			if(dtData != null)
				ObjectPicker.SetDataSource(GetEntity(navigator), dtData);
			else
				ObjectPicker.SetEntity(GetEntity(navigator));

			SetFilters(navigator, context);

			if(context.PageType == PageTypes.Filter || !IsCreateNewAllowed(navigator))
				ObjectPicker.IsCreateNewAllowed = false;
		}

		private void SetFilters(XPathNavigator navigator, PageContext context)
		{
			foreach (KeyValuePair<string, object> filter in GetFilters(navigator, context))
			{
				ObjectPicker.FilterValues[filter.Key] = filter.Value;
			}
		}

		private bool IsCreateNewAllowed(XPathNavigator navigator)
		{
			string val = navigator.GetAttribute(Attributes.IsCreateNewAllowed, "");
			if(val == string.Empty || !ParseHelper.ParseToBoolean(val)) return false;
			return true;
		}

		private static ObjectPicker2 CreateObjectPicker(XPathNavigator navigator)
        {
			string scenarioName = navigator.GetAttribute(Attributes.RelationScenario, "");
			return scenarioName == string.Empty ? new ObjectPicker2() : new ObjectPicker2(RelationManager.GetScenario(scenarioName));
		}

		public override void SetValue(Dictionary<string, object> parameters)
		{
			base.SetValue(parameters);
			if(parameters.ContainsKey(Name) && parameters[Name] != DBNull.Value)
				ObjectPicker.SelectObject(parameters[Name]);
		}

		public override void ApplyChanges(Dictionary<string, object> parameters)
		{
			if(ValueShouldBeSet && ObjectPicker.SelectedObject != null)
				parameters[Name] = ObjectPicker.SelectedObject.IDs[0];
			else
				parameters.Remove(Name);
		}

		public override void ValidateUserInput()
		{
			if(PageType == PageTypes.Passport && !isNullable && ObjectPicker.SelectedObject == null)
				throw new PassportException(this, controlInRightColumn);
		}

		private ObjectPicker2 ObjectPicker
		{
			get { return controlInRightColumn as ObjectPicker2; }
		}
	}
}