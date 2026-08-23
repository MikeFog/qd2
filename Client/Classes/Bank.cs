using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	public partial class Bank
	{
		// UpdateBankList переехал в Bank.WinForms.cs.

		public static PresentationObject Find(string bik)
		{
			Entity bankEntity = EntityManager.GetEntity((int)Entities.Bank);
            Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(bankEntity);
            procParameters[Organization.ParamNames.BankBIK] = bik;
            DataSet ds = (DataSet)DataAccessor.DoAction(procParameters);

			if (ds.Tables[Constants.TableNames.Data].Rows.Count == 0)
				return null;
			return bankEntity.CreateObject(ds.Tables[Constants.TableNames.Data].Rows[0]);
        }
	}
}
