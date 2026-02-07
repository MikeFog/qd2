using DocumentFormat.OpenXml.Packaging;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using FogSoft.WinForm.Win32;
using log4net;
using Merlin.Classes;
using Merlin.Classes.FakeContainers;
using Merlin.Classes.Reports;
using Merlin.Forms.CreateActionMaster;
using Merlin.Forms.FilterForm;
using Merlin.Forms.GridReport;
using Merlin.License;
using Merlin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DateTime = System.DateTime;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;
using Timer = System.Windows.Forms.Timer;

using DocumentFormat.OpenXml.Wordprocessing;

namespace Merlin.Forms
{
    public partial class MdiForm : Form
	{
		private const int WorkingDBVersion = 33;

		private const string RefreshAlias = "Обновить";
		private readonly bool exitFlag;

		private Timer announcementTimer;
		private static bool announcementCheckerLocked;
		private DateTime announcementLastCheck;
		private string announcementWindowCaption;

		private readonly MdiClientController mdiClientController = new MdiClientController();

		public MdiForm()
		{
			InitializeComponent();
			mdiClientController.ParentForm = this;
			mdiClientController.BackColor = System.Drawing.Color.FromArgb(204, 204, 204);
			mdiClientController.Paint += MdiClientControllerPaint;
			Globals.DBVersion = WorkingDBVersion;
			SplashLogginForm fLogin = new SplashLogginForm(Icon)
							{ ImgNameBackground = "Resources.Splash.png",
							  ImgNameBackgroundMain = "Resources.SplashMain.png", 
							ImgNameCancelButtonHover = "Resources.cancelhover.gif",
							ImgNameOkButtonHover = "Resources.okhover.gif",
							ImgNameOkButtonDisabled = "Resources.okdisabled.gif",
							ImgNameOkButton = "Resources.ok.gif",
							ImgNameCancelButton = "Resources.cancel.gif" };
			Globals.MdiParent = fLogin;
			fLogin.OnAfterSuccessLogin += AfterSuccessLogin;
			fLogin.OnAfterLogin += FLoginOnAfterLogin;
			if (fLogin.ShowDialog() != DialogResult.OK)
			{
				Globals.MdiParent = this;
				exitFlag = true;
				return;
			}
			Globals.MdiParent = this;
			SetFormCaption();
			Globals.IconLoader = Globals.GetIcon;
		}

		private static void FLoginOnAfterLogin(LoginEventArgs res)
		{
			if (SecurityManager.LoggedUser != null && SecurityManager.LoggedUser.Id == 0)
			{
				res.Res = LoginCtl.LoginRes.Error;
				SecurityManager.Clear();
				res.ErrorMsg = Resources.LoginErrorWithSuperAdmin;
			}
		}

		private bool AfterSuccessLogin(SuccesLoginEventArgs args)
		{
			try
			{
				args.ReportProgress(Resources.LoginLoadMenu);
				Application.DoEvents();
				MenuManager.CreateApplicationMenu(MDIMenu, MenuItemClick, ConfigurationManager.AppSettings["Language"]);
				Thread.Sleep(50);
				args.ReportProgress(Resources.CheckNewMessage);
				Application.DoEvents();
				InitAnnouncementChecker();
				Thread.Sleep(50);
				args.ReportProgress(Resources.LicenseCheck);
				Application.DoEvents();
				string res = AdvertAgLicence.CheckLicense();
				Thread.Sleep(50);
				if (!string.IsNullOrEmpty(res))
				{
					args.ReportProgress(res);
					return false;
				}

                GlobalContext.Properties["user"] = SecurityManager.LoggedUser.LoginName;
                return true;
			}
			catch(Exception exp)
			{
				ErrorManager.LogError("Cannot Load Merlin", exp);
				return false;
			}
		}

		private void SetFormCaption()
		{
            StringBuilder titleBuilder = new StringBuilder();
            titleBuilder.AppendFormat("{0} ", Application.ProductName);
            if (!string.IsNullOrEmpty(ConfigurationUtil.Title))
            {
                titleBuilder.AppendFormat("- {0} - ", ConfigurationUtil.Title);
            }
            titleBuilder.AppendFormat("[{0} {1}]", SecurityManager.LoggedUser.LastName, SecurityManager.LoggedUser.FirstName);
			Text = titleBuilder.ToString();
		}

		#region Main menu logic

