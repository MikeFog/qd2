using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm.Passport.Classes
{
	public enum PageTypes
	{
		Passport,
		Filter
	}

	public class PageContext
	{
		public readonly DataSet DataSource;
		public readonly PresentationObject PresentationObject;
		public Entity Entity;
		public readonly PageTypes PageType;
		private Dictionary<string, object> parameters;

		public PageContext(DataSet ds, PresentationObject presentationObject)
		{
			DataSource = ds;
			PresentationObject = presentationObject;
			parameters = presentationObject.Parameters;
			Entity = presentationObject.Entity;
			PageType = PageTypes.Passport;
		}

		public PageContext(DataSet ds, Dictionary<string, object> parameters)
		{
			DataSource = ds;
			PresentationObject = null;
			this.parameters = parameters;
			Entity = null;
			PageType = PageTypes.Passport;
		}

		public PageContext(DataSet ds, Dictionary<string, object> parameters, Entity entity)
		{
			DataSource = ds;
			this.parameters = parameters;
			Entity = entity;
			PresentationObject = null;
			PageType = PageTypes.Filter;
		}

		public Dictionary<string, object> Parameters
		{
			get { return parameters; }
			set { parameters = value; }
		}
	}
}