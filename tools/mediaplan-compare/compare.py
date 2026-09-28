"""Сверка xlsx медиаплана «было/стало»: значения, стили, ширины колонок, листы, картинки."""
import sys, os, glob
import openpyxl

def cell_sig(c):
    f, fl, b, a = c.font, c.fill, c.border, c.alignment
    return (
        c.value,
        c.number_format,
        (f.name, f.sz, f.b, f.i),
        (fl.fill_type, fl.fgColor.rgb if fl.fgColor is not None else None),
        tuple((s.style if s is not None else None) for s in (b.left, b.right, b.top, b.bottom)),
        (a.textRotation, a.horizontal, a.vertical, a.wrap_text),
    )

def sheet_sig(ws):
    cells = {}
    for row in ws.iter_rows():
        for c in row:
            sig = cell_sig(c)
            if sig[0] is None and sig[3][0] is None and not any(sig[4]):
                continue
            cells[c.coordinate] = sig
    widths = {k: round(v.width, 2) for k, v in ws.column_dimensions.items() if v.width}
    return {
        'cells': cells,
        'widths': widths,
        'orientation': ws.page_setup.orientation,
        'images': len(getattr(ws, '_images', [])),
        'dims': ws.dimensions,
    }

def compare(old, new):
    wo, wn = openpyxl.load_workbook(old), openpyxl.load_workbook(new)
    diffs = []
    if wo.sheetnames != wn.sheetnames:
        diffs.append(f'sheets: {wo.sheetnames} != {wn.sheetnames}')
        return diffs
    for name in wo.sheetnames:
        so, sn = sheet_sig(wo[name]), sheet_sig(wn[name])
        for k in ('orientation', 'images', 'dims'):
            if so[k] != sn[k]:
                diffs.append(f'[{name}] {k}: {so[k]} != {sn[k]}')
        if so['widths'] != sn['widths']:
            keys = sorted(set(so['widths']) | set(sn['widths']))
            d = [(k, so['widths'].get(k), sn['widths'].get(k)) for k in keys if so['widths'].get(k) != sn['widths'].get(k)]
            diffs.append(f'[{name}] widths: {d[:8]}')
        keys = sorted(set(so['cells']) | set(sn['cells']))
        cd = [(k, so['cells'].get(k), sn['cells'].get(k)) for k in keys if so['cells'].get(k) != sn['cells'].get(k)]
        for k, a, b in cd[:10]:
            diffs.append(f'[{name}] {k}: {a} != {b}')
        if len(cd) > 10:
            diffs.append(f'[{name}] ... ещё {len(cd) - 10} ячеек')
    return diffs

def main(old_dir, new_dir):
    names = sorted({os.path.basename(p) for p in glob.glob(os.path.join(old_dir, '*')) + glob.glob(os.path.join(new_dir, '*'))})
    total_cells = 0
    bad = 0
    for n in names:
        po, pn = os.path.join(old_dir, n), os.path.join(new_dir, n)
        if not (os.path.exists(po) and os.path.exists(pn)):
            print(f'MISSING {n}: old={os.path.exists(po)} new={os.path.exists(pn)}')
            bad += 1
            continue
        if not n.endswith('.xlsx'):
            print(f'SAME    {n}')
            continue
        d = compare(po, pn)
        wb = openpyxl.load_workbook(po)
        cells = sum(len(sheet_sig(wb[s])['cells']) for s in wb.sheetnames)
        total_cells += cells
        if d:
            bad += 1
            print(f'DIFF    {n}')
            for line in d:
                print('        ' + line)
        else:
            print(f'SAME    {n}  (листов {len(wb.sheetnames)}, ячеек {cells})')
    print(f'итого: файлов {len(names)}, с расхождениями {bad}, сверено ячеек {total_cells}')

if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
