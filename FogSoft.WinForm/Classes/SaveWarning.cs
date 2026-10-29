using System.Collections.Generic;
using System.Data;

namespace FogSoft.WinForm.Classes
{
	/// <summary>
	/// Что спросить у пользователя перед записью или удалением объекта, когда операция задевает
	/// другие данные (правка скидки, которой уже посчитаны акции): текст, таблица задетого и кнопка.
	/// Ядро только собирает вопрос (<see cref="PresentationObject.GetSaveWarning"/>,
	/// <see cref="PresentationObject.GetDeleteWarning"/>), спрашивает каждый UI сам — десктоп модальной
	/// формой изнутри Update()/ConfirmDelete(), веб своим окном до Update()/Delete(silenceFlag: true).
	/// Разрез «спросить / сделать» — docs/tasks/web-migration-dialogs.md.
	/// </summary>
	public sealed class SaveWarning
	{
		public SaveWarning(string title, IList<string> paragraphs, DataTable table, Entity.Attribute[] columns, string okText)
		{
			Title = title;
			Paragraphs = paragraphs;
			Table = table;
			Columns = columns;
			OkText = okText;
		}

		/// <summary>Заголовок окна; уже переведён.</summary>
		public string Title { get; }

		/// <summary>Абзацы текста над таблицей, каждый — целая фраза; уже переведены.</summary>
		public IList<string> Paragraphs { get; }

		/// <summary>Что задето; строк всегда больше нуля (нечего показать — предупреждения нет).</summary>
		public DataTable Table { get; }

		/// <summary>Колонки таблицы: имя — колонка <see cref="Table"/>, подпись — по-русски (переводит показ).</summary>
		public Entity.Attribute[] Columns { get; }

		/// <summary>Кнопка согласия («Сохранить»); уже переведена.</summary>
		public string OkText { get; }
	}
}
