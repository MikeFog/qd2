using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using FogSoft.WinForm.Passport.Classes;
using FogSoft.WinForm.Passport.Forms;
using FogSoft.WinForm.Properties;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace FogSoft.WinForm
{
    public static class Globals
	{
		private const int LicenseCheckInterval = 60000;

		private static Form mdiParent;
		private static IconLoaderDelegate iconLoader;
		private static readonly ReportExporter crReportExporter;
		private static readonly Timer _tmrLicenseCheck;
        
		public delegate void VoidCallback();

		public static event EmptyDelegate OnCheckLicense;

		static Globals()
		{
			crReportExporter = new ReportExporter();
			/*
			_tmrLicenseCheck = new Timer {Interval = LicenseCheckInterval};
			_tmrLicenseCheck.Tick += LicenseCheck;
			_tmrLicenseCheck.Start();
			*/
		}

		private static void LicenseCheck(object sender, EventArgs e)
		{
			if (OnCheckLicense != null)
				OnCheckLicense();

			_tmrLicenseCheck.Start();
		}

		public static Form MdiParent
		{
			get { return mdiParent; }
			set { mdiParent = value; }
		}

		// Return constraint name from error description

		/// <summary>
		/// Creates a row and populate it with data from Presentation Object
		/// </summary>
		/// <param name="table"></param>
		/// <param name="presentationObject"></param>
		internal static void AddObject2DataTable(DataTable table, PresentationObject presentationObject)
		{
			// Check that object exist
			if (presentationObject.Entity != null && presentationObject.Entity.PKColumns != null 
				&&  presentationObject.Entity.PKColumns.Length > 0)
			{
				DataRow[] rows = table.Select(presentationObject.PKWhereClause);
				if (rows != null && rows.Length > 0)
					return;
			}

			DataRow row = table.NewRow();

			foreach(DataColumn column in table.Columns)
			{
				if(presentationObject.Parameters.ContainsKey(column.Caption) &&
				   presentationObject[column.Caption] != null)
					row[column] = presentationObject[column.Caption];
				else
					row[column] = DBNull.Value;
			}
			table.Rows.InsertAt(row, 0);
		}

		public static ReportExporter ReportExporter
		{
			get { return crReportExporter; }
		}

		public static DialogResult ShowQuestion(string msgName, Dictionary<string, object> parameters)
		{
			MessageAccessor.Parameters = parameters;
			return MessageBox.ShowQuestion(MessageAccessor.GetMessage(msgName));
		}

		public static void ShowInfo(string msgName)
		{
			ShowInfo(msgName, null);
		}

		public static void ShowInfo(string msgName, Dictionary<string, object> parameters)
		{
			if(parameters != null) MessageAccessor.Parameters = parameters;
			Control.CheckForIllegalCrossThreadCalls = false;
			MessageBox.ShowInformation(MessageAccessor.GetMessage(msgName));
		}

		public static void ShowCompleted(string msgName)
		{
			ShowCompleted(msgName, null);
		}

		public static void ShowCompleted(string msgName, Dictionary<string, object> parameters)
		{
			if (parameters != null) MessageAccessor.Parameters = parameters;
			Control.CheckForIllegalCrossThreadCalls = false;
			MessageBox.ShowCompleted(MessageAccessor.GetMessage(msgName));
		}

		public static void ShowExclamation(string msgName)
		{
			ShowExclamation(msgName, null);
		}

		public static void ShowExclamation(string msgName, Dictionary<string, object> parameters)
		{
			if (parameters != null) MessageAccessor.Parameters = parameters;
			Control.CheckForIllegalCrossThreadCalls = false;
			string msg = MessageAccessor.GetMessage(msgName);

            MessageBox.ShowExclamation(msg ?? Resources.ApplicationError);
		}

		public static string GetMessage(string msgName, Dictionary<string, object> parameters)
		{
			MessageAccessor.Parameters = parameters;
			return MessageAccessor.GetMessage(msgName);
		}

		public static void ShowSimpleJournal(Entity entity, string caption)
		{
			JournalForm journal = new JournalForm(entity, caption) {MdiParent = MdiParent, Icon = MdiParent.Icon};
			journal.Show();
		}

		public static void ShowSimpleJournal(Entity entity, string caption, DataTable dt)
		{
			JournalForm journal = new JournalForm(entity, caption, dt) {MdiParent = MdiParent, Icon = MdiParent.Icon};
			journal.Show();
			journal.BringToFront();
		}

        public static void ShowSimpleJournal(Entity entity, string caption, Dictionary<string, object> filterValues, bool showModal = false)
		{
			JournalForm journal = new JournalForm(entity, caption, filterValues);
			if (showModal && MdiParent != null)
			{
				journal.StartPosition = FormStartPosition.CenterParent;
				journal.ShowDialog(MdiParent);
			}
			else
			{
				journal.MdiParent = MdiParent;
				journal.Icon = MdiParent.Icon;
                journal.Show();
			}
		}

		public static void ShowSimpleJournal(
			Entity entity, XPathNavigator xmlFilter, string caption,
			Dictionary<string, object> filterValues)
		{
			JournalForm journal = new JournalForm(entity, caption, filterValues)
			                      	{
			                      		MdiParent = MdiParent,
			                      		XmlFilter = xmlFilter,
			                      		Icon = MdiParent.Icon
			                      	};
			journal.Show();
		}

		public static void ShowSimpleJournal(
			Entity entity, JournalForm.FilterClick onFilterClick, string caption)
		{
			if (onFilterClick == null)
				throw new ArgumentNullException("onFilterClick");

			JournalForm journal = new JournalForm(entity, caption)
			{
				MdiParent = MdiParent,
				Icon = MdiParent.Icon
			};
			journal.OnFilterClick += onFilterClick;
			journal.Show();
		}

		public static void ShowBrowser(FakeContainer container, string caption, Form parent)
		{
			ExplorerForm browser = new ExplorerForm(container, caption) {MdiParent = parent, Icon = MdiParent.Icon};
			browser.Show();
		}

		/// <summary>
		/// Creates a comma-separated string with values of PK columns
		/// </summary>
		public static string CreateIDs(object[] IDs)
		{
			StringBuilder res = new StringBuilder();

			for(int i = 0; i < IDs.Length; i++)
			{
				if(i == 0) res.Append(IDs[i]);
				else res.AppendFormat(",{0}", IDs[i]);
			}
			return res.ToString();
		}

		#region Filter

		public static bool ShowFilter(
			IWin32Window owner, Entity entity, Dictionary<string, object> filter)
		{
			return ShowFilter(new FilterForm(entity, PrepareForFilter(entity), filter), owner);
		}
				
		public static bool ShowFilter(
			IWin32Window owner, Entity entity, XPathNavigator xmlFilter, Dictionary<string, object> filter)
		{
			return ShowFilter(new FilterForm(entity, xmlFilter, PrepareForFilter(entity), filter), owner);
		}

		public static DataSet PrepareForFilter(Entity entity)
		{
			// load data to display filter
			Dictionary<string, object> parameters =
				DataAccessor.PrepareParameters(entity, InterfaceObjects.FilterPage, Constants.Actions.Load);

			DataSet ds = null;
			if (DataAccessor.IsProcedureExist(parameters))
				ds = DataAccessor.DoAction(parameters) as DataSet;
			return ds;
		}

		private static bool ShowFilter(FilterForm filterForm, IWin32Window owner)
		{
			return filterForm.ShowDialog(owner) == DialogResult.OK;
		}

		#endregion
		
		public static void ResolveFilterInitialValues(
			Dictionary<string, object> filterValues, string filterXml)
		{
			if(filterXml == null || filterXml.Trim() == string.Empty) return;

			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(filterXml);

			foreach(XmlNode node in xmlDoc.SelectNodes("//*[@value]"))
			{
				string name = node.Attributes[PageControl.Attributes.Name].Value;
				string value = node.Attributes[PageControl.Attributes.Value].Value;
				string argument = StringUtil.SubstringAfter(value, ":");
				if (Regex.IsMatch(value, ".*:.*"))
				{
					value = StringUtil.SubstringBefore(value, ":");
				}
				switch(value)
				{
					case PageControl.InitialValueAbbreviations.LAST_MONTH:
						filterValues[name] = DateTime.Today.AddMonths(-1).ToString();
						break;

					case PageControl.InitialValueAbbreviations.LAST_WEEK:
						filterValues[name] = DateTime.Today.AddDays(-7).ToString();
						break;

					case PageControl.InitialValueAbbreviations.TODAY:
						filterValues[name] = DateTime.Today.ToString();
						break;

					case PageControl.InitialValueAbbreviations.StartOfTheMonth:
						filterValues[name] = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString();
						break;

                    case PageControl.InitialValueAbbreviations.StartOfTheLastMonth:
                        filterValues[name] = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1).ToString();
                        break;

					case PageControl.InitialValueAbbreviations.EndOfTheMonth:
						filterValues[name] =
							new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddDays(-1).ToString();
						break;

					case PageControl.InitialValueAbbreviations.LoggedUser:
						filterValues[name] = SecurityManager.LoggedUser.Id;
						break;

					default:
						filterValues[name] = value;
						break;
				}
			}
		}

		public static IconLoaderDelegate IconLoader
		{
			get { return iconLoader; }
			set { iconLoader = value; }
		}

		public static void EnableSummaryButton(ToolStripButton tbtnSummary, Type columnType)
		{
			tbtnSummary.Enabled =
				(columnType == typeof(short) || columnType == typeof(int) || columnType == typeof(double) ||
				 columnType == typeof(decimal) || columnType == typeof(byte));
		}

		public static void SetParameterValue(
			Dictionary<string, object> parameters, string key, object value)
		{
			if(!parameters.ContainsKey(key))
				parameters.Add(key, value);
			else
				parameters[key] = value;
		}

		public static Image GetIcon(string iconName)
		{
			Stream stream = GetImageStream(string.Format("Icons.{0}", iconName));
			return (stream == null) ? null : Image.FromStream(stream);
		}

		private static Stream GetImageStream(string imageName)
		{
			Assembly assembly = Assembly.GetEntryAssembly();
			string resourceName = string.Format("{0}.{1}", assembly.GetName().Name, imageName);
			return assembly.GetManifestResourceStream(resourceName);
		}

		public static Image GetImage(string imagename)
		{
			Stream stream = GetImageStream(imagename);
			return (stream == null) ? null : Image.FromStream(stream);
		}

		public static Bitmap GetBitmap(string imagename)
		{
			return new Bitmap(GetImageStream(imagename));
		}

		//---------------------------------------------------------------------
		//This is a thread safe methods
		delegate void ShowMessageCallback(string msg, Exception exc);
		public static void ShowMessageError(string msg, Exception exc)
		{
			if (MdiParent.InvokeRequired)
			{
				ShowMessageCallback d = new ShowMessageCallback(ShowMessageError);
				MdiParent.Invoke(d, new object[] { msg, exc });
			}
			else
			{
				MessageBox.ShowError(msg, exc);
			}
		}

		delegate void CopyToClipboardCallback(string text);
		public static void CopyToClipboard(string text)
		{
			if (MdiParent.InvokeRequired)
			{
				CopyToClipboardCallback d = new CopyToClipboardCallback(CopyToClipboard);
				MdiParent.Invoke(d, new object[] { text });
			}
			else
			{
				Clipboard.SetText(text);
			}
		}

		delegate void WaitCursorCallback(Form frm);
		public static void SetWaitCursor(Form frm)
		{
			if (frm.InvokeRequired)
			{
				WaitCursorCallback d = new WaitCursorCallback(SetWaitCursor);
				frm.Invoke(d, new object[] { frm });
            }
			else
			{
				try
				{
					if (!frm.IsDisposed)
						frm.Cursor = Cursors.WaitCursor;
				}
				catch(Exception e)
				{
					ErrorManager.LogError("Cannot set wait cursor", e);
				}
			}
            Application.DoEvents();
        }

		delegate void DefaultCursorCallback(Form frm);
		public static void SetDefaultCursor(Form frm)
		{
			if (frm.InvokeRequired)
			{
				DefaultCursorCallback d = new DefaultCursorCallback(SetDefaultCursor);
				frm.Invoke(d, new object[] { frm });
			}
			else
			{
				try
				{
					if (!frm.IsDisposed)
						frm.Cursor = Cursors.Default;
				}
				catch (Exception e)
				{
					ErrorManager.LogError("Cannot set default cursor", e);
				}
			}
		}
		//---------------------------------------------------------------------

		#region Db Version

		public static int DBVersion
		{
			get
			{
				if (!_dbversion.HasValue)
					return ConfigurationUtil.WorkingDbVesion;
				return _dbversion.Value;
			}
			set
			{
				_dbversion = value;
			}
		}

		private static int? _dbversion;

		#endregion

		#region Application Name

		private static string _applicationName;

		public static string ApplicationName
		{
			get
			{
				if (string.IsNullOrEmpty(_applicationName))
				{
					_applicationName = "FogSoft Application";
					Assembly assembly = Assembly.GetEntryAssembly();
					object[] objs = assembly.GetCustomAttributes(typeof (AssemblyProductAttribute), true);
					if (objs.Length > 0 && objs[0] is AssemblyProductAttribute)
					{
						AssemblyProductAttribute attribute = objs[0] as AssemblyProductAttribute;
						if (attribute != null && !string.IsNullOrEmpty(attribute.Product))
							_applicationName = attribute.Product;
					}
				}
				return _applicationName;
			}
		}

		#endregion

	}
}