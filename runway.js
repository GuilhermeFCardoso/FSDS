(function (root) {
const RE = 6371000, D2R = Math.PI / 180;
const DIGITS = { 0: 'abcdef', 1: 'bc', 2: 'abged', 3: 'abgcd', 4: 'fgbc', 5: 'afgcd', 6: 'afgedc', 7: 'abc', 8: 'abcdefg', 9: 'abcdfg' };
const PAPI_ANGLES = [2.5, 2.83, 3.17, 3.5];
const WHITE = [1, 1, 1], RED = [1, 0.12, 0.08], GREEN = [0.1, 1, 0.3];

function create(C, scene, loc, ref) {
  const h = loc.hdg * D2R, ch = Math.cos(h), sh = Math.sin(h);
  const W = loc.rwy.wid, len = loc.rwy.len, u0 = loc.rwy.thr;
  const lat0 = loc.lat * D2R, lon0 = loc.lon * D2R;
  const toLL = (u, v) => {
    const N = u * ch - v * sh, E = u * sh + v * ch;
    return [(lon0 + E / (RE * Math.cos(lat0))) / D2R, (lat0 + N / RE) / D2R];
  };
  const at = (end, a, b) => (end === 0 ? [u0 + a, b] : [u0 + len - a, -b]);
  const zPaint = ref + 0.22, zLight = ref + 0.5;
  const paint = [];
  const rect = (u1, v1, u2, v2, color = C.Color.WHITE) => {
    const pts = [[u1, v1], [u2, v1], [u2, v2], [u1, v2]].flatMap(([u, v]) => toLL(u, v));
    paint.push(new C.GeometryInstance({
      geometry: new C.PolygonGeometry({ polygonHierarchy: new C.PolygonHierarchy(C.Cartesian3.fromDegreesArray(pts)), height: zPaint, vertexFormat: C.PerInstanceColorAppearance.FLAT_VERTEX_FORMAT }),
      attributes: { color: C.ColorGeometryInstanceAttribute.fromColor(color) },
    }));
  };
  const endRect = (end, a1, b1, a2, b2) => {
    const [u1, v1] = at(end, a1, b1), [u2, v2] = at(end, a2, b2);
    rect(Math.min(u1, u2), Math.min(v1, v2), Math.max(u1, u2), Math.max(v1, v2));
  };

  const dw = 4.4, dh = 9, dt = 1.2;
  const seg = {
    a: [0, dh - dt, dw, dh], b: [dw - dt, dh / 2, dw, dh], c: [dw - dt, 0, dw, dh / 2], d: [0, 0, dw, dt],
    e: [0, 0, dt, dh / 2], f: [0, dh / 2, dt, dh], g: [0, dh / 2 - dt / 2, dw, dh / 2 + dt / 2],
  };
  const stripes = W >= 45 ? 12 : W >= 30 ? 8 : W >= 23 ? 6 : 4;
  const side = W * 0.17, tdz = W >= 30 ? 8 : 5.5;

  for (let end = 0; end < 2; end++) {
    const step = (W - 5) / (stripes - 1);
    for (let i = 0; i < stripes; i++) endRect(end, 6, -(W - 5) / 2 + i * step - 0.9, 36, -(W - 5) / 2 + i * step + 0.9);
    const num = String(end === 0 ? loc.rwy.num : loc.rwy.rev).padStart(2, '0');
    const total = 2 * dw + 1.8;
    [...num].forEach((dg, k) => {
      const x0 = -total / 2 + k * (dw + 1.8);
      for (const s of DIGITS[dg]) {
        const [x1, y1, x2, y2] = seg[s];
        endRect(end, 46 + y1, x0 + x1, 46 + y2, x0 + x2);
      }
    });
    const aim = Math.min(300, len * 0.25);
    for (const sg of [-1, 1]) {
      endRect(end, aim, sg * side, aim + 45, sg * (side + 3.2));
      endRect(end, 150, sg * tdz, 172, sg * (tdz + 1.8));
      if (len > 1200) endRect(end, 450, sg * tdz, 472, sg * (tdz + 1.8));
    }
  }
  for (let u = u0 + 70; u + 30 < u0 + len - 70; u += 50) rect(u, -0.45, u + 30, 0.45);
  for (const sg of [-1, 1]) rect(u0, sg * (W / 2 - 0.9) - 0.25, u0 + len, sg * (W / 2 - 0.9) + 0.25);

  const prims = [scene.primitives.add(new C.Primitive({
    geometryInstances: paint, appearance: new C.PerInstanceColorAppearance({ flat: true, translucent: false, closed: false }), asynchronous: false,
  }))];

  const lights = scene.primitives.add(new C.PointPrimitiveCollection());
  prims.push(lights);
  const near = new C.NearFarScalar(200, 1.3, 15000, 0.75);
  const mk = (u, v, rgb, size = 5) => {
    const [lon, lat] = toLL(u, v);
    const c = new C.Color(rgb[0], rgb[1], rgb[2], 1);
    return lights.add({
      id: 'fx', position: C.Cartesian3.fromDegrees(lon, lat, zLight), color: c, pixelSize: size, outlineColor: c.withAlpha(0.28), outlineWidth: 3,
      scaleByDistance: near, disableDepthTestDistance: 900,
    });
  };
  for (let a = 0; a <= len; a += 60) for (const sg of [-1, 1]) mk(u0 + a, sg * (W / 2 + 1.5), WHITE);
  const ends = [[], []];
  for (let end = 0; end < 2; end++) {
    for (let b = -(W / 2 + 2); b <= W / 2 + 2.01; b += 3) {
      const [u, v] = at(end, -0.6, b);
      ends[end].push(mk(u, v, GREEN, 5.5));
    }
    for (let a = 30; a <= 420; a += 30) mk(...at(end, -a, 0), WHITE, a % 90 === 0 ? 6 : 4.5);
    for (let b = -15; b <= 15; b += 3) if (b) mk(...at(end, -300, b), WHITE, 4.5);
  }
  const papi = [[], []];
  for (let end = 0; end < 2; end++) {
    for (let i = 0; i < 4; i++) {
      const [u, v] = at(end, 300, -(W / 2 + 14 + 8 * i));
      papi[end].push(mk(u, v, RED, 9));
    }
  }

  const loc2 = (lat, lon) => {
    const N = (lat - lat0) * RE, E = (lon - lon0) * RE * Math.cos(lat0);
    return [N * ch + E * sh, -N * sh + E * ch];
  };
  const setRgb = (p, rgb, show = true) => {
    p.color = new C.Color(rgb[0], rgb[1], rgb[2], 1);
    p.outlineColor = p.color.withAlpha(0.28);
    p.show = show;
  };

  return {
    prims,
    update(lat, lon, agl) {
      const [up, vp] = loc2(lat, lon);
      for (let end = 0; end < 2; end++) {
        const ap = end === 0 ? up - u0 : u0 + len - up, bp = end === 0 ? vp : -vp;
        const col = ap < 0 ? GREEN : RED;
        for (const p of ends[end]) setRgb(p, col);
        PAPI_ANGLES.forEach((ang, i) => {
          const dist = 300 - ap;
          const vis = dist > 40 && dist < 25000 && Math.abs(bp) < dist * 0.8 + 300;
          setRgb(papi[end][i], Math.atan2(agl, dist) / D2R > ang ? WHITE : RED, vis);
        });
      }
    },
    guide(lat, lon, agl, hdgDeg) {
      const [up, vp] = loc2(lat, lon);
      let best = null;
      for (let end = 0; end < 2; end++) {
        const ap = end === 0 ? up - u0 : u0 + len - up, bp = end === 0 ? vp : -vp;
        if (ap < -20 && ap > -20000 && Math.abs(bp) < -ap * 1.2 + 200 && (!best || -ap < best.d)) best = { end, d: -ap, b: bp };
      }
      if (!best) return '';
      const num = best.end === 0 ? loc.rwy.num : loc.rwy.rev;
      const slope = Math.atan2(agl, best.d + 300) / D2R;
      const lateral = Math.abs(best.b) < 5 ? 'alinhado' : `${Math.round(Math.abs(best.b))} m à ${best.b > 0 ? 'direita' : 'esquerda'}`;
      return `pista ${String(num).padStart(2, '0')}: ${(best.d / 1000).toFixed(1)} km · ${lateral} · rampa ${slope.toFixed(1)}° (ideal 3,0°)`;
    },
    remove() { for (const p of prims) scene.primitives.remove(p); },
  };
}

root.Runway = { create };
})(typeof self !== 'undefined' ? self : globalThis);
