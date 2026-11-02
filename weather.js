(function (root) {
const KT = 0.514444;
const WMO = {
  0: 'Céu limpo', 1: 'Poucas nuvens', 2: 'Parcialmente nublado', 3: 'Nublado', 45: 'Nevoeiro', 48: 'Nevoeiro com geada',
  51: 'Garoa fraca', 53: 'Garoa', 55: 'Garoa forte', 61: 'Chuva fraca', 63: 'Chuva', 65: 'Chuva forte',
  80: 'Pancadas fracas', 81: 'Pancadas de chuva', 82: 'Pancadas fortes', 95: 'Trovoada', 96: 'Trovoada com granizo', 99: 'Trovoada com granizo forte',
};
const CALM = { dir: 0, kt: 0, gust: 0, tempC: 15, qnh: 1013.25, vis: 20000, cloud: 0, desc: 'Padrão: sem vento, atmosfera ISA' };

async function live(lat, lon) {
  const u = `https://api.open-meteo.com/v1/forecast?latitude=${lat.toFixed(4)}&longitude=${lon.toFixed(4)}&current=temperature_2m,pressure_msl,wind_speed_10m,wind_direction_10m,wind_gusts_10m,cloud_cover,visibility,weather_code&wind_speed_unit=kn&timezone=auto`;
  const r = await fetch(u);
  if (!r.ok) throw new Error('HTTP ' + r.status);
  const c = (await r.json()).current;
  return {
    dir: c.wind_direction_10m, kt: c.wind_speed_10m, gust: c.wind_gusts_10m, tempC: c.temperature_2m, qnh: c.pressure_msl,
    vis: c.visibility ?? 20000, cloud: c.cloud_cover, desc: WMO[c.weather_code] ?? 'Sem descrição', time: c.time, live: true,
  };
}

function env(wx, elev) {
  const ms = wx.kt * KT, gs = Math.max(wx.gust, wx.kt) * KT, a = wx.dir * Math.PI / 180;
  return {
    wn: -ms * Math.cos(a), we: -ms * Math.sin(a),
    sigma: ms > 0.2 ? 0.06 * ms + 0.25 * (gs - ms) + 0.15 : 0.05,
    dT: wx.tempC - (15 - 0.0065 * elev), k: wx.qnh / 1013.25,
  };
}

function components(wx, hdg) {
  const d = (wx.dir - hdg) * Math.PI / 180;
  return { head: wx.kt * Math.cos(d), cross: wx.kt * Math.sin(d) };
}

function html(wx, hdg) {
  const c = components(wx, hdg), r = (x) => Math.round(x);
  const cross = Math.abs(c.cross) < 0.5 ? 'sem vento cruzado' : `cruzado ${r(Math.abs(c.cross))} kt ${c.cross > 0 ? 'da direita' : 'da esquerda'}`;
  const head = c.head >= 0 ? `vento de proa ${r(c.head)} kt` : `vento de cauda ${r(-c.head)} kt`;
  const wind = wx.kt < 1 ? 'Vento calmo' : `Vento <b>${String(r(wx.dir)).padStart(3, '0')}° / ${r(wx.kt)} kt</b>${wx.gust > wx.kt + 2 ? ` (rajadas ${r(wx.gust)} kt)` : ''}`;
  const vis = wx.vis >= 10000 ? '10 km ou mais' : (wx.vis / 1000).toFixed(1) + ' km';
  return `<div><b>${wx.desc}</b>${wx.time ? ` <small>· dados de ${wx.time.replace('T', ' ')} (hora local)</small>` : ''}</div>
    <div>${wind} · ${r(wx.tempC)} °C · QNH ${r(wx.qnh)} hPa · visibilidade ${vis} · nuvens ${r(wx.cloud)}%</div>
    <div>Na pista (${String(r(hdg)).padStart(3, '0')}°): ${wx.kt < 1 ? 'sem componentes de vento' : head + ' · ' + cross}</div>`;
}

root.Weather = { CALM, live, env, components, html, KT };
})(typeof self !== 'undefined' ? self : globalThis);
