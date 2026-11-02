'use strict';
const C = Cesium;
const $ = (id) => document.getElementById(id);
const clamp = (x, a, b) => Math.min(b, Math.max(a, x));
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const wrap = (a) => Math.atan2(Math.sin(a), Math.cos(a));

window.addEventListener('error', (e) => showError(e.message));
window.addEventListener('unhandledrejection', (e) => showError(String(e.reason?.stack || e.reason)));
function showError(msg) {
  let el = $('err');
  if (!el) { el = document.createElement('div'); el.id = 'err'; document.body.appendChild(el); }
  el.textContent += msg + '\n';
}

const store = {
  get: (k, d) => { try { const v = localStorage.getItem('fs2-' + k); return v === null ? d : JSON.parse(v); } catch { return d; } },
  set: (k, v) => { try { localStorage.setItem('fs2-' + k, JSON.stringify(v)); } catch {} },
};

const LOCS = {
  bacacheri: { name: 'Curitiba — Bacacheri (CINDACTA II)', info: 'Pista 15 · ~1.300 m · 920 m de altitude', lat: -25.4002498, lon: -49.2341712, elev: 920, hdg: 158, short: 'SBBI', imgMax: 19, rwy: { len: 1300, wid: 30, thr: -15, num: 15, rev: 33 } },
  casimiro: { name: 'Casimiro de Abreu — Aeródromo (SDCU)', info: 'Pista 11 · ~1.040 m x 18 m · ao nível do mar', lat: -22.5571, lon: -42.1197, elev: 6, hdg: 92, short: 'SDCU', imgMax: 18, rwy: { len: 1040, wid: 18, thr: -25, num: 11, rev: 29 } },
  custom: { name: 'Personalizado', info: 'Digite coordenadas e rumo da pista' },
};
const TAKEOFF_FLAP = { c172: 0, b737: 2, gripen: 1, f16: 1, a320: 2, tucano: 1 };
const CRUISE = { c172: 55, b737: 130, gripen: 180, f16: 200, a320: 130, tucano: 85 };

const cfg = {
  ac: store.get('ac', 'c172'), loc: store.get('loc', 'bacacheri'), start: store.get('start', 'runway'),
  fuel: store.get('fuel', 50), assist: store.get('assist', false), stallProt: store.get('stallProt', false), inst: store.get('inst', Instruments.list.filter((i) => i.on).map((i) => i.id)),
  wx: store.get('wx', 'live'), wxc: store.get('wxc', { dir: 90, kt: 8, gust: 12, tempC: 22, qnh: 1013, vis: 10000, cloud: 30 }), terrain: store.get('terrain', true), bld: store.get('bld', true),
  mode: store.get('mode', 'free'), diff: store.get('diff', 'normal'), invuln: store.get('invuln', false), aimAssist: store.get('aimAssist', false),
  gs: store.get('gs', 124), gkey: store.get('gkey', ''), ion: store.get('ion', ''), custom: store.get('custom', null),
};

const G = { viewer: null, scene: null, camera: null, plane: null, planeId: null, models: {}, ac: null, s: null, loc: null,
  ref: 0, lastGround: 0, groundMSL: 0, paused: true, started: false, cam: 'chase', zoom: 1, frame: 0, gmax: 1, invert: store.get('invert', false), hideInst: false,
  hold: false, holdVal: 0, padThr: false, sb: false, padYaw: 0, rumbleAt: 0, propAng: 0, head: { yaw: 0, pitch: 0 }, headAt: 0, fps: 60, sup: false, blk: 0, red: 0, locT: 0, fov: 0, wxCur: Weather.CALM, wxSeq: 0, terrain: false, bldList: [], bldKey: '', bldStatus: '' };
const RE = 6371000, D2R = Math.PI / 180;
const frame = { lat0: 0, lon0: 0, elev: 0, ref: 0 };
const ctl = { pitch: 0, roll: 0, yaw: 0, throttle: 0, trim: 0, brake: 0, parking: true, gearDown: true, flapIdx: 0, speedbrake: 0, assist: false, stallProt: false };
const keys = new Set();
const kb = { pitch: 0, roll: 0, yaw: 0 };
const prevBtn = [];
const horn = {};

function buildMenu() {
  const mk = (host, name, items, sel, onSel) => {
    host.innerHTML = '';
    for (const [id, it] of items) {
      const l = document.createElement('label');
      l.className = 'card';
      l.innerHTML = `<input type="radio" name="${name}" value="${id}" ${id === sel ? 'checked' : ''}><b>${it.name}</b><small>${it.desc ?? it.info}</small>`;
      l.querySelector('input').addEventListener('change', () => onSel(id));
      host.appendChild(l);
    }
  };
  mk($('acCards'), 'ac', Object.entries(AIRCRAFT), cfg.ac, (id) => { cfg.ac = id; modeNote(); });
  mk($('locCards'), 'loc', Object.entries(LOCS), cfg.loc, (id) => { cfg.loc = id; $('custom').hidden = id !== 'custom'; refreshWx(); });
  $('custom').hidden = cfg.loc !== 'custom';
  if (cfg.custom) for (const [k, id] of [['lat', 'cLat'], ['lon', 'cLon'], ['elev', 'cElev'], ['hdg', 'cHdg']]) $(id).value = cfg.custom[k];
  document.querySelector(`input[name=start][value=${cfg.start}]`).checked = true;
  $('fuel').value = cfg.fuel; $('fuelV').textContent = cfg.fuel + '%';
  $('fuel').oninput = () => { cfg.fuel = +$('fuel').value; $('fuelV').textContent = cfg.fuel + '%'; };
  mk($('modeCards'), 'mode', [['free', { name: 'Voo livre', desc: 'Sem armas nem inimigos.' }], ...Object.entries(Combat.MISSIONS)], cfg.mode, (id) => { cfg.mode = id; modeNote(); });
  $('diff').value = cfg.diff; $('invuln').checked = cfg.invuln; $('aimAssist').checked = cfg.aimAssist;
  $('assist').checked = cfg.assist;
  $('stallProt').checked = cfg.stallProt;
  $('invert').checked = G.invert;
  $('invert').onchange = () => { G.invert = $('invert').checked; store.set('invert', G.invert); };
  document.querySelector(`input[name=wx][value=${cfg.wx}]`).checked = true;
  for (const [k, id] of [['dir', 'wDir'], ['kt', 'wKt'], ['gust', 'wGust'], ['tempC', 'wTemp'], ['qnh', 'wQnh'], ['vis', 'wVis']]) {
    $(id).value = k === 'vis' ? cfg.wxc.vis / 1000 : cfg.wxc[k];
    $(id).oninput = refreshWx;
  }
  document.querySelectorAll('input[name=wx]').forEach((r) => (r.onchange = refreshWx));
  for (const id of ['cLat', 'cLon', 'cHdg']) $(id).addEventListener('change', refreshWx);
  $('terrain').checked = cfg.terrain; $('bld').checked = cfg.bld;
  modeNote();
  refreshWx();
  $('gkey').value = cfg.gkey; $('ion').value = cfg.ion;
  document.documentElement.style.setProperty('--gs', cfg.gs + 'px');
  $('gs').value = cfg.gs;
  $('gs').oninput = () => { cfg.gs = +$('gs').value; document.documentElement.style.setProperty('--gs', cfg.gs + 'px'); store.set('gs', cfg.gs); };
  const il = $('instList');
  il.innerHTML = '';
  for (const it of Instruments.list) {
    const l = document.createElement('label');
    l.className = 'chk';
    l.innerHTML = `<input type="checkbox" ${cfg.inst.includes(it.id) ? 'checked' : ''}> ${it.name}`;
    l.querySelector('input').addEventListener('change', (e) => {
      cfg.inst = e.target.checked ? [...cfg.inst, it.id] : cfg.inst.filter((x) => x !== it.id);
      store.set('inst', cfg.inst);
      Instruments.setEnabled(cfg.inst);
    });
    il.appendChild(l);
  }
  Instruments.mount($('inst'), cfg.inst);
}

