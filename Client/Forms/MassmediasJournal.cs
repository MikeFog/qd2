using System;
using FogSoft.WinForm.Classes;

namespace Merlin.Forms
{
	public partial class MassmediasJournal : FogSoft.WinForm.Forms.JournalForm
	{
		public MassmediasJournal(string text)
			:base(EntityManager.GetEntity((int)Entities.MassMedia), text)
		{
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			Grid.ObjectChanged += new FogSoft.WinForm.ObjectDelegate(Grid_ObjectChanged);
			Grid.ObjectCreated += new FogSoft.WinForm.ObjectDelegate(Grid_ObjectCreated);
			Grid.ObjectDeleted += new FogSoft.WinForm.ObjectDelegate(Grid_ObjectDeleted);
		}

		private void Grid_ObjectDeleted(PresentationObject presentationObject)
		{
			RefreshJournal();
		}

		private void Grid_ObjectCreated(PresentationObject presentationObject)
		{
			RefreshJournal();
		}

		private void Grid_ObjectChanged(PresentationObject presentationObject)
		{
			RefreshJournal();
		}
	}
}

