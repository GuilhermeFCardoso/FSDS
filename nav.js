(function (root) {
const D2R = Math.PI / 180, R = 6371008.8, KT = 1.943844;
const dist = (a, b) => {
  const x = Math.sin((b.lat - a.lat) * D2R / 2) ** 2 + Math.cos(a.lat * D2R) * Math.cos(b.lat * D2R) * Math.sin((b.lon - a.lon) * D2R / 2) ** 2;
  return 2 * R * Math.asin(Math.sqrt(x));
};
const bearing = (a, b) => {
  const p1 = a.lat * D2R, p2 = b.lat * D2R, dn = (b.lon - a.lon) * D2R;
  return (Math.atan2(Math.sin(dn) * Math.cos(p2), Math.cos(p1) * Math.sin(p2) - Math.sin(p1) * Math.cos(p2) * Math.cos(dn)) / D2R + 360) % 360;
};
const fmtD = (m) => (m < 9950 ? (m / 1000).toFixed(1) : Math.round(m / 1000)) + ' km';
const fmtT = (s) => (s >= 3600 ? `${Math.floor(s / 3600)} h ${String(Math.floor(s / 60) % 60).padStart(2, '0')} min` : `${Math.floor(s / 60)}:${String(Math.round(s % 60) % 60).padStart(2, '0')}`);
const pad3 = (b) => String(Math.round(b) % 360).padStart(3, '0') + '°';

const PLANE_SVG = '<svg viewBox="0 0 24 24" width="30" height="30"><path d="M12 1.5l2 8 8 4.5v2.2l-8-1.8.6 4.6 2.6 1.8V23L12 21.8 8.8 23v-1.7l2.6-1.8.6-4.6-8 1.8V14l8-4.5z" fill="#fff" stroke="#0b1220" stroke-width="1.2"/></svg>';

const Nav = {
  dist, bearing, pts: [], active: 0, full: false, mini: true, miniZoom: 12, plane: null, o: null,

  init(o) {
    this.o = o;
    const saved = o.load() || {};
    this.pts = saved.pts || []; this.active = Math.min(saved.active || 0, this.pts.length);
    this.wrap = document.getElementById('mapWrap');
    const start = o.start();
    const map = this.map = L.map('map', { zoomControl: true, zoomSnap: 0.5, preferCanvas: true, worldCopyJump: false }).setView([start.lat, start.lon], 12);
    this.layers = [
      ['Mapa', L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19, attribution: '&copy; OpenStreetMap' })],
      ['Satélite', L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', { maxZoom: 19, attribution: 'Esri, Maxar' })],
    ];
    this.layerIdx = 0; this.layers[0][1].addTo(map);
    this.route = L.polyline([], { color: '#ff4dd2', weight: 4, opacity: 0.9 }).addTo(map);
    this.leg = L.polyline([], { color: '#ff4dd2', weight: 3, opacity: 0.8, dashArray: '6 8' }).addTo(map);
    this.apLayer = L.layerGroup().addTo(map);
    this.wpLayer = L.layerGroup().addTo(map);
    this.markLayer = L.layerGroup().addTo(map);
    this.planeMk = L.marker([start.lat, start.lon], { icon: L.divIcon({ className: 'mapPlane', html: PLANE_SVG, iconSize: [30, 30], iconAnchor: [15, 15] }), interactive: false, keyboard: false, zIndexOffset: 1000 });
    map.on('click', (e) => { if (this.full) this.add({ lat: e.latlng.lat, lon: e.latlng.lng }); });
    const $ = (id) => document.getElementById(id);
    $('mapClose').onclick = () => this.close();
    $('mapClear').onclick = () => { this.pts = []; this.active = 0; this.changed(); };
    $('mapUndo').onclick = () => { this.pts.pop(); this.active = Math.min(this.active, this.pts.length); this.changed(); };
    $('mapLayer').onclick = () => {
      map.removeLayer(this.layers[this.layerIdx][1]);
      this.layerIdx = (this.layerIdx + 1) % this.layers.length;
      this.layers[this.layerIdx][1].addTo(map).bringToBack();
      $('mapLayer').textContent = this.layers[(this.layerIdx + 1) % this.layers.length][0];
    };
    $('mapLayer').textContent = this.layers[1][0];
    $('mapCenter').onclick = () => map.setView(this.plane ? [this.plane.lat, this.plane.lon] : [o.start().lat, o.start().lon], Math.max(map.getZoom(), 12));
    this.refreshAirports();
    this.setMode('hidden');
    this.changed(true);
  },

  setMode(m) {
    this.wrap.dataset.mode = m;
    this.map.invalidateSize();
    requestAnimationFrame(() => this.map.invalidateSize());
  },

  showMini(on) {
    this.mini = on;
    if (!this.full) this.setMode(on ? 'mini' : 'hidden');
  },

  isOpen() { return this.full; },

  setMarks(list) {
    this.markLayer.clearLayers();
    for (const m of list) L.circleMarker([m.lat, m.lon], { radius: 10, color: '#ff4d4d', fillColor: '#ff4d4d', fillOpacity: 0.35, weight: 3, interactive: false }).bindTooltip(m.label, { permanent: true, direction: 'top' }).addTo(this.markLayer);
  },

  open() {
    if (this.full) return;
    this.full = true;
    this.setMode('full');
    this.refreshAirports();
    const c = this.plane || this.o.start();
    this.map.setView([c.lat, c.lon], 12, { animate: false });
    this.render();
    this.o.onOpen?.();
  },

  close() {
    if (!this.full) return;
    this.full = false;
    this.setMode(this.o.flying() && this.mini ? 'mini' : 'hidden');
    this.o.onClose?.();
  },

  toggle() { this.full ? this.close() : this.open(); },

  zoom(d) { this.miniZoom = Math.min(15, Math.max(8, this.miniZoom + d)); if (this.plane && !this.full) this.map.setView([this.plane.lat, this.plane.lon], this.miniZoom, { animate: false }); },

  refreshAirports() {
    this.apLayer.clearLayers();
    for (const a of this.o.airports()) {
      L.polyline([a.a, a.b], { color: '#000', weight: 8, opacity: 0.8, interactive: false }).addTo(this.apLayer);
      L.polyline([a.a, a.b], { color: '#fff', weight: 4, interactive: false }).addTo(this.apLayer);
      L.marker([a.lat, a.lon], { icon: L.divIcon({ className: 'mapAp', html: `<b>${a.short}</b>`, iconSize: [46, 20], iconAnchor: [23, 28] }), title: a.name })
        .on('click', () => this.add({ lat: a.lat, lon: a.lon, name: a.short }))
        .addTo(this.apLayer);
    }
  },

  add(p) { this.pts.push(p); this.changed(); },

  name(i) { return this.pts[i].name || `WP${i + 1}`; },

  changed(silent) {
    this.o.save({ pts: this.pts, active: this.active });
    this.wpLayer.clearLayers();
    this.pts.forEach((p, i) => {
      const mk = L.marker([p.lat, p.lon], { draggable: true, icon: L.divIcon({ className: 'mapWp', html: `<span>${i + 1}</span>`, iconSize: [26, 26], iconAnchor: [13, 13] }), title: this.name(i) });
      mk.on('dragend', () => { const ll = mk.getLatLng(); p.lat = ll.lat; p.lon = ll.lng; delete p.name; this.changed(); });
      mk.on('contextmenu', () => { this.pts.splice(i, 1); this.active = Math.min(this.active, this.pts.length); this.changed(); });
      mk.on('click', () => { this.active = i; this.changed(); });
      mk.addTo(this.wpLayer);
    });
    this.route.setLatLngs(this.pts.map((p) => [p.lat, p.lon]));
    this.updateLeg();
    this.render();
    if (!silent) this.o.onChange?.();
  },

  updateLeg() {
    this.leg.setLatLngs(this.plane && this.active < this.pts.length ? [[this.plane.lat, this.plane.lon], [this.pts[this.active].lat, this.pts[this.active].lon]] : []);
  },

  render() {
    const body = document.getElementById('mapRows');
    if (!body) return;
    const live = this.plane && this.plane.gs > 30, v = live ? this.plane.gs : this.o.speed(), start = this.plane || this.o.start();
    let prev = start, total = 0, t = 0;
    body.innerHTML = '';
    this.pts.forEach((p, i) => {
      const d = dist(prev, p), b = bearing(prev, p), s = d / v;
      total += d; t += s; prev = p;
      const tr = document.createElement('tr');
      tr.className = i === this.active ? 'on' : '';
      tr.innerHTML = `<td>${i + 1}</td><td>${this.name(i)}</td><td>${fmtD(d)}</td><td>${pad3(b)}</td><td>${fmtT(s)}</td><td><button title="Remover" aria-label="Remover ponto ${i + 1}">✕</button></td>`;
      tr.onclick = (e) => {
        if (e.target.tagName === 'BUTTON') { this.pts.splice(i, 1); this.active = Math.min(this.active, this.pts.length); }
        else { this.active = i; this.map.panTo([p.lat, p.lon]); }
        this.changed();
      };
      body.appendChild(tr);
    });
    document.getElementById('mapTotal').textContent = this.pts.length
      ? `Total ${fmtD(total)} · ${fmtT(t)} a ${Math.round(v * KT)} kt${live ? ' (sua velocidade atual)' : ' (cruzeiro do avião)'}`
      : 'Clique no mapa para adicionar pontos. Clique num aeródromo para usá-lo como ponto. Arraste um ponto para movê-lo; botão direito remove.';
    const n = document.getElementById('routeCount');
    if (n) n.textContent = this.pts.length ? `${this.pts.length} pontos · ${fmtD(total)}` : 'nenhuma rota';
  },

  update(p) {
    this.plane = p;
    this.planeMk.setLatLng([p.lat, p.lon]);
    if (!this.map.hasLayer(this.planeMk)) this.planeMk.addTo(this.map);
    const svg = this.planeMk.getElement()?.querySelector('svg');
    if (svg) svg.style.transform = `rotate(${p.hdg}deg)`;
    if (!this.full) this.map.setView([p.lat, p.lon], this.miniZoom, { animate: false });
    this.updateLeg();
    if (this.active >= this.pts.length) return { text: this.pts.length ? 'rota concluída' : '', brg: null };
    const wp = this.pts[this.active], d = dist(p, wp), b = bearing(p, wp);
    if (d < Math.max(900, p.gs * 8)) {
      this.active++;
      this.changed();
      return this.update(p);
    }
    const ete = d / Math.max(p.gs, 25);
    return { text: `→ ${this.name(this.active)} (${this.active + 1}/${this.pts.length}) · rumo ${pad3(b)} · ${fmtD(d)} · ${fmtT(ete)}`, brg: b };
  },

  reset() { this.active = 0; this.plane = null; if (this.map.hasLayer(this.planeMk)) this.map.removeLayer(this.planeMk); this.changed(); },
};

root.Nav = Nav;
})(typeof self !== 'undefined' ? self : globalThis);
