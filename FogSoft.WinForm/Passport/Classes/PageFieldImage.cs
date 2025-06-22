using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Properties;

namespace FogSoft.WinForm.Passport.Classes
{
	public class PageFieldImage : PageField
	{
		private readonly Button _btnDelete;
		private readonly PictureBox _picture;

		public PageFieldImage(XPathNavigator navigator, PageTypes type)
			: base(navigator, new Button(), type)
		{
			Button.Image = Resources.open2;
			Button.Click += Button_Click;
			_picture = new PictureBox();
			string height = navigator.GetAttribute(Attributes.Height, "");
			Picture.Height = string.IsNullOrEmpty(height) ? 60 : ParseHelper.ParseToInt32(height);
			_btnDelete = new Button();
			_btnDelete.Click += (BtnDelete_Click);
			_btnDelete.Image = Resources.DeleteItem;
		}

		private PictureBox Picture
		{
			get { return _picture; }
		}

		public void SetImage(Image img)
		{
			Image = img;
			Picture.Image = PaintImage(img);
		}

		private Button Button
		{
			get { return (Button) controlInRightColumn; }
		}

		public Image Image { get; set; }

		public override int Height
		{
			get { return Math.Max(base.Height, Picture.Height + 7 + Button.Height); }
		}

		private void BtnDelete_Click(object sender, EventArgs e)
		{
            Image = null;
			Picture.Image = null;
			FireValueChanged();
		}

		public override void Add2Page(Control parent, int left, int top, PageDimensions dimensions)
		{
			base.Add2Page(parent, left, top, dimensions);

			Picture.Width = Button.Width;
			Picture.Left = Button.Left;
			Picture.Top = Button.Bottom + 7;
			parent.Controls.Add(Picture);
			Button.Width = Button.Height;
			_btnDelete.Top = Button.Top;
			_btnDelete.Left = Button.Right + 7;
			_btnDelete.Height = Button.Height;
			_btnDelete.Width = Button.Height;
			parent.Controls.Add(_btnDelete);
			
		}

		private void Button_Click(object sender, EventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog
			                     	{
			                     		Filter = "Файлы изображений(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG"
			                     	};
			Application.DoEvents();
			if (dlg.ShowDialog(Globals.MdiParent) == DialogResult.OK)
			{
				Image = GetImage(Image.FromFile(dlg.FileName, true));
				SetImage(Image);
				FireValueChanged();
			}
		}

		public override void ApplyChanges(Dictionary<string, object> parameters)
		{
            if (Image != null)
            {
                using (MemoryStream stream = new MemoryStream())
                {

                    Image.Save(stream, ImageFormat.Bmp);
                    parameters[Name] = stream.ToArray();

                }
            }
            else
            {
                parameters[Name] = null;
            }
		}

		public override void SetValue(Dictionary<string, object> parameters)
		{
			base.SetValue(parameters);
			if (parameters.ContainsKey(Name) && parameters[Name] != DBNull.Value)
			{
				using (MemoryStream stream = new MemoryStream((byte[]) parameters[Name]))
				{
					Image = Image.FromStream(stream);
					SetImage(Image);
				}
			}
		}

		private static Image GetImage(Image image)
		{
			Bitmap bmp = new Bitmap(image.Width, image.Height);
			Graphics g = Graphics.FromImage(bmp);
			g.FillRectangle(Brushes.White, 0, 0, bmp.Width, bmp.Height);
			g.DrawImage(new Bitmap(image), 0, 0, image.Width, image.Height);
			g.Dispose();
			return bmp;
		}

		private Image PaintImage(Image img)
		{
			if (img == null)
				return null;

			Bitmap bmp = new Bitmap(Picture.Width, Picture.Height);
			Graphics g = Graphics.FromImage(bmp);
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			Size imgSize = GetImageSize(img.Width, img.Height, Picture.Width, Picture.Height);
			g.DrawImage(new Bitmap(img), 0, 0, imgSize.Width, imgSize.Height);
			g.Dispose();
			return bmp;
		}

		public override void OnAfterCreate()
		{
			base.OnAfterCreate();

			Button.Anchor = AnchorStyles.Left | AnchorStyles.Top;
			_btnDelete.Anchor = AnchorStyles.Left | AnchorStyles.Top;
		}

		public static Size GetImageSize(int width, int height, int? wMax, int? hMax)
		{
			float w = 1;
			float h = 1;
			if (wMax.HasValue && wMax.Value > 0)
				w = ((float)wMax.Value) / ((float)width);
			if (hMax.HasValue && hMax.Value > 0)
				h = ((float)hMax.Value) / ((float)height);
			float scale = Math.Min(w, h);
			return new Size(Math.Min(width, (int)(((float)width) * scale)), Math.Min(height, (int)(((float)height) * scale)));
		}
	}
}