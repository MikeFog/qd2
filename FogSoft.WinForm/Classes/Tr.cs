namespace FogSoft.WinForm.Classes
{
	/// <summary>
	/// Перевод видимого пользователю текста. docs/tasks/web-i18n.md.
	///
	/// Ключ перевода — сам русский текст (как в gettext): вызов выглядит как
	/// <c>Tr.T("Сохранить")</c>, код остаётся читаемым, придумывать ключи не нужно,
	/// а непереведённая строка просто показывается по-русски.
	///
	/// Десктоп переводчик не задаёт — <see cref="T(string)"/> возвращает исходный
	/// текст без изменений. Веб подставляет свой при старте (тот же приём шва, что
	/// у <see cref="SecurityManager.SetLoggedUserStorage"/>): язык берётся из сеанса
	/// текущего пользователя.
	/// </summary>
	public static class Tr
	{
		public interface ITranslator
		{
			/// <param name="source">Русский исходный текст.</param>
			/// <param name="context">Уточнение для омонимов; обычно null.</param>
			string Translate(string source, string context);
		}

		private static ITranslator translator;

		public static void SetTranslator(ITranslator value)
		{
			translator = value;
		}

		public static string T(string source)
		{
			return T(source, null);
		}

		public static string T(string source, string context)
		{
			if (string.IsNullOrEmpty(source) || translator == null)
				return source;
			return translator.Translate(source, context);
		}

		/// <summary>Переводит шаблон, затем подставляет параметры.</summary>
		public static string Format(string source, params object[] args)
		{
			return string.Format(T(source), args);
		}
	}
}