function modeNote() {
  const armed = !!AIRCRAFT[cfg.ac].weapons;
  $('modeNote').textContent = cfg.mode === 'free' ? '' : armed ? 'Armamento: ' + AIRCRAFT[cfg.ac].weapons.gun.name + ', ' + [AIRCRAFT[cfg.ac].weapons.aam && AIRCRAFT[cfg.ac].weapons.aam + ' mísseis ar-ar', AIRCRAFT[cfg.ac].weapons.bomb + ' bombas', AIRCRAFT[cfg.ac].weapons.rkt + ' foguetes', AIRCRAFT[cfg.ac].weapons.sight.name].filter(Boolean).join(', ') + '.' : '⚠ Esta aeronave não tem armamento. Escolha o F-16, o Gripen ou o Super Tucano.';
}

const wxCache = {};
async function refreshWx() {
  const seq = ++G.wxSeq, box = $('wxBox'), mode = document.querySelector('input[name=wx]:checked').value;
  $('wxCustom').hidden = mode !== 'custom';
  const loc = currentLoc();
  let wx;
  if (mode === 'calm') wx = { ...Weather.CALM };
  else if (mode === 'custom') {
    const v = (id) => +$(id).value || 0;
    cfg.wxc = { dir: v('wDir'), kt: v('wKt'), gust: v('wGust'), tempC: v('wTemp'), qnh: v('wQnh') || 1013, vis: v('wVis') * 1000 || 10000, cloud: 30 };
    wx = { ...cfg.wxc, desc: 'Condições definidas por você' };
  } else {
    box.textContent = 'Buscando condições ao vivo…';
    const key = loc.lat.toFixed(2) + ',' + loc.lon.toFixed(2);
    try { wx = wxCache[key] ||= await Weather.live(loc.lat, loc.lon); }
    catch { wx = { ...Weather.CALM, desc: 'Sem conexão com o serviço de clima: usando condições calmas' }; }
  }
  if (seq !== G.wxSeq) return;
  cfg.wx = mode; G.wxCur = wx; box.innerHTML = Weather.html(wx, loc.hdg);
}

function gamepad() {
  const pads = navigator.getGamepads ? navigator.getGamepads() : [];
  return [...pads].find((p) => p && p.connected && p.axes.length >= 4) || null;
}
const shape = (v) => {
  const dz = 0.05, a = Math.abs(v);
  if (a < dz) return 0;
  const n = Math.min(1, (a - dz) / (0.94 - dz));
  return Math.sign(v) * (0.7 * n + 0.3 * n * n * n);
};
const ramp = (cur, target, dt, cap = 1, up = 1.3, down = 3) => {
  const rate = target === 0 ? down : up;
  return cur + clamp(target * cap - cur, -rate * dt, rate * dt);
};

function cycleFlaps(d) { ctl.flapIdx = clamp(ctl.flapIdx + d, 0, G.ac.flapNotches.length - 1); }
function pressed(i, now) { const r = now && !prevBtn[i]; prevBtn[i] = now; return r; }

function readInput(dt) {
  const k = (c) => (keys.has(c) ? 1 : 0);
  kb.pitch = ramp(kb.pitch, k('ArrowDown') - k('ArrowUp'), dt, 0.85, 2.6, 5);
  kb.roll = ramp(kb.roll, k('ArrowRight') - k('ArrowLeft'), dt, 0.8, 2.4, 5);
  kb.yaw = ramp(kb.yaw, k('KeyD') - k('KeyA'), dt, 0.8, 2.4, 5);
  const pad = gamepad();
  let pitch = kb.pitch, roll = kb.roll, yaw = kb.yaw;
  const air = !!G.cmb && !G.s.contact;
  let gun = air && keys.has('Space'), pickle = keys.has('KeyF') || keys.has('Enter');
  let throttleD = k('KeyW') - k('KeyS'), brake = air ? 0 : k('Space'), trimD = k('KeyE') - k('KeyQ'), sbPad = 0;
  if (throttleD) G.padThr = false;
  let thr = clamp(ctl.throttle + throttleD * 0.35 * dt, 0, 1);
  if (pad) {
    const b = (i) => pad.buttons[i]?.value ?? 0;
    const px = shape(pad.axes[1]), rx = shape(pad.axes[0]);
    if (px || rx) { pitch = px; roll = rx; }
    const lx = shape(pad.axes[2] ?? 0), ly = shape(pad.axes[3] ?? 0);
    if (lx || ly) {
      const k = 1 - Math.exp(-dt * 10);
      G.head.yaw += (-lx * 2.3 - G.head.yaw) * k; G.head.pitch += (-ly * 0.9 - G.head.pitch) * k;
      G.headAt = performance.now();
    }
    G.padYaw = ramp(G.padYaw, b(5) - b(4), dt, 1, 2.5, 4);
    yaw = clamp(yaw + G.padYaw, -1, 1);
    const rt = b(7);
    if (rt > 0.03 || G.hold) G.padThr = true;
    if (G.padThr) thr = Math.max(rt, G.hold ? G.holdVal : 0);
    const cb = air && (G.ac.gearFixed ? (G.s.diag.agl ?? 0) > 150 : !ctl.gearDown);
    if (cb) {
      gun = gun || b(6) > 0.3;
      pickle = pickle || b(1) > 0.5;
      sbPad = b(12);
      if (pressed(1, b(1) > 0.5)) G.cmb.fire();
      if (pressed(13, b(13) > 0.5)) G.cmb.flare();
    } else {
      brake = Math.max(brake, b(1));
      sbPad = b(6);
      trimD += b(13) - b(12);
    }
    if (pressed(0, b(0) > 0.5)) ctl.gearDown = !ctl.gearDown;
    if (pressed(15, b(15) > 0.5)) ctl.parking = !ctl.parking;
    if (pressed(14, b(14) > 0.5) && G.started) Nav.toggle();
    if (pressed(2, b(2) > 0.5)) { if (cb) G.cmb.cycleWeapon(); else cycleFlaps(-1); }
    if (pressed(3, b(3) > 0.5)) { if (cb) G.cmb.cycleTarget(); else cycleFlaps(1); }
    if (pressed(8, b(8) > 0.5)) restart();
    if (pressed(9, b(9) > 0.5)) toggleMenu();
    if (pressed(10, b(10) > 0.5)) G.cam = G.cam === 'chase' ? 'cockpit' : 'chase';
    if (pressed(11, b(11) > 0.5)) { G.hold = !G.hold; G.holdVal = ctl.throttle; }
  }
  if (performance.now() - G.headAt > 1200) { const k = Math.exp(-dt * 3); G.head.yaw *= k; G.head.pitch *= k; }
  ctl.pitch = clamp(pitch * (G.invert ? -1 : 1), -1, 1);
  ctl.roll = clamp(roll, -1, 1);
  ctl.yaw = clamp(yaw, -1, 1);
  if (G.locT > 0) ctl.pitch = ctl.roll = ctl.yaw = 0;
  if (ctl.parking && thr > 0.25) ctl.parking = false;
  ctl.throttle = thr;
  ctl.brake = brake;
  ctl.speedbrake = Math.max(G.sb ? 1 : 0, sbPad);
  ctl.trim = clamp(ctl.trim + trimD * 0.15 * dt, -1, 1);
  ctl.assist = cfg.assist;
  ctl.stallProt = cfg.stallProt;
  if (G.cmb) {
    G.cmb.in.gun = gun; G.cmb.in.pickle = pickle;
    const hp = G.cmb.pl.hp;
    if (hp < 60) ctl.throttle = Math.min(ctl.throttle, hp < 30 ? 0.55 : 0.85);
    if (hp < 30) { ctl.roll *= 0.65; ctl.pitch *= 0.8; }
  }
}