		private void MenuItemClick(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
                if (!(sender is ToolStripMenuItem mi)) return;

                string strMiName = mi.Tag.ToString();

				if (strMiName == "miExit")
					ApplicationExit();
				else if (strMiName == "miPrintInquire")
					ShowPrintInquireJournal(mi);
				else if (strMiName == "miStudioTariff")
					ShowStudioTariff(mi);
				else if (strMiName == "miProductionStudio")
					ShowStudioJournal(mi);
				else if (strMiName == "miProductionActionsBuh" || strMiName == "miProductionActionsStudio"
					|| strMiName == "miProductionActions")
					ShowOrderActions(mi);
				else if (strMiName == "miPaymentType")
					ShowPaymentTypes(mi);
				else if (strMiName == "miMassMedia")
					ShowMassMedia(mi);
				else if (strMiName == "miRolStyle")
					ShowRolStyles(mi);
				else if (strMiName == "miDisabledWindows")
					ShowDisabledWindows(mi);
				else if (strMiName == "miSponsorTariff")
					ShowSponsorTariff(mi);
				else if (strMiName == "miTariff")
					ShowTariff(mi);
				else if (strMiName == "miDiscount")
					ShowDiscounts(mi);
				else if (strMiName == "miModules")
					ShowModules(mi);
				else if (strMiName == "miAdvertSubject")
					ShowAdvertSubjects(mi);
				else if (strMiName == "miRoller")
					ShowRollers(mi);
				else if (strMiName == "miCreateUnconfirmedAction")
					CreateMassmediaAction();
				else if (strMiName == "miMasterCreateActions")
					MasterCreateAction();
				else if (strMiName == "miActionJournalTraffic" || strMiName == "miActionJournalBuh"
					|| strMiName == "miActionJournal")
					ShowMassmediaActions(mi, RelationScenarios.ConfirmedAction, "Подтверждённые рекламные акции", 
						Entities.FirmWithConfirmedActions, Entities.Action, Entities.HeadCompanyWithConfirmedActions);
				else if (strMiName == "miActionJournalUnconfirmed")
					ShowMassmediaActions(mi, RelationScenarios.UnconfirmedAction, "Макеты рекламных акций", 
						Entities.FirmWithUnconfirmedActions, Entities.Action, Entities.HeadCompanyWithUnconfirmedActions);
				else if (strMiName == "miActionJournalDeleted")
					ShowMassmediaActions(mi, RelationScenarios.DeletedAction, "Удалённые рекламные акции", 
						Entities.FirmWithDeletedActions, Entities.ActionDeleted, Entities.HeadCompanyWithDeletedActions);
				else if (strMiName == "miBank")
					ShowBanks(mi);
				else if (strMiName == "miBrand")
					ShowBrands(mi);
				else if (strMiName == "miFirm")
					ShowFirms(mi);
				else if (strMiName == "miStudioOrderActPrint")
					ShowStudioOrders(mi);
				else if (strMiName == "miPaymentStudioOrder" || strMiName == "miPaymentStudioOrderFRS")
					ShowPaymentStudioOrders(mi, strMiName == "miPaymentStudioOrderFRS");
				else if (strMiName == "miPaymentCommon" || strMiName == "miPayment" ||
						 strMiName == "miPaymentFRS")
					ShowPaymentCommon(mi, strMiName == "miPaymentFRS");
				else if (strMiName == "miBalanceStudioOrder")
					ShowBalanceStudioOrders(mi);
				else if (strMiName == "miBalance")
					ShowBalance(mi);
				else if (strMiName == "miBalanceStudioOrderFromRSection")
					ShowBalanceStudioOrders(mi);
				else if (strMiName == "miBalanceFromRSection")
					ShowBalance(mi);
				else if (strMiName == "miFirmBalanceStudioOrder")
					ShowFirmBalanceStudioOrders();
				else if (strMiName == "miFirmBalance")
					ShowFirmBalance();
				else if (strMiName == "miPaymentStudioOrderByManager")
					ShowPaymentStudioOrderByManager(mi);
				else if (strMiName == "miPaymentStudioOrderByManagerFRS")
					ShowPaymentStudioOrderByManagerFromRSection(mi);
				else if (strMiName == "miPaymentByManager")
					ShowCommonOrderByManager(mi);
				else if (strMiName == "miPaymentByManagerFromRSection")
					ShowCommonOrderByManagerFromRSection(mi);
				else if (strMiName == "miConfirmationHistory")
					ShowConfirmationHistory(mi);
				else if (strMiName == "miPrintGrid" || strMiName == "miPrintGridFromRSection")
					ShowPrintGridForm();
				else if (strMiName == "miPackModules")
					ShowPackModules(mi);
				else if (strMiName == "miRollerStatistic")
					ShowRollerStatistic(false);
				else if (strMiName == "miRollerStatisticWithFilter")
					ShowRollerStatistic(true);
				else if (strMiName == "miTrafficManagement")
					ShowTrafficManagement();
				else if (strMiName == "miTransferJournal")
					ShowTransferJournal(mi);
				else if (strMiName == "miAgencyTax")
					ShowAgencyTaxJournal(mi);
				else if (strMiName == "miTariffWindow")
					ShowTariffWindowJournal();
				else if (strMiName == "miActPrint")
					ShowActJournal(mi);
				else if (strMiName.StartsWith("miStats."))
					ShowStatsJournal(mi);
				else if (strMiName == "miUpdateBanksList")
					UpdateBanks();
				else if (strMiName == "miAnnouncements")
					ShowAnnouncements();
				else if (strMiName == "miPackageDiscounts")
					ShowPackageDicounts(mi);
				else if (strMiName == "miLog")
					Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.LogDeletedIssue), mi.Text);
				else if (strMiName == "miGroupMassmedia")
					ShowMassmediaGroupJournal(mi);
				else if (strMiName == "miSpecialActions")
					ShowSpecialAction(mi);
				else if (strMiName == "miSpecialStudioOrderActions")
					ShowSpecialStudioOrderAction(mi);
				else if (strMiName == "VolumeOfRealizationByManager")
					ShowGraphVolumeOfRealizationByPerson(mi);
				else if (mi.Tag.ToString() == "miExportGrid")
					ExportGrid();
				else if (strMiName == "miReportPartText")
					ShowReportPartText(mi);
				else if (strMiName == "deleteDummyRollers")
					DeleteDummyRollers();
				else if (strMiName == "deleteDeletedActions")
					DeleteDeletedActions();
				else if (strMiName == "deleteUnconfirmedActions")
					DeleteUnconfirmedActions();
				else if(strMiName == "miFirmImport")
				{
                    Merlin.Classes.Import.FirmImporter importer = new Classes.Import.FirmImporter();
                    importer.Import();
                }
                else if (strMiName == "miHeadOrganizations")
					ShowHeadCompanies(mi);
                else if (strMiName == "miPriceCalculator")
                    ShowPriceCalculator(mi);
            }
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

        private void DeleteDummyRollers()
        {
			try
			{
				if (MessageBox.ShowQuestion("Вы хотите удалить все неиспользуемые ролики-пустышки?") == DialogResult.Yes)
				{
					Cursor.Current = Cursors.WaitCursor;
                    DataAccessor.ExecuteNonQuery("DeleteUnusedDummyRollers", DataAccessor.CreateParametersDictionary());
				}
			}
			finally { Cursor.Current = Cursors.Default; }
        }

        private void DeleteDeletedActions()
        {
            try
            {
                if (MessageBox.ShowQuestion("Вы хотите окончательно удалить все акции из журнала удалённых акций?") == DialogResult.Yes)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    DataAccessor.ExecuteNonQuery("DeleteDeletedActions", DataAccessor.CreateParametersDictionary());
                }
            }
            finally { Cursor.Current = Cursors.Default; }
        }

        private void DeleteUnconfirmedActions()
        {
            try
            {
                if (MessageBox.ShowQuestion("Вы хотите переместить макеты рекламных акций в журнал удалённых акций?") == DialogResult.Yes)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    DataAccessor.ExecuteNonQuery("DeleteUnconfirmedActions", DataAccessor.CreateParametersDictionary(), 300, true);
                }
            }
            finally { Cursor.Current = Cursors.Default; }
        }

        private void ExportGrid()
		{
			ExportGridForm frm = new ExportGridForm {MdiParent = this, Icon = Icon};
			frm.Show();
		}

		private static void ShowSpecialAction(ToolStripMenuItem mi)
		{
			Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.SpecialAction), ManagerFilter.FilterClick, mi.Text);
		}

		private static void ShowSpecialStudioOrderAction(ToolStripMenuItem mi)
		{
			Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.SpecialStudioOrderAction), ManagerFilter.FilterClick, mi.Text);
		}

		private void ShowAbout()
		{
			About frm = new About { ImgNameBackground = "Resources.Splash.png", ImgNameBackgroundMain = "Resources.SplashMain.png" };
			frm.ShowDialog(this);
		}

		private void ShowPackageDicounts(ToolStripItem mi)
		{
			Entity.Action[] menu = new[]
			                       	{
			                       		new Entity.Action(Constants.EntityActions.Refresh, RefreshAlias, Constants.ActionsImages.Refresh),
			                       		new Entity.Action(Constants.EntityActions.AddNew, "Создать новую Скидку")
			                       	};

			FakeContainer container =
				new FakeContainer("Скидки", menu, RelationManager.GetScenario(RelationScenarios.PackageDiscount));
			Globals.ShowBrowser(container, mi.Text, this);
		}

		private void UpdateBanks()
		{
			Bank.UpdateBankList(this);
		}

		private static void ShowMassmediaGroupJournal(ToolStripItem mi)
		{
			Globals.ShowSimpleJournal(EntityManager.GetEntity((int) Entities.MassmediaGroup), mi.Text);
		}

		private void ShowActJournal(ToolStripItem mi)
		{
			JournalForm journal = new ActJournalForm(EntityManager.GetEntity((int) Entities.ActJournalRow), mi.Text)
			                      	{MdiParent = this, Icon = Globals.MdiParent.Icon};
			journal.Show();
		}

		private void ShowTariffWindowJournal()
		{
			TariffWindowGenerationForm form = new TariffWindowGenerationForm { MdiParent = this, Icon = Globals.MdiParent.Icon };
			form.Show();
		}

		private void ShowMasterDetailsJournal(Entity masterEntity, Entity childEntity, string caption,
		                                      Dictionary<string, object> filter)
		{
			MasterDetailForm fMasterDetails = filter == null
			                                  	? new MasterDetailForm(masterEntity, childEntity, caption)
			                                  	: new MasterDetailForm(masterEntity, childEntity, caption, filter);
			fMasterDetails.MdiParent = this;
			fMasterDetails.Icon = Globals.MdiParent.Icon;
			fMasterDetails.Show();
		}

		private void ShowMasterDetailsJournal(Entity masterEntity, Entity childEntity, string caption)
		{
			ShowMasterDetailsJournal(masterEntity, childEntity, caption, null);
		}

		private void ShowPrintInquireJournal(ToolStripItem mi)
		{
			Globals.ShowBrowser(new MassmediasAndCampaignsContainer(), mi.Text, this);
		}

		private static void ShowConfirmationHistory(ToolStripItem mi)
		{
			Globals.ShowSimpleJournal(
				EntityManager.GetEntity((int) Entities.ConfirmationHistory), mi.Text);
		}

		private void ShowStudioTariff(ToolStripItem mi)
		{
			Entity.Action[] menu = new[]
			                       	{
			                       		new Entity.Action(Constants.EntityActions.Refresh, RefreshAlias, Constants.ActionsImages.Refresh),
			                       		new Entity.Action(Constants.EntityActions.AddNew, "Создать новую студию")
			                       	};

			FakeContainer container = new FakeContainer("Студии", menu,
			                                            RelationManager.GetScenario(RelationScenarios.StudioTariff));
			Globals.ShowBrowser(container, mi.Text, this);
		}

		private void ShowStudioJournal(ToolStripItem mi)
		{
			try
			{
				Globals.ShowSimpleJournal(EntityManager.GetEntity((int) Entities.ProductionStudio), mi.Text);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void ShowOrderActions(ToolStripItem mi)
		{
			Globals.ShowBrowser(new StudioOrderActionContainer(), mi.Text, this);
		}

		private static void ShowPaymentTypes(ToolStripItem mi)
		{
			Globals.ShowSimpleJournal(
				EntityManager.GetEntity((int) Entities.PaymentType), mi.Text);
		}

		private void ShowMassmediaActions(ToolStripItem mi, string scenario, string caption, Entities firmEntity, Entities actionEntity, Entities headCompanyEntity)
		{
			Globals.ShowBrowser(new ActionContainer(RelationManager.GetScenario(scenario), caption, firmEntity, actionEntity, headCompanyEntity), mi.Text, this);
		}

		private static void ShowBanks(ToolStripItem mi)
		{
			Globals.ShowSimpleJournal(EntityManager.GetEntity((int) Entities.Bank), mi.Text);
		}

		private static void ShowReportPartText(ToolStripItem mi)
		{
			Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ReportPartText), mi.Text);
		}
		
		private void ShowAnnouncements()
		{
			if (announcementWindowCaption == null)
			{
				ToolStripMenuItem item = MenuManager.FindByTag(MDIMenu, "miAnnouncements");
				announcementWindowCaption = (item != null) ? item.Text : "Announcements";
			}
			announcementLastCheck = DateTime.Now;
			foreach (Form form in MdiChildren)
			{
				if (Entities.Announcement.Equals(form.Tag))
				{
					form.Activate();
					((JournalForm) form).RefreshJournal();
					return;
				}
			}
			AnnouncementJournalForm journal =
				new AnnouncementJournalForm(EntityManager.GetEntity((int)Entities.Announcement), announcementWindowCaption)
					{
						MdiParent = this,
						Tag = Entities.Announcement
					};
			journal.MdiParent = Globals.MdiParent;
			journal.Icon = Icon;
			journal.Show();
		}

		private static void ShowRolStyles(ToolStripItem mi)
		{
			Globals.ShowSimpleJournal(EntityManager.GetEntity((int) Entities.RolStyle), mi.Text);
		}

		private static void ShowBalanceStudioOrders(ToolStripItem mi)
		{
			Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.BalanceStudioOrder), ManagerFilter.FilterClick, mi.Text);
		}

		private static void ShowBalance(ToolStripItem mi)
		{
			Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.BalanceIssues), ManagerFilter.FilterClick, mi.Text);
		}

		private static void ShowStudioOrders(ToolStripItem mi)
		{
			Globals.ShowSimpleJournal(EntityManager.GetEntity((int) Entities.StudioOrderActJournal), mi.Text);
		}

		private static void ShowMassMedia(ToolStripItem mi)
		{
			MassmediasJournal journal = new MassmediasJournal(mi.Text) { MdiParent = Globals.MdiParent, Icon = Globals.MdiParent.Icon };
			journal.Show();
		}

		private void ShowDisabledWindows(ToolStripItem mi)
		{
			Entity.Action[] menu = new[]
			                       	{
			                       		new Entity.Action(Constants.EntityActions.Refresh, RefreshAlias, Constants.ActionsImages.Refresh),
			                       		new Entity.Action(Constants.EntityActions.AddNew, "Создать новую радиостанцию")
			                       	};

			MassmediasContainer container =
				new MassmediasContainer("Радиостанция", menu, RelationManager.GetScenario(RelationScenarios.DisabledWindows));
			Globals.ShowBrowser(container, mi.Text, this);
		}

		private void ShowSponsorTariff(ToolStripItem mi)
		{
			Entity.Action[] menu = new[]
			                       	{
			                       		new Entity.Action(Constants.EntityActions.Refresh, RefreshAlias, Constants.ActionsImages.Refresh),
			                       		new Entity.Action(Constants.EntityActions.AddNew, "Создать новую радиостанцию")
			                       	};

			MassmediasContainer container =
				new MassmediasContainer("Радиостанция", menu, RelationManager.GetScenario(RelationScenarios.SponsorProgramm));
			Globals.ShowBrowser(container, mi.Text, this);
		}

		private void ShowTariff(ToolStripItem mi)
		{
			Entity.Action[] menu = new[]
			                       	{
			                       		new Entity.Action(Constants.EntityActions.Refresh, RefreshAlias, Constants.ActionsImages.Refresh),
			                       		new Entity.Action(Constants.EntityActions.AddNew, "Создать новую радиостанцию")
			                       	};

			MassmediasContainer container =
				new MassmediasContainer("Радиостанция", menu, RelationManager.GetScenario(RelationScenarios.Tariff));
			Globals.ShowBrowser(container, mi.Text, this);
		}

		private void ShowDiscounts(ToolStripItem mi)
		{
			Entity.Action[] menu = new[]
			                       	{
			                       		new Entity.Action(Constants.EntityActions.Refresh, RefreshAlias, Constants.ActionsImages.Refresh),
			                       		new Entity.Action(Constants.EntityActions.AddNew, "Добавить скидку")
			                       	};

			MassmediasContainer container =
				new MassmediasContainer("Скидки", menu, RelationManager.GetScenario(RelationScenarios.Discount));
			Globals.ShowBrowser(container, mi.Text, this);
		}

		private void ShowModules(ToolStripItem mi)
		{
			Entity.Action[] menu = new[]
			                       	{
			                       		new Entity.Action(Constants.EntityActions.Refresh, RefreshAlias, Constants.ActionsImages.Refresh),
			                       		new Entity.Action(Constants.EntityActions.AddNew, "Создать новую радиостанцию")
			                       	};

			MassmediasContainer container =
				new MassmediasContainer("Радиостанция", menu, RelationManager.GetScenario(RelationScenarios.Module));
			Globals.ShowBrowser(container, mi.Text, this);
		}

		private void ShowAdvertSubjects(ToolStripItem mi)
		{
			Entity.Action[] menu = new[]
									   {
										new Entity.Action(Constants.EntityActions.Refresh, RefreshAlias, Constants.ActionsImages.Refresh),
										new Entity.Action(Constants.EntityActions.AddNew, "Создать новый предмет рекламы")
									   };
			FakeContainer container = new AdvertTypeContainer();
			Globals.ShowBrowser(container, mi.Text, this);
		}

		private void ShowRollers(ToolStripItem mi)
		{
			AudioJournalForm journal = new AudioJournalForm(EntityManager.GetEntity((int) Entities.Roller), mi.Text) { MdiParent = this, Icon = Globals.MdiParent.Icon };
			journal.Show();
		}

		private void CreateMassmediaAction()
		{
			try
			{
				Cursor.Current = Cursors.WaitCursor;
                Application.DoEvents();

                Firm firm = Firm.SelectFirm(this);
				if (firm != null)
					new ActionOnMassmedia(firm).ShowPassport(this);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void MasterCreateAction()
		{
			try
			{
				Cursor.Current = Cursors.WaitCursor;
                Application.DoEvents();

                Firm firm = Firm.SelectFirm(this);
				if (firm != null)
				{
					SelectMassmediasStep step1 = new SelectMassmediasStep(firm);
					if (step1.ShowDialog(this) == DialogResult.OK)
					{
                        EditIssuesForm step2 = new EditIssuesForm(firm, step1.Action, step1.MassmediasCount);
						step2.ShowDialog(this);
                        ActionForm step3 = new ActionForm(step1.Action);
					    step3.ShowDialog(this);
					}
				}
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void ShowBrands(ToolStripItem mi)
		{
			ShowMasterDetailsJournal(EntityManager.GetEntity((int) Entities.Brand),
			                         EntityManager.GetEntity((int) Entities.BrandFirm), mi.Text);
		}

		private void ShowFirms(ToolStripItem mi)
		{
            Globals.ShowSimpleJournal (EntityManager.GetEntity((int)Entities.Firm), mi.Text);
        }

		private void ShowPaymentStudioOrders(ToolStripItem mi, bool fAgenciesFilter)
		{
			Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                ["filterAgencies"] = fAgenciesFilter
            };
			ShowMasterDetailsJournal(
				EntityManager.GetEntity((int) Entities.PaymentStudioOrder),
				EntityManager.GetEntity((int) Entities.PaymentStudioOrderAction),
				mi.Text, parameters);
		}

        private void ShowHeadCompanies(ToolStripItem mi)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                ["ShowInactive"] = 1
            };
			ShowMasterDetailsJournal(
				EntityManager.GetEntity((int)Entities.HeadCompany),
				EntityManager.GetEntity((int)Entities.Firm),
				mi.Text, parameters);
        }

		private void ShowPriceCalculator(ToolStripItem mi)
		{
			PriceCalculatorForm form = new PriceCalculatorForm { MdiParent = this, Icon = Globals.MdiParent.Icon };
			form.Show();
        }

        private void ShowPaymentCommon(ToolStripItem mi, bool fAgenciesFilter)
		{
			Dictionary<string, object> parameters = new Dictionary<string, object>();
			parameters["filterAgencies"] = fAgenciesFilter;
			ShowMasterDetailsJournal(
				EntityManager.GetEntity((int) Entities.PaymentCommon),
				EntityManager.GetEntity((int) Entities.PaymentCommonAction),
				mi.Text, parameters);
		}

		private void ShowFirmBalanceStudioOrders()
		{
			Globals.SetDefaultCursor(this);
			FrmFirmStudioOrderBalance fBalance = new FrmFirmStudioOrderBalance { MdiParent = this, Icon = Globals.MdiParent.Icon };
			fBalance.Show();
		}

		private void ShowFirmBalance()
		{
			Globals.SetWaitCursor(this);
			FrmFirmIssuesBalance fBalance = new FrmFirmIssuesBalance { MdiParent = this, Icon = Globals.MdiParent.Icon };
			fBalance.Show();
		}

		private void ShowCommonOrderByManager(ToolStripItem mi)
		{
			FrmManagerSelector selector = new FrmManagerSelector(InterfaceObjects.SelectForCommon);
			if (selector.ShowDialog(this) == DialogResult.OK)
			{
				foreach (PresentationObject user in selector.SelectedUsers)
				{
					Entity entity = EntityManager.GetEntity((int) Entities.PaymentCommonAction);
					Dictionary<string, object> filterValues =
						new Dictionary<string, object>(3, StringComparer.InvariantCultureIgnoreCase);
					filterValues["startOfInterval"] = selector.StartDate.Date;
					filterValues["endOfInterval"] = selector.FinishDate.Date;
					filterValues["managerID"] = user.IDs[0].ToString();
					if (selector.SelectedAgency != null)
						filterValues["agencyID"] = selector.SelectedAgency.IDs[0].ToString();
					Globals.ShowSimpleJournal(entity, string.Format("{0}: {1}", mi.Text, user.Name), filterValues);
				}
			}
		}

		private static void ShowCommonOrderByManagerFromRSection(ToolStripItem mi)
		{
			Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.PaymentCommonAction), ManagerFilter.FilterClick, mi.Text);
		}

		private void ShowPaymentStudioOrderByManager(ToolStripItem mi)
		{
			FrmManagerSelector selector = new FrmManagerSelector(InterfaceObjects.SelectForStudioOrder);
			if (selector.ShowDialog(this) == DialogResult.OK)
			{
				Entity entity = EntityManager.GetEntity((int) Entities.PaymentStudioOrderAction);
				foreach (PresentationObject user in selector.SelectedUsers)
				{
					Dictionary<string, object> filterValues =
						new Dictionary<string, object>(3, StringComparer.InvariantCultureIgnoreCase);
					filterValues["startOfInterval"] = selector.StartDate.Date;
					filterValues["endOfInterval"] = selector.FinishDate.Date;
					filterValues["managerID"] = user.IDs[0].ToString();
					if (selector.SelectedAgency != null)
						filterValues["agencyID"] = selector.SelectedAgency.IDs[0].ToString();
					Globals.ShowSimpleJournal(entity, string.Format("{0}: {1}", mi.Text, user.Name), filterValues);
				}
			}
		}

		private static void ShowPaymentStudioOrderByManagerFromRSection(ToolStripItem mi)
		{
			Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.PaymentStudioOrderAction), ManagerFilter.FilterClick, mi.Text);
		}

		private void ShowPrintGridForm()
		{
			FrmGridReport fGridReport = new FrmGridReport { MdiParent = this, Icon = Globals.MdiParent.Icon };
			fGridReport.Show();
		}

		private void ShowPackModules(ToolStripItem mi)
		{
			Entity.Action[] menu = new[]
			                       	{
			                       		new Entity.Action(Constants.EntityActions.Refresh, RefreshAlias, Constants.ActionsImages.Refresh),
			                       		new Entity.Action(Constants.EntityActions.AddNew, "Создать новый пакетный модуль")
			                       	};

			FakeContainer container =
				new FakeContainer("Пакетные модули", menu, RelationManager.GetScenario(RelationScenarios.PackModules));
			Globals.ShowBrowser(container, mi.Text, this);
		}

		private void ShowRollerStatistic(bool setuser)
		{
			RollerStatisticForm rollerStatistic = new RollerStatisticForm(setuser) { MdiParent = this, Icon = Icon };
			rollerStatistic.Show();
		}

		private static void ShowTransferJournal(ToolStripItem mi)
		{
			Globals.ShowSimpleJournal(EntityManager.GetEntity((int) Entities.TransferLog), mi.Text);
		}

		private void ShowTrafficManagement()
		{
			TrafficManagementForm trafficManagement = new TrafficManagementForm { MdiParent = this, Icon = Icon };
			trafficManagement.Show();
		}

		private void ShowAgencyTaxJournal(ToolStripItem mi)
		{
			ShowMasterDetailsJournal(
				EntityManager.GetEntity((int) Entities.Agency),
				EntityManager.GetEntity((int) Entities.AgencyTax),
				mi.Text);
		}

		private static void ShowStatsJournal(ToolStripItem mi)
		{
			Entities entity = Entities.ErrTmplGen;
			string caption = string.Empty;
			switch (mi.Tag.ToString())
			{
				case "miStats.VolumeOfRealization":
					/*
					GraphForm frm = new GraphForm(new VolumeOfRealizationReportGenerator(), false)
										{
											MdiParent = Globals.MdiParent,
											Text = mi.Text
										};
					frm.OnFilterClick += ManagerFilter.FilterClick;
					frm.Show();
					return;
					*/
					entity = Entities.StatsVolumeofRealization;
					break;

                case "miStats.VolumeOfRealization4Roll":
					entity = Entities.StatsVolumeofRealization4Rollers;
					break;
				case "miStats.Balance":
					ShowStatBalance(mi);
					return;
				case "miStats.BalanceAgency":
					entity = Entities.StatsBalanceAgency;
					break;
				case "miStats.FillPercentage":
					entity = Entities.StatsFillPercentage;
					Globals.ShowSimpleJournal(EntityManager.GetEntity((int)entity), mi.Text);
					return;
				case "miStats.BalanceManager":
					entity = Entities.StatsBalanceManager;
					break;
				case "miStats.BalanceManagerOrder":
					entity = Entities.StatsBalanceManagerOrder;
					break;
				case "miStats.SponsorBusiness":
					entity = Entities.StatsSponsorBusiness;
                    caption = "Фактическое размещение спонсорских программ";
                    break;
				case "miStats.RollersCreated":
					entity = Entities.StatsRollersCreated;
					break;
				case "miStats.VolumeRealizationByMonth":
					entity = Entities.StatVolumeOfRealiztionByMonth;
					break;
                case "miStats.ModuleLoading":
					entity = Entities.StatModuleLoading;
					caption = "Фактическое размещение рекламных модулей";
					break;
				case "miStats.ModuleFinancy":
					entity = Entities.StatModuleFinancy;
					break;
				case "miStats.PackModuleLoading":
					entity = Entities.StatPackModuleLoading;
                    caption = "Фактическое размещение пакетных рекламных модулей";
                    break;
				case "miStats.PackModuleFinancy":
					entity = Entities.StatPackModuleFinancy;
					break;
				case "miStats.AvgDiscount":
					entity = Entities.StatAvgDiscount;
					break;
				case "miStats.VolumeOfRealizationSec":
					entity = Entities.StatVolumeOfRealizationSec;
					break;
				case "miStats.VolumeByPaymentType":
					entity = Entities.StatVolumeByPaymentType;
					break;
                case "miStats.FactorAnalysis":
                    entity = Entities.StatsFactorAnalysis;
                    break;
				default:
					MessageBox.ShowInformation(mi.Tag.ToString(), "Menu Item clicked!");
					break;
			}
			if (entity != Entities.ErrTmplGen)
				Globals.ShowSimpleJournal(EntityManager.GetEntity((int) entity), ManagerFilter.FilterClick, caption == string.Empty ? mi.Text : caption);
		}

		private static void ShowStatBalance(ToolStripItem mi)
		{
			StatBalanceJournalForm journal = new StatBalanceJournalForm(EntityManager.GetEntity((int) Entities.StatsBalance),
			                                                            "Статистика :: " + mi.Text) { MdiParent = Globals.MdiParent, Icon = Globals.MdiParent.Icon };
			journal.Show();
		}

		private static void ShowGraphVolumeOfRealizationByPerson(ToolStripItem mi)
		{
			Entity entity = EntityManager.GetEntity((int)Entities.StatsVolumeofRealization);
			Dictionary<string, object> filterValues = new Dictionary<string, object>();
			Globals.ResolveFilterInitialValues(filterValues, entity.XmlFilter);
			filterValues["managerID"] = SecurityManager.LoggedUser.Id;
			GraphForm form = new GraphForm(new VolumeOfRealizationReportGenerator(filterValues))
			{
				MdiParent = Globals.MdiParent,
				Text = mi.Text
			};
			form.OnFilterClick += ManagerFilter.FilterClick;
			form.Show();
		}

		#endregion

		private void CheckAnnouncements(object sender, EventArgs e)
		{
			/*
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationUtil.ProposalTemplatePath);
            var outFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CommercialOffers");
            Directory.CreateDirectory(outFolder);

            var outPath = Path.Combine(outFolder, $"КП_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

            var result = Cp.CpOneDocGenerator.GenerateOneDoc(
                Class1.BuildTestSnapshotsForCp(),
                templatePath,
                outPath,
                clientName: "Радар-Драйв",
                docDate: DateTime.Today,
                directorName: "Агамов Владислав Александрович",
                contactName: SecurityManager.LoggedUser.FullName,
                contactEmail: SecurityManager.LoggedUser.Email,
                contactPhone: SecurityManager.LoggedUser.Phone
            );

            Process.Start(new ProcessStartInfo(result) { UseShellExecute = true });
			*/
            
              var priceCalculatorForm = new PriceCalculatorForm() { MdiParent = this, Icon = Icon };
              priceCalculatorForm.Show();
            /**************************************************************

                                     Campaign c = new CampaignOnSingleMassmedia(383269);
                                     c.Refresh();
                                     c.DoAction("Edit", this, InterfaceObjects.SimpleJournal);


                          ActionOnMassmedia a = new ActionOnMassmedia(173694);
                          a.Refresh();
                          a.DoAction(Constants.EntityActions.Edit, this, InterfaceObjects.SimpleJournal);

                          return;

                          CampaignPackModule c2 = new CampaignPackModule(356193);
                          c2.Refresh();
                          c2.DoAction(Constants.EntityActions.Edit, this, InterfaceObjects.SimpleJournal);
                          return;

                          Massmedia massmedia = new Massmedia();
                          massmedia["MassmediaId"] = 163;
                          massmedia.IsNew = false;
                          massmedia.Refresh();

                          string fileName = "C:\\Tmp\\1\\";
                          GridReportCreator creater = new GridReportCreator(massmedia, new DateTime(2024, 06, 06), null);
                          creater.ExportDocument(fileName);
                          return;

                          a = new ActionOnMassmedia(161630);
                          a.Refresh();
                          a.DoAction(Classes.Action.ActionNames.PrintBillContract, this, InterfaceObjects.SimpleJournal);
                          return;

                          //ActionOnMassmedia a = new ActionOnMassmedia(161493);
                          a.Refresh();
                          a.DoAction(Merlin.Classes.Action.ActionNames.PrintBill, this, InterfaceObjects.SimpleJournal);
                          return;
                          Campaign c = new CampaignOnSingleMassmedia(355658);

                          c.Refresh();
                          c.ChildEntity = EntityManager.GetEntity((int)Entities.CampaignDay);
                          c.DoAction(Campaign.ActionNames.DeleteIssues, this, InterfaceObjects.SimpleJournal);
                          */

            //ShowTrafficManagement();
            /*
            Campaign c = new CampaignOnSingleMassmedia(355869);
			c.Refresh();
			c.ChildEntity = EntityManager.GetEntity((int)Entities.CampaignRoller);
			DataTable dt = c.GetContent();
            CampaignRoller po = new CampaignRoller();
			po.Init(dt.Rows[0]);
			po.DoAction(Constants.Actions.ChangePositions, this, InterfaceObjects.SimpleJournal);

		
			

			ActionOnMassmedia a = new ActionOnMassmedia(160481);
			a.Refresh();
			a.DoAction("Split", this, InterfaceObjects.SimpleJournal);
			return;

            ShowMassmediaActions(new ToolStripMenuItem("Акции"));
			*/

            if (ConfigurationUtil.GetBooleanSettings("Announcement.Disable", false))
				return;
			if (!announcementCheckerLocked && Globals.MdiParent != null)
			{
				Application.DoEvents();
				announcementCheckerLocked = true;
				Globals.VoidCallback callback = CheckAnnouncements;
				callback.BeginInvoke(null, null);
			}
		}

		private void CheckAnnouncements()
		{
			if (announcementLastCheck == DateTime.MinValue)
				announcementLastCheck = DateTime.Now.AddDays(-1);
			if (Announcement.HasNewEvents(announcementLastCheck))
				Invoke(new Globals.VoidCallback(ShowAnnouncements));
			announcementLastCheck = DateTime.Now;
			announcementCheckerLocked = false;
		}

		private void InitAnnouncementChecker()
		{
			if (ConfigurationUtil.GetBooleanSettings("Announcement.Disable", false))
				return;
			if (announcementTimer == null)
				announcementTimer = new Timer();
			announcementTimer.Interval = ConfigurationUtil.GetInt32Settings("Announcement.RefreshInterval", 300*1000);
			announcementTimer.Tick += CheckAnnouncements;
			announcementTimer.Start();
		}

		private static void ApplicationExit()
		{
			Application.Exit();
		}

		protected override void OnLoad(EventArgs e)
		{
			if (exitFlag)
				ApplicationExit();
			else
			{
				base.OnLoad(e);
				InitAnnouncementChecker();
				Activate();
				Globals.OnCheckLicense += AdvertAgLicence.CheckLicenseByTimer;

				CheckAnnouncements(null, null);
			}
		}
       
		#region Paint MDI Client Area

		private static Image RightImage
		{
			get { return Globals.GetImage("Resources.rightImage.jpg"); }
		}

		private static Image LeftImage
		{
			get { return Globals.GetImage("Resources.leftImage.png"); }
		}

		private static void MdiClientControllerPaint(object sender, PaintEventArgs e)
		{
			Image imgRight = RightImage;
			if (imgRight != null)
				e.Graphics.DrawImage(imgRight, e.ClipRectangle.Right - imgRight.Size.Width, e.ClipRectangle.Bottom - imgRight.Size.Height, imgRight.Size.Width,
							  imgRight.Size.Height);
			Image imgLeft = LeftImage;
			if (imgLeft != null)
				e.Graphics.DrawImage(imgLeft, 30, e.ClipRectangle.Bottom - imgLeft.Size.Height - 30, imgLeft.Size.Width, imgLeft.Size.Height);
		}

		#endregion

		private void MDIForm_KeyDown(object sender, KeyEventArgs e)
		{
			// Перезагрузить объекты из базы
			if (ConfigurationUtil.IsTestMode && e.Alt && e.Control && e.KeyCode == Keys.R)
			{
				EntityManager.ClearHash();
				MessageAccessor.ClearHash();
				PassportLoader.ClearHash();
				RelationManager.ClearHash();
				DataAccessor.ClearHash();

				if (ConfigurationUtil.IsFullLoadDictionaries)
				{
					ProgressForm.Show(this, MdiFormReloadObjects, "Перезагрузка объектов...", null);
				}
			}
		}

		private static void MdiFormReloadObjects(object sender, DoWorkEventArgs e)
		{
			EntityManager.FullLoadDictionaries();
			MessageAccessor.FullLoadDictionaries();
			PassportLoader.FullLoadDictionaries();
			RelationManager.Load();
			DataAccessor.LoadProcedureConfig();
		}
	}
}