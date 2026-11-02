(function (root) {
const TZ = 14, N = 65;
const TILE = (z, x, y) => `https://s3.amazonaws.com/elevation-tiles-prod/terrarium/${z}/${x}/${y}.png`;
const cache = new Map();
const smooth = (a, b, x) => { const t = Math.min(1, Math.max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t); };

function tile(z, x, y) {
  const k = `${z}/${x}/${y}`;
  if (!cache.has(k)) {
    cache.set(k, (async () => {
      const r = await fetch(TILE(z, x, y));
      if (!r.ok) throw new Error('terrain HTTP ' + r.status);
      const bmp = await createImageBitmap(await r.blob(), { colorSpaceConversion: 'none' });
      const ctx = new OffscreenCanvas(256, 256).getContext('2d');
      ctx.drawImage(bmp, 0, 0);
      const d = ctx.getImageData(0, 0, 256, 256).data, h = new Float32Array(65536);
      for (let i = 0; i < 65536; i++) h[i] = d[i * 4] * 256 + d[i * 4 + 1] + d[i * 4 + 2] / 256 - 32768;
      return h;
    })());
    if (cache.size > 400) cache.delete(cache.keys().next().value);
  }
  return cache.get(k);
}

function bilinear(h, u, v) {
  const fx = Math.min(255, Math.max(0, u * 255)), fy = Math.min(255, Math.max(0, v * 255));
  const x0 = Math.floor(fx), y0 = Math.floor(fy), x1 = Math.min(255, x0 + 1), y1 = Math.min(255, y0 + 1), tx = fx - x0, ty = fy - y0;
  const a = h[y0 * 256 + x0] * (1 - tx) + h[y0 * 256 + x1] * tx, b = h[y1 * 256 + x0] * (1 - tx) + h[y1 * 256 + x1] * tx;
  return a * (1 - ty) + b * ty;
}

const mercY = (lat) => (1 - Math.log(Math.tan(lat * Math.PI / 180) + 1 / Math.cos(lat * Math.PI / 180)) / Math.PI) / 2;

async function height(lon, lat) {
  const n = 1 << TZ, fx = (lon + 180) / 360 * n, fy = mercY(lat) * n;
  const x = Math.floor(fx), y = Math.floor(fy);
  return Math.max(0, bilinear(await tile(TZ, x, y), fx - x, fy - y));
}

function provider(C, flat) {
  return new C.CustomHeightmapTerrainProvider({
    tilingScheme: new C.WebMercatorTilingScheme(), width: N, height: N,
    callback: async (x, y, level) => {
      const out = new Float32Array(N * N);
      try {
        const Z = Math.min(level, TZ), s = 2 ** (level - Z), sx = Math.floor(x / s), sy = Math.floor(y / s);
        const h = await tile(Z, sx, sy), fx0 = x / s - sx, fy0 = y / s - sy, size = 1 / s, n = 2 ** level;
        const cl = Math.cos(flat.lat * Math.PI / 180);
        for (let j = 0; j < N; j++) {
          const ny = (y + j / (N - 1)) / n, lat = Math.atan(Math.sinh(Math.PI * (1 - 2 * ny))) * 180 / Math.PI;
          for (let i = 0; i < N; i++) {
            let v = Math.max(0, bilinear(h, fx0 + size * i / (N - 1), fy0 + size * j / (N - 1)));
            const lon = (x + i / (N - 1)) / n * 360 - 180;
            const d = Math.hypot((lon - flat.lon) * cl * 111320, (lat - flat.lat) * 110540);
            const t = smooth(flat.r0, flat.r1, d);
            out[j * N + i] = flat.h + (v - flat.h) * t;
          }
        }
      } catch { out.fill(flat.h); }
      return out;
    },
  });
}

async function overpass(q) {
  let err;
  for (const u of ['https://overpass.kumi.systems/api/interpreter', 'https://overpass-api.de/api/interpreter']) {
    try {
      const r = await fetch(u, { method: 'POST', body: 'data=' + encodeURIComponent(q), signal: AbortSignal.timeout(75000) });
      if (r.ok) return await r.json();
      err = new Error('Overpass HTTP ' + r.status);
    } catch (e) { err = e; }
  }
  throw err;
}

async function cached(key, fetcher) {
  let c = null;
  try { c = await caches.open('fs2-osm'); const hit = await c.match('/osm/' + key); if (hit) return await hit.json(); } catch {}
  const data = await fetcher();
  try { await c?.put('/osm/' + key, new Response(JSON.stringify(data))); } catch {}
  return data;
}

async function buildings(C, scene, lat, lon, base, radius = 2500) {
  const data = await cached(`${lat.toFixed(3)},${lon.toFixed(3)},${radius}`, () => overpass(`[out:json][timeout:60];way["building"](around:${radius},${lat},${lon});out geom;`));
  const cl = Math.cos(lat * Math.PI / 180);
  const ways = data.elements.filter((w) => w.geometry && w.geometry.length >= 4)
    .map((w) => ({ w, d: Math.hypot((w.geometry[0].lon - lon) * cl, w.geometry[0].lat - lat) }))
    .sort((a, b) => a.d - b.d).slice(0, 14000).map((e) => e.w);
  const tones = [0xd9d3c7, 0xc9c4bb, 0xe4ddd0, 0xb9b3aa, 0xcfc8bd, 0xa9a49b];
  let made = 0, prim = [];
  const list = [];
  const flush = () => {
    if (!prim.length) return;
    list.push(scene.primitives.add(new C.Primitive({ geometryInstances: prim, appearance: new C.PerInstanceColorAppearance({ closed: true, translucent: false }), asynchronous: true, releaseGeometryInstances: true })));
    prim = [];
  };
  for (const w of ways) {
    const t = w.tags || {};
    const hgt = parseFloat(t.height) || (parseFloat(t['building:levels']) * 3.2) || 6 + (w.id % 7);
    const pts = w.geometry.slice(0, -1).flatMap((p) => [p.lon, p.lat]);
    try {
      prim.push(new C.GeometryInstance({
        geometry: new C.PolygonGeometry({
          polygonHierarchy: new C.PolygonHierarchy(C.Cartesian3.fromDegreesArray(pts)), height: base - 0.5, extrudedHeight: base + Math.min(hgt, 150),
          vertexFormat: C.PerInstanceColorAppearance.VERTEX_FORMAT,
        }),
        attributes: { color: C.ColorGeometryInstanceAttribute.fromColor(C.Color.fromCssColorString('#' + tones[w.id % tones.length].toString(16).padStart(6, '0'))) },
      }));
    } catch { continue; }
    if (++made % 400 === 0) flush();
  }
  flush();
  return list;
}

root.World = { height, provider, buildings };
})(typeof self !== 'undefined' ? self : globalThis);
