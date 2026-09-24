using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// Трафик-менеджмент без UI — для веб-экрана «Трафик» (десктоп — TrafficManagementForm +
	/// TrafficGrid). Сетка окон — TariffWindowWeek.LoadForTraffic; здесь — то, что вокруг неё.
	/// Решения владельца — docs/tasks/web-tariffgrid.md, §9 «Трафик-менеджмент».
	/// </summary>
	public static class TrafficManagement
	{
		/// <summary>Группы станций с пунктом «Показать все» (id 0) — как фильтр десктопной формы.</summary>
		public static DataView Groups()
		{
			return Massmedia.LoadGroupsWithShowAllOption();
		}

		/// <summary>
		/// Станции группы (0 — все) с датой «обработано по» — massmediaList с набором колонок
		/// трафика (Massmedia.WinForms.LoadRadiostationsByGroup). Неактивные станции не
		/// показываем: окон по ним не ведут (десктоп показывает все).
		/// </summary>
		public static DataTable Stations(int groupId)
		{
			Entity entity = (Entity)Massmedia.GetEntity().Clone();
			entity.AttributeSelector = (int)Massmedia.AttributeSelectors.TrafficDeadLine;
			Dictionary<string, object> parameters = DataAccessor.PrepareParameters(entity);
			if (groupId > 0)
				parameters.Add(Massmedia.ParamNames.GroupId, groupId);

			DataTable table = ((DataSet)DataAccessor.DoAction(parameters)).Tables[Constants.TableNames.Data];
			DataView active = new DataView(table) { RowFilter = "isActive = true", Sort = "name" };
			return active.ToTable();
		}
	}
}
