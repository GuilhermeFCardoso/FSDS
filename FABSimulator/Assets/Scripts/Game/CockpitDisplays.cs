using System;
using System.Collections.Generic;
using FABSim.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace FABSim.Game
{
    // Telas da cabine: as paginas sao desenhadas num atlas por UI Toolkit e mapeadas nas telas do modelo.
    public sealed class CockpitDisplays
    {
        public const int Pfd = 0, Nav = 1, Eng = 2, Sms = 3, Reserva = 4;
        const int AtlasW = 1800, AtlasH = 900, Altura = 420;
        const float Intervalo = 1f / 20;

        struct Seg { public Vector2 A, B; public Color C; public float W; }
        struct Circ { public Vector2 P; public float R; public Color C; public float W; public bool Fill; }

        sealed class Tela { public Rect R; public int Pagina; public bool Cicla; }

        static readonly Color Verde = new Color(0.3f, 1f, 0.5f), Ambar = new Color(1f, 0.75f, 0.2f), Vermelho = new Color(1f, 0.25f, 0.2f), Ciano = new Color(0.35f, 0.85f, 1f), Magenta = new Color(1f, 0.3f, 0.82f);
        static readonly Color Ceu = new Color(0.1f, 0.42f, 0.8f), Terra = new Color(0.45f, 0.28f, 0.12f), Fundo = new Color(0.02f, 0.03f, 0.04f);

        readonly FlightController F;
        readonly Combate K;
        readonly List<Tela> telas = new List<Tela>();
        readonly List<Seg> segs = new List<Seg>();
        readonly List<Circ> circs = new List<Circ>();
        readonly List<List<Vector2>> ground = new List<List<Vector2>>();
        readonly List<(Rect r, Color c)> fundos = new List<(Rect, Color)>();
        readonly List<Label> labels = new List<Label>();
        readonly RenderTexture rt;
        readonly VisualElement canvas;
        int labelUse;
        float espera;

        public CockpitDisplays(FlightController f, Combate k, PanelSettings modelo)
        {
            F = f; K = k;
            rt = new RenderTexture(AtlasW, AtlasH, 24, RenderTextureFormat.ARGB32) { name = "TelasCabine", useMipMap = true, autoGenerateMips = true, anisoLevel = 4 };
            var mat = new Material(Resources.Load<Material>("FabTela"));
            mat.SetTexture("_UnlitColorMap", rt);
            float x = 0, y = 0;
            foreach (var d in f.Rig.Telas)
            {
                float w = Mathf.Clamp(Altura * d.Aspect, 240, 760);
                if (x + w > AtlasW) { x = 0; y += Altura + 4; }
                var r = new Rect(x, y, w, Altura);
                x += w + 4;
                telas.Add(new Tela { R = r, Pagina = d.Pagina, Cicla = d.Cicla });
                var uv = new Vector2[d.Uv.Length];
                for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2((r.x + d.Uv[i].x * r.width) / AtlasW, 1 - (r.y + (1 - d.Uv[i].y) * r.height) / AtlasH);
                d.Malha.uv = uv;
                var mats = new Material[d.R.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                d.R.sharedMaterials = mats;
                d.R.enabled = true;
                d.R.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.themeStyleSheet = modelo.themeStyleSheet;
            ps.targetTexture = rt;
            ps.scaleMode = PanelScaleMode.ConstantPixelSize;
            ps.scale = 1;
            ps.clearColor = true;
            ps.colorClearValue = Fundo;
            var go = new GameObject("TelasCabine");
            // Sem pai na hierarquia: um UIDocument filho de outro herda o painel dele e desenharia na tela do jogador.
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            canvas = new VisualElement { pickingMode = PickingMode.Ignore };
            canvas.style.position = Position.Absolute;
            canvas.style.left = 0; canvas.style.top = 0; canvas.style.width = AtlasW; canvas.style.height = AtlasH;
            canvas.generateVisualContent += Paint;
            doc.rootVisualElement.Add(canvas);
        }

        public void ProximaPagina()
        {
            foreach (var t in telas)
                if (t.Cicla) t.Pagina = t.Pagina == Eng ? Sms : t.Pagina == Sms ? Nav : Eng;
        }

        public void Atualizar()
        {
            if (!F.Cockpit || telas.Count == 0) return;
            espera -= Time.unscaledDeltaTime;
            if (espera > 0) return;
            espera = Intervalo;
            segs.Clear(); circs.Clear(); ground.Clear(); fundos.Clear(); labelUse = 0;
            foreach (var t in telas)
            {
                if (t.Pagina == Pfd) DrawPfd(t.R);
                else if (t.Pagina == Nav) DrawNav(t.R);
                else if (t.Pagina == Eng) DrawEng(t.R);
                else if (t.Pagina == Reserva) DrawReserva(t.R);
                else DrawSms(t.R);
            }
            for (int i = labelUse; i < labels.Count; i++) labels[i].style.display = DisplayStyle.None;
            canvas.MarkDirtyRepaint();
        }

        void Paint(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            p.lineCap = LineCap.Butt;
            foreach (var (r, c) in fundos)
            {
                p.fillColor = c; p.BeginPath();
                p.MoveTo(new Vector2(r.xMin, r.yMin)); p.LineTo(new Vector2(r.xMax, r.yMin)); p.LineTo(new Vector2(r.xMax, r.yMax)); p.LineTo(new Vector2(r.xMin, r.yMax));
                p.ClosePath(); p.Fill();
            }
            p.fillColor = Terra;
            foreach (var poly in ground)
            {
                if (poly.Count < 3) continue;
                p.BeginPath(); p.MoveTo(poly[0]);
                for (int i = 1; i < poly.Count; i++) p.LineTo(poly[i]);
                p.ClosePath(); p.Fill();
            }
            foreach (var g in segs)
            {
                p.strokeColor = g.C; p.lineWidth = g.W;
                p.BeginPath(); p.MoveTo(g.A); p.LineTo(g.B); p.Stroke();
            }
            foreach (var c in circs)
            {
                p.BeginPath();
                p.Arc(c.P, c.R, new Angle(0), new Angle(360));
                p.ClosePath();
                if (c.Fill) { p.fillColor = c.C; p.Fill(); }
                else { p.strokeColor = c.C; p.lineWidth = c.W; p.Stroke(); }
            }
        }

        void Line(Vector2 a, Vector2 b, Color c, float w = 3) => segs.Add(new Seg { A = a, B = b, C = c, W = w });
        void Ring(Vector2 p, float r, Color c, float w = 3) => circs.Add(new Circ { P = p, R = r, C = c, W = w });
        void Dot(Vector2 p, float r, Color c) => circs.Add(new Circ { P = p, R = r, C = c, Fill = true });
        void Bloco(Rect r, Color c) => fundos.Add((r, c));

        void Txt(string s, Vector2 p, float size, Color c, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            if (labelUse >= labels.Count)
            {
                var n = new Label { pickingMode = PickingMode.Ignore };
                n.style.position = Position.Absolute; n.style.width = 400; n.style.height = 60;
                n.style.paddingLeft = n.style.paddingRight = n.style.paddingTop = n.style.paddingBottom = 0;
                n.style.marginLeft = n.style.marginRight = n.style.marginTop = n.style.marginBottom = 0;
                canvas.Add(n); labels.Add(n);
            }
            var l = labels[labelUse++];
            l.text = s;
            l.style.display = DisplayStyle.Flex;
            l.style.fontSize = size; l.style.color = c; l.style.unityTextAlign = anchor;
            l.style.left = anchor == TextAnchor.MiddleLeft ? p.x : anchor == TextAnchor.MiddleRight ? p.x - 400 : p.x - 200;
            l.style.top = p.y - 30;
        }

        static List<Vector2> ClipBelow(Rect r, Vector2 a, Vector2 n)
        {
            var src = new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) };
            var res = new List<Vector2>();
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = src[i], q = src[(i + 1) % 4];
                float dp = Vector2.Dot(p - a, n), dq = Vector2.Dot(q - a, n);
                if (dp >= 0) res.Add(p);
                if (dp >= 0 != dq >= 0) res.Add(p + (q - p) * (dp / (dp - dq)));
            }
            return res;
        }

        static bool Clip(Rect r, ref Vector2 a, ref Vector2 b)
        {
            float t0 = 0, t1 = 1;
            Vector2 d = b - a;
            bool Lim(float p, float q)
            {
                if (p == 0) return q >= 0;
                float t = q / p;
                if (p < 0) { if (t > t1) return false; if (t > t0) t0 = t; }
                else { if (t < t0) return false; if (t < t1) t1 = t; }
                return true;
            }
            if (!Lim(-d.x, a.x - r.xMin) || !Lim(d.x, r.xMax - a.x) || !Lim(-d.y, a.y - r.yMin) || !Lim(d.y, r.yMax - a.y)) return false;
            var a0 = a;
            a = a0 + d * t0; b = a0 + d * t1;
            return true;
        }

        void Mostrador(Vector2 c, float R, float frac, string nome, string valor)
        {
            Ring(c, R, Color.white, 4);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36 * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Sin(a), -Mathf.Cos(a));
                Line(c + d * R * 0.86f, c + d * R, Color.white, 3);
            }
            float n = frac * 2 * Mathf.PI;
            Line(c, c + new Vector2(Mathf.Sin(n), -Mathf.Cos(n)) * R * 0.8f, Ambar, 6);
            Txt(nome, c + new Vector2(0, -R * 0.42f), R * 0.2f, Color.white);
            Txt(valor, c + new Vector2(0, R * 0.46f), R * 0.26f, Verde);
        }

        void DrawReserva(Rect r)
        {
            Bloco(r, Color.black);
            float kt = (float)(F.S.Diag.Ias * 1.943844), ft = (float)(F.S.Z * 3.28084), R = Mathf.Min(r.height, r.width / 2) * 0.47f;
            Mostrador(new Vector2(r.x + r.width * 0.25f, r.center.y), R, kt / 500, "KT", $"{kt:0}");
            Mostrador(new Vector2(r.x + r.width * 0.75f, r.center.y), R, Mathf.Repeat(ft, 1000) / 1000, "FT", $"{ft:0}");
        }

        void DrawPfd(Rect r)
        {
            var s = F.S; var d = s.Diag;
            FlightModel.Euler(s.Q, out double phi, out double th, out _);
            Bloco(r, Ceu);
            var c = r.center;
            float ppd = r.height / 44, roll = (float)phi;
            var up = new Vector2(Mathf.Sin(roll), -Mathf.Cos(roll)); var along = new Vector2(Mathf.Cos(roll), Mathf.Sin(roll));
            var h0 = c - up * (float)(th * Mathf.Rad2Deg) * ppd;
            ground.Add(ClipBelow(r, h0, -up));
            var interno = new Rect(r.x + 90, r.y + 34, r.width - 180, r.height - 76);
            void L(Vector2 a, Vector2 b, Color col, float w = 3) { if (Clip(interno, ref a, ref b)) Line(a, b, col, w); }
            L(h0 - along * r.width, h0 + along * r.width, Color.white, 3);
            for (int g = -40; g <= 40; g += 10)
            {
                if (g == 0) continue;
                var q = h0 + up * (g * ppd);
                float w = 44;
                L(q - along * w, q + along * w, Color.white, 2.5f);
                if (interno.Contains(q + along * (w + 22))) Txt($"{Mathf.Abs(g)}", q + along * (w + 22), 18, Color.white);
            }
            Line(c + new Vector2(-70, 0), c + new Vector2(-24, 0), Ambar, 6); Line(c + new Vector2(24, 0), c + new Vector2(70, 0), Ambar, 6);
            Dot(c, 5, Ambar);
            Bloco(new Rect(r.x, r.y, 86, r.height), new Color(0, 0, 0, 0.62f));
            Bloco(new Rect(r.xMax - 86, r.y, 86, r.height), new Color(0, 0, 0, 0.62f));
            double kt = d.Ias * 1.943844, ft = s.Z * 3.28084;
            for (int i = -3; i <= 3; i++)
            {
                double v = Math.Round(kt / 20) * 20 + i * 20; float yv = c.y - (float)(v - kt) * 2.6f;
                if (v >= 0 && yv > r.y + 20 && yv < r.yMax - 20 && Mathf.Abs(yv - c.y) > 34) Txt($"{v:0}", new Vector2(r.x + 78, yv), 18, Color.white, TextAnchor.MiddleRight);
                double a = Math.Round(ft / 200) * 200 + i * 200; float ya = c.y - (float)(a - ft) * 0.26f;
                if (ya > r.y + 20 && ya < r.yMax - 20 && Mathf.Abs(ya - c.y) > 34) Txt($"{a:0}", new Vector2(r.xMax - 78, ya), 16, Color.white, TextAnchor.MiddleLeft);
            }
            Bloco(new Rect(r.x, c.y - 21, 86, 42), Color.black);
            Bloco(new Rect(r.xMax - 92, c.y - 21, 92, 42), Color.black);
            Txt($"{kt:0}", new Vector2(r.x + 78, c.y), 30, Verde, TextAnchor.MiddleRight);
            Txt($"{ft:0}", new Vector2(r.xMax - 88, c.y), 25, Verde, TextAnchor.MiddleLeft);
            Txt($"M {d.Mach:0.00}", new Vector2(r.x + 6, r.yMax - 22), 20, Verde, TextAnchor.MiddleLeft);
            Txt($"{d.Vsi:+0;-0;0} ft/min", new Vector2(r.xMax - 6, r.yMax - 22), 18, Verde, TextAnchor.MiddleRight);
            Bloco(new Rect(c.x - 62, r.y, 124, 36), Color.black);
            Txt($"{d.Hdg:000}°", new Vector2(c.x, r.y + 18), 28, Color.white);
            Txt($"G {d.Nz:0.0}   AoA {d.Alpha * Mathf.Rad2Deg:0}°", new Vector2(c.x, r.yMax - 22), 19, d.StallWarn ? Vermelho : Color.white);
            if (F.AutoPilot) Txt("A/P", new Vector2(c.x, r.y + 56), 22, Verde);
        }

        void DrawNav(Rect r)
        {
            var s = F.S; var d = s.Diag; var sess = K.Sess;
            var c = new Vector2(r.center.x, r.y + r.height * 0.62f);
            float R = r.height * 0.5f;
            const double alcance = 60000;
            float hdg = (float)(d.Hdg * Mathf.Deg2Rad);
            Ring(c, R, new Color(1, 1, 1, 0.55f), 2); Ring(c, R / 2, new Color(1, 1, 1, 0.22f), 1.5f);
            for (int g = 0; g < 360; g += 30)
            {
                float a = g * Mathf.Deg2Rad - hdg;
                var dirv = new Vector2(Mathf.Sin(a), -Mathf.Cos(a));
                var pa = c + dirv * R; var pb = c + dirv * (R - 14);
                if (!Clip(r, ref pa, ref pb)) continue;
                Line(pa, pb, Color.white, 2.5f);
                var pt = c + dirv * (R - 32);
                if (r.Contains(pt)) Txt(g == 0 ? "N" : g == 90 ? "E" : g == 180 ? "S" : g == 270 ? "W" : $"{g / 10}", pt, 19, Color.white);
            }
            Vector2 Plot(V3 p)
            {
                float dx = (float)(p.X - sess.Pl.Pos.X), dy = (float)(p.Y - sess.Pl.Pos.Y);
                float e = dx * Mathf.Cos(hdg) - dy * Mathf.Sin(hdg), n = dx * Mathf.Sin(hdg) + dy * Mathf.Cos(hdg);
                return c + new Vector2(e, -n) * (R / (float)alcance);
            }
            foreach (var b in BaseDb.All)
            {
                var q = Plot(K.ToEnu(b.lat * Mathf.Deg2Rad, b.lon * Mathf.Deg2Rad, 0));
                if ((q - c).sqrMagnitude > R * R || !r.Contains(q)) continue;
                Ring(q, 7, Ciano, 2.5f); Txt(b.icao, q + new Vector2(0, -18), 16, Ciano);
            }
            foreach (var a in sess.Air)
            {
                if (a.Dying > 0) continue;
                var q = Plot(a.Pos);
                if ((q - c).sqrMagnitude < R * R && r.Contains(q)) Dot(q, 6, a.Team == Team.Red ? Vermelho : Ciano);
            }
            if (K.Reab != null)
            {
                var q = Plot(K.Reab.Pos);
                if ((q - c).sqrMagnitude < R * R && r.Contains(q)) { Dot(q, 7, Verde); Txt("KC", q + new Vector2(0, -18), 16, Verde); }
            }
            var ant = c;
            for (int i = Rota.Ativo; i < Rota.Pontos.Count; i++)
            {
                var q = Plot(K.ToEnu(Rota.Pontos[i].Lat * Mathf.Deg2Rad, Rota.Pontos[i].Lon * Mathf.Deg2Rad, 0));
                Vector2 a = ant, b = q;
                if (Clip(r, ref a, ref b)) Line(a, b, Magenta, 3);
                if (r.Contains(q)) Dot(q, 6, Magenta);
                ant = q;
            }
            Line(c + new Vector2(0, -14), c + new Vector2(0, 14), Ambar, 4); Line(c + new Vector2(-11, 3), c + new Vector2(11, 3), Ambar, 4);
            Bloco(new Rect(r.x, r.y, r.width, 40), Color.black);
            Txt($"{d.Hdg:000}°", new Vector2(r.x + 8, r.y + 20), 24, Color.white, TextAnchor.MiddleLeft);
            Txt($"GS {d.Gs * 1.943844:0}", new Vector2(r.center.x, r.y + 20), 20, Verde);
            Txt($"{alcance / 1000:0} km", new Vector2(r.xMax - 8, r.y + 20), 18, Color.white, TextAnchor.MiddleRight);
            var wp = Rota.Atual;
            if (wp != null)
            {
                double la = s.Lat * Mathf.Rad2Deg, lo = s.Lon * Mathf.Rad2Deg, dist = Rota.Dist(la, lo, wp.Lat, wp.Lon);
                Txt($"{Rota.Nome(Rota.Ativo)}  {Rota.Rumo(la, lo, wp.Lat, wp.Lon):000}°  {Rota.Km(dist)}  {Rota.Tempo(dist / Math.Max(d.Gs, 25))}", new Vector2(r.center.x, r.yMax - 18), 19, Magenta);
            }
        }

        void Barra(Rect r, float y, string nome, float frac, string valor, Color c)
        {
            Txt(nome, new Vector2(r.x + 14, y), 20, Color.white, TextAnchor.MiddleLeft);
            float x0 = r.x + 100, x1 = r.xMax - 110;
            Line(new Vector2(x0, y), new Vector2(x1, y), new Color(1, 1, 1, 0.16f), 17);
            Line(new Vector2(x0, y), new Vector2(x0 + (x1 - x0) * Mathf.Clamp01(frac), y), c, 17);
            Txt(valor, new Vector2(r.xMax - 12, y), 20, c, TextAnchor.MiddleRight);
        }

        void Luz(Vector2 p, string nome, bool on, Color c)
        {
            Bloco(new Rect(p.x, p.y, 106, 36), on ? c : new Color(1, 1, 1, 0.08f));
            Txt(nome, p + new Vector2(53, 18), 17, on ? Color.black : new Color(1, 1, 1, 0.5f));
        }

        void DrawEng(Rect r)
        {
            var s = F.S; var d = s.Diag; var ac = F.Ac;
            Txt("MOTOR E SISTEMAS", new Vector2(r.center.x, r.y + 22), 21, Ciano);
            float cap = (float)(ac.FuelMax + GameSession.ExtraFuel()), comb = (float)(s.Fuel / cap);
            Barra(r, r.y + 62, ac.Engine.NLabel ?? "N", (float)s.N, $"{d.NPct:0}%", Verde);
            Barra(r, r.y + 98, "ACEL", (float)F.Ctl.Throttle, $"{F.Ctl.Throttle * 100:0}%", Color.white);
            if (ac.Engine.Afterburner) Barra(r, r.y + 134, "PÓS", (float)s.Ab, s.Ab > 0.05 ? "LIG" : "DESL", Ambar);
            Barra(r, r.y + 170, "COMB", comb, $"{s.Fuel:0}", comb > 0.15f ? Verde : Vermelho);
            Txt($"{s.Flow * 3600:0} kg/h   autonomia {Rota.Tempo(s.Fuel / Math.Max(s.Flow, 0.01))}", new Vector2(r.center.x, r.y + 202), 19, Color.white);
            float y = r.y + 226, x = r.x + 12, dx = (r.width - 24) / 4;
            string trem = ac.GearFixed ? "TREM FIXO" : s.Gear > 0.99 ? "TREM BX" : s.Gear < 0.01 ? "TREM UP" : "TREM MOV";
            Luz(new Vector2(x, y), trem, s.Gear > 0.01, s.Gear > 0.99 ? Verde : Ambar);
            Luz(new Vector2(x + dx, y), $"FLAP {F.Ctl.FlapIdx}", F.Ctl.FlapIdx > 0, Verde);
            Luz(new Vector2(x + dx * 2, y), "AEROFREIO", s.Sb > 0.05, Ambar);
            Luz(new Vector2(x + dx * 3, y), "ESTAC", F.Ctl.Parking, Ambar);
            y += 44;
            Luz(new Vector2(x, y), "A/P", F.AutoPilot, Verde);
            Luz(new Vector2(x + dx, y), "SONDA", F.ProbeOut, Ambar);
            Luz(new Vector2(x + dx * 2, y), "FAROL", F.LandingLight, Verde);
            Luz(new Vector2(x + dx * 3, y), F.MasterArm ? "ARMADO" : "SEGURO", F.MasterArm, Vermelho);
            y += 44;
            Luz(new Vector2(x, y), "ESTOL", d.StallWarn, Vermelho);
            Luz(new Vector2(x + dx, y), "G", d.Nz > ac.NzLimitHigh * 0.9, Vermelho);
            Txt($"Massa {d.Mass:0} kg", new Vector2(r.xMax - 12, y + 18), 19, Color.white, TextAnchor.MiddleRight);
            y += 44;
            Luz(new Vector2(x, y), "MOTOR CORT", F.Ctl.EngineCut, Vermelho);
            Luz(new Vector2(x + dx, y), "REVERSO", F.Ctl.Reverse, Ambar);
        }

        void DrawSms(Rect r)
        {
            var sess = K.Sess; var w = sess.W;
            if (!F.TemArmamento)
            {
                Txt("ROTA", new Vector2(r.center.x, r.y + 24), 22, Ciano);
                double la = F.S.Lat * Mathf.Rad2Deg, lo = F.S.Lon * Mathf.Rad2Deg;
                float ly = r.y + 66;
                for (int i = 0; i < Rota.Pontos.Count && i < 9; i++)
                {
                    var p = Rota.Pontos[i];
                    Txt($"{i + 1} {Rota.Nome(i)}   {Rota.Rumo(la, lo, p.Lat, p.Lon):000}°   {Rota.Km(Rota.Dist(la, lo, p.Lat, p.Lon))}", new Vector2(r.x + 14, ly), 20, i == Rota.Ativo ? Magenta : Color.white, TextAnchor.MiddleLeft);
                    ly += 34;
                }
                if (Rota.Pontos.Count == 0) Txt("Sem rota: abra o mapa", new Vector2(r.center.x, ly), 20, Color.white);
                return;
            }
            Txt("ARMAMENTO", new Vector2(r.center.x, r.y + 24), 22, Ciano);
            Txt(F.MasterArm ? "ARMADO" : "SEGURO", new Vector2(r.center.x, r.y + 60), 30, F.MasterArm ? Vermelho : Verde);
            var itens = new (string nome, WeaponKind k, int n)[] { ("CANHÃO", WeaponKind.Gun, w.Gun), ("MÍSSEIS", WeaponKind.Aam, w.Aam), ("BOMBAS", WeaponKind.Bomb, w.Bomb), ("FOGUETES", WeaponKind.Rkt, w.Rkt) };
            float y = r.y + 112;
            foreach (var (nome, k, n) in itens)
            {
                bool sel = sess.Sel == k;
                if (sel) Bloco(new Rect(r.x + 10, y - 19, r.width - 20, 38), new Color(0.3f, 1f, 0.5f, 0.2f));
                Txt(nome, new Vector2(r.x + 20, y), 23, n > 0 ? Color.white : new Color(1, 1, 1, 0.35f), TextAnchor.MiddleLeft);
                Txt($"{n}", new Vector2(r.xMax - 20, y), 25, sel ? Verde : Color.white, TextAnchor.MiddleRight);
                y += 44;
            }
            Txt($"CHAFF/FLARE  {w.Flares}", new Vector2(r.x + 20, y + 6), 21, Ambar, TextAnchor.MiddleLeft);
        }
    }
}
