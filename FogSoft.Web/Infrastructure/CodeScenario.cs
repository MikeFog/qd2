using System.Xml;
using FogSoft.WinForm.Classes;
using Merlin;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Сценарий связей «родитель → ребёнок», объявленный в коде веба, а не в
/// метаданных (<c>as_relationScenarios</c>).
///
/// Нужен экранам, которые десктоп открывает через <c>MasterDetailForm</c> (две
/// таблицы), а веб делает на движке деревьев (решение владельца 2026-09-21).
/// Строки в <c>iRelationScenario</c>/<c>iEntityRelation</c> для них не заводятся:
/// база общая с десктопом, и сценарий пришлось бы накатывать на все базы
/// заказчиков ради экранов, которых в десктопе нет.
///
/// Разметка — ровно та, что читает конструктор <see cref="RelationScenario"/>
/// (<c>RelationManager.cs</c>); строится через DOM, а не склейкой строк: в
/// <c>filter</c> лежит XML, и его нужно экранировать.
///
/// Результат нигде не кэшируется. <see cref="RelationScenario.StartingEntity"/>
/// — сущность из кэша circuit, а метаданные в вебе персональные (права
/// пользователя вшиты в них при загрузке, см. <see cref="CircuitEntityCacheState"/>).
/// Статическое поле раздало бы объект одного пользователя другим; строится
/// дёшево, поэтому — при каждом открытии экрана.
/// </summary>
public static class CodeScenario
{
	/// <summary>
	/// Сценарий «мастер → деталь»: стартовая сущность — мастер, у неё одна связь
	/// на деталь. Отбор — <c>XmlFilter</c> мастера, как в
	/// <c>MasterDetailForm</c> (<c>masterEntity.XmlFilter</c>); у мастера без
	/// отбора панели на экране не будет.
	/// </summary>
	/// <remarks>
	/// <c>isChildNodeExpandable="0"</c>: узел мастера — лист, «+» у него нет. Детали
	/// показываются только справа списком выбранного мастера — как подчинённый
	/// грид десктопа, который никуда не разворачивается. <c>selector="0"</c> —
	/// обычный набор атрибутов детали.
	/// </remarks>
	public static RelationScenario MasterDetail(string name, Entities master, Entities detail)
	{
		Entity masterEntity = EntityManager.GetEntity((int)master);

		var document = new XmlDocument();
		XmlElement scenario = document.CreateElement("scenario");
		scenario.SetAttribute("name", name);
		scenario.SetAttribute("startingEntityID", ((int)master).ToString());
		if (masterEntity.IsFilterable)
			scenario.SetAttribute("filter", masterEntity.XmlFilter);

		XmlElement relation = document.CreateElement("relation");
		relation.SetAttribute("parentEntityID", ((int)master).ToString());
		relation.SetAttribute("childEntityID", ((int)detail).ToString());
		relation.SetAttribute("selector", "0");
		relation.SetAttribute("isChildNodeExpandable", "0");
		scenario.AppendChild(relation);

		document.AppendChild(scenario);
		return new RelationScenario(scenario);
	}
}
