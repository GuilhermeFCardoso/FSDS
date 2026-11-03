using System;
using System.Linq;
using FABSim.Core;

static class Checks
{
    static void Takeoff(string id, double vr, double pitchTarget, double throttle)
    {
        var a = AircraftDb.Get(id).Prepare();
        var s = FlightModel.SpawnGround(a, -0.28, -0.85, 1000, 0.68, 0.6);
        var c = new Controls { GearDown = true, FlapIdx = Math.Min(1, a.FlapNotches.Length - 1), Throttle = throttle };
        s.Flaps = a.FlapNotches[c.FlapIdx];
        const double h = 1.0 / 120;
        double liftT = -1, liftIas = 0, roll = 0, t1000 = -1, maxG = 1;
        double la0 = s.Lat, lo0 = s.Lon;
        for (int i = 0; i < 120 * 150; i++)
        {
            double t = i * h;
            FlightModel.Euler(s.Q, out _, out double th, out _);
            double ias = s.Diag.Ias;
            double err = pitchTarget * Math.PI / 180 - th;
            if (ias > vr) c.Pitch = FlightModel.Clamp(err * 2.2 - s.W.Y * 1.2, -0.6, 0.6);
            if (!s.Contact && s.Diag.Agl > 30) c.GearDown = false;
            if (!s.Contact && s.Diag.Agl > 200) c.FlapIdx = 0;
            FlightModel.Step(s, a, c, h, 1000);
            maxG = Math.Max(maxG, s.Diag.Nz);
            if (liftT < 0 && !s.Contact && s.Diag.Agl > 2 + a.CgHeight)
            {
                liftT = t; liftIas = ias;
                roll = Math.Sqrt(Math.Pow((s.Lat - la0) * FlightModel.RE, 2) + Math.Pow((s.Lon - lo0) * FlightModel.RE * Math.Cos(s.Lat), 2));
            }
            if (t1000 < 0 && s.Diag.Agl > 1000) { t1000 = t; break; }
            if (s.Crashed.Length > 0) break;
        }
        Console.WriteLine($"DECOLAGEM {id,-6} manete {throttle:0.00} | sai do chao em {liftT:0.0}s a {liftIas:0} kt | corrida {roll:0} m | 1000 m em {t1000:0.0}s | Gmax {maxG:0.0} | queda: '{s.Crashed}'");
    }

    static void MaxSpeed(string id, double z, double V0, double seconds)
    {
        var a = AircraftDb.Get(id).Prepare();
        var s = FlightModel.SpawnAir(a, -0.28, -0.85, z, 0, 0.68, V0, 0.5);
        var c = new Controls { Throttle = 1, Trim = s.Trim, GearDown = a.GearFixed };
        const double h = 1.0 / 120;
        for (int i = 0; i < seconds * 120; i++)
        {
            FlightModel.Euler(s.Q, out double phi, out double th, out _);
            double vz = -s.V.Z, zErr = z - s.Z;
            c.Pitch = FlightModel.Clamp((zErr * 0.002 - vz * 0.02) * 2, -0.6, 0.6);
            c.Roll = FlightModel.Clamp(-phi * 0.8, -0.3, 0.3);
            FlightModel.Step(s, a, c, h, 0);
            if (s.Crashed.Length > 0) break;
        }
        Console.WriteLine($"VELOCIDADE {id,-6} a {z:0} m | Mach {s.Diag.Mach:0.00} | IAS {s.Diag.Ias:0} kt | TAS {s.Diag.Tas:0} kt | alt {s.Z:0} m | queda: '{s.Crashed}'");
    }

