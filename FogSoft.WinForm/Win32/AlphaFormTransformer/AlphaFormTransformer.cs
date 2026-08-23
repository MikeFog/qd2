using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ArrayList=System.Collections.ArrayList;

namespace FogSoft.WinForm.Win32.AlphaFormTransformer
{
	/// <summary>
	/// A single instance of AlphaFormTransformer is added to the form
	/// to be transformed to an alpha channel border window. It hosts all
	/// controls to be displayed on the window, the image for the layered
	/// window that displays the image border, and one or more AlphaFormMarkers
	/// to construct the main form's region. 
	/// </summary> 
	public class AlphaFormTransformer : Panel
	{
		#region Class Variables
		bool m_drag = false;
		Point m_dragStart;
		LayeredWindowForm m_lwin = null;
		Bitmap m_alphaBitmap = null;
		uint m_dragSleep = 30;
		#endregion

		#region Constructors
		public AlphaFormTransformer()
		{
			Size = new Size(250, 250);
		}
		#endregion

		#region Overrides
		/// <summary>
		/// Here we're setting the bits of the layered window in response
		/// to OnPaint in the transformer control. You're not supposed to have
		/// to update the layered window RGB as the OS keeps it internally and
		/// double buffers it onto the desktop. And we initially set the bits
		/// in TransformForm(). However we found cases where, for some reason, 
		/// it appears to get lost or corrupted after the user Alt+Tabs or selects
		/// the application from the task bar. It only happens on XP. This 
		/// seems to fix it and there's not much overhead in any case (more or
		/// less equivalent to a blit). 
		/// </summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			if (m_alphaBitmap != null && m_lwin != null)
			{
				m_lwin.SetBits(m_alphaBitmap);
				ParentForm.Update();
			}
		}
		#endregion

		#region Properties
		[Category("Alpha Transformer Properties"), Description("Drag Sleep in Milliseconds (Ignored on Vista)")]
		[DefaultValue(30)]
		public uint DragSleep
		{
			get
			{
				return m_dragSleep;
			}
			set
			{
				m_dragSleep = value;
			}
		}

