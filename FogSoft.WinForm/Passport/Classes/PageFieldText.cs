using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm.Passport.Classes
{
	internal class PageFieldText : PageField
	{
		private readonly bool _hashIt = false;

		public PageFieldText(XPathNavigator navigator, ColumnInfo columnInfo, PageTypes pageType)
			: base(navigator, columnInfo, new TextBox(), CreateControlInLeftColumn(pageType, navigator), pageType)
		{
			TextBox.TextChanged += new EventHandler(textBox_TextChanged);
			if(columnInfo != null) TextBox.MaxLength = columnInfo.MaxLength;

			TextBox.MaxLength = ParseHelper.ParseToInt32(navigator.GetAttribute(Attributes.HashIt, ""), TextBox.MaxLength);

			char? passportChar = GetPassportChar(navigator);
			if (passportChar != null)
				TextBox.PasswordChar = (char)passportChar;
			_hashIt = ParseHelper.ParseToBoolean(navigator.GetAttribute(Attributes.HashIt, ""), false);
		}

		protected PageFieldText(XPathNavigator navigator, PageTypes pageType)
			: base(navigator, new TextBox(), pageType)
		{
			TextBox.TextChanged += new EventHandler(textBox_TextChanged);
		}

		private static char? GetPassportChar(XPathNavigator navigator)
		{
			if (navigator.GetAttribute(Attributes.PassportChar, "") == string.Empty)
				return null;
			return char.Parse(navigator.GetAttribute(Attributes.PassportChar, ""));
		}

		private void textBox_TextChanged(object sender, EventArgs e)
		{
			FireValueChanged();
		}

		public override void SetValue(Dictionary<string, object> parameters)
		{
			if (parameters.ContainsKey(Name))
			{
				TextBox.Text = parameters[Name].ToString();
				if(Checkbox != null) Checkbox.Checked = !string.IsNullOrEmpty(TextBox.Text);
			}
		}

		public override void ApplyChanges(Dictionary<string, object> parameters)
		{
			if (ValueShouldBeSet)
			{
				parameters[Name] = TextBox.Text;

				if (_hashIt)
					if (TextBox.Text != string.Empty)
						parameters[Name] = SecurityManager.GetHash(TextBox.Text);
					else // иначе затирался пароль
						parameters.Remove(Name);
            }
			else
				parameters.Remove(Name);
        }

		public override void ValidateUserInput()
		{
			if(PageType == PageTypes.Passport && !isNullable && TextBox.Text == string.Empty)
				throw new PassportException(this, controlInRightColumn);
		}

		protected TextBox TextBox
		{
			get { return (TextBox) controlInRightColumn; }
		}

		public override void Clear()
		{
			base.Clear();
			TextBox.Text = string.Empty;
		}
	}

	internal class PageFieldTextDoubleString : PageFieldText
	{
		public PageFieldTextDoubleString(
			XPathNavigator navigator, ColumnInfo columnInfo, PageTypes pageType)
			: base(navigator, columnInfo, pageType)
		{
			TextBox.Height *= 2;
			TextBox.Multiline = true;
		}
	}

    internal class PageFieldTextAria : PageFieldText
    {
        public PageFieldTextAria(
            XPathNavigator navigator, ColumnInfo columnInfo, PageTypes pageType)
            : base(navigator, columnInfo, pageType)
        {
            TextBox.Height *= 12;
            TextBox.Multiline = true;
        }
    }
}