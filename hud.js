(function (root) {
const GREEN = '#58ff8c', RED = '#ff4d4d', AMBER = '#ffc14d', BLUE = '#6fc3ff';
const KM = [8, 16, 32];
const WNAME = { gun: 'CANHÃO', aam: 'MÍSSIL AR-AR', bomb: 'BOMBAS', rkt: 'FOGUETES' };

function create(cv, rcv) {
  const g = cv.getContext('2d'), rg = rcv.getContext('2d');
  const st = { rangeIdx: 1, w: 0, h: 0, dpr: 1, hit: 0, flash: 0, cam: null };
  const hud = st;

  function resize() {
    st.dpr = Math.min(window.devicePixelRatio || 1, 2);
    st.w = innerWidth; st.h = innerHeight;
    cv.width = st.w * st.dpr; cv.height = st.h * st.dpr;
    rcv.width = rcv.height = 190 * st.dpr;
    g.setTransform(st.dpr, 0, 0, st.dpr, 0, 0);
    rg.setTransform(st.dpr, 0, 0, st.dpr, 0, 0);
  }
  addEventListener('resize', resize);
  resize();

  function proj(cam, th, aspect, wp) {
    const v = [wp.x - cam.p.x, wp.y - cam.p.y, wp.z - cam.p.z];
    const f = v[0] * cam.d.x + v[1] * cam.d.y + v[2] * cam.d.z;
    const rx = v[0] * cam.r.x + v[1] * cam.r.y + v[2] * cam.r.z, uy = v[0] * cam.u.x + v[1] * cam.u.y + v[2] * cam.u.z;
    if (f <= 1) return { x: -1e4, y: -1e4, front: false, rx, uy };
    return { x: st.w / 2 + rx / f / (th * aspect) * (st.w / 2), y: st.h / 2 - uy / f / th * (st.h / 2), front: true, rx, uy };
  }

  function glow(on = true) { g.shadowColor = on ? GREEN : 'transparent'; g.shadowBlur = on ? 5 : 0; }
  function circle(x, y, r, col, lw = 2) { g.strokeStyle = col; g.lineWidth = lw; g.beginPath(); g.arc(x, y, r, 0, Math.PI * 2); g.stroke(); }
  function text(s, x, y, col, size = 13, align = 'left') { g.fillStyle = col; g.font = `600 ${size}px system-ui, sans-serif`; g.textAlign = align; g.fillText(s, x, y); }
  function bracket(x, y, s, col, lw = 2) {
    const k = s * 0.35;
    g.strokeStyle = col; g.lineWidth = lw; g.beginPath();
    for (const [sx, sy] of [[-1, -1], [1, -1], [1, 1], [-1, 1]]) { g.moveTo(x + sx * s, y + sy * (s - k)); g.lineTo(x + sx * s, y + sy * s); g.lineTo(x + sx * (s - k), y + sy * s); }
    g.stroke();
  }
  function edgeArrow(p, col, label) {
    const ang = Math.atan2(-p.uy, p.rx), m = 52, cx = st.w / 2, cy = st.h / 2;
    const dx = Math.cos(ang), dy = Math.sin(ang), t = Math.min((cx - m) / Math.abs(dx || 1e-6), (cy - m) / Math.abs(dy || 1e-6));
    const x = cx + dx * t, y = cy + dy * t;
    g.save(); g.translate(x, y); g.rotate(ang); g.fillStyle = col; g.beginPath(); g.moveTo(14, 0); g.lineTo(-8, -9); g.lineTo(-4, 0); g.lineTo(-8, 9); g.closePath(); g.fill(); g.restore();
    if (label) text(label, x - Math.cos(ang) * 26, y - Math.sin(ang) * 26 + 4, col, 11, 'center');
  }

  function fullHud(o, P, ppr) {
    const { c, d, altFt } = o, pl = c.pl, w = st.w, h = st.h;
    const psi = Math.atan2(pl.fwd[0], pl.fwd[1]), dir = (a, b) => [pl.pos[0] + Math.sin(a) * Math.cos(b) * 3000, pl.pos[1] + Math.cos(a) * Math.cos(b) * 3000, pl.pos[2] + Math.sin(b) * 3000];
    g.strokeStyle = GREEN; g.fillStyle = GREEN; g.lineWidth = 1.6;
    for (let a = -40; a <= 60; a += 10) {
      const el = a * Math.PI / 180, C = P(dir(psi, el));
      if (!C.front || C.x < 80 || C.x > w - 80 || C.y < 60 || C.y > h - 120) continue;
      const half = a === 0 ? 0.16 : 0.05, gap = a === 0 ? 0.03 : 0.018, L0 = P(dir(psi - half, el)), L1 = P(dir(psi - gap, el)), R0 = P(dir(psi + half, el)), R1 = P(dir(psi + gap, el));
      if (a < 0) g.setLineDash([7, 5]);
      g.beginPath(); g.moveTo(L0.x, L0.y); g.lineTo(L1.x, L1.y); g.moveTo(R1.x, R1.y); g.lineTo(R0.x, R0.y); g.stroke(); g.setLineDash([]);
      if (a) text(String(Math.abs(a)), R0.x + 6, R0.y + 4, GREEN, 11);
    }
    const tape = (x, val, step, major, side, fmt) => {
      const H2 = 118, yc = h * 0.45, px = H2 / (step * 6);
      g.save(); g.beginPath(); g.rect(x - 44, yc - H2, 88, H2 * 2); g.clip();
      g.strokeStyle = GREEN; g.lineWidth = 1.5;
      for (let v = Math.floor((val - step * 7) / step) * step; v <= val + step * 7; v += step) {
        if (v < 0) continue;
        const y = yc - (v - val) * px, big = v % major === 0;
        g.beginPath(); g.moveTo(x, y); g.lineTo(x + side * (big ? 14 : 7), y); g.stroke();
        if (big) text(fmt(v), x + side * 18, y + 4, GREEN, 11, side > 0 ? 'left' : 'right');
      }
      g.restore();
      g.fillStyle = 'rgba(0,20,8,.72)'; g.fillRect(x - side * 60 - 30, yc - 12, 60, 24); g.strokeStyle = GREEN; g.strokeRect(x - side * 60 - 30, yc - 12, 60, 24);
      text(fmt(Math.round(val)), x - side * 60, yc + 5, GREEN, 14, 'center');
    };
    const off = Math.min(w * 0.25, 340);
    tape(w / 2 - off, d.ias ?? 0, 10, 50, -1, (v) => String(v));
    tape(w / 2 + off, altFt, 100, 500, 1, (v) => String(v));
    const hd = d.hdg ?? 0, yh = h * 0.13;
    g.save(); g.beginPath(); g.rect(w / 2 - 150, yh - 26, 300, 44); g.clip(); g.strokeStyle = GREEN; g.lineWidth = 1.5;
    for (let a = Math.floor((hd - 40) / 5) * 5; a <= hd + 40; a += 5) {
      const x = w / 2 + (a - hd) * 4.2, big = a % 10 === 0;
      g.beginPath(); g.moveTo(x, yh); g.lineTo(x, yh + (big ? 10 : 5)); g.stroke();
      if (big) text(String(((a % 360) + 360) % 360 / 10 | 0).padStart(2, '0'), x, yh - 4, GREEN, 11, 'center');
    }
    g.restore();
    text(String(Math.round(hd) % 360).padStart(3, '0'), w / 2, yh + 28, GREEN, 14, 'center');
    const xl = w / 2 - off - 30, yb = h * 0.45 + 145;
    text(`M ${(d.mach ?? 0).toFixed(2)}`, xl, yb, GREEN, 13, 'center');
    text(`G ${(d.nz ?? 1).toFixed(1)}`, xl, yb + 18, GREEN, 13, 'center');
    text(`AOA ${((d.alpha ?? 0) * 57.3).toFixed(0)}`, xl, yb + 36, GREEN, 13, 'center');
    const xr = w / 2 + off + 30;
    text(`VS ${Math.round(d.vsi ?? 0)}`, xr, yb, GREEN, 13, 'center');
    text({ gun: 'A/A CAN', aam: 'A/A IR', bomb: c.sol?.ccrp ? 'A/G CCRP' : 'A/G CCIP', rkt: 'A/G FOG' }[c.sel], xr, yb + 18, GREEN, 13, 'center');
  }

  function draw(o) {
    const { c, camera, world, fovy, aspect, ac, flashHit, now } = o;
    g.clearRect(0, 0, st.w, st.h);
    if (!c) return;
    const pl = c.pl, w = st.w, h = st.h, th = Math.tan(fovy / 2);
    const cam = { p: camera.positionWC, d: camera.directionWC, u: camera.upWC, r: camera.rightWC };
    const P = (e) => proj(cam, th, aspect, world(e));
    const pxPerRad = h / 2 / th;
    glow();
    if (pl.alive) {
      const bore = P([pl.pos[0] + pl.fwd[0] * 2000, pl.pos[1] + pl.fwd[1] * 2000, pl.pos[2] + pl.fwd[2] * 2000]);
      const spd = Math.hypot(pl.vel[0], pl.vel[1], pl.vel[2]);
      const fpm = spd > 20 ? P([pl.pos[0] + pl.vel[0] / spd * 2000, pl.pos[1] + pl.vel[1] / spd * 2000, pl.pos[2] + pl.vel[2] / spd * 2000]) : null;
      if (bore.front) {
        g.strokeStyle = GREEN; g.lineWidth = 2; g.beginPath();
        g.moveTo(bore.x - 16, bore.y); g.lineTo(bore.x - 5, bore.y); g.moveTo(bore.x + 5, bore.y); g.lineTo(bore.x + 16, bore.y); g.moveTo(bore.x, bore.y - 16); g.lineTo(bore.x, bore.y - 5); g.moveTo(bore.x, bore.y + 5); g.lineTo(bore.x, bore.y + 16); g.stroke();
        if (c.sel === 'aam') { g.setLineDash([6, 6]); circle(bore.x, bore.y, 0.3 * pxPerRad, GREEN, 1.5); g.setLineDash([]); }
      }
      if (fpm && fpm.front && !o.inCockpit) {
        circle(fpm.x, fpm.y, 7, GREEN, 1.5);
        g.beginPath(); g.moveTo(fpm.x - 17, fpm.y); g.lineTo(fpm.x - 7, fpm.y); g.moveTo(fpm.x + 7, fpm.y); g.lineTo(fpm.x + 17, fpm.y); g.moveTo(fpm.x, fpm.y - 7); g.lineTo(fpm.x, fpm.y - 13); g.stroke();
      }
    }

    const tgt = c.tgt;
    const items = [...c.air.filter((a) => !a.dying), ...c.gnd.filter((a) => a.alive)];
    for (const e of items) {
      const isAir = e.type === 'air', friend = isAir && e.team === 'blue';
      const d = Math.hypot(e.pos[0] - pl.pos[0], e.pos[1] - pl.pos[1], e.pos[2] - pl.pos[2]);
      const sel = e === tgt;
      if (!isAir && d > 12000 && !sel) continue;
      if (!isAir && !sel && d > 6500 && !e.primary) continue;
      const p = P(e.pos), col = friend ? BLUE : sel ? '#ffffff' : RED;
      if (!p.front || p.x < -40 || p.x > w + 40 || p.y < -40 || p.y > h + 40) {
        if (sel || friend && d > 3000) edgeArrow(p, friend ? BLUE : sel ? '#fff' : RED, `${(d / 1000).toFixed(1)}`);
        continue;
      }
      const s = Math.max(isAir ? 17 : 12, Math.min(90, (e.r * 2.6 / d) * pxPerRad));
      g.shadowColor = friend ? BLUE : RED;
      bracket(p.x, p.y, s, col, sel ? 3 : 1.6);
      if (isAir || sel) text(`${e.name} ${d >= 1000 ? (d / 1000).toFixed(1) + ' km' : Math.round(d) + ' m'}`, p.x, p.y + s + 14, col, 11, 'center');
      if (sel && c.sel === 'aam') {
        const lk = c.lock;
        if (lk.ok) { const bl = Math.floor(now / 140) % 2; if (bl) circle(p.x, p.y, s + 10, RED, 2.5); text('TRAVADO', p.x, p.y - s - 8, RED, 14, 'center'); }
        else if (lk.t > 0.05) { g.setLineDash([4, 4]); circle(p.x, p.y, s + 10, '#fff', 1.5); g.setLineDash([]); text('TRAVANDO…', p.x, p.y - s - 8, '#fff', 12, 'center'); }
      }
    }
    glow();

    if (pl.alive && tgt && c.sel === 'gun') {
      const ld = c.gunLead();
      const d = ld.range;
      if (d < 2600 && ld.t < 3) {
        const aim = P([pl.pos[0] + ld.dir[0] * d, pl.pos[1] + ld.dir[1] * d, pl.pos[2] + ld.dir[2] * d]);
        if (aim.front) {
          circle(aim.x, aim.y, 26, GREEN, 2); circle(aim.x, aim.y, 2.2, GREEN, 3);
          const frac = Math.min(1, d / 2400);
          g.strokeStyle = GREEN; g.lineWidth = 4; g.beginPath(); g.arc(aim.x, aim.y, 31, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2 * frac); g.stroke();
          text(`${Math.round(d)} m`, aim.x + 36, aim.y + 4, GREEN, 12);
        }
      }
    }
    const sol = c.sol;
    if (pl.alive && sol) {
      const sp = Math.hypot(pl.vel[0], pl.vel[1], pl.vel[2]) || 1;
      const fp = P([pl.pos[0] + pl.vel[0] / sp * 2000, pl.pos[1] + pl.vel[1] / sp * 2000, pl.pos[2] + pl.vel[2] / sp * 2000]);
      const mode = sol.ccrp ? 'CCRP' : sol.kind === 'bomb' ? 'CCIP' : 'FOG';
      const col = sol.onTarget ? RED : AMBER;
      const ip = sol.p ? P(sol.p) : null;
      if (ip && ip.front && ip.x > 30 && ip.x < w - 30 && ip.y > 30 && ip.y < h - 30) {
        if (sol.kind === 'bomb' && fp.front) { g.setLineDash([9, 7]); g.strokeStyle = col; g.lineWidth = 1.5; g.beginPath(); g.moveTo(fp.x, fp.y); g.lineTo(ip.x, ip.y); g.stroke(); g.setLineDash([]); }
        circle(ip.x, ip.y, sol.kind === 'bomb' ? 13 : 20, col, 2); circle(ip.x, ip.y, 1.8, col, 3);
        if (sol.kind === 'rkt') { g.strokeStyle = col; g.lineWidth = 4; g.beginPath(); g.arc(ip.x, ip.y, 26, -Math.PI / 2, -Math.PI / 2 + Math.PI * 2 * Math.min(1, sol.range / 3200)); g.stroke(); }
        text(`${mode} ${(sol.range / 1000).toFixed(1)} km · ${sol.tof.toFixed(1)} s`, ip.x + 32, ip.y + 4, col, 12);
      } else if (ip) edgeArrow(ip, AMBER, mode);
      if (sol.tgt) {
        const tp = P(sol.tgt.pos), up = P([sol.tgt.pos[0], sol.tgt.pos[1], sol.tgt.pos[2] + 3000]);
        if (sol.ccrp && tp.front && up.front) { g.strokeStyle = GREEN; g.lineWidth = 2; g.beginPath(); g.moveTo(tp.x, tp.y); g.lineTo(up.x, up.y); g.stroke(); }
        const cueY = h * 0.66;
        text(`${sol.tgt.name} · ${(sol.dist / 1000).toFixed(1)} km · erro ${Math.round(sol.miss)} m`, w / 2, cueY + 34, GREEN, 13, 'center');
        if (sol.ccrp) {
          g.strokeStyle = GREEN; g.lineWidth = 1.5; g.strokeRect(w / 2 - 110, cueY - 8, 220, 12);
          g.fillStyle = c.pickle ? RED : GREEN; g.fillRect(w / 2 - 110, cueY - 8, 220 * Math.min(1, Math.max(0, sol.ttr / 20)), 12);
          text(c.pickle ? `ARMADO · SOLTURA EM ${Math.max(0, sol.ttr).toFixed(1)} s` : `CCRP · SOLTURA EM ${Math.max(0, sol.ttr).toFixed(1)} s · SEGURE = AUTO · TOQUE = AGORA`, w / 2, cueY - 14, c.pickle ? RED : GREEN, 13, 'center');
        }
      }
      if (sol.onTarget && !sol.ccrp) text(sol.kind === 'bomb' ? 'LANCE' : 'ATIRE', w / 2, h * 0.66 - 8, RED, 26, 'center');
      else if (sol.kind === 'rkt' && !sol.inRange) text(sol.tooClose ? 'MUITO PERTO' : 'FORA DE ALCANCE', w / 2, h * 0.66 - 8, AMBER, 16, 'center');
    }
    if (pl.alive && !pl.grounded && pl.vel[2] < -5 && pl.agl / -pl.vel[2] < 5) {
      if (Math.floor(now / 130) % 2) text('SUBA!', w / 2, h * 0.36, RED, 34, 'center');
    }
    if (o.d && o.ac.weapons.sight.full && !o.inCockpit) fullHud(o, P, pxPerRad);

    glow(false);
    const wr = c.warn;
    if (wr.msl && pl.alive) {
      const blink = Math.floor(now / 160) % 2;
      if (blink) text(`⚠ MÍSSIL  ${(wr.d / 1000).toFixed(1)} km`, w / 2, h * 0.27, RED, 28, 'center');
      const mp = P(wr.msl.pos);
      if (!mp.front || mp.x < 30 || mp.x > w - 30 || mp.y < 30 || mp.y > h - 30) edgeArrow(mp, RED, 'MÍSSIL');
      else { circle(mp.x, mp.y, 16, RED, 2.5); }
    }

    if (flashHit > 0) { g.fillStyle = `rgba(255,30,20,${Math.min(0.45, flashHit * 0.5)})`; g.fillRect(0, 0, w, h); }
    if (pl.alive && pl.hp < 35) { const a = 0.14 + 0.1 * Math.sin(now / 200); const rg2 = g.createRadialGradient(w / 2, h / 2, h * 0.35, w / 2, h / 2, h * 0.85); rg2.addColorStop(0, 'rgba(255,0,0,0)'); rg2.addColorStop(1, `rgba(255,0,0,${a * 2})`); g.fillStyle = rg2; g.fillRect(0, 0, w, h); }

    if (o.inCockpit) return;
    const px = w - 14, py = h - 150;
    g.fillStyle = 'rgba(8,16,28,.62)'; g.strokeStyle = 'rgba(120,200,255,.35)'; g.lineWidth = 1;
    const pw = 214, ph = 30 + c.avail.length * 21 + 50;
    g.beginPath(); g.roundRect(px - pw, py - ph + 40, pw, ph, 10); g.fill(); g.stroke();
    let yy = py - ph + 40 + 24;
    for (const k of c.avail) {
      const on = k === c.sel, n = k === 'gun' ? c.w.gun : c.w[k];
      text(`${on ? '▶' : ' '} ${WNAME[k]}`, px - pw + 12, yy, on ? '#fff' : '#9fb3d1', 13);
      text(String(n), px - 12, yy, n === 0 ? RED : on ? GREEN : '#cfe3ff', 14, 'right');
      yy += 21;
    }
    text('  CONTRAMEDIDAS', px - pw + 12, yy, '#9fb3d1', 13); text(String(c.w.flares), px - 12, yy, c.w.flares ? '#cfe3ff' : RED, 14, 'right');
    yy += 12;
    const hp = Math.max(0, pl.hp / pl.hpMax);
    g.fillStyle = 'rgba(255,255,255,.14)'; g.fillRect(px - pw + 12, yy, pw - 24, 9);
    g.fillStyle = hp > 0.6 ? GREEN : hp > 0.3 ? AMBER : RED; g.fillRect(px - pw + 12, yy, (pw - 24) * hp, 9);
    text(`CASCO ${Math.round(hp * 100)}%`, px - pw + 12, yy + 24, '#cfe3ff', 11);
    text(`${c.score} pts`, px - 12, yy + 24, AMBER, 12, 'right');
  }

  function radar(o) {
    const { c, now } = o;
    const S = 190, cx = S / 2, R = S / 2 - 6;
    rg.clearRect(0, 0, S, S);
    if (!c) return;
    const pl = c.pl, range = KM[st.rangeIdx] * 1000, k = R / range;
    rg.fillStyle = 'rgba(6,24,14,.68)'; rg.beginPath(); rg.arc(cx, cx, R + 3, 0, Math.PI * 2); rg.fill();
    rg.strokeStyle = 'rgba(88,255,140,.35)'; rg.lineWidth = 1;
    for (const f of [0.33, 0.66, 1]) { rg.beginPath(); rg.arc(cx, cx, R * f, 0, Math.PI * 2); rg.stroke(); }
    rg.beginPath(); rg.moveTo(cx, cx - R); rg.lineTo(cx, cx + R); rg.moveTo(cx - R, cx); rg.lineTo(cx + R, cx); rg.stroke();
    const psi = Math.atan2(pl.fwd[0], pl.fwd[1]), cs = Math.cos(psi), sn = Math.sin(psi);
    const to = (pos) => {
      const dx = pos[0] - pl.pos[0], dy = pos[1] - pl.pos[1];
      let x = dx * cs - dy * sn, y = dx * sn + dy * cs;
      const m = Math.hypot(x, y) * k, edge = m > R;
      if (edge) { x *= R / m / k; y *= R / m / k; }
      return { x: cx + x * k, y: cx - y * k, edge };
    };
    const tri = (p, ang, col, s = 6) => {
      rg.save(); rg.translate(p.x, p.y); rg.rotate(ang); rg.fillStyle = col; rg.beginPath(); rg.moveTo(0, -s); rg.lineTo(s * 0.7, s * 0.8); rg.lineTo(-s * 0.7, s * 0.8); rg.closePath(); rg.fill(); rg.restore();
    };
    for (const gt of c.gnd) {
      if (!gt.alive) continue;
      const p = to(gt.pos); if (p.edge) continue;
      rg.fillStyle = gt.primary ? AMBER : 'rgba(255,193,77,.55)'; const s = gt.primary ? 4 : 2.5; rg.fillRect(p.x - s, p.y - s, s * 2, s * 2);
    }
    for (const e of c.air) {
      if (e.dying) continue;
      const p = to(e.pos), ang = Math.atan2(e.dir[0], e.dir[1]) - psi;
      tri(p, ang, e.team === 'red' ? RED : BLUE, e.team === 'red' ? 6.5 : 5.5);
      if (e === c.tgt) { rg.strokeStyle = '#fff'; rg.lineWidth = 1.5; rg.strokeRect(p.x - 9, p.y - 9, 18, 18); }
    }
    if (c.tgt && c.tgt.type === 'gnd') { const p = to(c.tgt.pos); rg.strokeStyle = '#fff'; rg.lineWidth = 1.5; rg.strokeRect(p.x - 8, p.y - 8, 16, 16); }
    for (const m of c.missiles) {
      if (m.tgt !== pl) continue;
      const p = to(m.pos);
      if (Math.floor(now / 150) % 2) { rg.strokeStyle = RED; rg.lineWidth = 2; rg.beginPath(); rg.moveTo(p.x - 5, p.y - 5); rg.lineTo(p.x + 5, p.y + 5); rg.moveTo(p.x + 5, p.y - 5); rg.lineTo(p.x - 5, p.y + 5); rg.stroke(); }
    }
    rg.fillStyle = '#fff'; rg.beginPath(); rg.moveTo(cx, cx - 7); rg.lineTo(cx + 5, cx + 6); rg.lineTo(cx, cx + 3); rg.lineTo(cx - 5, cx + 6); rg.closePath(); rg.fill();
    rg.fillStyle = 'rgba(160,255,190,.8)'; rg.font = '600 10px system-ui'; rg.textAlign = 'left'; rg.fillText(`${KM[st.rangeIdx]} km`, 8, S - 8);
    rg.textAlign = 'right'; rg.fillText('K', S - 8, S - 8);
  }

  st.draw = draw; st.radar = radar; st.resize = resize;
  st.cycleRange = () => { st.rangeIdx = (st.rangeIdx + 1) % KM.length; };
  st.clear = () => { g.clearRect(0, 0, st.w, st.h); rg.clearRect(0, 0, 190, 190); };
  return st;
}

root.Hud = { create };
})(typeof self !== 'undefined' ? self : globalThis);
