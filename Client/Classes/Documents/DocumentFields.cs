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
			public const string AgencyName = "Агентство.Название";
			public const string AgencyShortName = "Агентство.НазваниеБезФормы";
			public const string AgencyFullForm = "Агентство.ФормаПолностью";
			public const string AgencyActingBy = "Агентство.ВЛице";
			public const string AgencyRegistration = "Агентство.Регистрация";
			public const string AgencyDirector = "Агентство.Директор";
			public const string AgencyBookKeeper = "Агентство.Бухгалтер";
			public const string AgencyAddress = "Агентство.Адрес";
			public const string AgencyPhone = "Агентство.Телефон";
			public const string AgencyEmail = "Агентство.Почта";
			public const string AgencyInn = "Агентство.ИНН";
			public const string AgencyKpp = "Агентство.КПП";
			public const string AgencyOgrn = "Агентство.ОГРН";
			public const string AgencyAccount = "Агентство.Счёт";
			public const string AgencyBank = "Агентство.Банк";
			public const string AgencyCorAccount = "Агентство.КоррСчёт";
			public const string AgencyBik = "Агентство.БИК";
			public const string AgencyPlace = "Агентство.МестоДоговора";
			public const string AgencySignature = "Агентство.Подпись";

			public const string FirmName = "Фирма.Название";
			public const string FirmShortName = "Фирма.НазваниеБезФормы";
			public const string FirmActingBy = "Фирма.ВЛице";
			public const string FirmRegistration = "Фирма.Регистрация";
			public const string FirmDirector = "Фирма.Директор";
			public const string FirmAddress = "Фирма.Адрес";
			public const string FirmPhone = "Фирма.Телефон";
			public const string FirmEmail = "Фирма.Почта";
			public const string FirmInn = "Фирма.ИНН";
			public const string FirmKpp = "Фирма.КПП";
			public const string FirmOgrn = "Фирма.ОГРН";
			public const string FirmAccount = "Фирма.Счёт";
			public const string FirmBank = "Фирма.Банк";
			public const string FirmCorAccount = "Фирма.КоррСчёт";
			public const string FirmBik = "Фирма.БИК";

			public const string ActionNumber = "Акция.Номер";
			public const string ByAction = "ПоАкции";
			public const string Number = "Документ.Номер";
			public const string Date = "Документ.Дата";
			public const string DateInWords = "Документ.ДатаСловами";

			public const string ManagerName = "Менеджер.ФИО";
			public const string ManagerPhone = "Менеджер.Телефон";
			public const string ManagerEmail = "Менеджер.Почта";
			public const string ManagerContacts = "Менеджер.Контакты";

			public const string WithTax = "СНДС";
			public const string TaxRate = "НДС.Ставка";
			public const string TaxSum = "НДС.Сумма";
			public const string Total = "Сумма";
			public const string TotalInWords = "СуммаПрописью";

			public const string Rows = "Строки";
			public const string RowNumber = "Номер";
			public const string RowName = "Наименование";
			public const string RowQuantity = "Количество";
			public const string RowSum = "Сумма";
			public const string RowTax = "НДС";

			public const string ForMonth = "ЗаМесяц";
			public const string Month = "Месяц";
			public const string Qr = "QRКод";

			public const string StationName = "Станция.Название";
			public const string StationFounder = "Станция.Учредитель";
			public const string StationGroup = "Станция.Группа";
			public const string StationRadio = "Станция.Радиостанция";
			public const string StationDirector = "Станция.Директор";
			public const string StationCertificate = "Станция.Свидетельство";
			public const string StationSignature = "Станция.Подпись";

			public const string Issues = "Выходы";
			public const string SponsorIssues = "СпонсорскиеВыходы";
			public const string IssueName = "Название";
			public const string IssueDuration = "Хронометраж";
			public const string IssueDate = "Дата";
			public const string IssueTime = "Время";
			public const string WithPrice = "СЦеной";
		}

		/// <summary>Поля шаблона документа данного вида (описания — на языке пользователя).</summary>
		public static IList<DocumentField> For(DocumentKind kind)
		{
			var fields = new List<DocumentField>();
			fields.AddRange(AgencyFields());
			fields.AddRange(FirmFields());
			fields.Add(Text(Names.ActionNumber, "Номер акции"));

			switch (kind)
			{
				case DocumentKind.Contract:
				case DocumentKind.SponsorContract:
					fields.Add(Flag(Names.ByAction, "Договор печатается по акции (а не из карточки фирмы без акции)"));
					fields.AddRange(DocumentHeaderFields());
					fields.AddRange(TaxRateFields());
					break;
				case DocumentKind.Bill:
				case DocumentKind.BillContract:
					fields.AddRange(DocumentHeaderFields());
					fields.AddRange(TaxRateFields());
					fields.AddRange(SumFields());
					fields.AddRange(ManagerFields());
					fields.Add(new DocumentField(Names.Rows, DocumentFieldKind.List, Tr.T("Строки счёта: радиостанции и программы"), new[]
					{
						Text(Names.RowNumber, "Номер строки по порядку"),
						Text(Names.RowName, "Наименование услуги (текст из настроек счёта и радиостанции)"),
						Text(Names.RowQuantity, "Количество выходов"),
						Text(Names.RowSum, "Сумма строки с НДС"),
						Text(Names.RowTax, "НДС в строке")
					}));
					fields.Add(Flag(Names.ForMonth, "Счёт за один месяц (печать счёта по месяцам)"));
					fields.Add(Text(Names.Month, "Месяц счёта, например «сентябрь 2026»"));
					fields.Add(Image(Names.Qr, "QR-код для оплаты (нет, если у агентства не указаны счёт, банк или БИК)"));
					break;
				case DocumentKind.OnAirInquire:
					fields.AddRange(TaxRateFields());
					fields.AddRange(SumFields());
					fields.Add(Flag(Names.WithPrice, "При печати отмечено «Включать цену»"));
					fields.Add(Text(Names.Month, "Месяц справки, например «сентябрь 2026»"));
					fields.Add(Text(Names.StationName, "Радиостанция: название для документов"));
					fields.Add(Text(Names.StationFounder, "Радиостанция: учредитель"));
					fields.Add(Text(Names.StationGroup, "Радиостанция: группа"));
					fields.Add(Text(Names.StationRadio, "Радиостанция: название без группы"));
					fields.Add(Text(Names.StationDirector, "Радиостанция: директор"));
					fields.Add(Text(Names.StationCertificate, "Радиостанция: свидетельство о регистрации СМИ"));
					fields.Add(Image(Names.StationSignature, "Радиостанция: подпись и печать (если при печати выбрано «С подписью и печатью»)"));
					fields.Add(IssueList(Names.Issues, "Выходы роликов за месяц"));
					fields.Add(IssueList(Names.SponsorIssues, "Выходы спонсорских программ за месяц"));
					break;
			}
			return fields;
		}

		private static IEnumerable<DocumentField> AgencyFields()
		{
			yield return Text(Names.AgencyName, "Агентство: название с формой собственности, например ООО «Ромашка»");
			yield return Text(Names.AgencyShortName, "Агентство: название без формы собственности");
			yield return Text(Names.AgencyFullForm, "Агентство: форма собственности полностью");
			yield return Text(Names.AgencyActingBy, "Агентство: «в лице … действующего на основании …»");
			yield return Text(Names.AgencyRegistration, "Агентство: регистрация");
			yield return Text(Names.AgencyDirector, "Агентство: директор");
			yield return Text(Names.AgencyBookKeeper, "Агентство: бухгалтер");
			yield return Text(Names.AgencyAddress, "Агентство: адрес");
			yield return Text(Names.AgencyPhone, "Агентство: телефон");
			yield return Text(Names.AgencyEmail, "Агентство: электронная почта");
			yield return Text(Names.AgencyInn, "Агентство: ИНН");
			yield return Text(Names.AgencyKpp, "Агентство: КПП");
			yield return Text(Names.AgencyOgrn, "Агентство: ОГРН");
			yield return Text(Names.AgencyAccount, "Агентство: расчётный счёт");
			yield return Text(Names.AgencyBank, "Агентство: банк");
			yield return Text(Names.AgencyCorAccount, "Агентство: корреспондентский счёт банка");
			yield return Text(Names.AgencyBik, "Агентство: БИК банка");
			yield return Text(Names.AgencyPlace, "Агентство: город заключения договора");
			yield return Image(Names.AgencySignature, "Агентство: подпись и печать (если при печати выбрано «С подписью и печатью»)");
		}

		private static IEnumerable<DocumentField> FirmFields()
		{
			yield return Text(Names.FirmName, "Фирма: название с формой собственности");
			yield return Text(Names.FirmShortName, "Фирма: название без формы собственности");
			yield return Text(Names.FirmActingBy, "Фирма: «в лице … действующего на основании …» (пусто — линия для заполнения от руки)");
			yield return Text(Names.FirmRegistration, "Фирма: регистрация (пусто — линия для заполнения от руки)");
			yield return Text(Names.FirmDirector, "Фирма: директор");
			yield return Text(Names.FirmAddress, "Фирма: адрес");
			yield return Text(Names.FirmPhone, "Фирма: телефон");
			yield return Text(Names.FirmEmail, "Фирма: электронная почта");
			yield return Text(Names.FirmInn, "Фирма: ИНН");
			yield return Text(Names.FirmKpp, "Фирма: КПП");
			yield return Text(Names.FirmOgrn, "Фирма: ОГРН");
			yield return Text(Names.FirmAccount, "Фирма: расчётный счёт");
			yield return Text(Names.FirmBank, "Фирма: банк");
			yield return Text(Names.FirmCorAccount, "Фирма: корреспондентский счёт банка");
			yield return Text(Names.FirmBik, "Фирма: БИК банка");
		}

		private static IEnumerable<DocumentField> DocumentHeaderFields()
		{
			yield return Text(Names.Number, "Номер счёта (он же номер договора)");
			yield return Text(Names.Date, "Дата документа, например 28.09.2026");
			yield return Text(Names.DateInWords, "Дата документа словами, например 28 сентября 2026 г.");
		}

		private static IEnumerable<DocumentField> TaxRateFields()
		{
			yield return Flag(Names.WithTax, "Облагается НДС: у агентства есть ставка на дату документа");
			yield return Text(Names.TaxRate, "Ставка НДС в процентах, например 5");
		}

		private static IEnumerable<DocumentField> SumFields()
		{
			yield return Text(Names.Total, "Сумма с НДС");
			yield return Text(Names.TotalInWords, "Сумма с НДС прописью");
			yield return Text(Names.TaxSum, "В том числе НДС");
		}

		private static IEnumerable<DocumentField> ManagerFields()
		{
			yield return Text(Names.ManagerName, "Менеджер акции: фамилия и имя");
			yield return Text(Names.ManagerPhone, "Менеджер акции: телефон");
			yield return Text(Names.ManagerEmail, "Менеджер акции: электронная почта");
			yield return Text(Names.ManagerContacts, "Менеджер акции: фамилия, имя, телефон и почта одной строкой");
		}

		private static DocumentField IssueList(string name, string description)
		{
			return new DocumentField(name, DocumentFieldKind.List, Tr.T(description), new[]
			{
				Text(Names.IssueName, "Ролик или программа"),
				Text(Names.IssueDuration, "Хронометраж"),
				Text(Names.IssueDate, "Дата выхода"),
				Text(Names.IssueTime, "Время выхода; (F), (S), (L) — первый, второй, последний в блоке")
			});
		}

		private static DocumentField Text(string name, string description)
		{
			return new DocumentField(name, DocumentFieldKind.Text, Tr.T(description));
		}

		private static DocumentField Flag(string name, string description)
		{
			return new DocumentField(name, DocumentFieldKind.Flag, Tr.T(description));
		}

		private static DocumentField Image(string name, string description)
		{
			return new DocumentField(name, DocumentFieldKind.Image, Tr.T(description));
		}
	}
}
