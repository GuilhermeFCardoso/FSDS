(function (root) {
const lin = (h, a = 1) => [0, 1, 2].map((i) => Math.pow(((h >> (16 - 8 * i)) & 255) / 255, 2.2)).concat(a);
const sub = (a, b) => [a[0] - b[0], a[1] - b[1], a[2] - b[2]];
const add = (a, b) => [a[0] + b[0], a[1] + b[1], a[2] + b[2]];
const scl = (a, k) => [a[0] * k, a[1] * k, a[2] * k];
const dot = (a, b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
const crs = (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];
const nrm = (a) => { const l = Math.hypot(...a) || 1; return [a[0] / l, a[1] / l, a[2] / l]; };
const avg = (ps) => scl(ps.reduce((s, p) => add(s, p), [0, 0, 0]), 1 / ps.length);
const lerp = (a, b, t) => a + (b - a) * t;
const sgp = (v, e) => Math.sign(v) * Math.pow(Math.abs(v), e);

class Model {
  constructor() { this.layers = {}; this.pivots = {}; this.groups = {}; this.hinges = {}; }
  mat(name, color, metallic = 0.2, rough = 0.5, alpha = 1, unlit = false) { this.groups[name] = { color: lin(color, alpha), metallic, rough, name, alpha, unlit }; return name; }
  grp(layer, mat) {
    const L = (this.layers[layer] ||= {});
    return (L[mat] ||= { v: [], n: [], c: [], i: [], ...this.groups[mat] });
  }
  clr(g, c) { return c == null ? g.color : typeof c === 'number' ? lin(c, g.alpha) : c; }
  vert(g, p, n, c) { g.v.push(p[0], p[1], p[2]); g.n.push(n[0], n[1], n[2]); g.c.push(c[0], c[1], c[2], c[3]); return g.v.length / 3 - 1; }
  tri(g, a, b, c, ref, col) {
    let n = crs(sub(b, a), sub(c, a));
    if (ref && dot(n, sub(avg([a, b, c]), ref)) < 0) { [b, c] = [c, b]; n = scl(n, -1); }
    n = nrm(n);
    const cc = this.clr(g, col), o = g.v.length / 3;
    for (const p of [a, b, c]) this.vert(g, p, n, cc);
    g.i.push(o, o + 1, o + 2);
  }
  quad(g, a, b, c, d, ref, col) { this.tri(g, a, b, c, ref, col); this.tri(g, a, c, d, ref, col); }
  hexa(g, p, col) {
    const ref = avg(p);
    for (const q of [[0, 1, 2, 3], [4, 5, 6, 7], [0, 1, 5, 4], [1, 2, 6, 5], [2, 3, 7, 6], [3, 0, 4, 7]]) this.quad(g, p[q[0]], p[q[1]], p[q[2]], p[q[3]], ref, col);
  }
  box(g, c, s, col) {
    const [hx, hy, hz] = s.map((x) => x / 2), p = [];
    for (const [sx, sy, sz] of [[-1, -1, -1], [1, -1, -1], [1, 1, -1], [-1, 1, -1], [-1, -1, 1], [1, -1, 1], [1, 1, 1], [-1, 1, 1]])
      p.push([c[0] + sx * hx, c[1] + sy * hy, c[2] + sz * hz]);
    this.hexa(g, p, col);
  }
  bar(g, a, b, t, t2 = t, up = [0, 0, 1], col) {
    const d = nrm(sub(b, a));
    if (Math.abs(dot(d, up)) > 0.95) up = [1, 0, 0];
    const u = scl(nrm(crs(d, up)), t / 2), w = scl(nrm(crs(d, u)), t2 / 2), p = [];
    for (const e of [a, b]) for (const [s1, s2] of [[-1, -1], [1, -1], [1, 1], [-1, 1]]) p.push(add(e, add(scl(u, s1), scl(w, s2))));
    this.hexa(g, p, col);
  }
  disc(g, c, ry, rz, col, dir = 1, N = 20) {
    const ref = add(c, [-dir, 0, 0]);
    for (let k = 0; k < N; k++) {
      const a0 = 2 * Math.PI * k / N, a1 = 2 * Math.PI * (k + 1) / N;
      this.tri(g, c, add(c, [0, ry * Math.cos(a0), rz * Math.sin(a0)]), add(c, [0, ry * Math.cos(a1), rz * Math.sin(a1)]), ref, col);
    }
  }

  grid(g, R, o = {}) {
    const S = R.length, N = R[0].length, nk = o.open ? N - 1 : N;
    const cen = R.map(avg), acc = R.map((r) => r.map(() => [0, 0, 0])), faces = [];
    for (let s = 0; s < S - 1; s++) for (let k = 0; k < nk; k++) {
      const k2 = (k + 1) % N, p0 = R[s][k], p1 = R[s][k2], p2 = R[s + 1][k2], p3 = R[s + 1][k];
      let n = crs(sub(p2, p0), sub(p3, p1));
      const ref = o.cen ? o.cen(s) : scl(add(cen[s], cen[s + 1]), 0.5);
      const flip = dot(n, sub(avg([p0, p1, p2, p3]), ref)) < 0;
      if (flip) n = scl(n, -1);
      for (const [a, b] of [[s, k], [s, k2], [s + 1, k2], [s + 1, k]]) acc[a][b] = add(acc[a][b], n);
      faces.push([s, k, k2, flip]);
    }
    for (const [s, k, k2, flip] of faces) {
      const ids = [[s, k], [s, k2], [s + 1, k2], [s + 1, k]];
      const c = this.clr(g, o.paint ? o.paint(s, k, avg(ids.map(([a, b]) => R[a][b]))) : null);
      const base = g.v.length / 3;
      for (const [a, b] of ids) this.vert(g, R[a][b], nrm(acc[a][b]), c);
      if (flip) g.i.push(base, base + 2, base + 1, base, base + 3, base + 2); else g.i.push(base, base + 1, base + 2, base, base + 2, base + 3);
    }
    if (!o.open) for (const [end, other] of [[0, 1], [S - 1, S - 2]]) {
      if (!(end === 0 ? o.capStart : o.capEnd)) continue;
      for (let k = 0; k < N; k++) this.tri(g, cen[end], R[end][k], R[end][(k + 1) % N], cen[other], o.capColor);
    }
  }

  secPoint(secs, x, a) {
    let i = 0;
    for (; i < secs.length - 2; i++) if ((x - secs[i].x) * (x - secs[i + 1].x) <= 0) break;
    const A = secs[i], B = secs[i + 1], t = A.x === B.x ? 0 : clamp01((x - A.x) / (B.x - A.x));
    const f = (k, d = 0) => lerp(A[k] ?? d, B[k] ?? d, t);
    const e = 2 / f('e', 2), w = f('w'), h = f('h'), hb = lerp(A.hb ?? A.h, B.hb ?? B.h, t), cy = f('y'), cz = f('z');
    const ca = Math.cos(a), sa = Math.sin(a);
    return { p: [x, cy + w * sgp(ca, e), cz + (sa >= 0 ? h : hb) * sgp(sa, e)], c: [x, cy, cz] };
  }
  decal(g, secs, o) {
    const lo = Math.min(o.x0, o.x1), hi = Math.max(o.x0, o.x1);
    const xs = [lo, ...secs.map((s) => s.x).filter((x) => x > lo + 1e-6 && x < hi - 1e-6), hi].sort((a, b) => a - b);
    const na = Math.max(2, Math.ceil(Math.abs(o.a1 - o.a0) / 0.14) + 1);
    const cens = [];
    const R = xs.map((x) => {
      const row = [];
      for (let j = 0; j < na; j++) {
        const sp = this.secPoint(secs, x, lerp(o.a0, o.a1, j / (na - 1)));
        const out = nrm(sub(sp.p, sp.c));
        row.push(add(sp.p, scl(out, o.off ?? 0.006)));
        if (j === 0) cens.push(sp.c);
      }
      return row;
    });
    this.grid(g, R, { open: true, cen: (s) => scl(add(cens[s], cens[s + 1]), 0.5), paint: o.col != null ? () => o.col : null });
  }

  loft(g, secs, N = 24, o = {}) {
    const R = secs.map((s) => Array.from({ length: N }, (_, k) => {
      const a = 2 * Math.PI * k / N, e = 2 / (s.e ?? 2);
      return [s.x, (s.y ?? 0) + s.w * sgp(Math.cos(a), e), s.z + (Math.sin(a) >= 0 ? s.h : (s.hb ?? s.h)) * sgp(Math.sin(a), e)];
    }));
    const paint = o.paint && ((s, k, mid) => o.paint(mid[0], 2 * Math.PI * (k + 0.5) / N, s, k));
    this.grid(g, R, { ...o, paint, capStart: o.capStart ?? secs[0].w > 0.05, capEnd: o.capEnd ?? secs[secs.length - 1].w > 0.05 });
  }
  lathe(g, c, axis, prof, N = 24, o = {}) {
    const ax = { x: [[1, 0, 0], [0, 1, 0], [0, 0, 1]], y: [[0, 1, 0], [1, 0, 0], [0, 0, 1]], z: [[0, 0, 1], [1, 0, 0], [0, 1, 0]] }[axis];
    const R = prof.map(([t, r]) => Array.from({ length: N }, (_, k) => {
      const a = 2 * Math.PI * k / N;
      return add(add(c, scl(ax[0], t)), add(scl(ax[1], r * Math.cos(a)), scl(ax[2], r * Math.sin(a))));
    }));
    this.grid(g, R, { ...o, capStart: o.capStart ?? prof[0][1] > 0.01, capEnd: o.capEnd ?? prof[prof.length - 1][1] > 0.01 });
  }
  cyl(g, c, r, len, axis = 'y', N = 18, o = {}) { this.lathe(g, c, axis, [[-len / 2, r], [len / 2, r]], N, o); }

  foil(g, st, o = {}) {
    const NP = o.np ?? 12;
    const lo = o.from ?? 0;
    const xs = Array.from({ length: NP + 1 }, (_, i) => lo + (1 - lo) * 0.5 * (1 + Math.cos(Math.PI * i / NP)));
    const R = st.map((s) => {
      const t = s.t ?? 0.12, cam = s.cam ?? 0, tw = s.tw ?? 0, rl = s.r ?? 0, p = 0.4;
      const prof = (x, sg) => {
        const yt = 5 * t * (0.2969 * Math.sqrt(x) - 0.126 * x - 0.3516 * x * x + 0.2843 * x ** 3 - 0.1015 * x ** 4);
        const yc = cam ? (x < p ? cam / (p * p) * (2 * p * x - x * x) : cam / ((1 - p) ** 2) * (1 - 2 * p + 2 * p * x - x * x)) : 0;
        return yc + sg * yt;
      };
      const pts = [];
      for (let i = 0; i <= NP; i++) pts.push([xs[i], prof(xs[i], 1)]);
      for (let i = lo > 0 ? NP : NP - 1; i >= 0; i--) pts.push([xs[i], prof(xs[i], -1)]);
      return pts.map(([xc, yc]) => {
        const xl = -xc * s.c, zl = yc * s.c;
        const dx = xl * Math.cos(tw) - zl * Math.sin(tw), dz = xl * Math.sin(tw) + zl * Math.cos(tw);
        return add(s.le, [dx, -dz * Math.sin(rl), dz * Math.cos(rl)]);
      });
    });
    const paint = o.paint && ((s, k, mid) => o.paint(s, k, k < NP, mid));
    const ends = { capStart: o.capRoot ?? false, capEnd: o.capTip ?? true, capColor: o.capColor };
    for (const sd of o.side === -1 ? [-1] : o.mirror ? [1, -1] : [1]) this.grid(g, sd > 0 ? R : R.map((r) => r.map((p) => [p[0], -p[1], p[2]])), { paint, ...ends });
  }

  surface(name, mat, st, frac, spec, o = {}) {
    const hx = o.hinge ?? 1 - frac, vert = !!o.vert, first = st[0], last = st[st.length - 1];
    for (const side of o.sides ?? [1, -1]) {
      const layer = o.single ? name : name + (side > 0 ? '_L' : '_R');
      const g = this.grp(layer, mat);
      this.foil(g, o.inflate === false ? st : st.map((s) => ({ ...s, t: s.t * 1.07 })), { side, from: 1 - frac, capRoot: true, capTip: true, np: 6, paint: o.paint });
      const h = [first, last].map((s) => [s.le[0] - hx * s.c, side * s.le[1], s.le[2]]);
      let ax = nrm(sub(h[1], h[0]));
      if (vert ? ax[2] < 0 : ax[1] < 0) ax = scl(ax, -1);
      this.pivots[layer] = scl(add(h[0], h[1]), 0.5);
      this.hinges[layer] = { axis: ax, terms: spec(side) };
    }
  }

  xform(fn) {
    for (const L of Object.values(this.layers)) for (const g of Object.values(L)) for (let k = 0; k < g.v.length; k += 3) {
      const p = fn([g.v[k], g.v[k + 1], g.v[k + 2]]);
      g.v[k] = p[0]; g.v[k + 1] = p[1]; g.v[k + 2] = p[2];
    }
    for (const k of Object.keys(this.pivots)) this.pivots[k] = fn(this.pivots[k]);
    for (const h of Object.values(this.hinges)) { const o = fn([0, 0, 0]), a = fn(h.axis); h.axis = nrm(sub(a, o)); }
  }

  gearLayer(layer, pivot, axis, terms) { this.pivots[layer] = pivot; this.hinges[layer] = { axis, terms }; }

  toGlb() {
    const bins = [], views = [], accs = [], nodes = [{ name: 'root', children: [] }], meshes = [], mats = [], matIdx = {};
    let off = 0;
    const push = (typed, target) => {
      views.push({ buffer: 0, byteOffset: off, byteLength: typed.byteLength, target });
      bins.push(new Uint8Array(typed.buffer.slice(typed.byteOffset, typed.byteOffset + typed.byteLength)));
      off += typed.byteLength;
      return views.length - 1;
    };
    for (const [layerName, L] of Object.entries(this.layers)) {
      const pv = this.pivots[layerName] ?? [0, 0, 0];
      const prims = [];
      for (const [mn, g] of Object.entries(L)) {
        if (!g.i.length) continue;
        const n = g.v.length / 3, pos = new Float32Array(n * 3), nor = new Float32Array(n * 3), col = new Float32Array(g.c);
        for (let k = 0; k < n; k++) {
          pos.set([g.v[k * 3 + 1] - pv[1], g.v[k * 3 + 2] - pv[2], g.v[k * 3] - pv[0]], k * 3);
          nor.set([g.n[k * 3 + 1], g.n[k * 3 + 2], g.n[k * 3]], k * 3);
        }
        const mn3 = [Infinity, Infinity, Infinity], mx3 = [-Infinity, -Infinity, -Infinity];
        for (let k = 0; k < n; k++) for (let a = 0; a < 3; a++) { mn3[a] = Math.min(mn3[a], pos[k * 3 + a]); mx3[a] = Math.max(mx3[a], pos[k * 3 + a]); }
        const idx = new Uint32Array(g.i);
        const vp = push(pos, 34962), vn = push(nor, 34962), vc = push(col, 34962), vi = push(idx, 34963);
        accs.push({ bufferView: vp, componentType: 5126, count: n, type: 'VEC3', min: mn3, max: mx3 });
        accs.push({ bufferView: vn, componentType: 5126, count: n, type: 'VEC3' });
        accs.push({ bufferView: vc, componentType: 5126, count: n, type: 'VEC4' });
        accs.push({ bufferView: vi, componentType: 5125, count: idx.length, type: 'SCALAR' });
        if (!(mn in matIdx)) {
          matIdx[mn] = mats.length;
          mats.push({ name: mn, doubleSided: g.unlit || g.alpha < 1, ...(g.alpha < 1 ? { alphaMode: 'BLEND' } : {}), ...(g.unlit ? { extensions: { KHR_materials_unlit: {} } } : {}), pbrMetallicRoughness: { baseColorFactor: [1, 1, 1, 1], metallicFactor: g.metallic, roughnessFactor: g.rough } });
        }
        prims.push({ attributes: { POSITION: accs.length - 4, NORMAL: accs.length - 3, COLOR_0: accs.length - 2 }, indices: accs.length - 1, material: matIdx[mn] });
      }
      meshes.push({ primitives: prims });
      nodes.push({ name: layerName, mesh: meshes.length - 1, ...(this.pivots[layerName] ? { translation: [pv[1], pv[2], pv[0]] } : {}) });
      nodes[0].children.push(nodes.length - 1);
    }
    const json = JSON.stringify({
      asset: { version: '2.0' }, ...(mats.some((x) => x.extensions) ? { extensionsUsed: ['KHR_materials_unlit'] } : {}), scene: 0, scenes: [{ nodes: [0] }], nodes, meshes, materials: mats,
      accessors: accs, bufferViews: views, buffers: [{ byteLength: off }],
    });
    const enc = new TextEncoder().encode(json);
    const jl = Math.ceil(enc.length / 4) * 4, bl = Math.ceil(off / 4) * 4;
    const out = new Uint8Array(12 + 8 + jl + 8 + bl);
    const dv = new DataView(out.buffer);
    dv.setUint32(0, 0x46546c67, true); dv.setUint32(4, 2, true); dv.setUint32(8, out.length, true);
    dv.setUint32(12, jl, true); dv.setUint32(16, 0x4e4f534a, true);
    out.fill(0x20, 20, 20 + jl); out.set(enc, 20);
    dv.setUint32(20 + jl, bl, true); dv.setUint32(24 + jl, 0x004e4942, true);
    let p = 28 + jl;
    for (const b of bins) { out.set(b, p); p += b.length; }
    return out;
  }
}
const clamp01 = (x) => (x < 0 ? 0 : x > 1 ? 1 : x);

const lerpS = (a, b, t) => ({ ...a, le: [0, 1, 2].map((i) => lerp(a.le[i], b.le[i], t)), c: lerp(a.c, b.c, t), t: lerp(a.t, b.t, t), tw: lerp(a.tw ?? 0, b.tw ?? 0, t), r: lerp(a.r ?? 0, b.r ?? 0, t) });
const seg = (A, B, t0, t1) => [lerpS(A, B, t0), lerpS(A, B, t1)];

const S = (x, zt, zb, w, e = 2.2, y = 0) => ({ x, y, z: (zt + zb) / 2, w, h: (zt - zb) / 2, e });
const S2 = (x, y, z, w, h, e = 2, hb) => ({ x, y, z, w, h, hb, e });

function common(m) {
  return {
    white: m.mat('white', 0xf1f3f5, 0.15, 0.35), cream: m.mat('cream', 0xe9e4d6, 0.15, 0.4), navy: m.mat('navy', 0x15336e, 0.3, 0.35), red: m.mat('red', 0xb71c1c, 0.3, 0.4),
    grey: m.mat('grey', 0x8d969f, 0.45, 0.45), dark: m.mat('dark', 0x1e2226, 0.4, 0.6), glass: m.mat('glass', 0x1c3a52, 0.6, 0.1, 0.72),
    tire: m.mat('tire', 0x121212, 0, 0.95), metal: m.mat('metal', 0xb4bcc4, 0.9, 0.3), camo: m.mat('camo', 0x7b8791, 0.35, 0.55),
    blur: m.mat('blur', 0x20242a, 0, 1, 0.1),
  };
}

function gearWheel(m, g, T, H, c, r, w) {
  m.lathe(T, c, 'y', [[-w / 2, r * 0.74], [-w * 0.42, r * 0.95], [-w * 0.2, r], [w * 0.2, r], [w * 0.42, r * 0.95], [w / 2, r * 0.74]], 22);
  m.lathe(H, c, 'y', [[-w / 2 - 0.004, r * 0.56], [w / 2 + 0.004, r * 0.56]], 18);
}

function cessna() {
  const m = new Model(), c = common(m);
  const W = m.grp('body', c.white), N = m.grp('body', c.navy), R = m.grp('body', c.red), D = m.grp('body', c.dark), Gl = m.grp('canopy', c.glass), Mt = m.grp('body', c.metal);
  const fus = [
    S(2.46, 0.26, -0.20, 0.25), S(2.38, 0.38, -0.32, 0.38), S(2.22, 0.47, -0.42, 0.47), S(1.80, 0.52, -0.48, 0.53), S(1.30, 0.54, -0.55, 0.56),
    S(0.95, 0.60, -0.60, 0.58, 2.4), S(0.60, 0.80, -0.62, 0.59, 2.6), S(0.20, 0.87, -0.62, 0.59, 2.7), S(-0.60, 0.87, -0.62, 0.58, 2.7),
    S(-1.30, 0.74, -0.58, 0.52, 2.5), S(-2.00, 0.56, -0.52, 0.45, 2.3), S(-3.00, 0.40, -0.38, 0.34), S(-4.20, 0.28, -0.20, 0.22),
    S(-5.10, 0.21, -0.06, 0.12), S(-5.55, 0.17, 0.03, 0.04),
  ];
  m.loft(W, fus, 32);
  m.decal(N, fus, { x0: 2.0, x1: -5.2, a0: -0.40, a1: -0.17, col: 0x15336e, off: 0.004 });
  m.decal(N, fus, { x0: 2.0, x1: -5.2, a0: Math.PI + 0.17, a1: Math.PI + 0.40, col: 0x15336e, off: 0.004 });
  m.decal(R, fus, { x0: 2.0, x1: -5.2, a0: -0.14, a1: -0.07, col: 0xb71c1c, off: 0.004 });
  m.decal(R, fus, { x0: 2.0, x1: -5.2, a0: Math.PI + 0.07, a1: Math.PI + 0.14, col: 0xb71c1c, off: 0.004 });
  m.decal(Gl, fus, { x0: 0.98, x1: 0.55, a0: 0.75, a1: Math.PI - 0.75, col: 0x0d1b2a, off: 0.008 });
  for (const sd of [1, -1]) {
    const f = (a0, a1) => (sd > 0 ? [a0, a1] : [Math.PI - a1, Math.PI - a0]);
    const mirror = (a0, a1) => ({ a0: f(a0, a1)[0], a1: f(a0, a1)[1] });
    m.decal(Gl, fus, { x0: 0.82, x1: -0.38, ...mirror(0.18, 1.0), col: 0x0d1b2a, off: 0.008 });
    m.decal(Gl, fus, { x0: -0.55, x1: -1.45, ...mirror(0.2, 0.9), col: 0x0d1b2a, off: 0.008 });
  }
  m.decal(Gl, fus, { x0: -1.62, x1: -2.2, a0: 0.9, a1: Math.PI - 0.9, col: 0x0d1b2a, off: 0.008 });
  m.disc(D, [2.455, 0.1, -0.12], 0.1, 0.07, 0x15181b);
  m.disc(D, [2.455, -0.1, -0.12], 0.1, 0.07, 0x15181b);
  m.box(D, [2.2, 0, -0.44], [0.5, 0.5, 0.01], 0x15181b);

  const tipA = (s, k, up) => (s >= 6 ? 0x15336e : 0xf1f3f5);
  m.foil(W, [
    { le: [0.88, 0.5, 0.92], c: 1.64, t: 0.15, cam: 0.03 }, { le: [0.88, 3.0, 0.97], c: 1.64, t: 0.14, cam: 0.03 },
    { le: [0.68, 4.3, 1.01], c: 1.4, t: 0.13, cam: 0.03 }, { le: [0.48, 5.3, 1.04], c: 1.2, t: 0.12, cam: 0.03 },
    { le: [0.4, 5.58, 1.045], c: 1.1, t: 0.11, cam: 0.02 }, { le: [0.4, 5.66, 1.03], c: 0.9, t: 0.09 }, { le: [0.42, 5.72, 1.0], c: 0.5, t: 0.05 },
  ], { mirror: true, paint: (s) => (s >= 5 ? 0x15336e : 0xf1f3f5), capColor: 0x15336e });
  m.foil(W, [{ le: [-4.0, 0.0, 0.2], c: 1.2, t: 0.1 }, { le: [-4.3, 1.0, 0.21], c: 0.95, t: 0.09 }, { le: [-4.6, 1.72, 0.22], c: 0.62, t: 0.08 }, { le: [-4.66, 1.76, 0.22], c: 0.3, t: 0.04 }], { mirror: true });
  m.foil(W, [
    { le: [-3.65, 0, 0.3], c: 1.8, t: 0.1, r: -Math.PI / 2 }, { le: [-4.2, 0, 0.9], c: 1.3, t: 0.09, r: -Math.PI / 2 },
    { le: [-4.9, 0, 1.5], c: 0.8, t: 0.08, r: -Math.PI / 2 }, { le: [-5.15, 0, 1.64], c: 0.5, t: 0.05, r: -Math.PI / 2 },
  ], { paint: (s) => (s >= 2 ? 0x15336e : 0xf1f3f5), capColor: 0x15336e, capRoot: false });
  for (const s of [-1, 1]) {
    m.bar(W, [0.55, s * 0.62, -0.5], [0.4, s * 2.75, 0.96], 0.04, 0.13, [1, 0, 0]);
    m.bar(W, [0.18, s * 1.7, 0.3], [0.2, s * 2.2, 0.98], 0.025, 0.06, [1, 0, 0]);
  }

  const CW0 = { le: [0.88, 0.5, 0.92], c: 1.64, t: 0.15 }, CW1 = { le: [0.88, 3.0, 0.97], c: 1.64, t: 0.14 }, CW3 = { le: [0.48, 5.3, 1.04], c: 1.2, t: 0.12 };
  m.surface('flap', c.white, seg(CW0, CW1, 0.1, 0.95), 0.3, () => [['flap', -0.52]]);
  m.surface('aileron', c.white, seg(CW1, CW3, 0.04, 0.93), 0.3, (s) => [['da', -0.35 * s]]);
  m.surface('elevator', c.white, seg({ le: [-4.0, 0, 0.2], c: 1.2, t: 0.1 }, { le: [-4.6, 1.72, 0.22], c: 0.62, t: 0.08 }, 0.06, 0.96), 0.38, () => [['de', 0.45]]);
  m.surface('rudder', c.white, seg({ le: [-3.65, 0, 0.3], c: 1.8, t: 0.1, r: -Math.PI / 2 }, { le: [-4.9, 0, 1.5], c: 0.8, t: 0.08, r: -Math.PI / 2 }, 0.15, 0.97), 0.36, () => [['dr', 0.45]], { vert: true, single: true, sides: [1] });
  const Gr = m.grp('gear', c.white), T = m.grp('gear', c.tire), H = m.grp('gear', c.metal), Sp = m.grp('gear', c.white), Nv = m.grp('gear', c.navy);
  m.bar(H, [1.55, 0, -0.45], [1.55, 0, -0.93], 0.06);
  m.bar(Gr, [1.62, 0, -0.5], [1.55, 0, -0.84], 0.09, 0.2, [0, 1, 0]);
  gearWheel(m, Gr, T, H, [1.55, 0, -0.94], 0.16, 0.12);
  for (const s of [-1, 1]) {
    m.bar(Gr, [-0.1, s * 0.35, -0.55], [-0.14, s * 1.22, -0.88], 0.1, 0.22, [1, 0, 0]);
    gearWheel(m, Gr, T, H, [-0.15, s * 1.25, -0.93], 0.17, 0.13);
    m.loft(Sp, [S2(0.6, s * 1.25, -0.82, 0.03, 0.03), S2(0.4, s * 1.28, -0.84, 0.1, 0.2), S2(0.1, s * 1.3, -0.86, 0.12, 0.24), S2(-0.4, s * 1.3, -0.86, 0.1, 0.22), S2(-0.85, s * 1.27, -0.84, 0.04, 0.06)], 14, { capStart: false, capEnd: false });
    m.loft(Nv, [S2(0.42, s * 1.34, -0.84, 0.01, 0.04)].concat([S2(0.1, s * 1.43, -0.86, 0.015, 0.08)]), 6);
  }

  m.pivots.prop = [2.46, 0, 0.03];
  m.pivots.disc = [2.46, 0, 0.03];
  const P = m.grp('prop', c.dark), Ps = m.grp('prop', c.metal), Py = m.grp('prop', c.red);
  m.lathe(Ps, [2.46, 0, 0.03], 'x', [[0, 0.18], [0.07, 0.175], [0.18, 0.14], [0.3, 0.08], [0.37, 0]], 20);
  for (const sd of [1, -1]) {
    const bl = (r0, r1, w0, w1, col) => {
      const x = 2.52, p = (r, w, dx) => [[x + dx - 0.015, -w / 2, 0.03 + sd * r], [x + dx + 0.015, -w / 2, 0.03 + sd * r], [x + dx + 0.015, w / 2, 0.03 + sd * r], [x + dx - 0.015, w / 2, 0.03 + sd * r]];
      m.hexa(P, [...p(r0, w0, 0), ...p(r1, w1, 0.04)], col);
    };
    bl(0.12, 0.88, 0.17, 0.09, 0x1b1b1b);
    bl(0.88, 0.95, 0.09, 0.085, 0xf2c200);
  }
  const Bd = m.grp('disc', c.blur);
  m.lathe(Bd, [2.52, 0, 0.03], 'x', [[-0.002, 0.95], [0.002, 0.95]], 40);
  return m;
}

function b737(o = {}) {
  const m = new Model(), c = common(m);
  if (o.navy) m.groups.navy.color = lin(o.navy);
  const NAVY = o.navy ?? 0x15336e;
  const W = m.grp('body', c.white), N = m.grp('body', c.navy), D = m.grp('body', c.dark), Gl = m.grp('canopy', c.glass), G = m.grp('body', c.grey), Mt = m.grp('body', c.metal);
  const sec = (x, zc, r) => ({ x, y: 0, z: zc, w: r, h: r, e: 2 });
  const fus = [
    sec(17.82, -0.40, 0.06), sec(17.7, -0.40, 0.42), sec(17.35, -0.38, 0.85), sec(16.85, -0.33, 1.2), sec(16.2, -0.24, 1.5), sec(15.4, -0.14, 1.7),
    sec(14.4, -0.06, 1.82), sec(13.0, 0, 1.87), sec(11.5, 0, 1.88), sec(-11.5, 0, 1.88), sec(-13.2, 0.1, 1.84), sec(-14.8, 0.35, 1.7),
    sec(-16.4, 0.7, 1.45), sec(-18.0, 1.1, 1.1), sec(-19.6, 1.45, 0.72), sec(-20.9, 1.62, 0.4), sec(-21.7, 1.7, 0.22),
  ];
  m.loft(W, fus, 40, { paint: (x, a) => (x > 17.2 ? 0xa9b0b6 : Math.sin(a) < -0.62 ? 0xc3c9ce : null), capEnd: true });
  for (const sd of [0, Math.PI]) {
    const f = (a0, a1) => (sd ? [Math.PI - a1, Math.PI - a0] : [a0, a1]);
    m.decal(N, fus, { x0: 16.0, x1: -17.4, a0: f(-0.30, -0.12)[0], a1: f(-0.30, -0.12)[1], col: NAVY, off: 0.006 });
    m.decal(Gl, fus, { x0: 16.75, x1: 15.95, a0: f(0.62, 1.12)[0], a1: f(0.62, 1.12)[1], col: 0x0b1620, off: 0.01 });
    m.decal(Gl, fus, { x0: 15.85, x1: 14.95, a0: f(0.38, 0.9)[0], a1: f(0.38, 0.9)[1], col: 0x0b1620, off: 0.01 });
    for (let x = 12.4; x > -14.2; x -= 0.508) {
      if (Math.abs(x - 11.6) < 0.7 || Math.abs(x + 1.7) < 0.9) continue;
      m.decal(Gl, fus, { x0: x, x1: x - 0.24, a0: f(0.2, 0.36)[0], a1: f(0.2, 0.36)[1], col: 0x0b1620, off: 0.008 });
    }
    m.decal(G, fus, { x0: 13.5, x1: 12.6, a0: f(-0.05, 0.9)[0], a1: f(-0.05, 0.9)[1], col: 0xb8bec4, off: 0.005 });
    m.decal(G, fus, { x0: -12.5, x1: -13.4, a0: f(-0.05, 0.9)[0], a1: f(-0.05, 0.9)[1], col: 0xb8bec4, off: 0.005 });
  }
  m.loft(G, [S(6.5, -1.55, -1.95, 0.9, 2.4), S(4.5, -1.35, -2.05, 1.6, 2.4), S(0.0, -1.3, -2.15, 1.85, 2.4), S(-4.0, -1.3, -2.1, 1.75, 2.4), S(-6.5, -1.45, -1.95, 1.1, 2.4), S(-7.6, -1.6, -1.8, 0.4, 2.4)], 24);

  const wpaint = (s, k, up) => (up ? 0xdde2e6 : 0xb4bbc2);
  m.foil(W, [
    { le: [3.7, 1.6, -0.95], c: 7.4, t: 0.15, cam: 0.02 }, { le: [1.7, 5.8, -0.56], c: 5.2, t: 0.13, cam: 0.025 },
    { le: [-1.4, 12.0, 0.1], c: 3.1, t: 0.115, cam: 0.025, tw: -0.03 }, { le: [-4.55, 17.3, 0.96], c: 1.7, t: 0.105, cam: 0.02, tw: -0.045 },
  ], { mirror: true, paint: wpaint, np: 14 });
  m.foil(N, [
    { le: [-4.58, 17.3, 0.96], c: 1.7, t: 0.1, r: -0.1 }, { le: [-4.9, 17.55, 1.5], c: 1.45, t: 0.085, r: -0.6 },
    { le: [-5.5, 17.78, 2.4], c: 1.05, t: 0.08, r: -1.3 }, { le: [-6.1, 17.84, 3.15], c: 0.6, t: 0.06, r: -1.45 },
  ], { mirror: true, capTip: true });
  m.foil(G, [
    { le: [-17.2, 0.9, 1.15], c: 4.1, t: 0.1 }, { le: [-19.3, 4.2, 1.45], c: 2.6, t: 0.085 }, { le: [-21.0, 6.9, 1.7], c: 1.35, t: 0.075 },
  ], { mirror: true });
  m.foil(N, [
    { le: [-13.6, 0, 1.6], c: 7.0, t: 0.11, r: -Math.PI / 2 }, { le: [-15.8, 0, 4.3], c: 4.4, t: 0.1, r: -Math.PI / 2 },
    { le: [-17.9, 0, 7.0], c: 3.1, t: 0.09, r: -Math.PI / 2 }, { le: [-19.5, 0, 9.4], c: 2.1, t: 0.07, r: -Math.PI / 2 },
  ]);
  m.foil(N, [{ le: [-11.0, 0, 1.8], c: 2.2, t: 0.04, r: -Math.PI / 2 }, { le: [-13.5, 0, 2.3], c: 0.9, t: 0.03, r: -Math.PI / 2 }]);

  for (const s of [-1, 1]) {
    const y = s * 5.75, z = -1.72;
    const nac = [
      S2(4.40, y, z, 0.72, 0.70, 2, 0.66), S2(4.34, y, z, 0.82, 0.8, 2, 0.72), S2(4.15, y, z, 0.9, 0.9, 2.1, 0.78), S2(3.4, y, z, 0.96, 0.96, 2.15, 0.8),
      S2(1.8, y, z, 0.93, 0.93, 2.15, 0.78), S2(0.6, y, z, 0.8, 0.82, 2.1, 0.7), S2(0.15, y, z, 0.62, 0.62), S2(-0.7, y, z, 0.54, 0.54), S2(-1.3, y, z, 0.4, 0.4),
    ];
    m.loft(W, nac, 32, { capStart: false, capEnd: false });
    m.loft(G, [S2(4.40, y, z, 0.72, 0.70), S2(3.8, y, z, 0.7, 0.68), S2(3.5, y, z, 0.68, 0.66)], 28, { capStart: false, capEnd: false });
    m.disc(D, [3.49, y, z], 0.66, 0.64, 0x2b3036);
    const side = s > 0 ? 'L' : 'R', fl = 'fan_' + side, Fm = m.grp(fl, c.metal), Fd = m.grp(fl, c.dark);
    m.pivots[fl] = [3.5, y, z];
    m.lathe(Fm, [3.5, y, z], 'x', [[0, 0.24], [0.1, 0.22], [0.3, 0.15], [0.55, 0]], 20);
    for (let k = 0; k < 24; k++) {
      const a = 2 * Math.PI * k / 24, ca = Math.cos(a), sa = Math.sin(a);
      m.bar(Fd, [3.6, y + 0.2 * ca, z + 0.2 * sa], [3.52, y + 0.64 * ca, z + 0.62 * sa], 0.07, 0.012, [1, 0, 0]);
    }
    m.pivots['fanblur_' + side] = [3.5, y, z];
    m.lathe(m.grp('fanblur_' + side, c.blur), [3.5, y, z], 'x', [[-0.003, 0.64], [0.003, 0.64]], 36);
    m.lathe(G, [-1.2, y, z], 'x', [[0, 0.3], [0.5, 0.2], [1.0, 0.05], [1.2, 0]], 18);
    m.bar(W, [2.6, y, -0.45], [1.6, y, z + 0.55], 0.22, 1.9, [0, 0, 1]);
  }
  const BW0 = { le: [3.7, 1.6, -0.95], c: 7.4, t: 0.15 }, BW1 = { le: [1.7, 5.8, -0.56], c: 5.2, t: 0.13 };
  const BW2 = { le: [-1.4, 12.0, 0.1], c: 3.1, t: 0.115, tw: -0.03 }, BW3 = { le: [-4.55, 17.3, 0.96], c: 1.7, t: 0.105, tw: -0.045 };
  m.surface('flapI', c.white, seg(BW0, BW1, 0.12, 0.96), 0.3, () => [['flap', -0.6]], { paint: wpaint });
  m.surface('flapO', c.white, seg(BW1, BW2, 0.1, 0.82), 0.3, () => [['flap', -0.6]], { paint: wpaint });
  m.surface('aileron', c.white, seg(BW2, BW3, 0.35, 0.92), 0.28, (s) => [['da', -0.3 * s]], { paint: wpaint });
  m.surface('elevator', c.grey, seg({ le: [-17.2, 0.9, 1.15], c: 4.1, t: 0.1 }, { le: [-21.0, 6.9, 1.7], c: 1.35, t: 0.075 }, 0.05, 0.97), 0.32, () => [['de', 0.4]]);
  m.surface('rudder', c.navy, seg({ le: [-13.6, 0, 1.6], c: 7.0, t: 0.11, r: -Math.PI / 2 }, { le: [-19.5, 0, 9.4], c: 2.1, t: 0.07, r: -Math.PI / 2 }, 0.05, 0.97), 0.3, () => [['dr', 0.4]], { vert: true, single: true, sides: [1] });
  const Gr = m.grp('gear_nose', c.metal), T = m.grp('gear_nose', c.tire), Hb = m.grp('gear_nose', c.grey);
  m.gearLayer('gear_nose', [14.4, 0, -1.4], [0, 1, 0], [['gear', -1.5]]);
  m.bar(Gr, [14.45, 0, -1.4], [14.4, 0, -2.66], 0.17);
  m.bar(Hb, [14.1, 0, -1.9], [14.35, 0, -2.3], 0.06);
  for (const sd of [-1, 1]) gearWheel(m, Gr, T, Hb, [14.4, sd * 0.27, -2.67], 0.33, 0.22);
  for (const s of [-1, 1]) {
    const ly = s > 0 ? 'gear_mL' : 'gear_mR', Gr = m.grp(ly, c.metal), T = m.grp(ly, c.tire), Hb = m.grp(ly, c.grey);
    m.gearLayer(ly, [-1.2, s * 2.3, -0.9], [1, 0, 0], [['gear', -1.45 * s]]);
    m.bar(Gr, [-1.2, s * 2.3, -0.9], [-1.2, s * 2.86, -2.45], 0.26);
    m.bar(Gr, [-0.5, s * 2.5, -1.2], [-1.15, s * 2.84, -2.1], 0.09);
    m.bar(Hb, [-1.2, s * 2.3, -0.95], [-1.2, s * 2.86, -2.4], 0.1, 0.4, [1, 0, 0]);
    m.bar(Gr, [-1.2, s * 2.3, -2.5], [-1.2, s * 3.4, -2.5], 0.1);
    for (const dy of [-0.34, 0.34]) gearWheel(m, Gr, T, Hb, [-1.2, s * 2.86 + dy, -2.5], 0.5, 0.3);
  }
  return m;
}

function gripen() {
  const m = new Model(), c = common(m);
  const Gy = m.grp('body', c.camo), D = m.grp('body', c.dark), Gl = m.grp('canopy', c.glass), Wt = m.grp('body', c.white), Mt = m.grp('body', c.metal), Gk = m.grp('body', c.grey);
  const St = m.grp('stores', c.white), SD = m.grp('stores', c.dark), SM = m.grp('stores', c.metal), SG = m.grp('stores', c.grey);
  const top = 0x68747f, bot = 0x9aa5ae;
  const fus = [
    S(6.5, 0.04, -0.04, 0.04, 2), S(5.9, 0.30, -0.26, 0.27), S(5.2, 0.47, -0.42, 0.41), S(4.4, 0.52, -0.56, 0.52), S(3.0, 0.52, -0.64, 0.64, 2.2),
    S(1.2, 0.55, -0.68, 0.80, 2.4), S(-1.0, 0.64, -0.68, 0.88, 2.5), S(-3.2, 0.62, -0.62, 0.84, 2.5), S(-5.2, 0.48, -0.56, 0.70, 2.3),
    S(-6.8, 0.41, -0.49, 0.54), S(-7.55, 0.37, -0.46, 0.46),
  ];
  m.loft(Gy, fus, 32, { paint: (x, a) => (x > 5.7 ? 0x3d4249 : Math.sin(a) > 0.25 ? top : bot) });
  m.loft(Gl, [S2(5.0, 0, 0.43, 0.03, 0.02), S2(4.65, 0, 0.5, 0.2, 0.2, 2.2), S2(4.0, 0, 0.52, 0.34, 0.46, 2.2), S2(3.0, 0, 0.52, 0.38, 0.46, 2.2), S2(2.2, 0, 0.5, 0.32, 0.34, 2.2), S2(1.6, 0, 0.47, 0.05, 0.08)], 20, { capStart: false, capEnd: false });
  jetNozzle(m, c, -7.5, -0.05, 0.46, 6.5);
  m.bar(Gk, [6.5, 0, -0.02], [7.7, 0, -0.02], 0.035);
  m.bar(Mt, [5.2, 0.34, 0.38], [4.55, 0.38, 0.28], 0.05);

  for (const s of [-1, 1]) {
    const intake = [S2(3.1, s * 0.84, -0.3, 0.3, 0.5, 2.4), S2(2.8, s * 0.86, -0.26, 0.34, 0.54, 2.4), S2(1.2, s * 0.92, -0.14, 0.38, 0.56, 2.4), S2(-0.8, s * 0.96, -0.08, 0.36, 0.54, 2.4), S2(-2.8, s * 0.86, -0.04, 0.16, 0.34, 2.3)];
    m.loft(Gy, intake, 24, { paint: (x, a) => (Math.sin(a) > 0.2 ? top : bot), capStart: false, capEnd: false });
    m.loft(D, [S2(3.1, s * 0.84, -0.3, 0.27, 0.46, 2.4), S2(2.7, s * 0.84, -0.3, 0.25, 0.44, 2.4)], 22, { capStart: false, capEnd: true, capColor: 0x0e1012 });
    m.box(Gk, [3.0, s * 0.54, -0.3], [0.7, 0.025, 0.8]);
  }
  const wp = (s, k, up) => (up ? top : bot);
  m.foil(Gy, [
    { le: [1.2, 0.55, -0.3], c: 5.9, t: 0.06 }, { le: [-0.9, 2.2, -0.25], c: 4.2, t: 0.055 }, { le: [-2.2, 3.75, -0.22], c: 2.0, t: 0.05 },
    { le: [-2.65, 4.18, -0.2], c: 1.3, t: 0.045 }, { le: [-2.72, 4.25, -0.2], c: 0.9, t: 0.03 },
  ], { mirror: true, paint: wp, np: 10 });
  m.surface('canard', c.camo, [{ le: [4.0, 0.7, 0.14], c: 2.4, t: 0.05 }, { le: [3.2, 1.6, 0.16], c: 1.3, t: 0.04 }, { le: [2.8, 2.15, 0.17], c: 0.7, t: 0.03 }, { le: [2.74, 2.25, 0.17], c: 0.4, t: 0.02 }], 1, () => [['de', -0.35]], { hinge: 0.3, inflate: false, paint: wp });
  m.foil(Gy, [
    { le: [-2.8, 0, 0.35], c: 3.3, t: 0.07, r: -Math.PI / 2 }, { le: [-4.2, 0, 1.3], c: 2.3, t: 0.055, r: -Math.PI / 2 },
    { le: [-5.4, 0, 2.2], c: 1.55, t: 0.05, r: -Math.PI / 2 }, { le: [-6.2, 0, 3.0], c: 1.0, t: 0.04, r: -Math.PI / 2 },
  ], { paint: () => 0x78848e });
  for (const s of [-1, 1]) {
    const y = s * 4.35;
    m.box(Mt, [-2.65, y, -0.28], [1.5, 0.06, 0.06]);
    m.lathe(St, [-2.65, y, -0.22], 'x', [[1.45, 0], [1.38, 0.045], [1.2, 0.063], [-1.2, 0.063], [-1.45, 0.05]], 14, { capStart: false });
    m.lathe(SD, [-2.65, y, -0.22], 'x', [[1.5, 0], [1.42, 0.02], [1.34, 0.05], [1.3, 0.063]], 14, { capStart: false, capEnd: false });
    for (const [dx, dy, dz] of [[-1.25, 0.16, 0], [-1.25, -0.16, 0], [-1.25, 0, 0.16], [-1.25, 0, -0.16]]) {
      const dir = dy ? [0, 1, 0] : [0, 0, 1];
      m.box(SM, [-2.65 + dx, y + dy / 2, -0.22 + dz / 2], [0.4, dy ? 0.17 : 0.012, dy ? 0.012 : 0.17]);
    }
  }
  const GW0 = { le: [1.2, 0.55, -0.3], c: 5.9, t: 0.06 }, GW2 = { le: [-2.2, 3.75, -0.22], c: 2.0, t: 0.05 };
  m.surface('elevon', c.camo, seg(GW0, GW2, 0.18, 0.98), 0.24, (s) => [['de', 0.45], ['da', -0.45 * s]], { paint: wp });
  m.surface('rudder', c.camo, seg({ le: [-4.2, 0, 1.3], c: 2.3, t: 0.055, r: -Math.PI / 2 }, { le: [-6.2, 0, 3.0], c: 1.0, t: 0.04, r: -Math.PI / 2 }, 0.04, 0.97), 0.3, () => [['dr', 0.45]], { vert: true, single: true, sides: [1], paint: () => 0x78848e });
  const Gr = m.grp('gear_nose', c.metal), T = m.grp('gear_nose', c.tire), Hb = m.grp('gear_nose', c.grey), Dk = m.grp('gear_nose', c.dark);
  m.gearLayer('gear_nose', [3.5, 0, -0.55], [0, 1, 0], [['gear', -1.5]]);
  m.bar(Gr, [3.5, 0, -0.55], [3.6, 0, -1.3], 0.11);
  m.bar(Dk, [3.3, 0, -0.55], [3.2, 0, -1.0], 0.04);
  for (const sd of [-1, 1]) gearWheel(m, Gr, T, Hb, [3.6, sd * 0.1, -1.28], 0.22, 0.12);
  for (const s of [-1, 1]) {
    const ly = s > 0 ? 'gear_mL' : 'gear_mR', Gr = m.grp(ly, c.metal), T = m.grp(ly, c.tire), Hb = m.grp(ly, c.grey), Dk = m.grp(ly, c.dark);
    m.gearLayer(ly, [-0.8, s * 0.55, -0.55], [1, 0, 0], [['gear', -1.4 * s]]);
    m.bar(Gr, [-0.8, s * 0.55, -0.55], [-0.9, s * 1.08, -1.2], 0.13);
    m.bar(Dk, [-0.4, s * 0.6, -0.55], [-0.85, s * 1.05, -1.0], 0.05);
    gearWheel(m, Gr, T, Hb, [-0.9, s * 1.15, -1.22], 0.28, 0.2);
  }
  return m;
}

function jetNozzle(m, c, x, z, r0, len) {
  const Nz = m.grp('nozzle', c.nozzle || (c.nozzle = m.mat('nozzle', 0x5b5047, 0.8, 0.45))), Hot = m.grp('nozzle', c.hot || (c.hot = m.mat('hot', 0x2b1a10, 0.1, 0.8)));
  m.pivots.nozzle = [x, 0, z];
  m.lathe(Nz, [x, 0, z], 'x', [[0, r0], [-0.35, r0 * 0.97], [-0.85, r0 * 0.82]], 24, { capStart: false, capEnd: false });
  m.disc(Hot, [x - 0.08, 0, z], r0 * 0.95, r0 * 0.95, 0x2b1a10, -1, 24);
  const Fo = m.grp('flame', c.flameOut || (c.flameOut = m.mat('flameOut', 0xff7a1c, 0, 1, 0.8, true)));
  const Fi = m.grp('flame', c.flameIn || (c.flameIn = m.mat('flameIn', 0xd8ecff, 0, 1, 0.9, true)));
  const Fd = m.grp('flame', c.flameDia || (c.flameDia = m.mat('flameDia', 0xfff3c4, 0, 1, 1, true)));
  const fx = x - 0.85, r = r0 * 0.8, L = len;
  m.pivots.flame = [fx, 0, z];
  m.lathe(Fo, [fx, 0, z], 'x', [[0, r], [-L * 0.18, r * 1.08], [-L * 0.45, r * 0.82], [-L * 0.75, r * 0.46], [-L, 0]], 22, { capStart: false });
  m.lathe(Fi, [fx, 0, z], 'x', [[0, r * 0.68], [-L * 0.15, r * 0.66], [-L * 0.34, r * 0.43], [-L * 0.52, r * 0.16], [-L * 0.6, 0]], 16, { capStart: false });
  for (const t of [0.22, 0.4, 0.56]) m.lathe(Fd, [fx - L * t, 0, z], 'x', [[0.22, 0], [0.1, r * 0.3], [-0.1, r * 0.3], [-0.22, 0]], 10);
}

function f16() {
  const m = new Model(), c = common(m);
  const St = m.grp('stores', c.white), SD = m.grp('stores', c.dark), SM = m.grp('stores', c.metal), SG = m.grp('stores', c.grey);
  const Gy = m.grp('body', c.camo), D = m.grp('body', c.dark), Gl = m.grp('canopy', c.glass), Wt = m.grp('body', c.white), Mt = m.grp('body', c.metal), Gk = m.grp('body', c.grey);
  const top = 0x7f8993, bot = 0xa7b0b8, dk = 0x3a3f45;
  const fus = [
    S(6.6, 0.04, -0.04, 0.04, 2), S(6.2, 0.30, -0.26, 0.28), S(5.4, 0.50, -0.44, 0.46), S(4.4, 0.60, -0.56, 0.58, 2.2), S(3.2, 0.62, -0.66, 0.70, 2.3),
    S(1.6, 0.62, -0.80, 0.80, 2.4), S(0, 0.66, -0.78, 0.86, 2.5), S(-2.0, 0.66, -0.70, 0.84, 2.5), S(-4.0, 0.58, -0.62, 0.72, 2.4),
    S(-6.0, 0.52, -0.56, 0.60, 2.2), S(-7.2, 0.48, -0.50, 0.52, 2.0), S(-7.7, 0.47, -0.49, 0.50, 2.0),
  ];
  m.loft(Gy, fus, 32, { paint: (x, a) => (x > 5.7 ? dk : Math.sin(a) > 0.1 ? top : bot) });
  m.loft(Gl, [S2(4.3, 0, 0.58, 0.05, 0.04), S2(3.9, 0, 0.62, 0.32, 0.22, 2.2, 0.04), S2(3.2, 0, 0.66, 0.4, 0.36, 2.2, 0.04), S2(2.4, 0, 0.66, 0.4, 0.34, 2.2, 0.04), S2(1.6, 0, 0.64, 0.34, 0.24, 2.2, 0.04), S2(1.0, 0, 0.62, 0.2, 0.12, 2.2, 0.04), S2(0.6, 0, 0.6, 0.05, 0.04)], 20, { capStart: false, capEnd: false });
  for (const s of [-1, 1]) {
    m.bar(D, [3.95, s * 0.3, 0.7], [0.9, s * 0.36, 0.66], 0.035, 0.035);
  }
  m.loft(Gy, [S2(4.0, 0, -0.64, 0.5, 0.28, 2.4, 0.2), S2(3.4, 0, -0.72, 0.54, 0.34, 2.4, 0.2), S2(2.4, 0, -0.82, 0.58, 0.36, 2.4, 0.2), S2(1.4, 0, -0.84, 0.56, 0.3, 2.4, 0.18), S2(0.6, 0, -0.76, 0.42, 0.2, 2.4, 0.1)], 24, { capStart: false, capEnd: false, paint: () => bot });
  m.disc(D, [3.95, 0, -0.66], 0.46, 0.24, 0x14171a, 1);
  m.box(Gk, [3.7, 0, -0.38], [0.7, 0.04, 0.1]);

  const wp = (s, k, up) => (up ? top : bot);
  const W0 = { le: [1.0, 0.62, -0.30], c: 4.9, t: 0.055, cam: 0.01 }, W1 = { le: [-0.55, 2.5, -0.30], c: 3.2, t: 0.05 }, W2 = { le: [-2.4, 4.72, -0.30], c: 1.2, t: 0.04 }, W3 = { le: [-2.46, 4.78, -0.30], c: 0.9, t: 0.025 };
  m.foil(Gy, [W0, W1, W2, W3], { mirror: true, paint: wp, np: 10 });
  m.foil(Gy, [{ le: [3.8, 0.42, -0.2], c: 0.3, t: 0.05 }, { le: [2.2, 1.2, -0.26], c: 2.8, t: 0.045 }, { le: [1.0, 1.9, -0.29], c: 3.7, t: 0.05 }], { mirror: true, paint: wp, np: 8 });
  m.foil(Gy, [{ le: [-3.1, 0, 0.5], c: 4.0, t: 0.06, r: -Math.PI / 2 }, { le: [-5.9, 0, 3.35], c: 1.5, t: 0.04, r: -Math.PI / 2 }], { paint: () => 0x8a949d });
  m.foil(Gy, [{ le: [-3.4, 0.62, -0.5], c: 2.4, t: 0.03, r: -1.17 }, { le: [-4.6, 0.95, -1.15], c: 1.2, t: 0.02, r: -1.17 }], { mirror: true, paint: () => bot });
  m.surface('flaperon', c.camo, seg(W0, W1, 0.1, 1.0), 0.25, (s) => [['flap', -0.3], ['da', -0.4 * s]], { paint: wp });
  m.surface('stab', c.camo, [{ le: [-5.2, 0.62, -0.12], c: 2.9, t: 0.05 }, { le: [-6.95, 2.75, -0.12], c: 1.15, t: 0.035 }], 1, (s) => [['de', 0.45], ['da', -0.2 * s]], { hinge: 0.55, inflate: false, paint: wp });
  m.surface('rudder', c.camo, seg({ le: [-3.1, 0, 0.5], c: 4.0, t: 0.06, r: -Math.PI / 2 }, { le: [-5.9, 0, 3.35], c: 1.5, t: 0.04, r: -Math.PI / 2 }, 0.05, 0.98), 0.3, () => [['dr', 0.45]], { vert: true, single: true, sides: [1], paint: () => 0x8a949d });
  for (const s of [-1, 1]) {
    const y = s * 4.82;
    m.lathe(St, [-2.2, y, -0.3], 'x', [[1.45, 0], [1.38, 0.045], [1.2, 0.063], [-1.2, 0.063], [-1.45, 0.05]], 14, { capStart: false });
    m.lathe(SD, [-2.2, y, -0.3], 'x', [[1.5, 0], [1.42, 0.02], [1.34, 0.05], [1.3, 0.063]], 14, { capStart: false, capEnd: false });
    m.box(Mt, [-2.2, y, -0.3], [1.8, 0.06, 0.05]);
    for (const [dy, dz] of [[0.16, 0], [-0.16, 0], [0, 0.16], [0, -0.16]]) m.box(SM, [-3.45, y + dy / 2, -0.3 + dz / 2], [0.4, dy ? 0.17 : 0.012, dy ? 0.012 : 0.17]);
    m.lathe(SG, [-1.0, s * 2.7, -0.7], 'x', [[1.6, 0], [1.45, 0.1], [1.0, 0.2], [-1.0, 0.2], [-1.5, 0.12], [-1.65, 0]], 16);
    m.bar(SM, [-0.6, s * 2.7, -0.36], [-0.6, s * 2.7, -0.55], 0.08, 0.5, [0, 1, 0]);
  }
  jetNozzle(m, c, -7.7, 0, 0.5, 7.0);

  const Gr = m.grp('gear_nose', c.metal), T = m.grp('gear_nose', c.tire), Hb = m.grp('gear_nose', c.grey), Dk = m.grp('gear_nose', c.dark);
  m.gearLayer('gear_nose', [3.8, 0, -0.55], [0, 1, 0], [['gear', -1.5]]);
  m.bar(Gr, [3.8, 0, -0.55], [3.9, 0, -1.3], 0.1);
  m.bar(Dk, [3.6, 0, -0.55], [3.5, 0, -1.0], 0.04);
  for (const sd of [-1, 1]) gearWheel(m, Gr, T, Hb, [3.9, sd * 0.1, -1.3], 0.2, 0.11);
  for (const s of [-1, 1]) {
    const ly = s > 0 ? 'gear_mL' : 'gear_mR', Gr = m.grp(ly, c.metal), T = m.grp(ly, c.tire), Hb = m.grp(ly, c.grey), Dk = m.grp(ly, c.dark);
    m.gearLayer(ly, [-0.7, s * 0.6, -0.6], [1, 0, 0], [['gear', -1.4 * s]]);
    m.bar(Gr, [-0.7, s * 0.6, -0.6], [-0.8, s * 1.15, -1.22], 0.12);
    m.bar(Dk, [-0.4, s * 0.65, -0.6], [-0.78, s * 1.1, -1.0], 0.05);
    gearWheel(m, Gr, T, Hb, [-0.8, s * 1.2, -1.23], 0.27, 0.19);
  }
  return m;
}

function tucano() {
  const m = new Model(), c = common(m);
  const St = m.grp('stores', c.white), SD = m.grp('stores', c.dark), SM = m.grp('stores', c.metal), SG = m.grp('stores', c.grey);
  const Gy = m.grp('body', c.camo), D = m.grp('body', c.dark), Gl = m.grp('canopy', c.glass), Wt = m.grp('body', c.white), Mt = m.grp('body', c.metal), Gk = m.grp('body', c.grey), R = m.grp('body', c.red);
  const gr = 0x5d6f58, lg = 0xaab2b8;
  const fus = [
    S(4.42, 0.34, -0.34, 0.34, 2.2), S(4.3, 0.46, -0.46, 0.46), S(3.6, 0.52, -0.5, 0.52, 2.3), S(2.6, 0.58, -0.56, 0.58, 2.3), S(1.6, 0.62, -0.62, 0.6, 2.3),
    S(0.4, 0.66, -0.64, 0.62, 2.4), S(-0.8, 0.66, -0.64, 0.6, 2.4), S(-2.0, 0.58, -0.58, 0.52, 2.3), S(-3.5, 0.46, -0.48, 0.4, 2.2),
    S(-5.0, 0.34, -0.36, 0.26), S(-6.0, 0.26, -0.28, 0.15), S(-6.55, 0.22, -0.24, 0.08),
  ];
  m.loft(Gy, fus, 32, { paint: (x, a) => (x > 4.2 ? 0x2a2f33 : Math.sin(a) > -0.2 ? gr : lg) });
  m.loft(Gl, [S2(1.95, 0, 0.64, 0.04, 0.03), S2(1.65, 0, 0.74, 0.3, 0.2), S2(1.0, 0, 0.8, 0.36, 0.4, 2.2, 0.04), S2(0, 0, 0.8, 0.37, 0.46, 2.2, 0.04), S2(-1.0, 0, 0.78, 0.34, 0.4, 2.2, 0.04), S2(-1.7, 0, 0.72, 0.22, 0.22, 2.2, 0.04), S2(-2.2, 0, 0.64, 0.04, 0.04)], 20, { capStart: false, capEnd: false });
  const bow = (x, w, h) => {
    const F = m.grp('canopy', c.dark);
    m.bar(F, [x, -w, 0.8], [x, -w * 0.5, h], 0.04, 0.04); m.bar(F, [x, -w * 0.5, h], [x, w * 0.5, h], 0.04, 0.04); m.bar(F, [x, w * 0.5, h], [x, w, 0.8], 0.04, 0.04);
  };
  bow(1.75, 0.3, 1.14); bow(0.55, 0.36, 1.22); bow(-0.6, 0.35, 1.2); bow(-1.7, 0.2, 0.92);
  m.bar(m.grp('canopy', c.dark), [1.8, 0, 1.12], [-1.8, 0, 0.92], 0.03, 0.03);
  m.loft(Gy, [S2(3.9, 0, -0.5, 0.2, 0.16, 2.4, 0.1), S2(3.3, 0, -0.58, 0.3, 0.2, 2.4, 0.1), S2(2.5, 0, -0.62, 0.28, 0.18, 2.4, 0.08), S2(1.8, 0, -0.62, 0.1, 0.06)], 18, { capStart: false, capEnd: false, paint: () => lg });
  m.disc(D, [3.9, 0, -0.5], 0.17, 0.12, 0x14171a, 1);
  for (const s of [-1, 1]) for (const dx of [0, 0.3]) m.bar(D, [3.4 - dx, s * 0.5, 0.14], [3.05 - dx, s * 0.64, 0.1], 0.09, 0.09);

  const wp = (s, k, up) => (up ? gr : lg);
  const W0 = { le: [0.6, 0.6, -0.42], c: 2.5, t: 0.14, cam: 0.02 }, W1 = { le: [0.45, 2.8, -0.36], c: 2.0, t: 0.12, cam: 0.02 }, W2 = { le: [0.22, 5.3, -0.22], c: 1.15, t: 0.1 }, W3 = { le: [0.17, 5.56, -0.2], c: 0.8, t: 0.06 };
  m.foil(Gy, [W0, W1, W2, W3], { mirror: true, paint: wp, np: 12 });
  m.foil(Gy, [{ le: [-5.1, 0, 0.3], c: 1.5, t: 0.09 }, { le: [-5.55, 1.0, 0.3], c: 1.1, t: 0.08 }, { le: [-5.9, 2.05, 0.3], c: 0.65, t: 0.06 }], { mirror: true, paint: wp });
  const F0 = { le: [-3.6, 0, 0.45], c: 2.7, t: 0.1, r: -Math.PI / 2 }, F1 = { le: [-5.0, 0, 1.5], c: 1.7, t: 0.08, r: -Math.PI / 2 }, F2 = { le: [-5.9, 0, 2.55], c: 1.0, t: 0.06, r: -Math.PI / 2 };
  m.foil(Gy, [F0, F1, F2], { paint: () => gr });
  m.foil(Gy, [{ le: [-2.3, 0, 0.5], c: 1.4, t: 0.05, r: -Math.PI / 2 }, { le: [-3.5, 0, 0.95], c: 0.9, t: 0.04, r: -Math.PI / 2 }], { paint: () => gr });
  m.surface('flap', c.camo, seg(W0, W1, 0.12, 0.95), 0.3, () => [['flap', -0.5]], { paint: wp });
  m.surface('aileron', c.camo, seg(W1, W2, 0.1, 0.92), 0.3, (s) => [['da', -0.35 * s]], { paint: wp });
  m.surface('elevator', c.camo, seg({ le: [-5.1, 0, 0.3], c: 1.5, t: 0.09 }, { le: [-5.9, 2.05, 0.3], c: 0.65, t: 0.06 }, 0.05, 0.97), 0.36, () => [['de', 0.45]], { paint: wp });
  m.surface('rudder', c.camo, seg(F0, F2, 0.05, 0.98), 0.32, () => [['dr', 0.45]], { vert: true, single: true, sides: [1], paint: () => gr });
  for (const s of [-1, 1]) {
    m.lathe(Wt, [0.1, s * 5.62, -0.2], 'x', [[0.5, 0], [0.42, 0.04], [0.3, 0.06], [-0.3, 0.06], [-0.5, 0]], 10);
    m.bar(SG, [0.5, s * 2.2, -0.45], [0.0, s * 2.2, -0.62], 0.06, 0.5, [0, 1, 0]);
    m.lathe(SG, [0.1, s * 2.2, -0.8], 'x', [[1.2, 0], [1.05, 0.1], [0.7, 0.17], [-0.7, 0.17], [-1.1, 0.1], [-1.25, 0]], 14);
  }

  const cx = 4.42;
  m.pivots.prop = [cx, 0, 0];
  m.pivots.disc = [cx, 0, 0];
  const P = m.grp('prop', c.dark), Ps = m.grp('prop', c.metal), Pt = m.grp('prop', c.red);
  m.lathe(Ps, [cx, 0, 0], 'x', [[0, 0.27], [0.1, 0.25], [0.25, 0.17], [0.42, 0]], 22);
  for (let k = 0; k < 5; k++) {
    const a = 2 * Math.PI * k / 5, uy = Math.sin(a), uz = Math.cos(a), vy = Math.cos(a), vz = -Math.sin(a);
    const pt = (r, w, dx) => [[-1, -1], [1, -1], [1, 1], [-1, 1]].map(([sx, sw]) => [cx + 0.07 + dx + sx * 0.012, r * uy + sw * w / 2 * vy, r * uz + sw * w / 2 * vz]);
    m.hexa(P, [...pt(0.2, 0.2, 0), ...pt(1.12, 0.13, 0.07)], 0x1b1b1b);
    m.hexa(Pt, [...pt(1.12, 0.13, 0.07), ...pt(1.18, 0.125, 0.075)], 0xb71c1c);
  }
  m.lathe(m.grp('disc', c.blur), [cx + 0.07, 0, 0], 'x', [[-0.002, 1.18], [0.002, 1.18]], 44);

  const Gr = m.grp('gear_nose', c.metal), T = m.grp('gear_nose', c.tire), Hb = m.grp('gear_nose', c.grey);
  m.gearLayer('gear_nose', [2.6, 0, -0.5], [0, 1, 0], [['gear', -1.5]]);
  m.bar(Gr, [2.6, 0, -0.5], [2.65, 0, -1.17], 0.08);
  gearWheel(m, Gr, T, Hb, [2.65, 0, -1.19], 0.16, 0.1);
  for (const s of [-1, 1]) {
    const ly = s > 0 ? 'gear_mL' : 'gear_mR', Gr = m.grp(ly, c.metal), T = m.grp(ly, c.tire), Hb = m.grp(ly, c.grey);
    m.gearLayer(ly, [-0.3, s * 0.7, -0.45], [1, 0, 0], [['gear', -1.4 * s]]);
    m.bar(Gr, [-0.3, s * 0.7, -0.45], [-0.3, s * 1.28, -1.1], 0.1);
    gearWheel(m, Gr, T, Hb, [-0.3, s * 1.3, -1.12], 0.23, 0.14);
  }
  return m;
}

function a320() {
  const m = b737({ navy: 0x0b4aa8 });
  m.xform((p) => [p[0] * 0.95, p[1] * 0.955, p[2]]);
  return m;
}

const BUILD = { c172: cessna, b737, gripen, f16, a320, tucano };
const meta = {};
function glbUrl(id) {
  const m = BUILD[id]();
  meta[id] = Object.fromEntries(Object.entries(m.hinges).map(([k, h]) => [k, { axis: [h.axis[1], h.axis[2], h.axis[0]], terms: h.terms }]));
  const bytes = m.toGlb();
  return URL.createObjectURL(new Blob([bytes], { type: 'model/gltf-binary' }));
}
const out = { BUILD, glbUrl, meta };
if (typeof module !== 'undefined') module.exports = out; else root.Models = out;
})(typeof self !== 'undefined' ? self : globalThis);
