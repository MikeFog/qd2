using System.Data;
using FogSoft.WinForm.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Кэш метаданных сущностей одного circuit. Регистрируется как Scoped — то есть
/// живёт ровно столько, сколько вкладка браузера, как и <see cref="UserSession"/>.
///
/// Circuit здесь играет ту же роль, что процесс в десктопе: в десктопе на
/// процесс один пользователь и один кэш сущностей, в вебе — один пользователь и
/// один кэш на circuit.
/// </summary>
public sealed class CircuitEntityCacheState : EntityManager.IEntityCache
{
	public Dictionary<int, Entity> ById { get; } = new();

	public Dictionary<string, Entity> ByName { get; } =
		new(StringComparer.InvariantCultureIgnoreCase);

	public DataSet? FullData { get; set; }

	/// <summary>
	/// Сбросить всё. Нужно при смене пользователя внутри одного circuit (выход и
	/// повторный вход в той же вкладке): «на circuit» — не то же самое, что «на
	/// пользователя». Без этого сброса метаданные, а вместе с ними и права,
	/// остались бы от предыдущего пользователя вкладки — тот же дефект, что и
	/// общий кэш на процесс, только в меньшем масштабе.
	/// </summary>
	public void Clear()
	{
		ById.Clear();
		ByName.Clear();
		FullData = null;
	}
}

/// <summary>
/// Реализация <see cref="EntityManager.IEntityCache"/> для веба — тот же приём,
/// что и <see cref="WebLoggedUserStorage"/>: статическому ядру нужно хранилище,
/// а нужный экземпляр лежит в scope текущего circuit.
///
/// Зачем это вообще нужно. <c>EntityInfoRetrieve</c> вшивает в метаданные права
/// конкретного пользователя (<c>dbo.IsActionEnabled(@userID, …)</c>), а
/// <c>FullLoadDictionaries</c> (включён по умолчанию) снимает разом метаданные
/// ВСЕХ сущностей под тем, кто вошёл первым. С общим на процесс кэшем это
/// означало бы, что права первого вошедшего достаются всем до перезапуска.
///
/// Поведение вне обработки действия пользователя. Если circuit недоступен,
/// подставляется кэш на текущий поток выполнения (<see cref="AsyncLocal{T}"/>),
/// а НЕ общий на процесс: такой кэш никому не виден, максимум — лишняя загрузка
/// метаданных. Молча делить кэш между пользователями нельзя, это ровно тот
/// дефект, ради которого всё и делается.
/// </summary>
public sealed class WebEntityCache : EntityManager.IEntityCache
{
	private static readonly AsyncLocal<CircuitEntityCacheState?> _outsideCircuit = new();

	private readonly CircuitServicesAccessor _accessor;

	public WebEntityCache(CircuitServicesAccessor accessor)
	{
		_accessor = accessor;
	}

	private EntityManager.IEntityCache Current
	{
		get
		{
			CircuitEntityCacheState? fromCircuit =
				_accessor.Services?.GetService<CircuitEntityCacheState>();
			if (fromCircuit != null)
				return fromCircuit;

			return _outsideCircuit.Value ??= new CircuitEntityCacheState();
		}
	}

	public Dictionary<int, Entity> ById => Current.ById;

	public Dictionary<string, Entity> ByName => Current.ByName;

	public DataSet? FullData
	{
		get => Current.FullData;
		set => Current.FullData = value;
	}
}
