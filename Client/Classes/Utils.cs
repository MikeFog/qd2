using System;
using System.Collections.Generic;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	// SelectManager, AskConfirmation, HideTableLayoutRow — в Utils.WinForms.cs.
	// AskConfirmation НЕ переносится в веб как функция (решение согласовано с
	// владельцем продукта 2026-08-21, см. docs/tasks/web-migration-dialogs.md,
	// §8 п.1): авторизацией скидки логином админа/грантора прямо в диалоге не
	// пользуются даже в десктопе. Из ядра метод убран не поэтому, а потому что
	// принимает IWin32Window — иначе он блокировал мост в FogSoft.Core для
	// CreateBankById, который остаётся здесь и нужен Organization.cs (§10).
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal static partial class Utils
	{
		public static PresentationObject CreateBankById(int bankID)
		{
			Entity entity = EntityManager.GetEntity((int) Entities.Bank);
			Dictionary<string, object> parameters =
				new Dictionary<string, object>(1, StringComparer.CurrentCultureIgnoreCase);
			parameters.Add("bankID", bankID.ToString());
			PresentationObject bank = entity.CreateObject(parameters);
			bank.Refresh();
			return bank;
		}


		// AskConfirmation и HideTableLayoutRow переехали в Utils.WinForms.cs
		// (мост в FogSoft.Core, §10 конвенции: CreateBankById здесь понадобился
		// Organization.cs, а весь файл раньше был исключён из моста целиком).

    }
}