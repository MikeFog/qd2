// Прослушивание файла ролика (RollerPlayer.razor).
//
// Файл лежит на сервере, а адреса, по которому браузер мог бы его взять, нет: в вебе нет
// HTTP-эндпоинтов, пользователь живёт на circuit. Поэтому содержимое приходит потоком
// DotNetStreamReference (как выгрузка в download.js), превращается в blob-адрес и
// отдаётся обычному <audio controls>: перемотка, громкость и пауза — его собственные.

export async function play(audio, streamRef, contentType) {
    const buffer = await streamRef.arrayBuffer();
    release(audio);
    audio.src = URL.createObjectURL(new Blob([buffer], { type: contentType }));
    await audio.play();
}

export function stop(audio) {
    audio.pause();
    release(audio);
}

function release(audio) {
    if (audio.src && audio.src.startsWith("blob:")) {
        URL.revokeObjectURL(audio.src);
        audio.removeAttribute("src");
        audio.load();
    }
}
