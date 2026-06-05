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

            HasCurrentActionIssues = ParseHelper.GetBooleanFromObject(row["HasIssuesThisAction"], false);
            HasIssues = ParseHelper.GetBooleanFromObject(row["HasIssues"], false);
            HasIssuesAllMassmedia = ParseHelper.GetBooleanFromObject(row["HasIssuesAllMassmedia"], false);
            HasIssuesUnconfirmed = ParseHelper.GetBooleanFromObject(row["HasIssuesUnconfirmed"], false);
            HasIssuesUnconfirmedAllMassmedia = ParseHelper.GetBooleanFromObject(row["HasIssuesUnconfirmedAllMassmedia"], false);
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

        public bool HasCurrentActionIssues { get; private set; }
        public bool HasIssues { get; private set; }
        public bool HasIssuesAllMassmedia { get; private set; }
        public bool HasIssuesUnconfirmed { get; private set; }
        public bool HasIssuesUnconfirmedAllMassmedia { get; private set; }

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

        public bool IsPrime
        {
            get { return bool.Parse(_row["isPrime"].ToString()); }
        }
    }
}
