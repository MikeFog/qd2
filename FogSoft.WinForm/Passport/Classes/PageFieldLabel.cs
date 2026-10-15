using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm.Passport.Classes
{
	internal class PageFieldLabel : PageField
	{
		private readonly FieldTypeResolver typeResolver;

		public PageFieldLabel(XPathNavigator navigator, PageContext context)
			: base(navigator, new Label(), context.PageType)
		{
			typeResolver = GetTypeResolver(navigator, GetColumnInfo(navigator, context));
		}

		public override void SetValue(Dictionary<string, object> parameters)
		{
			if (parameters.ContainsKey(Name))
				controlInRightColumn.Text = FormatValue(parameters[Name]);
		}

		// type="df_date" | "df_datetime" | "boolean" | "money" на самом <label> задаёт вид текста;
		// без type значение показывается как есть (ToString).
		private string FormatValue(object value)
		{
			if (value == null || value == DBNull.Value)
				return string.Empty;

			if (typeResolver.IsMoney)
				return decimal.Parse(value.ToString()).ToString("c");

			if (typeResolver.IsBoolean)
				return ParseHelper.GetBooleanFromObject(value, false) ? "Да" : "Нет";

			if (typeResolver.IsDate || typeResolver.IsDateTime)
			{
				DateTime date;
				if (value is DateTime)
					date = (DateTime) value;
				else if (!DateTime.TryParse(value.ToString(), out date))
					return value.ToString();

				return typeResolver.IsDate ? date.ToString("dd.MM.yyyy") : date.ToString("dd.MM.yyyy HH:mm");
			}

			return value.ToString();
		}

		public override void ApplyChanges(Dictionary<string, object> parameters) {}
	}
}
