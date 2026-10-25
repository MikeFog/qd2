using System.Data;
using FogSoft.Web.Components;
using FogSoft.WinForm.Classes;
using Merlin.Classes;
using Merlin.Classes.Documents;
using Merlin.Classes.GridExport;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Документы для клиента из Word-шаблонов агентства (docs/tasks/web-reports.md §6.0, §8 этапы 5, 7).
/// По акции (Action.PrintContracts / PrintBills / PrintBillContracts): выбор агентств, если их
/// несколько; номер и дата счёта (FrmBill) — одним окном на все агентства, для счёта по месяцам —
/// ещё месяцы; флажок подписи вместо вопроса при печати; счёт записывается в базу. Договор из
/// карточки фирмы (Firm.PrintContract) — агентства с датой договора. Эфирная справка у кампании
/// и строки журнала актов — месяцы, «Включать цену». Затем все документы в одном окне просмотра
/// закладками; «Скачать Word» — один файл, «Скачать все» — все файлы в выбранную папку. Строит документы ядро — <see cref="ClientDocuments"/>.
/// </summary>
public sealed partial class ObjectActions
{
	// Отрицательный — строки в iEntity нет; сущность не кэшируется и не ищется по id.
	private const int DocumentAgenciesEntityId = -5102;

	/// <summary>Документ по акции; <paramref name="byMonth"/> — счёт по месяцам.</summary>
	private async Task<ActionEffect> PrintActionDocument(Merlin.Classes.Action action, DocumentKind kind, bool byMonth = false)
	{
		string caption = ClientDocuments.KindName(kind);
		if (await _saver.UnsupportedReasonAsync() is { } unsupported)
		{
			await ShowInfo(caption, unsupported);
			return ActionEffect.None;
		}

		IList<Agency>? agencies = await PickDocumentAgencies(caption, action);
		if (agencies == null)
			return ActionEffect.None;

		IList<DateTime>? months = null;
		if (byMonth)
		{
			await _busy.RunAsync(() => months = ClientDocuments.BillMonths(action));
			if (months!.Count == 0)
			{
				await ShowInfo(caption, Tr.T("У акции нет выходов — не за какой месяц выставлять счёт."));
				return ActionEffect.None;
			}
		}

		var model = new DocumentBillForm.Model
		{
			NextNumber = DocumentBills.NextNumber,
			Months = months?.Select(m => new DocumentBillForm.MonthItem { Date = m }).ToList(),
		};
		await _busy.RunAsync(() =>
		{
			foreach (Agency agency in agencies)
			{
				DocumentBill? bill = DocumentBills.Find(action, agency.AgencyId);
				model.Rows.Add(new DocumentBillForm.Row
				{
					AgencyId = agency.AgencyId,
					AgencyName = agency.Name,
					Existing = bill != null,
					Number = bill?.Number ?? DocumentBills.NextNumber(agency.AgencyId, DateTime.Today.Year),
					Date = bill?.Date ?? DateTime.Today,
				});
			}
		});

		while (true)
		{
			DialogOutcome outcome = await _dialogs.ShowAsync(caption, builder =>
			{
				builder.OpenComponent<DocumentBillForm>(0);
				builder.AddComponentParameter(1, nameof(DocumentBillForm.Value), model);
				builder.CloseComponent();
			}, okText: Tr.T("Сформировать"));
			if (outcome != DialogOutcome.Ok)
				return ActionEffect.None;
			model.Message = model.Months != null && !model.Months.Any(m => m.Checked)
				? Tr.T("Отметьте хотя бы один месяц.")
				: null;
			if (model.Message == null)
				break;
		}

		IList<DateTime?> pickedMonths = model.Months == null
			? new List<DateTime?> { null }
			: model.Months.Where(m => m.Checked).Select(m => (DateTime?)m.Date).ToList();
		var files = new List<DocumentFile>();
		string? error = null;
		await _busy.RunAsync(() =>
		{
			foreach (DocumentBillForm.Row row in model.Rows)
			{
				// Как «Ок» в FrmBill: номер и дата счёта записываются до печати.
				DocumentBills.Save(action, row.AgencyId, row.Number, row.Date);
				Agency agency = agencies.First(a => a.AgencyId == row.AgencyId);
				error = BuildDocuments(agency.Name, files, pickedMonths.Select(month => (Func<ExportFile>)(() =>
					ClientDocuments.ActionDocumentFile(action, agency, kind, new DocumentBill(row.Number, row.Date), month,
						model.WithSignature))));
				if (error != null)
					return;
			}
		});
		return await ShowDocumentsOrError(caption, files, error);
	}