    static void Stall(string id, double z, double V0)
    {
        var a = AircraftDb.Get(id).Prepare();
        var s = FlightModel.SpawnAir(a, -0.28, -0.85, z, 0, 0.68, V0, 0.5);
        var c = new Controls { Throttle = 0, Trim = s.Trim, GearDown = a.GearFixed };
        const double h = 1.0 / 120;
        double warnIas = -1, minIas = 1e9, sepIas = -1;
        for (int i = 0; i < 90 * 120; i++)
        {
            double zErr = z - s.Z, vz = -s.V.Z;
            c.Pitch = FlightModel.Clamp(c.Pitch + (zErr * 0.002 - vz * 0.02) * h * 3, -0.2, 1);
            FlightModel.Step(s, a, c, h, 0);
            minIas = Math.Min(minIas, s.Diag.Ias);
            if (warnIas < 0 && s.Diag.StallWarn) warnIas = s.Diag.Ias;
            if (sepIas < 0 && s.Diag.Sep > 0.3) { sepIas = s.Diag.Ias; break; }
            if (s.Crashed.Length > 0) break;
        }
        Console.WriteLine($"ESTOL {id,-6} aviso a {warnIas:0} kt | estola a {sepIas:0} kt | IAS minima {minIas:0} kt | queda: '{s.Crashed}'");
    }

    static void Maneuver(string id, double V, double z, double pitch, double roll)
    {
        var a = AircraftDb.Get(id).Prepare();
        var s = FlightModel.SpawnAir(a, -0.28, -0.85, z, 0, 0.68, V, 0.5);
        var c = new Controls { Throttle = s.Throttle, Trim = s.Trim, GearDown = a.GearFixed };
        const double h = 1.0 / 120;
        double maxNz = 0, minNz = 0, tMax = 0, crashT = -1;
        for (int i = 0; i < 120 * 12; i++)
        {
            double t = i * h;
            c.Pitch = t > 1 && t < 5 ? pitch : t > 7 && t < 9 ? -pitch * 0.6 : 0;
            c.Roll = t > 4 && t < 7 ? roll : t > 9 && t < 11 ? -roll : 0;
            FlightModel.Step(s, a, c, h, 0);
            if (s.Diag.Nz > maxNz) { maxNz = s.Diag.Nz; tMax = t; }
            minNz = Math.Min(minNz, s.Diag.Nz);
            if (s.Crashed.Length > 0) { crashT = t; break; }
        }
        Console.WriteLine($"MANOBRA {id,-6} {V * 1.944:0} kt a {z:0} m manche {pitch:0.0}/{roll:0.0} | G {minNz:0.0} a {maxNz:0.0} (pico {tMax:0.0}s) | limite {a.NzLimitHigh * 1.5:0.0} | queda: '{s.Crashed}' {(crashT >= 0 ? crashT.ToString("0.0") + "s" : "")}");
    }

    sealed class Rig
    {
        public CombatSession C;
        public V3 Fwd = new V3(0, 1, 0), Pos;
        public double Spd = 200;
        public Rig(string id, MissionSpec m, Difficulty d = null, int seed = 7, bool invuln = true)
        {
            C = new CombatSession((e, n) => 900 + 40 * Math.Sin(e / 3000) * Math.Cos(n / 2500), AircraftDb.DefaultLoadout(id), m, d ?? Difficulty.Normal, seed) { Invuln = invuln };
        }
        public void Tick(int n, Action<int> hook = null)
        {
            for (int i = 0; i < n; i++)
            {
                hook?.Invoke(i);
                V3 vel = Fwd * Spd;
                Pos += vel * (1.0 / 30);
                V3 r = Cm.Norm(V3.Cross(Fwd, new V3(0, 0, 1)));
                C.SetPlayer(Pos, vel, Fwd, V3.Cross(r, Fwd), r, Pos.Z - 900, false);
                C.Step(1.0 / 30);
            }
        }
        public void Quiet() { foreach (var g in C.Gnd) if (g.Kind == "sam" || g.Kind == "aaa") { g.Cd = 1e9; g.Burst = 0; } }
    }

