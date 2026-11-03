using System;
using System.IO;
using CesiumForUnity;
using FABSim.Core;
using FABSim.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// FAB_AUTOTEST: 1 = decolagem e combate, 2 = miras contra alvos no solo, 3 = saguao ate o voo, 4 = reabastecimento, 5 = cabine e mapa, 6 = rapto noturno, 7 = reverso e corte de motor, 8 = menu do A no D-pad
[InitializeOnLoad]
public static class FabAutoTest
{
    const string Flag = "FAB_AUTOTEST";
    static double start, phaseT;
    static int phase;
    static bool started;
    static StreamWriter log;
    static string Mode => Environment.GetEnvironmentVariable(Flag);

    static FabAutoTest()
    {
        if (Mode == null) return;
        Application.runInBackground = true;
        EditorApplication.update += Tick;
    }

    static void W(string s)
    {
        Debug.Log("[FabTest] " + s);
        log ??= new StreamWriter(Path.GetFullPath("Logs/autotest.txt"), true) { AutoFlush = true };
        log.WriteLine($"{DateTime.Now:HH:mm:ss} {s}");
    }

    static void Tela(string name)
    {
        Directory.CreateDirectory(Path.GetFullPath("Logs/shots"));
        ScreenCapture.CaptureScreenshot(Path.GetFullPath($"Logs/shots/{name}.png"));
        W("tela " + name);
    }

