// Сохранение файлов через диалог браузера (File System Access API: Chrome, Edge).
// Решение владельца 2026-09-25: вместо десктопных «папок из настроек» (костыль RDP) —
// обычный диалог, который показывает браузер на компьютере пользователя.
//
// Диалог браузер открывает только в ответ на действие пользователя, поэтому окно выбора
// вызывается первым, до долгой подготовки файлов: папку выбрали — потом пишем в неё
// сколько угодно файлов без новых вопросов.

let directory = null;
let fileHandle = null;

export function isSupported() {
    return "showDirectoryPicker" in window && "showSaveFilePicker" in window;
}

// null — пользователь закрыл окно выбора.
function cancelled(e) {
    return e && e.name === "AbortError";
}

// Выбор папки. true — выбрана, false — окно закрыли.
export async function pickDirectory() {
    try {
        directory = await window.showDirectoryPicker({ id: "qd2-export", mode: "readwrite" });
        return true;
    } catch (e) {
        if (cancelled(e)) return false;
        throw e;
    }
}

export async function writeToDirectory(name, streamRef) {
    const handle = await directory.getFileHandle(name, { create: true });
    await write(handle, streamRef);
}

// «Сохранить как» одного файла: выбор места. true — выбрано, false — окно закрыли.
export async function pickFile(suggestedName) {
    try {
        fileHandle = await window.showSaveFilePicker({ id: "qd2-export", suggestedName: suggestedName });
        return true;
    } catch (e) {
        if (cancelled(e)) return false;
        throw e;
    }
}

export async function writeFile(streamRef) {
    await write(fileHandle, streamRef);
}

async function write(handle, streamRef) {
    const buffer = await streamRef.arrayBuffer();
    const writable = await handle.createWritable();
    await writable.write(buffer);
    await writable.close();
}
