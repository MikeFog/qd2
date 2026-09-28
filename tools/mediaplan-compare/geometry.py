"""Сравнение вывода geometry.ps1: видимые ширины колонок и высоты строк двух папок."""
import sys, collections
lines = [l.rstrip('\n').split('|') for l in open(sys.argv[1], encoding='utf-8')]
dirs = []
data = collections.defaultdict(dict)
for d, f, s, k, v in lines:
    if d not in dirs:
        dirs.append(d)
    data[d][(f, s, k)] = float(v.replace(',', '.'))
a, b = data[dirs[0]], data[dirs[1]]
for kind in ('C', 'R'):
    diffs = []
    for key, va in a.items():
        if not key[2].startswith(kind):
            continue
        vb = b.get(key)
        if vb is None:
            continue
        diffs.append((abs(va - vb), va, vb, key))
    if not diffs:
        continue
    exact = sum(1 for d in diffs if d[0] < 0.01)
    limit = float(sys.argv[2]) if len(sys.argv) > 2 else 1.0
    big = sorted((d for d in diffs if d[0] > limit), reverse=True)
    name = 'колонки' if kind == 'C' else 'строки'
    print(f'{name}: {len(diffs)}, совпало {exact}, среднее отклонение {sum(d[0] for d in diffs) / len(diffs):.2f} пт, '
          f'больше порога {len(big)}')
    agg = collections.Counter((round(d[1], 2), round(d[2], 2)) for d in big)
    for (va, vb), n in agg.most_common(12):
        ex = next(d[3] for d in big if (round(d[1], 2), round(d[2], 2)) == (va, vb))
        print(f'   {n:4} x  {va} -> {vb}   напр. {ex}')
