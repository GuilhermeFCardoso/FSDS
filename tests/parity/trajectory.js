const path = require('path');
const root = path.join(__dirname, '..', '..');
const Sim = require(path.join(root, 'physics.js'));
const AIR = require(path.join(root, 'aircraft.js'));
Math.random = () => 0.5;

const row = (name, t, s) => [name, t.toFixed(2), s.lat, s.lon, s.z, s.v[0], s.v[1], s.v[2], s.q[0], s.q[1], s.q[2], s.q[3], s.w[0], s.w[1], s.w[2], s.n, s.ab, s.fuel, s.gear, s.flaps, s.crashed ? 1 : 0].map((x) => (typeof x === 'number' ? x.toPrecision(12) : x)).join(',');
const lines = [];

function run(name, a, s, ctl, gz, script) {
  const h = 1 / 120;
  for (let i = 0; i <= 120 * 40; i++) {
    const t = i * h;
    if (i % 60 === 0) lines.push(row(name, t, s));
    script(t, ctl, s);
    Sim.step(s, a, ctl, h, gz);
  }
}
function air(name, ac, V, z) {
  const a = Sim.prepare(ac);
  const s = Sim.spawnAir(a, { lat: -0.4, lon: -0.8, z, groundZ: 0, hdg: 1, V, fuelFrac: 0.6 });
  const ctl = { pitch: 0, roll: 0, yaw: 0, throttle: s.throttle, trim: s.trim, brake: 0, parking: false, gearDown: !!ac.gearFixed, flapIdx: 0, speedbrake: 0, assist: false, stallProt: false };
  run(name, a, s, ctl, 0, (t, c) => {
    c.pitch = t > 2 && t < 5 ? 0.6 : t > 8 && t < 10 ? -0.4 : 0;
    c.roll = t > 5 && t < 8 ? 0.7 : t > 12 && t < 14 ? -0.8 : 0;
    c.yaw = t > 15 && t < 17 ? 0.5 : 0;
    c.speedbrake = t > 25 && t < 28 ? 1 : 0;
    if (t > 18 && t < 24) c.throttle = 1;
    if (t >= 24) c.throttle = 0.5;
  });
}
function ground(name, ac, vr) {
  const a = Sim.prepare(ac);
  const s = Sim.spawnGround(a, { lat: -0.4, lon: -0.8, groundZ: 500, hdg: 1, fuelFrac: 0.6 });
  const ctl = { pitch: 0, roll: 0, yaw: 0, throttle: 0, trim: 0, brake: 0, parking: false, gearDown: true, flapIdx: 1, speedbrake: 0, assist: false, stallProt: false };
  s.flaps = a.flapNotches[1];
  run(name, a, s, ctl, 500, (t, c, st) => {
    c.throttle = t > 1 ? 1 : 0;
    c.pitch = (st.diag.ias ?? 0) > vr && t < 28 ? 0.45 : 0;
    if (t > 26) c.gearDown = false;
    c.brake = t < 1 ? 1 : 0;
  });
}
air('gripen_air', AIR.gripen, 180, 3000);
air('tucano_air', AIR.tucano, 85, 2000);
air('b737_air', AIR.b737, 130, 3000);
ground('gripen_to', AIR.gripen, 135);
ground('tucano_to', AIR.tucano, 85);
ground('b737_to', AIR.b737, 150);
console.log(lines.join('\n'));