function machCross(up) {
  Sound.boom(up ? 1 : 0.5);
  const el = $('mach');
  el.innerHTML = up ? 'MACH 1<small>barreira do som rompida</small>' : 'SUBSÔNICO<small>abaixo de Mach 1</small>';
  el.classList.remove('go'); void el.offsetWidth; el.classList.add('go');
  if (up) { const f = $('flash'); f.classList.remove('go'); void f.offsetWidth; f.classList.add('go'); }
}

function feedback(pad, d, on) {
  const buf = on ? d.buf ?? 0 : 0;
  const now = performance.now();
  if (pad?.vibrationActuator && buf > 0.1 && now - G.rumbleAt > 110) {
    G.rumbleAt = now;
    pad.vibrationActuator.playEffect('dual-rumble', { startDelay: 0, duration: 130, weakMagnitude: 0.15 + 0.6 * buf, strongMagnitude: 0.5 * buf }).catch?.(() => {});
  }
}

addEventListener('keydown', (e) => {
  if (Nav.isOpen()) { if (e.code === 'Escape' || e.code === 'KeyN') { e.preventDefault(); Nav.close(); } return; }
  if (e.code === 'Escape') { toggleMenu(); return; }
  if (!$('menu').hidden || e.target.tagName === 'INPUT') return;
  keys.add(e.code);
  if (e.code.startsWith('Arrow') || e.code === 'Space' || e.code === 'Tab') e.preventDefault();
  if (e.repeat) return;
  switch (e.code) {
    case 'KeyG': ctl.gearDown = !ctl.gearDown; break;
    case 'KeyP': ctl.parking = !ctl.parking; break;
    case 'KeyZ': cycleFlaps(-1); break;
    case 'KeyX': cycleFlaps(1); break;
    case 'KeyB': G.sb = !G.sb; break;
    case 'KeyV': G.cam = G.cam === 'chase' ? 'cockpit' : 'chase'; break;
    case 'KeyC': G.head.yaw = G.head.pitch = 0; break;
    case 'KeyN': Nav.open(); break;
    case 'KeyJ': Nav.showMini(!Nav.mini); break;
    case 'Comma': Nav.zoom(-1); break;
    case 'Period': Nav.zoom(1); break;
    case 'KeyM': $('info').dataset.mute = Sound.toggle() ? ' · 🔇 som desligado' : ''; break;
    case 'KeyI': G.invert = !G.invert; store.set('invert', G.invert); $('invert').checked = G.invert; break;
    case 'KeyH': G.hideInst = !G.hideInst; $('inst').hidden = G.hideInst || G.pit; break;
    case 'KeyR': restart(); break;
    case 'KeyF': case 'Enter': G.cmb?.fire(); break;
    case 'Tab': G.cmb?.cycleWeapon(); break;
    case 'KeyT': G.cmb?.cycleTarget(); break;
    case 'KeyL': G.cmb?.flare(); break;
    case 'KeyK': G.hud?.cycleRange(); break;
    case 'Digit1': case 'Digit2': case 'Digit3': case 'Digit4': if (G.cmb) { const w = ['gun', 'aam', 'bomb', 'rkt'][+e.code.slice(5) - 1]; if (G.cmb.avail.includes(w)) { G.cmb.sel = w; G.cmb.lock.t = 0; G.cmb.lock.ok = false; Sound.beep(800, 0.04); } } break;
    case 'Equal': case 'NumpadAdd': G.zoom = clamp(G.zoom * 0.85, 0.3, 4); break;
    case 'Minus': case 'NumpadSubtract': G.zoom = clamp(G.zoom / 0.85, 0.3, 4); break;
  }
});
addEventListener('keyup', (e) => keys.delete(e.code));
addEventListener('pointermove', (e) => {
  if (!(e.buttons & 1) || G.cam !== 'cockpit' || !$('menu').hidden) return;
  G.head.yaw = clamp(G.head.yaw - e.movementX * 0.004, -2.6, 2.6);
  G.head.pitch = clamp(G.head.pitch - e.movementY * 0.004, -0.9, 1.1);
  G.headAt = performance.now();
});
addEventListener('blur', () => keys.clear());

function toggleMenu() {
  if (!G.started) return;
  const show = $('menu').hidden;
  $('menu').hidden = !show;
  G.paused = show;
  $('resume').hidden = false;
  $('go').textContent = 'Reiniciar com estas opções';
  keys.clear();
}

function currentLoc() {
  if (cfg.loc !== 'custom') return LOCS[cfg.loc];
  const v = (id) => +$(id).value;
  cfg.custom = { lat: v('cLat'), lon: v('cLon'), elev: v('cElev'), hdg: v('cHdg') };
  const num = Math.round(cfg.custom.hdg / 10) % 36 || 36;
  return { name: 'Personalizado', short: 'PERS', ...cfg.custom, rwy: { len: 1200, wid: 30, thr: -25, num, rev: (num + 18) % 36 || 36 } };
}

async function initViewer() {
  if (G.viewer) return;
  C.Ion.defaultAccessToken = cfg.ion;
  const viewer = new C.Viewer('cesium', {
    baseLayer: new C.ImageryLayer(new C.UrlTemplateImageryProvider({
      url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
      maximumLevel: 19, credit: 'Esri, Maxar, Earthstar Geographics',
    })),
    baseLayerPicker: false, geocoder: false, homeButton: false, sceneModePicker: false, navigationHelpButton: false,
    animation: false, timeline: false, fullscreenButton: false, infoBox: false, selectionIndicator: false,
  });
  const scene = viewer.scene;
  scene.globe.depthTestAgainstTerrain = true;
  scene.screenSpaceCameraController.enableInputs = false;
  scene.postProcessStages.fxaa.enabled = true;
  scene.globe.enableLighting = false;
  viewer.clock.currentTime = C.JulianDate.fromIso8601('2026-11-01T17:00:00Z');
  viewer.clock.shouldAnimate = false;
  if (cfg.gkey) {
    scene.globe.show = false;
    scene.primitives.add(await C.Cesium3DTileset.fromUrl(`https://tile.googleapis.com/v1/3dtiles/root.json?key=${encodeURIComponent(cfg.gkey)}`));
  } else if (cfg.ion) {
    scene.setTerrain(new C.Terrain(C.createWorldTerrainAsync()));
    scene.primitives.add(await C.createOsmBuildingsAsync());
  }
  G.viewer = viewer; G.scene = scene; G.camera = viewer.camera; G.imgMax = 19;
  scene.preRender.addEventListener(onFrame);
}