	/// <summary>Договор из карточки фирмы — по агентствам с датой договора, без акции и счёта.</summary>
	private async Task<ActionEffect> PrintFirmContract(Firm firm, DocumentKind kind)
	{
		string caption = ClientDocuments.KindName(kind);
		if (await _saver.UnsupportedReasonAsync() is { } unsupported)
		{
			await ShowInfo(caption, unsupported);
			return ActionEffect.None;
		}

		var model = new FirmContractForm.Model();
		await _busy.RunAsync(() =>
		{
			DataTable agencies = Agency.LoadAgencies(true).Tables[FogSoft.WinForm.Constants.TableNames.Data];
			foreach (DataRow row in agencies.Rows.Cast<DataRow>().OrderBy(r => r["name"].ToString()))
				model.Rows.Add(new FirmContractForm.Row
				{
					AgencyId = Convert.ToInt32(row[Agency.ParamNames.AgencyId]),
					AgencyName = row["name"].ToString()!,
				});
		});
		if (model.Rows.Count == 1)
			model.Rows[0].Checked = true;

		while (true)
		{
			DialogOutcome outcome = await _dialogs.ShowAsync(caption, builder =>
			{
				builder.OpenComponent<FirmContractForm>(0);
				builder.AddComponentParameter(1, nameof(FirmContractForm.Value), model);
				builder.CloseComponent();
			}, okText: Tr.T("Сформировать"));
			if (outcome != DialogOutcome.Ok)
				return ActionEffect.None;
			// Как SelectCampaignsForm.PrepareAgencyData.
			model.Message = !model.Rows.Any(r => r.Checked)
				? Tr.T("Отметьте хотя бы одно агентство.")
				: model.Rows.Any(r => r.Checked && r.Date == null)
					? Tr.T("Укажите дату договора у каждого отмеченного агентства.")
					: null;
			if (model.Message == null)
				break;
		}

		var files = new List<DocumentFile>();
		string? error = null;
		await _busy.RunAsync(() =>
		{
			foreach (FirmContractForm.Row row in model.Rows.Where(r => r.Checked))
			{
				Agency agency = ClientDocuments.AgencyById(row.AgencyId);
				error = BuildDocuments(agency.Name, files, new Func<ExportFile>[]
				{
					() => ClientDocuments.FirmContractFile(firm, agency, kind, row.Date!.Value, model.WithSignature)
				});
				if (error != null)
					return;
			}
		});
		return await ShowDocumentsOrError(caption, files, error);
	}

