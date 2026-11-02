(function (root) {
const clamp = (x, a, b) => Math.min(b, Math.max(a, x));
let ctx, master, nb, muted = false;
const N = {};

function chain(type, f, q) {
  const s = ctx.createBufferSource();
  s.buffer = nb; s.loop = true;
  const flt = ctx.createBiquadFilter();
  flt.type = type; flt.frequency.value = f; flt.Q.value = q;
  const g = ctx.createGain(); g.gain.value = 0;
  s.connect(flt).connect(g).connect(master);
  s.start(0, Math.random() * 1.5);
  return { flt, g };
}

function tone(type, f) {
  const o = ctx.createOscillator(), g = ctx.createGain(), flt = ctx.createBiquadFilter();
  o.type = type; o.frequency.value = f; g.gain.value = 0; flt.type = 'lowpass'; flt.frequency.value = 900;
  o.connect(flt).connect(g).connect(master); o.start();
  return { o, g, flt };
}

function init() {
  if (ctx) { ctx.resume?.(); return; }
  try {
    ctx = new AudioContext();
    master = ctx.createGain(); master.gain.value = muted ? 0 : 0.9; master.connect(ctx.destination);
    nb = ctx.createBuffer(1, ctx.sampleRate * 2, ctx.sampleRate);
    const d = nb.getChannelData(0);
    for (let i = 0; i < d.length; i++) d[i] = Math.random() * 2 - 1;
    N.rumble = chain('lowpass', 160, 0.7);
    N.jet = chain('bandpass', 900, 0.8);
    N.roar = chain('lowpass', 450, 0.5);
    N.wind = chain('bandpass', 700, 0.5);
    N.whine = tone('sine', 900);
    N.piston = tone('sawtooth', 40);
    N.horn = tone('sawtooth', 1050);
    const lfo = ctx.createOscillator(), lg = ctx.createGain();
    lfo.frequency.value = 7; lg.gain.value = 60; lfo.connect(lg).connect(N.horn.o.frequency); lfo.start();
    N.horn.flt.frequency.value = 3000;
    N.lock = tone('sine', 700); N.lock.flt.frequency.value = 4000;
    N.rwr = tone('square', 1700); N.rwr.flt.frequency.value = 3200;
  } catch { ctx = null; }
}

const ramp = (p, v, t = 0.08) => p.setTargetAtTime(v, ctx.currentTime, t);

function update(a) {
  if (!ctx) return;
  const { ac, s, d, on } = a, e = ac.engine, n = on ? s.n : 0, ab = on ? s.ab : 0;
  const V = on ? d.tas / 1.944 : 0, q = on ? d.qbar : 0;
  const alive = on && s.fuel > 0 ? 1 : 0;
  ramp(N.horn.g.gain, on && d.stallWarn ? 0.05 : 0, 0.03);
  if (e.type === 'jet') {
    const big = ac.mass0 > 20000 ? 1 : 0;
    ramp(N.rumble.g.gain, alive * (0.06 + 0.3 * n ** 1.5) * (big ? 0.7 : 1), 0.15);
    ramp(N.jet.flt.frequency, 450 + 1700 * n, 0.15);
    ramp(N.jet.g.gain, alive * (0.015 + 0.17 * n * n) * (big ? 0.7 : 1), 0.15);
    ramp(N.roar.g.gain, alive * ab * 0.5, 0.2);
    ramp(N.roar.flt.frequency, 380 + 350 * ab, 0.2);
    N.whine.o.frequency.setTargetAtTime(700 + 3800 * n, ctx.currentTime, 0.2);
    ramp(N.whine.g.gain, alive * (0.004 + 0.02 * n) * (big ? 1.4 : 0.8), 0.2);
    ramp(N.piston.g.gain, 0, 0.1);
  } else {
    const rpm = (e.turbo ? 1400 : 700) + (e.turbo ? 600 : 2000) * n;
    N.piston.o.frequency.setTargetAtTime(e.turbo ? rpm / 60 * 5 : rpm / 60 * 2, ctx.currentTime, 0.1);
    N.piston.flt.frequency.setTargetAtTime(e.turbo ? 500 : 650 + 200 * n, ctx.currentTime, 0.1);
    ramp(N.piston.g.gain, alive * (0.05 + 0.13 * n), 0.15);
    ramp(N.rumble.g.gain, alive * (0.03 + 0.12 * n), 0.15);
    ramp(N.jet.flt.frequency, 280 + 300 * n, 0.15);
    ramp(N.jet.g.gain, alive * (0.01 + 0.07 * n), 0.15);
    N.whine.o.frequency.setTargetAtTime(e.turbo ? 1500 + 900 * n : 100, ctx.currentTime, 0.2);
    ramp(N.whine.g.gain, e.turbo ? alive * (0.004 + 0.012 * n) : 0, 0.2);
    ramp(N.roar.g.gain, 0, 0.2);
  }
  ramp(N.wind.g.gain, clamp(q / 4500, 0, 0.35) * (on ? 1 : 0), 0.2);
  ramp(N.wind.flt.frequency, 500 + 6 * V, 0.2);
}

function burst(t, peak, dur, f) {
  const s = ctx.createBufferSource(), flt = ctx.createBiquadFilter(), g = ctx.createGain();
  s.buffer = nb; flt.type = 'lowpass'; flt.frequency.value = f;
  g.gain.setValueAtTime(0, t); g.gain.linearRampToValueAtTime(peak, t + 0.008); g.gain.exponentialRampToValueAtTime(0.001, t + dur);
  s.connect(flt).connect(g).connect(master); s.start(t, Math.random()); s.stop(t + dur + 0.05);
}

function boom(strength = 1) {
  if (!ctx) return;
  const t = ctx.currentTime;
  burst(t, 1.0 * strength, 0.9, 420); burst(t + 0.11, 0.8 * strength, 0.9, 380);
  const o = ctx.createOscillator(), g = ctx.createGain();
  o.type = 'sine'; o.frequency.setValueAtTime(95, t); o.frequency.exponentialRampToValueAtTime(32, t + 0.9);
  g.gain.setValueAtTime(0.9 * strength, t); g.gain.exponentialRampToValueAtTime(0.001, t + 1.1);
  o.connect(g).connect(master); o.start(t); o.stop(t + 1.2);
}

let gunAcc = 0;
function combat(o) {
  if (!ctx) return;
  const t = ctx.currentTime, pulse = (hz) => (Math.floor(t * hz) % 2 ? 1 : 0);
  if (o.firing) {
    gunAcc += o.dt * Math.min(o.rate, 32);
    while (gunAcc >= 1) { gunAcc--; burst(t + Math.random() * 0.02, 0.22, 0.07, 2600); }
  } else gunAcc = 0;
  const lk = o.lock;
  N.lock.o.frequency.setTargetAtTime(lk === 2 ? 1450 : 560, t, 0.01);
  ramp(N.lock.g.gain, lk === 2 ? 0.05 : lk === 1 ? 0.035 * pulse(5) : 0, 0.01);
  const r = o.rwr;
  N.rwr.o.frequency.setTargetAtTime(pulse(r === 2 ? 6 : 3) ? 1900 : 1500, t, 0.005);
  ramp(N.rwr.g.gain, r === 2 ? 0.05 : r === 1 ? 0.03 * pulse(3) : 0, 0.01);
}

function boomAt(dist, size = 1) {
  if (!ctx) return;
  const k = Math.min(1, size / (1 + dist / 500));
  if (k < 0.02) return;
  const t = ctx.currentTime + Math.min(dist / 340, 6);
  burst(t, 0.9 * k, 0.9 + 0.5 * size, 380); burst(t + 0.08, 0.6 * k, 0.7, 700);
  const o = ctx.createOscillator(), g = ctx.createGain();
  o.type = 'sine'; o.frequency.setValueAtTime(80, t); o.frequency.exponentialRampToValueAtTime(28, t + 0.8);
  g.gain.setValueAtTime(0, t); g.gain.linearRampToValueAtTime(0.8 * k, t + 0.02); g.gain.exponentialRampToValueAtTime(0.001, t + 1);
  o.connect(g).connect(master); o.start(t); o.stop(t + 1.1);
}

function launch(dist = 0) {
  if (!ctx) return;
  const t = ctx.currentTime, k = 1 / (1 + dist / 600);
  burst(t, 0.45 * k, 1.5, 1500); burst(t + 0.05, 0.25 * k, 1.1, 3500);
}

function beep(f = 900, dur = 0.08, vol = 0.07) {
  if (!ctx) return;
  const t = ctx.currentTime, o = ctx.createOscillator(), g = ctx.createGain();
  o.type = 'sine'; o.frequency.value = f;
  g.gain.setValueAtTime(vol, t); g.gain.exponentialRampToValueAtTime(0.001, t + dur);
  o.connect(g).connect(master); o.start(t); o.stop(t + dur + 0.02);
}

function thud() { if (ctx) { burst(ctx.currentTime, 0.5, 0.18, 700); beep(130, 0.18, 0.25); } }

function toggle() {
  muted = !muted;
  if (master) master.gain.value = muted ? 0 : 0.9;
  return muted;
}

root.Sound = { init, update, boom, combat, boomAt, launch, beep, thud, toggle, get muted() { return muted; } };
})(typeof self !== 'undefined' ? self : globalThis);
