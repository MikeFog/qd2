using FogSoft.WinForm.Classes;
using Merlin.Classes.GridExport;
using Microsoft.JSInterop;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Сохранение готовых файлов на компьютер пользователя через диалог браузера — «Сохранить
/// как» для одного файла, выбор папки для многих (wwwroot/js/saveFiles.js). Решение
/// владельца 2026-09-25: десктопные «папки из настроек» были костылём RDP, в вебе — обычный
/// диалог. Браузеры — Chrome и Edge (File System Access API); в остальных — сообщение.
///
/// Окно выбора браузер открывает только в ответ на щелчок, поэтому место выбирается первым
/// (<see cref="PickFolderAsync"/>, <see cref="PickFileAsync"/>), а файлы пишутся после
/// подготовки. Scoped — модуль JS свой у circuit.
/// </summary>
public sealed class FileSaver : IAsyncDisposable
{
	private readonly IJSRuntime _js;
	private IJSObjectReference? _module;

	public FileSaver(IJSRuntime js)
	{
		_js = js;
	}

	private async Task<IJSObjectReference> Module() =>
		_module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/saveFiles.js");

	/// <summary>Сообщение для пользователя, если браузер не умеет сохранять в выбранное место; null — умеет.</summary>
	public async Task<string?> UnsupportedReasonAsync() =>
		await (await Module()).InvokeAsync<bool>("isSupported")
			? null
			: Tr.T("Выгрузка файлов работает в браузерах Chrome и Edge.");

	/// <summary>Выбор папки. false — пользователь закрыл окно.</summary>
	public async Task<bool> PickFolderAsync() => await (await Module()).InvokeAsync<bool>("pickDirectory");

	/// <summary>Записать файл в выбранную папку (существующий с тем же именем заменяется).</summary>
	public async Task WriteToFolderAsync(ExportFile file)
	{
		using var stream = new MemoryStream(file.Content);
		using var reference = new DotNetStreamReference(stream);
		await (await Module()).InvokeVoidAsync("writeToDirectory", file.Name, reference);
	}

	/// <summary>«Сохранить как» — выбор места для одного файла. false — пользователь закрыл окно.</summary>
	public async Task<bool> PickFileAsync(string suggestedName) =>
		await (await Module()).InvokeAsync<bool>("pickFile", suggestedName);

	/// <summary>Записать файл в место, выбранное <see cref="PickFileAsync"/>.</summary>
	public async Task WriteFileAsync(ExportFile file)
	{
		using var stream = new MemoryStream(file.Content);
		using var reference = new DotNetStreamReference(stream);
		await (await Module()).InvokeVoidAsync("writeFile", reference);
	}

	public async ValueTask DisposeAsync()
	{
		if (_module != null)
		{
			try { await _module.DisposeAsync(); }
			catch (JSDisconnectedException) { }
		}
	}
}
