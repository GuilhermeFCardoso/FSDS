(function (root) {
const RE = 6371000, TAU = Math.PI * 2;
const rnd = (a, b) => a + Math.random() * (b - a);
const lerp = (a, b, t) => a + (b - a) * t;

const BOXES = {
  hangar: [[0, 0, 4.5, 30, 18, 9, '#6f7a68'], [0, 0, 9.4, 28, 12, 1.6, '#566052'], [-15.1, 0, 3.5, 0.3, 11, 6.5, '#23282b']],
  fuel: [[-7.5, 0, 3.5, 11, 11, 7, '#d8d6cf'], [7.5, 0, 3.5, 11, 11, 7, '#d8d6cf'], [0, 11, 3.5, 11, 11, 7, '#cfcdc4'], [0, 0, 0.6, 30, 28, 1.2, '#55595c']],
  radar: [[0, 0, 1.5, 4, 4, 3, '#6a7367'], [0, 0, 5, 1, 1, 4, '#8b9087'], [0, 0, 8.2, 9, 0.8, 5, '#9aa09a', 0.35]],
  sam: [[0, 0, 1.3, 9, 3.2, 2.2, '#5f6a57'], [-1.5, 0, 3.7, 7, 0.9, 0.9, '#7b8374', -0.6], [0, 1.2, 3.7, 7, 0.9, 0.9, '#7b8374', -0.6], [0, -1.2, 3.7, 7, 0.9, 0.9, '#7b8374', -0.6]],
  aaa: [[0, 0, 0.7, 4.4, 3.2, 1.4, '#58604f'], [0, 0, 1.8, 2.2, 2.2, 1, '#4a5143'], [1.6, 0.5, 2.8, 4, 0.22, 0.22, '#2c2f2a', -0.5], [1.6, -0.5, 2.8, 4, 0.22, 0.22, '#2c2f2a', -0.5]],
  tank: [[0, 0, 1, 7, 3.4, 1.7, '#5c6450'], [-0.3, 0, 2.2, 3.4, 2.5, 1, '#525a47'], [3.4, 0, 2.4, 4.4, 0.3, 0.3, '#2c2f2a']],
  truck: [[0, 0, 1.7, 6.4, 2.4, 2.4, '#6b6f56'], [2.4, 0, 3.1, 1.6, 2.3, 1.2, '#585c47']],
};

function puffTexture() {
  const c = document.createElement('canvas');
  c.width = c.height = 128;
  const g = c.getContext('2d');
  let seed = 7;
  const r = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
  for (let i = 0; i < 70; i++) {
    const a = r() * 6.283, d = Math.sqrt(r()) * 38, x = 64 + Math.cos(a) * d, y = 64 + Math.sin(a) * d, rad = 9 + r() * 15;
    const grd = g.createRadialGradient(x, y, 0, x, y, rad);
    grd.addColorStop(0, `rgba(255,255,255,${0.32 + r() * 0.3})`); grd.addColorStop(0.65, 'rgba(255,255,255,.14)'); grd.addColorStop(1, 'rgba(255,255,255,0)');
    g.fillStyle = grd; g.beginPath(); g.arc(x, y, rad, 0, 6.283); g.fill();
  }
  return c.toDataURL();
}

const cyl = (x, len, top, bot, col) => ({ t: 'c', x, len, top, bot, col });
const box = (x, y, z, sx, sy, sz, col) => ({ t: 'b', x, y, z, sx, sy, sz, col });
const STORE_R = { aam: 0.07, bomb: 0.2, pod: 0.21 };
const fins = (x, sx, sp, col) => [box(x, 0, 0, sx, sp, 0.02, col), box(x, 0, 0, sx, 0.02, sp, col)];
const STORE = {
  bomb: [cyl(0, 2.1, 0.2, 0.2, '#4b5540'), cyl(1.42, 0.74, 0.035, 0.2, '#4b5540'), cyl(-1.35, 0.6, 0.2, 0.07, '#404836'), cyl(0.92, 0.1, 0.205, 0.205, '#d8b42a'), ...fins(-1.55, 0.5, 0.72, '#39402f')],
  aam: [cyl(0, 2.7, 0.07, 0.07, '#dcdfdb'), cyl(1.5, 0.3, 0.012, 0.07, '#26292b'), cyl(1.0, 0.07, 0.072, 0.072, '#d8b42a'), ...fins(0.95, 0.2, 0.44, '#c3c6c2'), ...fins(-1.1, 0.34, 0.66, '#c3c6c2')],
  rkt: [cyl(0, 1.15, 0.04, 0.04, '#cfd2cd'), cyl(0.67, 0.2, 0.005, 0.04, '#5a5d5a'), ...fins(-0.5, 0.14, 0.2, '#9fa29d')],
  pod: [cyl(0, 1.8, 0.21, 0.21, '#515c49'), cyl(1.02, 0.25, 0.1, 0.21, '#454f3e'), cyl(-1.0, 0.2, 0.21, 0.13, '#454f3e')],
  pylon: [box(0, 0, 0.5, 1.1, 0.07, 1, '#3b4046')],
};

function create(C, scene, fr) {
  const sizeInMeters = 'sizeInMeters' in C.Billboard.prototype;
  const world = (p, out = new C.Cartesian3()) => C.Cartesian3.fromRadians(fr.lon0 + p[0] / (RE * Math.cos(fr.lat0)), fr.lat0 + p[1] / RE, p[2] - fr.elev + fr.ref, undefined, out);
  const fx = { world, shake: 0, camPos: null, fires: [], t: 0 };

  const bills = scene.primitives.add(new C.BillboardCollection({ scene }));
  const url = puffTexture(), pool = [];
  for (let i = 0; i < 900; i++) {
    const b = bills.add({ id: 'fx', image: url, show: false, scale: 1, sizeInMeters, width: 64, height: 64, disableDepthTestDistance: 0 });
    pool.push({ b, on: false, col: new C.Color(1, 1, 1, 1), pos: new C.Cartesian3(), x: 0, y: 0, z: 0, vx: 0, vy: 0, vz: 0, age: 0, life: 1, s0: 1, s1: 1, c0: [1, 1, 1, 1], c1: [1, 1, 1, 0], drag: 0, grav: 0 });
  }
  let cursor = 0;
  fx.emit = (x, y, z, vx, vy, vz, life, s0, s1, c0, c1, drag = 0, grav = 0) => {
    for (let k = 0; k < pool.length; k++) {
      const p = pool[(cursor + k) % pool.length];
      if (p.on) continue;
      cursor = (cursor + k + 1) % pool.length;
      Object.assign(p, { on: true, x, y, z, vx, vy, vz, age: 0, life, s0, s1, c0, c1, drag, grav });
      p.b.show = true; p.b.rotation = Math.random() * 6.283;
      return p;
    }
    return null;
  };

  const FIRE0 = [1, 0.95, 0.65, 1], FIRE1 = [1, 0.35, 0.05, 0], SMK0 = [0.18, 0.17, 0.17, 0.85], SMK1 = [0.5, 0.5, 0.52, 0], FLASH = [1, 1, 0.85, 1];
  const camDist = (x, y, z) => {
    if (!fx.camPos) return 1e9;
    const w = world([x, y, z], tmpW);
    return C.Cartesian3.distance(w, fx.camPos);
  };
  const tmpW = new C.Cartesian3();

  fx.boom = (pos, size = 1, gnd = false) => {
    const [x, y, z] = pos;
    fx.emit(x, y, z, 0, 0, 0, 0.22, 12 * size, 55 * size, FLASH, [1, 0.7, 0.2, 0]);
    for (let i = 0; i < 9; i++) fx.emit(x, y, z, rnd(-12, 12) * size, rnd(-12, 12) * size, rnd(-3, 14) * size, rnd(0.7, 1.5), 8 * size, rnd(24, 38) * size, FIRE0, FIRE1, 1.2, -2);
    for (let i = 0; i < 12; i++) fx.emit(x, y, z, rnd(-8, 8) * size, rnd(-8, 8) * size, rnd(2, 12) * size, rnd(3.5, 7), 10 * size, rnd(28, 55) * size, SMK0, SMK1, 0.6, -2.5);
    for (let i = 0; i < 10; i++) fx.emit(x, y, z, rnd(-60, 60), rnd(-60, 60), rnd(5, 70), rnd(0.5, 1.3), 1.1, 0.4, FLASH, [1, 0.5, 0.1, 0], 0.4, 14);
    const d = camDist(x, y, z);
    fx.shake = Math.max(fx.shake, Math.min(1, size * 0.5 * (1 - d / 3500)));
    if (gnd && size > 1.5) fx.fires.push({ x, y, z, until: fx.t + 40 + size * 12, k: size });
    return d;
  };
  fx.spark = (pos, gnd) => {
    const [x, y, z] = pos;
    for (let i = 0; i < 3; i++) fx.emit(x, y, z, rnd(-18, 18), rnd(-18, 18), rnd(0, 22), rnd(0.15, 0.4), 0.9, 0.3, FLASH, [1, 0.5, 0.1, 0], 0.5, 18);
    fx.emit(x, y, z, 0, 0, 2, 0.12, 2.5, 3.5, FLASH, [1, 0.6, 0.2, 0]);
    if (gnd) fx.emit(x, y, z, 0, 0, 3, 1.5, 3, 9, [0.25, 0.23, 0.22, 0.6], SMK1, 1, -1);
  };
  fx.dust = (pos) => {
    const [x, y, z] = pos;
    for (let i = 0; i < 2; i++) fx.emit(x, y, z + 0.5, rnd(-3, 3), rnd(-3, 3), rnd(2, 6), rnd(0.8, 1.6), 1.6, 6, [0.55, 0.47, 0.36, 0.7], [0.55, 0.47, 0.36, 0], 0.8, -1);
  };
  fx.puffAt = (pos, s0, s1, life, c0, c1, v = [0, 0, 0]) => fx.emit(pos[0], pos[1], pos[2], v[0], v[1], v[2], life, s0, s1, c0, c1, 0.4, -0.5);
  fx.smoke = (pos, k = 1) => fx.emit(pos[0], pos[1], pos[2], rnd(-1, 1), rnd(-1, 1), rnd(0, 2), rnd(2.5, 4) * k, 2.2, 11 * k, SMK0, SMK1, 0.2, -0.8);
  fx.flame = (pos, k = 1) => fx.emit(pos[0], pos[1], pos[2], rnd(-1, 1), rnd(-1, 1), rnd(0, 3), rnd(0.25, 0.5), 3.2 * k, 1.2 * k, FIRE0, FIRE1, 0.2, -3);

  fx.step = (dt) => {
    fx.t += dt;
    const cam = fx.camPos;
    for (const p of pool) {
      if (!p.on) continue;
      p.age += dt;
      if (p.age >= p.life) { p.on = false; p.b.show = false; continue; }
      const k = Math.exp(-p.drag * dt);
      p.vx *= k; p.vy *= k; p.vz = p.vz * k - p.grav * dt;
      p.x += p.vx * dt; p.y += p.vy * dt; p.z += p.vz * dt;
      const u = p.age / p.life, s = lerp(p.s0, p.s1, Math.sqrt(u)), b = p.b;
      world([p.x, p.y, p.z], p.pos);
      b.position = p.pos;
      const c0 = p.c0, c1 = p.c1, fade = u < 0.08 ? u / 0.08 : 1;
      p.col.red = lerp(c0[0], c1[0], u); p.col.green = lerp(c0[1], c1[1], u); p.col.blue = lerp(c0[2], c1[2], u); p.col.alpha = lerp(c0[3], c1[3], u) * fade;
      b.color = p.col;
      if (sizeInMeters) { b.width = s; b.height = s; }
      else { const d = cam ? C.Cartesian3.distance(p.pos, cam) : 1000; b.scale = Math.max(0.05, s / Math.max(d, 1) * (scene.drawingBufferHeight / 1.15) / 64); }
    }
    fx.fires = fx.fires.filter((f) => f.until > fx.t);
    for (const f of fx.fires) {
      if (Math.random() < dt * 14 * Math.min(f.k, 2)) fx.smoke([f.x + rnd(-3, 3), f.y + rnd(-3, 3), f.z + 1], 1.5);
      if (Math.random() < dt * 9) fx.flame([f.x + rnd(-2.5, 2.5), f.y + rnd(-2.5, 2.5), f.z + 0.5], 1.4);
    }
    fx.shake = Math.max(0, fx.shake - dt * 1.4);
  };

  const lines = scene.primitives.add(new C.PolylineCollection());
  const tracers = [];
  const cYel = C.Color.fromCssColorString('#fff2a0'), cRed = C.Color.fromCssColorString('#ff5a3c');
  const matYel = C.Material.fromType('Color', { color: cYel }), matRed = C.Material.fromType('Color', { color: cRed });
  const wa = new C.Cartesian3(), wb = new C.Cartesian3();
  fx.tracers = (bullets) => {
    let n = 0;
    for (const b of bullets) {
      if (!b.tr) continue;
      let l = tracers[n];
      if (!l) { l = tracers[n] = lines.add({ id: 'fx', width: 2.4, positions: [new C.Cartesian3(), new C.Cartesian3()], material: matYel }); }
      const tail = [b.p[0] - b.v[0] * 0.035, b.p[1] - b.v[1] * 0.035, b.p[2] - b.v[2] * 0.035];
      l.positions = [world(b.p, wa).clone(), world(tail, wb).clone()];
      l.material = b.team === 'blue' ? matYel : matRed;
      l.show = true; n++;
    }
    for (let i = n; i < tracers.length; i++) tracers[i].show = false;
  };

  const trailLines = scene.primitives.add(new C.PolylineCollection());
  const pts = scene.primitives.add(new C.PointPrimitiveCollection());
  const trails = new Map(), freeLines = [], ptsById = new Map();
  const HOT = new C.Color(1, 0.97, 0.85, 1), DIM = new C.Color(1, 0.55, 0.2, 1), HALO = new C.Color(1, 0.55, 0.15, 0.4);
  function trail(id, pos, step, max, width, col, live) {
    live.add(id);
    let t = trails.get(id);
    if (!t) {
      const line = freeLines.pop() || trailLines.add({ id: 'fx', positions: [], width, material: C.Material.fromType('Color', { color: new C.Color(col[0], col[1], col[2], col[3]) }) });
      line.show = true; line.width = width; line.material.uniforms.color = new C.Color(col[0], col[1], col[2], col[3]);
      t = { line, col, fade: 0, pts: [world(pos)], last: [...pos] };
      trails.set(id, t);
      return;
    }
    const head = world(pos);
    if (Math.hypot(pos[0] - t.last[0], pos[1] - t.last[1], pos[2] - t.last[2]) >= step) {
      t.pts.push(head); t.last = [...pos];
      if (t.pts.length > max) t.pts.shift();
    }
    t.line.positions = [...t.pts, head];
  }
  function point(id, pos, size, color, live) {
    live.add(id);
    let p = ptsById.get(id);
    if (!p) { p = pts.add({ id: 'fx', position: world(pos), pixelSize: size, color, outlineColor: HALO, outlineWidth: 2 }); ptsById.set(id, p); return; }
    p.position = world(pos); p.pixelSize = size; p.color = color;
  }

  const stores = { bomb: [], aam: [], rkt: [], pod: [], pylon: [] }, allStores = [], flying = new Map(), stationed = [];
  const hprS = new C.HeadingPitchRoll(), wS = new C.Cartesian3();
  function buildStore(kind) {
    const inst = STORE[kind].map((e) => new C.GeometryInstance({
      geometry: e.t === 'c'
        ? new C.CylinderGeometry({ length: e.len, topRadius: e.top, bottomRadius: e.bot, slices: 12, vertexFormat: C.PerInstanceColorAppearance.VERTEX_FORMAT })
        : C.BoxGeometry.fromDimensions({ dimensions: new C.Cartesian3(e.sx, e.sy, e.sz), vertexFormat: C.PerInstanceColorAppearance.VERTEX_FORMAT }),
      modelMatrix: e.t === 'c' ? C.Matrix4.fromRotationTranslation(C.Matrix3.fromRotationY(Math.PI / 2), new C.Cartesian3(e.x, 0, 0)) : C.Matrix4.fromTranslation(new C.Cartesian3(e.x, e.y, e.z)),
      attributes: { color: C.ColorGeometryInstanceAttribute.fromColor(C.Color.fromCssColorString(e.col)) },
    }));
    const p = scene.primitives.add(new C.Primitive({ geometryInstances: inst, appearance: new C.PerInstanceColorAppearance({ closed: true, translucent: false }), asynchronous: false, modelMatrix: new C.Matrix4() }));
    p.show = false; p.vis = false; p.used = false; p.mat = new C.Matrix4();
    allStores.push(p); stores[kind].push(p);
    return p;
  }
  const takeStore = (kind) => { const p = stores[kind].find((x) => !x.used) || buildStore(kind); p.used = true; return p; };
  function fly(id, kind, pos, dir, live) {
    live.add(id);
    let p = flying.get(id);
    if (!p) { p = takeStore(kind); flying.set(id, p); p.vis = true; p.show = true; }
    hprS.heading = Math.atan2(dir[0], dir[1]) - Math.PI / 2; hprS.pitch = Math.asin(Math.max(-1, Math.min(1, dir[2]))); hprS.roll = 0;
    C.Transforms.headingPitchRollToFixedFrame(world(pos, wS), hprS, undefined, undefined, p.mat);
    p.modelMatrix = p.mat;
  }
  const local = new C.Matrix4(), offV = new C.Cartesian3();
  fx.stores = (c, planeM, visible) => {
    const st = c.ac.weapons.stations;
    if (!stationed.length) {
      for (const kind of ['aam', 'bomb', 'pod']) st[kind].forEach((at, i) => {
        stationed.push({ kind, i, at, p: takeStore(kind), py: at[3] ? null : takeStore('pylon'), top: at[2] + STORE_R[kind] });
      });
    }
    const left = { aam: c.w.aam, bomb: c.w.bomb, pod: c.w.rkt > 0 ? 99 : 0 };
    for (const s of stationed) {
      s.p.vis = visible && s.i < left[s.kind]; s.p.show = s.p.vis;
      C.Cartesian3.fromElements(s.at[0], s.at[1], s.at[2], offV);
      C.Matrix4.multiplyByTranslation(planeM, offV, local);
      if (s.p.vis) { C.Matrix4.clone(local, s.p.mat); s.p.modelMatrix = s.p.mat; }
      if (s.py) {
        s.py.vis = visible; s.py.show = visible;
        if (visible) {
          C.Cartesian3.fromElements(s.at[0], s.at[1], s.top, offV);
          C.Matrix4.multiplyByTranslation(planeM, offV, s.py.mat);
          C.Matrix4.multiplyByScale(s.py.mat, C.Cartesian3.fromElements(1, 1, Math.max(0.05, st.wingZ - s.top), offV), s.py.mat);
          s.py.modelMatrix = s.py.mat;
        }
      }
    }
  };

  const urls = {}, ents = new Map(), hpr = new C.HeadingPitchRoll(), scl = new C.Cartesian3(), mtx = new C.Matrix4();
  async function loadJet(e) {
    const rec = ents.get(e.id);
    try {
      const u = urls[e.model] ||= Models.glbUrl(e.model);
      const m = await C.Model.fromGltfAsync({ url: u, incrementallyLoadTextures: false });
      m.color = C.Color.fromCssColorString(e.team === 'red' ? '#ff7a63' : '#6fb2ff');
      m.colorBlendMode = C.ColorBlendMode.MIX; m.colorBlendAmount = 0.4;
      scene.primitives.add(m);
      if (!ents.has(e.id)) { scene.primitives.remove(m); return; }
      rec.model = m;
      const flame = m.getNode?.('flame');
      if (flame) { rec.flame = flame; rec.flameM = flame.originalMatrix; }
      for (const name of Object.keys(Models.meta[e.model] ?? {})) if (name.startsWith('gear')) { const nd = m.getNode?.(name); if (nd) nd.show = false; }
      const gear = m.getNode?.('gear'); if (gear) gear.show = false;
    } catch (err) { rec.err = err; }
  }

  function gndPrim(g, wreck) {
    const inst = [];
    for (const [dx, dy, dz, sx, sy, sz, col, tilt = 0] of BOXES[g.kind]) {
      const k = wreck ? 0.45 : 1;
      inst.push(new C.GeometryInstance({
        geometry: C.BoxGeometry.fromDimensions({ dimensions: new C.Cartesian3(sx, sy, sz * k), vertexFormat: C.PerInstanceColorAppearance.VERTEX_FORMAT }),
        modelMatrix: C.Matrix4.fromRotationTranslation(C.Matrix3.fromRotationY(tilt), new C.Cartesian3(dx, dy, dz * k)),
        attributes: { color: C.ColorGeometryInstanceAttribute.fromColor(wreck ? C.Color.fromCssColorString('#262120') : C.Color.fromCssColorString(col)) },
      }));
    }
    return scene.primitives.add(new C.Primitive({ geometryInstances: inst, appearance: new C.PerInstanceColorAppearance({ closed: true, translucent: false }), asynchronous: false, modelMatrix: C.Matrix4.IDENTITY }));
  }
  const gndMatrix = (g, out) => {
    const w = world([g.pos[0], g.pos[1], g.pos[2] + 0.15], wa);
    C.Transforms.eastNorthUpToFixedFrame(w, undefined, out);
    return C.Matrix4.multiplyByMatrix3(out, C.Matrix3.fromRotationZ(g.yaw || 0), out);
  };

  fx.sync = (c, dt) => {
    const seen = new Set();
    for (const e of c.air) {
      seen.add(e.id);
      let rec = ents.get(e.id);
      if (!rec) { rec = { model: null }; ents.set(e.id, rec); loadJet(e); }
      if (rec.model) {
        const psi = Math.atan2(e.dir[0], e.dir[1]);
        hpr.heading = psi - Math.PI / 2; hpr.pitch = Math.asin(Math.max(-1, Math.min(1, e.dir[2]))); hpr.roll = e.roll;
        C.Transforms.headingPitchRollToFixedFrame(world(e.pos, wa), hpr, undefined, undefined, rec.model.modelMatrix);
        if (rec.flame) {
          const k = e.dying ? 0 : 0.18 + 0.3 * Math.min(1, e.spd / 300);
          rec.flame.show = k > 0;
          rec.flame.matrix = C.Matrix4.multiplyByScale(rec.flameM, C.Cartesian3.fromElements(0.8, 0.8, k, scl), mtx);
        }
      }
      const tail = [e.pos[0] - e.dir[0] * 8, e.pos[1] - e.dir[1] * 8, e.pos[2] - e.dir[2] * 8];
      if (e.dying) { if (Math.random() < dt * 40) { fx.flame(tail, 1.8); fx.smoke(tail, 1.6); } }
      else if (e.hp < 55 && Math.random() < dt * (e.hp < 30 ? 38 : 20)) fx.smoke(tail, 1);
    }
    for (const [id, rec] of ents) if (typeof id === 'number' && !seen.has(id)) { if (rec.model) scene.primitives.remove(rec.model); ents.delete(id); }

    for (const g of c.gnd) {
      let rec = ents.get('g' + g.id);
      if (!rec) { rec = { prim: gndPrim(g, !g.alive), alive: g.alive, mat: new C.Matrix4(), lastZ: NaN }; ents.set('g' + g.id, rec); rec.prim.modelMatrix = gndMatrix(g, rec.mat); }
      if (rec.alive && !g.alive) { scene.primitives.remove(rec.prim); rec.prim = gndPrim(g, true); rec.alive = false; rec.prim.modelMatrix = gndMatrix(g, rec.mat); fx.fires.push({ x: g.pos[0], y: g.pos[1], z: g.pos[2], until: fx.t + 70, k: g.kind === 'fuel' ? 2 : 1 }); }
      if (g.move || Math.abs(g.pos[2] - rec.lastZ) > 0.3) { rec.lastZ = g.pos[2]; rec.prim.modelMatrix = gndMatrix(g, rec.mat); }
    }

    const liveFly = new Set(), liveTrail = new Set(), livePts = new Set();
    for (const m of c.missiles) {
      fly(m.id, 'aam', m.pos, m.dir, liveFly);
      trail(m.id, m.pos, 16, 90, m.kind === 'sam' ? 4 : 3, [0.95, 0.95, 0.95, 0.75], liveTrail);
      if (m.age < 3.5) { point(m.id, m.pos, 5, HOT, livePts); const b = [m.pos[0] - m.dir[0] * 2, m.pos[1] - m.dir[1] * 2, m.pos[2] - m.dir[2] * 2]; fx.emit(b[0], b[1], b[2], 0, 0, 0, 0.05, 2.6, 1.2, FIRE0, FIRE1); }
    }
    for (const r of c.rockets) {
      const sp = Math.hypot(r.v[0], r.v[1], r.v[2]) || 1, dir = [r.v[0] / sp, r.v[1] / sp, r.v[2] / sp];
      fly(r.id, 'rkt', r.pos, dir, liveFly);
      trail(r.id, r.pos, 10, 40, 2, [0.9, 0.9, 0.9, 0.6], liveTrail);
      point(r.id, r.pos, 4, HOT, livePts);
    }
    for (const b of c.bombs) {
      const sp = Math.hypot(b.vel[0], b.vel[1], b.vel[2]) || 1;
      fly(b.id, 'bomb', b.pos, [b.vel[0] / sp, b.vel[1] / sp, b.vel[2] / sp], liveFly);
    }
    for (const f of c.flares) {
      const k = Math.min(1, f.life / 1.2);
      point(f.id, f.pos, 3 + 4 * k, k > 0.3 ? HOT : DIM, livePts);
      trail(f.id, f.pos, 7, 45, 2, [0.92, 0.92, 0.92, 0.55], liveTrail);
      fx.emit(f.pos[0], f.pos[1], f.pos[2], 0, 0, 0, 0.05, 1.5 * k, 1.5 * k, [1, 0.9, 0.65, 0.7], [1, 0.7, 0.3, 0]);
      if (Math.random() < dt * 22) fx.emit(f.pos[0], f.pos[1], f.pos[2], f.vel[0] + rnd(-5, 5), f.vel[1] + rnd(-5, 5), f.vel[2] + rnd(-6, 2), rnd(0.25, 0.6), 0.5, 0.2, FLASH, [1, 0.5, 0.1, 0], 0.4, 9);
    }
    for (const [id, p] of flying) if (!liveFly.has(id)) { p.used = false; p.vis = false; p.show = false; flying.delete(id); }
    for (const [id, pt] of ptsById) if (!livePts.has(id)) { pts.remove(pt); ptsById.delete(id); }
    for (const [id, t] of trails) if (!liveTrail.has(id)) {
      t.fade += dt / 3;
      if (t.fade >= 1) { t.line.show = false; freeLines.push(t.line); trails.delete(id); }
      else t.line.material.uniforms.color = new C.Color(t.col[0], t.col[1], t.col[2], t.col[3] * (1 - t.fade));
    }
    fx.tracers(c.bullets);
  };

  fx.setShow = (on) => {
    bills.show = on; lines.show = on; trailLines.show = on; pts.show = on;
    for (const p of allStores) p.show = on && p.vis;
    for (const r of ents.values()) { if (r.model) r.model.show = on; if (r.prim) r.prim.show = on; }
  };
  fx.prims = () => [bills, lines, trailLines, pts, ...allStores, ...[...ents.values()].map((r) => r.model || r.prim).filter(Boolean)];

  fx.clear = () => {
    for (const rec of ents.values()) { if (rec.model) scene.primitives.remove(rec.model); if (rec.prim) scene.primitives.remove(rec.prim); }
    ents.clear();
    for (const p of pool) { p.on = false; p.b.show = false; }
    fx.fires = []; fx.shake = 0;
    for (const p of allStores) { p.vis = false; p.used = false; p.show = false; }
    flying.clear(); stationed.length = 0; pts.removeAll(); ptsById.clear();
    for (const t of trails.values()) { t.line.show = false; freeLines.push(t.line); }
    trails.clear();
    for (const t of tracers) t.show = false;
  };
  fx.destroy = () => { fx.clear(); scene.primitives.remove(bills); scene.primitives.remove(lines); scene.primitives.remove(trailLines); scene.primitives.remove(pts); };
  return fx;
}

root.Fx = { create };
})(typeof self !== 'undefined' ? self : globalThis);
