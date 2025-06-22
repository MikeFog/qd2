using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm.Passport.Classes
{
	internal abstract class PageFieldSelector : PageField
	{
		protected PageFieldSelector(XPathNavigator navigator, PageTypes type)
			: base(navigator, type) { }

		protected PageFieldSelector(XPathNavigator navigator, PageContext context, PageTypes type)
			: base(navigator, GetColumnInfo(navigator, context), type) { }

		protected PageFieldSelector(
			XPathNavigator navigator, PageContext context,
			Control controlInRightColumn, PageTypes type)
			: base(navigator, GetColumnInfo(navigator, context), controlInRightColumn,
				   CreateControlInLeftColumn(context.PageType, navigator), type) { }

		protected static DataTable GetSourceDataTable(XPathNavigator navigator, PageContext context)
		{
			string sourceName = navigator.GetAttribute(Attributes.Source, "");
			if(string.IsNullOrEmpty(sourceName) || context.DataSource == null) return null;

			if(context.DataSource.Tables.Contains(sourceName))
				return context.DataSource.Tables[sourceName];
			return null;
		}

		protected static Entity GetEntity(XPathNavigator navigator)
		{
			return EntityManager.GetEntity(navigator.GetAttribute(Attributes.Entity, ""));
		}

		protected Dictionary<string, object> GetFilters(XPathNavigator navigator, PageContext context)
		{
			Dictionary<string, object> dictionary = DataAccess.DataAccessor.CreateParametersDictionary();
			XPathNodeIterator nodes = navigator.Select("filter");
			while (nodes.MoveNext())
			{
				string type = nodes.Current.GetAttribute(Attributes.Type, string.Empty);

				if (!string.IsNullOrEmpty(type))
				{
					string name = nodes.Current.GetAttribute(Attributes.Name, string.Empty);
					string value = nodes.Current.GetAttribute(Attributes.Value, string.Empty);

					FieldTypeResolver typeResolver = new FieldTypeResolver(type, null);
					object val = null;

					if (typeResolver.IsString || typeResolver.IsDoubleString)
						val = value;
					else if (typeResolver.IsBoolean)
						val = ParseHelper.ParseToBoolean(value);
					else if (typeResolver.IsInteger)
						val = ParseHelper.ParseToInt32(value);
					else if (typeResolver.IsDecimal)
						val = decimal.Parse(value);
					else if (string.Compare(type, "parameter") == 0)
					{
						val = context.Parameters.ContainsKey(name) ? context.Parameters[name] : null;
					}
					else if (string.Compare(type, "parameter_isnew") == 0)
					{
						val = context.PresentationObject.IsNew;
					}
					dictionary.Add(name, val);
				}
			}
			return dictionary;
		}
	}
}