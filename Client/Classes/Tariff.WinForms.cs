using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using FogSoft.WinForm.Passport.Forms;
using System.Data;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть Tariff: диспетчеризация действий (клонирование тарифа через
	// паспорт). Переехала целиком — IWin32Window в сигнатуре (§8 п.3).
	// Логика не менялась. Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class Tariff
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch(actionName)
			{
				case Constants.Actions.Clone:
					PresentationObject tariff = CreateCloneDraft();

					if (tariff.ShowPassport(owner))
						//OnObjectCreated(tariff);
						OnObjectCloned(tariff);
					break;

				case ActionNames.EditSimilar:
					EditSimilarTariffs(owner);
					break;

				default:
					base.DoAction(actionName, owner, interfaceObject);
					break;
			}
		}

		private struct ActionNames
		{
			public const string EditSimilar = "EditSimilarTariffs";
		}

		private const string EditSimilarPassportName = "TariffMassEdit";
		private const string MassMinuteParam = "tariffMinute";
		private const string MassHourFromParam = "hourFrom";
		private const string MassHourToParam = "hourTo";
		private const string MassHintParam = "massEditHint";
		private const string MassDaysHintParam = "massDaysHint";

		/// <summary>
		/// «Изменить похожие тарифы»: форма как при массовом создании (паспорт TariffMassEdit),
		/// предзаполнена значениями этого тарифа. Дни недели в форме - область применения:
		/// все дни - тарифы правятся на месте, часть дней - тарифы делятся (Tariff.ApplyMassEdit).
		/// </summary>
		private void EditSimilarTariffs(IWin32Window owner)
		{
			try
			{
				DataTable similar = LoadSimilarTariffs();
				if (similar.Rows.Count == 0) return;

				int minHour = int.MaxValue, maxHour = int.MinValue;
				foreach (DataRow row in similar.Rows)
				{
					int hour = Convert.ToInt32(row["hour"]);
					minHour = Math.Min(minHour, hour);
					maxHour = Math.Max(maxHour, hour);
				}

				Dictionary<string, object> original = Parameters;
				int minute = Time.Minute;

				Tariff template = new Tariff { Parameters = Parameters };
				template[MassMinuteParam] = minute;
				template[MassHourFromParam] = minHour;
				template[MassHourToParam] = maxHour;
				template[MassHintParam] = string.Format("{0} шт., минута :{1:00}, часы {2}-{3}",
					similar.Rows.Count, minute, minHour, maxHour);
				template[MassDaysHintParam] = "снимите дни, которые менять не нужно";

				// Данные паспорта (справочник типов блока) - та же процедура, что у обычной карточки тарифа.
				Dictionary<string, object> procParameters = template.Parameters;
				DataAccessor.PrepareParameters(procParameters, entity, InterfaceObjects.PropertyPage, Constants.Actions.Load);
				DataSet ds = DataAccessor.IsProcedureExist(procParameters) ? DataAccessor.DoAction(procParameters) as DataSet : null;

				DataTable tableErrors = null;
				List<Tariff> changed = null, added = null;

				UniversalPassportForm form = new UniversalPassportForm(template, EditSimilarPassportName,
					string.Format("Изменить похожие тарифы ({0} шт.)", similar.Rows.Count), entity, ds,
					edited => ValidateEditSimilar(original, edited),
					edited =>
					{
						Application.DoEvents();
						Cursor.Current = Cursors.WaitCursor;
						ApplyMassEdit(original, edited, similar,
							Convert.ToInt32(edited[MassHourFromParam]), Convert.ToInt32(edited[MassHourToParam]),
							Convert.ToInt32(edited[MassMinuteParam]),
							out tableErrors, out changed, out added);
					});

				if (form.ShowDialog(owner) != DialogResult.OK || tableErrors == null) return;

				// Список перечитываем целиком одним разом (как после массового создания): порядок строк тогда
				// задаёт процедура (время, потом дни недели). Поштучные события ObjectChanged/ObjectCloned
				// не годятся - каждое из них перестраивает узел, а новая строка вставляется в начало списка.
				// У пункта на строке нет ссылки на контейнер, поэтому сообщаем «изменился родитель - прайс-лист».
				if (changed.Count + added.Count > 0)
					OnParentChanged(this, po => po is Pricelist);

				if (tableErrors.Rows.Count > 0)
					Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ErrTmplGen),
						string.Format("Изменено тарифов: {0}, создано новых: {1}, не обработано: {2}",
							changed.Count, added.Count, tableErrors.Rows.Count), tableErrors);
				else
					UserMessage.ShowInformation(string.Format("Изменено тарифов: {0}, создано новых: {1}",
						changed.Count, added.Count));
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private static bool ValidateEditSimilar(Dictionary<string, object> original, Dictionary<string, object> edited)
		{
			string error = ValidateMassEdit(original, edited,
				Convert.ToInt32(edited[MassHourFromParam]), Convert.ToInt32(edited[MassHourToParam]),
				Convert.ToInt32(edited[MassMinuteParam]));
			if (error == null) return true;

			UserMessage.ShowExclamation(error);
			return false;
		}

		/// <summary>Возвращает UI-тип PassportForm, поэтому здесь, а не в ядре
		/// (тот же случай, что базовый PresentationObject.GetPassportForm, этап 0.1).</summary>
		public override PassportForm GetPassportForm(DataSet ds)
		{
			return new TariffPassport(this, ds);
		}
	}
}
