using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm.Passport.Classes
{
	public abstract class PageControl
	{
		public struct Attributes
		{
			public const string Source = "source";
			public const string Caption = "caption";
			public const string Name = "name";
			public const string Entity = "entity";
			public const string DestName = "destination";
			public const string Multiselect = "multiselect";
			public const string RelationScenario = "relationScenario";
			public const string ObjectName = "objectName";
			//public const string ReadOnly = "readonly";
			public const string Filter = "filter";
			public const string Type = "type";
			public const string ColumnWithId = "columnWithID";
			public const string Value = "value";
			public const string Mandatory = "mandatory";
			public const string ParentLookupName = "parentLookupName";
			public const string IsCreateNewAllowed = "isCreateNewAllowed";
			public const string Locked = "locked"; // locked for all but Admin!
			public const string Disabled = "disabled";
			public const string Height = "height";
			public const string MinValue = "min";
			public const string MaxValue = "max";
			public const string PassportChar = "passportchar";
			public const string Show = "show";
			public const string DecimalPlaces = "decimalplaces";
			public const string NeedSavePict = "needsavepict";
			public const string Required = "required";
			public const string HashIt = "hashit";
			public const string Anchor = "anchor";
			public const string MaxLenght = "maxlenght";

			public const string ColumnName = "columnname";
			public const string ColumnParentid = "columnparentid";
			public const string ColumnId = "columnid";
			public const string IsMandatoryOnCreate = "isMandatoryOnCreate";

        }

		public enum ShowStatus
		{
			create, edit, always
		}

		public struct InitialValueAbbreviations
		{
			public const string LAST_MONTH = "LAST_MONTH";
			public const string LAST_WEEK = "LAST_WEEK";
			public const string TODAY = "TODAY";
			public const string StartOfTheMonth = "StartOfTheMonth";
			public const string EndOfTheMonth = "EndOfTheMonth";
            public const string StartOfTheLastMonth = "StartOfTheLastMonth";
			public const string LoggedUser = "LoggedUser";
		}

		public event EmptyDelegate ValueChanged;

		protected Color MANDATORY_COLOR = Color.BlueViolet;
		protected Control control;
		protected bool isNullable = true;
		protected bool isLocked;
		protected string caption;

		protected PageControl() {}

		protected PageControl(Control control)
		{
			this.control = control;
		}

		protected PageControl(Control control, XPathNavigator navigator) : this(control)
		{
			this.control.Name = GetControlName(navigator);
			caption = GetCaption(navigator);
        }

		protected PageControl(XPathNavigator navigator)
		{
			caption = GetCaption(navigator);
        }

		public virtual void Add2Page(Control parent, int left, int top, PageDimensions dimensions)
		{
			Add2Page(control, parent, left, top, dimensions);
		}

		protected void Add2Page(
			Control child, Control parent, int left, int top, PageDimensions dimensions)
		{
			child.Left = left;
			child.Top = top;
			parent.Controls.Add(child);
		}

		public virtual string Caption
		{
			get { return caption; }
		}

		public virtual int Height
		{
			get { return control.Height; }
		}

		public virtual string Name
		{
			get { return control.Name; }
		}

		protected void FireValueChanged()
		{
			if(ValueChanged != null) ValueChanged();
		}

		protected void SetControlLockedFlag(XPathNavigator navigator)
		{
			isLocked = IsDisabled(navigator) ||
			           (IsLocked(navigator) && !SecurityManager.LoggedUser.IsAdmin);
		}

		internal abstract void Focus();

		public abstract void SetValue(Dictionary<string, object> parameters);
		public abstract void ApplyChanges(Dictionary<string, object> parameters);

		public virtual void ValidateUserInput() {}

		protected static string GetControlName(XPathNavigator navigator)
		{
			return navigator.GetAttribute(Attributes.Name, "");
		}

		private string GetCaption(XPathNavigator navigator)
		{
			return navigator.GetAttribute(Attributes.Caption, "");
		}

		private bool IsLocked(XPathNavigator navigator)
		{
			string val = navigator.GetAttribute(Attributes.Locked, "");
			if(val == string.Empty) return false;
			return ParseHelper.ParseToBoolean(val);
		}

		private bool IsDisabled(XPathNavigator navigator)
		{
			string val = navigator.GetAttribute(Attributes.Disabled, "");
			if(val == string.Empty) return false;
			return ParseHelper.ParseToBoolean(val);
		}

		protected bool? IsMandatoryOnCreate(XPathNavigator navigator)
		{
            string val = navigator.GetAttribute(Attributes.IsMandatoryOnCreate, "");
            if (val == string.Empty) return null;
            return ParseHelper.ParseToBoolean(val);
        }

        public static PageControl CreateInstance(XPathNavigator navigator, PageContext context)
		{
			switch(navigator.Name)
			{
				case "label":
					return new PageFieldLabel(navigator, context);
				case "field":
					return PageField.CreateInstance(navigator, context);
				case "separator":
					return new Separator();
				case "lookup":
					return PageFieldLookUp.CreateInstance(navigator, context);
				case "objectPicker":
					return new PageFieldObjectPicker(navigator, context);
				case "selector":
					return new ObjectsSelector(navigator, context);
				case "button":
					return new PageButton(navigator);
				case "image":
					return new PageFieldImage(navigator, context.PageType);
				case "treeselector":
					return new TreeObjectsSelector(navigator, context);
				default:
					return null;
			}
		}

		public virtual void Clear()
		{
			
		}

		public virtual void OnAfterCreate()
		{
			
		}
	}
}