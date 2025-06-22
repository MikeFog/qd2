using System;
using System.Data;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
    class TariffWindowWithRange : ITariffWindow
    {
	    private readonly DataRow _row;

		public TariffWindowWithRange(DataRow row)
		{
		    _row = row;
		}

        public DateTime WindowDate { get { return ParseHelper.GetDateTimeFromObject(_row["date"], DateTime.Now); } }

        public bool IsFirstPositionOccupied
        {
            get { return bool.Parse(_row["isFirstPositionOccupied"].ToString()); }
        }

        public bool IsSecondPositionOccupied
        {
            get { return bool.Parse(_row["isSecondPositionOccupied"].ToString()); }
        }

        public bool IsLastPositionOccupied
        {
            get { return bool.Parse(_row["isLastPositionOccupied"].ToString()); }
        }

        public int FirstPositionsUnconfirmed
        {
            get { return int.Parse(_row["firstPositionsUnconfirmed"].ToString()); }
        }

        public int SecondPositionsUnconfirmed
        {
            get { return int.Parse(_row["secondPositionsUnconfirmed"].ToString()); }
        }

        public int LastPositionsUnconfirmed
        {
            get { return int.Parse(_row["lastPositionsUnconfirmed"].ToString()); }
        }

		public decimal Price { get; private set; }

		public int TariffId { get; private set; }

		public DataTable LoadIssues(bool showUnconfirmed, Entity issueEntity)
		{
			return null;
		}

        public bool IsDisabled
        {
            get { return false; }
        }

        public bool IsMarked
        {
            get { return false; }
        }
    }
}
