using System;
using System.Collections.Generic;
using System.Data;

namespace Merlin.Controls
{
	internal class TimePeriodAddResult
	{
		public List<DataRow>   Rows          { get; } = new List<DataRow>();
		public List<Exception> Errors        { get; } = new List<Exception>();
		public int             ExpectedCount { get; set; }
	}
}
