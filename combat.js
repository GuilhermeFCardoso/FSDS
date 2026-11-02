(function (root) {
const G0 = 9.80665;
const clamp = (x, a, b) => (x < a ? a : x > b ? b : x);
const rnd = (a, b) => a + Math.random() * (b - a);
const gauss = () => Math.random() + Math.random() + Math.random() - 1.5;
const add = (a, b) => [a[0] + b[0], a[1] + b[1], a[2] + b[2]];
const sub = (a, b) => [a[0] - b[0], a[1] - b[1], a[2] - b[2]];
const scl = (a, k) => [a[0] * k, a[1] * k, a[2] * k];
const dot = (a, b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
const len = (a) => Math.hypot(a[0], a[1], a[2]);
const norm = (a) => { const l = len(a) || 1; return [a[0] / l, a[1] / l, a[2] / l]; };
const cross = (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];
const dist = (a, b) => Math.hypot(a[0] - b[0], a[1] - b[1], a[2] - b[2]);
const angle = (a, b) => Math.acos(clamp(dot(a, b) / ((len(a) * len(b)) || 1), -1, 1));
const wrapA = (a) => Math.atan2(Math.sin(a), Math.cos(a));

function turnToward(dir, want, max) {
  let w = want, ang = Math.acos(clamp(dot(dir, w), -1, 1));
  if (ang <= max) return norm(w);
  if (ang > Math.PI - 1e-3) { w = norm(cross(dir, Math.abs(dir[2]) > 0.95 ? [1, 0, 0] : [0, 0, 1])); ang = Math.PI / 2; }
  const s = Math.sin(ang);
  return norm(add(scl(dir, Math.sin(ang - max) / s), scl(w, Math.sin(max) / s)));
}

function lead(P, V, T, VT, vb) {
  let t = dist(P, T) / vb, aim = sub(T, P);
  for (let i = 0; i < 5; i++) {
    aim = add(add(sub(T, P), scl(sub(VT, V), t)), [0, 0, 0.5 * G0 * t * t]);
    t = len(aim) / vb;
  }
  return { dir: norm(aim), t, range: dist(P, T) };
}

function segDist(p0, p1, c) {
  const d = sub(p1, p0), dd = dot(d, d), t = dd > 0 ? clamp(dot(sub(c, p0), d) / dd, 0, 1) : 0;
  const q = add(p0, scl(d, t));
  return { d: dist(q, c), q };
}

const MISSIONS = {
  caca: { name: 'Patrulha de combate', desc: 'Três ondas de caças inimigos (2, 3 e 4). Seu ala voa com você.', waves: [2, 3, 4], wingman: true },
  ataque: { name: 'Ataque ao solo', desc: 'Destrua a base inimiga: hangares, depósito de combustível, radar e baterias SAM. Interceptadores reagem depois.', ground: true, waves: [2, 2], wingman: true, delay: 70 },
  arena: { name: 'Arena de sobrevivência', desc: 'Ondas infinitas de caças, cada vez maiores. Quanto você aguenta?', waves: [], endless: true, wingman: true },
};
const DIFF = {
  facil: { skill: 0.5, dmg: 0.45, aaa: 0.5 },
  normal: { skill: 0.8, dmg: 1, aaa: 1 },
  veterano: { skill: 1.1, dmg: 1.4, aaa: 1.5 },
};
const GND = {
  hangar: { name: 'Hangar', hp: 150, r: 20, pts: 150, primary: true, size: 1.0 },
  fuel: { name: 'Depósito de combustível', hp: 90, r: 12, pts: 150, primary: true },
  radar: { name: 'Radar', hp: 60, r: 7, pts: 120, primary: true },
  sam: { name: 'Bateria SAM', hp: 70, r: 7, pts: 120, primary: true },
  aaa: { name: 'Antiaérea', hp: 45, r: 4.5, pts: 60 },
  tank: { name: 'Blindado', hp: 70, r: 5, pts: 40 },
  truck: { name: 'Caminhão', hp: 30, r: 4.5, pts: 25 },
};

function create(env, ac, opts = {}) {
  const diff = DIFF[opts.diff] || DIFF.normal;
  const def = MISSIONS[opts.mission] || MISSIONS.caca;
  const wd = ac.weapons;
  const c = {
    env, ac, def, diff, nid: 0, t: 0, frame: 0, gTop: -Infinity, sol: null, pickle: false, ev: [], msgs: [],
    air: [], gnd: [], bullets: [], missiles: [], bombs: [], rockets: [], flares: [],
    pl: { isPlayer: true, pos: [0, 0, 0], vel: [0, 0, 0], spd: 0, fwd: [0, 1, 0], up: [0, 0, 1], right: [1, 0, 0], hp: 100, hpMax: 100, r: 7, alive: true, agl: 0, grounded: false, name: 'Você' },
    w: null, in: { gun: false, pickle: false }, firing: false, sel: 'gun', tgt: null, lock: { t: 0, ok: false }, warn: { msl: null, d: 1e9 },
    score: 0, kills: { air: 0, gnd: 0 }, shots: 0, hits: 0, taken: 0, wave: 0, waveT: def.delay ?? 18, over: null, rearmT: 0, wingman: null, wingSpawned: false, baseBuilt: false, primLeft: 0, primTotal: 0,
    assist: !!opts.assist, invuln: !!opts.invuln, site: opts.site || null, home: opts.home || [0, 0, 0],
  };
  c.w = { gun: wd.gun.rounds, aam: wd.aam, bomb: wd.bomb, rkt: wd.rkt, flares: wd.flares, gunAcc: 0, n: 0 };
  c.avail = ['gun', ...(wd.aam ? ['aam'] : []), ...(wd.bomb ? ['bomb'] : []), ...(wd.rkt ? ['rkt'] : [])];

  const say = (text, kind = '') => c.msgs.push({ text, kind });
  const ev = (t, o) => c.ev.push({ t, ...o });
  const gAt = (p) => { const h = env.groundAt(p[0], p[1]); if (h > c.gTop) c.gTop = h; return h; };
  const nearGround = (p) => p[2] < c.gTop + 300;
  const pl = c.pl;

  c.player = (pos, vel, fwd, up, right, agl, grounded) => {
    pl.pos = pos; pl.vel = vel; pl.spd = len(vel); pl.fwd = fwd; pl.up = up; pl.right = right; pl.agl = agl; pl.grounded = grounded;
  };

  const foesOf = (team) => {
    const l = [];
    for (const a of c.air) if (a.team !== team && !a.dying) l.push(a);
    if (team === 'red') { if (pl.alive) l.push(pl); } else for (const g of c.gnd) if (g.alive) l.push(g);
    return l;
  };

  function mkJet(team, model, name, pos, dir, spd, skill) {
    const e = {
      id: ++c.nid, type: 'air', team, model, name, pos, dir: norm(dir), spd, vel: scl(norm(dir), spd), hp: 100, hpMax: 100, r: 7,
      gmax: 5 + 2.2 * skill, cruise: team === 'red' ? 205 : 215, vmin: 105, vmax: 330, skill, mode: 'patrol', mt: 0, t: 0, rt: 0, tgt: null, wp: null,
      gunCd: rnd(0, 2), burst: 0, gunAcc: 0, err: [0, 0], mslN: team === 'red' ? 2 : 4, mslCd: rnd(5, 12), flareN: 14, flareCd: 0, roll: 0, brkSide: Math.random() < 0.5 ? -1 : 1,
      dying: 0, gT: 0, gHere: 0, gAhead: 0, burn: 0,
    };
    c.air.push(e);
    return e;
  }

  function pickTarget(e) {
    const foes = foesOf(e.team);
    let best = null, bd = e.team === 'red' ? 26000 : 14000;
    for (const f of foes) {
      if (f.type === 'gnd') continue;
      const d = dist(e.pos, f.pos) * (f.isPlayer ? 0.75 : 1);
      if (d < bd) { bd = d; best = f; }
    }
    return best;
  }

  function incoming(e) {
    let best = null, bd = 6000;
    for (const m of c.missiles) {
      if (m.tgt !== e || m.dead) continue;
      const d = dist(m.pos, e.pos);
      if (d < bd) { bd = d; best = m; }
    }
    return best && { m: best, d: bd };
  }

  function dropFlare(e) {
    if (e.isPlayer ? c.w.flares <= 0 : e.flareN <= 0) return false;
    if (e.isPlayer) c.w.flares--; else e.flareN--;
    const team = e.isPlayer ? 'blue' : e.team, src = e.isPlayer ? pl : e;
    const dir = e.isPlayer ? pl.fwd : e.dir;
    for (let i = 0; i < 2; i++) {
      c.flares.push({ id: ++c.nid, pos: sub(add(src.pos, scl(src.vel, 0)), scl(dir, 8)), vel: add(src.vel, add(scl(cross(dir, [0, 0, 1]), (i ? 1 : -1) * rnd(8, 18)), [0, 0, rnd(-12, -4)])), life: 5.5, age: 0, team });
    }
    return true;
  }

  function hurt(t, dmg, by) {
    if (t.isPlayer) {
      if (!pl.alive) return;
      const d = dmg * diff.dmg;
      c.taken += d;
      if (!c.invuln) pl.hp -= d;
      ev('hurt', { dmg: d });
      if (pl.hp <= 0 && pl.alive) { pl.alive = false; pl.hp = 0; ev('boom', { pos: [...pl.pos], size: 3 }); say('Sua aeronave foi destruída' + (by ? ' por ' + by : ''), 'bad'); lose(); }
      return;
    }
    if (t.type === 'gnd') {
      if (!t.alive) return;
      t.hp -= dmg;
      if (t.hp <= 0) killGnd(t);
      return;
    }
    if (t.dying) return;
    t.hp -= dmg;
    if (t.hp <= 0) killAir(t, by);
  }

  function killAir(e, by) {
    e.dying = 0.001; e.mode = 'dying';
    ev('boom', { pos: [...e.pos], size: 1.4 });
    if (e.team === 'red') {
      if (by === 'player') { c.kills.air++; c.score += 100; say(`${e.name} abatido  +100`, 'kill'); }
      else say(`${e.name} abatido`, 'kill');
    } else say(`${e.name} (seu ala) foi abatido`, 'bad');
  }

  function killGnd(g) {
    g.alive = false; g.hp = 0;
    const big = g.kind === 'fuel' || g.kind === 'hangar';
    ev('boom', { pos: [...g.pos], size: big ? 3 : 1.6, gnd: true });
    ev('wreck', { id: g.id });
    c.kills.gnd++; c.score += g.pts;
    say(`${g.name} destruído  +${g.pts}`, 'kill');
    if (g.primary) c.primLeft--;
    if (g.kind === 'fuel') blast(g.pos, 34, 160, null);
    checkWin();
  }

  function blast(p, rad, dmg, team, by) {
    for (const t of [...c.air, ...c.gnd, pl]) {
      if (t === pl ? (!pl.alive) : t.type === 'gnd' ? !t.alive : t.dying) continue;
      if (team && (t === pl ? team === 'blue' : t.type === 'gnd' ? team === 'red' : t.team === team)) continue;
      const d = dist(p, t.pos);
      if (d < rad + t.r) hurt(t, dmg * (1 - clamp(d / (rad + t.r), 0, 1)), by);
    }
  }

  function lose() { if (!c.over) c.over = 'lose'; }
  function checkWin() {
    if (c.over) return;
    const redAir = c.air.some((a) => a.team === 'red' && !a.dying);
    if (def.ground && c.primLeft <= 0 && c.baseBuilt) { c.waveT = 1e9; if (!redAir) win(); }
    else if (!def.endless && !def.ground && c.wave >= def.waves.length && !redAir) win();
  }
  function win() { c.over = 'win'; say('MISSÃO CUMPRIDA', 'good'); }

  function shoot(e, dir, spd, dmg, spread, tracer, team) {
    const d = norm(add(dir, add(scl(cross(dir, [0, 0, 1]), gauss() * spread), [0, 0, gauss() * spread])));
    c.bullets.push({ p: [...e.pos], v: add(e.vel, scl(d, spd)), life: 2.4, team, dmg, tr: tracer, by: e.isPlayer ? 'player' : e.name });
  }

  function fireMissile(src, tgt, kind, team) {
    const dir = src.isPlayer ? pl.fwd : src.dir || [0, 0, 1];
    const side = src.isPlayer ? scl(pl.right, c.w.aam % 2 ? 3 : -3) : [0, 0, 0];
    const up = src.isPlayer ? scl(pl.up, -0.8) : [0, 0, 0];
    const m = {
      id: ++c.nid, kind, team, pos: add(add(src.pos, side), up), dir: [...dir], spd: kind === 'sam' ? 90 : Math.max(src.spd, 100) + 40, vel: [0, 0, 0],
      tgt, life: kind === 'sam' ? 24 : 16, age: 0, maxg: kind === 'sam' ? 22 : 30, fov: kind === 'sam' ? 1.2 : 0.8, dead: false, lost: false, src: src.isPlayer ? 'player' : src.name,
    };
    m.vel = scl(m.dir, m.spd);
    c.missiles.push(m);
    ev('launch', { pos: [...m.pos], kind });
    return m;
  }

  function aiJet(e, dt) {
    e.t += dt;
    if ((e.rt -= dt) <= 0) { e.rt = rnd(1, 2); e.tgt = pickTarget(e); }
    let tg = e.tgt;
    if (tg && (tg.isPlayer ? !pl.alive : tg.dying || tg.hp <= 0)) tg = e.tgt = null;
    const p = e.pos;
    let want = e.dir, spdT = e.cruise;
    const inc = incoming(e);
    if (inc && inc.d < 3800 && e.skill > 0.35) {
      const los = norm(sub(inc.m.pos, p));
      want = norm(add(scl(cross(los, [0, 0, 1]), e.brkSide), [0, 0, -0.35]));
      spdT = e.vmax;
      if (inc.d < 2400 && (e.flareCd -= dt) <= 0 && dropFlare(e)) e.flareCd = 0.6;
      e.mode = 'notch';
    } else if (tg) {
      if (e.mode === 'notch' || e.mode === 'patrol') e.mode = 'attack';
      const rel = sub(tg.pos, p), d = len(rel) || 1, los = scl(rel, 1 / d);
      const tgDir = tg.isPlayer ? tg.fwd : tg.dir, tgSpd = len(tg.vel);
      const threatened = dot(tgDir, scl(los, -1)) > 0.82 && d < 1800;
      if (e.mode === 'break' || e.mode === 'extend') { if ((e.mt -= dt) <= 0) e.mode = 'attack'; }
      else if (threatened && Math.random() < dt * 0.7 * e.skill) { e.mode = 'break'; e.mt = rnd(2, 4); e.brkSide = Math.random() < 0.5 ? -1 : 1; }
      else if (d < 300 && dot(e.dir, los) < 0.4) { e.mode = 'extend'; e.mt = rnd(3, 5); }
      if (e.mode === 'break') {
        want = norm(add(scl(cross(los, [0, 0, 1]), e.brkSide), [0, 0, -0.3]));
        spdT = e.vmax * 0.85;
      } else if (e.mode === 'extend') {
        want = norm([e.dir[0], e.dir[1], 0.22]);
        spdT = e.vmax;
      } else {
        if (d < 2200) want = lead(p, e.vel, tg.pos, tg.vel, 880).dir;
        else want = norm(sub(add(tg.pos, scl(tg.vel, clamp(d / (e.spd + 650), 0, 4))), p));
        spdT = d > 3500 ? e.cruise * 1.25 : clamp(tgSpd * 1.05 + (d - 700) * 0.035, e.vmin * 1.25, e.vmax);
        const err = angle(e.dir, want);
        if (e.burst > 0) {
          e.burst -= dt; e.gunAcc += 38 * dt;
          while (e.gunAcc >= 1) { e.gunAcc--; shoot(e, e.dir, 880, 4, 0.006 + (1.1 - e.skill) * 0.012 + Math.hypot(e.err[0], e.err[1]), Math.random() < 0.3, e.team); }
        } else if ((e.gunCd -= dt) <= 0 && d < 1300 && err < 0.075) {
          e.burst = rnd(0.5, 1.1); e.gunCd = rnd(1.8, 3.6) / e.skill; e.err = [gauss() * 0.01, gauss() * 0.01];
          if (e.team === 'red' && tg.isPlayer) ev('gunwarn', {});
        }
        if (e.mslN > 0 && (e.mslCd -= dt) <= 0 && d > 1500 && d < 8500 && angle(e.dir, los) < 0.3 && Math.random() < dt * 0.6 * e.skill) {
          e.mslN--; e.mslCd = 24 / e.skill;
          fireMissile(e, tg, 'aam', e.team);
          if (tg.isPlayer) say('MÍSSIL LANÇADO CONTRA VOCÊ!', 'bad');
        }
      }
    } else if (e.team === 'blue') {
      const off = add(add(pl.pos, scl(pl.right, -95)), add(scl(pl.fwd, -55), [0, 0, 8]));
      const rel = sub(off, p), d = len(rel);
      want = d > 20 ? norm(rel) : e.dir;
      spdT = clamp(pl.spd + (d - 40) * 0.25, e.vmin, e.vmax);
      if (d > 30000) { e.pos = add(off, [0, 0, 200]); }
    } else {
      if (!e.wp || Math.hypot(e.wp[0] - p[0], e.wp[1] - p[1]) < 1800) e.wp = [pl.pos[0] + rnd(-9000, 9000), pl.pos[1] + rnd(-9000, 9000), pl.pos[2] + rnd(-300, 400)];
      want = norm(sub(e.wp, p));
    }
    if ((e.gT -= dt) <= 0) { e.gT = 0.4; e.gHere = gAt(p); e.gAhead = gAt(add(p, scl(e.dir, e.spd * 4))); }
    if (p[2] < e.gHere + 160 || p[2] + e.dir[2] * e.spd * 4 < e.gAhead + 230) { want = norm([want[0] * 0.25, want[1] * 0.25, 1]); spdT = Math.max(spdT, e.cruise); }
    const maxT = Math.min(e.gmax * G0 / Math.max(e.spd, 100), 0.7) * (e.spd < e.vmin * 1.1 ? 0.6 : 1) * dt;
    const psi0 = Math.atan2(e.dir[0], e.dir[1]);
    const nd = turnToward(e.dir, want, maxT);
    const turned = angle(e.dir, nd);
    const dpsi = wrapA(Math.atan2(nd[0], nd[1]) - psi0);
    e.dir = nd;
    e.roll += (clamp(Math.atan2(e.spd * dpsi / Math.max(dt, 1e-3), G0), -1.35, 1.35) - e.roll) * Math.min(1, dt * 3.5);
    e.spd = clamp(e.spd + (clamp((spdT - e.spd) * 0.6, -5, 7) - G0 * e.dir[2] * 0.6 - (turned / Math.max(dt, 1e-3)) * 6) * dt, 70, e.vmax);
    if (e.spd < e.vmin) e.dir = turnToward(e.dir, [e.dir[0] * 0.3, e.dir[1] * 0.3, -1], 0.4 * dt);
    e.pos = add(p, scl(e.dir, e.spd * dt));
    e.vel = scl(e.dir, e.spd);
  }

  function stepDying(e, dt) {
    e.dying += dt;
    e.dir = turnToward(e.dir, [e.dir[0] * 0.4, e.dir[1] * 0.4, -1], 0.32 * dt);
    e.roll += 2.6 * dt;
    e.spd = clamp(e.spd + 18 * dt, 60, 260);
    e.pos = add(e.pos, scl(e.dir, e.spd * dt));
    e.vel = scl(e.dir, e.spd);
    if (e.pos[2] < gAt(e.pos) + 4 || e.dying > 14) { e.gone = true; ev('boom', { pos: [e.pos[0], e.pos[1], Math.max(gAt(e.pos), e.pos[2] - 3)], size: 3, gnd: true }); }
  }

  function stepGnd(g, dt) {
    if (!g.alive) return;
    if (g.move) { g.pos = add(g.pos, [g.move[0] * dt, g.move[1] * dt, 0]); g.gT -= dt; if (g.gT <= 0) { g.gT = 0.7; g.pos[2] = gAt(g.pos); } }
    if (!pl.alive) return;
    const rel = sub(add(pl.pos, [0, 0, 0]), g.pos), d = len(rel);
    if (g.kind === 'aaa') {
      if (g.burst > 0) {
        g.burst -= dt; g.acc += 22 * dt;
        const tof = d / 880, aim = add(add(pl.pos, scl(pl.vel, tof)), g.bias);
        aim[2] += 0.5 * G0 * tof * tof;
        const dir = norm(sub(aim, add(g.pos, [0, 0, 3])));
        while (g.acc >= 1) { g.acc--; c.bullets.push({ p: add(g.pos, [0, 0, 3]), v: scl(norm(add(dir, [gauss() * 0.004, gauss() * 0.004, gauss() * 0.004])), 880), life: 3.4, team: 'red', dmg: 3.2, tr: Math.random() < 0.5, by: g.name }); }
        if (g.burst <= 0) g.cd = rnd(2.5, 4.5) / diff.aaa;
      } else if ((g.cd -= dt) <= 0 && d < 3600 && pl.pos[2] - g.pos[2] < 3800) {
        g.burst = rnd(1.2, 2.2);
        const sg = (0.5 + d / 700) / diff.aaa;
        g.bias = [gauss() * sg * 2, gauss() * sg * 2, gauss() * sg * 2];
      }
    } else if (g.kind === 'sam') {
      if ((g.cd -= dt) <= 0 && g.ammo > 0 && d < 11000 && d > 1200 && pl.pos[2] - g.pos[2] > 120 && !pl.grounded && c.t > 10) {
        g.ammo--; g.cd = 17 / diff.aaa;
        fireMissile({ pos: add(g.pos, [0, 0, 4]), vel: [0, 0, 40], dir: [0, 0, 1], spd: 40, name: g.name }, pl, 'sam', 'red');
        say('MÍSSIL SAM LANÇADO!', 'bad');
      }
    }
  }

  function stepMissile(m, dt) {
    m.age += dt; m.life -= dt;
    const acc = m.age < (m.kind === 'sam' ? 3.5 : 3) ? 280 : 0;
    m.spd = clamp(m.spd + acc * dt - (acc ? 0 : m.spd * 0.07 * dt) - G0 * m.dir[2] * dt * 0.3, 0, 960);
    let t = m.tgt;
    if (t && (t.isFlare ? t.ref.dead : t.isPlayer ? !pl.alive : t.type === 'gnd' ? !t.alive : t.dying)) t = m.tgt = null;
    for (const f of c.flares) {
      if (f.team === m.team || f.age < 0.25 || t && t.isFlare || m.lost) continue;
      if (dist(f.pos, m.pos) < 3500 && angle(m.dir, sub(f.pos, m.pos)) < 0.32 && Math.random() < dt * 1.8) { t = m.tgt = { isFlare: true, ref: f, get pos() { return f.pos; }, get vel() { return f.vel; }, r: 2 }; break; }
    }
    if (t && t.isFlare && t.ref.life <= 0) t = m.tgt = null;
    if (t && !m.lost) {
      const rel = sub(t.pos, m.pos), d = len(rel);
      if (angle(m.dir, rel) > m.fov && d > 100) { m.lost = true; if (t.isPlayer) say('Míssil perdeu a trava!', 'good'); }
      else {
        const tl = clamp(d / Math.max(m.spd, 200), 0, 3);
        const aim = add(t.pos, scl(t.vel, tl * 0.8));
        m.dir = turnToward(m.dir, norm(sub(aim, m.pos)), (m.maxg * G0 / Math.max(m.spd, 200)) * dt);
      }
    }
    const p1 = add(m.pos, scl(m.dir, m.spd * dt));
    m.vel = scl(m.dir, m.spd);
    if (t) {
      const sd = segDist(m.pos, p1, t.pos), rt = t.r || 0;
      const direct = sd.d < rt + 3, passing = m.fuse && sd.d >= m.fuse.d;
      if (sd.d < 20 + rt * 0.5 && !passing) m.fuse = sd;
      if (direct || passing) {
        const hit = direct ? sd : m.fuse;
        m.dead = true;
        ev('boom', { pos: hit.q, size: 1 });
        if (!t.isFlare) {
          const dmgBase = m.kind === 'sam' ? 60 : t.isPlayer ? 75 : 150;
          hurt(t, dmgBase * (1 - clamp((hit.d - 4) / 22, 0, 1)), m.src);
          if (t.isPlayer) say('Atingido por míssil!', 'bad');
        }
        return;
      }
    }
    m.pos = p1;
    if (m.age > 0.4 && nearGround(m.pos) && m.pos[2] < gAt(m.pos)) { m.dead = true; ev('boom', { pos: [m.pos[0], m.pos[1], gAt(m.pos)], size: 1, gnd: true }); }
    else if (m.life <= 0 || (m.age > 4 && m.spd < 230)) { m.dead = true; ev('boom', { pos: [...m.pos], size: 0.6 }); }
  }

  function stepBullets(dt) {
    const targets = { blue: foesOf('blue'), red: foesOf('red') };
    for (const b of c.bullets) {
      const p0 = b.p;
      b.v[2] -= G0 * dt;
      const p1 = add(p0, scl(b.v, dt));
      b.life -= dt;
      for (const t of targets[b.team]) {
        if (t.dying || t.alive === false) continue;
        const s = segDist(p0, p1, t.pos);
        if (s.d < t.r * (b.assist ? 1.9 : 1) + 1) {
          b.life = 0;
          ev('spark', { pos: s.q, gnd: t.type === 'gnd' });
          if (b.by === 'player') c.hits++;
          hurt(t, t.type === 'air' ? b.dmg * 1.6 : b.dmg, b.by);
          if (t.isPlayer) ev('hit', {});
          break;
        }
      }
      b.p = p1;
      if (b.life > 0 && nearGround(p1) && (c.frame + (b.i ?? (b.i = c.nid++))) % 3 === 0 && p1[2] < gAt(p1)) { b.life = 0; ev('dust', { pos: [p1[0], p1[1], gAt(p1)] }); }
    }
    c.bullets = c.bullets.filter((b) => b.life > 0);
  }

  function stepRockets(dt) {
    for (const r of c.rockets) {
      r.age += dt; r.spd = Math.min(r.spd + 330 * dt, 520); r.life -= dt;
      r.v = add(scl(r.dir, r.spd), r.inh);
      r.v[2] -= G0 * r.age * 0.3;
      const p1 = add(r.pos, scl(r.v, dt));
      let hit = false;
      for (const t of foesOf('blue')) {
        if (t.dying || t.alive === false) continue;
        if (segDist(r.pos, p1, t.pos).d < t.r + 3) { hit = true; break; }
      }
      const gz = nearGround(p1) ? gAt(p1) : -Infinity;
      if (p1[2] < gz) { hit = true; p1[2] = gz; }
      r.pos = p1;
      if (hit || r.life <= 0) {
        r.dead = true;
        ev('boom', { pos: [...p1], size: 0.9, gnd: true });
        blast(p1, 11, 75, 'blue', 'player');
      }
    }
    c.rockets = c.rockets.filter((r) => !r.dead);
  }

  function stepBombs(dt) {
    for (const b of c.bombs) {
      b.vel[2] -= G0 * dt;
      b.pos = add(b.pos, scl(b.vel, dt));
      const gz = nearGround(b.pos) ? gAt(b.pos) : -Infinity;
      if (b.pos[2] <= gz) {
        b.dead = true;
        const p = [b.pos[0], b.pos[1], gz];
        ev('boom', { pos: p, size: 2.4, gnd: true });
        blast(p, 40, 280, 'blue', 'player');
      }
    }
    c.bombs = c.bombs.filter((b) => !b.dead);
  }

  const groundSel = () => c.sel === 'bomb' || c.sel === 'rkt';
  function updateLockAndTarget(dt) {
    const valid = (t) => t && (t.type === 'gnd' ? t.alive : !t.dying && t.hp > 0);
    if (!valid(c.tgt) || dist(c.tgt.pos, pl.pos) > 25000 || (c.sel === 'aam' && c.tgt.type === 'gnd') || (groundSel() && c.tgt.type === 'air')) c.tgt = null;
    if (!c.tgt) {
      let best = null, ba = 0.7;
      if (!groundSel()) for (const a of c.air) {
        if (a.team !== 'red' || a.dying) continue;
        const d = dist(a.pos, pl.pos);
        if (d > 22000) continue;
        const ang = angle(pl.fwd, sub(a.pos, pl.pos));
        if (ang < ba) { ba = ang; best = a; }
      }
      if (!best && c.sel !== 'aam') for (const g of c.gnd) {
        if (!g.alive || dist(g.pos, pl.pos) > (groundSel() ? wd.sight.range : 6000)) continue;
        const ang = angle(pl.fwd, sub(g.pos, pl.pos));
        if (ang < ba) { ba = ang; best = g; }
      }
      c.tgt = best;
    }
    const L = c.lock;
    if (c.sel === 'aam' && c.tgt && c.tgt.type === 'air' && c.w.aam > 0) {
      const rel = sub(c.tgt.pos, pl.pos), d = len(rel);
      if (d < 12000 && angle(pl.fwd, rel) < 0.3) { L.t += dt; L.ok = L.t > 1.1; }
      else { L.t = Math.max(0, L.t - dt * 2); L.ok = L.t > 1.1; if (L.t < 0.3) L.ok = false; }
    } else { L.t = 0; L.ok = false; }
  }

  function updateWarn() {
    let md = 1e9, mm = null;
    for (const m of c.missiles) if (m.tgt === pl && !m.dead) { const d = dist(m.pos, pl.pos); if (d < md) { md = d; mm = m; } }
    c.warn = { msl: mm, d: md };
  }

  const march = (kind, gz) => {
    let p = [...pl.pos], t = 0;
    const dt = 0.1, lim = kind === 'bomb' ? 40 : 6;
    while (t < lim) {
      const age = t + dt;
      const v = kind === 'bomb' ? [pl.vel[0], pl.vel[1], pl.vel[2] - G0 * t] : add(scl(pl.fwd, Math.min(40 + 330 * age, 520)), pl.vel);
      if (kind !== 'bomb') v[2] -= G0 * age * 0.3;
      const q = add(p, scl(v, dt));
      if (q[2] <= gz) {
        const k = (p[2] - gz) / ((p[2] - q[2]) || 1), r = [p[0] + (q[0] - p[0]) * k, p[1] + (q[1] - p[1]) * k, gz];
        r.t = t + dt * k;
        return r;
      }
      p = q; t += dt;
    }
    return null;
  };
  c.predict = (kind) => {
    let gz = gAt(pl.pos), q = null, t = 0;
    for (let i = 0; i < 3; i++) {
      q = march(kind, gz);
      if (!q) return null;
      t = q.t;
      gz = gAt(q);
    }
    q[2] = gz; q.t = t;
    return q;
  };

  function dropBomb() {
    c.w.bomb--;
    c.bombs.push({ id: ++c.nid, pos: add(pl.pos, scl(pl.up, -2)), vel: [...pl.vel], dead: false });
    ev('drop', {});
  }

  const RKT_MIN = 400, RKT_MAX = 3200;
  function updateSolution(dt) {
    const kind = c.sel, sight = wd.sight || {};
    if ((kind !== 'bomb' && kind !== 'rkt') || !pl.alive || pl.grounded) { c.sol = null; c.pickle = false; return; }
    const p = c.predict(kind), gs = Math.hypot(pl.vel[0], pl.vel[1]) || 1;
    const tr = [pl.vel[0] / gs, pl.vel[1] / gs, 0], rt = [tr[1], -tr[0], 0];
    const range = p ? Math.hypot(p[0] - pl.pos[0], p[1] - pl.pos[1]) : Infinity;
    const sol = { kind, p, tof: p ? p.t : 0, range, inRange: kind === 'bomb' ? !!p : range >= RKT_MIN && range <= RKT_MAX, tooClose: kind === 'rkt' && range < RKT_MIN };
    const t = c.tgt && c.tgt.type === 'gnd' ? c.tgt : null;
    if (t && p) {
      const aim = t.move ? add(t.pos, [t.move[0] * sol.tof, t.move[1] * sol.tof, 0]) : t.pos;
      const e = sub(aim, p), to = sub(aim, pl.pos);
      sol.tgt = t; sol.along = dot(e, tr); sol.cross = dot(e, rt); sol.miss = Math.hypot(sol.along, sol.cross);
      sol.dist = Math.hypot(to[0], to[1]); sol.ttr = sol.along / gs; sol.steer = Math.atan2(dot(to, rt), dot(to, tr));
      sol.rad = (kind === 'bomb' ? 22 : 7) + t.r;
      sol.onTarget = sol.miss < sol.rad && sol.inRange;
      sol.ccrp = kind === 'bomb' && !!sight.ccrp && sol.along > 0 && sol.ttr < 90;
    }
    c.sol = sol;
    if (c.pickle) {
      c.pickleT += dt;
      if (!c.in.pickle) { c.pickle = false; if (kind === 'bomb' && c.w.bomb > 0 && c.pickleT < 0.3) dropBomb(); }
      else if (!sol.tgt || kind !== 'bomb' || c.w.bomb <= 0) c.pickle = false;
      else if (sol.along <= 2) {
        c.pickle = false;
        if (Math.abs(sol.cross) < sol.rad) dropBomb();
        else { say('Fora do eixo: bomba retida', 'warn'); ev('nolock', {}); }
      }
    }
  }
  c.gunLead = () => {
    const t = c.tgt;
    if (!t) return null;
    return lead(pl.pos, pl.vel, t.pos, t.vel || (t.move ? [t.move[0], t.move[1], 0] : [0, 0, 0]), ac.weapons.gun.v);
  };

  c.cycleWeapon = (d = 1) => { const i = c.avail.indexOf(c.sel); c.sel = c.avail[(i + d + c.avail.length) % c.avail.length]; c.lock.t = 0; c.lock.ok = false; ev('select', {}); };
  c.cycleTarget = () => {
    const list = [...(groundSel() ? [] : c.air.filter((a) => a.team === 'red' && !a.dying)), ...c.gnd.filter((g) => g.alive)].filter((t) => dist(t.pos, pl.pos) < (groundSel() ? wd.sight.range : 16000));
    if (!list.length) { c.tgt = null; return; }
    list.sort((a, b) => dist(a.pos, pl.pos) - dist(b.pos, pl.pos));
    const i = list.indexOf(c.tgt);
    c.tgt = list[(i + 1) % list.length];
    c.lock.t = 0; c.lock.ok = false;
    ev('select', {});
  };
  c.flare = () => { if (pl.alive && dropFlare(pl)) ev('flare', {}); else if (!c.w.flares) say('SEM CONTRAMEDIDAS', 'bad'); };
  c.fire = () => {
    if (!pl.alive || pl.grounded) return;
    const w = c.w;
    if (c.sel === 'aam') {
      if (w.aam <= 0) return;
      if (!c.lock.ok || !c.tgt) { say('SEM TRAVA', 'warn'); ev('nolock', {}); return; }
      w.aam--;
      fireMissile(pl, c.tgt, 'aam', 'blue');
      c.lock.t = 0; c.lock.ok = false;
    } else if (c.sel === 'bomb') {
      if (w.bomb <= 0) return;
      if (c.sol && c.sol.ccrp && c.sol.along > 30) { c.pickle = true; c.pickleT = 0; ev('select', {}); return; }
      dropBomb();
    } else if (c.sel === 'rkt') {
      if (w.rkt <= 0) return;
      for (const sd of [-1, 1]) {
        if (w.rkt <= 0) break;
        w.rkt--;
        c.rockets.push({ id: ++c.nid, pos: add(pl.pos, scl(pl.right, sd * 3.2)), dir: [...pl.fwd], inh: [...pl.vel], spd: 40, age: 0, life: 6, v: [...pl.vel], dead: false });
      }
      ev('launch', { pos: [...pl.pos], kind: 'rkt' });
    }
  };

  function playerGun(dt) {
    const g = ac.weapons.gun, w = c.w;
    c.firing = !!(c.in.gun && w.gun > 0 && pl.alive && !pl.grounded);
    if (!c.firing) { w.gunAcc = 0; return; }
    w.gunAcc += g.rate * dt;
    while (w.gunAcc >= 1 && w.gun > 0) {
      w.gunAcc -= 1; w.gun--; w.n++; c.shots++;
      const dir = norm(add(pl.fwd, add(scl(pl.right, gauss() * g.spread * 1.8), scl(pl.up, gauss() * g.spread * 1.8))));
      c.bullets.push({ p: add(pl.pos, scl(pl.fwd, 7)), v: add(pl.vel, scl(dir, g.v)), life: 2.3, team: 'blue', dmg: g.dmg, tr: w.n % 4 === 0, assist: c.assist, by: 'player' });
    }
  }

  function buildBase(site) {
    const mk = (kind, dx, dy, yaw = 0, extra = {}) => {
      const d = GND[kind], p = [site[0] + dx, site[1] + dy, 0];
      p[2] = gAt(p);
      const g = { id: ++c.nid, type: 'gnd', kind, name: d.name, pos: p, yaw, hp: d.hp, hpMax: d.hp, r: d.r, pts: d.pts, primary: !!d.primary, alive: true, gT: 0, cd: rnd(kind === 'sam' ? 6 : 1, kind === 'sam' ? 14 : 3), burst: 0, acc: 0, bias: [0, 0, 0], ammo: 3, ...extra };
      c.gnd.push(g);
      if (g.primary) { c.primLeft++; c.primTotal++; }
      return g;
    };
    mk('hangar', -60, 30, 0); mk('hangar', 10, 40, 0); mk('fuel', 80, -20); mk('radar', -20, -70);
    mk('sam', -150, -40); mk('sam', 170, 60);
    mk('aaa', -100, 110); mk('aaa', 120, 130); mk('aaa', 0, -140); mk('aaa', 210, -60);
    for (let i = 0; i < 4; i++) mk('tank', -90 + i * 28, -10 + (i % 2) * 12, 90);
    for (let i = 0; i < 3; i++) mk('truck', -300 + i * 14, -230, 90, { move: [7, 0] });
    c.baseBuilt = true;
    say(`Base inimiga marcada no mapa: ${c.primTotal} alvos principais`, 'good');
  }

  function spawnWave() {
    const n = def.endless ? Math.min(2 + Math.floor(c.wave * 0.8), 8) : def.waves[c.wave];
    c.wave++;
    const psi = Math.atan2(pl.fwd[0], pl.fwd[1]);
    const brg = psi + rnd(-1.2, 1.2), D = rnd(15000, 21000);
    const base = [pl.pos[0] + Math.sin(brg) * D, pl.pos[1] + Math.cos(brg) * D, 0];
    const models = ['f16', 'gripen'];
    for (let i = 0; i < n; i++) {
      const p = [base[0] + rnd(-700, 700), base[1] + rnd(-700, 700), 0];
      p[2] = Math.max(pl.pos[2] + rnd(-300, 500), gAt(p) + 900);
      const dir = norm([pl.pos[0] - p[0], pl.pos[1] - p[1], rnd(-0.05, 0.05)]);
      mkJet('red', models[i % 2], `Bandido ${c.air.filter((a) => a.team === 'red').length + 1}`, p, dir, 215, clamp(diff.skill + rnd(-0.12, 0.12), 0.35, 1.2));
    }
    const bearing = Math.round(((Math.atan2(base[0] - pl.pos[0], base[1] - pl.pos[1]) * 180 / Math.PI) + 360) % 360);
    say(def.endless ? `Onda ${c.wave}: ${n} caças inimigos — rumo ${bearing}°` : `Onda ${c.wave}/${def.waves.length}: ${n} caças inimigos — rumo ${bearing}°`, 'warn');
    ev('radio', {});
  }

  function spawnWingman() {
    c.wingSpawned = true;
    const p = add(add(pl.pos, scl(pl.right, -90)), add(scl(pl.fwd, -60), [0, 0, 8]));
    c.wingman = mkJet('blue', 'f16', 'Ala', p, [pl.vel[0] || pl.fwd[0], pl.vel[1] || pl.fwd[1], 0], Math.max(pl.spd, 150), 0.85);
    say('Ala ao seu lado — ele ataca os bandidos próximos', 'good');
  }

  function rearm() {
    c.w.gun = wd.gun.rounds; c.w.aam = wd.aam; c.w.bomb = wd.bomb; c.w.rkt = wd.rkt; c.w.flares = wd.flares;
    pl.hp = pl.hpMax;
    ev('rearm', {});
    say('Rearmado, reparado e reabastecido ✓', 'good');
  }

  c.step = (dt) => {
    if (dt <= 0) return;
    const n = Math.max(1, Math.ceil(dt / 0.034)), h = dt / n;
    for (let i = 0; i < n; i++) sub1(h);
  };

  function sub1(dt) {
    c.t += dt; c.frame++;
    if (c.frame % 20 === 1) gAt(pl.pos);
    if (def.ground && !c.baseBuilt && c.site && c.t > 0.2) buildBase(c.site);
    playerGun(dt);
    updateLockAndTarget(dt);
    updateSolution(dt);
    for (const e of c.air) { if (e.dying) stepDying(e, dt); else aiJet(e, dt); }
    for (const g of c.gnd) stepGnd(g, dt);
    for (const m of c.missiles) stepMissile(m, dt);
    c.missiles = c.missiles.filter((m) => !m.dead);
    for (const f of c.flares) { f.age += dt; f.life -= dt; f.vel[2] -= 3 * dt; f.pos = add(f.pos, scl(f.vel, dt)); f.vel = scl(f.vel, 1 - 0.5 * dt); if (f.life <= 0) f.dead = true; }
    c.flares = c.flares.filter((f) => !f.dead);
    stepBullets(dt); stepRockets(dt); stepBombs(dt);
    c.air = c.air.filter((e) => { if (e.gone) { ev('gone', { id: e.id }); if (e === c.wingman) c.wingman = null; return false; } return true; });
    updateWarn();
    if (!c.over || c.over === 'win') {
      if (def.wingman && opts.wingman !== false && !c.wingSpawned && pl.alive && pl.agl > 150 && !pl.grounded) spawnWingman();
      if (c.t > 1 && !pl.grounded && pl.agl > 100 && (def.endless || c.wave < def.waves.length) && (c.waveT -= dt) <= 0) {
        const alive = c.air.filter((a) => a.team === 'red' && !a.dying).length;
        if (!alive || c.wave === 0) { spawnWave(); c.waveT = 8; }
      }
      if (c.t > 2 && !c.over) checkWin();
    }
    if (pl.alive && pl.grounded && pl.spd < 3 && Math.hypot(pl.pos[0] - c.home[0], pl.pos[1] - c.home[1]) < 1800) {
      if ((c.rearmT += dt) > 4 && (c.w.gun < wd.gun.rounds || c.w.aam < wd.aam || c.w.bomb < wd.bomb || c.w.rkt < wd.rkt || c.w.flares < wd.flares || pl.hp < pl.hpMax)) { rearm(); c.rearmT = 0; }
    } else c.rearmT = 0;
  }

  c.status = () => {
    const l = [];
    if (def.endless) l.push(`Arena · onda ${c.wave}`);
    else if (def.ground) l.push(`Alvos principais: ${c.primTotal - c.primLeft}/${c.primTotal}`);
    else l.push(`Onda ${Math.min(c.wave, def.waves.length)}/${def.waves.length}`);
    l.push(`Inimigos: ${c.air.filter((a) => a.team === 'red' && !a.dying).length} no ar${def.ground ? ' · ' + c.gnd.filter((g) => g.alive).length + ' no solo' : ''}`);
    return l;
  };

  return c;
}

const Combat = { create, MISSIONS, DIFF, GND, util: { lead, add, sub, scl, dot, len, norm, cross, dist, angle, turnToward } };
if (typeof module !== 'undefined') module.exports = Combat; else root.Combat = Combat;
})(typeof self !== 'undefined' ? self : globalThis);
