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
        public readonly Entity StartingEntity;

        public RelationScenario(XmlNode node)
		{
			Name = node.Attributes["name"].Value;
			StartingEntityID = ParseHelper.ParseToInt32(node.Attributes["startingEntityID"].Value);
			StartingEntity = EntityManager.GetEntity(StartingEntityID);


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

		public static void Load()
		{
			XmlDocument xmlDoc = DataAccessor.LoadXml("as_relationScenarios");
			foreach (XmlNode xmlNode in xmlDoc.DocumentElement.SelectNodes("scenario"))
			{
				RelationScenario scenario = new RelationScenario(xmlNode);
				scenarios[scenario.Name] = scenario;
			}
		}

		public static RelationScenario GetScenario(string scenarioName)
		{
			if (scenarios.Count <= 0)
				Load();
			return scenarios[scenarioName];
		}

		public static void ClearHash()
		{
			scenarios.Clear();
		}
	}
}