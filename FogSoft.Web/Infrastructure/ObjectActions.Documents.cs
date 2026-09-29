using System.Data;
using FogSoft.Web.Components;
using FogSoft.WinForm.Classes;
using Merlin.Classes;
using Merlin.Classes.Documents;
using Merlin.Classes.GridExport;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Документы для клиента из Word-шаблонов агентства (docs/tasks/web-reports.md §6.0, §8 этап 5).
/// Шаги как в десктопе (Action.PrintContracts): выбор агентств, если их несколько; номер и дата
/// счёта (FrmBill) — одним окном на все агентства, с флажком подписи вместо вопроса при печати;
/// счёт записывается в базу; затем документ показывается в окне и скачивается как Word.
/// Строит документ ядро — <see cref="ClientDocuments"/>.
/// </summary>
public sealed partial class ObjectActions
{
	// Отрицательный — строки в iEntity нет; сущность не кэшируется и не ищется по id.
	private const int DocumentAgenciesEntityId = -5102;

	private async Task<ActionEffect> PrintContract(Merlin.Classes.Action action, DocumentKind kind)
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

		var model = new DocumentBillForm.Model { NextNumber = DocumentBills.NextNumber };
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

		DialogOutcome outcome = await _dialogs.ShowAsync(caption, builder =>
		{
			builder.OpenComponent<DocumentBillForm>(0);
			builder.AddComponentParameter(1, nameof(DocumentBillForm.Value), model);
			builder.CloseComponent();
		}, okText: Tr.T("Сформировать"));
		if (outcome != DialogOutcome.Ok)
			return ActionEffect.None;

		var files = new List<ExportFile>();
		string? error = null;
		await _busy.RunAsync(() =>
		{
			foreach (DocumentBillForm.Row row in model.Rows)
			{
				// Как «Ок» в FrmBill: номер и дата счёта записываются до печати.
				DocumentBills.Save(action, row.AgencyId, row.Number, row.Date);
				Agency agency = agencies.First(a => a.AgencyId == row.AgencyId);
				try
				{
					files.Add(ClientDocuments.ContractFile(action, agency, kind,
						new DocumentBill(row.Number, row.Date), model.WithSignature));
				}
				catch (DocumentTemplateException e)
				{
					error = Tr.Format("Шаблон документа агентства «{0}» не подходит: {1}", agency.Name, e.Message);
					return;
				}
			}
		});
		if (error != null)
		{
			await ShowInfo(caption, error);
			return ActionEffect.None;
		}

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
		IReadOnlyList<DataRow>? picked = await PickAsync(Tr.T("Выбор агентств"), entity, table, Tr.T("Выбрать"),
			multiselect: true, validate: rows => rows.Count == 0 ? Tr.T("Отметьте хотя бы одно агентство.") : null);
		if (picked == null)
			return null;
		var ids = picked.Select(r => (int)r["agencyID"]).ToHashSet();
		return agencies.Where(a => ids.Contains(a.AgencyId)).ToList();
	}

	/// <summary>Документы по одному в окне просмотра; «Скачать Word» — «Сохранить как» браузера.</summary>
	private async Task ShowDocuments(string caption, IList<ExportFile> files)
	{
		for (int i = 0; i < files.Count; i++)
		{
			ExportFile file = files[i];
			string title = files.Count == 1 ? file.Name : Tr.Format("{0} ({1} из {2})", file.Name, i + 1, files.Count);
			DialogOutcome outcome = await _dialogs.ShowAsync(title, builder =>
			{
				builder.OpenComponent<DocxPreview>(0);
				builder.AddComponentParameter(1, nameof(DocxPreview.Content), file.Content);
				builder.CloseComponent();
			}, okText: Tr.T("Скачать Word"), cancelText: i < files.Count - 1 ? Tr.T("Следующий") : Tr.T("Закрыть"), wide: true);
			if (outcome == DialogOutcome.Ok)
				await SaveMediaPlan(file); // общее «Сохранить как» с повтором по новому щелчку
		}
	}
}
