using System.Drawing;
using System.IO;

namespace Merlin.Classes
{
	// UI-часть Organization: подпись как картинка.
	// Не диалог, но принимает/возвращает UI-тип (System.Drawing.Bitmap), поэтому
	// не может остаться в ядре — оно должно собираться вне проекта Client
	// (мост в FogSoft.Core, §10 конвенции).
	// В ядре остаётся байтовый SignatureBytes; Signature — ленивая обёртка над ним.
	// Логика не менялась, код перенесён как есть.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public abstract partial class Organization
	{
		private Bitmap _bitmap;

		public Bitmap Signature
		{
			get
			{
				if (SignatureBytes == null) return null;
				if (_bitmap == null)
				{
					using (MemoryStream stream = new MemoryStream(SignatureBytes))
					{
						_bitmap = new Bitmap(Image.FromStream(stream));
					}
				}
				return _bitmap;
			}
		}
	}
}
