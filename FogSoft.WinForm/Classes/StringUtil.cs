using System;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Collections.Generic;

namespace FogSoft.WinForm.Classes
{
	/// <summary>
	/// String utility methods.</summary>
	/// <remarks>Some methods reserved for future use.</remarks>
	public static class StringUtil
	{
		public static bool IsDBNullOrNull(object value)
		{
			return (value == null || value == DBNull.Value);
		}

		public static bool IsDBNullOrEmpty(object value)
		{
			if (IsDBNullOrNull(value)) return true;

			string stringValue = value as string;
			if (stringValue != null && stringValue.Length == 0)
				return true;

			return false;
		}
		
		public static string GetStringOrEmpty(string value)
		{
			if (value == null)
				return string.Empty;

			return value;
		}

		public static string GetStringOrEmpty(object value)
		{
			if (value == DBNull.Value)
				return string.Empty;

			if (value == null)
				return string.Empty;

			return value.ToString();
		}

		public static string GetStringOrEmptyTrimmed(string value)
		{
			return GetStringOrEmpty(value).Trim();
		}

		public static bool IsEmpty(string value)
		{
			return (value != null && value.Length == 0);
		}

		public static bool IsNullOrEmpty(string value)
		{
			return (value == null || value.Length == 0);
		}

		public static bool IsNullOrEmptyTrimmed(string value)
		{
			if (value == null || value.Length == 0 || value.Trim().Length == 0)
				return true;
			return false;
		}

		public static string GetNullIfEmpty(string value)
		{
			return IsNullOrEmpty(value) ? null : value;
		}

		public static string GetNullIfEmptyOrTrim(string value)
		{
			if (value == null || value.Length == 0)
				return null;

			string result = value.Trim();
			if (result.Length == 0)
				return null;

			return result;
		}

		public static string TrimBraces(string value)
		{
			if (IsNullOrEmpty(value)) return value;
			
			return value.TrimStart('{').TrimEnd('}');
		}

		/// <summary>
		/// Stuffs end of the string with specified character upon specified total width.</summary>
		/// <remarks>If string length less than or equal to total width, just returns the same string.</remarks>
		public static string StuffRight(string value, int totalWidth, char stuffingChar,
		                                int stuffingCharCount)
		{
			if (value == null || value.Length <= totalWidth)
			{
				return value;
			}
			return value.Substring(0, totalWidth - stuffingCharCount).PadRight(totalWidth, stuffingChar);
		}

		/// <summary>
		/// Search and remove all trailing ',' from string.
		/// </summary>
		/// <remarks>Be carefully when add spaces, newlines etc - because RemoveTrailingCommas search only ','</remarks>
		public static void RemoveTrailingCommas(StringBuilder builder)
		{
			while (builder.Length > 0 && builder[builder.Length - 1] == ',')
			{
				builder.Remove(builder.Length - 1, 1);
			}
		}

		public static string FirstCharToLower(string value)
		{
			StringBuilder builder = new StringBuilder(value);
			builder[0] = Char.ToLower(builder[0]);
			return builder.ToString();
		}

		public static string FirstCharToLowerAndAdd(string value, string addition)
		{
			if (IsNullOrEmpty(addition))
			{
				return FirstCharToLower(value);
			}
			StringBuilder builder = new StringBuilder(value, value.Length + addition.Length);
			builder.Append(addition);
			builder[0] = Char.ToLower(builder[0]);
			return builder.ToString();
		}

		/// <summary>
		/// Gets url-encoded string from <see cref="IDictionary"/>.</summary>
		public static string GetString(IDictionary values, bool ignoreEmptyValues)
		{
			StringBuilder result = new StringBuilder(values.Count*128);
			foreach (DictionaryEntry entry in values)
			{
				string value;
				if (entry.Value == null)
					value = string.Empty;
				else
					value = entry.Value.ToString();

				if (ignoreEmptyValues && IsNullOrEmpty(value))
					continue;

				result.Append(entry.Key.ToString());
				result.Append("=");
				result.Append(value);
				result.Append("&");
			}

			if (result.Length == 0)
				return string.Empty;

			return result.ToString(0, result.Length - 1);
		}