    static MissionSpec Base() => new MissionSpec { Id = "t", Wingman = false, GroundSite = new V3(0, 9000, 0) };
    static MissionSpec Empty() => new MissionSpec { Id = "t", Wingman = false, Waves = { new WaveSpec { Delay = 1e9 } } };

    static void Report(string name, bool ok, string detail) => Console.WriteLine($"{(ok ? "OK   " : "FALHA")} {name}: {detail}");

    static void Combat()
    {
        var r = new Rig("f39e", Base()); r.Pos = new V3(0, 0, 2000); r.Tick(15); r.Quiet();
        var h = r.C.Gnd.First(g => g.Kind == "hangar");
        r.Pos = new V3(h.Pos.X + 15, h.Pos.Y - 5500, 1700); r.C.Sel = WeaponKind.Bomb; r.C.Tgt = h; r.Tick(2);
        int b0 = r.C.W.Bomb; r.C.InPickle = true; r.C.Fire(); bool armed = r.C.Pickle, held = r.C.W.Bomb == b0;
        r.Tick(30 * 40);
        Report("CCRP segurando", armed && held && !h.Alive, $"armou={armed} reteve={held} bombas={r.C.W.Bomb} hangar vivo={h.Alive}");

        r = new Rig("f39e", Base()); r.Pos = new V3(0, 0, 2000); r.Tick(15); r.Quiet();
        var rd = r.C.Gnd.First(g => g.Kind == "radar");
        r.Pos = new V3(rd.Pos.X, rd.Pos.Y - 5000, 1700); r.C.Sel = WeaponKind.Bomb; r.C.Tgt = rd; r.Tick(2);
        b0 = r.C.W.Bomb; r.C.InPickle = true; r.C.Fire(); r.Tick(30); r.C.InPickle = false; r.Tick(30 * 30);
        Report("CCRP soltou antes", r.C.W.Bomb == b0, $"bombas {b0} -> {r.C.W.Bomb}");

        r = new Rig("f39e", Base()); r.Pos = new V3(0, 0, 2000); r.Tick(15); r.Quiet();
        rd = r.C.Gnd.First(g => g.Kind == "radar");
        r.Pos = new V3(rd.Pos.X + 400, rd.Pos.Y - 5000, 1700); r.C.Sel = WeaponKind.Bomb; r.C.Tgt = rd; r.Tick(2);
        b0 = r.C.W.Bomb; r.C.InPickle = true; r.C.Fire(); r.Tick(30 * 30);
        Report("CCRP fora do eixo", r.C.W.Bomb == b0, $"bombas {b0} -> {r.C.W.Bomb}");

        r = new Rig("f39e", Base()); r.Pos = new V3(0, 0, 2000); r.Tick(15); r.Quiet();
        rd = r.C.Gnd.First(g => g.Kind == "radar");
        r.Pos = new V3(rd.Pos.X, rd.Pos.Y - 5000, 1700); r.C.Sel = WeaponKind.Bomb; r.C.Tgt = rd; r.Tick(2);
        b0 = r.C.W.Bomb; r.C.InPickle = true; r.C.Fire(); r.Tick(2); r.C.InPickle = false; r.Tick(3);
        Report("Toque rapido solta", r.C.W.Bomb == b0 - 1, $"bombas {b0} -> {r.C.W.Bomb}");

        r = new Rig("a29", Base()); r.Pos = new V3(0, 0, 2000); r.Tick(15); r.Quiet();
        var hg = r.C.Gnd.First(g => g.Kind == "hangar");
        r.Pos = new V3(hg.Pos.X, hg.Pos.Y - 4000, 1500); r.C.Sel = WeaponKind.Bomb; r.C.Tgt = hg; r.Tick(2);
        b0 = r.C.W.Bomb; bool ccrpA29 = r.C.Sol != null && r.C.Sol.Ccrp; r.C.Fire();
        Report("A-29 sem CCRP", !ccrpA29 && r.C.W.Bomb == b0 - 1, $"ccrp={ccrpA29} bombas {b0} -> {r.C.W.Bomb}");

        r = new Rig("f39e", Base()); r.Pos = new V3(0, 0, 2000); r.Tick(15); r.Quiet();
        var tr = r.C.Gnd.First(g => g.Kind == "truck");
        r.Pos = new V3(tr.Pos.X, tr.Pos.Y - 4200, 1500); r.C.Sel = WeaponKind.Bomb; r.C.Tgt = tr; r.Tick(2); r.C.InPickle = true; r.C.Fire();
        double hdg = 0; int b1 = r.C.W.Bomb;
        r.Tick(30 * 30, _ => { if (r.C.Sol?.Target != null && r.C.Pickle) { hdg += Cm.Clamp(r.C.Sol.Steer * 0.05, -0.01, 0.01); r.Fwd = new V3(Math.Sin(hdg), Math.Cos(hdg), 0); } });
        Report("CCRP em alvo movel", !tr.Alive, $"vivo={tr.Alive} bombas {b1} -> {r.C.W.Bomb}");

        r = new Rig("f39e", Base()); r.Pos = new V3(0, 0, 2000); r.Tick(15); r.Quiet();
        var rr = r.C.Gnd.First(g => g.Kind == "radar");
        double ang = 14 * Math.PI / 180; r.Pos = new V3(rr.Pos.X, rr.Pos.Y - 3000, 900 + 3000 * Math.Tan(ang)); r.C.Sel = WeaponKind.Rkt; r.C.Tgt = rr; int shots = 0;
        r.Tick(30 * 11, i => { r.Fwd = new V3(0, Math.Cos(ang), -Math.Sin(ang)); if (r.C.Sol?.Target != null) { ang += Cm.Clamp(r.C.Sol.Along * -0.00002, -0.004, 0.004); if (r.C.Sol.OnTarget && shots < 5 && i % 8 == 0) { r.C.Fire(); shots++; } } });
        Report("Foguetes com ATIRE", !rr.Alive && shots > 0, $"salvas={shots} radar vivo={rr.Alive} hp={rr.Hp:0}");

        r = new Rig("f39e", Base()); r.Pos = new V3(0, 0, 2000); r.Tick(15); r.Quiet();
        var tk = r.C.Gnd.First(g => g.Kind == "tank");
        r.Pos = new V3(tk.Pos.X, tk.Pos.Y - 1800, 1100); r.C.Tgt = tk; r.C.InGun = true;
        r.Tick(150, i => { if (r.C.GunLead(out V3 d, out _, out _)) r.Fwd = d; });
        Report("Canhao em blindado", !tk.Alive, $"vivo={tk.Alive} disparos={r.C.Shots} acertos={r.C.Hits}");

        r = new Rig("f39e", Empty()); r.Pos = new V3(0, 0, 3000); r.Tick(1);
        var e = r.C.SpawnUnit(Team.Red, "Mirage III", "Argentina", "Mirage 1", new V3(200, 4000, 3100), new V3(0, 1, 0), 200, 0.2);
        e.MslN = 0; e.GunCd = 1e9; e.FlareN = 0; r.C.Sel = WeaponKind.Aam; r.C.Tgt = e; r.Tick(60); bool lk = r.C.LockOk; r.C.Fire(); r.Tick(240);
        Report("Missil abate Mirage", lk && e.Dying > 0, $"trava={lk} abatido={e.Dying > 0} kills={r.C.KillsAir} pontos={r.C.Score}");

        foreach (var (nome, d) in new[] { ("facil", Difficulty.Facil), ("normal", Difficulty.Normal), ("veterano", Difficulty.Veterano) })
        {
            r = new Rig("f39e", Empty(), d, 11, false); r.Pos = new V3(0, 0, 3000); r.Spd = 220; r.Tick(1);
            r.C.SpawnUnit(Team.Red, "Mirage III", "Argentina", "Mirage 1", new V3(0, -900, 3000), new V3(0, 1, 0), 215, d.Skill);
            r.C.SpawnUnit(Team.Red, "F/A-18E", "EUA", "F/A-18 1", new V3(300, -1400, 3100), new V3(0, 1, 0), 215, d.Skill);
            r.Tick(30 * 30);
            double hpReto = r.C.Pl.Hp;
            r = new Rig("f39e", Empty(), d, 11, false); r.Pos = new V3(0, 0, 3000); r.Spd = 220; r.Tick(1);
            r.C.SpawnUnit(Team.Red, "Mirage III", "Argentina", "Mirage 1", new V3(0, -900, 3000), new V3(0, 1, 0), 215, d.Skill);
            r.C.SpawnUnit(Team.Red, "F/A-18E", "EUA", "F/A-18 1", new V3(300, -1400, 3100), new V3(0, 1, 0), 215, d.Skill);
            r.Tick(30 * 60, i => { double t = i / 30.0, a = Math.Sin(t * 0.8) * 2.4; r.Fwd = Cm.Norm(new V3(Math.Sin(a), Math.Cos(a), 0.12 * Math.Sin(t * 1.3))); if (i % 180 == 150) r.C.Flare(); });
            double soma = 0; int vivos = 0;
            for (int seed = 1; seed <= 12; seed++)
            {
                r = new Rig("f39e", Empty(), d, seed, false); r.Pos = new V3(0, 0, 3000); r.Spd = 220; r.Tick(1);
                r.C.SpawnUnit(Team.Red, "Mirage III", "Argentina", "Mirage 1", new V3(0, -900, 3000), new V3(0, 1, 0), 215, d.Skill);
                r.C.SpawnUnit(Team.Red, "F/A-18E", "EUA", "F/A-18 1", new V3(300, -1400, 3100), new V3(0, 1, 0), 215, d.Skill);
                r.Tick(30 * 60, i => { double t = i / 30.0, a2 = Math.Sin(t * 0.8) * 2.4; r.Fwd = Cm.Norm(new V3(Math.Sin(a2), Math.Cos(a2), 0.12 * Math.Sin(t * 1.3))); if (i % 180 == 150) r.C.Flare(); });
                soma += r.C.Pl.Hp; if (r.C.Pl.Alive) vivos++;
            }
            Console.WriteLine($"INFO  IA {nome,-8}: reto 30 s = casco {hpReto:0}; manobrando 60 s, 12 lutas = casco medio {soma / 12:0}, sobreviveu {vivos}/12");
        }

        r = new Rig("f39e", Empty(), null, 3, false); r.Pos = new V3(0, 0, 3000); r.Tick(1);
        var cs = r.C.SpawnUnit(Team.Red, "Cessna 172", "NarcotrÃ¡fico", "Cessna 1", new V3(0, 800, 3000), new V3(1, 0, 0), 62, 0.8);
        r.Tick(30 * 30);
        Report("Cessna nao atira", r.C.Pl.Hp == 100 && r.C.Bullets.Count == 0 && r.C.Missiles.Count == 0, $"casco={r.C.Pl.Hp:0} velocidade={cs.Spd:0} m/s");

        var ms = new MissionSpec { Id = "t", Wingman = false };
        ms.Waves.Add(new WaveSpec { Delay = 1, Groups = { new SpawnSpec { Tipo = "Mirage III", Forca = "Argentina", Count = 2, Pos = new V3(0, 6000, 3000) } }, Radio = "Contato" });
        r = new Rig("f39e", ms); r.Pos = new V3(0, 0, 3000); r.Tick(90);
        int n = r.C.Air.Count; string forca = n > 0 ? r.C.Air[0].Forca : "?";
        foreach (var a in r.C.Air.ToArray()) a.Hp = 0.01;
        r.C.InGun = false;
        foreach (var a in r.C.Air) { a.Dying = 0.001; a.Mode = AiMode.Dying; }
        r.Tick(30 * 20);
        Report("Onda e vitoria", n == 2 && forca == "Argentina" && r.C.Over == Outcome.Win, $"inimigos={n} forca={forca} resultado={r.C.Over}");

        foreach (var (nome2, d2) in new[] { ("normal", Difficulty.Normal), ("veterano", Difficulty.Veterano) })
        {
            double soma2 = 0; int vivos2 = 0, abates = 0;
            for (int seed = 1; seed <= 12; seed++)
            {
                r = new Rig("f39e", Empty(), d2, seed, false); r.Pos = new V3(0, 0, 3000); r.Spd = 220; r.Tick(1);
                r.C.SpawnUnit(Team.Red, "F-22A", "EUA", "F-22 1", new V3(0, -1500, 3200), new V3(0, 1, 0), 260, d2.Skill);
                r.Tick(30 * 60, i => { double t = i / 30.0, a3 = Math.Sin(t * 0.8) * 2.4; r.Fwd = Cm.Norm(new V3(Math.Sin(a3), Math.Cos(a3), 0.12 * Math.Sin(t * 1.3))); if (i % 180 == 150) r.C.Flare(); });
                soma2 += r.C.Pl.Hp; if (r.C.Pl.Alive) vivos2++; abates += r.C.KillsAir;
            }
            Console.WriteLine($"INFO  F-22 {nome2,-8}: 1 F-22, manobrando 60 s, 12 lutas = casco medio {soma2 / 12:0}, sobreviveu {vivos2}/12");
        }

        r = new Rig("f39e", new MissionSpec { Id = "t", Wingman = true, Waves = { new WaveSpec { Delay = 1e9 } } }); r.Pos = new V3(0, 0, 3000); r.Tick(20);
        Report("Ala surge", r.C.Wingman != null && r.C.Wingman.Model == "F39E_ALA", $"modelo={r.C.Wingman?.Model}");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        r = new Rig("f39e", new MissionSpec { Id = "t", Wingman = true, Endless = true }, null, 5, true); r.Pos = new V3(0, 0, 3000); r.C.InGun = true;
        r.Tick(30 * 120, i => { if (i % 90 == 0) { r.C.W.Gun = 500; } });
        Console.WriteLine($"INFO  custo: 120 s de arena em {sw.ElapsedMilliseconds} ms | no ar {r.C.Air.Count} | onda {r.C.Wave} | projeteis {r.C.Bullets.Count}");
    }

