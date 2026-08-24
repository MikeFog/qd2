using System.Reflection;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Сопоставляет имя сборки из метаданных с реальной сборкой веб-приложения.
///
/// Проблема. Метаданные хранят, каким классом представлена сущность, парой
/// «класс + сборка»: у сущности 17 это
/// <c>Merlin.Classes.AdvertType</c> в сборке <c>Merlin</c>. Имя <c>Merlin</c> —
/// это <c>Merlin.exe</c>, то есть сборка десктопа. <c>Entity.CreateObject</c>
/// поднимает класс через <c>Activator.CreateInstance(assemblyName, className)</c>,
/// и в вебе этот вызов падает: <c>Merlin.exe</c> рядом нет, а тот же самый
/// исходник <c>Client/Classes/AdvertType.cs</c> подключён ссылкой в
/// <c>FogSoft.Core</c> и живёт в <c>FogSoft.Core.dll</c>.
///
/// Решение. Заявить это соответствие явно: запрос сборки <c>Merlin</c> в вебе
/// удовлетворяется сборкой ядра. Это не обход, а констатация факта о том, как
/// собран веб: классы те же, сборка другая.
///
/// Почему не иначе:
/// - <b>переименовать FogSoft.Core в Merlin</b> — метаданные заработали бы без
///   единой правки, но имя <c>Merlin</c> принадлежит десктопному приложению, а
///   в ядре лежит и половина FogSoft.WinForm; два разных артефакта с одним
///   именем сборки — источник путаницы, а не решение;
/// - <b>править метаданные</b> (<c>iEntity.assemblyName</c>) — это общая с
///   десктопом база: сменив имя, мы сломали бы десктоп, а поддерживать разные
///   значения для двух фронтендов нечем, колонка одна;
/// - <b>править Entity.CreateObject</b> в ядре — правка общего кода ради
///   веба, тогда как достаточно настройки на стороне веба.
///
/// Долгосрочно: когда десктоп будет выведен из эксплуатации, правильным
/// станет обновить метаданные и убрать это сопоставление. До тех пор оно —
/// единственное место, где веб знает про имя десктопной сборки.
/// </summary>
public static class DomainAssemblyResolver
{
	/// <summary>Имя сборки, записанное в метаданных (десктопное приложение).</summary>
	private const string MetadataAssemblyName = "Merlin";

	public static void Register()
	{
		AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
		{
			// args.Name — полное имя ("Merlin, Version=..."), нам нужна только
			// простая часть.
			string requested = new AssemblyName(args.Name).Name ?? "";
			return string.Equals(requested, MetadataAssemblyName, StringComparison.OrdinalIgnoreCase)
				? typeof(FogSoft.WinForm.Classes.PresentationObject).Assembly
				: null;
		};
	}
}