function setImagery(max) {
  if (G.imgMax === max) return;
  const layers = G.viewer.imageryLayers;
  layers.removeAll();
  layers.addImageryProvider(new C.UrlTemplateImageryProvider({
    url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
    maximumLevel: max, credit: 'Esri, Maxar, Earthstar Geographics',
  }));
  G.imgMax = max;
}

async function loadPlane(id) {
  if (G.planeId === id) return;
  if (G.plane) G.plane.show = false;
  if (!G.models[id]) {
    const m = await C.Model.fromGltfAsync({ url: Models.glbUrl(id), incrementallyLoadTextures: false });
    G.scene.primitives.add(m);
    G.models[id] = m;
  }
  G.plane = G.models[id];
  G.plane.show = true;
  G.planeId = id;
}

function needsPick(lon, lat) {
  if (cfg.gkey || cfg.ion) return true;
  if (!G.bldList.length || !G.loc || (G.s?.diag.agl ?? 0) > 220) return false;
  return Math.hypot((lat - G.loc.lat * D2R) * RE, (lon - G.loc.lon * D2R) * RE * Math.cos(G.loc.lat * D2R)) < 2800;
}

function sampleGround(lon, lat) {
  if (!G.terrain) return 0;
  const c = new C.Cartographic(lon, lat);
  if (!needsPick(lon, lat)) return G.scene.globe.getHeight(c);
  let g;
  G.fx?.setShow(false);
  try { g = G.scene.sampleHeight(c, [G.plane, ...(G.rwy?.prims ?? []), ...(G.bcn?.prims ?? []), 'fx'].filter(Boolean)); } catch {}
  G.fx?.setShow(true);
  return g ?? G.scene.globe.getHeight(c);
}

async function setupWorld(loc) {
  G.terrain = !!(cfg.gkey || cfg.ion);
  if (G.terrain) return;
  if (!cfg.terrain) { G.scene.terrainProvider = new C.EllipsoidTerrainProvider(); return; }
  const h = await World.height(loc.lon, loc.lat).catch(() => 0);
  G.scene.terrainProvider = World.provider(C, { lat: loc.lat, lon: loc.lon, h, r0: 1800, r1: 4500 });
  G.terrain = true;
}

async function setupBuildings(loc) {
  const key = loc.lat.toFixed(4) + ',' + loc.lon.toFixed(4);
  if (!cfg.bld || !cfg.terrain || cfg.gkey || cfg.ion || G.bldKey === key) return;
  for (const p of G.bldList) G.scene.primitives.remove(p);
  G.bldList = []; G.bldKey = key;
  G.bldStatus = ' · carregando prédios (pode levar ~1 min)…';
  try { G.bldList = await World.buildings(C, G.scene, loc.lat, loc.lon, G.ref); } catch (e) { G.bldKey = ''; showError('Prédios (OpenStreetMap) indisponíveis: ' + (e.message || e)); }
  G.bldStatus = '';
}

async function groundAt(lon, lat) {
  G.camera.lookAtTransform(C.Matrix4.IDENTITY);
  G.camera.setView({ destination: C.Cartesian3.fromRadians(lon, lat, 3000) });
  const settled = () => G.scene.globe.tilesLoaded && G.scene.primitives._primitives.every((p) => p.tilesLoaded !== false);
  for (let i = 0; i < 160; i++) {
    await sleep(250);
    if (sampleGround(lon, lat) !== undefined && settled()) { await sleep(1500); return sampleGround(lon, lat); }
  }
  return sampleGround(lon, lat);
}

async function launch() {
  const status = $('status');
  $('go').disabled = true;
  status.className = '';
  try {
    cfg.start = document.querySelector('input[name=start]:checked').value;
    cfg.diff = $('diff').value; cfg.invuln = $('invuln').checked; cfg.aimAssist = $('aimAssist').checked;
    if (cfg.mode !== 'free' && !AIRCRAFT[cfg.ac].weapons) { status.className = 'err'; status.textContent = 'Esta aeronave não tem armamento: escolha o F-16, o Gripen ou o Super Tucano, ou use o Voo livre.'; return; }
    cfg.assist = $('assist').checked; cfg.stallProt = $('stallProt').checked; cfg.gkey = $('gkey').value.trim(); cfg.ion = $('ion').value.trim();
    const loc = currentLoc();
    cfg.terrain = $('terrain').checked; cfg.bld = $('bld').checked;
    for (const k of ['ac', 'loc', 'start', 'fuel', 'assist', 'stallProt', 'inst', 'gkey', 'ion', 'custom', 'wx', 'wxc', 'terrain', 'bld', 'mode', 'diff', 'invuln', 'aimAssist']) store.set(k, cfg[k]);
    if (G.viewer && (cfg.gkey !== G.keyUsed || cfg.ion !== G.ionUsed)) { location.reload(); return; }
    status.textContent = 'Carregando mapa…';
    await initViewer();
    setImagery(loc.imgMax ?? 17);
    G.keyUsed = cfg.gkey; G.ionUsed = cfg.ion;
    status.textContent = 'Montando a aeronave…';
    G.ac = Sim.prepare(AIRCRAFT[cfg.ac]);
    await loadPlane(cfg.ac);
    status.textContent = 'Carregando o terreno (pode levar alguns segundos)…';
    G.loc = loc;
    G.rwy?.remove(); G.rwy = null;
    G.paused = true;
    Sound.init();
    await setupWorld(loc);
    G.ref = await groundAt(C.Math.toRadians(loc.lon), C.Math.toRadians(loc.lat));
    G.lastGround = G.ref ?? 0; G.groundMSL = loc.elev;
    G.site = cfg.mode === 'ataque' ? await pickSite(loc) : null;
    spawn();
    setupBuildings(loc);
    G.started = true;
    $('menu').hidden = true; $('info').hidden = false;
    $('go').textContent = 'Reiniciar com estas opções'; $('resume').hidden = false;
    G.paused = false;
    Nav.reset(); Nav.showMini(Nav.mini); buildBeacons();
    Nav.setMarks(G.site ? [{ lat: loc.lat + G.site[1] / RE / D2R, lon: loc.lon + G.site[0] / (RE * Math.cos(loc.lat * D2R)) / D2R, label: 'BASE INIMIGA' }] : []);
    status.textContent = '';
  } catch (err) {
    status.className = 'err';
    status.textContent = 'Erro: ' + (err.message || err);
    showError(err.stack || String(err));
  } finally { $('go').disabled = false; }
}

