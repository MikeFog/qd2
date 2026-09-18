// Размещение всплывающего меню действий (ActionMenuHost.razor).
//
// Единственное, чего Blazor не умеет без JS: узнать размеры элементов и окна.
// Меню ставится от кнопки «⋯» (по её правому краю, под ней) или от точки
// щелчка правой кнопкой и сдвигается так, чтобы не вылезти за окно: у нижних
// строк — вверх, у правого края — влево.

const GAP = 4;
const MARGIN = 8;

export function place(menu, x, y, fromButton) {
    if (!menu) return;

    let left = x, top = y, anchorTop = y;
    const anchor = fromButton ? document.activeElement : null;
    if (anchor && anchor !== document.body) {
        const r = anchor.getBoundingClientRect();
        left = r.right - menu.offsetWidth;
        top = r.bottom + GAP;
        anchorTop = r.top - GAP;
    }

    const vw = window.innerWidth, vh = window.innerHeight;
    if (left + menu.offsetWidth > vw - MARGIN) left = vw - MARGIN - menu.offsetWidth;
    if (left < MARGIN) left = MARGIN;
    if (top + menu.offsetHeight > vh - MARGIN) top = Math.max(MARGIN, anchorTop - menu.offsetHeight);

    menu.style.left = left + "px";
    menu.style.top = top + "px";
    menu.style.visibility = "visible";

    // Подменю у правого края окна раскрывается влево.
    menu.classList.toggle("am-flip", left + menu.offsetWidth * 2 > vw - MARGIN);

    if (!menu.dataset.keys) {
        menu.dataset.keys = "1";
        menu.addEventListener("keydown", e => move(menu, e));
    }
    focusItem(menu, 0);
}

function items(menu) {
    return Array.from(menu.querySelectorAll(":scope > .am-item:not(.am-off)"));
}

function focusItem(menu, index) {
    const list = items(menu);
    if (list.length) list[(index + list.length) % list.length].focus();
    else menu.focus();
}

// Стрелки вверх/вниз ходят по доступным пунктам верхнего уровня.
function move(menu, e) {
    if (e.key !== "ArrowDown" && e.key !== "ArrowUp") return;
    e.preventDefault();
    const list = items(menu);
    const i = list.indexOf(document.activeElement);
    focusItem(menu, e.key === "ArrowDown" ? i + 1 : (i < 0 ? -1 : i - 1));
}
