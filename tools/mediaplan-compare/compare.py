"""Сверка xlsx медиаплана двух папок: значения, стили, ширины колонок, высоты строк, листы, картинки.

python compare.py <папка_эталон> <папка_проверяемая> [--loose]

--loose — для сравнения Excel (COM) с OpenXml: рамки сверяются по видимым граням
(Excel хранит общую рамку у одной из соседних ячеек), шрифт пустых ячеек не
сравнивается, ширины и высоты — не здесь, а через geometry.ps1 + geometry.py
(в пунктах, как их показывает Excel), картинки — по числу, ячейке и размеру.
"""
import sys, os, glob, zipfile, re
import openpyxl
from openpyxl.utils import get_column_letter

WIDTH_TOL = 0.15    # относительный допуск ширины колонки
HEIGHT_TOL = 0.15   # относительный допуск высоты строки

def edges(ws, c):
    """Видимые грани ячейки: своя рамка или рамка соседа с той же стороны."""
    def st(side):
        return side.style if side is not None else None
    b = c.border
    left = st(b.left) or (c.column > 1 and st(ws.cell(c.row, c.column - 1).border.right))
    right = st(b.right) or st(ws.cell(c.row, c.column + 1).border.left)
    top = st(b.top) or (c.row > 1 and st(ws.cell(c.row - 1, c.column).border.bottom))
    bottom = st(b.bottom) or st(ws.cell(c.row + 1, c.column).border.top)
    return tuple(x or None for x in (left, right, top, bottom))

def cell_sig(ws, c, loose):
    f, fl, a = c.font, c.fill, c.alignment
    b = c.border
    borders = edges(ws, c) if loose else tuple((s.style if s is not None else None) for s in (b.left, b.right, b.top, b.bottom))
    # Шрифт пустой ячейки не виден; «Обычный» шрифт у Excel зависит от машины.
    font = None if loose and c.value is None else (f.name, f.sz, f.b, f.i)
    return (
        c.value,
        c.number_format,
        font,
        (fl.fill_type, fl.fgColor.rgb if fl.fgColor is not None and fl.fill_type else None),
        borders,
        (a.textRotation, a.wrap_text),
    )

def sheet_sig(ws, loose):
    cells = {}
    # Список ячеек снимаем до сверки граней: ws.cell() для соседей создаёт новые.
    existing = [c for row in ws.iter_rows() for c in row]
    for c in existing:
            sig = cell_sig(ws, c, loose)
            if sig[0] is None and sig[3][0] is None and not any(sig[4]):
                continue
            cells[c.coordinate] = sig
    widths = {}
    for k, v in ws.column_dimensions.items():
        if v.width and v.customWidth:
            for i in range(v.min or 0, (v.max or 0) + 1):
                widths[get_column_letter(i)] = round(v.width, 2)
    heights = {r: round(d.height, 2) for r, d in ws.row_dimensions.items() if d.height}
    return {'cells': cells, 'widths': widths, 'heights': heights,
            'orientation': ws.page_setup.orientation, 'dims': ws.dimensions}

def images(path):
    """[(лист, ячейка привязки, ширина EMU, высота EMU)] из drawing*.xml."""
    z = zipfile.ZipFile(path)
    out = []
    for n in sorted(z.namelist()):
        if re.match(r'xl/drawings/drawing\d+\.xml$', n):
            s = z.read(n).decode('utf-8')
            for m in re.finditer(r'<xdr:from><xdr:col>(\d+)</xdr:col>.*?<xdr:row>(\d+)</xdr:row>.*?<a:ext cx="(\d+)" cy="(\d+)"', s, re.S):
                out.append((n, int(m.group(1)), int(m.group(2)), int(m.group(3)), int(m.group(4))))
    return out

def near(a, b, tol):
    return a is not None and b is not None and abs(a - b) <= tol * max(abs(a), abs(b), 1)

