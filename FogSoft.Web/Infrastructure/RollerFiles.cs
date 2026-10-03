using FogSoft.WinForm.Classes;
using NAudio.Wave;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Файлы роликов на сервере. Веб стоит на той же машине, что и папка роликов (на проде
/// C:\FTP\Repository, туда же файлы кладут по FTP), поэтому в <c>Roller.path</c> лежит
/// локальный путь сервера и веб читает и пишет файлы напрямую — как десктоп под RDP
/// (MediaControl, RollersCopyFrm, RollersDeleteFrm, RollerPassportForm).
///
/// Папка роликов нужна только для выбора и загрузки файла в карточке ролика — ключ
/// <c>RollerFolder</c> в FogSoft.Web.dll.config. Прослушивание, сохранение и удаление идут
/// по пути из базы, какой бы он ни был.
/// </summary>
public static class RollerFiles
{
	public const string FolderSetting = "RollerFolder";

	/// <summary>Звуковые файлы, которые принимал десктопный диалог «Загрузить с диска».</summary>
	public static readonly string[] Extensions = { ".mp3", ".mp2", ".wav" };

	/// <summary>Фильтр для &lt;input type="file"&gt;.</summary>
	public static string Accept => string.Join(",", Extensions);

	/// <summary>
	/// Предел загрузки. Ролик в mp3 — сотни килобайт, минутный wav — около 10 МБ; предел нужен,
	/// чтобы отказ был внятным, а не обрывом соединения.
	/// </summary>
	public const long MaxUploadBytes = 100L * 1024 * 1024;

	/// <summary>Папка роликов из конфига; null — не задана.</summary>
	public static string? Folder
	{
		get
		{
			string folder = ConfigurationUtil.GetSettings(FolderSetting, "");
			return folder.Length == 0 ? null : folder;
		}
	}

	/// <summary>Сообщение, если папкой пользоваться нельзя; null — можно.</summary>
	public static string? FolderProblem()
	{
		string? folder = Folder;
		if (folder == null)
			return Tr.Format("Папка роликов не задана: ключ {0} в настройках веба.", FolderSetting);
		if (!Directory.Exists(folder))
			return Tr.Format("Папка роликов {0} недоступна.", folder);
		return null;
	}

	public static bool IsSound(string fileName) =>
		Extensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);

	/// <summary>Звуковые файлы папки роликов, новые сверху: только что пришедший по FTP — первым.</summary>
	public static List<FileInfo> List()
	{
		var folder = new DirectoryInfo(Folder!);
		return folder.EnumerateFiles()
			.Where(f => IsSound(f.Name))
			.OrderByDescending(f => f.LastWriteTime)
			.ToList();
	}

	/// <summary>
	/// Продолжительность в секундах — как RollerPassportForm.GetFileDuration: полное время файла,
	/// округлённое до целых секунд от нуля. Десктоп читал AudioFileReader; для mp3 это
	/// Mp3FileReader, который считает время по заголовкам кадров, — здесь тот же разбор кадров
	/// из NAudio.Core, без декодера (звук не распаковывается, нужен только счёт отсчётов).
	/// </summary>
	public static int DurationSeconds(string path)
	{
		using WaveStream reader = Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase)
			? new WaveFileReader(path)
			: new Mp3FileReaderBase(path, format => new CountOnlyDecompressor(format));
		return (int)Math.Round(reader.TotalTime.TotalSeconds, MidpointRounding.AwayFromZero);
	}

	/// <summary>
	/// Сохраняет загруженный файл в папку роликов под его именем. Файл с таким именем уже есть —
	/// отказ (решение владельца 2026-10-02): чужой ролик не затирается, существующий файл
	/// выбирается из списка папки.
	/// </summary>
	/// <returns>Полный путь сохранённого файла либо отказ пользователю.</returns>
	public static async Task<(string? Path, string? Refusal)> SaveUploadAsync(string fileName, Stream content)
	{
		string name = Path.GetFileName(fileName);
		if (!IsSound(name))
			return (null, Tr.Format("Нужен звуковой файл: {0}.", string.Join(", ", Extensions)));

		string path = Path.Combine(Folder!, name);
		FileStream target;
		try
		{
			// CreateNew — проверка «уже есть» и создание одним действием.
			target = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
		}
		catch (IOException) when (File.Exists(path))
		{
			return (null, Tr.Format("Файл {0} уже есть в папке роликов. Выберите его из списка или переименуйте файл.", name));
		}

		try
		{
			await using (target)
				await content.CopyToAsync(target);
		}
		catch
		{
			// Оборванная загрузка не должна оставить в папке половину файла.
			File.Delete(path);
			throw;
		}

		return (path, null);
	}

	/// <summary>Тип для &lt;audio&gt; по расширению.</summary>
	public static string ContentType(string path) =>
		Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase) ? "audio/wav" : "audio/mpeg";

	/// <summary>
	/// «Декодер» для <see cref="Mp3FileReaderBase"/>, который ничего не декодирует: читателю он
	/// нужен только ради формата вывода, а продолжительность считается по кадрам.
	/// </summary>
	private sealed class CountOnlyDecompressor : IMp3FrameDecompressor
	{
		public CountOnlyDecompressor(WaveFormat mp3Format)
		{
			OutputFormat = new WaveFormat(mp3Format.SampleRate, 16, mp3Format.Channels);
		}

		public WaveFormat OutputFormat { get; }

		public int DecompressFrame(Mp3Frame frame, byte[] dest, int destOffset) => 0;

		public void Reset() { }

		public void Dispose() { }
	}
}