function spawn() {
  const { ac, loc } = G;
  const env = Weather.env(G.wxCur, loc.elev);
  G.scene.fog.density = 2e-4 * 10000 / clamp(G.wxCur.vis, 800, 40000);
  G.scene.fog.minimumBrightness = 0.03;
  const lat = C.Math.toRadians(loc.lat), lon = C.Math.toRadians(loc.lon), hdg = C.Math.toRadians(loc.hdg);
  Object.assign(ctl, { pitch: 0, roll: 0, yaw: 0, brake: 0, speedbrake: 0, trim: 0, assist: cfg.assist, stallProt: cfg.stallProt });
  G.sb = false; G.padYaw = 0;
  if (cfg.start === 'air') {
    G.s = Sim.spawnAir(ac, { lat, lon, z: loc.elev + (cfg.mode !== 'free' ? 1800 : 800), groundZ: loc.elev, hdg, V: CRUISE[ac.id], fuelFrac: cfg.fuel / 100, env });
    ctl.throttle = G.s.throttle; ctl.trim = G.s.trim; ctl.gearDown = !!ac.gearFixed; ctl.flapIdx = 0; ctl.parking = false;
    Object.assign(G, { hold: true, holdVal: G.s.throttle, padThr: true });
  } else {
    G.s = Sim.spawnGround(ac, { lat, lon, groundZ: loc.elev, hdg, fuelFrac: cfg.fuel / 100, env });
    Object.assign(ctl, { throttle: 0, gearDown: true, flapIdx: TAKEOFF_FLAP[ac.id], parking: true });
    Object.assign(G, { hold: false, holdVal: 0, padThr: false });
    G.s.flaps = ac.flapNotches[ctl.flapIdx];
  }
  G.gmax = 1; G.sup = false; G.blk = G.red = G.locT = 0;
  G.rebaseUntil = performance.now() + 1500;
  $('msg').textContent = '';
  startCombat();
}

function restart() { if (G.started) { spawn(); G.groundMSL = G.loc.elev; } }

const hpr = new C.HeadingPitchRoll(), pos = new C.Cartesian3(), eye = new C.Cartesian3(), eyeOff = new C.Cartesian3();
const dirV = new C.Cartesian3(), dirW = new C.Cartesian3(), upV = new C.Cartesian3(), upW = new C.Cartesian3(), scV = new C.Cartesian3(), flameM = new C.Matrix4(), nozM = new C.Matrix4();
const camHpr = new C.HeadingPitchRange(0, -0.15, 30);
const propM = new C.Matrix4(), axisV = new C.Cartesian3(), quatQ = new C.Quaternion(), rotM = new C.Matrix3();
let camH = 0, camP = -0.15, last = performance.now(), pad0 = false;

function updateGround() {
  const s = G.s;
  const agl = s.diag.agl ?? 0;
  if (needsPick(s.lon, s.lat) ? G.frame % (agl < 60 ? 2 : 5) === 0 : agl < 600 || G.frame % 8 === 0) {
    const g = sampleGround(s.lon, s.lat);
    if (g !== undefined) G.lastGround = g;
  }
  if (G.ref === undefined) { if (G.lastGround === undefined) return false; G.ref = G.lastGround; }
  if (performance.now() < G.rebaseUntil) G.ref = G.lastGround;
  const target = G.lastGround - G.ref + G.loc.elev;
  G.groundMSL = Math.abs(target - G.groundMSL) > 2 ? target : G.groundMSL + (target - G.groundMSL) * 0.35;
  return true;
}

