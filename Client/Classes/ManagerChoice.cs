using System.Data;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// Поле «Менеджер» в отборе экранов без метаданных фильтра (журнал использования роликов,
	/// сетка вещания). Правило десктопных форм (RollerStatisticForm, FrmGridReport): менять
	/// менеджера можно только с правом на чужие или групповые акции — иначе это всегда сам
	/// пользователь; список — UserListByRights.
	/// </summary>
	public static class ManagerChoice
	{
		public static bool CanChoose()
		{
			SecurityManager.User user = SecurityManager.LoggedUser;
			return user.IsRightToViewForeignActions() || user.IsRightToViewGroupActions();
		}

		/// <summary>Менеджеры, доступные пользователю: userID, name — по имени.</summary>
		public static DataTable Managers()
		{
			DataTable table = DataAccessor.LoadDataSet("UserListByRights", DataAccessor.CreateParametersDictionary()).Tables[0];
			DataView view = new DataView(table) { Sort = "name" };
			return view.ToTable(false, SecurityManager.ParamNames.UserId, "name");
		}
	}
}
