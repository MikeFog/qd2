using System;
using System.Data;
using FogSoft.WinForm.Classes;
using System.Collections.Generic;
using FogSoft.WinForm.DataAccess;
using System.Drawing;
using System.IO;

namespace Merlin.Classes
{
    public interface IOrganization
	{
		PresentationObject Bank { get; }
		string Registration { get; }
		string Prefix { get; }
		string PrefixWithName { get; }
		string ReportString { get; }
		string Address { get; }
		string INN { get; }
        string KPP { get; }
        string Account { get; }
		string Phone { get; }
		string Fax { get; }
		string Director { get; }
		string BookKeeper { get; }
		string EGRN { get; }
	}

	public abstract class Organization : ObjectContainer, IOrganization
	{
        public struct ParamNames
		{
			public const string Prefix = "prefix";
			public const string Name = "name";
			public const string BankId = "bankID";
			public const string Address = "address";
			public const string INN = "inn";
            public const string KPP = "kpp";
            public const string Phone = "phone";
			public const string Fax = "fax";
			public const string Account = "account";
			public const string Registration = "registration";
			public const string Director = "director";
			public const string BookKeeper = "bookkeeper";
			public const string FullPrefix = "fullprefix";
			public const string OKONH = "okonh";
			public const string OKPO = "okpo";
			public const string EGRN = "EGRN";
			public const string ReportString = "ReportString";
            public const string Painting = "painting";
            public const string BankAccount = "corAccount";
            public const string BankBIK = "bik";
            public const string FirmId = "firmID";
            public const string Email = "email";
            public const string IsActive = "isActive";
        }
		private Bitmap _bitmap;

		private PresentationObject bank;

		public Organization(Entity entity) : base(entity)
		{
		}

		public Organization(Entity entity, DataRow row)
			: base(entity, row)
		{
		}

        public Organization(Entity entity, Dictionary<string, object> parameters)
            : base(entity, parameters)
		{
		}

		public byte[] SignatureBytes
		{
			get
			{
				return this[ParamNames.Painting] == DBNull.Value ? null : (byte[])this[ParamNames.Painting];
            }
		}

		public Bitmap Signature
		{
			get
			{
				if (SignatureBytes == null) return null;
				if (_bitmap == null)
				{
					using (MemoryStream stream = new MemoryStream(SignatureBytes))
					{
						_bitmap = new Bitmap(Image.FromStream(stream));
					}
				}
                return _bitmap;
            }
		}

		#region IOrganization Properties

		public PresentationObject Bank
		{
			get
			{
				if (bank == null && !String.IsNullOrEmpty(Convert.ToString(this[ParamNames.BankId])))
					bank = Utils.CreateBankById(int.Parse(Convert.ToString(this[ParamNames.BankId])));
				return bank;
			}
		}

		public string Registration
		{
			get { return this[ParamNames.Registration].ToString(); }
		}

        public string FullPrefix
        {
            get { return this[ParamNames.FullPrefix].ToString(); }
        }

        public string Prefix
		{
			get { return this[ParamNames.Prefix].ToString(); }
		}

		public string PrefixWithName
		{
			get { return string.Format("{0} {1}", Prefix, Name).Trim(); }
		}

		public string ReportString
		{
			get { return this[ParamNames.ReportString].ToString(); }
		}

		public string Address
		{
			get { return this[ParamNames.Address].ToString(); }
		}

		public string INN
		{
			get { return this[ParamNames.INN].ToString(); }
		}

        public string KPP
        {
            get { return this[ParamNames.KPP].ToString(); }
        }

        public string Account
		{
			get { return this[ParamNames.Account].ToString(); }
		}

		public string Phone
		{
			get { return this[ParamNames.Phone].ToString(); }
		}

		public string Fax
		{
			get { return this[ParamNames.Fax].ToString(); }
		}

        public string Email
        {
            get { return this[ParamNames.Email].ToString(); }
        }

        public string Director
		{
			get { return this[ParamNames.Director].ToString(); }
		}

		public string BookKeeper
		{
			get { return this[ParamNames.BookKeeper].ToString(); }
		}

		public string EGRN
		{
			get { return this[ParamNames.EGRN].ToString(); }
		}


		#endregion
	}
}