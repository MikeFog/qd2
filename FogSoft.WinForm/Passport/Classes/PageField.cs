using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm.Passport.Classes
{
	public abstract class PageField : PageControl
	{
		protected Control controlInLeftColumn;
		protected Control controlInRightColumn;
		protected AnchorStyles? aStyles;

		protected PageTypes PageType { get; private set;}

		protected PageField(XPathNavigator navigator, PageTypes type)
			: base(navigator)
		{
			controlInLeftColumn = CreateLabel(navigator);
			SetControlLockedFlag(navigator);
			SetRequied(navigator);
			PageType = type;
			GetAnchorStyles(navigator);
		}

		private void GetAnchorStyles(XPathNavigator navigator)
		{
			string anchor = navigator.GetAttribute(Attributes.Anchor, string.Empty);
			if (!string.IsNullOrEmpty(anchor))
			{
				string[] anchors = anchor.Split('|');
				foreach (string s in anchors)
				{
					AnchorStyles styles = ParseHelper.GetEnumValue<AnchorStyles>(s);
					if (!aStyles.HasValue)
						aStyles = styles;
					else
						aStyles |= styles;
				}
			}
		}

		private void SetRequied(XPathNavigator navigator)
		{
			string requied = navigator.GetAttribute(Attributes.Required, string.Empty);
			if (!string.IsNullOrEmpty(requied))
			{
				isNullable = !ParseHelper.ParseToBoolean(requied, false);
			}
		}

		protected PageField(XPathNavigator navigator, ColumnInfo columnInfo, PageTypes type)
			: this(navigator, type)
		{
			if (isNullable)
			{
				if (columnInfo != null)
					isNullable = columnInfo.IsNullable;
				else
					isNullable = !IsMandatoryElement(navigator);
			}
		}

		protected PageField(XPathNavigator navigator, Control controlInRightColumn, PageTypes type)
			: this(navigator, type)
		{
			InitializeControlInRightColumn(navigator, controlInRightColumn);
		}

		protected PageField(
			XPathNavigator navigator, ColumnInfo columnInfo,
			Control controlInRightColumn, Control controlInLeftColumn, PageTypes type)
			: this(navigator, columnInfo, type)
		{
			this.controlInRightColumn = controlInRightColumn;
			this.controlInRightColumn.Name = GetControlName(navigator);
			this.controlInLeftColumn = controlInLeftColumn;
			if(controlInLeftColumn is CheckBox)
			{
				this.controlInRightColumn.Enabled = false;
				Checkbox.CheckedChanged += new EventHandler(PageField_CheckedChanged);
			}
		}

		private bool IsMandatoryElement(XPathNavigator navigator)
		{
			if(navigator.GetAttribute(Attributes.Mandatory, "") == string.Empty)
				return false;
			return ParseHelper.ParseToBoolean(navigator.GetAttribute(Attributes.Mandatory, ""));
		}

		private void PageField_CheckedChanged(object sender, EventArgs e)
		{
			controlInRightColumn.Enabled = Checkbox.Checked && !isLocked;
		}

		private void InitializeControlInRightColumn(XPathNavigator navigator, Control ctlInRightColumn)
		{
			controlInRightColumn = ctlInRightColumn;
			controlInRightColumn.Name = GetControlName(navigator);
		}

		public override void Add2Page(Control parent, int left, int top, PageDimensions dimensions)
		{
			if (Label != null)
				Label.ForeColor = isNullable ? SystemColors.ControlText : MANDATORY_COLOR;

			controlInLeftColumn.AutoSize = true;
			Size size = controlInLeftColumn.PreferredSize;
			controlInLeftColumn.AutoSize = false;
			controlInLeftColumn.Width = dimensions.ControlWidthInLeftColumn;
			controlInLeftColumn.Height = (size.Height)*(size.Width/(dimensions.ControlWidthInLeftColumn + 1) + 1) + 10;

            Add2Page(controlInLeftColumn, parent, left, top, dimensions);
			controlInRightColumn.Width = dimensions.ControlWidthInRightColumn;
			Add2Page(controlInRightColumn, parent, dimensions.RightColumnX, top, dimensions);

			if(isLocked)
				controlInRightColumn.Enabled = controlInLeftColumn.Enabled = false;
		}

		public override void SetValue(Dictionary<string, object> parameters)
		{
			if(Checkbox != null)
				Checkbox.Checked = parameters.ContainsKey(Name);
		}

		public override int Height
		{
			get { return Math.Max(controlInRightColumn.Height, controlInLeftColumn.Height); }
		}

		public override string Name
		{
			get { return controlInRightColumn.Name; }
		}

		protected Label Label
		{
			get { return controlInLeftColumn as Label; }
		}

		protected CheckBox Checkbox
		{
			get { return controlInLeftColumn as CheckBox; }
		}

		protected bool ValueShouldBeSet
		{
			get
			{
				return (PageType == PageTypes.Passport && !isNullable) || 
					!(controlInLeftColumn is CheckBox) || (controlInLeftColumn is CheckBox && Checkbox.Checked);
			}
		}

		internal override void Focus()
		{	
			controlInRightColumn?.Focus();
		}

		public override void OnAfterCreate()
		{
			base.OnAfterCreate();
			if (controlInRightColumn != null)
			{
				controlInRightColumn.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
				if (aStyles.HasValue)
					controlInRightColumn.Anchor = aStyles.Value;
			}
		}

		public new static PageControl CreateInstance(XPathNavigator navigator, PageContext context)
		{
			if(IsDependentField(navigator))
				return new PageFieldDependent(navigator, context.PageType);
			else
			{
				ColumnInfo columnInfo = GetColumnInfo(navigator, context);
				FieldTypeResolver typeResolver = GetTypeResolver(navigator, columnInfo);

				if(typeResolver.IsString)
					return new PageFieldText(navigator, columnInfo, context.PageType);
				if(typeResolver.IsDoubleString)
					return new PageFieldTextDoubleString(navigator, columnInfo, context.PageType);
				if(typeResolver.IsBoolean)
					return new PageFieldBoolean(navigator);
				if(typeResolver.IsInteger)
					return new PageFieldNumeric(navigator, columnInfo, 0, context.PageType);
				if (typeResolver.IsDecimal || typeResolver.IsMoney)
					return new PageFieldNumeric(navigator, columnInfo, 2, context.PageType);
				if(typeResolver.IsDate)
					return new PageFieldDate(navigator, columnInfo, context.PageType);
				if(typeResolver.IsTime)
					return new PageFieldTime(navigator, columnInfo, context.PageType);
				if(typeResolver.IsTimeDuration)
					return new PageFieldTimeDuration(navigator, columnInfo, context.PageType);
				if (typeResolver.IsDateTime)
					return new PageFieldFullDateTime(navigator, columnInfo, context.PageType);
				if (typeResolver.IsTextArea)
					return new PageFieldTextAria(navigator, columnInfo, context.PageType);
            }
			return null;
		}

		protected static FieldTypeResolver GetTypeResolver(
			XPathNavigator navigator, ColumnInfo columnInfo)
		{
			FieldTypeResolver typeResolver =
				new FieldTypeResolver(navigator.GetAttribute(Attributes.Type, ""), columnInfo);
			return typeResolver;
		}

		protected static ColumnInfo GetColumnInfo(XPathNavigator navigator, PageContext context)
		{
			ColumnInfo columnInfo = null;
			if(context.Entity != null)
			{
				context.Entity.ColumnsInfo.
					TryGetValue(navigator.GetAttribute(Attributes.Name, ""), out columnInfo);
			}
			return columnInfo;
		}

		protected static Control CreateControlInLeftColumn(PageTypes pageType, XPathNavigator navigator)
		{
			if(pageType == PageTypes.Filter)
				return CreateCheckBox(navigator);

			return CreateLabel(navigator);
		}

		private static Label CreateLabel(XPathNavigator navigator)
		{
			Label label = new Label {AutoSize = true, Text = GetCaption(navigator)};
			return label;
		}

		private static CheckBox CreateCheckBox(XPathNavigator navigator)
		{
			CheckBox checkBox = new CheckBox { Text = GetCaption(navigator), Name = string.Format("{0}_checkbox", GetControlName(navigator)) };
			return checkBox;
		}

		private static string GetCaption(XPathNavigator navigator)
		{
			return navigator.GetAttribute(Attributes.Caption, "");
		}

		private static bool IsDependentField(XPathNavigator navigator)
		{
			return navigator.GetAttribute(Attributes.Source, "") != string.Empty;
		}
	}
}