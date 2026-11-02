(function (root) {
const G = 9.80665, RE = 6371000, KT = 1.943844, FT = 3.28084;
const clamp = (x, a, b) => (x < a ? a : x > b ? b : x);

const NOENV = { wn: 0, we: 0, sigma: 0, dT: 0, k: 1 };

function isa(h, env = NOENV) {
  const hh = clamp(h, -500, 25000);
  let T = hh < 11000 ? 288.15 - 0.0065 * hh : 216.65;
  let rho = hh < 11000 ? 1.225 * Math.pow(T / 288.15, 4.2559) : 0.3639 * Math.exp(-(hh - 11000) / 6341.6);
  if (env.dT || env.k !== 1) { rho *= (T / (T + env.dT)) * env.k; T += env.dT; }
  return { rho, a: Math.sqrt(401.87 * T), T };
}

function windAt(env, hAgl) {
  const f = Math.pow(clamp(hAgl, 2, 300) / 10, 0.14);
  return [env.wn * f, env.we * f];
}

const smooth = (a, b, x) => { const t = clamp((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t); };
function liftSlope(M) {
  const sub = 1 + 0.5 * (1 / Math.sqrt(1 - Math.min(M, 0.85) ** 2) - 1);
  if (M < 0.85) return sub;
  const sup = clamp(1.05 / Math.sqrt(Math.max(M * M - 1, 0.01)), 0.55, 1.1);
  return sub + (sup - sub) * smooth(0.85, 1.2, M);
}

const randn = () => Math.random() + Math.random() + Math.random() - 1.5;

const qMat = ([w, x, y, z]) => [
  1 - 2 * (y * y + z * z), 2 * (x * y - w * z), 2 * (x * z + w * y),
  2 * (x * y + w * z), 1 - 2 * (x * x + z * z), 2 * (y * z - w * x),
  2 * (x * z - w * y), 2 * (y * z + w * x), 1 - 2 * (x * x + y * y),
];
const mul = (R, v) => [
  R[0] * v[0] + R[1] * v[1] + R[2] * v[2],
  R[3] * v[0] + R[4] * v[1] + R[5] * v[2],
  R[6] * v[0] + R[7] * v[1] + R[8] * v[2],
];
const mulT = (R, v) => [
  R[0] * v[0] + R[3] * v[1] + R[6] * v[2],
  R[1] * v[0] + R[4] * v[1] + R[7] * v[2],
  R[2] * v[0] + R[5] * v[1] + R[8] * v[2],
];
const cross = (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];

function qMulQ(a, b) {
  return [
    a[0] * b[0] - a[1] * b[1] - a[2] * b[2] - a[3] * b[3],
    a[0] * b[1] + a[1] * b[0] + a[2] * b[3] - a[3] * b[2],
    a[0] * b[2] - a[1] * b[3] + a[2] * b[0] + a[3] * b[1],
    a[0] * b[3] + a[1] * b[2] - a[2] * b[1] + a[3] * b[0],
  ];
}

function qFromEuler(phi, theta, psi) {
  const cr = Math.cos(phi / 2), sr = Math.sin(phi / 2);
  const cp = Math.cos(theta / 2), sp = Math.sin(theta / 2);
  const cy = Math.cos(psi / 2), sy = Math.sin(psi / 2);
  return [
    cr * cp * cy + sr * sp * sy,
    sr * cp * cy - cr * sp * sy,
    cr * sp * cy + sr * cp * sy,
    cr * cp * sy - sr * sp * cy,
  ];
}

function euler([w, x, y, z]) {
  return {
    phi: Math.atan2(2 * (w * x + y * z), 1 - 2 * (x * x + y * y)),
    theta: Math.asin(clamp(2 * (w * y - z * x), -1, 1)),
    psi: Math.atan2(2 * (w * z + x * y), 1 - 2 * (y * y + z * z)),
  };
}

function prepare(ac) {
  if (ac._ready) return ac;
  ac.flapMax = ac.flapNotches[ac.flapNotches.length - 1];
  ac.gain = { e: 1, a: 1, r: 1, ...ac.gain };
  ac.stall = { wake: 0.45, qd: 1, knee: 0.09, w: 0.03, on: 0.2, off: 0.5, cm0: 0.2, cm1: 1.5, rot: 1.15, rb: 1.5, ail: 0.6, cd: 1.6, bias: 0.03, warn: 0.045, ...ac.stall };
  [ac.Ix, ac.Iy, ac.Iz] = ac.I;
  const mref = ac.mass0 + ac.fuelMax * 0.5;
  for (const g of ac.gear) {
    g.k = g.share * mref * G / ac.gearDefl;
    g.c = 2 * 0.35 * Math.sqrt(g.k * g.share * mref);
    g.N0 = g.share * mref * G;
  }
  ac._ready = true;
  return ac;
}

function thrustCalc(ac, n, ab, atm, V, hasFuel) {
  const e = ac.engine, sigma = atm.rho / 1.225;
  if (!hasFuel) return { T: 0, flow: 0 };
  if (e.type === 'piston') {
    const P = e.P * (0.04 + 0.96 * n) * (e.turbo ? Math.pow(sigma, 0.85) : sigma - (1 - sigma) / 7.55);
    return { T: Math.max(0, e.eta * P / (V + e.v0)), flow: e.bsfc * Math.max(P, 0) / 3.6e6 };
  }
  const M = V / atm.a;
  const sg = Math.pow(sigma, e.sx) * Math.max(0.3, 1 + e.km * M);
  const tf = clamp((n - e.idle) / (1 - e.idle), 0, 1);
  const dry = e.T0 * (e.fi + (1 - e.fi) * Math.pow(tf, 1.4)) * sg;
  let flow = dry * e.tsfc, T = dry;
  if (e.ab && ab > 0) {
    const extra = (e.TAB - e.T0) * sg * ab;
    T += extra;
    flow += extra * e.tsfcAB;
  }
  return { T, flow };
}

function newState(ac, o) {
  prepare(ac);
  return {
    lat: o.lat, lon: o.lon, z: o.z, v: [0, 0, 0], q: qFromEuler(0, 0, o.hdg), w: [0, 0, 0],
    fuel: ac.fuelMax * (o.fuelFrac ?? 0.5),
    n: ac.engine.type === 'piston' ? 0 : ac.engine.idle, ab: 0,
    flaps: 0, gear: 1, sb: 0, de: 0, da: 0, dr: 0, att: 1, stallDir: 0, bn: [0, 0, 0], buf: 0,
    contact: false, crashed: '', diag: {}, thrust: 0, flow: 0, env: o.env || NOENV, turb: [0, 0, 0],
  };
}

function spawnGround(ac, o) {
  const s = newState(ac, { ...o, z: o.groundZ + ac.cgHeight });
  s.contact = true;
  return s;
}

function spawnAir(ac, o) {
  const s = newState(ac, o);
  const V = o.V, atm = isa(o.z, s.env), m = ac.mass0 + s.fuel;
  const qS = 0.5 * atm.rho * V * V * ac.S;
  s.gear = ac.gearFixed ? 1 : 0;
  s.flaps = 0;
  const alpha = (m * G / qS - ac.CL0) / ac.CLa;
  s.q = qFromEuler(0, alpha, o.hdg);
  const w0 = windAt(s.env, o.z - (o.groundZ ?? 0));
  s.v = [V * Math.cos(o.hdg) + w0[0], V * Math.sin(o.hdg) + w0[1], 0];
  s.trim = 0;
  if (ac.control === 'direct') {
    const cm = ac.Cm0 + ac.Cma * alpha;
    s.trim = clamp(-cm / ac.Cmde / ac.trimAuth, -1, 1);
  }
  const CD = ac.CD0 + ac.k * (ac.CL0 + ac.CLa * alpha) ** 2;
  const D = qS * CD;
  let lo = 0, hi = 1;
  for (let i = 0; i < 30; i++) {
    const mid = (lo + hi) / 2;
    const e = ac.engine;
    const nn = e.type === 'piston' ? mid : e.idle + (1 - e.idle) * clamp(mid / (e.ab ? 0.88 : 1), 0, 1);
    const T = thrustCalc(ac, nn, 0, atm, V, true).T;
    if (T < D) lo = mid; else hi = mid;
  }
  s.throttle = hi;
  const e = ac.engine;
  s.n = e.type === 'piston' ? hi : e.idle + (1 - e.idle) * clamp(hi / (e.ab ? 0.88 : 1), 0, 1);
  s.contact = false;
  return s;
}

function step(s, ac, ctl, dt, groundZ) {
  if (s.crashed) return;
  const m = ac.mass0 + s.fuel;
  const env = s.env;
  const atm = isa(s.z, env);
  const hw = s.z - groundZ, wmean = windAt(env, hw);
  if (env.sigma > 0) {
    const kt = Math.exp(-dt / 2.5), sg = env.sigma * (1 + 0.4 * Math.exp(-Math.max(hw, 0) / 150)) * Math.sqrt(1 - kt * kt) * 2;
    s.turb[0] = s.turb[0] * kt + sg * randn();
    s.turb[1] = s.turb[1] * kt + sg * randn();
    s.turb[2] = s.turb[2] * kt + sg * 0.6 * randn();
  }
  const wind = [wmean[0] + s.turb[0], wmean[1] + s.turb[1], s.turb[2]];
  const R = qMat(s.q);
  const vb = mulT(R, [s.v[0] - wind[0], s.v[1] - wind[1], s.v[2] - wind[2]]);
  const V = Math.hypot(vb[0], vb[1], vb[2]);
  const alpha = V > 1 ? Math.atan2(vb[2], vb[0]) : 0;
  const beta = V > 1 ? Math.asin(clamp(vb[1] / V, -1, 1)) : 0;
  const Vn = Math.max(V, 12);
  const qbar = 0.5 * atm.rho * V * V;
  const qS = qbar * ac.S;
  const qbEff = Math.max(qbar, 400);
  const M = V / atm.a, kM = liftSlope(M), sup = ac.wave?.sup ? smooth(0.9, 1.3, M) : 0;
  const Cma = ac.Cma * (1 + sup), Cmde = ac.Cmde * (1 - 0.3 * smooth(1, 1.6, M));
  const [p, q, r] = s.w;
  const ph = p * ac.b / (2 * Vn), qh = q * ac.c / (2 * Vn), rh = r * ac.b / (2 * Vn);
  const eul = euler(s.q);
  const f = s.flaps / ac.flapMax;

  const flapTarget = ac.flapNotches[ctl.flapIdx];
  s.flaps += clamp(flapTarget - s.flaps, -ac.flapRate * dt, ac.flapRate * dt);
  if (!ac.gearFixed) {
    const gTarget = ctl.gearDown || s.contact ? 1 : 0;
    s.gear += clamp(gTarget - s.gear, -ac.gearRate * dt, ac.gearRate * dt);
  }
  s.sb += clamp(ctl.speedbrake - s.sb, -dt, dt);

  const st = ac.stall, aAbs = Math.abs(alpha), sgnA = alpha < 0 ? -1 : 1;
  const a0 = ac.aStall + ac.dAsf * f;
  const x0 = 0.5 * (1 - Math.tanh((aAbs - a0 - 0.01) / st.w));
  s.att += (x0 - s.att) * (1 - Math.exp(-dt / (x0 < s.att ? st.on : st.off)));
  const sep = 1 - s.att;
  if (sep > 0.15 && !s.stallDir) s.stallDir = Math.sign(ctl.roll * 0.4 + ctl.yaw * 0.8 - beta * 3 + (Math.random() - 0.5) * 0.5) || 1;
  else if (sep < 0.03) s.stallDir = 0;
  const buf = V > 8 && !s.contact ? clamp(Math.max((aAbs - a0 + 0.1) / 0.1, sep), 0, 1) : 0;
  s.buf = buf;
  for (let i = 0; i < 3; i++) s.bn[i] += (Math.random() * 2 - 1 - s.bn[i]) * (1 - Math.exp(-dt * 10));
  const Clda = ac.Clda * (1 - st.ail * sep);

  const cm0 = ac.Cm0 + ac.dCmf * f + Cma * alpha + ac.Cmq * (1 + st.qd * sep) * qh - sep * (st.cm0 * sgnA + st.cm1 * alpha);
  const cl0 = ac.Clb * (1 - st.rb * sep) * beta + ac.Clp * (1 - st.rot * sep) * ph + ac.Clr * rh + sep * st.bias * s.stallDir + 0.1 * buf * s.bn[0];
  const cn0 = ac.Cnb * (1 - 0.6 * sep) * beta + ac.Cnr * rh + ac.Cnp * ph + 0.02 * buf * s.bn[2];
  const Vc = Math.max(V, 55);

  const yawLaw = (lr, daC) => {
    const rCmd = G / Vc * Math.sin(eul.phi) * Math.cos(eul.theta) + p * alpha;
    const bCmd = -ctl.yaw * 0.15;
    const rdot = lr * (rCmd - r) + (ac.control === "fbw" ? 2 : 6) * (beta - bCmd);
    return clamp((ac.Iz * rdot / (qbEff * ac.S * ac.b) - cn0 - ac.Cnda * daC) / ac.Cndr, -1, 1);
  };

  let deC, daC, drC;
  if (ac.control === 'fbw') {
    const F = ac.fbw;
    const nzUp = ctl.stallProt ? Math.max(1, Math.min(F.nzMax, qS * (ac.CL0 + ac.dCLf * f + ac.CLa * kM * F.alphaLim) / (m * G))) : F.nzMax;
    const nzDn = ctl.stallProt ? clamp(qS * (ac.CL0 + ac.dCLf * f - ac.CLa * kM * 0.35) / (m * G), F.nzMin, 1) : F.nzMin;
    const nzCmd = ctl.pitch >= 0 ? 1 + ctl.pitch * (nzUp - 1) : 1 + ctl.pitch * (1 - nzDn);
    const qCmd = G * (nzCmd - Math.cos(eul.theta) * Math.cos(eul.phi)) / Vc + p * Math.tan(beta);
    deC = clamp((ac.Iy * F.lq * (qCmd - q) / (qbEff * ac.S * ac.c) - cm0) / Cmde, -1, 1);
    const pCmd = ctl.roll * F.pMax * clamp(V / 120, 0.3, 1);
    daC = clamp((ac.Ix * F.lp * (pCmd - p) / (qbEff * ac.S * ac.b) - cl0) / Clda, -1, 1);
    drC = s.contact || qbar < 600 ? ctl.yaw : yawLaw(F.lr, daC);
  } else {
    const ka = clamp((ac.vRef / Math.max(V, 10)) ** 2, 0.25, 1), g = ac.gain;
    deC = clamp(ctl.pitch * ka * g.e + ctl.trim * ac.trimAuth, -1, 1);
    daC = ctl.roll * ka * g.a;
    drC = ctl.assist && !s.contact && qbar > 600 ? yawLaw(4, daC) : ctl.yaw * (s.contact ? 1 : ka * g.r);
  }
  const kd = (x, c, tau) => x + (c - x) * (1 - Math.exp(-dt / tau));
  s.de = kd(s.de, deC, 0.07);
  s.da = kd(s.da, daC, 0.07);
  s.dr = kd(s.dr, drC, 0.08);

  const dCLf = ac.dCLf * Math.pow(f, ac.flapExp ?? 1);
  const kn = a0 - st.knee, alin = aAbs > kn ? sgnA * (kn + 0.6 * (aAbs - kn)) : alpha;
  const CLlin = ac.CL0 + dCLf + ac.CLa * kM * alin + ac.dCLsb * s.sb;
  const sa = Math.sin(alpha), ca = Math.cos(alpha);
  const wfp = clamp((aAbs - a0 - 0.25) / 0.35, 0, 1);
  const CLs = CLlin * ((1 + Math.sqrt(s.att)) / 2) ** 2;
  const CL = (1 - wfp) * CLs + wfp * 0.8 * Math.sin(2 * alpha);

  let wave = 0;
  const wv = ac.wave;
  if (wv?.sup) wave = wv.peak * Math.exp(-(((M - 1.05) / 0.12) ** 2)) + wv.post * clamp((M - wv.crit) / 0.3, 0, 1);
  else if (wv && M > wv.mdd) wave = wv.k * Math.pow(M - wv.mdd, 4);
  const hAgl = s.z - groundZ;
  const gh = Math.max(hAgl - 1, 0.5) * 16 / ac.b;
  const geF = gh * gh / (1 + gh * gh);
  const CDlin = ac.CD0 + ac.dCDf * f * f + ac.dCDgear * s.gear + ac.dCDsb * s.sb + wave + ac.k * geF * CLs * CLs;
  const CD = CDlin + sep * st.cd * sa * sa;
  const CY = ac.CYb * beta + ac.CYdr * s.dr;

  const L = qS * CL, D = qS * CD, Y = qS * CY;
  let Fx = -D * ca + L * sa, Fy = Y, Fz = -D * sa - L * ca;

  const e = ac.engine;
  const out = s.fuel > 0;
  let nCmd, abCmd = 0;
  if (e.type === 'piston') nCmd = out ? ctl.throttle : 0;
  else {
    nCmd = out ? e.idle + (1 - e.idle) * clamp(ctl.throttle / (e.ab ? 0.88 : 1), 0, 1) : 0;
    if (e.ab && out) abCmd = clamp((ctl.throttle - 0.9) / 0.08, 0, 1);
  }
  s.n = kd(s.n, nCmd, e.tau);
  s.ab = kd(s.ab, abCmd, 0.8);
  const th = thrustCalc(ac, s.n, s.ab, atm, V, out);
  s.thrust = th.T;
  s.flow = th.flow;
  s.fuel = Math.max(0, s.fuel - th.flow * dt);
  Fx += th.T;

  let Mx = qS * ac.b * (cl0 + Clda * s.da + ac.Cldr * s.dr);
  let My = qS * ac.c * (cm0 + Cmde * (1 - st.wake * sep) * s.de + 0.07 * buf * s.bn[1]) + th.T * e.dz;
  let Mz = qS * ac.b * (cn0 + ac.Cndr * s.dr + ac.Cnda * s.da);
  if (e.type === 'piston') Mz -= 0.1 * th.T / (1 + V / 40);

  const gravB = mulT(R, [0, 0, m * G]);
  Fx += gravB[0]; Fy += gravB[1]; Fz += gravB[2];
  const nonGrav = [Fx - gravB[0], Fy - gravB[1], Fz - gravB[2]];

  let contact = false, crash = '';
  const brake = Math.max(ctl.brake, ctl.parking ? 1 : 0);
  const wheelsOn = ac.gearFixed || s.gear > 0.95;
  const apply = (rb, Fned) => {
    const Fb = mulT(R, Fned);
    Fx += Fb[0]; Fy += Fb[1]; Fz += Fb[2];
    nonGrav[0] += Fb[0]; nonGrav[1] += Fb[1]; nonGrav[2] += Fb[2];
    const mm = cross(rb, Fb);
    Mx += mm[0]; My += mm[1]; Mz += mm[2];
  };
  const contactAt = (rb, k, c, mu, brk, steer, N0, stroke) => {
    const rn = mul(R, rb);
    const depth = groundZ - (s.z - rn[2]);
    if (depth <= 0) return;
    const vr = mul(R, cross(s.w, rb));
    const vp = [s.v[0] + vr[0], s.v[1] + vr[1], s.v[2] + vr[2]];
    let N = k * depth * (1 + (stroke ? 2 * (depth / stroke) ** 2 : 0)) + c * vp[2];
    if (N <= 0) return;
    const sp = Math.hypot(vp[0], vp[1]);
    const d = steer * Math.PI / 180;
    const fwdB = [Math.cos(d), Math.sin(d), 0];
    const fn = mul(R, fwdB);
    const fh = Math.hypot(fn[0], fn[1]) || 1;
    const fx = fn[0] / fh, fy = fn[1] / fh;
    const lx = -fy, ly = fx;
    const vf = vp[0] * fx + vp[1] * fy, vl = vp[0] * lx + vp[1] * ly;
    let Ff, Fl;
    if (brk === null) {
      Ff = sp > 1e-3 ? -N * mu * Math.tanh(sp / 0.5) * vp[0] / sp : 0;
      Fl = sp > 1e-3 ? -N * mu * Math.tanh(sp / 0.5) * vp[1] / sp : 0;
      apply(rb, [Ff, Fl, -N]);
      return;
    }
    Ff = -N * (0.02 + 0.55 * brk) * Math.tanh(vf / 0.15);
    Fl = -N * 0.9 * Math.tanh(vl / 0.25);
    apply(rb, [Ff * fx + Fl * lx, Ff * fy + Fl * ly, -N]);
    contact = true;
    if (N0 && N / N0 > ac.gearLoadLimit) crash = 'Trem de pouso colapsou no impacto';
  };

  if (wheelsOn) {
    for (const g of ac.gear) {
      const steer = g.steer ? ctl.yaw * g.steer / (1 + Math.hypot(s.v[0], s.v[1]) / 12) : 0;
      contactAt(g.p, g.k, g.c, 0, g.brake ? brake : 0, steer, g.N0, ac.gearStroke);
    }
  }
  const kc = m * G / 0.03, cc = Math.sqrt(kc * m);
  for (const rb of ac.crash) {
    const rn = mul(R, rb);
    const depth = groundZ - (s.z - rn[2]);
    if (depth <= 0.02) continue;
    const spd = Math.hypot(s.v[0], s.v[1]);
    const vdown = s.v[2] + mul(R, cross(s.w, rb))[2];
    if (rb[3] ? depth > 0.4 || vdown > 3 : spd > 12 || vdown > 3 || depth > 0.3) crash = 'Colisão com o solo';
    contactAt(rb, kc, cc, 0.5, null, 0, 0, 0);
  }

  s.contact = contact;
  if (crash) s.crashed = crash;

  const accB = [Fx / m, Fy / m, Fz / m];
  const acc = mul(R, accB);
  s.v[0] += acc[0] * dt;
  s.v[1] += acc[1] * dt;
  s.v[2] += acc[2] * dt;

  const dp = (Mx + (ac.Iy - ac.Iz) * q * r) / ac.Ix;
  const dq = (My + (ac.Iz - ac.Ix) * p * r) / ac.Iy;
  const dr = (Mz + (ac.Ix - ac.Iy) * p * q) / ac.Iz;
  s.w[0] += dp * dt; s.w[1] += dq * dt; s.w[2] += dr * dt;

  const wm = Math.hypot(s.w[0], s.w[1], s.w[2]);
  if (wm > 1e-9) {
    const h = wm * dt / 2, k = Math.sin(h) / wm;
    s.q = qMulQ(s.q, [Math.cos(h), s.w[0] * k, s.w[1] * k, s.w[2] * k]);
    const n = Math.hypot(...s.q);
    s.q = s.q.map((x) => x / n);
  }

  s.lat += s.v[0] / (RE + s.z) * dt;
  s.lon += s.v[1] / ((RE + s.z) * Math.cos(s.lat)) * dt;
  s.z -= s.v[2] * dt;

  const sigma = atm.rho / 1.225;
  const nzNow = -nonGrav[2] / (m * G);
  if (!s.contact && V > 20 && (nzNow > ac.nzLimit[1] * 1.5 || nzNow < ac.nzLimit[0] * 1.5)) s.overG = (s.overG || 0) + dt; else s.overG = 0;
  if (s.overG > 0.15 && !s.crashed) s.crashed = 'Estrutura rompeu por sobrecarga de G';
  s.diag = {
    alpha, beta, ias: V * Math.sqrt(sigma) * KT, tas: V * KT, mach: M, qbar,
    nz: -nonGrav[2] / (m * G), ay: nonGrav[1] / m, gs: Math.hypot(s.v[0], s.v[1]),
    hdg: ((eul.psi * 180 / Math.PI) % 360 + 360) % 360, pitch: eul.theta, roll: eul.phi,
    vsi: -s.v[2] * 196.85, turn: (q * Math.sin(eul.phi) + r * Math.cos(eul.phi)) / Math.max(Math.cos(eul.theta), 0.1),
    agl: s.z - groundZ, nPct: s.n * 100, mass: m, wkt: Math.hypot(wmean[0], wmean[1]) * KT, wdir: ((Math.atan2(-wmean[1], -wmean[0]) * 180 / Math.PI) % 360 + 360) % 360, buf, sep, stallWarn: !s.contact && aAbs > a0 - st.warn,
  };
}

const Sim = { step, prepare, spawnGround, spawnAir, isa, windAt, euler, qFromEuler, KT, FT, G };
if (typeof module !== 'undefined') module.exports = Sim; else root.Sim = Sim;
})(typeof self !== 'undefined' ? self : globalThis);
