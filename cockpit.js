(function (root) {
const clamp = (x, a, b) => Math.min(b, Math.max(a, x));
const DEG = 180 / Math.PI;
let cv, ctx, W = 0, H = 0, dpr = 1;

function resize() {
  dpr = Math.min(devicePixelRatio || 1, 1.5);
  W = innerWidth; H = innerHeight;
  cv.width = Math.round(W * dpr); cv.height = Math.round(H * dpr);
}

function mount(el) {
  cv = el; ctx = cv.getContext('2d');
  resize(); addEventListener('resize', resize);
}

const poly = (pts, fill, stroke, lw = 1) => {
  ctx.beginPath(); pts.forEach(([x, y], i) => (i ? ctx.lineTo(x, y) : ctx.moveTo(x, y))); ctx.closePath();
  if (fill) { ctx.fillStyle = fill; ctx.fill(); }
  if (stroke) { ctx.lineWidth = lw; ctx.strokeStyle = stroke; ctx.stroke(); }
};
const lin = (x0, y0, x1, y1, stops) => {
  const g = ctx.createLinearGradient(x0, y0, x1, y1);
  stops.forEach(([t, c]) => g.addColorStop(t, c));
  return g;
};

function yoke(cx, cy, r, roll, pitch, col = '#2d3238') {
  ctx.save();
  ctx.translate(cx, cy + pitch * r * 0.35); ctx.scale(1 + pitch * 0.06, 1 + pitch * 0.06);
  ctx.fillStyle = lin(cx, cy, cx, cy + r * 2, [[0, '#2a2f35'], [1, '#0c0e10']]);
  ctx.fillRect(-r * 0.09, 0, r * 0.18, r * 1.9);
  ctx.rotate(roll * 1.1);
  ctx.lineWidth = r * 0.17; ctx.lineCap = 'round'; ctx.strokeStyle = col;
  ctx.beginPath(); ctx.moveTo(-r, -r * 0.1); ctx.quadraticCurveTo(-r * 1.05, -r * 0.85, -r * 0.5, -r * 0.8); ctx.lineTo(r * 0.5, -r * 0.8); ctx.quadraticCurveTo(r * 1.05, -r * 0.85, r, -r * 0.1); ctx.stroke();
  ctx.lineWidth = r * 0.12; ctx.beginPath(); ctx.moveTo(-r, -r * 0.1); ctx.lineTo(-r * 0.9, r * 0.35); ctx.moveTo(r, -r * 0.1); ctx.lineTo(r * 0.9, r * 0.35); ctx.stroke();
  ctx.lineWidth = r * 0.05; ctx.strokeStyle = '#3a4048';
  ctx.beginPath(); ctx.moveTo(-r * 0.5, -r * 0.84); ctx.lineTo(r * 0.5, -r * 0.84); ctx.stroke();
  ctx.restore();
}

function ga(u, c) {
  poly([[0, 0], [W, 0], [W, 0.075 * H], [0.5 * W, 0.045 * H], [0, 0.075 * H]], lin(0, 0, 0, 0.08 * H, [[0, '#0c0d0f'], [1, '#2a2e33']]));
  poly([[0.16 * W, 0], [0.205 * W, 0], [0.085 * W, 0.7 * H], [0.03 * W, 0.7 * H]], lin(0, 0, 0.2 * W, 0, [[0, '#15181b'], [1, '#2b3036']]), '#07080a', 2);
  poly([[0.84 * W, 0], [0.795 * W, 0], [0.915 * W, 0.7 * H], [0.97 * W, 0.7 * H]], lin(W, 0, 0.8 * W, 0, [[0, '#15181b'], [1, '#2b3036']]), '#07080a', 2);
  poly([[0.497 * W, 0.045 * H], [0.503 * W, 0.045 * H], [0.505 * W, 0.4 * H], [0.495 * W, 0.4 * H]], '#16191c');
  poly([[0, 0.12 * H], [0.03 * W, 0.7 * H], [0, 0.9 * H]], lin(0, 0, 0.03 * W, 0, [[0, '#0a0b0c'], [1, '#24282d']]));
  poly([[W, 0.12 * H], [0.97 * W, 0.7 * H], [W, 0.9 * H]], lin(W, 0, 0.97 * W, 0, [[0, '#0a0b0c'], [1, '#24282d']]));
  poly([[0.03 * W, 0.7 * H], [0.085 * W, 0.7 * H], [0.5 * W, 0.66 * H], [0.915 * W, 0.7 * H], [0.97 * W, 0.7 * H], [W, 0.9 * H], [W, H], [0, H], [0, 0.9 * H]], lin(0, 0.66 * H, 0, H, [[0, '#262a2f'], [0.15, '#1a1d21'], [1, '#0d0f11']]));
  poly([[0.085 * W, 0.7 * H], [0.5 * W, 0.66 * H], [0.915 * W, 0.7 * H], [0.9 * W, 0.725 * H], [0.5 * W, 0.695 * H], [0.1 * W, 0.725 * H]], '#07080a');
  ctx.fillStyle = '#3b4148'; ctx.fillRect(0.12 * W, 0.735 * H, 0.76 * W, 2 * u);
  yoke(0.5 * W, 0.8 * H, 120 * u, c.ctl.roll, c.ctl.pitch);
}

function airliner(u, c) {
  poly([[0, 0], [W, 0], [W, 0.1 * H], [0.5 * W, 0.075 * H], [0, 0.1 * H]], lin(0, 0, 0, 0.1 * H, [[0, '#0b0c0e'], [1, '#2c3036']]));
  for (const [x0, x1, w] of [[0.5, 0.5, 22], [0.31, 0.29, 12], [0.69, 0.71, 12]]) {
    ctx.lineWidth = w * u; ctx.strokeStyle = '#17191c'; ctx.beginPath(); ctx.moveTo(x0 * W, 0.08 * H); ctx.lineTo(x1 * W, 0.6 * H); ctx.stroke();
  }
  poly([[0.1 * W, 0], [0.17 * W, 0], [0.07 * W, 0.66 * H], [0, 0.66 * H], [0, 0.3 * H]], lin(0, 0, 0.17 * W, 0, [[0, '#101214'], [1, '#2a2e33']]), '#06070a', 2);
  poly([[0.9 * W, 0], [0.83 * W, 0], [0.93 * W, 0.66 * H], [W, 0.66 * H], [W, 0.3 * H]], lin(W, 0, 0.83 * W, 0, [[0, '#101214'], [1, '#2a2e33']]), '#06070a', 2);
  poly([[0, 0.66 * H], [0.07 * W, 0.66 * H], [0.5 * W, 0.62 * H], [0.93 * W, 0.66 * H], [W, 0.66 * H], [W, H], [0, H]], lin(0, 0.62 * H, 0, H, [[0, '#2a2e33'], [0.1, '#1b1e22'], [1, '#0c0e10']]));
  ctx.fillStyle = '#050607'; ctx.fillRect(0.07 * W, 0.664 * H, 0.86 * W, 5 * u);
  const scr = [[0.14, 0.72], [0.28, 0.72], [0.42, 0.72], [0.56, 0.72], [0.7, 0.72]];
  for (const [x, y] of scr) {
    ctx.fillStyle = '#05080b'; ctx.strokeStyle = '#3a4048'; ctx.lineWidth = 2;
    ctx.fillRect(x * W, y * H, 0.115 * W, 0.115 * H); ctx.strokeRect(x * W, y * H, 0.115 * W, 0.115 * H);
    ctx.fillStyle = '#0f3a2a'; ctx.fillRect(x * W + 6 * u, y * H + 6 * u, 0.115 * W - 12 * u, 3 * u);
  }
  yoke(0.3 * W, 0.78 * H, 105 * u, c.ctl.roll, c.ctl.pitch);
  yoke(0.72 * W, 0.78 * H, 105 * u, c.ctl.roll, c.ctl.pitch);
}

const LAYOUT = {
  gripen: { frame: true, center: true, screens: [['sms', 0.285], ['radar', 0.5], ['eng', 0.715]], strip: true },
  f16: { frame: false, center: false, screens: [['sms', 0.335], ['radar', 0.665]], icp: true },
  tucano: { frame: true, center: true, screens: [['sms', 0.335], ['radar', 0.665]], icp: true },
};
const LIGHTS = ['MASTER', 'MOTOR', 'FOGO', 'ESTOL', 'MÍSSIL', 'COMB', 'FREIO', 'A/B'];
let fc = null;
const mfdCanvas = [], mfdAt = [];

function buildFighter(id) {
  const L = LAYOUT[id] || LAYOUT.f16, u = H / 1080, mx = Math.round(W * 0.8), my = Math.round(H * 0.7), dn = Math.round(H * 0.6);
  const off = document.createElement('canvas');
  off.width = W + 2 * mx; off.height = H + my + dn;
  const main = ctx;
  ctx = off.getContext('2d');
  ctx.translate(mx, my);
  let seed = 11;
  const rnd = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
  const metal = (y0, y1) => lin(0, y0, 0, y1, [[0, '#343a40'], [0.2, '#262b30'], [1, '#14171a']]);
  const label = (txt, x, y, size = 9, col = '#c9ced2', al = 'center') => { ctx.fillStyle = col; ctx.font = `600 ${size * u}px system-ui, sans-serif`; ctx.textAlign = al; ctx.textBaseline = 'middle'; ctx.fillText(txt, x, y); };
  const screw = (x, y) => { ctx.fillStyle = '#50565c'; ctx.beginPath(); ctx.arc(x, y, 2.4 * u, 0, 7); ctx.fill(); ctx.strokeStyle = '#0d0f11'; ctx.lineWidth = u; ctx.beginPath(); ctx.moveTo(x - 2 * u, y); ctx.lineTo(x + 2 * u, y); ctx.stroke(); };
  const knob = (x, y, r) => { ctx.fillStyle = lin(x, y - r, x, y + r, [[0, '#4a5056'], [1, '#101214']]); ctx.beginPath(); ctx.arc(x, y, r, 0, 7); ctx.fill(); ctx.strokeStyle = '#d5d8da'; ctx.lineWidth = 1.2 * u; ctx.beginPath(); ctx.moveTo(x, y); ctx.lineTo(x + r * 0.7 * (rnd() - 0.5) * 2, y - r * 0.8); ctx.stroke(); };
  const toggle = (x, y) => { ctx.fillStyle = '#0c0e10'; ctx.fillRect(x - 5 * u, y - 7 * u, 10 * u, 14 * u); ctx.fillStyle = '#b9bdc0'; ctx.fillRect(x - 1.6 * u, y - (rnd() > 0.5 ? 9 : 1) * u, 3.2 * u, 9 * u); };
  const console_ = (pts, labels, sx, sy, cols, rows, dx, dy) => {
    poly(pts, metal(pts[0][1], H + dn), '#08090b', 2);
    labels.forEach((t, r) => {
      for (let i = 0; i < cols; i++) {
        const x = sx + i * dx, y = sy + r * dy;
        if ((i + r) % 3 === 0) knob(x, y, 8 * u); else toggle(x, y);
      }
      label(t, sx + (cols - 1) * dx / 2, sy + r * dy + 15 * u, 8, '#9aa2a8');
    });
    void rows;
  };

  for (const m of [1, -1]) {
    const X = (x) => (m > 0 ? x : W - x);
    poly([[X(-mx), 0.5 * H], [X(0.02 * W), 0.62 * H], [X(0.125 * W), 0.8 * H], [X(0.2 * W), H + dn], [X(-mx), H + dn]], lin(X(-0.2 * W), 0, X(0.2 * W), 0, [[0, '#121518'], [1, '#2a2f34']]), '#050607', 2);
    ctx.lineJoin = 'round'; ctx.lineCap = 'round';
    ctx.lineWidth = 15 * u; ctx.strokeStyle = '#2e3338'; ctx.beginPath(); ctx.moveTo(X(-mx), 0.5 * H); ctx.lineTo(X(0.02 * W), 0.62 * H); ctx.lineTo(X(0.125 * W), 0.8 * H); ctx.stroke();
    ctx.lineWidth = 2.5 * u; ctx.strokeStyle = '#59616a'; ctx.beginPath(); ctx.moveTo(X(-mx), 0.5 * H - 6 * u); ctx.lineTo(X(0.02 * W), 0.62 * H - 6 * u); ctx.lineTo(X(0.125 * W), 0.8 * H - 6 * u); ctx.stroke();
  }
  console_([[-0.24 * W, 0.86 * H], [0.13 * W, 0.86 * H], [0.19 * W, H + dn * 0.6], [-0.34 * W, H + dn * 0.6]], ['COMBUSTÍVEL', 'ILUMINAÇÃO', 'OXIGÊNIO', 'RÁDIO'], -0.13 * W, 0.895 * H, 6, 4, 0.038 * W, 0.075 * H);
  console_([[1.24 * W, 0.86 * H], [0.87 * W, 0.86 * H], [0.81 * W, H + dn * 0.6], [1.34 * W, H + dn * 0.6]], ['ELÉTRICA', 'HIDRÁULICA', 'CONTRAMEDIDAS', 'PAINEL'], 0.92 * W, 0.895 * H, 6, 4, 0.038 * W, 0.075 * H);

  poly([[0.1 * W, 0.78 * H], [0.9 * W, 0.78 * H], [0.84 * W, H + dn], [0.16 * W, H + dn]], metal(0.78 * H, H + dn * 0.5), '#07080a', 2);
  ctx.strokeStyle = 'rgba(0,0,0,.45)'; ctx.lineWidth = 1.2 * u;
  for (const y of [0.975, 1.08, 1.2]) { ctx.beginPath(); ctx.moveTo(0.13 * W, y * H); ctx.lineTo(0.87 * W, y * H); ctx.stroke(); }
  for (let i = 0; i < 14; i++) screw(0.13 * W + i * 0.057 * W, 0.97 * H);

  const screens = [], mh = 0.2 * H, mw = mh * 1.22, my0 = 0.785 * H;
  for (const [page, cx] of L.screens) {
    const x = cx * W - mw / 2, y = my0, b = 15 * u;
    ctx.fillStyle = '#16191c'; ctx.beginPath(); ctx.roundRect(x - b, y - b, mw + 2 * b, mh + 2 * b, 7 * u); ctx.fill();
    ctx.strokeStyle = '#3d4349'; ctx.lineWidth = 1.5 * u; ctx.stroke();
    for (let i = 0; i < 5; i++) {
      const bx = x + (i + 0.5) * mw / 5, by = y + (i + 0.5) * mh / 5;
      ctx.fillStyle = '#3a4046';
      for (const [px, py, pw, ph] of [[bx - 8 * u, y - 11 * u, 16 * u, 7 * u], [bx - 8 * u, y + mh + 4 * u, 16 * u, 7 * u], [x - 11 * u, by - 7 * u, 7 * u, 14 * u], [x + mw + 4 * u, by - 7 * u, 7 * u, 14 * u]]) { ctx.beginPath(); ctx.roundRect(px, py, pw, ph, 1.5 * u); ctx.fill(); }
    }
    ctx.fillStyle = '#020304'; ctx.fillRect(x, y, mw, mh);
    screens.push({ page, x, y, w: mw, h: mh });
  }

  const lx = 0.128 * W, ly = 0.8 * H, lw = 0.058 * W, lh = 0.028 * H, lights = [];
  ctx.fillStyle = '#0e1012'; ctx.beginPath(); ctx.roundRect(lx - 6 * u, ly - 6 * u, lw + 12 * u, (lh + 5 * u) * LIGHTS.length + 7 * u, 4 * u); ctx.fill();
  LIGHTS.forEach((t, i) => {
    const y = ly + i * (lh + 5 * u);
    ctx.fillStyle = '#191c1f'; ctx.fillRect(lx, y, lw, lh);
    label(t, lx + lw / 2, y + lh / 2, 9, '#50565b');
    lights.push({ t, x: lx, y, w: lw, h: lh });
  });

  const gears = [], gx = 0.845 * W, gy = 0.795 * H;
  ctx.fillStyle = '#0e1012'; ctx.beginPath(); ctx.roundRect(gx - 36 * u, gy - 10 * u, 72 * u, 46 * u, 4 * u); ctx.fill();
  label('TREM', gx, gy - 1 * u, 8, '#9aa2a8');
  for (const [dx, dy] of [[0, 11], [-18, 26], [18, 26]]) gears.push({ x: gx + dx * u, y: gy + dy * u });
  const stby = [{ x: 0.845 * W, y: 0.875 * H, r: 33 * u, t: 'KT' }, { x: 0.845 * W, y: 0.952 * H, r: 33 * u, t: 'ALT' }];
  for (const g of stby) {
    ctx.fillStyle = '#08090a'; ctx.beginPath(); ctx.arc(g.x, g.y, g.r, 0, 7); ctx.fill();
    ctx.strokeStyle = '#4a5056'; ctx.lineWidth = 2.5 * u; ctx.stroke();
    ctx.strokeStyle = '#e4e6e7'; ctx.lineWidth = 1.2 * u;
    for (let i = 0; i < 10; i++) { const a = i / 10 * 6.283, r0 = g.r * 0.8; ctx.beginPath(); ctx.moveTo(g.x + Math.sin(a) * r0, g.y - Math.cos(a) * r0); ctx.lineTo(g.x + Math.sin(a) * g.r * 0.94, g.y - Math.cos(a) * g.r * 0.94); ctx.stroke(); }
    label(g.t, g.x, g.y + g.r * 0.42, 8, '#cfd3d6');
  }

  let ded = null;
  if (L.icp) {
    const ix = 0.43 * W, iy = my0 - 6 * u, iw = 0.14 * W, ih = mh + 12 * u;
    ctx.fillStyle = '#1b1e22'; ctx.beginPath(); ctx.roundRect(ix, iy, iw, ih, 5 * u); ctx.fill(); ctx.strokeStyle = '#3d4349'; ctx.lineWidth = 1.2 * u; ctx.stroke();
    ded = { x: ix + 8 * u, y: iy + 8 * u, w: iw - 16 * u, h: ih * 0.34 };
    ctx.fillStyle = '#020503'; ctx.fillRect(ded.x, ded.y, ded.w, ded.h);
    const keys = ['1', '2', '3', 'COM', '4', '5', '6', 'NAV', '7', '8', '9', 'A-G', 'CLR', '0', 'ENT', 'A-A'];
    keys.forEach((k, i) => {
      const kw = (iw - 16 * u) / 4, kh = (ih * 0.56) / 4, kx = ix + 8 * u + (i % 4) * kw, ky = iy + ih * 0.41 + Math.floor(i / 4) * kh;
      ctx.fillStyle = lin(kx, ky, kx, ky + kh, [[0, '#3e444a'], [1, '#22262a']]); ctx.beginPath(); ctx.roundRect(kx + 2 * u, ky + 2 * u, kw - 4 * u, kh - 4 * u, 2.5 * u); ctx.fill();
      label(k, kx + kw / 2, ky + kh / 2, 9, '#e8eaeb');
    });
  }

  ctx.beginPath(); ctx.moveTo(0.085 * W, 0.812 * H); ctx.quadraticCurveTo(0.5 * W, 0.655 * H, 0.915 * W, 0.812 * H); ctx.lineTo(0.895 * W, 0.83 * H); ctx.quadraticCurveTo(0.5 * W, 0.695 * H, 0.105 * W, 0.83 * H); ctx.closePath();
  ctx.fillStyle = lin(0, 0.69 * H, 0, 0.82 * H, [[0, '#23262a'], [0.35, '#101214'], [1, '#050607']]); ctx.fill();
  ctx.setLineDash([5 * u, 4 * u]); ctx.lineWidth = 1 * u; ctx.strokeStyle = 'rgba(150,155,160,.32)';
  ctx.beginPath(); ctx.moveTo(0.1 * W, 0.812 * H); ctx.quadraticCurveTo(0.5 * W, 0.672 * H, 0.9 * W, 0.812 * H); ctx.stroke(); ctx.setLineDash([]);

  poly([[0.41 * W, 0.755 * H], [0.59 * W, 0.755 * H], [0.565 * W, 0.697 * H], [0.435 * W, 0.697 * H]], lin(0, 0.697 * H, 0, 0.755 * H, [[0, '#2a2e33'], [1, '#0a0b0d']]), '#040506', 1.5);
  const hw = 0.14 * W, top = 0.285 * H, bot = 0.705 * H, gx0 = 0.5 * W;
  ctx.strokeStyle = '#1a1d20'; ctx.lineWidth = 7 * u; ctx.lineCap = 'round';
  ctx.beginPath(); ctx.moveTo(gx0 - hw - 3 * u, bot); ctx.lineTo(gx0 - hw * 1.02, top); ctx.moveTo(gx0 + hw + 3 * u, bot); ctx.lineTo(gx0 + hw * 1.02, top); ctx.stroke();
  ctx.lineWidth = 2 * u; ctx.strokeStyle = 'rgba(190,200,205,.35)'; ctx.strokeRect(gx0 - hw * 1.02, top, hw * 2.04, bot - top);
  ctx.fillStyle = lin(gx0 - hw, top, gx0 + hw, bot, [[0, 'rgba(130,220,190,.05)'], [0.5, 'rgba(130,220,190,.02)'], [1, 'rgba(160,230,200,.06)']]); ctx.fillRect(gx0 - hw, top, hw * 2, bot - top);

  let strip = null;
  if (L.strip) {
    const sx = 0.43 * W, sy = 0.74 * H, sw = 0.14 * W, sh = 0.034 * H;
    ctx.fillStyle = '#1b1e22'; ctx.fillRect(sx - 3 * u, sy - 3 * u, sw + 6 * u, sh + 6 * u);
    strip = { x: sx, y: sy, w: sw, h: sh };
    ctx.fillStyle = '#020503'; ctx.fillRect(sx, sy, sw, sh);
  }

  if (L.frame) {
    ctx.lineCap = 'butt';
    for (const m of [1, -1]) {
      const X = (x) => (m > 0 ? x : W - x);
      ctx.lineWidth = 13 * u; ctx.strokeStyle = lin(X(0.1 * W), 0, X(0.03 * W), 0, [[0, '#23272b'], [1, '#0c0e10']]);
      ctx.beginPath(); ctx.moveTo(X(0.125 * W), 0.795 * H); ctx.quadraticCurveTo(X(0.08 * W), 0.2 * H, X(0.34 * W), -0.33 * H); ctx.stroke();
      ctx.lineWidth = 1.5 * u; ctx.strokeStyle = 'rgba(200,210,215,.25)'; ctx.beginPath(); ctx.moveTo(X(0.133 * W), 0.795 * H); ctx.quadraticCurveTo(X(0.088 * W), 0.2 * H, X(0.348 * W), -0.33 * H); ctx.stroke();
    }
    ctx.lineWidth = 15 * u; ctx.strokeStyle = '#14171a'; ctx.beginPath(); ctx.moveTo(0.34 * W, -0.33 * H); ctx.quadraticCurveTo(0.5 * W, -0.42 * H, 0.66 * W, -0.33 * H); ctx.stroke();
    ctx.fillStyle = '#1b1e22'; ctx.beginPath(); ctx.roundRect(0.46 * W, -0.34 * H, 0.08 * W, 0.04 * H, 4 * u); ctx.fill();
  } else {
    ctx.strokeStyle = 'rgba(190,205,215,.13)'; ctx.lineWidth = 1.5 * u; ctx.beginPath(); ctx.moveTo(-0.3 * W, 0.42 * H); ctx.quadraticCurveTo(0.5 * W, -0.75 * H, 1.3 * W, 0.42 * H); ctx.stroke();
  }
  ctx.fillStyle = lin(0.02 * W, 0, 0.3 * W, 0.5 * H, [[0, 'rgba(255,255,255,.035)'], [0.5, 'rgba(255,255,255,0)'], [1, 'rgba(255,255,255,.012)']]); ctx.fillRect(-mx, -my, W + 2 * mx, 0.62 * H + my);

  ctx = main;
  return { id, W, H, off, mx, my, L, screens, lights, gears, stby, ded, strip };
}

function mfdPage(m, page, c, f) {
  const w = 320, h = 256, G = '#68ff9e', A = '#ffc14d', R = '#ff5a52', d = c.d || {}, s = c.s, ac = c.ac, cmb = c.cmb;
  m.fillStyle = '#020604'; m.fillRect(0, 0, w, h);
  m.textBaseline = 'alphabetic'; m.textAlign = 'left';
  const t = (x, y, str, col = G, size = 15, al = 'left') => { m.fillStyle = col; m.font = `600 ${size}px ui-monospace, Consolas, monospace`; m.textAlign = al; m.fillText(str, x, y); };
  const tabs = f.L.screens.map((e) => e[0].toUpperCase());
  tabs.forEach((n, i) => t(40 + i * 86, 16, n === 'RADAR' ? 'RDR' : n, f.L.screens[i][0] === page ? '#fff' : '#3f8a5c', 12));
  m.strokeStyle = 'rgba(104,255,158,.3)'; m.lineWidth = 1; m.beginPath(); m.moveTo(8, 22); m.lineTo(w - 8, 22); m.stroke();
  if (page === 'radar') {
    if (cmb && c.radar) { m.drawImage(c.radar, (w - 222) / 2, 26, 222, 222); return; }
    const R0 = 96, cx = w / 2, cy = 138, hd = (d.hdg ?? 0) * Math.PI / 180;
    m.strokeStyle = G; m.beginPath(); m.arc(cx, cy, R0, 0, 7); m.stroke();
    for (let a = 0; a < 36; a++) { const b = a * Math.PI / 18 - hd, r0 = a % 3 ? R0 - 6 : R0 - 12; m.beginPath(); m.moveTo(cx + Math.sin(b) * r0, cy - Math.cos(b) * r0); m.lineTo(cx + Math.sin(b) * R0, cy - Math.cos(b) * R0); m.stroke(); if (a % 9 === 0) t(cx + Math.sin(b) * (R0 - 24), cy - Math.cos(b) * (R0 - 24) + 5, 'NESO'[a / 9], G, 14, 'center'); }
    if (c.brg != null) { const b = c.brg * Math.PI / 180 - hd; m.strokeStyle = '#ff6fd8'; m.lineWidth = 2.5; m.beginPath(); m.moveTo(cx, cy); m.lineTo(cx + Math.sin(b) * (R0 - 14), cy - Math.cos(b) * (R0 - 14)); m.stroke(); }
    t(cx, cy + 6, String(Math.round(d.hdg ?? 0)).padStart(3, '0'), '#fff', 16, 'center');
    return;
  }
  if (page === 'eng') {
    const n = (s.n ?? 0), fuelF = (s.fuel ?? 0) / ac.fuelMax;
    const bar = (y, lbl, v, col, txt) => { t(10, y + 11, lbl, G, 13); m.strokeStyle = 'rgba(104,255,158,.5)'; m.strokeRect(62, y, 180, 14); m.fillStyle = col; m.fillRect(63, y + 1, 178 * Math.max(0, Math.min(1, v)), 12); t(w - 10, y + 12, txt, '#fff', 13, 'right'); };
    bar(34, ac.engine.nlabel, n, G, `${Math.round(n * 100)}%`);
    bar(58, 'A/B', s.ab ?? 0, A, `${Math.round((s.ab ?? 0) * 100)}%`);
    bar(82, 'FUEL', fuelF, fuelF < 0.15 ? R : G, `${Math.round(s.fuel ?? 0)}kg`);
    const hp = cmb ? cmb.pl.hp / cmb.pl.hpMax : 1;
    bar(106, 'CASCO', hp, hp > 0.6 ? G : hp > 0.3 ? A : R, `${Math.round(hp * 100)}%`);
    t(10, 150, `FLAP ${ac.flapLabels?.[c.ctl.flapIdx] ?? ac.flapNotches[c.ctl.flapIdx]}`, G); t(130, 150, `TREM ${s.gear > 0.99 ? 'BAIXO' : s.gear < 0.01 ? 'CIMA' : 'MOV'}`, s.gear > 0.01 && s.gear < 0.99 ? A : G);
    t(10, 174, `SB ${s.sb > 0.05 ? 'ABERTO' : 'FECH'}`, s.sb > 0.05 ? A : G); t(130, 174, `G ${(d.nz ?? 1).toFixed(1)} / ${c.gmax.toFixed(1)}`, G);
    t(10, 198, `FLUXO ${((s.flow ?? 0) * 3600).toFixed(0)} KG/H`, G); t(10, 222, `M ${(d.mach ?? 0).toFixed(2)}  VS ${Math.round(d.vsi ?? 0)}`, '#fff');
    return;
  }
  if (!cmb) { t(w / 2, 110, ac.weapons ? 'ARMAMENTO SEGURO' : 'SEM ARMAMENTO', A, 16, 'center'); t(w / 2, 136, 'VOO LIVRE', G, 14, 'center'); return; }
  const wn = cmb.w, sol = cmb.sol, sel = cmb.sel, items = [['gun', 'CAN', wn.gun], ['aam', 'AAM', wn.aam], ['bomb', 'BOM', wn.bomb], ['rkt', 'FOG', wn.rkt]].filter((e) => cmb.avail.includes(e[0]));
  items.forEach(([k, n, v], i) => {
    const y = 42 + i * 22;
    if (k === sel) { m.fillStyle = G; m.fillRect(8, y - 15, 112, 20); t(12, y, `${n} ${v}`, '#02140a', 15); } else t(12, y, `${n} ${v}`, v ? G : R, 15);
  });
  t(12, 42 + items.length * 22, `FLR ${wn.flares}`, wn.flares ? G : R, 15);
  const px = 222, py = 76;
  m.strokeStyle = G; m.lineWidth = 1.5; m.beginPath(); m.moveTo(px, py - 42); m.lineTo(px + 7, py - 8); m.lineTo(px + 62, py + 26); m.lineTo(px + 62, py + 34); m.lineTo(px + 8, py + 22); m.lineTo(px + 8, py + 40); m.lineTo(px - 8, py + 40); m.lineTo(px - 8, py + 22); m.lineTo(px - 62, py + 34); m.lineTo(px - 62, py + 26); m.lineTo(px - 7, py - 8); m.closePath(); m.stroke();
  const st = ac.weapons.stations;
  for (const [kind, n, col] of [['aam', wn.aam, '#fff'], ['bomb', wn.bomb, A], ['pod', wn.rkt > 0 ? 99 : 0, '#7cc8ff']]) (st[kind] || []).forEach((at, i) => {
    const b = ac.b / 2, x = px + (at[1] / b) * 60, y = py + 12 - at[0] * 5;
    m.fillStyle = i < n ? col : 'rgba(255,255,255,.14)'; m.fillRect(x - 3, y - 3, 6, 6);
  });
  m.strokeStyle = 'rgba(104,255,158,.3)'; m.beginPath(); m.moveTo(8, 146); m.lineTo(w - 8, 146); m.stroke();
  if (sol) {
    t(12, 168, sol.ccrp ? 'A/G CCRP' : sol.kind === 'bomb' ? 'A/G CCIP' : 'A/G FOG', '#fff', 15);
    if (sol.p) t(w - 12, 168, `${(sol.range / 1000).toFixed(1)}KM ${sol.tof.toFixed(1)}S`, G, 14, 'right'); else t(w - 12, 168, 'SEM SOLUCAO', A, 14, 'right');
    if (sol.tgt) {
      t(12, 192, sol.tgt.name.toUpperCase().slice(0, 22), G, 14);
      t(12, 214, `DIST ${(sol.dist / 1000).toFixed(1)} ERRO ${Math.round(sol.miss)}M`, sol.onTarget ? R : G, 14);
      if (sol.ccrp) t(12, 236, cmb.pickle ? `ARMADO ${Math.max(0, sol.ttr).toFixed(1)}S` : `SOLTURA ${Math.max(0, sol.ttr).toFixed(1)}S`, cmb.pickle ? R : G, 14);
      else if (sol.onTarget) t(12, 236, sol.kind === 'bomb' ? 'LANCE' : 'ATIRE', R, 15);
    } else t(12, 192, 'SEM ALVO: APERTE T', A, 14);
  } else if (sel === 'aam') {
    t(12, 168, 'A/A IR', '#fff', 15);
    const tg = cmb.tgt;
    t(12, 192, tg ? tg.name.toUpperCase() : 'SEM ALVO', tg ? G : A, 14);
    t(12, 214, cmb.lock.ok ? 'TRAVADO' : cmb.lock.t > 0.05 ? 'TRAVANDO' : 'BUSCANDO', cmb.lock.ok ? R : G, 15);
  } else {
    t(12, 168, 'A/A CANHAO', '#fff', 15);
    t(12, 192, ac.weapons.gun.name.toUpperCase().slice(0, 24), G, 12);
    if (cmb.tgt) t(12, 214, cmb.tgt.name.toUpperCase().slice(0, 22), G, 14);
  }
}

function fighter(u, c, fov) {
  if (!fc || fc.id !== c.ac.id || fc.W !== W || fc.H !== H) fc = buildFighter(c.ac.id);
  const f = fc, now = performance.now(), d = c.d || {}, s = c.s, cmb = c.cmb;
  ctx.drawImage(f.off, -f.mx, -f.my);
  f.screens.forEach((sc, i) => {
    if (!mfdCanvas[i]) { mfdCanvas[i] = document.createElement('canvas'); mfdCanvas[i].width = 320; mfdCanvas[i].height = 256; mfdAt[i] = 0; }
    if (now - mfdAt[i] > 110) { mfdAt[i] = now; mfdPage(mfdCanvas[i].getContext('2d'), sc.page, c, f); }
    ctx.drawImage(mfdCanvas[i], sc.x, sc.y, sc.w, sc.h);
    ctx.fillStyle = lin(sc.x, sc.y, sc.x + sc.w, sc.y + sc.h, [[0, 'rgba(255,255,255,.055)'], [0.4, 'rgba(255,255,255,0)'], [1, 'rgba(255,255,255,.015)']]);
    ctx.fillRect(sc.x, sc.y, sc.w, sc.h);
  });
  const hp = cmb ? cmb.pl.hp : 100, fuelLow = (s.fuel ?? 0) / c.ac.fuelMax < 0.15;
  const on = { MASTER: hp < 60 || !!cmb?.warn.msl || fuelLow, MOTOR: hp < 60, FOGO: hp < 30, ESTOL: !!d.stallWarn, 'MÍSSIL': !!cmb?.warn.msl, COMB: fuelLow, FREIO: !!c.ctl.parking, 'A/B': s.ab > 0.05 };
  const blink = Math.floor(now / 220) % 2 === 0;
  for (const l of f.lights) {
    if (!on[l.t] || ((l.t === 'MÍSSIL' || l.t === 'MASTER') && !blink)) continue;
    const red = l.t === 'FOGO' || l.t === 'MÍSSIL' || l.t === 'MASTER';
    ctx.fillStyle = red ? '#d3261c' : l.t === 'A/B' || l.t === 'FREIO' ? '#22a650' : '#d99a14';
    ctx.fillRect(l.x, l.y, l.w, l.h);
    ctx.fillStyle = '#0b0b0b'; ctx.font = `700 ${9 * u}px system-ui, sans-serif`; ctx.textAlign = 'center'; ctx.textBaseline = 'middle'; ctx.fillText(l.t, l.x + l.w / 2, l.y + l.h / 2);
  }
  const gearCol = s.gear > 0.99 ? '#2bdc66' : s.gear > 0.01 ? '#e01f18' : '#1b1e20';
  for (const g of f.gears) { ctx.fillStyle = gearCol; ctx.beginPath(); ctx.arc(g.x, g.y, 5.5 * u, 0, 7); ctx.fill(); }
  const needle = (g, a, col = '#fff') => { ctx.strokeStyle = col; ctx.lineWidth = 2 * u; ctx.lineCap = 'round'; ctx.beginPath(); ctx.moveTo(g.x, g.y); ctx.lineTo(g.x + Math.sin(a) * g.r * 0.76, g.y - Math.cos(a) * g.r * 0.76); ctx.stroke(); };
  needle(f.stby[0], clamp(d.ias ?? 0, 0, 900) / 900 * 5.5);
  needle(f.stby[1], (c.altFt % 1000) / 1000 * 6.283);
  needle(f.stby[1], (c.altFt % 10000) / 10000 * 6.283, '#9aa2a8');
  const hdg = String(Math.round(d.hdg ?? 0) % 360).padStart(3, '0'), line = `HDG ${hdg}  M${(d.mach ?? 0).toFixed(2)}  ${Math.round(c.altFt / 100) * 100}FT`;
  const dedDraw = (r, l1, l2) => {
    ctx.fillStyle = '#6dff9f'; ctx.font = `600 ${Math.round(r.h * (l2 ? 0.32 : 0.56))}px ui-monospace, Consolas, monospace`; ctx.textAlign = 'left'; ctx.textBaseline = 'middle';
    ctx.fillText(l1, r.x + 5 * u, r.y + r.h * (l2 ? 0.3 : 0.5), r.w - 10 * u);
    if (l2) ctx.fillText(l2, r.x + 5 * u, r.y + r.h * 0.72, r.w - 10 * u);
  };
  if (f.ded) dedDraw(f.ded, line, `${c.ac.engine.nlabel} ${Math.round((s.n ?? 0) * 100)}%  FUEL ${Math.round(s.fuel ?? 0)}`);
  if (f.strip) dedDraw(f.strip, line);

  const st = c.ctl, side = !f.L.center;
  const sx = side ? 0.79 * W : 0.5 * W, sy = side ? 1.02 * H : 1.13 * H;
  ctx.save(); ctx.translate(sx, sy); ctx.rotate(st.roll * (side ? 0.07 : 0.2));
  const lean = st.pitch * (side ? 3 : 16) * u, gh = side ? 110 : 165;
  ctx.fillStyle = '#0c0e10'; ctx.beginPath(); ctx.roundRect(-11 * u, -(gh - 60) * u + lean, 22 * u, 300 * u, 6 * u); ctx.fill();
  ctx.fillStyle = lin(-22 * u, 0, 22 * u, 0, [[0, '#14171a'], [0.5, '#30353a'], [1, '#101215']]); ctx.beginPath(); ctx.roundRect(-20 * u, -gh * u + lean, 40 * u, 92 * u, 13 * u); ctx.fill();
  ctx.fillStyle = '#6d7277'; ctx.beginPath(); ctx.arc(0, (-gh + 16) * u + lean, 6 * u, 0, 7); ctx.fill();
  ctx.fillStyle = '#b02a22'; ctx.fillRect(12 * u, (-gh + 28) * u + lean, 7 * u, 19 * u);
  ctx.restore();
  const tx = 0.12 * W, ty = 1.0 * H, ts = st.throttle;
  ctx.fillStyle = '#101214'; ctx.fillRect(tx - 28 * u, ty - 66 * u, 56 * u, 150 * u);
  ctx.fillStyle = '#050607'; ctx.fillRect(tx - 5 * u, ty - 60 * u, 10 * u, 130 * u);
  const ky = ty + 56 * u - ts * 100 * u;
  ctx.fillStyle = lin(tx - 24 * u, 0, tx + 24 * u, 0, [[0, '#14171a'], [0.5, '#353a40'], [1, '#101215']]); ctx.beginPath(); ctx.roundRect(tx - 24 * u, ky - 28 * u, 48 * u, 42 * u, 10 * u); ctx.fill();
  ctx.fillStyle = '#6d7277'; ctx.beginPath(); ctx.arc(tx, ky - 14 * u, 4.5 * u, 0, 7); ctx.fill();

  hud(u, c, 0.5 * W, 0.5 * H, 0.14 * W, 0.2 * H, fov);
}

function hud(u, c, cx, cy, hw, hh, fov) {
  const d = { ias: 0, mach: 0, nz: 1, alpha: 0, beta: 0, hdg: 0, pitch: 0, roll: 0, vsi: 0, ...c.d }, G = '#5dff9a';
  const ppd = (H / 2) / Math.tan(fov.y / 2) * Math.PI / 180;
  ctx.save();
  ctx.beginPath(); ctx.rect(cx - hw, cy - hh, 2 * hw, 2 * hh); ctx.clip();
  ctx.strokeStyle = G; ctx.fillStyle = G; ctx.lineWidth = 2 * u; ctx.shadowColor = G; ctx.shadowBlur = 6 * u;
  ctx.font = `${Math.round(17 * u)}px ui-monospace, Consolas, monospace`; ctx.textBaseline = 'middle';
  const pitch = d.pitch * DEG, roll = d.roll;
  ctx.save(); ctx.translate(cx, cy); ctx.rotate(-roll);
  for (let p = -90; p <= 90; p += 5) {
    const y = (pitch - p) * ppd;
    if (Math.abs(y) > hh * 1.6) continue;
    if (p === 0) { ctx.beginPath(); ctx.moveTo(-hw * 1.4, y); ctx.lineTo(-48 * u, y); ctx.moveTo(48 * u, y); ctx.lineTo(hw * 1.4, y); ctx.stroke(); continue; }
    const w = (p % 10 === 0 ? 62 : 36) * u, sg = p > 0 ? 1 : -1;
    ctx.beginPath();
    if (p < 0) ctx.setLineDash([8 * u, 6 * u]);
    ctx.moveTo(-w - 24 * u, y + sg * 9 * u); ctx.lineTo(-w - 24 * u, y); ctx.lineTo(-24 * u, y);
    ctx.moveTo(w + 24 * u, y + sg * 9 * u); ctx.lineTo(w + 24 * u, y); ctx.lineTo(24 * u, y);
    ctx.stroke(); ctx.setLineDash([]);
    if (p % 10 === 0) { ctx.textAlign = 'right'; ctx.fillText(Math.abs(p), -w - 32 * u, y); ctx.textAlign = 'left'; ctx.fillText(Math.abs(p), w + 32 * u, y); }
  }
  ctx.restore();
  const fx = cx + (d.beta || 0) * DEG * ppd, fy = cy + (d.alpha || 0) * DEG * ppd;
  const px = clamp(fx, cx - hw + 20 * u, cx + hw - 20 * u), py = clamp(fy, cy - hh + 20 * u, cy + hh - 20 * u);
  ctx.beginPath(); ctx.arc(px, py, 9 * u, 0, 2 * Math.PI); ctx.moveTo(px - 9 * u, py); ctx.lineTo(px - 22 * u, py); ctx.moveTo(px + 9 * u, py); ctx.lineTo(px + 22 * u, py); ctx.moveTo(px, py - 9 * u); ctx.lineTo(px, py - 20 * u); ctx.stroke();
  ctx.beginPath(); ctx.arc(cx, cy, 3 * u, 0, 2 * Math.PI); ctx.stroke();
  const box = (x, y, w, txt, al) => { ctx.strokeRect(x - w / 2, y - 14 * u, w, 28 * u); ctx.textAlign = 'center'; ctx.fillText(txt, x, y); };
  box(cx - hw + 60 * u, cy, 86 * u, String(Math.round(d.ias)));
  box(cx + hw - 70 * u, cy, 106 * u, String(Math.round(c.altFt)));
  box(cx, cy - hh + 24 * u, 70 * u, String(Math.round(d.hdg)).padStart(3, '0'));
  ctx.textAlign = 'left';
  ctx.fillText('M ' + d.mach.toFixed(2), cx - hw + 12 * u, cy + hh - 62 * u);
  ctx.fillText('G ' + d.nz.toFixed(1), cx - hw + 12 * u, cy + hh - 38 * u);
  ctx.fillText('AOA ' + (d.alpha * DEG).toFixed(0), cx - hw + 12 * u, cy + hh - 14 * u);
  ctx.textAlign = 'right';
  ctx.fillText('VS ' + Math.round(d.vsi), cx + hw - 12 * u, cy + hh - 38 * u);
  if (c.s.ab > 0.3) ctx.fillText('A/B', cx + hw - 12 * u, cy + hh - 14 * u);
  ctx.restore();
}

function gfx(c) {
  const g = c.gfx;
  if (!g || (g.black < 0.02 && g.red < 0.02)) return;
  const r = Math.hypot(W, H) / 2;
  const grad = ctx.createRadialGradient(W / 2, H / 2, r * (0.75 - 0.7 * Math.max(g.black, g.red)), W / 2, H / 2, r);
  const rgb = g.red > g.black ? '160,0,0' : '0,0,0';
  grad.addColorStop(0, `rgba(${rgb},0)`); grad.addColorStop(1, `rgba(${rgb},${clamp(Math.max(g.black, g.red) * 1.2, 0, 1)})`);
  ctx.fillStyle = grad; ctx.fillRect(0, 0, W, H);
  if (g.black > 0.95) { ctx.fillStyle = '#000'; ctx.fillRect(0, 0, W, H); }
}

function draw(c) {
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  ctx.clearRect(0, 0, W, H);
  if (!c.on) { gfx(c); return; }
  const u = H / 1080;
  const fov = { y: c.fovy, x: 2 * Math.atan(Math.tan(c.fovy / 2) * W / H) };
  const ox = Math.tan(c.head.yaw) * (W / 2) / Math.tan(fov.x / 2), oy = Math.tan(c.head.pitch) * (H / 2) / Math.tan(fov.y / 2);
  ctx.save();
  ctx.translate(clamp(ox, -W, W), clamp(oy, -H, H));
  ({ ga, airliner, fighter }[c.ac.cockpit] || ga)(u, c, fov);
  ctx.restore();
  gfx(c);
  ctx.save(); ctx.restore();
}

root.Cockpit = { mount, draw };
})(typeof self !== 'undefined' ? self : globalThis);
