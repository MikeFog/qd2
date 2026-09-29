// Копирование текста в буфер обмена (справочник полей шаблонов документов).
export async function copy(text) {
    await navigator.clipboard.writeText(text);
}
