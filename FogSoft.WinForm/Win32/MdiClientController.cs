using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using log4net;

namespace FogSoft.WinForm.Win32
{
	/// <summary>
	/// Listens for messages sent to a <see cref="MdiClient"/>
	/// class and controls its properties.
	/// </summary>
	[ToolboxBitmap(typeof (MdiClientController))]
	public class MdiClientController : NativeWindow
	{
		#region Private Fields

		private Form parentForm;
		private MdiClient mdiClient;
		private Color backColor;

		#endregion // Private Fields

		#region Public Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="FogSoft.WinForm.Win32.MdiClientController"/> class.
		/// </summary>
		public MdiClientController()
			: this(null)
		{
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="FogSoft.WinForm.Win32.MdiClientController"/> class
		/// for the given MDI form.
		/// </summary>
		/// <param name="parentForm">The MDI form.</param>
		public MdiClientController(Form parentForm)
		{
			// Initialize the variables.
			this.parentForm = null;
			mdiClient = null;
			backColor = SystemColors.AppWorkspace;

			// Set the ParentForm property.
			ParentForm = parentForm;
		}

		#endregion // Public Constructors

		#region Public Events

		/// <summary>
		/// Occurs when the control is redrawn.
		/// </summary>
		[Category("Appearance"), Description("Occurs when a control needs repainting.")]
		public event PaintEventHandler Paint;


		/// <summary>
		/// Occurs when the <see cref="NativeWindow"/> handle
		/// is assigned.
		/// </summary>
		[Browsable(false)]
		public event EventHandler HandleAssigned;

		#endregion // Public Events

		#region Public Properties
        

		/// <summary>
		/// Gets or sets the form that the <see cref="MdiClient"/>
		/// control is assigned to.
		/// </summary>
		[Browsable(false)]
		public Form ParentForm
		{
			get { return parentForm; }
			set
			{
				// If the ParentForm has previously been set,
				// unwire events connected to the old parent.
				if (parentForm != null)
					parentForm.HandleCreated -= ParentFormHandleCreated;

				parentForm = value;

				if (parentForm == null)
					return;

				// If the parent form has not been created yet,
				// wait to initialize the MDI client until it is.
				if (parentForm.IsHandleCreated)
				{
					InitializeMdiClient();
					RefreshProperties();
				}
				else
					parentForm.HandleCreated += ParentFormHandleCreated;
			}
		}


		/// <summary>
		/// Gets the <see cref="MdiClient"/> being controlled.
		/// </summary>
		[Browsable(false)]
		public MdiClient MdiClient
		{
			get { return mdiClient; }
		}


		/// <summary>
		/// Gets or sets the background color for the control.
		/// </summary>
		[Category("Appearance"), DefaultValue(typeof (Color), "AppWorkspace")]
		[Description("The backcolor used to display text and graphics in the control.")]
		public Color BackColor
		{
			// Use the BackColor property of the MdiClient control. This is one of
			// the few properties in the MdiClient class that actually works.

			get
			{
				if (mdiClient != null)
					return mdiClient.BackColor;

				return backColor;
			}
			set
			{
				backColor = value;
				if (mdiClient != null)
					mdiClient.BackColor = value;
			}
		}
        
		/// <summary>
		/// Gets the handle for this window.
		/// </summary>
		[Browsable(false)]
		public new IntPtr Handle
		{
			// Hide this from the property grid during design-time.
			get { return base.Handle; }
		}

		#endregion // Public Properties

		#region Public Methods

		/// <summary>
		/// Reestablishes a connection to the <see cref="MdiClient"/>
		/// control if the <see cref="FogSoft.WinForm.Win32.MdiClientController.ParentForm"/>
		/// hasn't changed but its <see cref="Form.IsMdiContainer"/>
		/// property has.
		/// </summary>
		public void RenewMdiClient()
		{
			// Reinitialize the MdiClient and its properties.
			InitializeMdiClient();
			RefreshProperties();
		}

		#endregion // Public Methods

		#region Protected Methods

		/// <summary>
		/// Invokes the default window procedure associated with this window.
		/// </summary>
		/// <param name="m">A <see cref="Message"/> that is associated with the current Windows message. </param>
		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
					//Do all painting in WM_PAINT to reduce flicker.
				case WM_ERASEBKGND:
					return;

				case WM_PAINT:

					// This code is influenced by Steve McMahon's article:
					// "Painting in the MDI Client Area".
					// http://vbaccelerator.com/article.asp?id=4306

					// Use Win32 to get a Graphics object.
					Win32.PAINTSTRUCT paintStruct = new Win32.PAINTSTRUCT();
					IntPtr screenHdc = Win32.BeginPaint(m.HWnd, ref paintStruct);

					using (Graphics screenGraphics = Graphics.FromHdc(screenHdc))
					{
						// Get the area to be updated.
						Rectangle clipRect = new Rectangle(
							paintStruct.rcPaint.left,
							paintStruct.rcPaint.top,
							paintStruct.rcPaint.right - paintStruct.rcPaint.left,
							paintStruct.rcPaint.bottom - paintStruct.rcPaint.top);

						// Double-buffer by painting everything to an image and
						// then drawing the image.
						int width = (mdiClient.ClientRectangle.Width > 0 ? mdiClient.ClientRectangle.Width : 0);
						int height = (mdiClient.ClientRectangle.Height > 0 ? mdiClient.ClientRectangle.Height : 0);
						using (Image i = new Bitmap(width, height))
						{
							using (Graphics g = Graphics.FromImage(i))
							{
								// This code comes from J Young's article:
								// "Generating missing Paint event for TreeView and ListView".
								// http://www.codeproject.com/cs/miscctrl/genmissingpaintevent.asp

								// Draw base graphics and raise the base Paint event.
								IntPtr hdc = g.GetHdc();
								Message printClientMessage =
									Message.Create(m.HWnd, WM_PRINTCLIENT, hdc, IntPtr.Zero);
								DefWndProc(ref printClientMessage);
								g.ReleaseHdc(hdc);

								// Call our OnPaint here to draw graphics over the
								// original and raise our Paint event.
								OnPaint(new PaintEventArgs(g, mdiClient.ClientRectangle));
							}

							// Now draw all the graphics at once.
							screenGraphics.DrawImage(i, mdiClient.ClientRectangle);
						}
					}

					Win32.EndPaint(m.HWnd, ref paintStruct);
					return;

				case WM_SIZE:

					// Repaint on every resize.
					mdiClient.Invalidate();
					break;

				case WM_SCROLL:
					
					// Repaint on every scroll.
					mdiClient.Invalidate();
					break;
					
			}
			base.WndProc(ref m);
		}

