using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes.Documents
{
	/// <summary>Вид документа = строка справочника <c>ReportType</c>.</summary>
	public enum DocumentKind
	{
		Bill = 1,
		OnAirInquire = 2,
		BillContract = 3,
		Contract = 4,
		SponsorContract = 5
	}

	/// <summary>Версия Word-шаблона без самого файла (строка экрана «Шаблоны документов»).</summary>
	public sealed class DocumentTemplateVersion
	{
		public int Id { get; set; }
		public int AgencyId { get; set; }
		public string AgencyName { get; set; }
		public DocumentKind Kind { get; set; }
		public string KindName { get; set; }
		public DateTime StartDate { get; set; }
		public string FileName { get; set; }
		public string Comment { get; set; }
		public int Size { get; set; }
		public string CreatedByName { get; set; }
		public DateTime CreateDate { get; set; }

		/// <summary>Действует сегодня.</summary>
		public bool IsCurrent { get; set; }
	}

	/// <summary>Шаблон, выбранный для печати: файл и его версия.</summary>
	public sealed class DocumentTemplateFile
	{
		public int Id { get; set; }
		public DateTime StartDate { get; set; }
		public string FileName { get; set; }
		public byte[] Content { get; set; }
	}

	/// <summary>
	/// Хранилище Word-шаблонов (таблица <c>DocumentTemplate</c>, docs/tasks/web-reports.md §8 этап 2):
	/// шаблон на агентство × вид документа, версии с датой «действует с».
	/// </summary>
	public static class DocumentTemplateStore
	{
		public static List<DocumentTemplateVersion> List(int? agencyId = null, DocumentKind? kind = null)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			if (agencyId.HasValue)
				parameters["agencyID"] = agencyId.Value;
			if (kind.HasValue)
				parameters["reportTypeID"] = (int)kind.Value;

			var result = new List<DocumentTemplateVersion>();
			foreach (DataRow row in DataAccessor.LoadDataSet("DocumentTemplates", parameters).Tables[0].Rows)
				result.Add(new DocumentTemplateVersion
				{
					Id = (int)row["documentTemplateID"],
					AgencyId = Convert.ToInt32(row["agencyID"]),
					AgencyName = row["agencyName"].ToString(),
					Kind = (DocumentKind)Convert.ToInt32(row["reportTypeID"]),
					KindName = row["reportTypeName"].ToString(),
					StartDate = (DateTime)row["startDate"],
					FileName = row["fileName"].ToString(),
					Comment = row["comment"] as string,
					Size = Convert.ToInt32(row["size"]),
					CreatedByName = row["createdByName"] as string,
					CreateDate = (DateTime)row["createDate"],
					IsCurrent = (bool)row["isCurrent"]
				});
			return result;
		}

		public static DocumentTemplateFile Load(int id)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["documentTemplateID"] = id;
			return Retrieve(parameters);
		}

		/// <summary>Шаблон, действующий на дату документа; null — у агентства такого шаблона нет.</summary>
		public static DocumentTemplateFile ForDate(int agencyId, DocumentKind kind, DateTime date)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["agencyID"] = agencyId;
			parameters["reportTypeID"] = (int)kind;
			parameters["date"] = date.Date;
			return Retrieve(parameters);
		}

		private static DocumentTemplateFile Retrieve(Dictionary<string, object> parameters)
		{
			DataTable table = DataAccessor.LoadDataSet("DocumentTemplateRetrieve", parameters).Tables[0];
			if (table.Rows.Count == 0)
				return null;
			DataRow row = table.Rows[0];
			return new DocumentTemplateFile
			{
				Id = (int)row["documentTemplateID"],
				StartDate = (DateTime)row["startDate"],
				FileName = row["fileName"].ToString(),
				Content = (byte[])row["content"]
			};
		}

		/// <summary>
		/// Новая версия. Шаблон должен быть проверен заранее (<see cref="DocxTemplate.Validate"/>);
		/// автор — текущий пользователь (<c>@loggedUserId</c> подставляет DataAccessor).
		/// </summary>
		public static int Add(int agencyId, DocumentKind kind, DateTime startDate, byte[] content,
			string fileName, string comment)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["agencyID"] = agencyId;
			parameters["reportTypeID"] = (int)kind;
			parameters["startDate"] = startDate.Date;
			parameters["content"] = content;
			parameters["fileName"] = fileName;
			parameters["comment"] = comment;
			return Convert.ToInt32(DataAccessor.ExecuteScalar("DocumentTemplateIns", parameters));
		}

		public static void Delete(int id)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["documentTemplateID"] = id;
			DataAccessor.ExecuteNonQuery("DocumentTemplateDel", parameters);
		}
	}
}
