using FogSoft.Web.Components;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Карточка ролика — правила RollerPassportForm поверх паспорта сущности 20:
/// <list type="bullet">
/// <item>тип ролика не меняется у используемого ролика;</item>
/// <item>продолжительность не вводится — её даёт файл;</item>
/// <item>название не вводится, когда файл выбран (его даёт имя файла);</item>
/// <item>фирма недоступна у пустышки; «Для всех фирм» ставит только администратор, и тогда
/// фирма и предмет рекламы очищаются и недоступны;</item>
/// <item>фирма и предмет рекламы — либо оба, либо ни одного (кроме пустышки).</item>
/// </list>
/// Кнопка «Загрузить с диска» — <see cref="RollerFileField"/>: выбор файла в папке роликов
/// или загрузка с компьютера в неё. Проверка «выбран корневой предмет рекламы»
/// (IncorrectRolTypeSelected) не нужна: выбор идёт по сущности adverttypeChild, а её процедура
/// (AdvertTypeChildrenFlat) корневых не отдаёт.
/// </summary>
public static class RollerPassport
{
	private const string LoadButton = "btnLoad";
	private const string Name = "name";
	private const string FirmId = "firmID";
	private const string AdvertTypeId = "advertTypeID";

	public static PassportRules Rules(Roller roller, bool isNew)
	{
		bool isUsed = !isNew && roller.IsUsed;
		bool isAdmin = SecurityManager.LoggedUser?.IsAdmin == true;

		return new PassportRules
		{
			FieldDisabled = name => name switch
			{
				Roller.ParamNames.RolActionTypeID => isUsed,
				Roller.ParamNames.Duration => true,
				Name => HasFile(roller),
				FirmId => Flag(roller, Roller.ParamNames.IsMute) || Flag(roller, Roller.ParamNames.IsCommon),
				AdvertTypeId => Flag(roller, Roller.ParamNames.IsCommon),
				Roller.ParamNames.IsCommon => !isAdmin,
				_ => false,
			},
			Control = (name, refresh) => name == LoadButton
				? builder =>
				{
					builder.OpenComponent<RollerFileField>(0);
					builder.AddComponentParameter(1, nameof(RollerFileField.Roller), roller);
					builder.AddComponentParameter(2, nameof(RollerFileField.Changed), refresh);
					builder.CloseComponent();
				}
				: null,
			Validate = () => Validate(roller),
		};
	}

	/// <summary>RollerPassportForm.firmStatusUpdate + ApplyChanges.</summary>
	private static string? Validate(Roller roller)
	{
		// В десктопе галочка очищает поля сразу; здесь — перед записью, а до того недоступные
		// поля просто показывают прежнее значение (снял галочку — оно вернулось).
		if (Flag(roller, Roller.ParamNames.IsCommon))
		{
			roller[FirmId] = DBNull.Value;
			roller[AdvertTypeId] = DBNull.Value;
		}

		if (Flag(roller, Roller.ParamNames.IsMute) || IsSet(roller, FirmId) == IsSet(roller, AdvertTypeId))
			return null;

		return MessageAccessor.GetMessage("FirmAndAdvertTypeConstraint");
	}

	private static bool HasFile(Roller roller) => IsSet(roller, Roller.ParamNames.Path);

	private static bool IsSet(PresentationObject obj, string name) =>
		Convert.ToString(obj[name]) is { Length: > 0 };

	/// <summary>Галочка из параметров: у нового объекта значения может не быть вовсе.</summary>
	private static bool Flag(PresentationObject obj, string name) =>
		obj[name] is bool b ? b : bool.TryParse(Convert.ToString(obj[name]), out bool parsed) && parsed;
}
