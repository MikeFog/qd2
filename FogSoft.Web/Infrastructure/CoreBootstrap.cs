using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Однократная загрузка справочников ядра.
///
/// Зачем нужно. Почти все словари ядра догружаются лениво при промахе кэша
/// (<c>EntityManager</c>, <c>MessageAccessor</c>, <c>PassportLoader</c> — все
/// вызывают <c>FullLoadDictionaries()</c>, если ключа нет). Единственное
/// исключение — <c>DataAccessor.procedureConfigs</c>: <c>DoAction</c> берёт
/// <c>procedureConfigs[key]</c> напрямую и падает с
/// <c>KeyNotFoundException: '17_Load_118'</c>, если словарь не загружен.
/// Поэтому <see cref="DataAccessor.LoadProcedureConfig"/> обязателен до
/// первого <c>DoAction</c> — в десктопе он вызывается после входа
/// (<c>SplashLogginForm</c>), здесь нужен свой вызов.
///
/// Почему один раз на процесс, а не на пользователя. Это read-only кэш
/// соответствий «сущность+действие+интерфейс → процедура», одинаковый для
/// всех; см. docs/tasks/web-migration.md, раздел 3.1, где такие кэши прямо
/// названы корректными для веба как есть.
///
/// Почему после входа, а не при старте приложения. Загрузка ходит в базу.
/// При старте это сделало бы приложение незапускаемым, если база ещё не
/// поднялась (порядок запуска служб после перезагрузки сервера). После входа
/// база заведомо доступна — вход сам только что через неё прошёл.
/// </summary>
public static class CoreBootstrap
{
	private static readonly object _lock = new();
	private static bool _done;

	public static void EnsureDictionariesLoaded()
	{
		if (_done)
			return;

		lock (_lock)
		{
			if (_done)
				return;

			DataAccessor.LoadProcedureConfig();

			// Остальные словари грузятся лениво сами. Тянуть их здесь целиком
			// (как делает десктоп) не нужно: в вебе это лишняя задержка первого
			// входа, а на корректность не влияет.

			_done = true;
		}
	}
}