    public static void Run()
    {
        Takeoff("f39e", 130, 12, 1.0);
        Takeoff("f39e", 135, 12, 0.85);
        Takeoff("a29", 80, 6, 0.9);
        Takeoff("a29", 80, 9, 1.0);
        Takeoff("kc390", 120, 9, 1.0);
        MaxSpeed("f39e", 11000, 280, 240);
        MaxSpeed("f39e", 500, 200, 120);
        MaxSpeed("a29", 3000, 100, 120);
        MaxSpeed("kc390", 9000, 200, 180);
        Stall("f39e", 3000, 120);
        Stall("a29", 3000, 70);
        Stall("kc390", 3000, 90);
        Maneuver("f39e", 180, 3000, 0.6, 0.7);
        Maneuver("f39e", 180, 3000, 1.0, 1.0);
        Maneuver("f39e", 300, 2000, 1.0, 1.0);
        Maneuver("f39e", 120, 3000, 1.0, 1.0);
        Maneuver("f39e", 250, 1000, -1.0, 1.0);
        Maneuver("f39e", 400, 500, 1.0, -1.0);
        Maneuver("a29", 110, 2000, 0.6, 0.7);
        Maneuver("a29", 140, 2000, 1.0, 1.0);
        Maneuver("kc390", 150, 4000, 0.6, 0.7);
        Combat();
    }
}
