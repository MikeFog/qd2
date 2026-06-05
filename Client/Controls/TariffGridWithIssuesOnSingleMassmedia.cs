using System;
using System.Data;
using Merlin.Classes;

namespace Merlin.Controls
{
	internal abstract partial class TariffGridWithIssuesOnSingleMassmedia : TariffGridWithCampaignIssues
	{
        protected const short ColPrice = 0;
        protected const short ColTime = 1;
		protected Massmedia _massmedia;

		protected TariffGridWithIssuesOnSingleMassmedia()
		{
			InitializeComponent();
		}

		protected override void InitializeGridColumns()
		{
			gridColumns = new []
				{
					new GridColumn("Цена", ColumnNames.Price, "c", Type.GetType("System.Decimal")),
					new GridColumn("Время", ColumnNames.TimeString),
					new GridColumn("Пн.", ColumnNames.Monday),
					new GridColumn("Вт.", ColumnNames.Tuesday),
					new GridColumn("Ср.", ColumnNames.Wednesday),
					new GridColumn("Чт.", ColumnNames.Thursday),
					new GridColumn("Пт.", ColumnNames.Friday),
					new GridColumn("Сб.", ColumnNames.Saturday),
					new GridColumn("Вс.", ColumnNames.Sunday),
					new GridColumn(ColumnNames.Time, ColumnNames.Time, true)
				};
		}

		public override Campaign Campaign
		{
			get { return campaign; }
			set
			{
				campaign = value;

				if (value == null) return;
				_massmedia = ((CampaignOnSingleMassmedia)value).Massmedia;
				if (campaign.StartDate != DateTime.MinValue)
					_currentDate = campaign.StartDate.Date;
			}
		}

		protected CampaignOnSingleMassmedia CampaignOnSingleMassmedia
		{
			get { return (CampaignOnSingleMassmedia) campaign; }
		}

		protected MassmediaPricelist PricelistOnMassmedia
		{
			get { return (MassmediaPricelist)pricelist; }
		}

		protected static void SetFixedColumnsValues(DataRow row, DataRow tariffRow)
		{
			row[ColumnNames.TimeString] = tariffRow[ColumnNames.TimeString];
			row[ColumnNames.Time] = tariffRow[ColumnNames.Time];
			row[ColumnNames.Price] = tariffRow[ColumnNames.Price].ToString();
		}
	}
}