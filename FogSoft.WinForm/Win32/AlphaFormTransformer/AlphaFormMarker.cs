using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Color=System.Drawing.Color;
using Pen=System.Drawing.Pen;
using Rectangle=System.Drawing.Rectangle;
using SolidBrush=System.Drawing.SolidBrush;

namespace FogSoft.WinForm.Win32.AlphaFormTransformer
{
	/// <summary>
	/// AlphaFormMarker serves as a design time aid for specifying 
	/// one or more points in the background image that will be used
	/// to build the main form's Region. In the designer, it must
	/// always be added to the AlphaFormTransformer control (not the main form)
	/// </summary>
	/// <remarks>
	/// At runtime all instances of AlphaFormMarker are made invisible.
	/// </remarks>
	public class AlphaFormMarker : UserControl
	{
		#region Class Variables
		uint m_fillBorder = 4;
		#endregion

		#region Constructors
		public AlphaFormMarker()
		{
			Bounds = new Rectangle(Location, new Size(17, 17));
		}
		#endregion

		#region Overrides
		protected override void OnHandleCreated(EventArgs e)
		{
			InitializeStyles();
			base.OnHandleCreated(e);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			SolidBrush sb = new SolidBrush(Color.FromArgb(255, 255, 0, 0));
			Pen p = new Pen(sb, 1);
			e.Graphics.DrawEllipse(p, new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1));
			e.Graphics.DrawLine(p, Bounds.Width / 2, 0, Bounds.Width / 2, Bounds.Height);
			e.Graphics.DrawLine(p, 0, Bounds.Height / 2, Bounds.Width, Bounds.Height / 2);
			p.Dispose();
			sb.Dispose();
		}
		#endregion

		#region Properties
		[Category("Marker Properties"), Description("Fill Border (Pixels)")]
		[DefaultValue(4)]
		public uint FillBorder
		{
			get
			{
				return m_fillBorder;
			}
			set
			{
				m_fillBorder = value;
			}
		}
		#endregion

		#region Methods
		void InitializeStyles()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			UpdateStyles();
		}
		#endregion

	}
}
