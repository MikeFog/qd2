using System;
using System.Data;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public interface ITariffWindow
	{
		DateTime WindowDate { get; }
		decimal Price { get; }
		int TariffId { get; }
		DataTable LoadIssues(bool showUnconfirmed, Entity issueEntity);
		bool IsDisabled { get; }
		bool IsMarked {  get; }
	}
}