	/// <summary>Эфирная справка: по месяцам кампании и её радиостанциям (у пакета — по каждой).</summary>
	private async Task<ActionEffect> PrintOnAirInquire(PresentationObject target)
	{
		string caption = ClientDocuments.KindName(DocumentKind.OnAirInquire);
		if (await _saver.UnsupportedReasonAsync() is { } unsupported)
		{
			await ShowInfo(caption, unsupported);
			return ActionEffect.None;
		}

		PresentationObject? campaign = null;
		IList<DateTime>? months = null;
		IList<Massmedia>? massmedias = null;
		await _busy.RunAsync(() =>
		{
			campaign = ClientDocuments.OnAirCampaign(target);
			if (campaign == null)
				return;
			months = ClientDocuments.OnAirMonths(campaign);
			massmedias = ClientDocuments.OnAirMassmedias(campaign);
		});
		if (campaign == null || massmedias!.Count == 0)
		{
			await ShowInfo(caption, Tr.T("Эфирная справка печатается по кампании на радиостанции или пакетному модулю."));
			return ActionEffect.None;
		}
		if (months!.Count == 0)
		{
			await ShowInfo(caption, Tr.T("У кампании нет выходов — справку печатать не за что."));
			return ActionEffect.None;
		}

		var model = new OnAirInquireForm.Model
		{
			Months = months.Select(m => new DocumentBillForm.MonthItem { Date = m }).ToList(),
		};
		while (true)
		{
			DialogOutcome outcome = await _dialogs.ShowAsync(caption, builder =>
			{
				builder.OpenComponent<OnAirInquireForm>(0);
				builder.AddComponentParameter(1, nameof(OnAirInquireForm.Value), model);
				builder.CloseComponent();
			}, okText: Tr.T("Сформировать"));
			if (outcome != DialogOutcome.Ok)
				return ActionEffect.None;
			model.Message = model.Months.Any(m => m.Checked) ? null : Tr.T("Отметьте хотя бы один месяц.");
			if (model.Message == null)
				break;
		}

		var files = new List<DocumentFile>();
		string? error = null;
		await _busy.RunAsync(() =>
		{
			string agencyName = ClientDocuments.CampaignAgency(campaign).Name;
			var jobs = from month in model.Months.Where(m => m.Checked)
					   from massmedia in massmedias
					   select (Func<ExportFile>)(() => ClientDocuments.OnAirInquireFile(campaign, massmedia, month.Date,
						   model.WithPrice, model.WithSignature));
			error = BuildDocuments(agencyName, files, jobs);
		});
		return await ShowDocumentsOrError(caption, files, error);
	}

	/// <summary>Строит документы; ошибка шаблона агентства — текст для пользователя, иначе null.</summary>
	private static string? BuildDocuments(string agencyName, List<DocumentFile> files, IEnumerable<Func<ExportFile>> jobs)
	{
		try
		{
			foreach (Func<ExportFile> job in jobs)
				files.Add(new DocumentFile(job(), agencyName));
			return null;
		}
		catch (DocumentTemplateException e)
		{
			return Tr.Format("Шаблон документа агентства «{0}» не подходит: {1}", agencyName, e.Message);
		}
	}

	private async Task<ActionEffect> ShowDocumentsOrError(string caption, IList<DocumentFile> files, string? error)
	{
		if (error != null)
			await ShowInfo(caption, error);
		else
			await ShowDocuments(caption, files);
		return ActionEffect.None;
	}

	/// <summary>
	/// Агентства документа: одно — без вопроса, несколько — выбор с отметками (как «Выбор
	/// агентств» в десктопе). null — печатать не по чему или пользователь отказался.
	/// </summary>
	private async Task<IList<Agency>?> PickDocumentAgencies(string caption, PresentationObject owner)
	{
		IList<Agency>? agencies = null;
		await _busy.RunAsync(() => agencies = ClientDocuments.AgenciesOf(owner));
		if (agencies!.Count == 0)
		{
			await ShowInfo(caption, Tr.T("У акции нет кампаний — не по какому агентству печатать."));
			return null;
		}
		if (agencies.Count == 1)
			return agencies;

		var table = new DataTable();
		table.Columns.Add("agencyID", typeof(int));
		table.Columns.Add("name", typeof(string));
		foreach (Agency agency in agencies)
			table.Rows.Add(agency.AgencyId, agency.Name);
		Entity entity = EntityManager.CreateVirtualEntity(DocumentAgenciesEntityId, Tr.T("Агентства"), "DocumentAgencies",
			"agencyID", new Entity.Attribute("name", "Агентство", "nvarchar")); // i18n-ok: заголовок колонки переводит ObjectList при выводе (Tr.T(a.Alias))
		// Обычно печатают по всем агентствам акции — отмечены все, лишние пользователь снимает.
		IReadOnlyList<DataRow>? picked = await PickAsync(Tr.T("Выбор агентств"), entity, table, Tr.T("Выбрать"),
			multiselect: true, validate: rows => rows.Count == 0 ? Tr.T("Отметьте хотя бы одно агентство.") : null,
			initiallySelected: table.Rows.Cast<DataRow>().ToList());
		if (picked == null)
			return null;
		var ids = picked.Select(r => (int)r["agencyID"]).ToHashSet();
		return agencies.Where(a => ids.Contains(a.AgencyId)).ToList();
	}

