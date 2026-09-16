using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using FogSoft.WinForm.Forms;

namespace Merlin.Forms
{
	// Простой немодальный просмотрщик html-справки — общий для любого экрана, не только
	// веера. Файлы справки лежат обычными файлами на диске (Client\Help\*.html,
	// CopyToOutputDirectory в Client.csproj), не EmbeddedResource: их можно
	// поправить прямо в развёрнутой копии, без пересборки и передеплоя exe.
	// См. CampaignForm.HelpFileName/ShowHelp — конкретный экран просто называет свой
	// файл, эта форма и вся навигация — общие.
	internal partial class HelpViewerForm : Form
	{
		// Одно окно на файл: повторный вызов той же справки выводит уже открытое окно
		// на передний план, а не плодит копии. Разные файлы — разные окна одновременно.
		private static readonly Dictionary<string, HelpViewerForm> OpenForms =
			new Dictionary<string, HelpViewerForm>(StringComparer.OrdinalIgnoreCase);

		private readonly string _filePath;

		private HelpViewerForm(string title, string filePath)
		{
			InitializeComponent();
			Text = title;
			_filePath = filePath;
			webBrowser.Navigate(filePath);
		}

		// Название отличается от унаследованного Form.Show() нарочно (другая сигнатура их
		// и так не путает компилятору, но глазами читать так спокойнее).
		public static void ShowHelp(string title, string filePath)
		{
			if (!File.Exists(filePath))
			{
				UserMessage.ShowExclamation("Файл справки не найден: " + filePath);
				return;
			}

			if (OpenForms.TryGetValue(filePath, out HelpViewerForm existing) && !existing.IsDisposed)
			{
				existing.Activate();
				return;
			}

			HelpViewerForm form = new HelpViewerForm(title, filePath);
			OpenForms[filePath] = form;
			form.FormClosed += (s, e) => OpenForms.Remove(form._filePath);
			form.Show();
		}
	}
}
