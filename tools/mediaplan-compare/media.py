import zipfile, glob, os, sys, hashlib
base = sys.argv[1]
for v in ('old', 'new'):
    tot = 0
    for p in sorted(glob.glob(os.path.join(base, v, '*.xlsx'))):
        z = zipfile.ZipFile(p)
        m = [n for n in z.namelist() if n.startswith('xl/media/')]
        if m:
            print(v, os.path.basename(p), [(n, hashlib.md5(z.read(n)).hexdigest()[:8]) for n in m])
        tot += len(m)
    print(v, 'media total', tot)
