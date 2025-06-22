using System;
using System.Windows.Forms;
using Merlin.Classes;

namespace Merlin.Forms
{
	public partial class TransferDayForm : Form
	{
		public TransferDayForm()
		{
			InitializeComponent();
		}

		public TransferDayForm(DateTime sourceDate, Pricelist priceList) : this()
		{
			if (priceList != null)
			{
				lblStartDate.Text = priceList.StartDate.ToShortDateString();
				lblFinishDate.Text = priceList.FinishDate.ToShortDateString();
			}
			lblSourceDate.Text = sourceDate.ToShortDateString();
			dtTargetDate.Value = sourceDate;
		}

		public DateTime TargetDate
		{
			get { return dtTargetDate.Value; }
		}
	}
}