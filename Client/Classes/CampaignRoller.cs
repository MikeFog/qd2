using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using static Merlin.Classes.Campaign;

namespace Merlin.Classes
{
    // UI-часть (DoAction, SubstituteRoller, DeleteIssues, диалог Substitute) —
	// в CampaignRoller.WinForms.cs. Здесь остаётся точка записи в БД
	// ApplyRollerSubstitutionForDays: она возвращает таблицу незаменённых
	// роликов, а показывает её вызывающий UI-код.
	// Конвенция — docs/tasks/web-migration-dialogs.md, §8 п.4.
	internal partial class CampaignRoller : CampaignPart
	{
		private Roller roller;

		public CampaignRoller() : base(EntityManager.GetEntity((int) Entities.CampaignRoller))
		{
		}

		protected CampaignRoller(Entity entity) : base(entity)
		{
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (actionName == Constants.Actions.PlayRoller)
				return base.IsActionEnabled(actionName, type) && !IsMute;
			return base.IsActionEnabled(actionName, type);
		}

		// DoAction, SubstituteRoller и DeleteIssues переехали в CampaignRoller.WinForms.cs.

        public Roller Roller
		{
			get
			{
				if (roller == null)
					roller = new Roller(int.Parse(this[Roller.ParamNames.RollerId].ToString()));

                return roller;
            }
		}

        public int? ModuleID
		{
			get {return (this["moduleID"] == null || this["moduleID"] == DBNull.Value) ? null : (int?)ParseHelper.ParseToInt32(this["moduleID"].ToString()); }
		}
        
		public bool IsMute
		{
			get { return ParseHelper.GetBooleanFromObject(this["isMute"], false); }
		}

		// Substitute (диалог выбора ролика и дней) переехал в CampaignRoller.WinForms.cs.

		/// <summary>
		/// Заменяет <paramref name="oldRoller"/> на <paramref name="newRoller"/> в
		/// кампании <paramref name="campaign"/> по набору дней <paramref name="days"/>,
		/// опционально в рамках модуля или пакетного модуля.
		/// Возвращает таблицу незаменённых роликов (null, если процедура ничего не
		/// вернула) — показать её пользователю решает вызывающий UI-код.
		///
		/// Имя отличается от <see cref="CampaignPart.ApplyRollerSubstitution"/> не
		/// случайно: тот заменяет ролик в одном выпуске, этот — по набору дней.
		/// </summary>
		public static DataTable ApplyRollerSubstitutionForDays(Campaign campaign, Roller oldRoller, Roller newRoller,
								  DataTable days, object moduleID, object packModuleID)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				EntityManager.GetEntity((int)Entities.CampaignRoller),
				InterfaceObjects.FakeModule, Constants.Actions.Substitute);
			procParameters["oldRollerId"] = oldRoller.RollerId;
			procParameters["oldDuration"] = oldRoller.Duration;
			procParameters["newRollerId"] = newRoller.RollerId;
			procParameters["newDuration"] = newRoller.Duration;
			if (moduleID != null)
				procParameters["moduleID"] = moduleID;
			if (packModuleID != null)
				procParameters["packModuleID"] = packModuleID;
			procParameters[Campaign.ParamNames.CampaignId] = campaign.CampaignId;
			procParameters[Campaign.ParamNames.CampaignTypeId] = (int)campaign.CampaignType;

			DataSet ds = DataAccessor.LoadDataSet("RollerSubstitute", procParameters, days);

