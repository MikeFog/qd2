// Отдача файла из circuit браузеру (ObjectList.ExportToExcelAsync).
//
// Blazor Server сам файл скачать не умеет: содержимое собрано на сервере, а
// сохранить его должен браузер. Поток приходит как DotNetStreamReference —
// кусками по SignalR, без промежуточного адреса на сервере и без контроллера:
// состояние списка живёт в circuit, а не в запросе.

export async function downloadStream(fileName, contentType, streamRef) {
    const buffer = await streamRef.arrayBuffer();
    const url = URL.createObjectURL(new Blob([buffer], { type: contentType }));

    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();

    // Скачивание стартует синхронно после click; адрес нужен ещё недолго.
    setTimeout(() => URL.revokeObjectURL(url), 10000);
}