	/// <summary>Готовый документ и агентство, по шаблону которого он сделан.</summary>
	private sealed record DocumentFile(ExportFile File, string Agency);

	/// <summary>
	/// Все документы в одном окне просмотра — закладками, в любую сторону. «Скачать Word» — один файл («Сохранить как»),
	/// «Скачать все» — все файлы в выбранную папку.
	/// </summary>
	private async Task ShowDocuments(string caption, IList<DocumentFile> documents)
	{
		if (documents.Count == 0)
			return;
		IReadOnlyList<ExportFile> files = UniqueNames(documents);
		// Закладка — агентство, если оно у каждого документа своё; иначе (месяцы, станции) — имя файла.
		bool byAgency = documents.Select(d => d.Agency).Distinct().Count() == documents.Count;
		var items = documents.Select((d, i) => new DocumentsPreview.Item(
			byAgency ? d.Agency : Path.GetFileNameWithoutExtension(files[i].Name), files[i].Name, files[i].Content)).ToList();

		DialogOutcome outcome = await _dialogs.ShowAsync(files.Count == 1 ? files[0].Name : caption, builder =>
		{
			builder.OpenComponent<DocumentsPreview>(0);
			builder.AddComponentParameter(1, nameof(DocumentsPreview.Files), items);
			builder.CloseComponent();
		}, okText: files.Count == 1 ? Tr.T("Скачать Word") : Tr.Format("Скачать все ({0})", files.Count),
			cancelText: Tr.T("Закрыть"), wide: true);
		if (outcome != DialogOutcome.Ok)
			return;

		if (files.Count == 1)
		{
			await SaveMediaPlan(files[0]); // общее «Сохранить как» с повтором по новому щелчку
			return;
		}
		bool saved = await _saver.SaveToFolderAsync(files, async () =>
			await _dialogs.ShowAsync(caption, b => b.AddContent(0, Tr.T("Файл готов. Выберите, куда сохранить файл.")),
				okText: Tr.T("Сохранить…")) == DialogOutcome.Ok);
		if (saved)
			await ShowInfo(caption, Tr.Format("Сохранено файлов: {0}.", files.Count));
	}

	/// <summary>
	/// Имена файлов в одной папке не повторяются: у двух агентств счёт может получить один и тот же номер, а договор фирмы
	/// у всех агентств называется одинаково — одноимённым добавляется агентство, а если и оно не развело — номер.
	/// </summary>
	private static IReadOnlyList<ExportFile> UniqueNames(IList<DocumentFile> documents)
	{
		HashSet<string> repeated = documents.GroupBy(d => d.File.Name, StringComparer.CurrentCultureIgnoreCase)
			.Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.CurrentCultureIgnoreCase);
		var used = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
		var result = new List<ExportFile>();
		foreach (DocumentFile document in documents)
		{
			string stem = Path.GetFileNameWithoutExtension(document.File.Name);
			if (repeated.Contains(document.File.Name))
				stem += " — " + Path.GetInvalidFileNameChars().Aggregate(document.Agency, (name, c) => name.Replace(c, '_'));
			string name = stem + ".docx";
			for (int n = 2; !used.Add(name); n++)
				name = $"{stem} ({n}).docx";
			result.Add(new ExportFile { Name = name, Content = document.File.Content });
		}
		return result;
	}
}
