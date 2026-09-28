using System.Data;
using FogSoft.Web.Components;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Classes;
using Merlin.Classes.GridExport;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Медиаплан («График размещения») — веб-обработчики тех же пунктов, что в десктопе
/// (docs/mediaplan.md, §1.1): у акции — лист на каждую кампанию, у кампании любого вида,
/// у строки акта выполненных работ. Шаги как в десктопе: разбивка (месяцы / период) и
/// настройки печати — одним окном, затем выбор роликов для «частичного», затем файл
/// через «Сохранить как» браузера. Строит файл ядро — <see cref="MediaPlanJob"/>.
/// Сводный план по нескольким акциям — отдельный экран (MultiActionMediaPlan.razor).
/// </summary>
public sealed partial class ObjectActions
{
	static ObjectActions()
	{
		// Имена действий у кампаний internal в ядре — берём из MediaPlanJob.
		foreach (var (name, mode) in MediaPlanJob.ActionActions)
			ClassActions["Action"][name] = (s, t) => s.PrintMediaPlan(
				() => MediaPlanJob.ForAction((Merlin.Classes.Action)t, mode.Selectively), mode.Breakdown);
		foreach (var (name, mode) in MediaPlanJob.CampaignActions)
			ClassActions["Campaign"][name] = (s, t) => s.PrintMediaPlan(
				() => MediaPlanJob.ForCampaign((PresentationObject)t, mode.Selectively), mode.Breakdown);
		ClassActions["ActJournalRow"] = new()
		{
			[MediaPlanJob.ActJournalRowAction] = (s, t) => s.PrintMediaPlan(
				() => MediaPlanJob.ForActJournalRow((PresentationObject)t), MediaPlanJob.Breakdown.Whole),
		};
	}

	private async Task<ActionEffect> PrintMediaPlan(Func<MediaPlanJob> createJob, MediaPlanJob.Breakdown breakdown)
	{
		string caption = Tr.T("График размещения");
		if (await _saver.UnsupportedReasonAsync() is { } unsupported)
		{
			await ShowInfo(caption, unsupported);
			return ActionEffect.None;
		}

		MediaPlanJob? job = null;
		IList<DateTime>? months = null;
		await _busy.RunAsync(() =>
		{
			job = createJob();
			if (breakdown == MediaPlanJob.Breakdown.Months)
				months = job.AvailableMonths();
		});

		var model = new MediaPlanPrintForm.Model
		{
			Months = months?.Select(m => new MediaPlanPrintForm.MonthItem { Date = m }).ToList(),
			ByPeriod = breakdown == MediaPlanJob.Breakdown.Period,
			Start = job!.PeriodStart.Date,
			Finish = job.PeriodFinish.Date,
		};
		if (!await AskMediaPlanSettings(caption, model))
			return ActionEffect.None;

		IList<DateTime>? pickedMonths = model.Months?.Where(m => m.Checked).Select(m => m.Date).ToList();
		DateTime? from = model.ByPeriod ? model.Start : null;
		DateTime? to = model.ByPeriod ? model.Finish : null;

		string? rollers = null;
		if (job.Selectively)
		{
			rollers = await PickMediaPlanRollers(job, pickedMonths, from, to);
			if (rollers == null)
				return ActionEffect.None;
		}

		ExportFile? file = null;
		await _busy.RunAsync(() => file = job.Build(ToPrintSettings(model), pickedMonths, from, to, rollers));
		if (file == null)
		{
			await ShowInfo(caption, Tr.T("Выпусков нет — печатать нечего."));
			return ActionEffect.None;
		}
		await SaveMediaPlan(file);
		return ActionEffect.None;
	}

	/// <summary>
	/// Окно разбивки и настроек печати; повторяется, пока ввод неверен. Общее с экраном
	/// сводного плана (там без разбивки).
	/// </summary>
	internal async Task<bool> AskMediaPlanSettings(string caption, MediaPlanPrintForm.Model model)
	{
		while (true)
		{
			DialogOutcome outcome = await _dialogs.ShowAsync(caption, builder =>
			{
				builder.OpenComponent<MediaPlanPrintForm>(0);
				builder.AddComponentParameter(1, nameof(MediaPlanPrintForm.Value), model);
				builder.CloseComponent();
			}, okText: Tr.T("Сформировать"));
			if (outcome != DialogOutcome.Ok)
				return false;

			model.Message = model.Months != null && !model.Months.Any(m => m.Checked)
				? Tr.T("Отметьте хотя бы один месяц.")
				: model.ByPeriod && model.Start > model.Finish
					? Tr.T("Дата начала периода позже даты окончания.")
					: null;
			if (model.Message == null)
				return true;
		}
	}

	internal static PrintSettings ToPrintSettings(MediaPlanPrintForm.Model model) => new()
	{
		PrintWithSignatures = model.PrintWithSignatures,
		ShowAdvertisingInfo = model.ShowAdvertisingInfo,
		HideTariffPrice = model.HideTariffPrice,
	};

	// Отрицательный — строки в iEntity нет; сущность не кэшируется и не ищется по id.
	private const int MediaPlanRollersEntityId = -5101;

	/// <summary>Выбор роликов для «частичного» графика (SelectionForm в десктопе): «id,id,» или null — отказ.</summary>
	private async Task<string?> PickMediaPlanRollers(MediaPlanJob job, IList<DateTime>? months, DateTime? from, DateTime? to)
	{
		DataTable? rollers = null;
		await _busy.RunAsync(() => rollers = job.Rollers(months, from, to));
		if (rollers!.Rows.Count == 0)
		{
			await ShowInfo(Tr.T("График размещения"), Tr.T("Выпусков нет — выбирать не из чего."));
			return null;
		}

		// В строках только rollerID и имя: сущность «Ролик» показала бы десяток пустых
		// колонок, поэтому — виртуальная с одной колонкой (как у TableDialog).
		Entity entity = EntityManager.CreateVirtualEntity(MediaPlanRollersEntityId, Tr.T("Ролики"), "MediaPlanRollers",
			"rollerID", new Entity.Attribute("name", "Ролик", "nvarchar")); // i18n-ok: заголовок колонки переводит ObjectList при выводе (Tr.T(a.Alias))
		IReadOnlyList<DataRow>? picked = await PickAsync(Tr.T("Выберите ролики"),
			entity, rollers.DefaultView.ToTable(), Tr.T("Выбрать"),
			multiselect: true, validate: rows => rows.Count == 0 ? Tr.T("Отметьте хотя бы один ролик.") : null);
		return picked == null ? null : string.Join(",", picked.Select(r => r["rollerID"])) + ",";
	}

	/// <summary>Готовый файл — «Сохранить как» браузера.</summary>
	internal Task SaveMediaPlan(ExportFile file) =>
		_saver.SaveAsAsync(file, async () =>
			await _dialogs.ShowAsync(Tr.T("Файл готов"), b => b.AddContent(0, Tr.T("Выберите, куда сохранить файл.")),
				okText: Tr.T("Сохранить…")) == DialogOutcome.Ok);
}
