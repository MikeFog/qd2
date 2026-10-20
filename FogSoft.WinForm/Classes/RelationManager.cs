using System.Collections.Generic;
using System.Xml;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.WinForm.Classes
{
	public class RelationScenario
	{
		public class EntityRelation
		{
			public readonly bool IsChildNodeExpandable;
			private readonly int childEntityId;
			private readonly int selector;

			public EntityRelation(XmlNode node)
			{
				IsChildNodeExpandable = node.Attributes["isChildNodeExpandable"].Value == "1";
				childEntityId = ParseHelper.ParseToInt32(node.Attributes["childEntityID"].Value);
				selector = ParseHelper.ParseToInt32(node.Attributes["selector"].Value);
			}

			public Entity ChildEntity
			{
				get
				{
					Entity entity = EntityManager.GetEntity(childEntityId);
					entity.AttributeSelector = selector;
					return entity;
				}
			}
		}

		private readonly Dictionary<int, EntityRelation> entityPairs = new Dictionary<int, EntityRelation>();
		public readonly string Name;
		public readonly int StartingEntityID;
        /// <summary>
        /// Стартовая сущность — на каждое обращение, как <see cref="EntityRelation.ChildEntity"/>: сценарий лежит в кэше
        /// на процесс, а Entity в вебе у каждого пользователя своя (права вшиты в метаданные) — запомненная ссылка
        /// досталась бы всем от первого открывшего дерево (IMPROVEMENTS [WEB-01]).
        /// </summary>
        public Entity StartingEntity
        {
            get { return EntityManager.GetEntity(StartingEntityID); }
        }
        public readonly string XmlFilter;

        public RelationScenario(XmlNode node)
		{
			Name = node.Attributes["name"].Value;
			StartingEntityID = ParseHelper.ParseToInt32(node.Attributes["startingEntityID"].Value);
            XmlFilter = node.Attributes["filter"]?.Value;

            foreach (XmlNode xmlNode in node.SelectNodes("relation[@childEntityID]"))
			{
				entityPairs[ParseHelper.ParseToInt32(xmlNode.Attributes["parentEntityID"].Value)] =
					new EntityRelation(xmlNode);
			}
		}

		public EntityRelation GetChildEntity(int parentEntityId)
		{
			if(entityPairs.ContainsKey(parentEntityId))
				return entityPairs[parentEntityId];
			return null;
		}
	}

	public static class RelationManager
	{
		private static readonly Dictionary<string, RelationScenario> scenarios =
			new Dictionary<string, RelationScenario>();
		// Веб: несколько circuit на холодном старте заполняли бы словарь одновременно.
		private static readonly object sync = new object();

		public static void Load()
		{
			XmlDocument xmlDoc = DataAccessor.LoadXml("as_relationScenarios");
			lock (sync)
			{
				foreach (XmlNode xmlNode in xmlDoc.DocumentElement.SelectNodes("scenario"))
				{
					RelationScenario scenario = new RelationScenario(xmlNode);
					scenarios[scenario.Name] = scenario;
				}
			}
		}

		public static RelationScenario GetScenario(string scenarioName)
		{
			lock (sync)
			{
				if (scenarios.Count <= 0)
					Load();
				return scenarios[scenarioName];
			}
		}

		public static void ClearHash()
		{
			lock (sync)
				scenarios.Clear();
		}
	}
}