function onFrame() {
  const now = performance.now();
  const dt = Math.min((now - last) / 1000, 0.5);
  last = now;
  G.fps += (1 / Math.max(dt, 1e-3) - G.fps) * 0.1;
  if (!G.started) return;
  G.frame++;
  const s = G.s, ac = G.ac;
  if (G.ref === undefined && G.paused) return;
  if (!G.paused) {
    readInput(dt);
    if (!s.crashed) {
      if (updateGround()) {
        const n = clamp(Math.ceil(dt * 120), 1, 60), h = dt / n;
        for (let i = 0; i < n; i++) Sim.step(s, ac, ctl, h, G.groundMSL);
      }
    } else $('msg').textContent = '💥 ' + s.crashed + ' — aperte R para reiniciar';
  } else if (gamepad()) readInput(0);
  if (G.cmb) combatStep(dt, s);
  feedback(gamepad(), s.diag, !G.paused && !s.crashed);

  if (!G.rwy && now > G.rebaseUntil && G.ref !== undefined) G.rwy = Runway.create(C, G.scene, G.loc, G.ref);
  if (G.rwy && G.frame % 4 === 0) G.rwy.update(s.lat, s.lon, s.z - G.loc.elev);
  const e = Sim.euler(s.q), d = s.diag;
  hpr.heading = e.psi - Math.PI / 2; hpr.pitch = e.theta; hpr.roll = e.phi;
  C.Cartesian3.fromRadians(s.lon, s.lat, s.z - G.loc.elev + G.ref, undefined, pos);
  const plane = G.plane;
  C.Transforms.headingPitchRollToFixedFrame(pos, hpr, undefined, undefined, plane.modelMatrix);
  const gearNode = plane.getNode?.('gear');
  if (gearNode) gearNode.show = ac.gearFixed || s.gear > 0.1;
  G.propAng = (G.propAng + (G.paused ? 0 : (2 + s.n * 26) * dt)) % (2 * Math.PI);
  const spinTo = (name, ang) => {
    const node = plane.getNode?.(name);
    if (node) node.matrix = C.Matrix4.multiplyByMatrix3(node.originalMatrix, C.Matrix3.fromRotationZ(ang), propM);
    return node;
  };
  const propNode = spinTo('prop', G.propAng), discNode = plane.getNode?.('disc');
  if (propNode) { const turbo = ac.engine.turbo; propNode.show = s.n < (turbo ? 0.12 : 0.6); if (discNode) discNode.show = s.n > (turbo ? 0.06 : 0.3); }
  for (const sd of ['L', 'R']) {
    const fan = spinTo('fan_' + sd, G.propAng * 1.7), blur = plane.getNode?.('fanblur_' + sd);
    if (fan) { fan.show = s.n < 0.55; if (blur) blur.show = s.n > 0.25; }
  }
  const flame = plane.getNode?.('flame'), nozzle = plane.getNode?.('nozzle');
  if (flame) {
    const len = s.ab > 0.03 ? 0.2 + 0.8 * s.ab : s.thrust > 0 ? 0.03 + 0.05 * clamp((s.n - 0.5) / 0.5, 0, 1) : 0;
    flame.show = len > 0.035;
    const r = 0.75 + 0.25 * s.ab, fk = 1 + (Math.random() - 0.5) * 0.14 * (0.3 + s.ab);
    flame.matrix = C.Matrix4.multiplyByScale(flame.originalMatrix, C.Cartesian3.fromElements(r * (2 - fk), r * (2 - fk), len * fk, scV), flameM);
  }
  if (nozzle) {
    const k = 1 + 0.16 * s.ab;
    nozzle.matrix = C.Matrix4.multiplyByScale(nozzle.originalMatrix, C.Cartesian3.fromElements(k, k, 1, scV), nozM);
  }
  const src = { da: s.da, de: s.de, dr: s.dr, flap: s.flaps / ac.flapMax, gear: 1 - s.gear };
  for (const [name, h] of Object.entries(Models.meta[G.planeId] ?? {})) {
    const node = plane.getNode?.(name);
    if (!node) continue;
    if (name.startsWith('gear')) node.show = s.gear > 0.03;
    let ang = 0;
    for (const [k, w] of h.terms) ang += w * src[k];
    C.Quaternion.fromAxisAngle(C.Cartesian3.fromArray(h.axis, 0, axisV), ang, quatQ);
    node.matrix = C.Matrix4.multiplyByMatrix3(node.originalMatrix, C.Matrix3.fromQuaternion(quatQ, rotM), propM);
  }

  const M = d.mach ?? 0;
  if (!G.paused && !s.crashed) {
    if (!G.sup && M >= 1.005) { G.sup = true; machCross(true); }
    else if (G.sup && M <= 0.995) { G.sup = false; machCross(false); }
    const tol = ac.cockpit === 'fighter' ? 6.5 : 4.5, nzv = d.nz ?? 1;
    if (G.locT > 0) { G.locT -= dt; G.blk = 1; if (G.locT <= 0) G.blk = 0.8; }
    else {
      G.blk = clamp(G.blk + (nzv > tol ? (nzv - tol) * 0.22 : -0.4) * dt, 0, 1);
      if (G.blk >= 1) G.locT = 3;
      G.red = clamp(G.red + (nzv < -1.5 ? (-1.5 - nzv) * 0.3 : -0.4) * dt, 0, 1);
    }
  }
  const alive = !G.cmb || G.cmb.pl.alive;
  const inCockpit = G.cam === 'cockpit';
  const fovWant = inCockpit ? 1.5 : 1.0471975512;
  if (G.fov !== fovWant) { G.camera.frustum.fov = fovWant; G.fov = fovWant; }
  const pit = inCockpit && ac.cockpit === 'fighter';
  if (pit !== G.pit) { G.pit = pit; $('inst').hidden = G.hideInst || pit; $('radar').style.visibility = pit ? 'hidden' : 'visible'; }
  const canopyNode = plane.getNode?.('canopy');
  if (canopyNode) canopyNode.show = !inCockpit;
  if (inCockpit) {
    plane.show = alive;
    G.camera.lookAtTransform(C.Matrix4.IDENTITY);
    C.Cartesian3.fromElements(ac.eye[0], ac.eye[1], ac.eye[2] - clamp((d.nz ?? 1) - 1, -2, 8) * 0.008, eyeOff);
    C.Matrix4.multiplyByPoint(plane.modelMatrix, eyeOff, eye);
    const trans = ac.wave?.sup ? 1 - clamp(Math.abs(M - 1) / 0.07, 0, 1) : 0;
    const jit = Math.max((d.buf ?? 0) * 0.012, trans * 0.008) * 2 + (G.fx?.shake ?? 0) * 0.05;
    const yaw = G.head.yaw + (Math.random() - 0.5) * jit, pit = G.head.pitch + (Math.random() - 0.5) * jit;
    const cy = Math.cos(yaw), sy = Math.sin(yaw), cp = Math.cos(pit), sp = Math.sin(pit);
    C.Matrix4.multiplyByPointAsVector(plane.modelMatrix, C.Cartesian3.fromElements(cp * cy, cp * sy, sp, dirV), dirW);
    C.Matrix4.multiplyByPointAsVector(plane.modelMatrix, C.Cartesian3.fromElements(-sp * cy, -sp * sy, cp, upV), upW);
    G.camera.setView({ destination: eye, orientation: { direction: dirW, up: upW } });
  } else {
    plane.show = alive;
    camH += wrap(e.psi - camH) * (1 - Math.exp(-4 * dt));
    camP += (-0.16 + e.theta * 0.45 - camP) * (1 - Math.exp(-3 * dt));
    const shk = (d.buf ?? 0) + (G.fx?.shake ?? 0) * 3;
    camHpr.heading = camH + (Math.random() - 0.5) * 0.01 * shk; camHpr.pitch = camP + (Math.random() - 0.5) * 0.012 * shk; camHpr.range = ac.camDist * G.zoom;
    G.camera.lookAt(pos, camHpr);
  }

  if (G.cmb) combatVisuals(dt, now);
  Sound.update({ ac, s, d, on: !G.paused && !s.crashed });
  if (inCockpit || G.frame % 4 === 0) {
    Cockpit.draw({ on: inCockpit, ac, d, s, ctl, head: G.head, fovy: G.camera.frustum.fovy, altFt: s.z * Sim.FT, gfx: { black: G.blk, red: G.red }, cmb: G.cmb, radar: $('radar'), brg: G.brg ?? null, gmax: G.gmax });
  }
  if (d.nz !== undefined) G.gmax = Math.max(G.gmax, d.nz);
  if (G.frame % 2 === 0 && !G.hideInst && !pit) {
    Instruments.update({
      ac, ias: d.ias ?? 0, mach: d.mach ?? 0, alt: s.z * Sim.FT, vsi: d.vsi ?? 0, hdg: d.hdg ?? 0, pitch: d.pitch ?? 0, roll: d.roll ?? 0,
      turn: d.turn ?? 0, ay: d.ay ?? 0, nz: d.nz ?? 1, gmax: G.gmax, alpha: d.alpha ?? 0, n: s.n, ab: s.ab, thr: ctl.throttle,
      fuel: s.fuel, flow: s.flow, thrust: s.thrust, flapIdx: ctl.flapIdx, flapNotch: ac.flapNotches[ctl.flapIdx], gear: s.gear,
      brake: ctl.brake, parking: ctl.parking, sb: s.sb, stallWarn: d.stallWarn, trim: ctl.trim, assist: ctl.assist, brg: G.brg ?? null, agl: d.agl ?? 0, onGround: s.contact,
    });
  }
  if (G.frame % 10 === 0) {
    const pl = { lat: s.lat * 180 / Math.PI, lon: s.lon * 180 / Math.PI, hdg: d.hdg ?? 0, gs: d.gs ?? 0 };
    const nv = Nav.update(pl);
    G.brg = nv.brg;
    updateBeacons(pl);
    if (!s.crashed) $('msg').textContent = !ac.gearFixed && !ctl.gearDown && (d.agl ?? 1e9) < 120 && (d.vsi ?? 0) < -100 ? '⚠ TREM RECOLHIDO — aperte G' : '';
    const gd = G.rwy ? G.rwy.guide(s.lat, s.lon, s.z - G.loc.elev) : '';
    $('info').innerHTML = `${ac.name} · ${G.cam === 'chase' ? 'câmera externa' : 'cabine'} · GS ${Math.round((d.gs ?? 0) * Sim.KT)} kt · ${Math.round(G.fps)} fps${M > 0.3 ? ` · <b>M ${M.toFixed(2)}</b>${M >= 1 ? ' supersônico' : ''}` : ''}${G.locT > 0 ? ' · <b>G-LOC</b>' : ''}${$('info').dataset.mute ?? ''} ·${gamepad() ? '🎮 controle conectado' : '⌨️ teclado'}${(d.wkt ?? 0) > 0.5 ? ' · vento ' + String(Math.round(d.wdir)).padStart(3, '0') + '°/' + Math.round(d.wkt) + ' kt' : ''}${G.bldStatus}${G.hold ? ' · manete travada ' + Math.round(G.holdVal * 100) + '%' : ''}${nv.text ? `<div>🧭 ${nv.text}</div>` : ''}${G.cmb ? `<div>⚔ ${G.cmb.status().join(' · ')}${baseInfo()}</div>` : ''}${gd ? `<div>🛬 ${gd}</div>` : ''}`;
  }
}

