using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Xml;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes.Exchange.MediaPlus
{
	internal class XmlFormat
	{
		private const string issueElementName = "rss";
		private const string dataElementName = "rs:data";
		private const string massmediaAttributeName = "sid";
		private const string dateFormat = "yyyy-MM-dd";
		private const string campaignDataElementName = "row";
		private const string campaignVolumeDiscountAttributeName = "d03";
		private const string campaignManagerDiscountWithAAAttributeName = "fda";
		private const string campaignManagerDiscountWithoutAAAttributeName = "d05";
		private const string campaignPackDiscountAttributeName = "d07";
		private const string campaignAgreementAgecnyAttributeName = "fts";
		private const string issueDateAttributeName = "date";
		private const string issueHHourAttributeName = "hhour";
		private const string issueFormatAttributeName = "format";
		private const string issueTariffAttributeName = "tariff";
		private const string issueHourAttributeName = "hour";


		private XmlDocument Document { get; set;}
		private XmlElement CampaignNode { get; set; }
		private readonly string path;

		public XmlFormat(string path)
		{
			this.path = path;
			Document = new XmlDocument();
			Document.Load(path);
			CampaignNode = Document.GetElementsByTagName(dataElementName)[0].ChildNodes[0] as XmlElement;
		}
        
		private XmlNodeList DataNodes
		{
			get { return CampaignNode.SelectNodes(issueElementName); }
		}

		public int MediaPlusMassmediaID
		{
			get { return ParseHelper.GetInt32FromObject(CampaignNode.Attributes[massmediaAttributeName].Value, 0); }
		}

		public List<SimpleIssue> Issues
		{
			get
			{
				List<SimpleIssue> issues = new List<SimpleIssue>(DataNodes.Count);
				foreach (XmlNode node in DataNodes)
				{
					DateTime date = DateTime.ParseExact(ParseHelper.GetStringFromObject(node.Attributes[issueDateAttributeName].Value, null), dateFormat, Thread.CurrentThread.CurrentCulture);
					byte hour = ParseHelper.GetByteFromObject(node.Attributes[issueHourAttributeName].Value, byte.MinValue);
					bool isFirstHalf = ParseHelper.GetInt32FromObject(node.Attributes[issueHHourAttributeName].Value, 1) == 1;
					string spotFormat = ParseHelper.GetStringFromObject(node.Attributes[issueFormatAttributeName].Value, null);
					int duration;
					if (int.TryParse(spotFormat, out duration))
						issues.Add(new SimpleIssue(duration, date, hour, isFirstHalf));
				}
				return issues;
			}
		}

		private bool IsHasAgencyAgreement
		{
			get { return ParseHelper.GetInt32FromObject(GetCampaignAttribute(campaignAgreementAgecnyAttributeName).Value, 0) == 1; }
		}

		public void SetDiscountValues(float volume, float manager, float pack)
		{
			GetCampaignAttribute(campaignVolumeDiscountAttributeName).Value = volume.ToString("R", Format);
			GetCampaignAttribute(campaignPackDiscountAttributeName).Value = pack.ToString("R", Format);
			GetCampaignAttribute(campaignManagerDiscountWithAAAttributeName).Value = IsHasAgencyAgreement ? manager.ToString("R", Format) : "0";
			GetCampaignAttribute(campaignManagerDiscountWithoutAAAttributeName).Value = IsHasAgencyAgreement ? "0" : manager.ToString("R", Format);
		}

		public XmlAttribute GetCampaignAttribute(string attributeName)
		{
			return
				CampaignNode.SelectSingleNode(string.Format("//{0}[@{1}]", campaignDataElementName,
															attributeName)).Attributes[attributeName];
		}

		public void ClearIssues()
		{
			foreach (XmlNode xmlNode in DataNodes)
				CampaignNode.RemoveChild(xmlNode);
		}

		public void SetIssues(DataTable dt)
		{
			foreach (DataRow row in dt.Rows)
			{
				XmlElement element = Document.CreateElement(issueElementName);
				element.Attributes.Append(Document.CreateAttribute(issueDateAttributeName)).Value = row[issueDateAttributeName].ToString();
				element.Attributes.Append(Document.CreateAttribute(issueHourAttributeName)).Value = row[issueHourAttributeName].ToString();
				element.Attributes.Append(Document.CreateAttribute(issueHHourAttributeName)).Value = row[issueHHourAttributeName].ToString();
				element.Attributes.Append(Document.CreateAttribute(issueFormatAttributeName)).Value = row[issueFormatAttributeName].ToString();
				element.Attributes.Append(Document.CreateAttribute(issueTariffAttributeName)).Value = decimal.ToSingle(ParseHelper.GetDecimalFromObject(row[issueTariffAttributeName], 0)).ToString("F02", Format);
				CampaignNode.AppendChild(element);
			}
		}

		public IFormatProvider Format
		{
			get
			{
				if (format == null)
				{
					NumberFormatInfo info = new NumberFormatInfo {NumberDecimalSeparator = ",", NumberGroupSeparator = ""};
					format = info;
				}
				return format;
			}
		}

		private IFormatProvider format = null;

		public void Save()
		{
			using (XmlTextWriter writer = new XmlTextWriter(path, Encoding.Unicode))
			{
				writer.Formatting = Formatting.Indented;
				writer.IndentChar = '\t';
				writer.Indentation = 1;
				Document.Save(writer);
			}
		}
	}
}
