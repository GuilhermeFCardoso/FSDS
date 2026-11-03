using System;
using System.Collections.Generic;
using FABSim.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace FABSim.Game
{
    [DefaultExecutionOrder(20)]
    public sealed class Hud : MonoBehaviour
    {
        struct Seg { public Vector2 A, B; public Color C; public float W; }
        struct Circ { public Vector2 P; public float R, W; public Color C; public bool Fill; }

        public FlightController F;
        public Combate K;

        readonly List<Seg> segs = new List<Seg>();
        readonly List<Circ> circs = new List<Circ>();
        readonly List<Label> labels = new List<Label>();
        int labelUse;
        VisualElement root, canvas, radioBox, toastBox, overlay, loading, hurt, overlayList;
        Label radioFrom, radioText, loadingText, overlayTitle, overlayBody;
        readonly List<Label> toastLabels = new List<Label>();
        float W, H;
        MapaTatico mapa;
        CockpitDisplays telas;
        public MapaTatico Mapa => mapa;
        public CockpitDisplays Telas => telas;
        readonly List<(Vector2 p, Vector2 s, Color c)> blocos = new List<(Vector2, Vector2, Color)>();
        int menuIdx;
        readonly List<(string text, Action act)> menu = new List<(string, Action)>();
        bool debriefShown;
        float debriefDelay = 2.5f;

        static readonly Color G = new Color(0.35f, 1f, 0.55f, 0.95f);
        static readonly Color GD = new Color(0.35f, 1f, 0.55f, 0.55f);
        static readonly Color RD = new Color(1f, 0.28f, 0.22f, 1f);
        static readonly Color AM = new Color(1f, 0.78f, 0.25f, 1f);
        static readonly Color BL = new Color(0.45f, 0.75f, 1f, 1f);
        static readonly Color RT = new Color(1f, 0.3f, 0.82f, 1f);

        void Start()
        {
            F = GetComponent<FlightController>();
            K = GetComponent<Combate>();
            root = GetComponent<UIDocument>().rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.left = root.style.right = root.style.top = root.style.bottom = 0;
            root.pickingMode = PickingMode.Ignore;

            hurt = Abs(root); Fill(hurt); hurt.style.backgroundColor = new Color(0.8f, 0.05f, 0, 0);

            canvas = new VisualElement { pickingMode = PickingMode.Ignore };
            Fill(canvas); canvas.style.position = Position.Absolute;
            canvas.generateVisualContent += Draw;
            root.Add(canvas);

            radioBox = Abs(root);
            radioBox.style.left = 24; radioBox.style.top = 20; radioBox.style.width = 560;
            radioBox.style.backgroundColor = new Color(0.02f, 0.05f, 0.07f, 0.72f);
            Pad(radioBox, 12); Corner(radioBox, 6);
            radioBox.style.borderLeftWidth = 3; radioBox.style.borderLeftColor = BL;
            radioFrom = new Label { pickingMode = PickingMode.Ignore }; radioFrom.style.color = BL; radioFrom.style.fontSize = 15; radioFrom.style.unityFontStyleAndWeight = FontStyle.Bold;
            radioText = new Label { pickingMode = PickingMode.Ignore }; radioText.style.color = Color.white; radioText.style.fontSize = 19; radioText.style.whiteSpace = WhiteSpace.Normal;
            radioBox.Add(radioFrom); radioBox.Add(radioText);

            toastBox = Abs(root);
            toastBox.style.right = 24; toastBox.style.top = 20; toastBox.style.width = 460; toastBox.style.alignItems = Align.FlexEnd;
            for (int i = 0; i < 5; i++)
            {
                var l = new Label { pickingMode = PickingMode.Ignore };
                l.style.color = AM; l.style.fontSize = 17; l.style.unityTextAlign = TextAnchor.MiddleRight;
                toastBox.Add(l); toastLabels.Add(l);
            }

            loading = Abs(root); Fill(loading);
            loading.style.backgroundColor = new Color(0.02f, 0.04f, 0.06f, 1);
            loading.style.alignItems = Align.Center; loading.style.justifyContent = Justify.Center;
            var t = new Label("FAB SIMULATOR") { pickingMode = PickingMode.Ignore }; t.style.color = Color.white; t.style.fontSize = 58; t.style.unityFontStyleAndWeight = FontStyle.Bold;
            var st = new Label("Defendendo a Soberania Nacional") { pickingMode = PickingMode.Ignore }; st.style.color = BL; st.style.fontSize = 22; st.style.marginBottom = 40;
            loadingText = new Label { pickingMode = PickingMode.Ignore }; loadingText.style.color = new Color(.8f, .85f, .9f); loadingText.style.fontSize = 20;
            loading.Add(t); loading.Add(st); loading.Add(loadingText);

            overlay = Abs(root); Fill(overlay);
            overlay.style.backgroundColor = new Color(0.01f, 0.03f, 0.05f, 0.82f);
            overlay.style.alignItems = Align.Center; overlay.style.justifyContent = Justify.Center;
            overlayTitle = new Label(); overlayTitle.style.color = Color.white; overlayTitle.style.fontSize = 44; overlayTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            overlayBody = new Label(); overlayBody.style.color = new Color(.85f, .9f, .95f); overlayBody.style.fontSize = 21; overlayBody.style.whiteSpace = WhiteSpace.Normal; overlayBody.style.maxWidth = 900; overlayBody.style.marginTop = 12; overlayBody.style.marginBottom = 24; overlayBody.style.unityTextAlign = TextAnchor.MiddleCenter;
            overlayList = new VisualElement();
            overlay.Add(overlayTitle); overlay.Add(overlayBody); overlay.Add(overlayList);
            overlay.style.display = DisplayStyle.None;

            mapa = new MapaTatico(root, F) { PontoEm = BaseEm };
            if (F.Rig != null) telas = new CockpitDisplays(F, K, GetComponent<UIDocument>().panelSettings);
        }

        static VisualElement Abs(VisualElement parent) { var v = new VisualElement { pickingMode = PickingMode.Ignore }; v.style.position = Position.Absolute; parent.Add(v); return v; }
        static void Fill(VisualElement v) { v.style.left = 0; v.style.right = 0; v.style.top = 0; v.style.bottom = 0; }
        static void Pad(VisualElement v, float p) { v.style.paddingLeft = p; v.style.paddingRight = p; v.style.paddingTop = p; v.style.paddingBottom = p; }
        static void Corner(VisualElement v, float r) { v.style.borderTopLeftRadius = r; v.style.borderTopRightRadius = r; v.style.borderBottomLeftRadius = r; v.style.borderBottomRightRadius = r; }

        void Line(Vector2 a, Vector2 b, Color c, float w = 1.6f) => segs.Add(new Seg { A = a, B = b, C = c, W = w });
        void Ring(Vector2 p, float r, Color c, float w = 1.6f) => circs.Add(new Circ { P = p, R = r, C = c, W = w });
        void Dot(Vector2 p, float r, Color c) => circs.Add(new Circ { P = p, R = r, C = c, Fill = true });
        void Box(Vector2 p, float hw, float hh, Color c, float w = 1.6f)
        {
            var a = p + new Vector2(-hw, -hh); var b = p + new Vector2(hw, -hh); var d = p + new Vector2(hw, hh); var e = p + new Vector2(-hw, hh);
            Line(a, b, c, w); Line(b, d, c, w); Line(d, e, c, w); Line(e, a, c, w);
        }

        void Txt(string s, Vector2 p, float size, Color c, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            if (labelUse >= labels.Count)
            {
                var n = new Label { pickingMode = PickingMode.Ignore };
                n.style.position = Position.Absolute; n.style.width = 520; n.style.height = 34;
                n.style.paddingLeft = n.style.paddingRight = n.style.paddingTop = n.style.paddingBottom = 0;
                n.style.marginLeft = n.style.marginRight = n.style.marginTop = n.style.marginBottom = 0;
                canvas.Add(n); labels.Add(n);
            }
            var l = labels[labelUse++];
            l.text = s;
            l.style.display = DisplayStyle.Flex;
            l.style.fontSize = size; l.style.color = c; l.style.unityTextAlign = anchor;
            l.style.left = anchor == TextAnchor.MiddleLeft ? p.x : anchor == TextAnchor.MiddleRight ? p.x - 520 : p.x - 260;
            l.style.top = p.y - 17;
        }

        bool Proj(Vector3 world, out Vector2 p)
        {
            var v = F.cam.WorldToViewportPoint(world);
            p = new Vector2(v.x * W, (1 - v.y) * H);
            return v.z > 0;
        }

        Vector3 Eye => F.cam.transform.position;
        bool ProjDir(Vector3 dir, out Vector2 p) => Proj(Eye + dir * 2000f, out p);

        void LateUpdate()
        {
            if (root == null) return;
            W = root.resolvedStyle.width; H = root.resolvedStyle.height;
            if (float.IsNaN(W) || W <= 0) return;
            segs.Clear(); circs.Clear(); blocos.Clear(); labelUse = 0;
            bool ready = F.Ready && K != null && K.Sess != null;
            loading.style.display = ready ? DisplayStyle.None : DisplayStyle.Flex;
            if (!ready) { loadingText.text = F.Status; Hide(); return; }
            var s = F.S;
            if (F.Input.NextPage) telas?.ProximaPagina();
            telas?.Atualizar();
            if (F.Input.ToggleMap && s.Crashed.Length == 0 && !K.Encerrada && overlay.style.display == DisplayStyle.None) mapa.Alternar();
            Rota.Avancar(s.Lat * 57.29578, s.Lon * 57.29578, Math.Sqrt(s.V.X * s.V.X + s.V.Y * s.V.Y));
            if (mapa.Aberto)
            {
                mapa.Atualizar(W, H);
                DrawMapa();
            }
            else if (s.Crashed.Length == 0)
            {
                // Com zoom nos instrumentos, as telas da cabine substituem o HUD sobreposto.
                if (!F.Cockpit || F.Zoom < 0.35f)
                {
                    DrawHudFlight();
                    DrawWeapons();
                    DrawContacts();
                    DrawStatus();
                    DrawScope();
                    DrawNav();
                }
            }
            DrawRadio();
            UpdateOverlay();
            hurt.style.backgroundColor = new Color(0.8f, 0.05f, 0, Mathf.Clamp01(K.HurtFlash) * 0.35f);
            Hide();
            canvas.MarkDirtyRepaint();
        }

        void Hide() { for (int i = labelUse; i < labels.Count; i++) labels[i].style.display = DisplayStyle.None; }

        void Draw(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            p.lineCap = LineCap.Round;
            foreach (var b in blocos)
            {
                p.fillColor = b.c; p.BeginPath();
                p.MoveTo(b.p); p.LineTo(b.p + new Vector2(b.s.x, 0)); p.LineTo(b.p + b.s); p.LineTo(b.p + new Vector2(0, b.s.y));
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

        void DrawHudFlight()
        {
            var s = F.S; var d = s.Diag;
            var C = new Vector2(W / 2, H / 2);
            FlightModel.Euler(s.Q, out double phi, out double th, out double psi);
            float hdg = (float)psi;

            // Escada de arfagem conforme com o horizonte real
            var hRight = new Vector3(Mathf.Cos(hdg), 0, -Mathf.Sin(hdg));
            for (int deg = -60; deg <= 60; deg += 5)
            {
                float a = deg * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(hdg) * Mathf.Cos(a), Mathf.Sin(a), Mathf.Cos(hdg) * Mathf.Cos(a));
                float span = deg == 0 ? 0.22f : 0.085f, gap = deg == 0 ? 0.03f : 0.03f;
                if (!ProjDir(dir, out var mid)) continue;
                if ((mid - C).sqrMagnitude > 340f * 340f) continue;
                ProjDir((dir + hRight * span).normalized, out var r1);
                ProjDir((dir + hRight * gap).normalized, out var r0);
                ProjDir((dir - hRight * span).normalized, out var l1);
                ProjDir((dir - hRight * gap).normalized, out var l0);
                Color col = deg == 0 ? G : GD;
                Line(l1, l0, col); Line(r0, r1, col);
                if (deg != 0)
                {
                    var tick = deg > 0 ? new Vector2(0, 8) : new Vector2(0, -8);
                    Line(l1, l1 + tick, col); Line(r1, r1 + tick, col);
                    Txt(Math.Abs(deg).ToString(), r1 + new Vector2(20, 0), 14, col);
                }
            }

            // Vetor de velocidade (FPM)
            var v = new Vector3((float)s.V.Y, (float)-s.V.Z, (float)s.V.X);
            if (v.magnitude > 25 && ProjDir(v.normalized, out var fpm))
            {
                Ring(fpm, 8, G); Line(fpm + new Vector2(-20, 0), fpm + new Vector2(-8, 0), G); Line(fpm + new Vector2(8, 0), fpm + new Vector2(20, 0), G); Line(fpm + new Vector2(0, -8), fpm + new Vector2(0, -16), G);
            }
            if (ProjDir(F.model.forward, out var bore)) { Line(bore + new Vector2(-14, 0), bore + new Vector2(-5, 0), G); Line(bore + new Vector2(5, 0), bore + new Vector2(14, 0), G); Line(bore + new Vector2(0, 5), bore + new Vector2(0, 10), G); }

            // Fitas
            float tx = 300, ty = 170;
            DrawTape(C + new Vector2(-tx, 0), ty, d.Ias, 10, 50, true, "IAS");
            DrawTape(C + new Vector2(tx, 0), ty, s.Z * FlightModel.FT, 100, 500, false, "ALT");
            DrawHeading(C + new Vector2(0, -ty - 60), d.Hdg);
            Txt($"M {d.Mach:0.00}", C + new Vector2(-tx - 40, ty + 26), 17, G);
            Txt($"G {d.Nz:0.0}", C + new Vector2(-tx - 40, ty + 48), 17, d.Nz > F.Ac.NzLimitHigh * 0.9 ? AM : G);
            Txt($"AoA {d.Alpha * 57.3:0}", C + new Vector2(-tx - 40, ty + 70), 15, GD);
            Txt($"RA {d.Agl * FlightModel.FT:0}", C + new Vector2(tx + 40, ty + 26), 17, d.Agl < 300 ? AM : G);
            Txt($"VS {d.Vsi:0}", C + new Vector2(tx + 40, ty + 48), 15, GD);
            Txt($"{s.Throttle * 100:0}%{(s.Ab > 0.05 ? " A/B" : "")}", C + new Vector2(tx + 40, ty + 70), 15, s.Ab > 0.05 ? AM : GD);

            if (d.Agl < 500 && d.Vsi < -2000 && !s.Contact && !F.Ctl.GearDown) Txt("PULL UP", C + new Vector2(0, 80), 34, RD);
            if (s.Buf > 0.15) Txt("STALL", C + new Vector2(0, 120), 30, RD);
        }

        void DrawTape(Vector2 c, float h, double value, double minor, double major, bool left, string title)
        {
            float pxPer = (float)(h * 2 / (major * 4));
            double start = Math.Floor((value - major * 2) / minor) * minor;
            float dir = left ? 1 : -1;
            Line(c + new Vector2(0, -h), c + new Vector2(0, h), GD);
            for (double x = start; x <= value + major * 2; x += minor)
            {
                if (x < 0) continue;
                float y = (float)(value - x) * pxPer;
                if (Mathf.Abs(y) > h) continue;
                bool big = Math.Abs(x % major) < 0.01;
                Line(c + new Vector2(0, y), c + new Vector2(dir * (big ? 12 : 6), y), GD);
                if (big && Mathf.Abs(y) < h - 8 && Mathf.Abs(y) > 20) Txt(x.ToString("0"), c + new Vector2(dir * 18, y), 14, GD, left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            }
            var bx = c + new Vector2(-dir * 45, 0);
            Box(bx, 42, 15, G);
            Txt(value.ToString("0"), bx, 21, G);
            Txt(title, c + new Vector2(-dir * 45, -h - 14), 13, GD);
        }

        void DrawHeading(Vector2 c, double hdg)
        {
            float span = 200, pxPerDeg = 6;
            Line(c + new Vector2(-span, 12), c + new Vector2(span, 12), GD);
            for (int a = (int)hdg - 40; a <= hdg + 40; a++)
            {
                if (a % 5 != 0) continue;
                float x = (float)(a - hdg) * pxPerDeg;
                if (Mathf.Abs(x) > span) continue;
                bool big = a % 10 == 0;
                Line(c + new Vector2(x, 12), c + new Vector2(x, big ? 2 : 7), GD);
                if (big && Mathf.Abs(x) > 28)
                {
                    int w = ((a % 360) + 360) % 360;
                    Txt(w == 0 ? "N" : w == 90 ? "E" : w == 180 ? "S" : w == 270 ? "O" : (w / 10).ToString("00"), c + new Vector2(x, -12), 14, GD);
                }
            }
            Box(c + new Vector2(0, -10), 26, 13, G);
            Txt((((int)Math.Round(hdg) % 360 + 360) % 360).ToString("000"), c + new Vector2(0, -10), 19, G);
        }

        string WeaponName(WeaponKind k)
        {
            switch (k)
            {
                case WeaponKind.Gun: return "CAN";
                case WeaponKind.Aam: return "AAM";
                case WeaponKind.Bomb: return "BOM";
                default: return "FOG";
            }
        }

        int Count(WeaponKind k)
        {
            var w = K.Sess.W;
            return k == WeaponKind.Gun ? w.Gun : k == WeaponKind.Aam ? w.Aam : k == WeaponKind.Bomb ? w.Bomb : w.Rkt;
        }

        void DrawWeapons()
        {
            var s = K.Sess;
            var C = new Vector2(W / 2, H / 2);
            float y = H - 86;
            float x0 = W / 2 - s.Avail.Count * 50;
            for (int i = 0; i < s.Avail.Count; i++)
            {
                var k = s.Avail[i];
                bool sel = k == s.Sel;
                var p = new Vector2(x0 + i * 100 + 50, y);
                if (sel) Box(p, 46, 22, G, 2);
                Txt($"{WeaponName(k)} {Count(k)}", p, sel ? 20 : 17, sel ? G : GD);
            }
            Txt($"FLR {s.W.Flares}", new Vector2(W / 2 + s.Avail.Count * 50 + 62, y), 17, s.W.Flares > 0 ? GD : RD);

            if (s.Tgt != null && s.Tgt.Valid)
            {
                var tp = K.World(s.Tgt.Pos);
                double range = Cm.Dist(s.Tgt.Pos, s.Pl.Pos);
                if (Proj(tp, out var tgt) && tgt.x > 0 && tgt.x < W && tgt.y > 0 && tgt.y < H)
                {
                    Box(tgt, 18, 18, s.Tgt is AirUnit ? AM : G, 2);
                    Txt($"{s.Tgt.Name}  {range / 1000:0.0} km", tgt + new Vector2(0, -32), 15, AM);
                }
                else DrawOffscreen(tp, C);

                if (s.Sel == WeaponKind.Gun && s.GunLead(out var ld, out _, out double r) && r < 2500 && ProjDir(Combate.Dir(ld), out var lead))
                {
                    Ring(lead, 13, G, 2); Dot(lead, 2, G);
                    Txt($"{r:0} m", lead + new Vector2(38, 0), 14, G);
                }
                if (s.Sel == WeaponKind.Aam)
                {
                    if (Proj(tp, out var seek))
                    {
                        float k = Mathf.Clamp01((float)s.LockT / 1.1f);
                        var p = Vector2.Lerp(C, seek, k);
                        Ring(p, s.LockOk ? 26 : 20, s.LockOk ? RD : G, s.LockOk ? 3 : 1.6f);
                        if (s.LockOk) Txt("DISPARE", p + new Vector2(0, 44), 22, RD);
                    }
                }
            }
            if (s.Sel == WeaponKind.Gun && ProjDir(F.model.forward, out var gc)) Ring(gc, 44, GD, 1);
            if (s.Sel == WeaponKind.Aam && (s.Tgt == null || !s.Tgt.Valid)) { Ring(C, 62, GD, 1); Txt("SEM ALVO", C + new Vector2(0, 84), 15, GD); }

            DrawSight(s);

            if (s.WarnMsl != null) Txt($"MÍSSIL {s.WarnD / 1000:0.0} km", C + new Vector2(0, -130), 36, Time.unscaledTime % 0.4f < 0.2f ? RD : AM);
        }

        void DrawOffscreen(Vector3 world, Vector2 C)
        {
            var local = F.cam.transform.InverseTransformPoint(world);
            var d = new Vector2(local.x, -local.y).normalized;
            var p = C + d * Mathf.Min(W, H) * 0.42f;
            Line(p, p - d * 24 + new Vector2(-d.y, d.x) * 10, AM, 2); Line(p, p - d * 24 - new Vector2(-d.y, d.x) * 10, AM, 2);
        }

        void DrawSight(CombatSession s)
        {
            var sol = s.Sol;
            if (sol == null) return;
            var C = new Vector2(W / 2, H / 2);
            string mode = sol.Kind == WeaponKind.Rkt ? "CCIP FOG" : sol.Ccrp ? "CCRP" : "CCIP";
            Txt(mode, C + new Vector2(-300, 215), 22, G);
            var v = new Vector3((float)F.S.V.Y, (float)-F.S.V.Z, (float)F.S.V.X);
            Vector2 fpm = C;
            if (v.magnitude > 1) ProjDir(v.normalized, out fpm);

            if (sol.Ccrp && sol.Target != null)
            {
                // Linha de direção azimutal com a marca de lançamento descendo ate o FPM
                float steer = (float)sol.Steer * 57.3f * 6f;
                var top = new Vector2(fpm.x + steer, fpm.y - 170);
                Line(top, new Vector2(fpm.x + steer, fpm.y + 120), Mathf.Abs(steer) < 16 ? G : AM, 2);
                float cue = Mathf.Clamp((float)sol.Ttr * 16f, -40, 170);
                Line(new Vector2(fpm.x - 30, fpm.y - cue), new Vector2(fpm.x + 30, fpm.y - cue), G, 3);
                Txt($"{sol.Ttr:0} s", new Vector2(fpm.x + 62, fpm.y - cue), 18, G);
                Txt($"{sol.Dist / 1000:0.0} km", C + new Vector2(-300, 240), 16, G);
                if (s.Pickle) Txt("MANTENHA - LANÇAMENTO AUTOMÁTICO", C + new Vector2(0, 150), 20, AM);
                else Txt("SEGURE O DISPARO", C + new Vector2(0, 150), 16, GD);
                return;
            }
            if (!sol.HasImpact) { Txt("FORA DE ENVELOPE", C + new Vector2(0, 150), 18, AM); return; }
            if (!Proj(K.World(sol.Impact), out var imp)) { Txt("PIQUE PARA A MIRA", C + new Vector2(0, 150), 18, AM); return; }
            Color col = sol.OnTarget ? RD : G;
            Line(fpm, imp, GD);
            if (sol.Kind == WeaponKind.Rkt)
            {
                Ring(imp, 16, col, 2); Dot(imp, 2, col);
                float k = Mathf.Clamp01((float)((sol.Range - CombatSession.RktMin) / (CombatSession.RktMax - CombatSession.RktMin)));
                Line(imp + new Vector2(24, 16), imp + new Vector2(24, 16 - 32 * k), col, 3);
                Txt(sol.TooClose ? "MUITO PERTO" : sol.InRange ? $"{sol.Range:0} m" : "LONGE", imp + new Vector2(0, 36), 15, sol.InRange ? G : AM);
            }
            else
            {
                Ring(imp, 22, col, 2); Dot(imp, 2.5f, col);
                Line(imp + new Vector2(-30, 0), imp + new Vector2(-22, 0), col, 2); Line(imp + new Vector2(22, 0), imp + new Vector2(30, 0), col, 2);
                Txt($"{sol.Tof:0.0} s", imp + new Vector2(0, 38), 15, G);
            }
            if (sol.OnTarget) Txt("SOLTE", imp + new Vector2(0, -40), 20, RD);
        }

        void DrawContacts()
        {
            var s = K.Sess;
            foreach (var a in s.Air)
            {
                if (a.Dying > 0 || a == s.Tgt) continue;
                var w = K.World(a.Pos);
                if (!Proj(w, out var p) || p.x < 0 || p.x > W || p.y < 0 || p.y > H) continue;
                double r = Cm.Dist(a.Pos, s.Pl.Pos);
                Color c = a.Team == Team.Red ? RD : BL;
                float k = 7;
                Line(p + new Vector2(0, -k), p + new Vector2(k, 0), c); Line(p + new Vector2(k, 0), p + new Vector2(0, k), c); Line(p + new Vector2(0, k), p + new Vector2(-k, 0), c); Line(p + new Vector2(-k, 0), p + new Vector2(0, -k), c);
                Txt(a.Team == Team.Red ? $"{a.Name} {r / 1000:0}" : a.Name, p + new Vector2(0, -18), 13, c);
            }
            foreach (var g in s.Gnd)
            {
                if (!g.Alive || !g.Primary || Cm.Dist(g.Pos, s.Pl.Pos) > 25000 || g == s.Tgt) continue;
                if (Proj(K.World(g.Pos), out var p)) Box(p, 5, 5, AM, 1.4f);
            }
        }

        void DrawStatus()
        {
            var s = F.S;
            var sess = K.Sess;
            var bl = new Vector2(30, H - 190);
            float hp = (float)(sess.Pl.Hp / sess.Pl.HpMax);
            Txt("CASCO", bl, 14, GD, TextAnchor.MiddleLeft);
            Line(bl + new Vector2(62, 0), bl + new Vector2(212, 0), new Color(1, 1, 1, 0.18f), 7);
            Line(bl + new Vector2(62, 0), bl + new Vector2(62 + 150 * hp, 0), hp > 0.5f ? G : hp > 0.25f ? AM : RD, 7);
            double totalFuel = F.Ac.FuelMax + GameSession.ExtraFuel();
            float fuel = (float)(s.Fuel / totalFuel);
            Txt("COMB", bl + new Vector2(0, 22), 14, GD, TextAnchor.MiddleLeft);
            Line(bl + new Vector2(62, 22), bl + new Vector2(212, 22), new Color(1, 1, 1, 0.18f), 7);
            Line(bl + new Vector2(62, 22), bl + new Vector2(62 + 150 * fuel, 22), fuel > 0.15f ? G : RD, 7);
            Txt($"{s.Fuel:0} kg", bl + new Vector2(220, 22), 13, GD, TextAnchor.MiddleLeft);
            string gear = F.Ac.GearFixed ? "FIXO" : s.Gear > 0.99 ? "BAIXO" : s.Gear < 0.01 ? "RECOLH" : "MOV";
            Txt($"TREM {gear}", bl + new Vector2(0, 46), 15, s.Gear > 0.01 && s.Gear < 0.99 ? AM : GD, TextAnchor.MiddleLeft);
            Txt($"FLAP {F.Ctl.FlapIdx}", bl + new Vector2(135, 46), 15, GD, TextAnchor.MiddleLeft);
            if (s.Sb > 0.05) Txt("AEROFREIO", bl + new Vector2(215, 46), 15, AM, TextAnchor.MiddleLeft);
            if (F.Ctl.Parking) Txt("FREIO ESTAC.", bl + new Vector2(0, 68), 15, AM, TextAnchor.MiddleLeft);
            string sistemas = (F.AutoPilot ? "A/P  " : "") + (F.ProbeOut ? "SONDA  " : "") + (F.LandingLight ? "FAROL  " : "") + (F.TemArmamento && !F.MasterArm ? "ARM SEGURO" : "");
            if (sistemas.Length > 0) Txt(sistemas, bl + new Vector2(130, 68), 15, G, TextAnchor.MiddleLeft);
            if (K.Ativo != null && sess.Wave > 0)
                Txt($"{K.Ativo.nome}: contatos a {K.AmeacaKm:0} km", bl + new Vector2(0, -26), 16, K.AmeacaKm < K.Ativo.raio_de_seguranca_km + 20 ? RD : AM, TextAnchor.MiddleLeft);
            double dh = Cm.Dist(new V3(sess.Pl.Pos.X, sess.Pl.Pos.Y, 0), new V3(sess.Home.X, sess.Home.Y, 0));
            double brg = (Math.Atan2(sess.Home.X - sess.Pl.Pos.X, sess.Home.Y - sess.Pl.Pos.Y) * 57.2958 + 360) % 360;
            Txt($"BASE {brg:000}°  {dh / 1000:0.0} km", new Vector2(W - 30, H - 34), 15, GD, TextAnchor.MiddleRight);
            Txt($"PONTOS {sess.Score}", new Vector2(30, 188), 15, GD, TextAnchor.MiddleLeft);
        }

        void DrawScope()
        {
            var s = K.Sess;
            float R = 105;
            double range = 40000;
            var c = new Vector2(W - R - 24, H - R - 56);
            Dot(c, R, new Color(0.01f, 0.05f, 0.05f, 0.58f));
            Ring(c, R, GD, 1.4f); Ring(c, R * 0.5f, new Color(0.35f, 1, 0.55f, 0.22f), 1);
            float hdg = (float)(F.S.Diag.Hdg * Mathf.Deg2Rad);
            Vector2 Plot(V3 p)
            {
                float dx = (float)(p.X - s.Pl.Pos.X), dy = (float)(p.Y - s.Pl.Pos.Y);
                float e = dx * Mathf.Cos(hdg) - dy * Mathf.Sin(hdg), n = dx * Mathf.Sin(hdg) + dy * Mathf.Cos(hdg);
                return c + new Vector2(e, -n) * (R / (float)range);
            }
            bool Inside(Vector2 q) => (q - c).sqrMagnitude < R * R;
            Line(c + new Vector2(0, -8), c + new Vector2(0, 8), G, 2); Line(c + new Vector2(-7, 2), c + new Vector2(7, 2), G, 2);
            Txt($"{range / 1000:0} km", c + new Vector2(0, R + 12), 13, GD);
            foreach (var a in s.Air)
            {
                if (a.Dying > 0) continue;
                var q = Plot(a.Pos);
                if (!Inside(q)) continue;
                Dot(q, a == s.Tgt ? 5 : 3.5f, a.Team == Team.Red ? RD : BL);
            }
            foreach (var g in s.Gnd) { if (!g.Alive) continue; var q = Plot(g.Pos); if (Inside(q)) Dot(q, 2.5f, g.Primary ? AM : GD); }
            foreach (var b in BaseDb.All)
            {
                var q = Plot(K.ToEnu(b.lat * Mathf.Deg2Rad, b.lon * Mathf.Deg2Rad, 0));
                if (!Inside(q)) continue;
                Box(q, 3.5f, 3.5f, BL, 1.2f);
            }
            foreach (var m in s.Missiles) { if (m.Team == Team.Red) { var q = Plot(m.Pos); if (Inside(q)) Dot(q, 2.5f, AM); } }
            for (int i = Rota.Ativo; i < Rota.Pontos.Count; i++)
            {
                var q = Plot(K.ToEnu(Rota.Pontos[i].Lat * Mathf.Deg2Rad, Rota.Pontos[i].Lon * Mathf.Deg2Rad, 0));
                if (Inside(q)) Dot(q, i == Rota.Ativo ? 4 : 3, RT);
            }
            if (K.Ativo != null)
            {
                var q = Plot(K.ToEnu(K.Ativo.posicao.lat * Mathf.Deg2Rad, K.Ativo.posicao.lon * Mathf.Deg2Rad, 0));
                if (!Inside(q)) q = c + (q - c).normalized * (R - 7);
                Ring(q, 5, AM, 2);
            }
            var home = Plot(s.Home);
            if (!Inside(home)) { var d = (home - c).normalized; home = c + d * (R - 7); }
            Ring(home, 5, BL, 1.6f);
        }

        void DrawNav()
        {
            var s = F.S;
            double lat = s.Lat * 57.29578, lon = s.Lon * 57.29578, gs = Math.Sqrt(s.V.X * s.V.X + s.V.Y * s.V.Y);
            void Marca(double mLat, double mLon, Color c, string nomeMarca)
            {
                double d = Rota.Dist(lat, lon, mLat, mLon);
                if (!Proj(K.World(K.ToEnu(mLat * Mathf.Deg2Rad, mLon * Mathf.Deg2Rad, s.Z)), out var p)) return;
                float r = 9;
                Line(p + new Vector2(0, -r), p + new Vector2(r, 0), c, 2); Line(p + new Vector2(r, 0), p + new Vector2(0, r), c, 2);
                Line(p + new Vector2(0, r), p + new Vector2(-r, 0), c, 2); Line(p + new Vector2(-r, 0), p + new Vector2(0, -r), c, 2);
                Txt($"{nomeMarca} {Rota.Km(d)}", p + new Vector2(0, 22), 14, c);
            }
            if (K.Ativo != null) Marca(K.Ativo.posicao.lat, K.Ativo.posicao.lon, AM, K.Ativo.nome);
            var reab = K.Reab;
            if (reab != null && !reab.Feito && reab.Distancia < 9000)
            {
                if (Proj(reab.CestaMundo, out var cesta)) { Ring(cesta, 11, reab.Contato ? G : AM, 2.5f); Dot(cesta, 2.5f, reab.Contato ? G : AM); }
                string guia;
                if (reab.Contato) guia = $"CONTATO   +{reab.Transferido:0} kg   {s.Fuel:0} / {reab.Capacidade:0}";
                else if (!F.ProbeOut) guia = "ESTENDA A SONDA";
                else
                {
                    var e = reab.Erro;
                    guia = $"CESTA {reab.Distancia:0} m   fechando {reab.Fechamento:0.0} m/s   {(e.x >= 0 ? "DIR" : "ESQ")} {Mathf.Abs(e.x):0.0}   {(e.y >= 0 ? "SOBE" : "DESCE")} {Mathf.Abs(e.y):0.0}";
                }
                Txt(guia, new Vector2(W / 2, H * 0.72f), 19, reab.Contato ? G : AM);
            }
            var wp = Rota.Atual;
            if (wp == null)
            {
                if (Rota.Pontos.Count > 0) Txt("ROTA CONCLUÍDA", new Vector2(W - 30, H - 56), 15, RT, TextAnchor.MiddleRight);
                return;
            }
            string nome = Rota.Nome(Rota.Ativo);
            Marca(wp.Lat, wp.Lon, RT, nome);
            double dist = Rota.Dist(lat, lon, wp.Lat, wp.Lon), rumo = Rota.Rumo(lat, lon, wp.Lat, wp.Lon);
            Txt($"{nome} ({Rota.Ativo + 1}/{Rota.Pontos.Count})  {rumo:000}°  {Rota.Km(dist)}  {Rota.Tempo(dist / Math.Max(gs, 25))}", new Vector2(W - 30, H - 56), 15, RT, TextAnchor.MiddleRight);
        }

        PontoRota BaseEm(Vector2 tela)
        {
            foreach (var b in BaseDb.All)
                if ((mapa.Tela(b.lat, b.lon) - tela).sqrMagnitude < 196) return new PontoRota { Lat = b.lat, Lon = b.lon, Nome = b.icao };
            return null;
        }

        void DrawMapa()
        {
            var sess = K.Sess;
            var s = F.S;
            double lat = s.Lat * 57.29578, lon = s.Lon * 57.29578, gs = Math.Sqrt(s.V.X * s.V.X + s.V.Y * s.V.Y);
            Vector2 Enu(V3 p) { K.FromEnu(p, out double la, out double lo); return mapa.Tela(la, lo); }
            var preto = new Color(0, 0, 0, 0.8f);

            foreach (var b in BaseDb.All)
            {
                foreach (var r in b.pistas)
                {
                    Vector2 a = mapa.Tela(r.cabeceira_a.lat, r.cabeceira_a.lon), z = mapa.Tela(r.cabeceira_b.lat, r.cabeceira_b.lon);
                    Line(a, z, preto, 7); Line(a, z, Color.white, 3);
                }
                var q = mapa.Tela(b.lat, b.lon);
                Txt(b.icao, q + new Vector2(1, -21), 14, Color.black);
                Txt(b.icao, q + new Vector2(0, -22), 14, Color.white);
            }

            if (K.Ativo != null)
            {
                var q = mapa.Tela(K.Ativo.posicao.lat, K.Ativo.posicao.lon);
                float raio = (float)(K.Ativo.raio_de_seguranca_km * 1000 / mapa.MetrosPorPx(K.Ativo.posicao.lat));
                Ring(q, raio, RD, 2); Dot(q, 6, RD);
                Txt(K.Ativo.nome, q + new Vector2(1, -19), 15, Color.black);
                Txt(K.Ativo.nome, q + new Vector2(0, -20), 15, RD);
            }
            foreach (var g in sess.Gnd)
            {
                if (!g.Alive || !g.Primary) continue;
                var q = Enu(g.Pos);
                Ring(q, 8, RD, 2.5f); Txt("ALVO", q + new Vector2(0, -16), 13, RD);
            }
            foreach (var a in sess.Air)
            {
                if (a.Dying > 0) continue;
                var q = Enu(a.Pos);
                Dot(q, a == sess.Tgt ? 6 : 4.5f, a.Team == Team.Red ? RD : BL);
                Txt(a.Name, q + new Vector2(0, -14), 13, a.Team == Team.Red ? RD : BL);
            }
            foreach (var m in sess.Missiles) if (m.Team == Team.Red) Dot(Enu(m.Pos), 3, AM);

            if (K.Reab != null)
            {
                var q = Enu(K.Reab.Pos);
                Dot(q, 7, G); Ring(q, 12, G, 2);
                Txt("KC-390 TANQUE", q + new Vector2(1, -19), 14, Color.black);
                Txt("KC-390 TANQUE", q + new Vector2(0, -20), 14, G);
            }
            var home = Enu(sess.Home);
            Ring(home, 9, BL, 2.5f); Txt("POUSO", home + new Vector2(0, 18), 13, BL);

            var aviao = mapa.Tela(lat, lon);
            var ant = aviao;
            for (int i = Rota.Ativo; i < Rota.Pontos.Count; i++)
            {
                var q = mapa.Tela(Rota.Pontos[i].Lat, Rota.Pontos[i].Lon);
                Line(ant, q, RT, i == Rota.Ativo ? 2.5f : 4);
                ant = q;
            }
            for (int i = 0; i < Rota.Pontos.Count; i++)
            {
                var q = mapa.Tela(Rota.Pontos[i].Lat, Rota.Pontos[i].Lon);
                Dot(q, 12, i == Rota.Ativo ? RT : new Color(0.55f, 0.25f, 0.5f, 1)); Ring(q, 12, Color.white, 1.5f);
                Txt($"{i + 1}", q, 14, Color.white);
            }

            float hdg = (float)(s.Diag.Hdg * Mathf.Deg2Rad);
            Vector2 Dir(float ang, float r) => aviao + new Vector2(Mathf.Sin(hdg + ang), -Mathf.Cos(hdg + ang)) * r;
            Line(Dir(0, 16), Dir(2.6f, 12), Color.white, 3); Line(Dir(0, 16), Dir(-2.6f, 12), Color.white, 3); Line(Dir(2.6f, 12), Dir(-2.6f, 12), Color.white, 3);

            int objetivos = K.Missao.objetivos?.Length ?? 0;
            Painel(new Vector2(12, 50), new Vector2(480, 34 + objetivos * 24 + 8));
            float y = 66;
            Txt(K.Missao.titulo, new Vector2(24, y), 17, Color.white, TextAnchor.MiddleLeft); y += 26;
            for (int i = 0; i < objetivos; i++)
            {
                var o = K.Missao.objetivos[i];
                string t = o.descricao.Length > 56 ? o.descricao.Substring(0, 55) + "…" : o.descricao;
                Txt(t, new Vector2(24, y), 14, o.obrigatorio ? AM : GD, TextAnchor.MiddleLeft); y += 24;
            }

            float x = W - 430;
            Painel(new Vector2(x - 12, 50), new Vector2(430, 58 + Mathf.Max(1, Rota.Pontos.Count) * 22));
            y = 66;
            Txt("#", new Vector2(x, y), 13, GD, TextAnchor.MiddleLeft); Txt("PONTO", new Vector2(x + 30, y), 13, GD, TextAnchor.MiddleLeft);
            Txt("DIST", new Vector2(x + 170, y), 13, GD, TextAnchor.MiddleLeft); Txt("RUMO", new Vector2(x + 250, y), 13, GD, TextAnchor.MiddleLeft); Txt("TEMPO", new Vector2(x + 320, y), 13, GD, TextAnchor.MiddleLeft);
            y += 22;
            double pLat = lat, pLon = lon, total = 0, v = Math.Max(gs, 25);
            for (int i = 0; i < Rota.Pontos.Count; i++)
            {
                var p = Rota.Pontos[i];
                double d = Rota.Dist(pLat, pLon, p.Lat, p.Lon), b = Rota.Rumo(pLat, pLon, p.Lat, p.Lon);
                total += d; pLat = p.Lat; pLon = p.Lon;
                var cor = i == Rota.Ativo ? RT : Color.white;
                Txt($"{i + 1}", new Vector2(x, y), 14, cor, TextAnchor.MiddleLeft); Txt(Rota.Nome(i), new Vector2(x + 30, y), 14, cor, TextAnchor.MiddleLeft);
                Txt(Rota.Km(d), new Vector2(x + 170, y), 14, cor, TextAnchor.MiddleLeft); Txt($"{b:000}°", new Vector2(x + 250, y), 14, cor, TextAnchor.MiddleLeft); Txt(Rota.Tempo(d / v), new Vector2(x + 320, y), 14, cor, TextAnchor.MiddleLeft);
                y += 22;
            }
            if (Rota.Pontos.Count == 0) { Txt("Clique no mapa para criar pontos. Botão direito remove.", new Vector2(x, y), 13, Color.white, TextAnchor.MiddleLeft); y += 22; }
            Txt(Rota.Pontos.Count > 0 ? $"Total {Rota.Km(total)} · {Rota.Tempo(total / v)}" : "Clique numa base para usá-la como ponto.", new Vector2(x, y + 2), 13, GD, TextAnchor.MiddleLeft);
            Txt(mapa.Credito, new Vector2(W - 12, H - 14), 12, Color.white, TextAnchor.MiddleRight);
        }

        void Painel(Vector2 p, Vector2 size) => blocos.Add((p, size, new Color(0.02f, 0.05f, 0.07f, 0.8f)));

        void DrawRadio()
        {
            var cur = K.Radio.Current;
            radioBox.style.display = cur != null && !mapa.Aberto ? DisplayStyle.Flex : DisplayStyle.None;
            if (cur != null) { radioFrom.text = cur.De.ToUpperInvariant(); radioText.text = cur.Texto; }
            for (int i = 0; i < toastLabels.Count; i++)
            {
                int j = K.Toasts.Count - toastLabels.Count + i;
                toastLabels[i].text = j >= 0 ? K.Toasts[j] : "";
            }
        }

        void UpdateOverlay()
        {
            if (K.Encerrada && !debriefShown)
            {
                debriefDelay -= Time.unscaledDeltaTime;
                if (debriefDelay <= 0) { debriefShown = true; ShowDebrief(); }
            }
            if (F.Input.ToggleMenu && !debriefShown)
            {
                if (mapa.Aberto) { mapa.Fechar(); return; }
                F.Paused = !F.Paused;
                if (F.Paused) ShowPause(); else overlay.style.display = DisplayStyle.None;
            }
            if (overlay.style.display == DisplayStyle.None) return;
            var pad = Gamepad.current; var kb = Keyboard.current;
            int mv = 0; bool ok = false;
            if (pad != null) { if (pad.dpad.down.wasPressedThisFrame) mv = 1; if (pad.dpad.up.wasPressedThisFrame) mv = -1; ok |= pad.buttonSouth.wasPressedThisFrame; }
            if (kb != null) { if (kb.downArrowKey.wasPressedThisFrame) mv = 1; if (kb.upArrowKey.wasPressedThisFrame) mv = -1; ok |= kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame; }
            if (mv != 0) { menuIdx = (menuIdx + mv + menu.Count) % menu.Count; RefreshMenu(); }
            if (ok && menu.Count > 0) menu[menuIdx].act();
        }

        void ShowPause()
        {
            overlayTitle.text = "PAUSA";
            overlayBody.text = K.Missao.titulo + (K.Missao.objetivos != null && K.Missao.objetivos.Length > 0 ? "\n" + K.Missao.objetivos[0].descricao : "");
            SetMenu(("Continuar", () => { F.Paused = false; overlay.style.display = DisplayStyle.None; }), ("Reiniciar missão", Reload), ("Voltar ao saguão", ToLobby));
        }

        void ShowDebrief()
        {
            var s = K.Sess;
            bool win = s.Over == Outcome.Win && K.Falha == null;
            overlayTitle.text = win ? "MISSÃO CUMPRIDA" : "MISSÃO FRACASSADA";
            overlayTitle.style.color = win ? G : RD;
            overlayBody.text = (K.Falha != null ? K.Falha + "\n" : "") + $"Pontos {s.Score}   ·   Abates aéreos {s.KillsAir}   ·   Alvos no solo {s.KillsGnd}\nDisparos {s.Shots}   ·   Acertos {s.Hits}   ·   Tempo {s.T / 60:0.0} min";
            SetMenu(("Repetir missão", Reload), ("Voltar ao saguão", ToLobby));
        }

        void SetMenu(params (string, Action)[] items)
        {
            menu.Clear(); menu.AddRange(items); menuIdx = 0;
            overlay.style.display = DisplayStyle.Flex;
            overlay.pickingMode = PickingMode.Position;
            RefreshMenu();
        }

        void RefreshMenu()
        {
            overlayList.Clear();
            for (int i = 0; i < menu.Count; i++)
            {
                int idx = i;
                var b = new Label(menu[i].text);
                b.style.fontSize = 24; b.style.color = i == menuIdx ? Color.black : Color.white;
                b.style.backgroundColor = i == menuIdx ? G : new Color(1, 1, 1, 0.08f);
                b.style.unityTextAlign = TextAnchor.MiddleCenter; b.style.width = 380; b.style.height = 50; b.style.marginBottom = 8;
                Corner(b, 6);
                b.RegisterCallback<ClickEvent>(_ => menu[idx].act());
                overlayList.Add(b);
            }
        }

        void Reload() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        void ToLobby() => SceneManager.LoadScene("Saguao");
    }
}