		/// <summary>
		/// Raises the <see cref="FogSoft.WinForm.Win32.MdiClientController.Paint"/> event.
		/// </summary>
		/// <param name="e">A <see cref="PaintEventArgs"/> that
		/// contains the event data.</param>
		protected virtual void OnPaint(PaintEventArgs e)
		{
			// Raise the Paint event.
			if (Paint != null)
				Paint(this, e);
		}


		/// <summary>
		/// Raises the <see cref="FogSoft.WinForm.Win32.MdiClientController.HandleAssigned"/> event.
		/// </summary>
		/// <param name="e">A <see cref="System.EventArgs"/> that contains the event
		/// data.</param>
		protected virtual void OnHandleAssigned(EventArgs e)
		{
			// Raise the HandleAssigned event.
			if (HandleAssigned != null)
				HandleAssigned(this, e);
		}

		#endregion // Protected Methods

		#region Private Methods

		private void InitializeMdiClient()
		{
			// If the mdiClient has previously been set, unwire events connected
			// to the old MDI.
			if (mdiClient != null)
				mdiClient.HandleDestroyed -= MdiClientHandleDestroyed;

			if (parentForm == null)
				return;

			// Get the MdiClient from the parent form.
			for (int i = 0; i < parentForm.Controls.Count; i++)
			{
				// If the form is an MDI container, it will contain an MdiClient control
				// just as it would any other control.
				mdiClient = parentForm.Controls[i] as MdiClient;
				if (mdiClient != null)
				{
					// Assign the MdiClient Handle to the NativeWindow.
					ReleaseHandle();
					AssignHandle(mdiClient.Handle);

					// Raise the HandleAssigned event.
					OnHandleAssigned(EventArgs.Empty);

					// Monitor the MdiClient for when its handle is destroyed.
					mdiClient.HandleDestroyed += MdiClientHandleDestroyed;
				}
			}
		}
        
		private void MdiClientHandleDestroyed(object sender, EventArgs e)
		{
			// If the MdiClient handle has been released, drop the reference and
			// release the handle.
			if (mdiClient != null)
			{
				mdiClient.HandleDestroyed -= MdiClientHandleDestroyed;
				mdiClient = null;
			}

			ReleaseHandle();
		}


		private void ParentFormHandleCreated(object sender, EventArgs e)
		{
			// The form has been created, unwire the event, and initialize the MdiClient.
			parentForm.HandleCreated -= ParentFormHandleCreated;
			InitializeMdiClient();
			RefreshProperties();
		}


		private void RefreshProperties()
		{
			// Refresh all the properties
			BackColor = backColor;
		}

		// If you don't own a Weezer album then you're really missing out on some
		// fantastic music.

		#endregion // Private Methods

		#region Win32

		private const int WM_PAINT = 0x000F;
		private const int WM_ERASEBKGND = 0x0014;
		private const int WM_SIZE = 0x0005;
		private const int WM_PRINTCLIENT = 0x0318;
		private const int WM_SCROLL = 0x0084;





		

		#endregion // Win32
	}
}