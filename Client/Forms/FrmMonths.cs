using System.Collections.Generic;
using System.Windows.Forms;

namespace Merlin.Forms
{
	public partial class FrmMonths : Form
	{
		private readonly List<object> lstObjectKeyValues;

		public FrmMonths()
		{
			InitializeComponent();
			//Globals.SetFontSize(this);
		}

		public FrmMonths(IEnumerable<KeyValuePair<object, object>> lstObjects)
			: this()
		{
			lstObjectKeyValues = new List<object>();
			foreach (KeyValuePair<object, object> li in lstObjects)
			{
				lstObjectKeyValues.Add(li);
				lstMonth.Items.Add(li.Value, true);
			}
		}

		public FrmMonths(IEnumerable<KeyValuePair<object, object>> lstObjects, bool showPrice)
			: this()
		{
			lstObjectKeyValues = new List<object>();
			foreach (KeyValuePair<object, object> li in lstObjects)
			{
				lstObjectKeyValues.Add(li);
				lstMonth.Items.Add(li.Value, true);
			}
			chkOption.Visible = showPrice;
		}

		public Dictionary<object, object> CheckedItems
		{
			get
			{
				Dictionary<object, object> checkedItems = new Dictionary<object, object>();
				foreach (int i in lstMonth.CheckedIndices)
				{
					KeyValuePair<object, object> kvp = (KeyValuePair<object, object>) lstObjectKeyValues[i];
					checkedItems.Add(kvp.Key, kvp.Value);
				}
				return checkedItems;
			}
		}

		public bool IsOptionChecked
		{
			get { return chkOption.Checked; }
		}
	}
}