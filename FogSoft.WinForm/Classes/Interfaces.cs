using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm
{
	public interface IJournal
	{
		Dictionary<string, object> Filters { get; }
	}

	public interface IParentObject
	{
		void SetChildrenChanges(
			Entity entity, List<PresentationObject> addedObjects,
			List<PresentationObject> deletedObjects);
	}

	public interface IObjectContainer : IEnumerable<PresentationObject>
	{
		DataTable GetContent();
		DataTable GetContent(Dictionary<string, object> filterValues);
		Entity ChildEntity { get; set; }
		RelationScenario RelationScenario { get; set; }
		bool IsChildNodeExpandable { get; }
		void ClearCache();
		string Name { get; }
         Dictionary<string, object> Filter {  get; set; }	
    }

	public interface IVisualContainer
	{
		event ContainerDelegate ContainerRefreshed;
	}

	public interface IDataControl
	{
		DataView DataSource { set; }
	}

	public interface IObjectControl
	{
		void DeleteCurrentObject();
		void EditCurrentObject();
	}

	public delegate void ObjectDelegate(PresentationObject presentationObject);
	public delegate void ObjectsDelegate(IList<PresentationObject> presentationObjects);

	public delegate void ObjectParentChange(PresentationObject presentationObject, int parentDepth);

    public delegate void ObjectParentChange2(PresentationObject presentationObject, Entity parentEntity);

    public delegate void ObjectCheckedDelegate(PresentationObject presentationObject, bool state);

	public delegate void ContainerWithCaptionDelegate(IObjectContainer container, string caption);

	public delegate void ContainerDelegate(IObjectContainer container);

	public delegate void EmptyDelegate();

	public delegate void DateTimeDelegate(DateTime date);

	public delegate DataTable DataLoadDelegate();
}