		public static bool EqualsIgnoreCase(string s1, string s2)
		{
			return string.Equals(s1, s2, StringComparison.CurrentCultureIgnoreCase);
		}

		/// <summary>
		/// Appends name and value to <see cref="StringBuilder"/> with 'name: value' format.</summary>
		public static void AppendNameValue(StringBuilder builder, string caption, object value)
		{
			builder.Append(caption).Append(": ").Append(value).AppendLine();
		}

		/// <summary>
		/// Returns the first non-null and non-empty parameter. 
		/// If all parameters is null or empty, returns <see cref="string.Empty"/>.
		/// </summary>
		public static string FirstNonEmpty(params string[] values)
		{
			if (values == null) return string.Empty;
			foreach (string s in values)
			{
				if (!IsNullOrEmpty(s)) return s;
			}
			return string.Empty;
		}

		/// <summary>
		/// Returns values in string representation separated by separator.
		/// </summary>
		/// <param name="separator"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public static string Concat(string separator, params object[] values)
		{
			StringBuilder result = new StringBuilder();
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i] != null)
					result.Append(values[i].ToString());
				if (i < values.Length - 1)
					result.Append(separator);
			}
			return result.ToString();
		}

		public static string Concat(string separator, IEnumerable values)
		{
			StringBuilder result = new StringBuilder();
			string sep = string.Empty;
			IEnumerator enumerator = values.GetEnumerator();
			enumerator.Reset();
			while(enumerator.MoveNext())
			{
				result.Append(sep);
				sep = separator;
				result.Append(enumerator.Current);
			}
			return result.ToString();
		}

		public delegate string ConcatConverter<T>(T item);
		public static string Concat<T>(string separator, IEnumerable<T> values, ConcatConverter<T> converter)
		{
			StringBuilder result = new StringBuilder();
			string sep = string.Empty;
			IEnumerator<T> enumerator = values.GetEnumerator();
			enumerator.Reset();
			while (enumerator.MoveNext())
			{
				result.Append(sep);
				sep = separator;
				result.Append(converter(enumerator.Current));
			}
			return result.ToString();
		}

		public static string SubstringAfter(string source, string value)
		{
			if (IsNullOrEmpty(value))
			{
				return source;
			}
			CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;            
			int index = compareInfo.IndexOf(source, value, CompareOptions.Ordinal);
			if (index < 0)
			{
				return string.Empty;
			}
			return source.Substring(index + value.Length);            
		}

		public static string SubstringBefore(string source, string value)
		{
			if (IsNullOrEmpty(value))
			{
				return value;
			}
			CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;
			int index = compareInfo.IndexOf(source, value, CompareOptions.Ordinal);
			if (index < 0)
			{
				return string.Empty;                    
			}
			return source.Substring(0, index);
		}

		public static int MiddleIndexOf(string source, char value)
		{
			if (IsNullOrEmpty(source)) return -1;

			int length = source.Length;
			int middleIndex = length/2;

			for (int i = 0; i <= middleIndex; i++)
			{
				int checkIndex;
				checkIndex = middleIndex + i;
				if (checkIndex >= 0 && checkIndex < length
					&& source[checkIndex] == value) return checkIndex;

				checkIndex = middleIndex - i - 1;
				if (checkIndex >= 0 && checkIndex < length
					&& source[checkIndex] == value) return checkIndex;
			}

			return -1;
		}

		public static bool NextCharIs(char ch, string value, int index)
		{
#if DEBUG
			if (value == null)
				throw new ArgumentNullException("value");
			if (index < 0 || index > value.Length)
				throw new ArgumentOutOfRangeException("index");
#endif
			if (index == value.Length)
				return false;
			return value[index + 1] == ch;
		}
	}
}