const rwyPt = (l, u) => {
  const h = l.hdg * Math.PI / 180;
  return [l.lat + u * Math.cos(h) / 110574, l.lon + u * Math.sin(h) / (111320 * Math.cos(l.lat * Math.PI / 180))];
};
function airports() {
  const list = Object.values(LOCS).filter((l) => l.rwy);
  if (cfg.loc === 'custom') list.push(currentLoc());
  return list.map((l) => {
    const [lat, lon] = rwyPt(l, l.rwy.thr + l.rwy.len / 2);
    return { name: l.name, short: l.short, lat, lon, a: rwyPt(l, l.rwy.thr), b: rwyPt(l, l.rwy.thr + l.rwy.len) };
  });
}

function buildBeacons() {
  if (!G.scene) return;
  if (G.bcn) for (const p of G.bcn.prims) G.scene.primitives.remove(p);
  G.bcn = null;
  if (!Nav.pts.length || G.ref === undefined) return;
  const lines = G.scene.primitives.add(new C.PolylineCollection()), labels = G.scene.primitives.add(new C.LabelCollection());
  for (const p of Nav.pts) {
    lines.add({ id: 'fx', positions: [C.Cartesian3.fromDegrees(p.lon, p.lat, G.ref - 200), C.Cartesian3.fromDegrees(p.lon, p.lat, G.ref + 12000)], width: 3, material: C.Material.fromType('Color', { color: C.Color.MAGENTA.withAlpha(0.7) }) });
    labels.add({ id: 'fx', position: C.Cartesian3.fromDegrees(p.lon, p.lat, G.ref + 500), text: '', font: '600 15px system-ui', style: C.LabelStyle.FILL_AND_OUTLINE, outlineWidth: 3, outlineColor: C.Color.BLACK, verticalOrigin: C.VerticalOrigin.BOTTOM, pixelOffset: new C.Cartesian2(0, -8), disableDepthTestDistance: Number.POSITIVE_INFINITY });
  }
  G.bcn = { prims: [lines, labels], lines, labels };
}

function updateBeacons(pl) {
  const b = G.bcn;
  if (!b || b.lines.length !== Nav.pts.length) return;
  const alt = G.ref + (G.s.z - G.loc.elev);
  Nav.pts.forEach((p, i) => {
    const on = i === Nav.active, label = b.labels.get(i);
    label.position = C.Cartesian3.fromDegrees(p.lon, p.lat, alt);
    label.text = `${i + 1} ${Nav.name(i)} · ${(Nav.dist(pl, p) / 1000).toFixed(1)} km`;
    label.fillColor = on ? C.Color.fromCssColorString('#ffb0f4') : C.Color.WHITE;
    b.lines.get(i).material.uniforms.color = (on ? C.Color.fromCssColorString('#ff4dd2') : C.Color.MAGENTA).withAlpha(on ? 0.95 : 0.6);
  });
}

const qAxes = ([w, x, y, z]) => ({
  f: [1 - 2 * (y * y + z * z), 2 * (x * y + w * z), 2 * (x * z - w * y)],
  r: [2 * (x * y - w * z), 1 - 2 * (x * x + z * z), 2 * (y * z + w * x)],
  d: [2 * (x * z + w * y), 2 * (y * z - w * x), 1 - 2 * (x * x + y * y)],
});
const cg = new C.Cartographic();

const groundCache = new Map();
let groundBudget = 6;
function enuGround(e, n) {
  if (cfg.gkey) return G.loc.elev;
  const key = Math.round(e / 40) * 1e6 + Math.round(n / 40), now = performance.now(), hit = groundCache.get(key);
  if (hit && now - hit.t < 10000) return hit.h;
  if (groundBudget <= 0) return hit ? hit.h : G.groundMSL;
  groundBudget--;
  cg.latitude = frame.lat0 + n / RE; cg.longitude = frame.lon0 + e / (RE * Math.cos(frame.lat0));
  const h = G.scene.globe.getHeight(cg);
  if (h === undefined) return G.groundMSL;
  if (groundCache.size > 6000) groundCache.clear();
  const msl = h - G.ref + G.loc.elev;
  groundCache.set(key, { h: msl, t: now });
  return msl;
}

async function pickSite(loc) {
  const psi = loc.hdg * D2R, cl = Math.cos(loc.lat * D2R), cand = [];
  for (let i = 0; i < 12; i++) {
    const off = i <= 6 ? i : i - 12, brg = psi + off * 30 * D2R, e0 = Math.sin(brg) * 9000, n0 = Math.cos(brg) * 9000;
    const hs = await Promise.all([-350, 0, 350].flatMap((a) => [-350, 0, 350].map((b) => World.height(loc.lon + (e0 + a) / (RE * cl) / D2R, loc.lat + (n0 + b) / RE / D2R).catch(() => 0))));
    cand.push({ e: e0, n: n0, range: Math.max(...hs) - Math.min(...hs), wet: Math.min(...hs) < 3, off: Math.abs(off) });
  }
  const ok = cand.filter((c) => !c.wet);
  const best = (ok.length ? ok : cand).sort((a, b) => a.range + a.off * 6 - (b.range + b.off * 6))[0];
  return [best.e, best.n];
}

function baseInfo() {
  if (!G.site || !G.cmb.baseBuilt || !G.cmb.primLeft) return '';
  const dx = G.site[0] - (G.s.lon - frame.lon0) * RE * Math.cos(frame.lat0), dy = G.site[1] - (G.s.lat - frame.lat0) * RE;
  return ` · BASE ${(Math.hypot(dx, dy) / 1000).toFixed(1)} km rumo ${String(Math.round((Math.atan2(dx, dy) / D2R + 360) % 360)).padStart(3, '0')}°`;
}

function setFrame(loc) { frame.lat0 = loc.lat * D2R; frame.lon0 = loc.lon * D2R; frame.elev = loc.elev; frame.ref = G.ref; }

function startCombat() {
  setFrame(G.loc);
  groundCache.clear();
  const armed = cfg.mode !== 'free' && !!G.ac.weapons;
  if (armed) { G.fx ||= Fx.create(C, G.scene, frame); G.hud ||= Hud.create($('hud'), $('radar')); }
  G.fx?.clear(); G.hud?.clear();
  const fixedStores = G.plane.getNode?.('stores');
  if (fixedStores) fixedStores.show = !armed;
  $('feed').innerHTML = ''; $('result').hidden = true;
  G.resAt = 0; G.resShown = false; G.siteSet = false; G.hitFlash = 0;
  G.cmb = armed ? Combat.create({ groundAt: enuGround }, G.ac, { mission: cfg.mode, diff: cfg.diff, invuln: cfg.invuln, assist: cfg.aimAssist, home: [0, 0, G.loc.elev] }) : null;
  $('radar').style.display = armed ? 'block' : 'none';
  Sound.combat({ firing: false, lock: 0, rwr: 0, dt: 0, rate: 1 });
}

function feedMsg(m) {
  const el = document.createElement('div');
  el.className = m.kind; el.textContent = m.text;
  $('feed').appendChild(el);
  while ($('feed').children.length > 6) $('feed').firstChild.remove();
  setTimeout(() => el.remove(), 6200);
}

function rumble(strong, ms) {
  gamepad()?.vibrationActuator?.playEffect('dual-rumble', { startDelay: 0, duration: ms, weakMagnitude: strong * 0.6, strongMagnitude: strong }).catch?.(() => {});
}

