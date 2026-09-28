using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Merlin.Classes.Documents
{
	/// <summary>Вид поля шаблона документа (docs/tasks/web-reports.md §8.1).</summary>
	public enum DocumentFieldKind
	{
		/// <summary>Текст: <c>{{Поле}}</c>; в <c>{{#Поле}}</c> — «непустой».</summary>
		Text,
		/// <summary>Условие: только <c>{{#Поле}}…{{/Поле}}</c> и <c>{{^Поле}}…{{/Поле}}</c>.</summary>
		Flag,
		/// <summary>Список: <c>{{#Поле}}…{{/Поле}}</c> повторяется для каждого элемента.</summary>
		List,
		/// <summary>Картинка: <c>{{Поле}}</c> заменяется изображением.</summary>
		Image
	}

	/// <summary>
	/// Сравнение имён полей: без учёта регистра и без различия «е»/«ё» — в Word
	/// администратор наберёт «Счет» так же часто, как «Счёт».
	/// </summary>
	public sealed class DocumentFieldNameComparer : IEqualityComparer<string>
	{
		public static readonly DocumentFieldNameComparer Instance = new DocumentFieldNameComparer();

		private static string Normalize(string name)
		{
			return name == null ? null : name.Replace('ё', 'е').Replace('Ё', 'Е');
		}

		public bool Equals(string x, string y)
		{
			return StringComparer.OrdinalIgnoreCase.Equals(Normalize(x), Normalize(y));
		}

		public int GetHashCode(string name)
		{
			return StringComparer.OrdinalIgnoreCase.GetHashCode(Normalize(name));
		}
	}

	/// <summary>Описание поля для проверки шаблона и справочника полей.</summary>
	public sealed class DocumentField
	{
		public DocumentField(string name, DocumentFieldKind kind, string description,
			IEnumerable<DocumentField> itemFields = null)
		{
			Name = name;
			Kind = kind;
			Description = description;
			ItemFields = itemFields == null ? new List<DocumentField>() : new List<DocumentField>(itemFields);
		}

		public string Name { get; private set; }
		public DocumentFieldKind Kind { get; private set; }
		public string Description { get; private set; }

		/// <summary>Поля элемента списка (только для <see cref="DocumentFieldKind.List"/>).</summary>
		public IList<DocumentField> ItemFields { get; private set; }
	}

	/// <summary>Картинка для поля-картинки. Хранится в PNG, размер — в EMU (1 см = 360 000).</summary>
	public sealed class DocumentImage
	{
		private const long EmuPerInch = 914400;

		public DocumentImage(byte[] png, long widthEmu, long heightEmu)
		{
			Png = png;
			WidthEmu = widthEmu;
			HeightEmu = heightEmu;
		}

		public byte[] Png { get; private set; }
		public long WidthEmu { get; private set; }
		public long HeightEmu { get; private set; }

		/// <summary>
		/// Из байтов картинки любого формата (как хранит поле <c>painting</c>).
		/// Размер — по разрешению самой картинки, как в Crystal (<c>GenericReport.SetPaintings</c>).
		/// </summary>
		public static DocumentImage FromBytes(byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0)
				return null;
			using (var source = new MemoryStream(bytes))
			using (var image = Image.FromStream(source))
			using (var png = new MemoryStream())
			{
				image.Save(png, ImageFormat.Png);
				float dpiX = image.HorizontalResolution > 0 ? image.HorizontalResolution : 96;
				float dpiY = image.VerticalResolution > 0 ? image.VerticalResolution : 96;
				return new DocumentImage(png.ToArray(),
					(long)(image.Width * EmuPerInch / dpiX),
					(long)(image.Height * EmuPerInch / dpiY));
			}
		}

		/// <summary>Та же картинка, вписанная в заданную ширину (пропорционально).</summary>
		public DocumentImage WithWidth(long widthEmu)
		{
			if (WidthEmu <= 0)
				return this;
			return new DocumentImage(Png, widthEmu, HeightEmu * widthEmu / WidthEmu);
		}
	}

	/// <summary>
	/// Значения полей для одного документа. Элемент списка — тоже <see cref="DocumentData"/>,
	/// поиск поля идёт от элемента к общим значениям документа.
	/// </summary>
	public sealed class DocumentData
	{
		private readonly Dictionary<string, object> values =
			new Dictionary<string, object>(DocumentFieldNameComparer.Instance);

		public DocumentData()
		{
		}

		private DocumentData(DocumentData parent)
		{
			Parent = parent;
		}

		public DocumentData Parent { get; private set; }

		public DocumentData Set(string name, string value)
		{
			values[name] = value ?? string.Empty;
			return this;
		}

		public DocumentData SetFlag(string name, bool value)
		{
			values[name] = value;
			return this;
		}

		public DocumentData SetImage(string name, DocumentImage image)
		{
			values[name] = image;
			return this;
		}

		/// <summary>Добавляет элемент списка <paramref name="name"/> и возвращает его для заполнения.</summary>
		public DocumentData AddItem(string name)
		{
			List<DocumentData> list = GetOrCreateList(name);
			var item = new DocumentData(this);
			list.Add(item);
			return item;
		}

		/// <summary>Заводит пустой список (чтобы <c>{{^Список}}</c> сработал, а не упал на неизвестном поле).</summary>
		public DocumentData SetEmptyList(string name)
		{
			GetOrCreateList(name);
			return this;
		}

		private List<DocumentData> GetOrCreateList(string name)
		{
			object existing;
			if (values.TryGetValue(name, out existing) && existing is List<DocumentData>)
				return (List<DocumentData>)existing;
			var list = new List<DocumentData>();
			values[name] = list;
			return list;
		}

		internal bool TryGet(string name, out object value)
		{
			for (DocumentData scope = this; scope != null; scope = scope.Parent)
				if (scope.values.TryGetValue(name, out value))
					return true;
			value = null;
			return false;
		}
	}
}
