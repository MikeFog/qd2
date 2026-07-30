using System;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Forms;
using Merlin.Classes;
using FogSoft.WinForm.Passport.Classes;
using System.IO;
using System.Drawing;

namespace Merlin.Forms
{
	public partial class MassmediaPassport : PassportForm
	{
		public bool isInit = false;
		private readonly Massmedia _radioStation;

		public MassmediaPassport(Massmedia massmedia, DataSet ds)
            : base(massmedia, ds)
		{
			InitializeComponent();
			_radioStation = massmedia;
		}

		public MassmediaPassport()
		{
			InitializeComponent();
		}

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);

			if (!isInit)
			{
				// страница с подписью не обязана быть последней (после неё может быть
				// добавлена другая, например "Политическая агитация") - ищем контрол
				// картинки по всем страницам, а не по последнему индексу
				foreach (var page in pages)
				{
					foreach (PageControl ctl in page.PageControls)
					{
						if (ctl is PageFieldImage ctlImage)
						{
							ctlImage.SetImage(GetImage());
						}
					}
				}
			}
			isInit = true;
		}

		private void RolTypeSelected(object sender, EventArgs e)
		{
			try
			{
				LookUp lookup = (LookUp)sender;
				//ShowHideDJinProperties(lookup);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void ShowHideDJinProperties(LookUp lookup)
		{
			if (!string.IsNullOrEmpty(lookup.GetValue("id").ToString()))
			{
				int type = int.Parse(lookup.GetValue("id").ToString());

				bool isAudio = true;
				int tabToShow = isAudio ? 4 : 5;
				int tabToHide = isAudio ? 5 : 4;

				if (!tabPassport.TabPages.Contains(pages[tabToShow].Page))
					tabPassport.TabPages.Add(pages[tabToShow].Page);

				tabPassport.TabPages.Remove(pages[tabToHide].Page);
				pages[tabToHide].ResetValues();
			}
		}

        private Image GetImage()
        {
			return _radioStation.Signature;
        }
	}
}