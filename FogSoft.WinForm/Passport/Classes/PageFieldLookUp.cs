using System;
using System.Collections.Generic;
using System.Data;
using System.Xml.XPath;

namespace FogSoft.WinForm.Passport.Classes
{
	internal class PageFieldLookUp : PageFieldSelector, IObjectSelector
	{
		protected DataTable dtData;

		protected PageFieldLookUp(XPathNavigator navigator, PageContext context, PageTypes pageType)
			: base(navigator, context, new LookUp(), pageType)
		{
			Lookup.IsNullable = isNullable && context.PageType == PageTypes.Passport;
			SetColumnWithId(navigator.GetAttribute(Attributes.ColumnWithId, ""));
			dtData = GetSourceDataTable(navigator, context);

			if (dtData == null && !string.IsNullOrEmpty(navigator.GetAttribute(Attributes.Entity, "")))
				dtData = GetEntity(navigator).GetContent(GetFilters(navigator, context));
			if (!IsDependantLookup(navigator) && dtData != null)
				Lookup.DataSource = dtData.DefaultView;

			Lookup.SelectedItemChanged += PageLookUpField_SelectedItemChanged;
		}

		private void SetColumnWithId(string columnName)
		{
			if(columnName != string.Empty) Lookup.ColumnWithID = columnName;
		}

		private void PageLookUpField_SelectedItemChanged(object sender, EventArgs e)
		{
			FireValueChanged();
		}

		public override void SetValue(Dictionary<string, object> parameters)
		{
			base.SetValue(parameters);
			if(parameters.ContainsKey(Name))
				Lookup.SelectedValue = parameters[Name];
		}

		public override void ApplyChanges(Dictionary<string, object> parameters)
		{
			if(ValueShouldBeSet)
				parameters[Name] = Lookup.SelectedValue;
			else
				parameters.Remove(Name);
		}

		public string GetSelectedObjectValue(string parameterName)
		{
			object value = Lookup.GetValue(parameterName);
			return value == null ? string.Empty : value.ToString();
		}

		internal LookUp Lookup
		{
			get { return controlInRightColumn as LookUp; }
		}

		internal new static PageFieldLookUp CreateInstance(XPathNavigator navigator, PageContext context)
		{
			if(IsDependantLookup(navigator))
				return new PageFieldDependentLookup(navigator, context, context.PageType);
			return new PageFieldLookUp(navigator, context, context.PageType);
		}

		private static bool IsDependantLookup(XPathNavigator navigator)
		{
			return navigator.GetAttribute(Attributes.ParentLookupName, "") != string.Empty;
		}

		public override void ValidateUserInput()
		{
			if(PageType == PageTypes.Passport && !isNullable && Lookup.SelectedValue == null)
				throw new PassportException(this, controlInRightColumn);
		}
	}
}