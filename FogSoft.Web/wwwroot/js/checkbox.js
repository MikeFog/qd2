// Состояние «отмечено частично» у заголовочного чекбокса ObjectList
// (выделение строк чекбоксами для массового удаления).
//
// indeterminate — свойство DOM-элемента, а не HTML-атрибут: Blazor умеет
// биндить checked разметкой, а это состояние — только через JS после отрисовки.

export function setIndeterminate(el, value) {
    if (el)
        el.indeterminate = value;
}
