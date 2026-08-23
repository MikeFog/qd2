using System;
using System.Runtime.InteropServices;

namespace FogSoft.WinForm.Win32
{
	public class Win32
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct Size
		{
			public Int32 cx;
			public Int32 cy;

			public Size(Int32 x, Int32 y)
			{
				cx = x;
				cy = y;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct BLENDFUNCTION
		{
			public byte BlendOp;
			public byte BlendFlags;
			public byte SourceConstantAlpha;
			public byte AlphaFormat;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Point
		{
			public Int32 x;
			public Int32 y;

			public Point(Int32 x, Int32 y)
			{
				this.x = x;
				this.y = y;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct PAINTSTRUCT
		{
			public IntPtr hdc;
			public int fErase;
			public RECT rcPaint;
			public int fRestore;
			public int fIncUpdate;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public byte[] rgbReserved;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public readonly int left;
			public readonly int top;
			public readonly int right;
			public readonly int bottom;
		}

		public const byte AC_SRC_OVER = 0;
		public const Int32 ULW_ALPHA = 2;
		public const byte AC_SRC_ALPHA = 1;

		[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

		[DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern IntPtr GetDC(IntPtr hWnd);

		[DllImport("gdi32.dll", ExactSpelling = true)]
		public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObj);

		[DllImport("user32.dll", ExactSpelling = true)]
		public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

		[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern int DeleteDC(IntPtr hDC);

		[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern int DeleteObject(IntPtr hObj);

		[DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern int UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst, ref Size psize, IntPtr hdcSrc, ref Point pptSrc, Int32 crKey, ref BLENDFUNCTION pblend, Int32 dwFlags);

		[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern IntPtr ExtCreateRegion(IntPtr lpXform, uint nCount, IntPtr rgnData);

		[DllImport("user32.dll")]
		public static extern IntPtr BeginPaint(IntPtr hWnd, ref PAINTSTRUCT paintStruct);

		[DllImport("user32.dll")]
		public static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT paintStruct);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int GetWindowLong(IntPtr hWnd, int Index);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int SetWindowLong(IntPtr hWnd, int Index, int Value);

		[DllImport("user32.dll", ExactSpelling = true)]
		public static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	}
}
