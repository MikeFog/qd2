/****************************************************************************
'Title:			Class ParseHelper
'Author:		Oleg Aksenov
'Date Created:	9/30/2005
'Copyright:		Copyright ©2005-2010, Excis, LLC.
'Purpose: 		This class checks strings for minimal "Parse"-requirements 
'				and perform safe parsing
'History:
'Author			Date of modification	Description 
****************************************************************************/
using System;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;

namespace FogSoft.WinForm.Classes
{
	/// <summary>
	/// This class checks strings for minimal "Parse"-requirements.</summary>
	/// <remarks>The basic check - check on equality to an null and range.
	/// Additional check - check on presence in a string only digits.</remarks>
	public static class ParseHelper
	{
		/// <summary>
		/// Maximal length for <see cref="int"/>, in characters.</summary>
		private const int INT32_MAX_CHAR_LENGTH = 10;

		/// <summary>
		/// Maximal length for <see cref="uint"/>, in characters.</summary>
		private const int UINT32_MAX_CHAR_LENGTH = 9;

		/// <summary>
		/// Need to perform additional check in the IsCanParseToXXX methods.</summary>
		/// <remarks>Default value - true.</remarks>
		private static bool _PerformAdditionalCheck = true;

		/// <summary>
		/// Use <see cref="double.TryParse(string,NumberStyles,IFormatProvider,out double)"/>.</summary>
		private static bool _UseTryParse = true;

		/// <summary>
		/// Check string to the minimal requirements for <see cref="int.Parse(string,NumberStyles)"/>.</summary>
		/// <param name="value">String for check.</param>
		/// <returns>true, if the string satisfies to the minimal requirements for <see cref="int.Parse(string,NumberStyles)"/>.</returns>
		public static bool IsCanParseToInt32(string value)
		{
			if (_UseTryParse)
			{
				double fakeResult;
				return Double.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out fakeResult);
			}
			return InternalIsCanParseToInt32(value);
		}

