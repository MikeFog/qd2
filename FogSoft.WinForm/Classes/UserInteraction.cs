using System;

namespace FogSoft.WinForm.Classes
{
	/// <summary>
	/// Точка подстановки для вопросов пользователю из кода, который не должен
	/// знать про конкретный UI. Десктоп подставляет диалог WinForms, веб — свой
	/// способ подтверждения. См. docs/tasks/web-migration.md, этап 0.
	///
	/// Обработчик обязан быть назначен при старте приложения. Умолчания «да»
	/// здесь намеренно нет: незаданный обработчик означал бы удаление объектов
	/// без подтверждения, поэтому такая ситуация падает громко.
	/// </summary>
	public static class UserInteraction
	{
		private static Func<string, bool> _confirm;

		/// <summary>
		/// Задаёт способ задать пользователю вопрос «да/нет».
		/// </summary>
		public static void SetConfirmHandler(Func<string, bool> handler)
		{
			_confirm = handler ?? throw new ArgumentNullException(nameof(handler));
		}

		/// <summary>
		/// Задаёт пользователю вопрос. true — пользователь подтвердил действие.
		/// </summary>
		public static bool Confirm(string question)
		{
			if (_confirm == null)
				throw new InvalidOperationException(
					"UserInteraction.SetConfirmHandler не вызван при старте приложения: " +
					"некому задать пользователю вопрос о подтверждении.");

			return _confirm(question);
		}
	}
}
