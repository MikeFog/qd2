using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Отказ по правам. Отдельный тип, чтобы отличать его от сбоя: это нормальный
/// отказ пользователю, такой же по смыслу, как отказ по бизнес-правилу из
/// процедуры (см. решение о WARN вместо ERROR в docs/LOGGING.md).
/// </summary>
public sealed class ActionNotAllowedException : Exception
{
	public ActionNotAllowedException(string message) : base(message)
	{
	}
}

/// <summary>
/// Проверка прав на действие для веба — подставляется в
/// <see cref="DataAccessor.IActionAuthorization"/> при старте.
///
/// Права НЕ придумываются заново: они уже посчитаны сервером. <c>EntityInfoRetrieve</c>
/// отдаёт по каждому действию <c>dbo.IsActionEnabled(@userID, entityActionID)</c>
/// (модель — <c>GroupRight</c> + <c>UserAdditionRight</c> + <c>isGrantingAllowed</c>,
/// ровно как <c>GroupMenu</c>/<c>UserAdditionMenu</c> для меню), и это значение
/// лежит в <c>Entity.Action.IsEnabled</c>. Десктоп этим значением только гасит
/// кнопки; здесь оно наконец применяется на исполнении.
///
/// Словари имён действий у слоя данных и у прав разные, поэтому нужно
/// сопоставление (проверено запросом к iEntityAction на ArtvisDev):
/// <list type="bullet">
/// <item><c>AddItem</c>, <c>DeleteItem</c>, <c>Clone</c> — имена совпадают;</item>
/// <item><c>UpdateItem</c> (сохранение существующей записи) в iEntityAction не
/// встречается ни разу — правом на него в десктопе служит <c>Properties</c>,
/// то есть право открыть карточку;</item>
/// <item><c>Load*</c> (чтение) в iEntityAction тоже не встречается: чтение
/// правами на действия не контролируется, его гейтит меню. Гасить чтение по
/// <c>RefreshItem</c> было бы строже десктопа — это меняло бы поведение,
/// а не переносило его.</item>
/// </list>
/// </summary>
public sealed class WebActionAuthorization : DataAccessor.IActionAuthorization
{
	/// <summary>Действия чтения — вне модели прав на действия.</summary>
	private static readonly HashSet<string> _readActions =
		new(StringComparer.OrdinalIgnoreCase)
		{
			Constants.Actions.Load,
			Constants.Actions.LoadIssues,
			Constants.Actions.LoadAgencies,
			Constants.Actions.LoadForSelection,
			Constants.Actions.LoadNo,
		};

	public void EnsureAllowed(int entityId, string actionName, int interfaceObjectId)
	{
		if (string.IsNullOrEmpty(actionName) || _readActions.Contains(actionName))
			return;

		if (SecurityManager.LoggedUser == null)
			throw new ActionNotAllowedException("Нужно войти в систему.");

		string rightName = ResolveRightName(actionName);
		Entity entity = EntityManager.GetEntity(entityId);

		// Сущность такого действия не объявляет — значит оно не под контролем
		// прав, а не запрещено (в десктопе для него просто нет кнопки).
		if (!entity.TryGetActionRight(rightName, out bool isEnabled))
			return;

		if (isEnabled)
			return;

		ErrorManager.Log.Warn(string.Format(
			"Отказано по правам. Пользователь: {0} (id {1}); сущность: {2} (id {3}); действие: {4}; право: {5}.",
			SecurityManager.LoggedUser.LoginName, SecurityManager.LoggedUser.Id,
			entity.Name, entityId, actionName, rightName));

		throw new ActionNotAllowedException(
			string.Format("Недостаточно прав: действие «{0}» для «{1}» вам не разрешено.",
				rightName, entity.Name));
	}

	private static string ResolveRightName(string actionName)
	{
		// Единственное расхождение имён, кроме чтения.
		return string.Equals(actionName, Constants.Actions.Update, StringComparison.OrdinalIgnoreCase)
			? Constants.EntityActions.ShowPassport
			: actionName;
	}
}
