(function (root) {
const S = 140, C = 70, R = 64, PI = Math.PI;
const clamp = (x, a, b) => Math.min(b, Math.max(a, x));
const P = (r, a) => [C + r * Math.cos(a), C + r * Math.sin(a)];
const COL = { fg: '#e6edf3', dim: '#8b949e', ok: '#3fb950', warn: '#d29922', bad: '#f85149', off: '#30363d', sky: '#3a7bc8', gnd: '#7a5230' };

function base(ctx) {
  ctx.clearRect(0, 0, S, S);
  ctx.beginPath(); ctx.arc(C, C, R, 0, 2 * PI);
  ctx.fillStyle = '#0b0f14'; ctx.fill();
  ctx.lineWidth = 3; ctx.strokeStyle = '#3b4655'; ctx.stroke();
}
function panel(ctx, title) {
  ctx.clearRect(0, 0, S, S);
  ctx.beginPath(); ctx.roundRect(3, 3, S - 6, S - 6, 12);
  ctx.fillStyle = '#0b0f14'; ctx.fill();
  ctx.lineWidth = 3; ctx.strokeStyle = '#3b4655'; ctx.stroke();
  if (title) text(ctx, title, C, 13, 9, COL.dim);
}
function line(ctx, r1, r2, a, w, col) {
  const [x1, y1] = P(r1, a), [x2, y2] = P(r2, a);
  ctx.beginPath(); ctx.moveTo(x1, y1); ctx.lineTo(x2, y2);
  ctx.lineWidth = w; ctx.strokeStyle = col; ctx.lineCap = 'round'; ctx.stroke();
}
function arc(ctx, r, a0, a1, w, col) {
  ctx.beginPath(); ctx.arc(C, C, r, a0, a1);
  ctx.lineWidth = w; ctx.strokeStyle = col; ctx.lineCap = 'butt'; ctx.stroke();
}
function text(ctx, s, x, y, size = 10, col = COL.fg, align = 'center') {
  ctx.font = `${size}px system-ui, sans-serif`;
  ctx.fillStyle = col; ctx.textAlign = align; ctx.textBaseline = 'middle';
  ctx.fillText(s, x, y);
}
function hub(ctx) { ctx.beginPath(); ctx.arc(C, C, 4, 0, 2 * PI); ctx.fillStyle = '#c9d1d9'; ctx.fill(); }
function bar(ctx, x, y, w, h, frac, col, label, val) {
  text(ctx, label, x, y - 7, 8, COL.dim, 'left');
  text(ctx, val, x + w, y - 7, 9, COL.fg, 'right');
  ctx.fillStyle = '#1b222c'; ctx.beginPath(); ctx.roundRect(x, y, w, h, 3); ctx.fill();
  ctx.fillStyle = col; ctx.beginPath(); ctx.roundRect(x, y, Math.max(0, w * clamp(frac, 0, 1)), h, 3); ctx.fill();
}

function asi(ctx, d) {
  base(ctx);
  const A = d.ac.asi, a = (v) => 0.75 * PI + 1.5 * PI * clamp(v / A.max, 0, 1);
  arc(ctx, 49, a(A.white[0]), a(A.white[1]), 4, '#e6edf3');
  arc(ctx, 54, a(A.green[0]), a(A.green[1]), 5, COL.ok);
  arc(ctx, 54, a(A.yellow[0]), a(A.yellow[1]), 5, COL.warn);
  line(ctx, 47, 59, a(A.red), 3, COL.bad);
  for (let i = 0; i <= 20; i++) {
    const v = i * A.max / 20, major = i % 2 === 0;
    line(ctx, major ? 44 : 47, 49 + (major ? 0 : 0), a(v), major ? 1.5 : 1, COL.fg);
    if (major && i % 4 === 0) { const [x, y] = P(36, a(v)); text(ctx, A.max > 400 ? Math.round(v / 10) : Math.round(v), x, y, 9); }
  }
  text(ctx, A.max > 400 ? 'KT x10' : 'KT', C, C + 20, 8, COL.dim);
  if (d.mach >= 0.3) text(ctx, 'M ' + d.mach.toFixed(2), C, C + 33, 10, COL.fg);
  line(ctx, -8, 50, a(d.ias), 2.5, '#fff'); hub(ctx);
}

function ai(ctx, d) {
  ctx.clearRect(0, 0, S, S);
  ctx.save();
  ctx.beginPath(); ctx.arc(C, C, R, 0, 2 * PI); ctx.clip();
  ctx.translate(C, C); ctx.rotate(-d.roll);
  const ppd = 1.15, off = d.pitch * 180 / PI * ppd;
  ctx.fillStyle = COL.sky; ctx.fillRect(-150, -300 + off, 300, 300);
  ctx.fillStyle = COL.gnd; ctx.fillRect(-150, off, 300, 300);
  ctx.strokeStyle = '#fff'; ctx.lineWidth = 1.5; ctx.beginPath(); ctx.moveTo(-150, off); ctx.lineTo(150, off); ctx.stroke();
  for (let p = -30; p <= 30; p += 5) {
    if (!p) continue;
    const y = off - p * ppd, w = p % 10 === 0 ? 20 : 10;
    ctx.beginPath(); ctx.moveTo(-w, y); ctx.lineTo(w, y); ctx.stroke();
    if (p % 10 === 0) { text(ctx, Math.abs(p), -w - 8, y, 8); text(ctx, Math.abs(p), w + 8, y, 8); }
  }
  for (const b of [-60, -45, -30, -20, -10, 0, 10, 20, 30, 45, 60]) {
    const a = b * PI / 180 - PI / 2, [x1, y1] = [Math.cos(a) * (R - 2), Math.sin(a) * (R - 2)], l = b % 30 === 0 ? 9 : 5;
    ctx.beginPath(); ctx.moveTo(x1, y1); ctx.lineTo(Math.cos(a) * (R - 2 - l), Math.sin(a) * (R - 2 - l)); ctx.stroke();
  }
  ctx.restore();
  ctx.beginPath(); ctx.arc(C, C, R, 0, 2 * PI); ctx.lineWidth = 3; ctx.strokeStyle = '#3b4655'; ctx.stroke();
  ctx.fillStyle = '#fff'; ctx.beginPath(); ctx.moveTo(C, C - R + 12); ctx.lineTo(C - 5, C - R + 3); ctx.lineTo(C + 5, C - R + 3); ctx.fill();
  ctx.strokeStyle = '#ffa000'; ctx.lineWidth = 3; ctx.lineCap = 'round';
  ctx.beginPath(); ctx.moveTo(C - 34, C); ctx.lineTo(C - 12, C); ctx.lineTo(C - 12, C + 5); ctx.moveTo(C + 34, C); ctx.lineTo(C + 12, C); ctx.lineTo(C + 12, C + 5); ctx.stroke();
  ctx.fillStyle = '#ffa000'; ctx.fillRect(C - 2, C - 2, 4, 4);
}

function alt(ctx, d) {
  base(ctx);
  for (let i = 0; i < 50; i++) {
    const a = i / 50 * 2 * PI - PI / 2, major = i % 5 === 0;
    line(ctx, major ? 50 : 54, 58, a, major ? 1.6 : 1, COL.fg);
    if (major) { const [x, y] = P(42, a); text(ctx, i / 5, x, y, 11); }
  }
  const h = ((d.alt % 1000) + 1000) % 1000 / 1000 * 2 * PI - PI / 2;
  const k = ((d.alt % 10000) + 10000) % 10000 / 10000 * 2 * PI - PI / 2;
  line(ctx, 0, 30, k, 4, '#c9d1d9');
  line(ctx, -6, 52, h, 2.5, '#fff');
  hub(ctx);
  ctx.fillStyle = '#000'; ctx.beginPath(); ctx.roundRect(C - 26, C + 24, 52, 17, 3); ctx.fill();
  text(ctx, Math.round(d.alt).toLocaleString('pt-BR'), C, C + 33, 11, '#fff');
  text(ctx, 'FT', C, C - 22, 8, COL.dim);
}

function vsi(ctx, d) {
  base(ctx);
  const M = d.ac.vsiMax, a = (v) => PI + clamp(v / M, -1.05, 1.05) * 0.8 * PI;
  const step = M / 4;
  for (let i = -4; i <= 4; i++) {
    const v = i * step;
    line(ctx, 50, 59, a(v), i % 2 === 0 ? 2 : 1, COL.fg);
    if (i % 2 === 0) { const [x, y] = P(40, a(v)); text(ctx, Math.abs(v) / 1000, x, y, 10); }
  }
  text(ctx, 'UP', C + 8, C - 22, 8, COL.dim); text(ctx, 'DN', C + 8, C + 22, 8, COL.dim);
  text(ctx, 'x1000 FPM', C + 12, C + 38, 8, COL.dim);
  text(ctx, Math.round(d.vsi / 10) * 10, C + 12, C - 38, 10, '#fff');
  line(ctx, -8, 54, a(d.vsi), 2.5, '#fff'); hub(ctx);
}

function hdg(ctx, d) {
  base(ctx);
  ctx.save(); ctx.translate(C, C); ctx.rotate(-d.hdg * PI / 180);
  for (let deg = 0; deg < 360; deg += 5) {
    const a = deg * PI / 180 - PI / 2, major = deg % 10 === 0;
    ctx.beginPath(); ctx.moveTo(Math.cos(a) * 59, Math.sin(a) * 59); ctx.lineTo(Math.cos(a) * (major ? 51 : 55), Math.sin(a) * (major ? 51 : 55));
    ctx.strokeStyle = COL.fg; ctx.lineWidth = 1; ctx.stroke();
    if (deg % 30 === 0) {
      ctx.save(); ctx.rotate(a + PI / 2); ctx.translate(0, -42);
      const L = { 0: 'N', 90: 'E', 180: 'S', 270: 'W' }[deg] ?? deg / 10;
      text(ctx, L, 0, 0, deg % 90 === 0 ? 13 : 10, deg === 0 ? COL.bad : COL.fg);
      ctx.restore();
    }
  }
  if (d.brg != null) {
    ctx.save(); ctx.rotate(d.brg * PI / 180);
    ctx.fillStyle = '#ff4dd2'; ctx.strokeStyle = '#000'; ctx.lineWidth = 1;
    ctx.beginPath(); ctx.moveTo(0, -R + 1); ctx.lineTo(-7, -R + 15); ctx.lineTo(7, -R + 15); ctx.closePath(); ctx.fill(); ctx.stroke();
    ctx.restore();
  }
  ctx.restore();
  ctx.fillStyle = '#ffa000';
  ctx.beginPath(); ctx.moveTo(C, C - 6); ctx.lineTo(C + 3, C + 4); ctx.lineTo(C, C + 1); ctx.lineTo(C - 3, C + 4); ctx.fill();
  ctx.fillRect(C - 13, C - 1, 26, 3); ctx.fillRect(C - 1, C - 14, 3, 16);
  ctx.fillStyle = '#fff'; ctx.beginPath(); ctx.moveTo(C, C - R + 1); ctx.lineTo(C - 4, C - R - 5 + 12); ctx.lineTo(C + 4, C - R - 5 + 12); ctx.fill();
  ctx.fillStyle = '#000'; ctx.beginPath(); ctx.roundRect(C - 18, C + 20, 36, 16, 3); ctx.fill();
  text(ctx, String(Math.round(d.hdg) % 360).padStart(3, '0') + '°', C, C + 28, 11, '#fff');
}

function tc(ctx, d) {
  base(ctx);
  const rate = d.turn * 180 / PI, tilt = clamp(rate / 3, -1.6, 1.6) * 20 * PI / 180;
  for (const s of [-1, 1]) {
    ctx.save(); ctx.translate(C, C - 6); ctx.rotate(s * 20 * PI / 180);
    ctx.fillStyle = COL.fg; ctx.fillRect(s * 38 - 1.5, -2, 3, 5); ctx.restore();
  }
  text(ctx, 'L', C - 52, C - 6, 10, COL.dim); text(ctx, 'R', C + 52, C - 6, 10, COL.dim);
  ctx.save(); ctx.translate(C, C - 6); ctx.rotate(tilt);
  ctx.strokeStyle = '#ffa000'; ctx.lineWidth = 4; ctx.lineCap = 'round';
  ctx.beginPath(); ctx.moveTo(-34, 0); ctx.lineTo(34, 0); ctx.moveTo(0, 0); ctx.lineTo(0, -12); ctx.moveTo(-8, -12); ctx.lineTo(8, -12); ctx.stroke();
  ctx.fillStyle = '#ffa000'; ctx.beginPath(); ctx.arc(0, 0, 5, 0, 2 * PI); ctx.fill();
  ctx.restore();
  ctx.fillStyle = '#05080c'; ctx.beginPath(); ctx.roundRect(C - 38, C + 30, 76, 16, 8); ctx.fill();
  ctx.strokeStyle = '#3b4655'; ctx.lineWidth = 1; ctx.stroke();
  ctx.strokeStyle = COL.fg; ctx.beginPath(); ctx.moveTo(C - 7, C + 30); ctx.lineTo(C - 7, C + 46); ctx.moveTo(C + 7, C + 30); ctx.lineTo(C + 7, C + 46); ctx.stroke();
  const bx = clamp(-d.ay / 9.81 * 140, -28, 28);
  ctx.fillStyle = '#e6edf3'; ctx.beginPath(); ctx.arc(C + bx, C + 38, 6, 0, 2 * PI); ctx.fill();
  text(ctx, '2 MIN', C, C + 18, 8, COL.dim);
}

function eng(ctx, d) {
  panel(ctx, 'MOTOR / COMBUSTÍVEL');
  const e = d.ac.engine;
  const piston = e.type === 'piston';
  const nLabel = piston ? 'RPM' : e.nlabel;
  const nVal = piston ? Math.round((0.22 + 0.78 * d.n) * e.nmax / 10) * 10 : (d.n * 100).toFixed(0) + '%';
  bar(ctx, 14, 32, 112, 7, piston ? 0.22 + 0.78 * d.n : d.n, COL.ok, nLabel, nVal);
  const abOn = d.ab > 0.5;
  bar(ctx, 14, 58, 112, 7, d.thr, abOn ? COL.bad : COL.warn, d.ac.engine.ab ? 'MANETE (A/B >90%)' : 'MANETE', Math.round(d.thr * 100) + '%');
  const fr = d.fuel / d.ac.fuelMax;
  bar(ctx, 14, 84, 112, 7, fr, fr < 0.15 ? COL.bad : COL.ok, 'COMBUSTÍVEL', Math.round(d.fuel) + ' kg');
  text(ctx, piston ? Math.round(d.flow * 3600 / 0.72) + ' L/h' : Math.round(d.flow * 3600) + ' kg/h', 14, 112, 9, COL.fg, 'left');
  if (!piston) text(ctx, (d.thrust / 1000).toFixed(0) + ' kN', 126, 112, 9, COL.dim, 'right');
}

function aoa(ctx, d) {
  panel(ctx, 'ÂNG. ATAQUE');
  const a0 = (d.ac.aStall) * 180 / PI, lo = -5, hi = a0 * 1.25, y0 = 24, y1 = 128;
  const Y = (a) => y1 - (clamp(a, lo, hi) - lo) / (hi - lo) * (y1 - y0);
  const zones = [[lo, a0 * 0.6, COL.ok], [a0 * 0.6, a0 * 0.85, COL.warn], [a0 * 0.85, hi, COL.bad]];
  for (const [a, b, c] of zones) { ctx.fillStyle = c; ctx.fillRect(34, Y(b), 12, Y(a) - Y(b)); }
  const al = d.alpha * 180 / PI, y = Y(al);
  ctx.fillStyle = '#fff'; ctx.beginPath(); ctx.moveTo(50, y); ctx.lineTo(64, y - 6); ctx.lineTo(64, y + 6); ctx.fill();
  text(ctx, al.toFixed(1) + '°', 100, 70, 18, '#fff');
  text(ctx, 'limite ~' + a0.toFixed(0) + '°', 100, 92, 9, COL.dim);
}

function gm(ctx, d) {
  base(ctx);
  const [lo, hi] = d.ac.nzLimit, a = (g) => 0.75 * PI + 1.5 * PI * clamp((g - lo) / (hi - lo), 0, 1);
  for (let g = Math.ceil(lo); g <= hi; g++) {
    line(ctx, 50, 59, a(g), 1.5, COL.fg);
    const [x, y] = P(40, a(g)); text(ctx, g, x, y, 10);
  }
  arc(ctx, 56, a(hi * 0.85), a(hi), 4, COL.bad);
  line(ctx, 44, 60, a(d.gmax), 3, COL.warn);
  text(ctx, 'G', C, C - 18, 11, COL.dim);
  text(ctx, d.nz.toFixed(1), C, C + 22, 16, '#fff');
  line(ctx, -8, 50, a(d.nz), 2.5, '#fff'); hub(ctx);
}

function lamp(ctx, x, y, w, label, col, on) {
  ctx.fillStyle = on ? col : '#161c24'; ctx.beginPath(); ctx.roundRect(x, y, w, 20, 4); ctx.fill();
  text(ctx, label, x + w / 2, y + 10, 9, on ? '#05080c' : COL.dim);
}
function cfg(ctx, d) {
  panel(ctx, 'CONFIGURAÇÃO');
  const ac = d.ac, fl = performance.now() % 600 < 300;
  const gearTxt = ac.gearFixed ? 'TREM FIXO' : d.gear > 0.99 ? 'TREM ↓' : d.gear < 0.01 ? 'TREM ↑' : 'TREM ⇅';
  lamp(ctx, 12, 24, 54, gearTxt, d.gear > 0.99 ? COL.ok : COL.warn, d.gear > 0.01 || ac.gearFixed);
  lamp(ctx, 74, 24, 54, 'FLAPS ' + (ac.flapLabels ? ac.flapLabels[d.flapIdx] : d.flapNotch), COL.ok, d.flapIdx > 0);
  lamp(ctx, 12, 48, 54, d.parking ? 'PARK BRK' : 'FREIO', COL.warn, d.parking || d.brake > 0.1);
  lamp(ctx, 74, 48, 54, 'AIRBRAKE', COL.warn, d.sb > 0.05);
  lamp(ctx, 12, 72, 54, ac.engine.ab ? 'A/B' : 'ASSIST', ac.engine.ab ? COL.bad : COL.ok, ac.engine.ab ? d.ab > 0.3 : d.assist);
  lamp(ctx, 74, 72, 54, 'TRIM ' + (d.trim >= 0 ? '+' : '') + Math.round(d.trim * 100), COL.ok, Math.abs(d.trim) > 0.01);
  const stall = !!d.stallWarn;
  const low = !ac.gearFixed && d.gear < 0.99 && d.agl < 150 && d.ias < 200 && !d.onGround;
  const over = d.ias > ac.asi.red;
  lamp(ctx, 12, 100, 54, 'STALL', COL.bad, stall && fl);
  lamp(ctx, 74, 100, 54, over ? 'OVERSPD' : 'GEAR!', COL.bad, (over || low) && fl);
}

const LIST = [
  { id: 'asi', name: 'Velocímetro (IAS)', draw: asi, on: true },
  { id: 'ai', name: 'Horizonte artificial', draw: ai, on: true },
  { id: 'alt', name: 'Altímetro', draw: alt, on: true },
  { id: 'tc', name: 'Coordenador de curva', draw: tc, on: true },
  { id: 'hdg', name: 'Direcional (rumo)', draw: hdg, on: true },
  { id: 'vsi', name: 'Climb / descida', draw: vsi, on: true },
  { id: 'eng', name: 'Motor e combustível', draw: eng, on: true },
  { id: 'cfg', name: 'Trem, flaps e alertas', draw: cfg, on: true },
  { id: 'aoa', name: 'Ângulo de ataque', draw: aoa, on: false },
  { id: 'g', name: 'G-meter', draw: gm, on: false },
];

const Instruments = {
  list: LIST,
  canvases: {},
  mount(el, enabled) {
    this.el = el;
    el.innerHTML = '';
    for (const it of LIST) {
      const cv = document.createElement('canvas');
      cv.width = S * 2; cv.height = S * 2; cv.title = it.name;
      cv.getContext('2d').scale(2, 2);
      cv.hidden = !enabled.includes(it.id);
      el.appendChild(cv);
      this.canvases[it.id] = cv;
    }
    this.fit();
  },
  fit() { this.el.style.setProperty('--n', Math.max(1, LIST.filter((it) => !this.canvases[it.id].hidden).length)); },
  setEnabled(enabled) { for (const it of LIST) this.canvases[it.id].hidden = !enabled.includes(it.id); this.fit(); },
  update(d) {
    for (const it of LIST) {
      const cv = this.canvases[it.id];
      if (cv.hidden) continue;
      cv.getContext('2d').save();
      it.draw(cv.getContext('2d'), d);
      cv.getContext('2d').restore();
    }
  },
};
root.Instruments = Instruments;
})(typeof self !== 'undefined' ? self : globalThis);