		private Form ParentForm
		{
			get
			{
				return (Form)TopLevelControl;
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// This methods does the following:
		/// 1. Sets the layered window (m_lwin) image data from the bitmap.
		/// 2. Constructs the main form's Region from the AlphaFormMarkers in 
		/// this control. 
		/// 3. Updates the layered window's bitmap.
		/// </summary>
		/// <remarks>
		/// If this routine is used at runtime to change the alpha channel image,
		/// it does nothing to resize the main form, this control, or the  
		/// the AlphaFormMarkers. If the user intends to update the image
		/// with one that has a *different* alpha channel, he'll need to do the
		/// following BEFORE calling this routine:
		/// 1. If the image size has changed, resize the main form (and this control if it's not docked)
		/// 2. Reposition the AlphaFormMarker controls (potentially adding new 
		/// ones) as needed so the form Region can be calculated. 
		/// 3. Reposition any other controls on the form.
		/// On the other hand, if we're simply swapping in an image with the same
		/// size and alpha channel, then simply call this routine.
		/// </remarks>
		public void UpdateSkin(Bitmap bm)
		{
			if (!Bitmap.IsCanonicalPixelFormat(bm.PixelFormat) || !Bitmap.IsAlphaPixelFormat(bm.PixelFormat))
				throw new ApplicationException("The bitmap must be 32 bits per pixel with an alpha channel.");

			m_alphaBitmap = bm;

			// Build a array of the alpha values taken from the background image.
			// Some of the transparent pixels in this array will be the target of
			// what is essentially a seed fill operation in UpdateRectListFromAlpha().
			// As some members are set to a non-zero value (i.e., filled), corresponding 
			// rectangles are added to a running arraylist of rectangles that will
			// eventually be used to build the main form's Region.

			BitmapData bData = m_alphaBitmap.LockBits(
			  new Rectangle(0, 0, m_alphaBitmap.Width, m_alphaBitmap.Height),
					  ImageLockMode.ReadOnly,
			  m_alphaBitmap.PixelFormat);

			byte[,] alphaArr = new byte[m_alphaBitmap.Width, m_alphaBitmap.Height];

#if FAST_ALPHA_BUILD

			// Here is an optional way to build the alpha array. It's
			// fast but requires pointer access.
			unsafe
			{
				byte *line = (byte*)bData.Scan0;
				byte *alpha;
		
				for (int j = 0; j < m_alphaBitmap.Height; j++, line += bData.Stride)
				{
					// image data is r->g->b->a in memory
					alpha = line + 3;
					for (int i = 0; i < m_alphaBitmap.Width; i++, alpha += 4)
						alphaArr[i,j] = *alpha;
				}
			}
#else

			// Seems faster albeit somewhat wasteful to marshal to a 
			// managed array than to use Bitmap.GetPixel. If you're
			// OK with unsafe code, then define FAST_ALPHA_BUILD and
			// enable unsafe compilation 

			byte[] mngImgData = new byte[m_alphaBitmap.Height * bData.Stride];
			Marshal.Copy(bData.Scan0, mngImgData, 0, mngImgData.Length);
			for (int j = 0; j < m_alphaBitmap.Height; j++)
			{
				int ai = j * bData.Stride + 3;
				for (int i = 0; i < m_alphaBitmap.Width; i++, ai += 4)
				{
					alphaArr[i, j] = mngImgData[ai];
				}
			}
#endif

			m_alphaBitmap.UnlockBits(bData);

			Rectangle bounds = new Rectangle();
			ArrayList rectList = new ArrayList();

			// The location of each AlphaFormMarker control serves as a seed point location
			// for building the set of rectangles that describe the enclosed transparent region 
			// (within the background image's alpha channel) around that point.

			foreach (Control cntrl in Controls)
			{
				if (typeof(AlphaFormMarker).IsInstanceOfType(cntrl))
				{
					AlphaFormMarker marker = (AlphaFormMarker)cntrl;

					UpdateRectListFromAlpha(
						rectList,
						ref bounds,
						new Point(marker.Location.X + marker.Width / 2, marker.Location.Y + marker.Height / 2),
						alphaArr,
						m_alphaBitmap.Width,
						m_alphaBitmap.Height,
						(int)marker.FillBorder);

					// Hide the marker as we don't want to see it at run time.
					marker.Visible = false;
				}
			}

			// Build the main form's region
			ParentForm.Region = RegionFromRectList(rectList, bounds);

			// Set the layered window bitmap
			m_lwin.SetBits(m_alphaBitmap);
		}


		/// <summary>
		/// This methods does the following:
		/// 1. Constructs a LayeredWindowForm and shows it. The layered form's
		/// bitmap is built from this control's BackgroundImage.
		/// 2. Calls UpdateSkin to updated the layered window's image data
		/// and build the main form's Region.
		/// 3. The BackgroundImage is set to the main form's Background image.
		/// This methods should only be called *once* from the main form's
		/// Load event. If the user wants to change the image at runtime,
		/// UpdateSkin should be used.
		/// </summary>
		public void TransformForm()
		{
			if (DesignMode)
				return;

			m_lwin = new LayeredWindowForm();

			// Setting the layered window's TopMost to the main
			// form's value keeps the relative Z order the same for 
			// the pair of windows
			m_lwin.TopMost = ParentForm.TopMost;

			// We don't want the layered window form to show in the 
			// taskbar now do we :-)
			m_lwin.ShowInTaskbar = false;

			// These will handle dragging for both the layered window
			// and the main form.
			m_lwin.MouseDown += new MouseEventHandler(LayeredFormMouseDown);
			m_lwin.MouseMove += new MouseEventHandler(LayeredFormMouseMove);
			m_lwin.MouseUp += new MouseEventHandler(LayeredFormMouseUp);
			ParentForm.Move += new EventHandler(ParentFormMove);

			// Form must be shown by specifying an owner so that activation of
			// the layered form activates the main app. It's also needed to keep
			// the Z order of the two windows in sync. Note that it's not 
			// necessary to set the size of the layered window. The call to 
			// SetBits() does this.
			m_lwin.Show(ParentForm);
			m_lwin.Location = ParentForm.Location;

			Bitmap aphaBmap;

			if (BackgroundImage != null)
			{
				// A prerequisite with this control is that the main form's size
				// is set equal to the background image size. However, if the main form's
				// AutoScaleMode is set to DPI, and the application is run
				// on a system where the font or DPI resolution differs from the design
				// time values, then .NET will scale the form and all its controls. However
				// the background image is not scaled, so we'll catch that
				// condition here and scale the image accordingly. Again, this logic works
				// *assuming* you always set the main form's size equal to the image size 
				// at design time.
				if (BackgroundImage.Size != ParentForm.Size)
				{
					aphaBmap = new Bitmap(ParentForm.Width, ParentForm.Height);
					Graphics gr = Graphics.FromImage(aphaBmap);
					gr.SmoothingMode = SmoothingMode.HighQuality;
					gr.CompositingQuality = CompositingQuality.HighQuality;
					gr.InterpolationMode = InterpolationMode.HighQualityBicubic;

					gr.DrawImage(BackgroundImage, new Rectangle(0, 0, ParentForm.Width, ParentForm.Height),
						new Rectangle(0, 0, BackgroundImage.Width, BackgroundImage.Height), GraphicsUnit.Pixel);
					gr.Dispose();
				}
				else
					aphaBmap = new Bitmap(BackgroundImage);

				// Update the layered window bits and form region
				UpdateSkin(aphaBmap);

				// Swap in the main form's background image (if any) and layout
				BackgroundImage = ParentForm.BackgroundImage;
				BackgroundImageLayout = ParentForm.BackgroundImageLayout;

				if (BackColor == Color.Transparent)
				{
					// user may have it this way in the designer too see the 
					// main form's background image, but it can't be set to 
					// this at runtime as the background image will be copied from
					// the main form to this control. Even in the absence of a background
					// image, it can't be transparent as it won't composite correctly
					// with the main form at runtime 
					BackColor = SystemColors.Control;
				}
			}
		}

		void PushSeg(System.Collections.Stack stack, int x1, int xr, int y, int dy, int height)
		{
			int
				yn = y + dy;
			if (yn >= 0 && yn < height)
				stack.Push(new LineSeg(x1, xr, y, dy));
		}

		void PopSeg(System.Collections.Stack stack, out int xl, out int xr, out int y, out int dy)
		{
			LineSeg lseg = (LineSeg)(stack.Pop());
			xl = lseg.x1;
			xr = lseg.x2;
			dy = lseg.dy;
			y = lseg.y + dy;
		}

		/// <summary>
		/// Updates a list of rectangles using a seedfill of the alpha channel
		/// array starting from the seedPt. 
		/// 
		/// The border value specifies how far into the non-transparent pixel border
		/// the region will be constructed. The composited image will typically have
		/// semitransparent edges (in fact you *want* this for esthetic reasons, and 
		/// it might be a consequence of antialiasing, Photoshop mask feathering, or
		/// whatever tool you use to construct the image). Therefore the region that's
		/// built from the marker position needs to expand some number of pixels into
		/// (and *under* from the compositing point of view) the semitransparent area, 
		/// otherwise you will see through to the desktop along these borders. You want
		/// the border value to be large enough the cover the thickness of the
		/// semitransparent edge (a few pixels), but not too large which might
		/// cause it to extend past the other side of the masked image boundary.
		/// </summary>
		/// <remarks>
		/// The substantive part of this routine is adapted from Paul Heckbert's
		/// seed fill algorithm ("Graphics Gems", Academic Press, 1990). From
		/// the original C implementation we got rid of a goto and changed the
		/// fixed stack size design to use a Collections.Stack.
		/// The fill value in this case is any non-zero alpha value. 
		/// For each filled pixel location, a rectangle is added to the rectList 
		/// which will be used by the caller to build a region. Finally, a bounding
		/// rectangle is calculated as it's needed for region construction. 
		/// </remarks>
		void UpdateRectListFromAlpha(ArrayList rectList, ref Rectangle bounds, Point seedPt, byte[,] alphaArr, int width, int height, int border)
		{
			Stack segStack = new Stack();
			Rectangle addRct = new Rectangle();
			int left, x;

			if (rectList.Count == 0)
			{
				bounds.X = Int32.MaxValue;
				bounds.Y = Int32.MaxValue;
				bounds.Width = 0;
				bounds.Height = 0;
			}

			if (alphaArr[seedPt.X, seedPt.Y] != 0 || seedPt.X < 0 || seedPt.X >= width || seedPt.Y < 0 || seedPt.Y >= height)
				return;

			PushSeg(segStack, seedPt.X, seedPt.X, seedPt.Y, 1, height);
			PushSeg(segStack, seedPt.X, seedPt.X, seedPt.Y + 1, -1, height);

			int
				x1, x2, y, dy;

			while (segStack.Count > 0)
			{
				PopSeg(segStack, out x1, out x2, out y, out dy);
				for (x = x1; x >= 0 && alphaArr[x, y] == 0; --x)
				{
					addRct.X = x - border;
					addRct.Y = y - border;
					addRct.Width = 2 * border + 1;
					addRct.Height = addRct.Width;
					rectList.Add(addRct);

					if (addRct.Left < bounds.Left)
						bounds.X = addRct.Left;
					if (addRct.Top < bounds.Top)
						bounds.Y = addRct.Top;
					if (addRct.Width > bounds.Width)
						bounds.Width = addRct.Width;
					if (addRct.Height > bounds.Height)
						bounds.Height = addRct.Height;

					// any non-zero value will do
					alphaArr[x, y] = 1;
				}

				if (x < x1)
				{
					left = x + 1;
					if (left < x1)
						PushSeg(segStack, left, x1 - 1, y, -dy, height);
					x = x1 + 1;
				}
				else
				{
					for (++x; x <= x2 && alphaArr[x, y] != 0; ++x)
					{ ;}
					left = x;
					if (x > x2)
						continue;
				}

				do
				{
					for (; x < width && alphaArr[x, y] == 0; ++x)
					{
						addRct.X = x - border;
						addRct.Y = y - border;
						addRct.Width = 2 * border + 1;
						addRct.Height = addRct.Width;
						rectList.Add(addRct);

						if (addRct.X < bounds.Left)
							bounds.X = addRct.Left;
						if (addRct.Y < bounds.Top)
							bounds.Y = addRct.Top;
						if (addRct.Width > bounds.Width)
							bounds.Width = addRct.Width;
						if (addRct.Height > bounds.Height)
							bounds.Height = addRct.Height;

						// any non-zero value will do
						alphaArr[x, y] = 1;
					}

					PushSeg(segStack, left, x - 1, y, dy, height);

					if (x > x2 + 1)
						PushSeg(segStack, x2 + 1, x - 1, y, -dy, height);

					for (++x; x <= x2 && alphaArr[x, y] != 0; ++x)
					{ ;}

					left = x;
				} while (x <= x2);
			}
		}

		/// <summary>
		/// The region is constructed from a list of Rectangles using the
		/// Win32 ExtCreateRegion() call. 
		/// </summary>
		/// <remarks>
		/// If you're wondering why we use this unmanged API, it's because
		/// GraphicsPath.AddRectangles() followed by instantiating the Region
		/// from the path is *terribly* slow - more than an order of magnitude
		/// slower than ExtCreateRegion. And Region.Union is slower still 
		///	(and broken to boot). 
		/// </remarks>
		Region RegionFromRectList(ArrayList rectList, Rectangle bounds)
		{
			uint
				dSize = (uint)(32 + rectList.Count * 16);

			IntPtr
				rgnData = Marshal.AllocHGlobal((Int32)dSize);

			// We're just filling out the equivalent of a RGNDATA 
			// followed by an array of Win32 RECTs
			int[] mngRgnData = new int[rectList.Count * 4 + 8];
			mngRgnData[0] = 32;											// dwSize
			mngRgnData[1] = 1;											// iType = RDH_RECTANGLES
			mngRgnData[2] = rectList.Count;					// nCount
			mngRgnData[3] = rectList.Count * 16;		// nRgnSize
			mngRgnData[4] = bounds.Left;						// rcBound
			mngRgnData[5] = bounds.Top;							// .
			mngRgnData[6] = bounds.Width;						// .
			mngRgnData[7] = bounds.Height;					// .
			// Array of RECTs
			for (int i = 0; i < rectList.Count; i++)
			{
				Rectangle mngRct = (Rectangle)rectList[i];
				mngRgnData[4 * i + 8] = mngRct.Left;
				mngRgnData[4 * i + 9] = mngRct.Top;
				mngRgnData[4 * i + 10] = mngRct.Right;
				mngRgnData[4 * i + 11] = mngRct.Bottom;
			}

			Marshal.Copy(mngRgnData, 0, rgnData, mngRgnData.Length);
			IntPtr nativeRgn = Win32.ExtCreateRegion(new IntPtr(0), dSize, rgnData);
			Marshal.FreeHGlobal(rgnData);
			return Region.FromHrgn(nativeRgn);
		}

		void LayeredFormMouseDown(Object sender, MouseEventArgs e)
		{
			OnClick(e);
			if (e.Button == MouseButtons.Left)
			{
				m_drag = true;
				m_dragStart = e.Location;
			}
		}

		void LayeredFormMouseMove(Object sender, MouseEventArgs e)
		{
			if (m_drag)
			{
				int dx = e.X - m_dragStart.X;
				int dy = e.Y - m_dragStart.Y;

				// Move the main form - ParentFormMove will move the layered window
				ParentForm.Location = new Point(ParentForm.Location.X + dx, ParentForm.Location.Y + dy);
				ParentForm.Update();
			}
		}

		void LayeredFormMouseUp(Object sender, MouseEventArgs e)
		{
			m_drag = false;
		}

		void ParentFormMove(Object sender, EventArgs e)
		{
			m_lwin.Location = ParentForm.Location;
			// On XP or earlier, we wait a specified number of milliseconds
			// to allow the OS to update invalidated areas revealed by dragging.
			// The trade off here is slightly less responsiveness while dragging
			// for reduced repainting unsightliness (not our windows, but other
			// windows behind). On Vista, it's unnecessary as everything is double-buffered.
			if (m_drag && System.Environment.OSVersion.Version.Major < 6)
				System.Threading.Thread.Sleep((int)m_dragSleep);
		}
		#endregion

	}
}