def compare(old, new, loose, stats):
    wo, wn = openpyxl.load_workbook(old), openpyxl.load_workbook(new)
    diffs = []
    if wo.sheetnames != wn.sheetnames:
        return [f'sheets: {wo.sheetnames} != {wn.sheetnames}']
    for name in wo.sheetnames:
        so, sn = sheet_sig(wo[name], loose), sheet_sig(wn[name], loose)
        if so['orientation'] != sn['orientation']:
            diffs.append(f'[{name}] orientation: {so["orientation"]} != {sn["orientation"]}')
        if not loose and so['dims'] != sn['dims']:
            diffs.append(f'[{name}] dims: {so["dims"]} != {sn["dims"]}')
        # В --loose ширины/высоты в файле не сравниваем: их единицы зависят от
        # шрифта «Обычный»; видимую геометрию проверяет geometry.ps1/geometry.py.
        for kind, tol in (() if loose else (('widths', WIDTH_TOL), ('heights', HEIGHT_TOL))):
            keys = sorted(set(so[kind]) | set(sn[kind]), key=str)
            bad = []
            for k in keys:
                a, b = so[kind].get(k), sn[kind].get(k)
                if a == b:
                    continue
                if loose and near(a, b, tol):
                    stats.setdefault(kind, []).append(abs(a - b) / max(a, b))
                    continue
                bad.append((k, a, b))
            if bad:
                diffs.append(f'[{name}] {kind}: {bad[:8]}' + (f' ... всего {len(bad)}' if len(bad) > 8 else ''))
        keys = sorted(set(so['cells']) | set(sn['cells']))
        cd = [(k, so['cells'].get(k), sn['cells'].get(k)) for k in keys if so['cells'].get(k) != sn['cells'].get(k)]
        for k, a, b in cd[:10]:
            diffs.append(f'[{name}] {k}: {a} != {b}')
        if len(cd) > 10:
            diffs.append(f'[{name}] ... ещё {len(cd) - 10} ячеек')
    io, inew = images(old), images(new)
    if loose:
        ia = [(c, r, cx, cy) for _, c, r, cx, cy in io]
        ib = [(c, r, cx, cy) for _, c, r, cx, cy in inew]
        # Excel через COM ставит картинку по координате в пунктах с округлением
        # вниз и привязывает к колонке левее со смещением почти на её ширину.
        if len(ia) != len(ib) or any(abs(x[0] - y[0]) > 1 or x[1] != y[1] or not near(x[2], y[2], 0.02) or not near(x[3], y[3], 0.02) for x, y in zip(ia, ib)):
            diffs.append(f'images: {ia} != {ib}')
    return diffs

def main(old_dir, new_dir, loose):
    names = sorted({os.path.basename(p) for p in glob.glob(os.path.join(old_dir, '*')) + glob.glob(os.path.join(new_dir, '*'))})
    total_cells, bad, stats = 0, 0, {}
    for n in names:
        po, pn = os.path.join(old_dir, n), os.path.join(new_dir, n)
        if not (os.path.exists(po) and os.path.exists(pn)):
            print(f'MISSING {n}: old={os.path.exists(po)} new={os.path.exists(pn)}')
            bad += 1
            continue
        if not n.endswith('.xlsx'):
            print(f'SAME    {n}')
            continue
        d = compare(po, pn, loose, stats)
        wb = openpyxl.load_workbook(po)
        cells = sum(len(sheet_sig(wb[s], False)['cells']) for s in wb.sheetnames)
        total_cells += cells
        if d:
            bad += 1
            print(f'DIFF    {n}')
            for line in d:
                print('        ' + line)
        else:
            print(f'SAME    {n}  (листов {len(wb.sheetnames)}, ячеек {cells})')
    print(f'итого: файлов {len(names)}, с расхождениями {bad}, сверено ячеек {total_cells}')
    for kind, v in stats.items():
        print(f'{kind} в допуске: {len(v)}, среднее отклонение {sum(v) / len(v):.1%}, максимум {max(v):.1%}')

if __name__ == '__main__':
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    main(args[0], args[1], '--loose' in sys.argv)
