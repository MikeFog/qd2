using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using static FogSoft.WinForm.Constants;

namespace Merlin.Classes
{
	// UI-часть (DoAction, PrintContract, SelectFirm) — в Firm.WinForms.cs. PrintContract перенесён целиком:
	// генерация отчёта — отдельная область (docs/tasks/web-migration.md, этап 4).
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class Firm : Organization
	{
		/// <summary>
		/// Набор колонок журнала «Фирмы-заказчики» (iEntityAttribute.selector сущности 16, Deploy/100): с «Идентификатором».
		/// Остальные таблицы фирм (выбор фирмы, «Группы компаний», пикеры веба) берут selector 0 — без него (просьба заказчика 19.11.2026).
		/// </summary>
		public const int JournalAttributeSelector = 1;

		/// <summary>
		/// Сущность фирм для журнала: клон с набором колонок журнала. Клон — потому что AttributeSelector у общей
		/// кэшированной сущности увидели бы и остальные таблицы фирм.
		/// </summary>
		public static Entity JournalEntity()
		{
			Entity entity = (Entity)EntityManager.GetEntity((int)Entities.Firm).Clone();
			entity.AttributeSelector = JournalAttributeSelector;
			return entity;
		}

		#region Constructors ----------------------------------

		public Firm(int firmID)
			: base(EntityManager.GetEntity((int) Entities.Firm))
		{
			this[ParamNames.FirmId] = firmID;
			isNew = false;
		}

		public Firm() : base(EntityManager.GetEntity((int) Entities.Firm))
		{
		}

		public Firm(DataRow row) : base(EntityManager.GetEntity((int) Entities.Firm), row)
		{
		}

        public Firm(Dictionary<string, object> parameters)
            : base(EntityManager.GetEntity((int)Entities.Firm), parameters)
		{
		}

		public Firm(Entity entity, DataRow row) : base(entity, row)
		{
		}

        #endregion

        // DoAction, PrintContract переехали в Firm.WinForms.cs.

		public DataTable GetRollers()
		{
			DataAccessor.PrepareParameters(parameters, EntityManager.GetEntity((int) Entities.Roller),
			                               InterfaceObjects.SimpleJournal, Constants.Actions.Load);
			parameters["ShowInactive"] = false;
			return ((DataSet) DataAccessor.DoAction(parameters)).Tables[Constants.TableNames.Data];
		}

		public int FirmId
		{
			get { return int.Parse(IDs[0].ToString()); }
		}

		public static Firm GetFirmById(int firmId)
		{
			Firm firm = new Firm(firmId);
			firm.Refresh();
			return firm;
		}

		/// <summary>Фирмы-заказчики — кандидаты для выбора.</summary>
		public static DataTable GetFirmCandidates()
		{
			Entity entity = EntityManager.GetEntity((int) Entities.Firm);
			Dictionary<string, object> filterValues =
				new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
			if (entity.IsFilterable)
				Globals.ResolveFilterInitialValues(filterValues, entity.XmlFilter);
			return entity.GetContent(filterValues);
		}

		// SelectFirm переехал в Firm.WinForms.cs.
    }
}
