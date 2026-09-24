using System.Data;
using FogSoft.WinForm.Classes;
using Microsoft.AspNetCore.Components;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Показ готовой таблицы списком в модальном диалоге — веб-аналог десктопного
/// <c>Globals.ShowSimpleJournal(entity, caption, DataTable)</c>.
///
/// Зачем отдельный механизм. Массовая операция в десктопе почти всегда кончается
/// показом итогов: ошибки клонирования прайс-листа и смены типа оплаты, ошибки
/// удаления и изменения позиционирования, ошибки импорта фирм, ошибки добавления
/// рекламных окон — одиннадцать мест, и одно из них в самом фреймворке
/// (<c>SmartGrid.ShowDeleteErrors</c>, то есть любая сущность с
/// <c>isMassDeleteAllowed</c>). Веб-журнал так не умеет: он всегда грузит данные
/// сам по сущности. Поэтому показ готовой таблицы — не частный случай одного
/// экрана, а общая возможность.
///
/// Модальность — не веб-выдумка: для случая массового удаления десктоп и сам
/// показывает такой журнал модально (<c>ShowSimpleJournal(..., showModal: true)</c>).
///
/// <b>Сущность создаётся на лету и в базе не заводится.</b> Историческое решение
/// десктопа — держать в <c>iEntity</c> служебную строку «Ошибки» (157,
/// <c>ErrTmplGen</c>) с парой колонок на все случаи жизни — заставляет разные
/// операции показывать итоги в одних и тех же полях. Правильный путь —
/// <see cref="EntityManager.CreateVirtualEntity"/>: колонки объявляются в коде
/// под конкретную операцию (у ролика одни, у тарифа другие). Так уже сделано в
/// десктопе для массового удаления (<c>SmartGrid.cs</c>, сущность −5001) и для
/// итогов активации акции (<c>ActionOnMassmedia.WinForms.cs</c>).
/// Виртуальная сущность никуда не кэшируется и живёт только на время диалога.
///
/// Scoped — пользуется диалогами circuit.
/// </summary>
public sealed class TableDialog
{
	/// <summary>
	/// Идентификатор виртуальной сущности. Отрицательный, как и в десктопе
	/// (−5001 у массового удаления): с настоящими сущностями не пересекается, а
	/// по знаку сразу видно, что строки в <c>iEntity</c> за ним нет. Значение
	/// одно на все таблицы: сущность нигде не кэшируется и не ищется по id.
	/// </summary>
	private const int VirtualEntityId = -5100;

	private readonly DialogService _dialogs;

	public TableDialog(DialogService dialogs)
	{
		_dialogs = dialogs;
	}

	/// <summary>
	/// Показать таблицу. <paramref name="columns"/> — колонки именно этой
	/// операции: имя должно совпадать с колонкой <paramref name="table"/>,
	/// подпись увидит пользователь. Колонку, которой нет в данных, список
	/// пропустит сам — то же правило, что и у обычного журнала.
	/// </summary>
	/// <param name="caption">Заголовок диалога, он же имя виртуальной сущности.</param>
	public async Task ShowAsync(string caption, DataTable table, params Entity.Attribute[] columns)
	{
		if (table == null || table.Rows.Count == 0)
			return;

		Entity entity = EntityManager.CreateVirtualEntity(
			VirtualEntityId, caption, "VirtualTable", pkColumn: string.Empty, attributes: columns);

		RenderFragment body = builder =>
		{
			builder.OpenComponent<Components.ObjectList>(0);
			builder.AddComponentParameter(1, nameof(Components.ObjectList.Entity), entity);
			builder.AddComponentParameter(2, nameof(Components.ObjectList.Data), table);
			builder.AddComponentParameter(3, nameof(Components.ObjectList.ReadOnly), true);
			builder.CloseComponent();
		};

		await _dialogs.ShowAsync(caption, body, okText: Tr.T("Закрыть"));
	}
}