    static void Foto(Camera cam, string name)
    {
        Directory.CreateDirectory(Path.GetFullPath("Logs/shots"));
        var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        var old = cam.targetTexture;
        cam.targetTexture = rt;
        for (int i = 0; i < 2; i++) cam.Render();
        cam.targetTexture = old;
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var img = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        img.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        img.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(Path.GetFullPath($"Logs/shots/{name}.png"), img.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(img);
        W("foto " + name);
    }

    static string State(FlightController f)
    {
        if (f.S == null) return "sem estado: " + f.Status;
        var d = f.S.Diag;
        FlightModel.Euler(f.S.Q, out double phi, out double th, out _);
        return $"IAS {d.Ias:0} AGL {d.Agl:0.0} Z {f.S.Z:0} VS {d.Vsi:0} pitch {th * 57.3:0.0} roll {phi * 57.3:0.0} G {d.Nz:0.0} contato {f.S.Contact} trem {f.S.Gear:0.0} fps {f.Fps:0} terreno {f.terrain.ComputeLoadProgress():0}% queda '{f.S.Crashed}'";
    }

    static void Finish(int code, string why)
    {
        W("FIM: " + why);
        EditorApplication.update -= Tick;
        log?.Dispose();
        EditorApplication.Exit(code);
    }

    static void Hold(FlightController f, double thetaDeg) { FlightModel.Euler(f.S.Q, out _, out double th, out _); f.PitchOverride = Cm.Clamp((thetaDeg / 57.3 - th) * 2.2 - f.S.W.Y * 1.2, -0.6, 0.6); }
    static void Level(FlightController f) => f.PitchOverride = Cm.Clamp(-f.S.Diag.Vsi / 196.85 * 0.02 - f.S.W.Y * 0.5, -0.5, 0.5);

    static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            if (!started && !EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.timeSinceStartup > 5)
            {
                started = true;
                string scene = Mode == "3" ? "Assets/Scenes/Saguao.unity" : "Assets/Scenes/Voo.unity";
                if (EditorSceneManager.GetActiveScene().path != scene) EditorSceneManager.OpenScene(scene);
                EditorApplication.EnterPlaymode();
            }
            return;
        }
        double now = EditorApplication.timeSinceStartup;
        if (start == 0) { start = now; phaseT = now; W("play iniciado, modo " + Mode); }
        if (now - start > 400) { Finish(2, "tempo esgotado"); return; }
        if (Mode == "3") { TickLobby(now); return; }
        var f = UnityEngine.Object.FindFirstObjectByType<FlightController>();
        if (f == null || !f.Ready) return;
        var k = f.GetComponent<Combate>();
        if (f.S.Crashed.Length > 0 && !k.Sess.Invuln) { W(State(f)); Tela("98_falha"); Finish(3, "queda: " + f.S.Crashed); return; }
        if (Mode == "8") TickMenu(f, k, now);
        else if (Mode == "7") TickMotor(f, now);
        else if (Mode == "6") TickRapto(f, k, now);
        else if (Mode == "4" || Mode == "5") TickCabine(f, k, now);
        else if (Mode == "2") TickSights(f, k, now); else TickCombat(f, k, now);
    }

    static Gamepad padTeste;

    static void Botoes(params GamepadButton[] b)
    {
        var st = new GamepadState();
        foreach (var x in b) st = st.WithButton(x);
        InputSystem.QueueStateEvent(padTeste, st);
    }

    static void TickMenu(FlightController f, Combate k, double now)
    {
        double dt = now - phaseT;
        var s = f.S;
        var inp = f.Input;
        void Next() { phase++; phaseT = now; }
        const double P = 0.4;
        Level(f);
        switch (phase)
        {
            case 0:
                f.ThrottleOverride = 0.6;
                f.Teleport(s.Lat, s.Lon, s.Z + 1500, s.Diag.Hdg / 57.29578, 150);
                f.Ctl.GearDown = true;
                padTeste = InputSystem.AddDevice<Gamepad>();
                Next();
                break;
            case 1: if (dt > 1) { W($"sem A: menu {inp.MenuAberto} flap {f.Ctl.FlapIdx} arma {k.Sess.Sel}"); Botoes(GamepadButton.DpadRight); Next(); } break;
            case 2: if (dt > P) { W($"D-pad direita sem A: flap {f.Ctl.FlapIdx} menu {inp.MenuAberto}"); Botoes(); Next(); } break;
            case 3: if (dt > P) { Botoes(GamepadButton.DpadDown); Next(); } break;
            case 4: if (dt > P) { W($"D-pad baixo sem A: arma {k.Sess.Sel} idx {inp.MenuIdx}"); Botoes(); Next(); } break;
            case 5: if (dt > P) { Botoes(GamepadButton.South); Next(); } break;
            case 6: if (dt > P) { W($"A segurado: menu {inp.MenuAberto} itens {string.Join(",", inp.MenuItens)} idx {inp.MenuIdx} trem pedido {f.Ctl.GearDown}"); Tela("m0_menu"); Next(); } break;
            case 7: if (dt > P) { Botoes(GamepadButton.South, GamepadButton.DpadRight); Next(); } break;
            case 8: if (dt > P) { W($"A + direita em Trem: trem pedido {f.Ctl.GearDown} flap {f.Ctl.FlapIdx}"); Botoes(GamepadButton.South); Next(); } break;
            case 9: if (dt > P) { Botoes(GamepadButton.South, GamepadButton.DpadDown); Next(); } break;
            case 10: if (dt > P) { Botoes(GamepadButton.South); W($"A + baixo: idx {inp.MenuIdx} arma {k.Sess.Sel}"); Next(); } break;
            case 11: if (dt > P) { Botoes(GamepadButton.South, GamepadButton.DpadRight); Next(); } break;
            case 12: if (dt > P) { W($"A + direita em Aerofreio: aerofreio {f.Ctl.Speedbrake}"); Tela("m1_menu_acionado"); Botoes(GamepadButton.South); Next(); } break;
            case 13: if (dt > P) { Botoes(); Next(); } break;
            case 14: if (dt > P) { W($"A solto: menu {inp.MenuAberto} trem pedido {f.Ctl.GearDown}"); InputSystem.RemoveDevice(padTeste); Finish(0, "menu ok"); } break;
        }
    }

    static double v0;

    static void TickMotor(FlightController f, double now)
    {
        double dt = now - phaseT;
        var s = f.S;
        void Next() { phase++; phaseT = now; }
        string Motor() => $"N {s.N:0.00} empuxo {s.Thrust:0} fluxo {s.Flow * 3600:0} kg/h IAS {s.Diag.Ias * 1.944:0} Z {s.Z:0} corte {f.Ctl.EngineCut}";
        if (phase > 5) Level(f);
        switch (phase)
        {
            case 0: W("pronto: " + State(f)); f.ThrottleOverride = 1; Next(); break;
            case 1: if (s.Diag.Gs > 35 || dt > 40) { f.ThrottleOverride = 0; v0 = s.Diag.Gs; Next(); } break;
            case 2: if (dt > 3) { W($"sem reverso: desacelera {(v0 - s.Diag.Gs) / dt:0.00} m/s2 a partir de {v0:0} m/s"); f.ThrottleOverride = 1; Next(); } break;
            case 3: if (s.Diag.Gs > 35 || dt > 40) { f.ReverseOverride = true; v0 = s.Diag.Gs; Next(); } break;
            case 4: if (dt > 3) { W($"com reverso a plena potencia: desacelera {(v0 - s.Diag.Gs) / dt:0.00} m/s2 a partir de {v0:0} m/s, aplicado {f.Ctl.Reverse}"); f.ReverseOverride = false; f.ThrottleOverride = 0.6; Next(); } break;
            case 5: f.Teleport(s.Lat, s.Lon, s.Z + 1500, s.Diag.Hdg / 57.29578, 150); Next(); break;
            case 6: if (dt > 5) { W("motor ligado: " + Motor()); f.Ctl.EngineCut = true; Next(); } break;
            case 7: if (dt > 8) { W("motor cortado: " + Motor()); f.Ctl.EngineCut = false; Next(); } break;
            case 8: if (dt > 8) { W("apos partida: " + Motor()); Finish(0, "motor ok"); } break;
        }
    }

    static void TickRapto(FlightController f, Combate k, double now)
    {
        double dt = now - phaseT;
        var c = k.Sess;
        void Next() { phase++; phaseT = now; }
        AirUnit Heli() { foreach (var a in c.Air) if (a.Profile.Heli && a.Profile.Tipo == "MH-60M") return a; return null; }
        if (phase > 1 && phase < 8) Level(f);
        switch (phase)
        {
            case 0: if (f.terrain.ComputeLoadProgress() > 90 || dt > 25) { Tela("r00_noite_solo"); Next(); } break;
            case 1: if (dt > 1) { f.Teleport(f.S.Lat, f.S.Lon, f.S.Z + 1500, f.S.Diag.Hdg / 57.29578, 150); f.ThrottleOverride = 0.6; Next(); } break;
            case 2:
                if (Heli() == null) { if (dt > 60) Finish(4, "sem helicopteros"); break; }
                var h = Heli();
                W($"helicopteros no radar: {c.Air.Count} unidades, Palacio a {k.AmeacaKm:0} km");
                var pos = h.Pos - h.Dir * 150 + new V3(0, 0, 20);
                k.FromEnu(pos, out double la, out double lo);
                f.ThrottleOverride = 0.4;
                f.Teleport(la / 57.29578, lo / 57.29578, pos.Z, Math.Atan2(h.Dir.X, h.Dir.Y), 95);
                Next();
                break;
            case 3: if (dt > 2) { W(State(f)); Tela("r1_noite_atras"); Next(); } break;
            case 4: if (dt > 1) { f.SetCockpit(true); Next(); } break;
            case 5: if (dt > 2) { Tela("r2_noite_cabine"); Next(); } break;
            case 6: if (dt > 1) { f.SetCockpit(false); Next(); } break;
            case 7: if (dt > 1) { c.HeliNoAlvo = true; Next(); } break;
            case 8: if (dt > 3) { W($"chegada ao alvo: encerrada {k.Encerrada} motivo '{k.Falha}'"); Finish(0, "rapto ok"); } break;
        }
    }

    static void TickCabine(FlightController f, Combate k, double now)
    {
        double dt = now - phaseT;
        var s = f.S;
        var hud = UnityEngine.Object.FindFirstObjectByType<Hud>();
        var r = k.Reab;
        if (phase > 1 && phase < 12) Level(f);
        void Next() { phase++; phaseT = now; }
        void Atras(double metros)
        {
            V3 alvo = r != null ? r.Pos - r.Dir * metros : k.ToEnu(s.Lat, s.Lon, 3500);
            k.FromEnu(alvo, out double la, out double lo);
            double hdg = r != null ? Math.Atan2(r.Dir.X, r.Dir.Y) : 0;
            f.Teleport(la / 57.29578, lo / 57.29578, alvo.Z, hdg, r != null ? 175 : 150);
        }
        void Encaixa()
        {
            Vector3 d = r.CestaMundo - r.PontaMundo;
            V3 alvo = k.Sess.Pl.Pos + new V3(d.x, d.z, d.y);
            k.FromEnu(alvo, out double la, out double lo);
            f.Teleport(la / 57.29578, lo / 57.29578, alvo.Z, Math.Atan2(r.Dir.X, r.Dir.Y), 175);
        }
        switch (phase)
        {
            case 0: if (f.terrain.ComputeLoadProgress() > 90 || dt > 25) { Tela("r00_solo"); Next(); } break;
            case 1: if (dt > 1) { W("pronto: " + State(f)); Atras(1500); f.ThrottleOverride = 0.55; Next(); } break;
            case 2: if (dt > 8) { W($"cruzeiro: {State(f)} fluxo {s.Flow * 3600:0} kg/h comb {s.Fuel:0}"); Tela("r0_externa"); Next(); } break;
            case 3: if (dt > 1) { if (r == null) { phase = 6; phaseT = now; break; } f.ProbeOut = true; Next(); } break;
            case 4: if (dt > 2) { Encaixa(); Next(); } break;
            case 5:
                if (dt < 4) Encaixa();
                if (dt > 6) { W($"reabastecimento: contato {r.Contato} transferido {r.Transferido:0} comb {s.Fuel:0} dist {r.Distancia:0.0} {State(f)}"); Tela("r1_contato"); Next(); }
                break;
            case 6: if (dt > 1) { f.SetCockpit(true); Next(); } break;
            case 7: if (dt > 2) { Tela("r2_cabine"); Next(); } break;
            case 8: if (dt > 1) { f.Zoom = 1; Next(); } break;
            case 9: if (dt > 2) { Tela("r3_zoom"); Next(); } break;
            case 10: if (dt > 1) { f.Zoom = 0; f.SetCockpit(false); hud.Mapa.Alternar(); Next(); } break;
            case 11: if (dt > 10) { Tela("r4_mapa"); Next(); } break;
            case 12:
                if (dt < 1) break;
                hud.Mapa.Fechar();
                f.PitchOverride = null;
                f.AlternarPiloto();
                W($"A/P {f.AutoPilot}: proa {s.Diag.Hdg:0} rumo da rota {(Rota.Atual != null ? Rota.Rumo(s.Lat * 57.29578, s.Lon * 57.29578, Rota.Atual.Lat, Rota.Atual.Lon) : -1):0} Z {s.Z:0}");
                Next();
                break;
            case 13: if (dt > 1) { f.CameraDist = 0.5f; Next(); } break;
            case 14: if (dt > 2) { Tela("r5_camera_perto"); Next(); } break;
            case 15: if (dt > 1) { f.CameraDist = 3; Next(); } break;
            case 16: if (dt > 2) { Tela("r6_camera_longe"); Next(); } break;
            case 17: if (dt > 1) { f.CameraDist = 1; Next(); } break;
            case 18:
                if (dt > 12 && dt < 12.05) W($"A/P meio: manche {f.Ctl.Pitch:0.00} override {f.PitchOverride} Z {s.Z:0} pitch {s.Diag.Pitch:0.0} G {s.Diag.Nz:0.0}");
                if (dt < 25) break;
                W($"A/P {f.AutoPilot} apos o teste: manche {f.Ctl.Pitch:0.00} override {f.PitchOverride} proa {s.Diag.Hdg:0} {State(f)}");
                Finish(0, "cabine, mapa e piloto ok");
                break;
        }
    }

    static void TickLobby(double now)
    {
        var s = UnityEngine.Object.FindFirstObjectByType<Saguao>();
        double dt = now - phaseT;
        void Step(string shot, Action next, double wait = 4)
        {
            if (dt < wait) return;
            Tela(shot); next?.Invoke(); phase++; phaseT = now;
        }
        switch (phase)
        {
            case 0: Step("s0_aeronave", null, 8); break;
            case 1: Step("s0_aeronave_b", () => s.Next(), 2); break;
            case 2: Step("s1_armamento", () => s.Next()); break;
            case 3: Step("s2_missao", () => s.Next()); break;
            case 4:
                var f = UnityEngine.Object.FindFirstObjectByType<FlightController>();
                if (f != null && f.Ready) { W("voo carregado pelo saguão: " + State(f)); phase++; phaseT = now; }
                break;
            case 5: if (dt > 3) { Tela("s3_voo"); phase++; phaseT = now; } break;
            case 6: if (dt > 2) Finish(0, "saguão completo"); break;
        }
    }

    static void TickCombat(FlightController f, Combate k, double now)
    {
        double dt = now - phaseT;
        var s = f.S; var c = k.Sess;
        switch (phase)
        {
            case 0: W("pronto: " + State(f)); phase = 1; phaseT = now; break;
            case 1:
                if (dt > 12)
                {
                    Tela("00_solo_tela");
                    var keep = f.cam.transform.position;
                    f.cam.transform.position = f.model.position + f.model.right * 26 + Vector3.up * 1.5f;
                    f.cam.transform.rotation = Quaternion.LookRotation(f.model.position - f.cam.transform.position);
                    Foto(f.cam, "00_solo_lado");
                    f.cam.transform.position = keep;
                    f.SetCockpit(true); phase = 2; phaseT = now;
                }
                break;
            case 2: if (dt > 1.5) { Tela("01_cabine_tela"); phase = 21; phaseT = now; } break;
            case 21: if (dt > 1) { f.SetCockpit(false); f.ThrottleOverride = 1; phase = 3; phaseT = now; } break;
            case 3:
                if (s.Diag.Ias > 140) Hold(f, 12);
                if (!s.Contact && s.Diag.Agl > 30) f.Ctl.GearDown = false;
                if (s.Diag.Agl > 120) { Tela("02_decolagem_tela"); phase = 4; phaseT = now; }
                if (dt > 120) Finish(3, "não decolou");
                break;
            case 4:
                Hold(f, 12);
                if (s.Diag.Agl > 1500) { f.ThrottleOverride = 0.75; phase = 5; phaseT = now; W("subiu: " + State(f)); }
                break;
            case 5:
                Level(f);
                if (c.Air.Exists(a => a.Team == Team.Red)) { W($"onda: {c.Air.Count} unidades"); phase = 6; phaseT = now; }
                else if (dt > 90) Finish(4, "inimigos não apareceram");
                break;
            case 6:
                Level(f);
                if (dt > 2)
                {
                    int i = 0;
                    foreach (var a in c.Air)
                    {
                        if (a.Team != Team.Red) continue;
                        a.Pos = c.Pl.Pos + c.Pl.Fwd * (700 + i * 160) + c.Pl.Right * (i * 70 - 90);
                        a.Dir = c.Pl.Fwd; a.Vel = a.Dir * a.Spd; i++;
                    }
                    phase = 7; phaseT = now;
                }
                break;
            case 7:
                Level(f);
                if (dt > 0.6) { Tela("03_inimigos_tela"); phase = 8; phaseT = now; }
                break;
            case 8:
                Level(f);
                if (c.Sel != WeaponKind.Aam && c.Avail.Contains(WeaponKind.Aam)) c.Sel = WeaponKind.Aam;
                if (c.LockOk || dt > 6)
                {
                    W($"trava {c.LockOk} alvo {c.Tgt?.Name}");
                    Tela("04_trava_tela");
                    c.Fire();
                    phase = 9; phaseT = now;
                }
                break;
            case 9:
                Level(f);
                if (dt > 1.2) { Tela("05_missil_tela"); c.Flare(); c.Sel = WeaponKind.Gun; c.InGun = true; phase = 10; phaseT = now; }
                break;
            case 10:
                Level(f);
                if (dt > 0.3) { Tela("06_canhao_tela"); phase = 11; phaseT = now; }
                break;
            case 11:
                Level(f);
                if (dt > 6) { W($"final: {State(f)} pontos {c.Score} abates {c.KillsAir} casco {c.Pl.Hp:0}"); f.SetCockpit(true); phase = 12; phaseT = now; }
                break;
            case 12:
                Level(f);
                if (dt > 1.5) { Tela("07_cabine_voo_tela"); phase = 13; phaseT = now; }
                break;
            case 13: if (dt > 1) Finish(0, "voo e combate completos"); break;
        }
    }

    static void TickSights(FlightController f, Combate k, double now)
    {
        double dt = now - phaseT;
        var s = f.S; var c = k.Sess;
        switch (phase)
        {
            case 0: W("pronto: " + State(f)); f.ThrottleOverride = 1; phase = 1; phaseT = now; break;
            case 1:
                if (s.Diag.Ias > 140) Hold(f, 10);
                if (!s.Contact && s.Diag.Agl > 30) f.Ctl.GearDown = false;
                if (s.Diag.Agl > 900) { f.ThrottleOverride = 0.6; phase = 2; phaseT = now; }
                if (dt > 120) Finish(3, "não decolou");
                break;
            case 2:
                Level(f);
                if (c.Gnd.Count > 0 && dt > 4) { W($"alvos: {c.Gnd.Count}; primeiro a {Cm.Dist(c.Gnd[0].Pos, c.Pl.Pos) / 1000:0.0} km"); phase = 3; phaseT = now; }
                break;
            case 3:
                Level(f);
                if (c.Avail.Contains(WeaponKind.Bomb)) c.Sel = WeaponKind.Bomb;
                if (c.Tgt == null && dt > 1) c.CycleTarget();
                if (dt > 3) { W($"bomba: sol {c.Sol?.Ccrp} alvo {c.Tgt?.Name} dist {c.Sol?.Dist:0} ttr {c.Sol?.Ttr:0}"); Tela("10_ccrp_tela"); phase = 4; phaseT = now; }
                break;
            case 4:
                Level(f);
                c.Tgt = null;
                if (dt > 1.5) { W($"ccip: tem impacto {c.Sol?.HasImpact}"); Tela("11_ccip_tela"); phase = 5; phaseT = now; }
                break;
            case 5:
                Hold(f, -20);
                if (c.Avail.Contains(WeaponKind.Rkt)) c.Sel = WeaponKind.Rkt;
                if (dt > 5) { W($"foguete: impacto {c.Sol?.HasImpact} alcance {c.Sol?.Range:0}"); Tela("12_foguete_tela"); c.Fire(); phase = 6; phaseT = now; }
                break;
            case 6:
                Hold(f, 0);
                if (dt > 1) { Tela("13_foguete_voo_tela"); Finish(0, "miras completas"); }
                break;
        }
    }
}
