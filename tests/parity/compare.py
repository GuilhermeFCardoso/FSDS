import csv, sys

js = list(csv.reader(open(sys.argv[1], encoding='utf-8-sig')))
cs = list(csv.reader(open(sys.argv[2], encoding='utf-8-sig')))
names = ['lat', 'lon', 'z', 'vn', 've', 'vd', 'qw', 'qx', 'qy', 'qz', 'p', 'q', 'r', 'n', 'ab', 'fuel', 'gear', 'flaps', 'crash']
print('linhas', len(js), len(cs))
if len(js) != len(cs):
    sys.exit('numero de linhas diferente')
worst = {}
first = {}
for a, b in zip(js, cs):
    if a[0] != b[0] or a[1] != b[1]:
        sys.exit(f'desalinhado: {a[:2]} x {b[:2]}')
    case = a[0]
    w = worst.setdefault(case, {})
    for i, n in enumerate(names):
        x, y = float(a[i + 2]), float(b[i + 2])
        d = abs(x - y)
        if d > w.get(n, (0,))[0]:
            w[n] = (d, a[1])
        if d > 1e-6 * max(1, abs(x)) and case not in first:
            first[case] = (a[1], n, x, y)
for case, w in worst.items():
    top = sorted(w.items(), key=lambda kv: -kv[1][0])[:4]
    print(f'{case:12} maiores:', ', '.join(f'{n}={v[0]:.3g}@{v[1]}s' for n, v in top) or 'zero', '| primeira divergencia:', first.get(case, 'nenhuma'))
last = {}
for a in js:
    last[a[0]] = a
for case, a in last.items():
    print(f'{case:12} final JS: z={float(a[4]):.1f} n={float(a[15]):.3f} crash={a[20]}')