		private static bool InternalIsCanParseToInt32(string value)
		{
			// simplest check (value.Length not saved in local vavriable, because compiled code will be good enough)
			if (StringUtil.IsNullOrEmpty(value) || value.Length > INT32_MAX_CHAR_LENGTH)
			{
				return false;
			}
			// additional check not needed?
			if (!_PerformAdditionalCheck)
			{
				return true;
			}
			char character;
			// enumerate all characters, except first
			for (int i = value.Length - 1; i > 0; i--)
			{
				if ((character = value[i]) < '0' || character > '9')
				{
					return false;
				}
			}
			// first character also may be '-'
			if ((character = value[0]) == '-' || (character >= '0' && character <= '9'))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Check string to the minimal requirements for <see cref="uint.Parse(string,NumberStyles)"/>.</summary>
		/// <param name="value">String for check.</param>
		/// <returns>true, if the string satisfies to the minimal requirements for <see cref="uint.Parse(string,NumberStyles)"/>.</returns>
		public static bool IsCanParseToUInt32(string value)
		{
			if (_UseTryParse)
			{
				double fakeResult;
				return Double.TryParse(value, NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingWhite,
				                       CultureInfo.CurrentCulture, out fakeResult);
			}
			return InternalIsCanParseToUInt32(value);
		}

		private static bool InternalIsCanParseToUInt32(string value)
		{
			// simplest check (value.Length not saved in local vavriable, because compiled code will be good enough)
			if (StringUtil.IsNullOrEmpty(value) || value.Length > UINT32_MAX_CHAR_LENGTH)
			{
				return false;
			}
			// additional check not needed?
			if (!_PerformAdditionalCheck)
			{
				return true;
			}
			// enumerate all characters
			for (int i = value.Length - 1; i >= 0; i--)
			{
				char character;
				if ((character = value[i]) < '0' || character > '9')
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Safe parse string (<see cref="IsCanParseToInt32"/>) using <see cref="int.Parse(string,NumberStyles)"/>.</summary>
		/// <param name="value">String for parse.</param>
		/// <param name="defaultValue">Default value, used if any errors occurs.</param>
		/// <returns>Parsed value or "defaultValue", if any errors occurs.</returns>
		public static int ParseToInt32(string value, int defaultValue)
		{
			if (_UseTryParse)
			{
				double result;
				if (!Double.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out result))
				{
					return defaultValue;
				}
				try
				{
					return (int) result;
				}
				catch
				{
					return defaultValue;
				}
			}
			return InternalParseToInt32(value, defaultValue);
		}

		/// <summary>
		/// Safe parse string (<see cref="IsCanParseToInt32"/>) using <see cref="int.Parse(string,NumberStyles)"/>.</summary>
		/// <param name="value">String for parse.</param>
		/// <returns>Parsed value or "0", if any errors occurs.</returns>
		public static int ParseToInt32(string value)
		{
			return ParseToInt32(value, 0);
		}

		public static bool ParseToBoolean(string value)
		{
			return ParseToBoolean(value, false);
		}

		public static bool ParseToBoolean(string value, bool defaultValue)
		{
			if (StringUtil.IsNullOrEmpty(value))
				return defaultValue;
			switch (value.ToLower())
			{
				case "true":
				case "yes":
				case "on":
					return true;
				case "false":
				case "no":
				case "off":
					return false;
			}
			switch (ParseToInt32(value, -1))
			{
				case 1:
					return true;
				case 0:
					return false;
				default:
					return defaultValue;
			}
		}

		private static int InternalParseToInt32(string value, int defaultValue)
		{
			// if we don't use Double.TryParse - do following:
			if (!IsCanParseToInt32(value))
			{
				return defaultValue;
			}
			try
			{
				return int.Parse(value);
			}
			catch
			{
				return defaultValue;
			}
		}

		/// <summary>
		/// Safe parse string (<see cref="IsCanParseToUInt32"/>) using <see cref="int.Parse(string,NumberStyles)"/>.</summary>
		/// <param name="value">String for parse.</param>
		/// <param name="defaultValue">Default value, used if any errors occurs.</param>
		/// <returns>Parsed value or "defaultValue", if any errors occurs.</returns>
		/// <remarks>Used "int" as return parameter for CLS compatibility.</remarks>
		public static int ParseToUInt32(string value, int defaultValue)
		{
			if (_UseTryParse)
			{
				double result;
				if (!Double.TryParse(value, NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingWhite,
				                     CultureInfo.CurrentCulture, out result))
				{
					return defaultValue;
				}
				try
				{
					return (int) result;
				}
				catch
				{
					return defaultValue;
				}
			}
			return InternalParseToUInt32(value, defaultValue);
		}

		private static int InternalParseToUInt32(string value, int defaultValue)
		{
			// if we don't use Double.TryParse - do following:
			if (!IsCanParseToUInt32(value))
			{
				return defaultValue;
			}
			try
			{
				return int.Parse(value);
			}
			catch
			{
				return defaultValue;
			}
		}


		public static int GetInt32FromObject(object value, int defaultValue)
		{
			if (StringUtil.IsDBNullOrNull(value))
				return defaultValue;

			return Convert.ToInt32(value);
		}

		public static decimal GetDecimalFromObject(object value, decimal defaultValue)
		{
			if (StringUtil.IsDBNullOrNull(value))
				return defaultValue;

			return Convert.ToDecimal(value);
		}

		public static float GetFloatFromObject(object value, float defaultValue)
		{
			if (StringUtil.IsDBNullOrNull(value))
				return defaultValue;

			return float.Parse(value.ToString());
		}

		public static double GetDoubleFromObject(object value, double defaultValue)
		{
			if (StringUtil.IsDBNullOrNull(value))
				return defaultValue;

			return double.Parse(value.ToString());
		}

		public static DateTime GetDateTimeFromObject(object value, DateTime defaultValue)
		{
			if (StringUtil.IsDBNullOrNull(value))
				return defaultValue;

			return Convert.ToDateTime(value);
		}

		public static bool GetBooleanFromObject(object value, bool defaultValue)
		{
			if (StringUtil.IsDBNullOrNull(value))
				return defaultValue;

			return Convert.ToBoolean(value);
		}

		public static byte GetByteFromObject(object value, byte defaultValue)
		{
			if (StringUtil.IsDBNullOrNull(value))
				return defaultValue;

			return Convert.ToByte(value);
		}

		public static string GetStringFromObject(object value, string defaultValue)
		{
			if (StringUtil.IsDBNullOrNull(value))
				return defaultValue;

			return value.ToString();
		}

		public static int GetAttributeIntValue(XPathNavigator node, string attributeName)
		{
			return GetAttributeIntValue(node, attributeName, -1);
		}

		public static int GetAttributeIntValue(XPathNavigator node, string attributeName, int defaultValue)
		{
			if (node == null || !node.HasAttributes) return defaultValue;

			string attrValue = node.GetAttribute(attributeName, string.Empty);
			if (attrValue.Length != 0)
				return ParseToInt32(attrValue, defaultValue);
			return defaultValue;
		}

		public static string GetAttributeStringValue(XPathNavigator node, string attributeName)
		{
			if (node == null || !node.HasAttributes) return null;

			string attrValue = node.GetAttribute(attributeName, string.Empty);
			if (attrValue.Length != 0)
				return attrValue;
			return null;
		}

		public static bool GetAttributeBoolValue(XmlReader reader, string attributeName, bool defaultValue)
		{
			if (reader == null || !reader.HasAttributes) return defaultValue;

			return GetBooleanValue(reader.GetAttribute(attributeName), defaultValue);
		}

		public static bool GetAttributeBoolValue(XPathNavigator node, string attributeName,
		                                         bool defaultValue)
		{
			if (node == null || !node.HasAttributes) return defaultValue;

			return GetBooleanValue(node.GetAttribute(attributeName, string.Empty), defaultValue);
		}

		private static bool GetBooleanValue(string attributeValue, bool defaultValue)
		{
			if (StringUtil.IsNullOrEmpty(attributeValue))
				return defaultValue;

			if (attributeValue.Equals("1")) return true;
			if (attributeValue.Equals("0")) return false;

			return bool.Parse(attributeValue);
		}

		public static int GetAttributeIntValue(XmlNode node, string attributeName)
		{
			return GetAttributeIntValue(node, attributeName, -1);
		}

		public static int GetAttributeIntValue(XmlNode node, string attributeName, int defaultValue)
		{
			if (node == null || node.Attributes == null) return defaultValue;

			XmlAttribute attr = node.Attributes[attributeName];
			if (attr != null)
				return ParseToInt32(attr.Value, defaultValue);
			return defaultValue;
		}

		public static string GetAttributeStringValue(XmlNode node, string attributeName)
		{
			if (node == null || node.Attributes == null) return null;

			XmlAttribute attr = node.Attributes[attributeName];
			if (attr != null)
				return attr.Value;
			return null;
		}

		public static bool GetAttributeBoolValue(XmlNode node, string attributeName, bool defaultValue)
		{
			if (node == null || node.Attributes == null) return defaultValue;

			XmlAttribute attr = node.Attributes[attributeName];
			if (attr != null)
			{
				return GetBooleanValue(attr.Value, defaultValue);
			}
			return defaultValue;
		}

		public static T GetAttributeEnumValue<T>(XmlNode node, string attributeName)
		{
			if (node != null && node.Attributes != null)
			{
				XmlAttribute attr = node.Attributes[attributeName];
				if (attr != null)
				{
					return GetEnumValue<T>(attr.Value);
				}
			}

			return default(T);
		}

		public static T GetAttributeEnumValue<T>(XmlNode node, string attributeName, T defaultValue)
		{
			try
			{
				return GetAttributeEnumValue<T>(node, attributeName);
			}
			catch
			{
				return defaultValue;
			}
		}

		public static T GetEnumValue<T>(string value)
		{
			return (T)Enum.Parse(typeof(T), value, true);
		}

		public static T GetEnumValue<T>(string value, T defaultValue)
		{
			if (StringUtil.IsNullOrEmpty(value)) return defaultValue;

			try
			{
				return GetEnumValue<T>(value);
			}
			catch
			{
				return defaultValue;
			}
		}

		/// <summary>
		/// Need to perform additional check in the IsCanParseToXXX methods (when we not use <see cref="double.TryParse(string,NumberStyles,IFormatProvider,out double)"/>).</summary>
		/// <remarks>Default value - true.</remarks>
		public static bool PerformAdditionalCheck
		{
			get { return _PerformAdditionalCheck; }
			set { _PerformAdditionalCheck = value; }
		}

		/// <summary>
		/// Whether or not to use <see cref="double.TryParse(string,NumberStyles,IFormatProvider,out double)"/> for integer operations.</summary>
		/// <remarks>Default value - true.</remarks>
		public static bool UseTryParse
		{
			get { return _UseTryParse; }
			set { _UseTryParse = value; }
		}
	}
}