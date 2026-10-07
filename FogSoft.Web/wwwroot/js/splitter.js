// Перетаскиваемая граница между панелями (PaneSplitter.razor): дерево слева, список справа.
//
// Всё перетаскивание — в браузере: движения мыши на сервер не ходят (правило веба 2026-09-18).
// Ширина панели — CSS-переменная --pane-width на соседнем элементе: обычно слева от ручки, а у
// ручки правой панели (rightPane) — справа, тогда движение вправо панель сужает. Узкая раскладка
// (колонкой) переменную не использует. Ширина запоминается в localStorage этого браузера по ключу
// экрана, двойной щелчок по ручке возвращает исходную ширину, стрелки — шаг 16 px.

const MIN = 200;          // px
const MAX_RATIO = 0.6;    // доля ширины контейнера
const STEP = 16;          // px на нажатие стрелки

const states = new WeakMap();

function read(key) {
    try { return parseInt(localStorage.getItem(key), 10); } catch { return NaN; }
}

function write(key, width) {
    try {
        if (width == null) localStorage.removeItem(key);
        else localStorage.setItem(key, String(width));
    } catch { /* хранилище недоступно — ширина просто не запомнится */ }
}

export function attach(handle, storageKey, rightPane) {
    const pane = rightPane ? handle?.nextElementSibling : handle?.previousElementSibling;
    if (!pane) return;

    let state = states.get(handle);
    if (!state) {
        // sign: куда растёт панель при движении мыши вправо.
        state = { key: "", pane, sign: rightPane ? -1 : 1 };
        states.set(handle, state);
        listen(handle, state);
    }

    // Тот же компонент на другом экране (другой ключ) — своя ширина или исходная.
    state.key = "pane-width:" + storageKey;
    const saved = read(state.key);
    if (saved > 0) apply(handle, state, clamp(handle, saved));
    else reset(handle, state, false);
}

function clamp(handle, width) {
    const max = handle.parentElement.clientWidth * MAX_RATIO;
    return Math.round(Math.max(MIN, Math.min(width, max)));
}

function apply(handle, state, width) {
    state.pane.style.setProperty("--pane-width", width + "px");
    handle.setAttribute("aria-valuenow", String(width));
}

function reset(handle, state, forget) {
    state.pane.style.removeProperty("--pane-width");
    handle.removeAttribute("aria-valuenow");
    if (forget) write(state.key, null);
}

function currentWidth(state) {
    return Math.round(state.pane.getBoundingClientRect().width);
}

function listen(handle, state) {
    let startX = 0, startWidth = 0;

    const move = e => apply(handle, state, clamp(handle, startWidth + state.sign * (e.clientX - startX)));

    const up = e => {
        try { handle.releasePointerCapture(e.pointerId); } catch { /* захвата не было */ }
        handle.removeEventListener("pointermove", move);
        handle.removeEventListener("pointerup", up);
        handle.removeEventListener("pointercancel", up);
        document.body.classList.remove("pane-resizing");
        write(state.key, currentWidth(state));
    };

    handle.addEventListener("pointerdown", e => {
        if (e.button !== 0) return;
        e.preventDefault();
        startX = e.clientX;
        startWidth = currentWidth(state);
        // Захват — чтобы тянуть и за пределами ручки; без него (указатель, который захватить нельзя)
        // работает в пределах ручки, но не ломается.
        try { handle.setPointerCapture(e.pointerId); } catch { /* без захвата */ }
        handle.addEventListener("pointermove", move);
        handle.addEventListener("pointerup", up);
        handle.addEventListener("pointercancel", up);
        document.body.classList.add("pane-resizing");
    });

    handle.addEventListener("dblclick", () => reset(handle, state, true));

    handle.addEventListener("keydown", e => {
        if (e.key !== "ArrowLeft" && e.key !== "ArrowRight") return;
        e.preventDefault();
        const width = clamp(handle, currentWidth(state) + state.sign * (e.key === "ArrowRight" ? STEP : -STEP));
        apply(handle, state, width);
        write(state.key, width);
    });
}
