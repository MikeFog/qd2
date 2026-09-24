using System.Collections.Concurrent;
using System.Data;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Переводы из iTranslation (процедура TranslationLoad), по языку целиком в памяти
/// процесса. Общие для всех пользователей: перевод от пользователя не зависит.
/// Перечитываются при перезапуске веба. docs/tasks/web-i18n.md.
/// </summary>
public static class TranslationStore
{
	private static readonly ConcurrentDictionary<string, Lazy<Dictionary<string, string>>> Languages = new();

	/// <summary>Перевод или null. Сначала с контекстом, потом без него.</summary>
	public static string? Find(string language, string source, string? context)
	{
		Dictionary<string, string> texts = Languages
			.GetOrAdd(language, lang => new Lazy<Dictionary<string, string>>(() => Load(lang)))
			.Value;
		if (!string.IsNullOrEmpty(context) && texts.TryGetValue(Key(context, source), out string? withContext))
			return withContext;
		return texts.TryGetValue(Key("", source), out string? text) ? text : null;
	}

	private static string Key(string context, string source) => context + "\u0001" + source;

	private static Dictionary<string, string> Load(string language)
	{
		var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["lang"] = language };
		DataTable table = DataAccessor.LoadDataSet("TranslationLoad", parameters).Tables[0];
		var texts = new Dictionary<string, string>(table.Rows.Count, StringComparer.Ordinal);
		foreach (DataRow row in table.Rows)
			texts[Key(row["context"].ToString()!, row["source"].ToString()!)] = row["text"].ToString()!;
		return texts;
	}
}
