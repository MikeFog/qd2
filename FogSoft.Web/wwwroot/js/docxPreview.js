// Просмотр документа Word в окне приложения (docx-preview), docs/tasks/web-reports.md §6.1.
// Картинка приближённая: её задача — проверить документ до скачивания, а не заменить Word.
// Библиотеки грузятся при первом просмотре, а не со страницей.
const libraries = ['lib/jszip/jszip.min.js', 'lib/docx-preview/docx-preview.min.js'];
let loading;
let counter = 0;

function loadScript(src) {
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = src;
        script.onload = resolve;
        script.onerror = () => reject(new Error('Script not loaded: ' + src));
        document.head.appendChild(script);
    });
}

function ensureLibraries() {
    // jszip должна загрузиться раньше docx-preview — по очереди.
    loading ??= libraries.reduce((chain, src) => chain.then(() => loadScript(src)), Promise.resolve());
    return loading;
}

export async function render(container, streamRef) {
    await ensureLibraries();
    const buffer = await streamRef.arrayBuffer();
    container.innerHTML = '';
    // Свой префикс классов на каждый показ: стили docx-preview общие для всей страницы,
    // иначе второй документ перекрасил бы первый (шрифты, отступы).
    await docx.renderAsync(buffer, container, null, { inWrapper: true, className: 'docx' + (++counter) });
}