function combatEvents(c) {
  const pl = c.pl;
  for (const ev of c.ev.splice(0)) {
    switch (ev.t) {
      case 'boom': Sound.boomAt(G.fx.boom(ev.pos, ev.size, ev.gnd), ev.size); break;
      case 'spark': G.fx.spark(ev.pos, ev.gnd); break;
      case 'dust': G.fx.dust(ev.pos); break;
      case 'launch': Sound.launch(Math.hypot(ev.pos[0] - pl.pos[0], ev.pos[1] - pl.pos[1], ev.pos[2] - pl.pos[2])); break;
      case 'drop': Sound.beep(300, 0.12, 0.1); break;
      case 'hurt': G.hitFlash = Math.min(1, 0.25 + ev.dmg * 0.05); G.fx.shake = Math.max(G.fx.shake, Math.min(0.7, ev.dmg * 0.03)); rumble(Math.min(1, 0.3 + ev.dmg * 0.04), 220); Sound.thud(); break;
      case 'gunwarn': Sound.beep(1250, 0.07, 0.06); break;
      case 'select': Sound.beep(820, 0.04); break;
      case 'nolock': Sound.beep(280, 0.18, 0.09); break;
      case 'flare': Sound.beep(1500, 0.05, 0.05); break;
      case 'rearm': Sound.beep(1000, 0.14); break;
      case 'radio': Sound.beep(1100, 0.05, 0.04); break;
    }
  }
  for (const m of c.msgs.splice(0)) { feedMsg(m); if (m.kind === 'bad') Sound.beep(520, 0.12, 0.08); else if (m.kind === 'kill') Sound.beep(1300, 0.07, 0.06); }
}

function combatStep(dt, s) {
  groundBudget = 6;
  const c = G.cmb, e = qAxes(s.q), d = s.diag;
  const ned = (v) => [v[1], v[0], -v[2]];
  c.player([(s.lon - frame.lon0) * RE * Math.cos(frame.lat0), (s.lat - frame.lat0) * RE, s.z], [s.v[1], s.v[0], -s.v[2]], ned(e.f), [-e.d[1], -e.d[0], e.d[2]], ned(e.r), d.agl ?? 0, !!s.contact);
  frame.ref = G.ref;
  if (!G.siteSet && G.site && performance.now() > G.rebaseUntil) { c.site = G.site; G.siteSet = true; }
  if (!G.paused) {
    c.step(dt);
    if (G.frame % 45 === 0) for (const t of c.gnd) if (t.alive && !t.move) t.pos[2] = enuGround(t.pos[0], t.pos[1]);
  }
  if (s.crashed && c.pl.alive) { c.pl.alive = false; c.pl.hp = 0; c.over ||= 'lose'; Sound.boomAt(G.fx.boom([...c.pl.pos], 2.5, true), 2); }
  if (!c.pl.alive) { if (!s.crashed) s.crashed = 'Aeronave destruída em combate'; G.cam = 'chase'; }
  combatEvents(c);
  if (c.over && !G.resShown) {
    G.resAt ||= performance.now() + (c.over === 'win' ? 1500 : 2800);
    if (performance.now() > G.resAt) showResult(c);
  }
}

function showResult(c) {
  G.resShown = true;
  const win = c.over === 'win', r = $('result');
  const acc = c.shots ? Math.round(c.hits / c.shots * 100) : 0;
  r.className = win ? 'win' : 'lose';
  r.innerHTML = `<div><h2>${win ? 'MISSÃO CUMPRIDA' : 'MISSÃO FALHOU'}</h2><div style="color:var(--muted)">${c.def.name}</div><table>
    <tr><td>Pontuação</td><td>${c.score}</td></tr><tr><td>Aviões abatidos</td><td>${c.kills.air}</td></tr><tr><td>Alvos no solo</td><td>${c.kills.gnd}</td></tr>
    <tr><td>Precisão do canhão</td><td>${acc}%</td></tr><tr><td>Tempo de missão</td><td>${Math.floor(c.t / 60)}:${String(Math.floor(c.t % 60)).padStart(2, '0')}</td></tr></table>
    <div class="row" style="justify-content:center"><button id="rsRestart">Jogar de novo (R)</button>${win ? '<button id="rsGo">Continuar voando</button>' : '<button id="rsMenu">Menu</button>'}</div></div>`;
  r.hidden = false;
  $('rsRestart').onclick = () => restart();
  const go = $('rsGo'); if (go) go.onclick = () => { r.hidden = true; };
  const mn = $('rsMenu'); if (mn) mn.onclick = () => { r.hidden = true; toggleMenu(); };
}

function combatVisuals(dt, now) {
  const c = G.cmb, fx = G.fx, pl = c.pl, cam = G.camera;
  fx.camPos = cam.positionWC;
  const sdt = G.paused ? 0 : Math.min(dt, 0.1);
  if (sdt > 0) {
    fx.step(sdt);
    fx.sync(c, sdt);
    if (pl.alive && pl.hp < 60 && Math.random() < sdt * (pl.hp < 30 ? 40 : 18)) {
      const tail = [pl.pos[0] - pl.fwd[0] * 8, pl.pos[1] - pl.fwd[1] * 8, pl.pos[2] - pl.fwd[2] * 8];
      fx.smoke(tail, 0.9); if (pl.hp < 30) fx.flame(tail, 1.2);
    }
    if (c.firing) fx.emit(pl.pos[0] + pl.fwd[0] * 10, pl.pos[1] + pl.fwd[1] * 10, pl.pos[2] + pl.fwd[2] * 10, pl.vel[0], pl.vel[1], pl.vel[2], 0.05, 3, 1.5, [1, 0.95, 0.6, 1], [1, 0.5, 0.1, 0]);
  }
  G.hitFlash = Math.max(0, G.hitFlash - dt * 1.6);
  fx.stores(c, G.plane.modelMatrix, pl.alive);
  G.hud.draw({ c, camera: cam, world: fx.world, fovy: cam.frustum.fovy, aspect: cam.frustum.aspectRatio, ac: G.ac, flashHit: G.hitFlash, now, inCockpit: G.cam === 'cockpit', d: G.s.diag, altFt: G.s.z * Sim.FT });
  if (G.frame % 2 === 0) G.hud.radar({ c, now });
  const run = !G.paused && pl.alive;
  Sound.combat({ firing: run && c.firing, rate: G.ac.weapons.gun.rate, dt, lock: run ? (c.lock.ok || c.sol?.onTarget ? 2 : c.pickle || (c.sel === 'aam' && c.lock.t > 0.05) ? 1 : 0) : 0, rwr: run && c.warn.msl ? 2 : 0 });
}

buildMenu();
Nav.init({
  load: () => store.get('route', null), save: (r) => store.set('route', r),
  start: () => { const l = currentLoc(); return { lat: l.lat, lon: l.lon }; },
  airports, speed: () => CRUISE[cfg.ac] ?? 60, flying: () => G.started,
  onOpen: () => { Nav.was = G.paused; if (G.started) G.paused = true; },
  onClose: () => { if (G.started) G.paused = Nav.was ?? false; keys.clear(); },
  onChange: buildBeacons,
});
$('openMap').onclick = () => Nav.open();
Cockpit.mount($('cockpit'));
$('go').addEventListener('click', launch);
$('resume').addEventListener('click', toggleMenu);
