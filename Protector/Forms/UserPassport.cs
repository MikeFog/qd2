using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using Protector.Domain;

namespace Protector.Forms
{
	public partial class UserPassport : FogSoft.WinForm.Passport.Forms.PassportForm
	{
		public UserPassport(PresentationObject presentationObject, DataSet ds) 
			: base(presentationObject, ds)
		{
			InitializeComponent();
		}

		protected override void ApplyChanges(Button clickedButton)
		{
			PresentationObject[] wAdded = new PresentationObject[MassmediasWork.Added2Checked.Count];
			MassmediasWork.Added2Checked.CopyTo(wAdded);

			PresentationObject[] wRemoved = new PresentationObject[MassmediasWork.RemovedFromChecked.Count];
			MassmediasWork.RemovedFromChecked.CopyTo(wRemoved);

			PresentationObject[] aAdded = new PresentationObject[MassmediasAdd.Added2Checked.Count];
			MassmediasAdd.Added2Checked.CopyTo(aAdded);

			PresentationObject[] aRemoved = new PresentationObject[MassmediasAdd.RemovedFromChecked.Count];
			MassmediasAdd.RemovedFromChecked.CopyTo(aRemoved);

			base.ApplyChanges(clickedButton);
			MassmediasWorkUpdate(((User)pageContext.PresentationObject).UserID, wAdded, wRemoved);
			MassmediasAddUpdate(((User)pageContext.PresentationObject).UserID, aAdded, aRemoved);
		}

		private static void MassmediasWorkUpdate(int UserID, IEnumerable<PresentationObject> wAdded, IEnumerable<PresentationObject> wRemoved)
		{
			// Submit children changes to database 
			foreach (PresentationObject po in wAdded)
			{
				UserMassmedia member =
					new UserMassmedia(int.Parse((po).IDs[0].ToString()), UserID);
				member.Update();
			}
			foreach (PresentationObject po in wRemoved)
			{
				UserMassmedia member = new UserMassmedia(int.Parse((po).IDs[0].ToString()), UserID);
				member.Delete(true);
			}
		}

		private static void MassmediasAddUpdate(int UserID, IEnumerable<PresentationObject> wAdded, IEnumerable<PresentationObject> wRemoved)
		{
			// Submit children changes to database 
			foreach (PresentationObject po in wAdded)
			{
				UserMassmediaAdd member =
					new UserMassmediaAdd(int.Parse((po).IDs[0].ToString()), UserID);
				member.Update();
			}
			foreach (PresentationObject po in wRemoved)
			{
				UserMassmediaAdd member = new UserMassmediaAdd(int.Parse((po).IDs[0].ToString()), UserID);
				member.Delete(true);
			}
		}

		public SmartGrid MassmediasWork
		{
			get
			{
				return FindControl("massmedias") as SmartGrid;
			}
		}

		public SmartGrid MassmediasAdd
		{
			get
			{
				return FindControl("massmediasAdd") as SmartGrid;
			}
		}
	}
}