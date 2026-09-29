using System.Collections.Generic;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes.Documents
{
	/// <summary>
	/// Поля Word-шаблонов документов для клиента (docs/tasks/web-reports.md §8.4):
	/// по нему проверяется загружаемый шаблон и строится справочник полей на экране.
	/// Значения собирает <see cref="ClientDocuments"/> теми же именами.
	/// </summary>
	public static class DocumentFields
	{
		public static class Names
		{
			public const string AgencyName = "Агентство.Название"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyShortName = "Агентство.НазваниеБезФормы"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyFullForm = "Агентство.ФормаПолностью"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyActingBy = "Агентство.ВЛице"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyRegistration = "Агентство.Регистрация"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyDirector = "Агентство.Директор"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyBookKeeper = "Агентство.Бухгалтер"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyAddress = "Агентство.Адрес"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyPhone = "Агентство.Телефон"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyEmail = "Агентство.Почта"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyInn = "Агентство.ИНН"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyKpp = "Агентство.КПП"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyOgrn = "Агентство.ОГРН"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyAccount = "Агентство.Счёт"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyBank = "Агентство.Банк"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyCorAccount = "Агентство.КоррСчёт"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyBik = "Агентство.БИК"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencyPlace = "Агентство.МестоДоговора"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string AgencySignature = "Агентство.Подпись"; // i18n-ok: имя поля в шаблоне (ключ данных)

			public const string FirmName = "Фирма.Название"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string FirmShortName = "Фирма.НазваниеБезФормы"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string FirmActingBy = "Фирма.ВЛице"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string FirmRegistration = "Фирма.Регистрация"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string FirmDirector = "Фирма.Директор"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string FirmAddress = "Фирма.Адрес"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string FirmPhone = "Фирма.Телефон"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string FirmEmail = "Фирма.Почта"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string FirmInn = "Фирма.ИНН"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string FirmKpp = "Фирма.КПП"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string FirmOgrn = "Фирма.ОГРН"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string FirmAccount = "Фирма.Счёт"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string FirmBank = "Фирма.Банк"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string FirmCorAccount = "Фирма.КоррСчёт"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string FirmBik = "Фирма.БИК"; // i18n-ok: имя поля в шаблоне (ключ данных)

			public const string ActionNumber = "Акция.Номер"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string ByAction = "ПоАкции"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string Number = "Документ.Номер"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string Date = "Документ.Дата"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string DateInWords = "Документ.ДатаСловами"; // i18n-ok: имя поля в шаблоне (ключ данных)

			public const string ManagerName = "Менеджер.ФИО"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string ManagerPhone = "Менеджер.Телефон"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string ManagerEmail = "Менеджер.Почта"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string ManagerContacts = "Менеджер.Контакты"; // i18n-ok: имя поля в шаблоне (ключ данных)

			public const string WithTax = "СНДС"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string TaxRate = "НДС.Ставка"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string TaxSum = "НДС.Сумма"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string Total = "Сумма"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string TotalInWords = "СуммаПрописью"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string TotalWithoutTax = "СуммаБезНДС"; // i18n-ok: имя поля в шаблоне (ключ данных)

			public const string Rows = "Строки"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string RowNumber = "Номер"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string RowName = "Наименование"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string RowQuantity = "Количество"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string RowSum = "Сумма"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string RowSumWithoutTax = "СуммаБезНДС"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string RowTax = "НДС"; // i18n-ok: имя поля в шаблоне (ключ данных)

			public const string ForMonth = "ЗаМесяц"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string Month = "Месяц"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string Qr = "QRКод"; // i18n-ok: имя поля в шаблоне (ключ данных)

			public const string StationName = "Станция.Название"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string StationFounder = "Станция.Учредитель"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string StationGroup = "Станция.Группа"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string StationRadio = "Станция.Радиостанция"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string StationDirector = "Станция.Директор"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string StationCertificate = "Станция.Свидетельство"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string StationSignature = "Станция.Подпись"; // i18n-ok: имя поля в шаблоне (ключ данных)

			public const string Issues = "Выходы"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string SponsorIssues = "СпонсорскиеВыходы"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string IssueName = "Название"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string IssueDuration = "Хронометраж"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string IssueDate = "Дата"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string IssueTime = "Время"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string WithPrice = "СЦеной"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string IssueCount = "ВсегоВыходов"; // i18n-ok: имя поля в шаблоне (ключ данных)
			public const string HasSponsorIssues = "ЕстьСпонсорскиеВыходы"; // i18n-ok: имя поля в шаблоне (ключ данных)
		}

		/// <summary>Поля шаблона документа данного вида (описания — на языке пользователя).</summary>
		public static IList<DocumentField> For(DocumentKind kind)
		{
			var fields = new List<DocumentField>();
			fields.AddRange(AgencyFields());
			fields.AddRange(FirmFields());
			fields.Add(Text(Names.ActionNumber, Tr.T("Номер акции")));

			switch (kind)
			{
				case DocumentKind.Contract:
				case DocumentKind.SponsorContract:
					fields.Add(Flag(Names.ByAction, Tr.T("Договор печатается по акции (а не из карточки фирмы без акции)")));
					fields.AddRange(DocumentHeaderFields());
					fields.AddRange(TaxRateFields());
					break;
				case DocumentKind.Bill:
				case DocumentKind.BillContract:
					fields.AddRange(DocumentHeaderFields());
					fields.AddRange(TaxRateFields());
					fields.AddRange(SumFields());
					fields.Add(Text(Names.TotalWithoutTax, Tr.T("Сумма без НДС")));
					fields.AddRange(ManagerFields());
					fields.Add(new DocumentField(Names.Rows, DocumentFieldKind.List, Tr.T("Строки счёта: радиостанции и программы"), new[]
					{
						Text(Names.RowNumber, Tr.T("Номер строки по порядку")),
						Text(Names.RowName, Tr.T("Наименование услуги (текст из настроек счёта и радиостанции)")),
						Text(Names.RowQuantity, Tr.T("Количество выходов")),
						Text(Names.RowSum, Tr.T("Сумма строки с НДС")),
						Text(Names.RowSumWithoutTax, Tr.T("Сумма строки без НДС")),
						Text(Names.RowTax, Tr.T("НДС в строке"))
					}));
					fields.Add(Flag(Names.ForMonth, Tr.T("Счёт за один месяц (печать счёта по месяцам)")));
					fields.Add(Text(Names.Month, Tr.T("Месяц счёта, например «сентябрь 2026»")));
					fields.Add(Image(Names.Qr, Tr.T("QR-код для оплаты (нет, если у агентства не указаны счёт, банк или БИК)")));
					break;
				case DocumentKind.OnAirInquire:
					fields.AddRange(TaxRateFields());
					fields.AddRange(SumFields());
					fields.Add(Flag(Names.WithPrice, Tr.T("При печати отмечено «Включать цену»")));
					fields.Add(Text(Names.Month, Tr.T("Месяц справки, например «сентябрь 2026»")));
					fields.Add(Text(Names.StationName, Tr.T("Радиостанция: название для документов")));
					fields.Add(Text(Names.StationFounder, Tr.T("Радиостанция: учредитель")));
					fields.Add(Text(Names.StationGroup, Tr.T("Радиостанция: группа")));
					fields.Add(Text(Names.StationRadio, Tr.T("Радиостанция: название без группы")));
					fields.Add(Text(Names.StationDirector, Tr.T("Радиостанция: директор")));
					fields.Add(Text(Names.StationCertificate, Tr.T("Радиостанция: свидетельство о регистрации СМИ")));
					fields.Add(Image(Names.StationSignature, Tr.T("Радиостанция: подпись и печать (если при печати выбрано «С подписью и печатью»)")));
					fields.Add(IssueList(Names.Issues, Tr.T("Выходы роликов за месяц")));
					fields.Add(Text(Names.IssueCount, Tr.T("Количество выходов роликов за месяц")));
					fields.Add(IssueList(Names.SponsorIssues, Tr.T("Выходы спонсорских программ за месяц")));
					fields.Add(Flag(Names.HasSponsorIssues, Tr.T("За месяц были выходы спонсорских программ")));
					break;
			}
			return fields;
		}

		private static IEnumerable<DocumentField> AgencyFields()
		{
			yield return Text(Names.AgencyName, Tr.T("Агентство: название с формой собственности, например ООО «Ромашка»"));
			yield return Text(Names.AgencyShortName, Tr.T("Агентство: название без формы собственности"));
			yield return Text(Names.AgencyFullForm, Tr.T("Агентство: форма собственности полностью"));
			yield return Text(Names.AgencyActingBy, Tr.T("Агентство: «в лице … действующего на основании …»"));
			yield return Text(Names.AgencyRegistration, Tr.T("Агентство: регистрация"));
			yield return Text(Names.AgencyDirector, Tr.T("Агентство: директор"));
			yield return Text(Names.AgencyBookKeeper, Tr.T("Агентство: бухгалтер"));
			yield return Text(Names.AgencyAddress, Tr.T("Агентство: адрес"));
			yield return Text(Names.AgencyPhone, Tr.T("Агентство: телефон"));
			yield return Text(Names.AgencyEmail, Tr.T("Агентство: электронная почта"));
			yield return Text(Names.AgencyInn, Tr.T("Агентство: ИНН"));
			yield return Text(Names.AgencyKpp, Tr.T("Агентство: КПП"));
			yield return Text(Names.AgencyOgrn, Tr.T("Агентство: ОГРН"));
			yield return Text(Names.AgencyAccount, Tr.T("Агентство: расчётный счёт"));
			yield return Text(Names.AgencyBank, Tr.T("Агентство: банк"));
			yield return Text(Names.AgencyCorAccount, Tr.T("Агентство: корреспондентский счёт банка"));
			yield return Text(Names.AgencyBik, Tr.T("Агентство: БИК банка"));
			yield return Text(Names.AgencyPlace, Tr.T("Агентство: город заключения договора"));
			yield return Image(Names.AgencySignature, Tr.T("Агентство: подпись и печать (если при печати выбрано «С подписью и печатью»)"));
		}

		private static IEnumerable<DocumentField> FirmFields()
		{
			yield return Text(Names.FirmName, Tr.T("Фирма: название с формой собственности"));
			yield return Text(Names.FirmShortName, Tr.T("Фирма: название без формы собственности"));
			yield return Text(Names.FirmActingBy, Tr.T("Фирма: «в лице … действующего на основании …» (пусто — линия для заполнения от руки)"));
			yield return Text(Names.FirmRegistration, Tr.T("Фирма: регистрация (пусто — линия для заполнения от руки)"));
			yield return Text(Names.FirmDirector, Tr.T("Фирма: директор"));
			yield return Text(Names.FirmAddress, Tr.T("Фирма: адрес"));
			yield return Text(Names.FirmPhone, Tr.T("Фирма: телефон"));
			yield return Text(Names.FirmEmail, Tr.T("Фирма: электронная почта"));
			yield return Text(Names.FirmInn, Tr.T("Фирма: ИНН"));
			yield return Text(Names.FirmKpp, Tr.T("Фирма: КПП"));
			yield return Text(Names.FirmOgrn, Tr.T("Фирма: ОГРН"));
			yield return Text(Names.FirmAccount, Tr.T("Фирма: расчётный счёт"));
			yield return Text(Names.FirmBank, Tr.T("Фирма: банк"));
			yield return Text(Names.FirmCorAccount, Tr.T("Фирма: корреспондентский счёт банка"));
			yield return Text(Names.FirmBik, Tr.T("Фирма: БИК банка"));
		}

		private static IEnumerable<DocumentField> DocumentHeaderFields()
		{
			yield return Text(Names.Number, Tr.T("Номер счёта (он же номер договора)"));
			yield return Text(Names.Date, Tr.T("Дата документа, например 28.09.2026"));
			yield return Text(Names.DateInWords, Tr.T("Дата документа словами, например 28 сентября 2026 г."));
		}

		private static IEnumerable<DocumentField> TaxRateFields()
		{
			yield return Flag(Names.WithTax, Tr.T("Облагается НДС: у агентства есть ставка на дату документа"));
			yield return Text(Names.TaxRate, Tr.T("Ставка НДС в процентах, например 5"));
		}

		private static IEnumerable<DocumentField> SumFields()
		{
			yield return Text(Names.Total, Tr.T("Сумма с НДС"));
			yield return Text(Names.TotalInWords, Tr.T("Сумма с НДС прописью"));
			yield return Text(Names.TaxSum, Tr.T("В том числе НДС"));
		}

		private static IEnumerable<DocumentField> ManagerFields()
		{
			yield return Text(Names.ManagerName, Tr.T("Менеджер акции: фамилия и имя"));
			yield return Text(Names.ManagerPhone, Tr.T("Менеджер акции: телефон"));
			yield return Text(Names.ManagerEmail, Tr.T("Менеджер акции: электронная почта"));
			yield return Text(Names.ManagerContacts, Tr.T("Менеджер акции: фамилия, имя, телефон и почта одной строкой"));
		}

		private static DocumentField IssueList(string name, string description)
		{
			return new DocumentField(name, DocumentFieldKind.List, description, new[]
			{
				Text(Names.IssueName, Tr.T("Ролик или программа")),
				Text(Names.IssueDuration, Tr.T("Хронометраж")),
				Text(Names.IssueDate, Tr.T("Дата выхода")),
				Text(Names.IssueTime, Tr.T("Время выхода; (F), (S), (L) — первый, второй, последний в блоке"))
			});
		}

		private static DocumentField Text(string name, string description)
		{
			return new DocumentField(name, DocumentFieldKind.Text, description);
		}

		private static DocumentField Flag(string name, string description)
		{
			return new DocumentField(name, DocumentFieldKind.Flag, description);
		}

		private static DocumentField Image(string name, string description)
		{
			return new DocumentField(name, DocumentFieldKind.Image, description);
		}
	}
}
