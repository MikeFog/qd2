using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm.Passport.Classes
{
	// Словари констант (Attributes, InitialValueAbbreviations) вынесены в
	// PageControl.Constants.cs — они нужны коду без UI.
	// См. docs/tasks/web-migration.md, этап 0.
	public abstract partial class PageControl
	{
		public enum ShowStatus
		{
			create, edit, always
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

		/// <summary>
		/// Высота, нужная контролу, чтобы показать подпись целиком с переносом по словам.
		/// Вызывать только после добавления в parent: у неприсоединённого контрола
		/// Control.DefaultFont, а рисуется он шрифтом формы. Замер чужим шрифтом занижал
		/// число строк, и хвост подписи обрезался.
		/// </summary>
		protected static int GetWrappedTextHeight(Control control, int width)
		{
			if(string.IsNullOrEmpty(control.Text)) return control.Font.Height;

			control.AutoSize = true;
			Size preferred = control.PreferredSize;
			control.AutoSize = false;

			// разница с плоским замером - глиф чекбокса и отступы: по ширине на текст остаётся
			// меньше, по высоте нужен запас, иначе последняя строка не влезает
			Size flat = TextRenderer.MeasureText(control.Text, control.Font);
			Size textSize = TextRenderer.MeasureText(
				control.Text, control.Font,
				new Size(width - (preferred.Width - flat.Width), int.MaxValue), TextFormatFlags.WordBreak);

			return textSize.Height + (preferred.Height - flat.Height);
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