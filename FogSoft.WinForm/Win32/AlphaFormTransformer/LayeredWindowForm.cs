using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Form=System.Windows.Forms.Form;

namespace FogSoft.WinForm.Win32.AlphaFormTransformer
{
	/// <summary>
	/// This window sets the WS_EX_LAYERED extended style with
	/// a method to set the layered window bitmap
	/// </summary>
	internal class LayeredWindowForm : Form
	{
		#region Constructors
		public LayeredWindowForm()
		{
			FormBorderStyle = FormBorderStyle.None;
		}
		#endregion

		#region Overrides
		/// <summary>
		/// This window can be the active window when you click
		/// to drag it, and it can receive a close event from
		/// the system (e.g., user clicks Alt+F4), therefore we 
		/// instruct the owner to close and cancel the close for
		/// this window.
		/// </summary>
		protected override void OnClosing(CancelEventArgs e)
		{
			e.Cancel = true;
			base.OnClosing(e);
			Owner.Close();
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			InitializeStyles();
			base.OnHandleCreated(e);
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cParms = base.CreateParams;
				cParms.ExStyle |= 0x00080000; // WS_EX_LAYERED
				return cParms;
			}
		}
		#endregion

		#region Methods
		public void SetBits(Bitmap bitmap)
		{
			if (!Image.IsCanonicalPixelFormat(bitmap.PixelFormat) || !Image.IsAlphaPixelFormat(bitmap.PixelFormat))
				throw new ApplicationException("The bitmap must be 32 bits per pixel with an alpha channel.");

			IntPtr oldBits = IntPtr.Zero;
			IntPtr screenDC = Win32.GetDC(IntPtr.Zero);
			IntPtr hBitmap = IntPtr.Zero;
			IntPtr memDc = Win32.CreateCompatibleDC(screenDC);

			try
			{
				Win32.Point topLoc = new Win32.Point(Left, Top);
				Win32.Size bitMapSize = new Win32.Size(bitmap.Width, bitmap.Height);
				Win32.BLENDFUNCTION blendFunc = new Win32.BLENDFUNCTION();
				Win32.Point srcLoc = new Win32.Point(0, 0);

				hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
				oldBits = Win32.SelectObject(memDc, hBitmap);

				blendFunc.BlendOp = Win32.AC_SRC_OVER;
				blendFunc.SourceConstantAlpha = 255;
				blendFunc.AlphaFormat = Win32.AC_SRC_ALPHA;
				blendFunc.BlendFlags = 0;

				Win32.UpdateLayeredWindow(Handle, screenDC, ref topLoc, ref bitMapSize, memDc, ref srcLoc, 0, ref blendFunc, Win32.ULW_ALPHA);
			}
			finally
			{
				if (hBitmap != IntPtr.Zero)
				{
					Win32.SelectObject(memDc, oldBits);
					Win32.DeleteObject(hBitmap);
				}
				Win32.ReleaseDC(IntPtr.Zero, screenDC);
				Win32.DeleteDC(memDc);
			}
		}

		private void InitializeStyles()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			UpdateStyles();
		}
		#endregion
	}
}
