using System;
using System.Collections.Generic;

namespace FogSoft.WinForm.Classes
{
	/// <summary>
	/// Точка подстановки для взаимодействия с пользователем из кода, который не
	/// должен знать про конкретный UI. Десктоп подставляет диалог WinForms, веб —
	/// свой способ. См. docs/tasks/web-migration.md, этап 0.
	///
	/// Обработчики обязаны быть назначены при старте приложения. Умолчаний нет
	/// намеренно: например, незаданный обработчик подтверждения означал бы
	/// удаление объектов без подтверждения, поэтому такая ситуация падает громко.
	/// </summary>
	public static class UserInteraction
	{
		private static Func<string, bool> _confirm;
		private static Action<string, Dictionary<string, object>> _notify;

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

		/// <summary>
		/// Задаёт способ уведомить пользователя о результате операции (не вопрос,
		/// ответа не ждём). <paramref name="handler"/> получает ключ сообщения
		/// (MessageAccessor/Resources) и параметры подстановки.
		/// </summary>
		public static void SetNotifyHandler(Action<string, Dictionary<string, object>> handler)
		{
			_notify = handler ?? throw new ArgumentNullException(nameof(handler));
		}

		/// <summary>
		/// Уведомляет пользователя о результате операции по ключу сообщения.
		/// </summary>
		public static void Notify(string messageKey, Dictionary<string, object> parameters)
		{
			if (_notify == null)
				throw new InvalidOperationException(
					"UserInteraction.SetNotifyHandler не вызван при старте приложения: " +
					"некому показать пользователю сообщение.");

			_notify(messageKey, parameters);
		}
	}
}