			return (ds != null && ds.Tables.Count > 0) ? ds.Tables[0] : null;
		}
	}

	internal class CampaignRollerInsideDay : CampaignRoller
	{
		public CampaignRollerInsideDay()
			:base(EntityManager.GetEntity((int)Entities.CampaignRollerInsideDay))
		{
		}
	}

	/// <summary>
	/// Замена ролика по набору дней: данные паспорта RollerSubstitute, проверки по
	/// «ОК» и запись. Всё, что между ними, — показ формы — делает UI:
	/// RollerSubstitutionForm в десктопе, NamedPassportDialog в вебе.
	///
	/// Публичен ради веба (отдельная сборка): CampaignRoller и Campaign internal, и
	/// открывать их целиком незачем — вебу нужна только эта операция.
	/// </summary>
	public sealed class RollerSubstitution
	{
		/// <summary>Имя паспорта в iPassport.</summary>
		public const string PassportName = "RollerSubstitute";

		/// <summary>Имена полей паспорта RollerSubstitute.</summary>
		public struct ParamNames
		{
			public const string RollerId = "rollerID";
			public const string SubstituteMute = "subtituteMute";
			public const string MuteDuration = "rollerMuteDuration";
			public const string AdvertTypeId = "advertTypeID";
			public const string Days = "days";
		}

		private const string RollersTable = "rollers";

		private readonly Campaign campaign;
		private readonly Roller roller;
		private readonly int? moduleID;
		private readonly int? packModuleID;

		internal RollerSubstitution(Campaign campaign, Roller roller, int? moduleID, int? packModuleID)
		{
			this.campaign = campaign;
			this.roller = roller;
			this.moduleID = moduleID;
			this.packModuleID = packModuleID;
		}

		/// <summary>
		/// То же, что CampaignRoller.SubstituteRoller до показа формы: акция
		/// перечитывается, модуль — свой, пакетного модуля нет.
		/// </summary>
		/// <param name="campaignRoller">Ролик кампании (сущности 95, 97).</param>
		public static RollerSubstitution ForCampaignRoller(PresentationObject campaignRoller)
		{
			CampaignRoller cr = (CampaignRoller)campaignRoller;
			cr.Campaign.Action.Refresh();
			return new RollerSubstitution(cr.Campaign, cr.Roller, cr.ModuleID, null);
		}

		/// <summary>Наборы строк паспорта: quantity, rollers, days.</summary>
		public DataSet LoadPassportData()
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				EntityManager.GetEntity((int) Entities.CampaignRoller),
				InterfaceObjects.PropertyPage, Constants.Actions.Substitute);

			procParameters[Roller.ParamNames.RollerId] = roller.RollerId;
			procParameters[Campaign.ParamNames.CampaignId] = campaign.CampaignId;
			procParameters[Campaign.ParamNames.CampaignTypeId] = (int)campaign.CampaignType;
			if (moduleID.HasValue)
				procParameters["moduleID"] = moduleID;
			if (packModuleID.HasValue)
				procParameters["packModuleID"] = packModuleID;
			return DataAccessor.DoAction(procParameters) as DataSet;
		}

		/// <summary>Значения подписей паспорта.</summary>
		public Dictionary<string, object> CreatePassportParameters(DataSet ds)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["rollerName"] = roller.Name;
			parameters["duration"] = roller.DurationString;
			parameters["issues"] = ds.Tables["quantity"].Rows[0]["issues"];
			return parameters;
		}

		/// <summary>Есть ли на что менять, кроме молчания.</summary>
		public static bool HasRollers(DataSet ds)
		{
			return ds != null && ds.Tables.Contains(RollersTable) && ds.Tables[RollersTable].Rows.Count > 0;
		}

		/// <summary>
		/// Строки дерева выпусков, чьи id отмечены (TreeView2.AddedIDs). Пустая
		/// таблица — ничего не выбрано, сообщение NoIssueSelected.
		/// </summary>
		public static DataTable SelectDays(DataTable days, ICollection<object> addedIds)
		{
			DataTable selectedDays = days.Clone();
			foreach (DataRow row in days.Rows)
			{
				if (addedIds.Contains(row["id"]))
					selectedDays.Rows.Add(row.ItemArray);
			}
			return selectedDays;
		}

		/// <summary>
		/// Проверка замены на молчание («пустышку»): текст сообщения или null.
		/// </summary>
		public string ValidateMuteRoller(int? advertTypeId, int duration)
		{
			// если это активированная акция, то для "пустышки" обязательно надо указать предмет рекламы
			if (advertTypeId == null && campaign.Action.IsConfirmed)
				return Tr.T(Properties.Resources.SubstitutionImpossibleForDummyRoller);

			if (duration == 0)
				return Tr.T(Properties.Resources.DummyRollerWithZeroDuration);

			return null;
		}

		/// <summary>Ролик-молчание заданной длины для фирмы акции.</summary>
		public Roller CreateMuteRoller(int duration, int? advertTypeId)
		{
			return MuteRoller.GetRoller(duration, campaign.Action.FirmID, advertTypeId);
		}

		/// <summary>
		/// Проверка нового ролика: в подтверждённой акции у него должен быть
		/// предмет рекламы. Текст сообщения или null.
		/// </summary>
		public string ValidateNewRoller(Roller newRoller)
		{
			if (!newRoller.HasAdvertType && campaign.Action.IsConfirmed)
				return MessageAccessor.GetMessage("WrongRollerForSubstitution");
			return null;
		}

		/// <summary>
		/// Нужен ли пересчёт акции: RollerSubstitute переписывает tariffPrice только
		/// при другой длине ролика (@diffDuration), при равной цена та же.
		/// </summary>
		public bool PriceMayChange(Roller newRoller)
		{
			return roller.Duration != newRoller.Duration;
		}

		/// <summary>Запись замены; возвращает таблицу незаменённых роликов или null.</summary>
		public DataTable Apply(Roller newRoller, DataTable days)
		{
			return CampaignRoller.ApplyRollerSubstitutionForDays(campaign, roller, newRoller, days, moduleID, packModuleID);
		}

		/// <summary>
		/// Пересчёт акции и текст сообщения о смене цены — то же, что
		/// CampaignPart.RecalculateAndShowPriceChange(Campaign.Action.TotalPrice), но
		/// сообщение возвращается, а не показывается: в вебе UserInteraction.Notify
		/// не назначен.
		/// </summary>
		public string RecalculateAction()
		{
			decimal price = campaign.Action.TotalPrice;
			campaign.RecalculateAction();
			decimal newPrice = campaign.Action != null ? campaign.Action.TotalPrice : decimal.Zero;

			string messageKey = CampaignPart.GetPriceChangeMessage(price, newPrice, out Dictionary<string, object> msgParameters);
			MessageAccessor.Parameters = msgParameters;
			return MessageAccessor.GetMessage(messageKey);
		}
	}
}
