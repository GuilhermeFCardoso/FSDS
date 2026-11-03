using System;
using System.Collections.Generic;

namespace FABSim.Core
{
    public static class Cm
    {
        public const double G0 = 9.80665;
        public static double Clamp(double x, double a, double b) => x < a ? a : x > b ? b : x;
        public static double Dot(V3 a, V3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static V3 Norm(V3 a) { double l = a.Length; if (l == 0) l = 1; return new V3(a.X / l, a.Y / l, a.Z / l); }
        public static double Dist(V3 a, V3 b) => (a - b).Length;
        public static double Angle(V3 a, V3 b) { double k = a.Length * b.Length; if (k == 0) k = 1; return Math.Acos(Clamp(Dot(a, b) / k, -1, 1)); }
        public static double WrapA(double a) => Math.Atan2(Math.Sin(a), Math.Cos(a));

        public static V3 TurnToward(V3 dir, V3 want, double max)
        {
            V3 w = want;
            double ang = Math.Acos(Clamp(Dot(dir, w), -1, 1));
            if (ang <= max) return Norm(w);
            if (ang > Math.PI - 1e-3) { w = Norm(V3.Cross(dir, Math.Abs(dir.Z) > 0.95 ? new V3(1, 0, 0) : new V3(0, 0, 1))); ang = Math.PI / 2; }
            double s = Math.Sin(ang);
            return Norm(dir * (Math.Sin(ang - max) / s) + w * (Math.Sin(max) / s));
        }

        public static void Lead(V3 P, V3 Vel, V3 T, V3 VT, double vb, out V3 dir, out double t, out double range)
        {
            t = Dist(P, T) / vb;
            V3 aim = T - P;
            for (int i = 0; i < 5; i++)
            {
                aim = (T - P) + (VT - Vel) * t + new V3(0, 0, 0.5 * G0 * t * t);
                t = aim.Length / vb;
            }
            dir = Norm(aim);
            range = Dist(P, T);
        }

        public static void SegDist(V3 p0, V3 p1, V3 c, out double d, out V3 q)
        {
            V3 s = p1 - p0;
            double dd = Dot(s, s), t = dd > 0 ? Clamp(Dot(c - p0, s) / dd, 0, 1) : 0;
            q = p0 + s * t;
            d = Dist(q, c);
        }
    }

    public enum Team { Blue, Red }
    public enum WeaponKind { Gun, Aam, Bomb, Rkt }
    public enum AiMode { Patrol, Attack, Break, Extend, Notch, Dying }
    public enum EventType { Boom, Spark, Dust, Launch, Drop, Hurt, Hit, GunWarn, Select, NoLock, Flare, Rearm, Radio, Wreck, Gone }
    public enum MsgKind { Info, Kill, Bad, Warn, Good }
    public enum Outcome { None, Win, Lose }

    public struct CombatEvent { public EventType Type; public V3 Pos; public double Size, Dmg; public bool Ground; public int Id; public string Kind; }
    public struct CombatMsg { public string Text; public MsgKind Kind; }

    public sealed class GunDef { public string Name; public int Rounds; public double Rate, V, Dmg, Spread; }
    public sealed class SightDef { public string Name; public bool Ccrp, Full; public double Range; }
    public sealed class Loadout { public GunDef Gun; public int Aam, Bomb, Rkt, Flares; public SightDef Sight; }

    public sealed class UnitProfile
    {
        public string Tipo, Model, Name;
        public bool Armed = true, Heli;
        public double Hp = 100, R = 7, GmaxBase = 5, GmaxSkill = 2.2, Cruise = 205, VMin = 105, VMax = 330;
        public int Missiles = 2, Flares = 14;

        static readonly Dictionary<string, UnitProfile> table = new Dictionary<string, UnitProfile>
        {
            ["F/A-18E"] = new UnitProfile { Tipo = "F/A-18E", Model = "EUA_FA18", Name = "F/A-18", Cruise = 200, VMax = 310, R = 8, Missiles = 3 },
            ["F-22A"] = new UnitProfile { Tipo = "F-22A", Model = "EUA_F22", Name = "F-22", Hp = 130, R = 8, GmaxBase = 6.5, GmaxSkill = 2.5, Cruise = 260, VMin = 95, VMax = 420, Missiles = 6, Flares = 30 },
            ["P-8"] = new UnitProfile { Tipo = "P-8", Model = "EUA_P8", Name = "P-8", Armed = false, Hp = 220, R = 18, GmaxBase = 1.8, GmaxSkill = 0.3, Cruise = 210, VMin = 90, VMax = 245, Missiles = 0, Flares = 24 },
            ["Mirage III"] = new UnitProfile { Tipo = "Mirage III", Model = "ARG_Mirage3", Name = "Mirage", GmaxBase = 4, R = 7, Cruise = 205, VMax = 340, Missiles = 2, Flares = 10 },
            ["A-4X"] = new UnitProfile { Tipo = "A-4X", Model = "ARG_A4X", Name = "A-4", Cruise = 170, VMax = 280, R = 6, GmaxBase = 4, Missiles = 1 },
            ["Cessna 172"] = new UnitProfile { Tipo = "Cessna 172", Model = "NARCO_Cessna172", Name = "Cessna 172", Armed = false, Hp = 30, R = 5, GmaxBase = 2.5, GmaxSkill = 0.5, Cruise = 55, VMin = 26, VMax = 66, Missiles = 0, Flares = 0 },
            ["Cessna 421"] = new UnitProfile { Tipo = "Cessna 421", Model = "NARCO_Cessna421", Name = "Cessna 421", Armed = false, Hp = 45, R = 6, GmaxBase = 2.5, GmaxSkill = 0.5, Cruise = 90, VMin = 38, VMax = 118, Missiles = 0, Flares = 0 },
            ["drone_de_ataque_ficticio"] = new UnitProfile { Tipo = "drone_de_ataque_ficticio", Model = "ARG_A4X", Name = "Drone", Hp = 40, R = 5, GmaxBase = 3, GmaxSkill = 0.5, Cruise = 150, VMax = 220, Missiles = 0, Flares = 0 },
            ["aeronave_nao_cooperativa"] = new UnitProfile { Tipo = "aeronave_nao_cooperativa", Model = "EUA_P8", Name = "Não cooperativa", Armed = false, Hp = 150, R = 18, GmaxBase = 1.8, GmaxSkill = 0.3, Cruise = 200, VMin = 90, VMax = 245, Missiles = 0, Flares = 10 },
            ["MH-47G"] = new UnitProfile { Tipo = "MH-47G", Model = "EUA_MH47", Name = "MH-47G", Armed = false, Heli = true, Hp = 140, R = 11, GmaxBase = 1.5, GmaxSkill = 0.2, Cruise = 82, VMin = 30, VMax = 90, Missiles = 0, Flares = 24 },
            ["MH-60M"] = new UnitProfile { Tipo = "MH-60M", Model = "EUA_MH60", Name = "MH-60M", Armed = false, Heli = true, Hp = 90, R = 8, GmaxBase = 1.8, GmaxSkill = 0.3, Cruise = 78, VMin = 30, VMax = 90, Missiles = 0, Flares = 24 },
            ["Ala"] = new UnitProfile { Tipo = "Ala", Model = "F39E_ALA", Name = "Ala", Cruise = 215, Missiles = 4 },
        };
        static readonly UnitProfile fallback = new UnitProfile { Tipo = "?", Model = "EUA_FA18", Name = "Inimigo" };
        public static UnitProfile Get(string tipo) => tipo != null && table.TryGetValue(tipo, out var p) ? p : fallback;
    }

    public sealed class SpawnSpec { public string Tipo, Forca; public int Count; public V3 Pos; }
    public sealed class WaveSpec { public double Delay; public List<SpawnSpec> Groups = new List<SpawnSpec>(); public string Radio; }
    public sealed class MissionSpec
    {
        public bool SemInimigos;
        public V3? AlvoHeli;
        public string Id, Title;
        public bool Wingman = true, Endless;
        public V3? GroundSite;
        public List<WaveSpec> Waves = new List<WaveSpec>();
    }
    public sealed class Difficulty
    {
        public double Skill, Dmg, Aaa;
        public static readonly Difficulty Facil = new Difficulty { Skill = 0.5, Dmg = 0.45, Aaa = 0.5 };
        public static readonly Difficulty Normal = new Difficulty { Skill = 0.8, Dmg = 1, Aaa = 1 };
        public static readonly Difficulty Veterano = new Difficulty { Skill = 1.1, Dmg = 1.4, Aaa = 1.5 };
    }

    public abstract class Entity
    {
        public int Id;
        public string Name;
        public V3 Pos, Vel;
        public double R, Hp, HpMax;
        public abstract bool Valid { get; }
    }

    public sealed class PlayerEntity : Entity
    {
        public V3 Fwd = new V3(0, 1, 0), Up = new V3(0, 0, 1), Right = new V3(1, 0, 0);
        public double Spd, Agl;
        public bool Alive = true, Grounded;
        public override bool Valid => Alive;
    }

    public sealed class AirUnit : Entity
    {
        public Team Team;
        public string Model, Forca;
        public UnitProfile Profile;
        public V3 Dir;
        public double Spd, Gmax, Cruise, VMin, VMax, Skill, Mt, T, Rt, GunCd, Burst, GunAcc, ErrA, ErrB, MslCd, FlareCd, Roll, BrkSide, Dying, GT, GHere, GAhead;
        public int MslN, FlareN;
        public AiMode Mode = AiMode.Patrol;
        public Entity Tgt;
        public V3? Wp;
        public bool Gone;
        public override bool Valid => Dying == 0 && Hp > 0;
    }

    public sealed class GroundUnit : Entity
    {
        public string Kind;
        public double Yaw, GT, Cd, Burst, Acc, MoveE, MoveN;
        public V3 Bias;
        public int Pts, Ammo = 3;
        public bool Primary, Alive = true;
        public override bool Valid => Alive;
        public bool Moving => MoveE != 0 || MoveN != 0;
    }

    public sealed class FlareObj : Entity
    {
        public double Life = 5.5, Age;
        public Team Team;
        public bool Dead;
        public override bool Valid => !Dead;
    }

    public sealed class Bullet { public V3 P, V; public double Life, Dmg; public Team Team; public bool Tracer, Assist; public string By; public int I = -1; }
    public sealed class Missile { public int Id; public string Kind, Src; public Team Team; public V3 Pos, Vel, Dir; public double Spd, Life, Age, MaxG, Fov; public Entity Tgt; public bool Dead, Lost, HasFuse; public double FuseD; public V3 FuseQ; }
    public sealed class Rocket { public int Id; public V3 Pos, Dir, Inh, Vel; public double Spd, Age, Life; public bool Dead; }
    public sealed class Bomb { public int Id; public V3 Pos, Vel; public bool Dead; }

    public sealed class FireSolution
    {
        public WeaponKind Kind;
        public bool HasImpact, InRange, TooClose, OnTarget, Ccrp;
        public V3 Impact;
        public double Tof, Range, Along, Cross, Miss, Dist, Ttr, Steer, Rad;
        public GroundUnit Target;
    }

    public sealed class Ammo { public int Gun, Aam, Bomb, Rkt, Flares; public double GunAcc; public int N; }

    public sealed class CombatSession
    {
        public const double RktMin = 400, RktMax = 3200;
        readonly Func<double, double, double> groundAt;
        readonly Random rng;
        readonly Loadout load;
        readonly Difficulty diff;
        readonly MissionSpec mission;

        public readonly PlayerEntity Pl = new PlayerEntity { Name = "Você", R = 7, Hp = 100, HpMax = 100 };
        public readonly List<AirUnit> Air = new List<AirUnit>();
        public readonly List<GroundUnit> Gnd = new List<GroundUnit>();
        public readonly List<Bullet> Bullets = new List<Bullet>();
        public readonly List<Missile> Missiles = new List<Missile>();
        public readonly List<Rocket> Rockets = new List<Rocket>();
        public readonly List<Bomb> Bombs = new List<Bomb>();
        public readonly List<FlareObj> Flares = new List<FlareObj>();
        public readonly List<CombatEvent> Events = new List<CombatEvent>();
        public readonly List<CombatMsg> Msgs = new List<CombatMsg>();
        public readonly List<WeaponKind> Avail = new List<WeaponKind>();
        public readonly Ammo W = new Ammo();
        public bool HeliNoAlvo;

        public bool InGun, InPickle, Firing, Pickle, Invuln, Assist;
        public WeaponKind Sel = WeaponKind.Gun;
        public Entity Tgt;
        public double LockT;
        public bool LockOk;
        public Missile WarnMsl;
        public double WarnD = 1e9;
        public FireSolution Sol;
        public int Score, KillsAir, KillsGnd, Shots, Hits, PrimLeft, PrimTotal, Wave;
        public double T, Taken;
        public Outcome Over;
        public AirUnit Wingman;
        public V3 Home;

        int nid, frame;
        double gTop = double.NegativeInfinity, waveT, rearmT, pickleT;
        bool wingSpawned, baseBuilt;
        readonly List<Entity> foesBlue = new List<Entity>(), foesRed = new List<Entity>();

        public CombatSession(Func<double, double, double> groundAt, Loadout load, MissionSpec mission, Difficulty diff, int? seed = null)
        {
            this.groundAt = groundAt;
            this.load = load;
            this.mission = mission;
            this.diff = diff ?? Difficulty.Normal;
            rng = seed.HasValue ? new Random(seed.Value) : new Random();
            W.Gun = load.Gun.Rounds; W.Aam = load.Aam; W.Bomb = load.Bomb; W.Rkt = load.Rkt; W.Flares = load.Flares;
            Avail.Add(WeaponKind.Gun);
            if (load.Aam > 0) Avail.Add(WeaponKind.Aam);
            if (load.Bomb > 0) Avail.Add(WeaponKind.Bomb);
            if (load.Rkt > 0) Avail.Add(WeaponKind.Rkt);
            waveT = mission.Waves.Count > 0 ? mission.Waves[0].Delay : 18;
        }

        double Rnd(double a, double b) => a + rng.NextDouble() * (b - a);
        double Gauss() => rng.NextDouble() + rng.NextDouble() + rng.NextDouble() - 1.5;
        void Say(string text, MsgKind kind = MsgKind.Info) => Msgs.Add(new CombatMsg { Text = text, Kind = kind });
        void Ev(EventType t, V3 pos = default, double size = 0, bool ground = false, double dmg = 0, int id = 0, string kind = null) =>
            Events.Add(new CombatEvent { Type = t, Pos = pos, Size = size, Ground = ground, Dmg = dmg, Id = id, Kind = kind });
        double GAt(V3 p) { double h = groundAt(p.X, p.Y); if (h > gTop) gTop = h; return h; }
        bool NearGround(V3 p) => p.Z < gTop + 300;
        bool GroundSel => Sel == WeaponKind.Bomb || Sel == WeaponKind.Rkt;

        public void SetPlayer(V3 pos, V3 vel, V3 fwd, V3 up, V3 right, double agl, bool grounded)
        {
            Pl.Pos = pos; Pl.Vel = vel; Pl.Spd = vel.Length; Pl.Fwd = fwd; Pl.Up = up; Pl.Right = right; Pl.Agl = agl; Pl.Grounded = grounded;
        }

        void BuildFoes()
        {
            foesBlue.Clear(); foesRed.Clear();
            foreach (var a in Air) if (a.Dying == 0) (a.Team == Team.Red ? foesBlue : foesRed).Add(a);
            if (Pl.Alive) foesRed.Add(Pl);
            foreach (var g in Gnd) if (g.Alive) foesBlue.Add(g);
        }

        public AirUnit SpawnUnit(Team team, string tipo, string forca, string name, V3 pos, V3 dir, double spd, double skill)
        {
            var p = UnitProfile.Get(tipo);
            V3 nd = Cm.Norm(dir);
            var e = new AirUnit
            {
                Id = ++nid, Team = team, Model = p.Model, Forca = forca, Profile = p, Name = name, Pos = pos, Dir = nd, Spd = spd, Vel = nd * spd,
                Hp = p.Hp, HpMax = p.Hp, R = p.R, Gmax = p.GmaxBase + p.GmaxSkill * skill, Cruise = p.Cruise, VMin = p.VMin, VMax = p.VMax, Skill = skill,
                GunCd = Rnd(0, 2), MslN = p.Missiles, MslCd = Rnd(5, 12), FlareN = p.Flares, BrkSide = rng.NextDouble() < 0.5 ? -1 : 1,
            };
            Air.Add(e);
            return e;
        }

        Entity PickTarget(AirUnit e)
        {
            if (!e.Profile.Armed) return null;
            Entity best = null;
            double bd = e.Team == Team.Red ? 26000 : 14000;
            foreach (var f in e.Team == Team.Red ? foesRed : foesBlue)
            {
                if (f is GroundUnit) continue;
                double d = Cm.Dist(e.Pos, f.Pos) * (f is PlayerEntity ? 0.75 : 1);
                if (d < bd) { bd = d; best = f; }
            }
            return best;
        }

        Missile Incoming(Entity e, out double dist)
        {
            Missile best = null;
            dist = 6000;
            foreach (var m in Missiles)
            {
                if (m.Tgt != e || m.Dead) continue;
                double d = Cm.Dist(m.Pos, e.Pos);
                if (d < dist) { dist = d; best = m; }
            }
            return best;
        }

        bool DropFlare(Entity e)
        {
            var air = e as AirUnit;
            if (air == null ? W.Flares <= 0 : air.FlareN <= 0) return false;
            if (air == null) W.Flares--; else air.FlareN--;
            Team team = air == null ? Team.Blue : air.Team;
            V3 dir = air == null ? Pl.Fwd : air.Dir;
            for (int i = 0; i < 2; i++)
            {
                Flares.Add(new FlareObj
                {
                    Id = ++nid, Team = team, Name = "Flare", R = 2, Pos = e.Pos - dir * 8,
                    Vel = e.Vel + V3.Cross(dir, new V3(0, 0, 1)) * ((i == 1 ? 1 : -1) * Rnd(8, 18)) + new V3(0, 0, Rnd(-12, -4)),
                });
            }
            return true;
        }

        void Hurt(Entity t, double dmg, string by)
        {
            if (t is PlayerEntity)
            {
                if (!Pl.Alive) return;
                double d = dmg * diff.Dmg;
                Taken += d;
                if (!Invuln) Pl.Hp -= d;
                Ev(EventType.Hurt, dmg: d);
                if (Pl.Hp <= 0)
                {
                    Pl.Alive = false; Pl.Hp = 0;
                    Ev(EventType.Boom, Pl.Pos, 3);
                    Say("Sua aeronave foi destruída" + (by != null ? " por " + by : ""), MsgKind.Bad);
                    if (Over == Outcome.None) Over = Outcome.Lose;
                }
                return;
            }
            if (t is GroundUnit g)
            {
                if (!g.Alive) return;
                g.Hp -= dmg;
                if (g.Hp <= 0) KillGnd(g);
                return;
            }
            if (t is AirUnit a)
            {
                if (a.Dying > 0) return;
                a.Hp -= dmg;
                if (a.Hp <= 0) KillAir(a, by);
            }
        }

        void KillAir(AirUnit e, string by)
        {
            e.Dying = 0.001; e.Mode = AiMode.Dying;
            Ev(EventType.Boom, e.Pos, 1.4);
            if (e.Team == Team.Red)
            {
                if (by == "player") { KillsAir++; Score += 100; Say($"{e.Name} abatido  +100", MsgKind.Kill); }
                else Say($"{e.Name} abatido", MsgKind.Kill);
            }
            else Say($"{e.Name} (seu ala) foi abatido", MsgKind.Bad);
        }

        void KillGnd(GroundUnit g)
        {
            g.Alive = false; g.Hp = 0;
            Ev(EventType.Boom, g.Pos, g.Kind == "fuel" || g.Kind == "hangar" ? 3 : 1.6, true);
            Ev(EventType.Wreck, id: g.Id);
            KillsGnd++; Score += g.Pts;
            Say($"{g.Name} destruído  +{g.Pts}", MsgKind.Kill);
            if (g.Primary) PrimLeft--;
            if (g.Kind == "fuel") Blast(g.Pos, 34, 160, null, null);
            CheckWin();
        }

        void Blast(V3 p, double rad, double dmg, Team? team, string by)
        {
            void Hit(Entity t)
            {
                double d = Cm.Dist(p, t.Pos);
                if (d < rad + t.R) Hurt(t, dmg * (1 - Cm.Clamp(d / (rad + t.R), 0, 1)), by);
            }
            for (int i = 0; i < Air.Count; i++) { var a = Air[i]; if (a.Dying == 0 && !(team.HasValue && a.Team == team)) Hit(a); }
            for (int i = 0; i < Gnd.Count; i++) { var g = Gnd[i]; if (g.Alive && team != Team.Red) Hit(g); }
            if (Pl.Alive && team != Team.Blue) Hit(Pl);
        }

        void CheckWin()
        {
            if (Over != Outcome.None) return;
            bool redAir = false;
            foreach (var a in Air) if (a.Team == Team.Red && a.Dying == 0) { redAir = true; break; }
            bool groundDone = !mission.GroundSite.HasValue || (baseBuilt && PrimLeft <= 0);
            if (mission.GroundSite.HasValue && groundDone) waveT = 1e9;
            bool wavesDone = mission.GroundSite.HasValue ? groundDone : Wave >= mission.Waves.Count;
            if (!mission.Endless && !mission.SemInimigos && groundDone && wavesDone && !redAir) { Over = Outcome.Win; Say("MISSÃO CUMPRIDA", MsgKind.Good); }
        }

        void Shoot(AirUnit e, V3 dir, double spd, double dmg, double spread, bool tracer)
        {
            V3 d = Cm.Norm(dir + V3.Cross(dir, new V3(0, 0, 1)) * (Gauss() * spread) + new V3(0, 0, Gauss() * spread));
            Bullets.Add(new Bullet { P = e.Pos, V = e.Vel + d * spd, Life = 2.4, Team = e.Team, Dmg = dmg, Tracer = tracer, By = e.Name });
        }

        Missile FireMissile(V3 pos, V3 dir, double spd, Entity tgt, string kind, Team team, string src)
        {
            bool sam = kind == "sam";
            var m = new Missile { Id = ++nid, Kind = kind, Team = team, Pos = pos, Dir = dir, Spd = spd, Vel = dir * spd, Tgt = tgt, Life = sam ? 24 : 16, MaxG = sam ? 22 : 30, Fov = sam ? 1.2 : 0.8, Src = src };
            Missiles.Add(m);
            Ev(EventType.Launch, pos, kind: kind);
            return m;
        }

        static V3 TgDir(Entity t) => t is PlayerEntity p ? p.Fwd : t is AirUnit a ? a.Dir : new V3(0, 1, 0);

        void AiHeli(AirUnit e, double dt)
        {
            if ((e.GT -= dt) <= 0) { e.GT = 0.4; e.GHere = GAt(e.Pos); e.GAhead = GAt(e.Pos + e.Dir * (e.Spd * 6)); }
            V3 alvo = mission.AlvoHeli ?? e.Pos;
            double dx = alvo.X - e.Pos.X, dy = alvo.Y - e.Pos.Y, d = Math.Max(Math.Sqrt(dx * dx + dy * dy), 1);
            if (d < 1500) HeliNoAlvo = true;
            if (Incoming(e, out double incD) != null && incD < 2400 && (e.FlareCd -= dt) <= 0 && DropFlare(e)) e.FlareCd = 0.7;
            double cota = Math.Max(e.GHere, e.GAhead) + 140;
            V3 want = Cm.Norm(new V3(dx / d, dy / d, Cm.Clamp((cota - e.Pos.Z) * 0.03, -0.3, 0.3)));
            e.Dir = Cm.TurnToward(e.Dir, want, 0.35 * dt);
            e.Roll *= 1 - Math.Min(1, dt * 2);
            e.Spd = e.Cruise;
            e.Pos += e.Dir * (e.Spd * dt);
            e.Vel = e.Dir * e.Spd;
        }

        void AiJet(AirUnit e, double dt)
        {
            if (e.Profile.Heli) { AiHeli(e, dt); return; }
            e.T += dt;
            if ((e.Rt -= dt) <= 0) { e.Rt = Rnd(1, 2); e.Tgt = PickTarget(e); }
            Entity tg = e.Tgt;
            if (tg != null && !tg.Valid) tg = e.Tgt = null;
            V3 p = e.Pos, want = e.Dir;
            double spdT = e.Cruise;
            Missile inc = Incoming(e, out double incD);
            if (inc != null && incD < 3800 && e.Skill > 0.35)
            {
                V3 los = Cm.Norm(inc.Pos - p);
                want = Cm.Norm(V3.Cross(los, new V3(0, 0, 1)) * e.BrkSide + new V3(0, 0, -0.35));
                spdT = e.VMax;
                if (incD < 2400 && (e.FlareCd -= dt) <= 0 && DropFlare(e)) e.FlareCd = 0.6;
                e.Mode = AiMode.Notch;
            }
            else if (tg != null)
            {
                if (e.Mode == AiMode.Notch || e.Mode == AiMode.Patrol) e.Mode = AiMode.Attack;
                V3 rel = tg.Pos - p;
                double d = rel.Length;
                if (d == 0) d = 1;
                V3 los = rel * (1 / d);
                double tgSpd = tg.Vel.Length;
                bool threatened = Cm.Dot(TgDir(tg), los * -1) > 0.82 && d < 1800;
                if (e.Mode == AiMode.Break || e.Mode == AiMode.Extend) { if ((e.Mt -= dt) <= 0) e.Mode = AiMode.Attack; }
                else if (threatened && rng.NextDouble() < dt * 0.7 * e.Skill) { e.Mode = AiMode.Break; e.Mt = Rnd(2, 4); e.BrkSide = rng.NextDouble() < 0.5 ? -1 : 1; }
                else if (d < 300 && Cm.Dot(e.Dir, los) < 0.4) { e.Mode = AiMode.Extend; e.Mt = Rnd(3, 5); }
                if (e.Mode == AiMode.Break)
                {
                    want = Cm.Norm(V3.Cross(los, new V3(0, 0, 1)) * e.BrkSide + new V3(0, 0, -0.3));
                    spdT = e.VMax * 0.85;
                }
                else if (e.Mode == AiMode.Extend)
                {
                    want = Cm.Norm(new V3(e.Dir.X, e.Dir.Y, 0.22));
                    spdT = e.VMax;
                }
                else
                {
                    if (d < 2200) { Cm.Lead(p, e.Vel, tg.Pos, tg.Vel, 880, out want, out _, out _); }
                    else want = Cm.Norm(tg.Pos + tg.Vel * Cm.Clamp(d / (e.Spd + 650), 0, 4) - p);
                    spdT = d > 3500 ? e.Cruise * 1.25 : Cm.Clamp(tgSpd * 1.05 + (d - 700) * 0.035, e.VMin * 1.25, e.VMax);
                    double err = Cm.Angle(e.Dir, want);
                    if (e.Burst > 0)
                    {
                        e.Burst -= dt; e.GunAcc += 38 * dt;
                        while (e.GunAcc >= 1)
                        {
                            e.GunAcc--;
                            double spread = 0.006 + (1.1 - e.Skill) * 0.012 + Math.Sqrt(e.ErrA * e.ErrA + e.ErrB * e.ErrB);
                            Shoot(e, e.Dir, 880, 3.2, spread, rng.NextDouble() < 0.3);
                        }
                    }
                    else if ((e.GunCd -= dt) <= 0 && d < 1300 && err < 0.075)
                    {
                        e.Burst = Rnd(0.5, 1.1); e.GunCd = Rnd(1.8, 3.6) / e.Skill; e.ErrA = Gauss() * 0.01; e.ErrB = Gauss() * 0.01;
                        if (e.Team == Team.Red && tg is PlayerEntity) Ev(EventType.GunWarn);
                    }
                    if (e.MslN > 0 && (e.MslCd -= dt) <= 0 && d > 1500 && d < 8500 && Cm.Angle(e.Dir, los) < 0.3 && rng.NextDouble() < dt * 0.6 * e.Skill)
                    {
                        e.MslN--; e.MslCd = 24 / e.Skill;
                        FireMissile(e.Pos, e.Dir, Math.Max(e.Spd, 100) + 40, tg, "aam", e.Team, e.Name);
                        if (tg is PlayerEntity) Say("MÍSSIL LANÇADO CONTRA VOCÊ!", MsgKind.Bad);
                    }
                }
            }
            else if (e.Team == Team.Blue)
            {
                V3 off = Pl.Pos + Pl.Right * -95 + Pl.Fwd * -55 + new V3(0, 0, 8);
                V3 rel = off - p;
                double d = rel.Length;
                want = d > 20 ? Cm.Norm(rel) : e.Dir;
                spdT = Cm.Clamp(Pl.Spd + (d - 40) * 0.25, e.VMin, e.VMax);
                if (d > 30000) e.Pos = off + new V3(0, 0, 200);
            }
            else
            {
                V3 c = e.Profile.Armed ? Pl.Pos : p;
                if (!e.Wp.HasValue || Math.Sqrt(Math.Pow(e.Wp.Value.X - p.X, 2) + Math.Pow(e.Wp.Value.Y - p.Y, 2)) < 1800)
                    e.Wp = new V3(c.X + Rnd(-9000, 9000), c.Y + Rnd(-9000, 9000), e.Profile.Armed ? Pl.Pos.Z + Rnd(-300, 400) : p.Z);
                want = Cm.Norm(e.Wp.Value - p);
            }
            if ((e.GT -= dt) <= 0) { e.GT = 0.4; e.GHere = GAt(p); e.GAhead = GAt(p + e.Dir * (e.Spd * 4)); }
            if (p.Z < e.GHere + 160 || p.Z + e.Dir.Z * e.Spd * 4 < e.GAhead + 230) { want = Cm.Norm(new V3(want.X * 0.25, want.Y * 0.25, 1)); spdT = Math.Max(spdT, e.Cruise); }
            double maxT = Math.Min(e.Gmax * Cm.G0 / Math.Max(e.Spd, 100), 0.7) * (e.Spd < e.VMin * 1.1 ? 0.6 : 1) * dt;
            double psi0 = Math.Atan2(e.Dir.X, e.Dir.Y);
            V3 nd = Cm.TurnToward(e.Dir, want, maxT);
            double turned = Cm.Angle(e.Dir, nd);
            double dpsi = Cm.WrapA(Math.Atan2(nd.X, nd.Y) - psi0);
            e.Dir = nd;
            double ndt = Math.Max(dt, 1e-3);
            e.Roll += (Cm.Clamp(Math.Atan2(e.Spd * dpsi / ndt, Cm.G0), -1.35, 1.35) - e.Roll) * Math.Min(1, dt * 3.5);
            e.Spd = Cm.Clamp(e.Spd + (Cm.Clamp((spdT - e.Spd) * 0.6, -5, 7) - Cm.G0 * e.Dir.Z * 0.6 - turned / ndt * 6) * dt, 70 * (e.VMin / 105), e.VMax);
            if (e.Spd < e.VMin) e.Dir = Cm.TurnToward(e.Dir, new V3(e.Dir.X * 0.3, e.Dir.Y * 0.3, -1), 0.4 * dt);
            e.Pos += e.Dir * (e.Spd * dt);
            e.Vel = e.Dir * e.Spd;
        }

        void StepDying(AirUnit e, double dt)
        {
            e.Dying += dt;
            e.Dir = Cm.TurnToward(e.Dir, new V3(e.Dir.X * 0.4, e.Dir.Y * 0.4, -1), 0.32 * dt);
            e.Roll += 2.6 * dt;
            e.Spd = Cm.Clamp(e.Spd + 18 * dt, 60, 260);
            e.Pos += e.Dir * (e.Spd * dt);
            e.Vel = e.Dir * e.Spd;
            double gz = GAt(e.Pos);
            if (e.Pos.Z < gz + 4 || e.Dying > 14)
            {
                e.Gone = true;
                Ev(EventType.Boom, new V3(e.Pos.X, e.Pos.Y, Math.Max(gz, e.Pos.Z - 3)), 3, true);
            }
        }

        void StepGnd(GroundUnit g, double dt)
        {
            if (!g.Alive) return;
            if (g.Moving)
            {
                g.Pos += new V3(g.MoveE * dt, g.MoveN * dt, 0);
                g.Vel = new V3(g.MoveE, g.MoveN, 0);
                if ((g.GT -= dt) <= 0) { g.GT = 0.7; g.Pos = new V3(g.Pos.X, g.Pos.Y, GAt(g.Pos)); }
            }
            if (!Pl.Alive) return;
            double d = Cm.Dist(Pl.Pos, g.Pos);
            if (g.Kind == "aaa")
            {
                if (g.Burst > 0)
                {
                    g.Burst -= dt; g.Acc += 22 * dt;
                    double tof = d / 880;
                    V3 muzzle = g.Pos + new V3(0, 0, 3);
                    V3 aim = Pl.Pos + Pl.Vel * tof + g.Bias + new V3(0, 0, 0.5 * Cm.G0 * tof * tof);
                    V3 dir = Cm.Norm(aim - muzzle);
                    while (g.Acc >= 1)
                    {
                        g.Acc--;
                        Bullets.Add(new Bullet { P = muzzle, V = Cm.Norm(dir + new V3(Gauss() * 0.004, Gauss() * 0.004, Gauss() * 0.004)) * 880, Life = 3.4, Team = Team.Red, Dmg = 3.2, Tracer = rng.NextDouble() < 0.5, By = g.Name });
                    }
                    if (g.Burst <= 0) g.Cd = Rnd(2.5, 4.5) / diff.Aaa;
                }
                else if ((g.Cd -= dt) <= 0 && d < 3600 && Pl.Pos.Z - g.Pos.Z < 3800)
                {
                    g.Burst = Rnd(1.2, 2.2);
                    double sg = (0.5 + d / 700) / diff.Aaa;
                    g.Bias = new V3(Gauss() * sg * 2, Gauss() * sg * 2, Gauss() * sg * 2);
                }
            }
            else if (g.Kind == "sam")
            {
                if ((g.Cd -= dt) <= 0 && g.Ammo > 0 && d < 11000 && d > 1200 && Pl.Pos.Z - g.Pos.Z > 120 && !Pl.Grounded && T > 10)
                {
                    g.Ammo--; g.Cd = 17 / diff.Aaa;
                    FireMissile(g.Pos + new V3(0, 0, 4), new V3(0, 0, 1), 90, Pl, "sam", Team.Red, g.Name);
                    Say("MÍSSIL SAM LANÇADO!", MsgKind.Bad);
                }
            }
        }

        void StepMissile(Missile m, double dt)
        {
            m.Age += dt; m.Life -= dt;
            double acc = m.Age < (m.Kind == "sam" ? 3.5 : 3) ? 280 : 0;
            m.Spd = Cm.Clamp(m.Spd + acc * dt - (acc != 0 ? 0 : m.Spd * 0.07 * dt) - Cm.G0 * m.Dir.Z * dt * 0.3, 0, 960);
            Entity t = m.Tgt;
            if (t != null && !t.Valid) t = m.Tgt = null;
            foreach (var f in Flares)
            {
                if (f.Team == m.Team || f.Age < 0.25 || t is FlareObj || m.Lost) continue;
                if (Cm.Dist(f.Pos, m.Pos) < 3500 && Cm.Angle(m.Dir, f.Pos - m.Pos) < 0.32 && rng.NextDouble() < dt * 1.8) { t = m.Tgt = f; break; }
            }
            if (t != null && !m.Lost)
            {
                V3 rel = t.Pos - m.Pos;
                double d = rel.Length;
                if (Cm.Angle(m.Dir, rel) > m.Fov && d > 100)
                {
                    m.Lost = true;
                    if (t is PlayerEntity) Say("Míssil perdeu a trava!", MsgKind.Good);
                }
                else
                {
                    double tl = Cm.Clamp(d / Math.Max(m.Spd, 200), 0, 3);
                    m.Dir = Cm.TurnToward(m.Dir, Cm.Norm(t.Pos + t.Vel * (tl * 0.8) - m.Pos), m.MaxG * Cm.G0 / Math.Max(m.Spd, 200) * dt);
                }
            }
            V3 p1 = m.Pos + m.Dir * (m.Spd * dt);
            m.Vel = m.Dir * m.Spd;
            if (t != null)
            {
                Cm.SegDist(m.Pos, p1, t.Pos, out double sd, out V3 sq);
                bool direct = sd < t.R + 3, passing = m.HasFuse && sd >= m.FuseD;
                if (sd < 20 + t.R * 0.5 && !passing) { m.HasFuse = true; m.FuseD = sd; m.FuseQ = sq; }
                if (direct || passing)
                {
                    double hitD = direct ? sd : m.FuseD;
                    V3 hitQ = direct ? sq : m.FuseQ;
                    m.Dead = true;
                    Ev(EventType.Boom, hitQ, 1);
                    if (!(t is FlareObj))
                    {
                        double dmgBase = m.Kind == "sam" ? 60 : t is PlayerEntity ? 75 : 150;
                        Hurt(t, dmgBase * (1 - Cm.Clamp((hitD - 4) / 22, 0, 1)), m.Src);
                        if (t is PlayerEntity) Say("Atingido por míssil!", MsgKind.Bad);
                    }
                    return;
                }
            }
            m.Pos = p1;
            if (m.Age > 0.4 && NearGround(m.Pos) && m.Pos.Z < GAt(m.Pos)) { m.Dead = true; Ev(EventType.Boom, new V3(m.Pos.X, m.Pos.Y, GAt(m.Pos)), 1, true); }
            else if (m.Life <= 0 || (m.Age > 4 && m.Spd < 230)) { m.Dead = true; Ev(EventType.Boom, m.Pos, 0.6); }
        }

        void StepBullets(double dt)
        {
            BuildFoes();
            foreach (var b in Bullets)
            {
                V3 p0 = b.P;
                b.V = new V3(b.V.X, b.V.Y, b.V.Z - Cm.G0 * dt);
                V3 p1 = p0 + b.V * dt;
                b.Life -= dt;
                var foes = b.Team == Team.Blue ? foesBlue : foesRed;
                for (int i = 0; i < foes.Count; i++)
                {
                    var t = foes[i];
                    if (!t.Valid) continue;
                    Cm.SegDist(p0, p1, t.Pos, out double sd, out V3 sq);
                    if (sd < t.R * (b.Assist ? 1.9 : 1) + 1)
                    {
                        b.Life = 0;
                        Ev(EventType.Spark, sq, ground: t is GroundUnit);
                        if (b.By == "player") Hits++;
                        Hurt(t, t is AirUnit ? b.Dmg * 1.6 : b.Dmg, b.By);
                        if (t is PlayerEntity) Ev(EventType.Hit);
                        break;
                    }
                }
                b.P = p1;
                if (b.I < 0) b.I = nid++;
                if (b.Life > 0 && NearGround(p1) && (frame + b.I) % 3 == 0 && p1.Z < GAt(p1))
                {
                    b.Life = 0;
                    Ev(EventType.Dust, new V3(p1.X, p1.Y, GAt(p1)));
                }
            }
            Bullets.RemoveAll(b => b.Life <= 0);
        }

        void StepRockets(double dt)
        {
            foreach (var r in Rockets)
            {
                r.Age += dt; r.Spd = Math.Min(r.Spd + 330 * dt, 520); r.Life -= dt;
                V3 v = r.Dir * r.Spd + r.Inh;
                v = new V3(v.X, v.Y, v.Z - Cm.G0 * r.Age * 0.3);
                r.Vel = v;
                V3 p1 = r.Pos + v * dt;
                bool hit = false;
                foreach (var t in foesBlue)
                {
                    if (!t.Valid) continue;
                    Cm.SegDist(r.Pos, p1, t.Pos, out double sd, out _);
                    if (sd < t.R + 3) { hit = true; break; }
                }
                double gz = NearGround(p1) ? GAt(p1) : double.NegativeInfinity;
                if (p1.Z < gz) { hit = true; p1 = new V3(p1.X, p1.Y, gz); }
                r.Pos = p1;
                if (hit || r.Life <= 0)
                {
                    r.Dead = true;
                    Ev(EventType.Boom, p1, 0.9, true);
                    Blast(p1, 11, 75, Team.Blue, "player");
                }
            }
            Rockets.RemoveAll(r => r.Dead);
        }

        void StepBombs(double dt)
        {
            foreach (var b in Bombs)
            {
                b.Vel = new V3(b.Vel.X, b.Vel.Y, b.Vel.Z - Cm.G0 * dt);
                b.Pos += b.Vel * dt;
                double gz = NearGround(b.Pos) ? GAt(b.Pos) : double.NegativeInfinity;
                if (b.Pos.Z <= gz)
                {
                    b.Dead = true;
                    var p = new V3(b.Pos.X, b.Pos.Y, gz);
                    Ev(EventType.Boom, p, 2.4, true);
                    Blast(p, 40, 280, Team.Blue, "player");
                }
            }
            Bombs.RemoveAll(b => b.Dead);
        }

        void UpdateLockAndTarget(double dt)
        {
            if (Tgt != null && (!Tgt.Valid || Cm.Dist(Tgt.Pos, Pl.Pos) > 25000 || (Sel == WeaponKind.Aam && Tgt is GroundUnit) || (GroundSel && Tgt is AirUnit))) Tgt = null;
            if (Tgt == null)
            {
                Entity best = null;
                double ba = 0.7;
                if (!GroundSel)
                {
                    foreach (var a in Air)
                    {
                        if (a.Team != Team.Red || a.Dying > 0 || Cm.Dist(a.Pos, Pl.Pos) > 22000) continue;
                        double ang = Cm.Angle(Pl.Fwd, a.Pos - Pl.Pos);
                        if (ang < ba) { ba = ang; best = a; }
                    }
                }
                if (best == null && Sel != WeaponKind.Aam)
                {
                    double lim = GroundSel ? load.Sight.Range : 6000;
                    foreach (var g in Gnd)
                    {
                        if (!g.Alive || Cm.Dist(g.Pos, Pl.Pos) > lim) continue;
                        double ang = Cm.Angle(Pl.Fwd, g.Pos - Pl.Pos);
                        if (ang < ba) { ba = ang; best = g; }
                    }
                }
                Tgt = best;
            }
            if (Sel == WeaponKind.Aam && Tgt is AirUnit && W.Aam > 0)
            {
                V3 rel = Tgt.Pos - Pl.Pos;
                if (rel.Length < 12000 && Cm.Angle(Pl.Fwd, rel) < 0.3) { LockT += dt; LockOk = LockT > 1.1; }
                else { LockT = Math.Max(0, LockT - dt * 2); LockOk = LockT > 1.1; if (LockT < 0.3) LockOk = false; }
            }
            else { LockT = 0; LockOk = false; }
        }

        void UpdateWarn()
        {
            WarnD = 1e9; WarnMsl = null;
            foreach (var m in Missiles)
            {
                if (m.Tgt != Pl || m.Dead) continue;
                double d = Cm.Dist(m.Pos, Pl.Pos);
                if (d < WarnD) { WarnD = d; WarnMsl = m; }
            }
        }

        bool March(WeaponKind kind, double gz, out V3 impact, out double tof)
        {
            V3 p = Pl.Pos;
            double t = 0, dt = 0.1, lim = kind == WeaponKind.Bomb ? 40 : 6;
            while (t < lim)
            {
                double age = t + dt;
                V3 v;
                if (kind == WeaponKind.Bomb) v = new V3(Pl.Vel.X, Pl.Vel.Y, Pl.Vel.Z - Cm.G0 * t);
                else { v = Pl.Fwd * Math.Min(40 + 330 * age, 520) + Pl.Vel; v = new V3(v.X, v.Y, v.Z - Cm.G0 * age * 0.3); }
                V3 q = p + v * dt;
                if (q.Z <= gz)
                {
                    double den = p.Z - q.Z;
                    if (den == 0) den = 1;
                    double k = (p.Z - gz) / den;
                    impact = new V3(p.X + (q.X - p.X) * k, p.Y + (q.Y - p.Y) * k, gz);
                    tof = t + dt * k;
                    return true;
                }
                p = q; t += dt;
            }
            impact = default; tof = 0;
            return false;
        }

        public bool Predict(WeaponKind kind, out V3 impact, out double tof)
        {
            double gz = GAt(Pl.Pos);
            impact = default; tof = 0;
            for (int i = 0; i < 3; i++)
            {
                if (!March(kind, gz, out impact, out tof)) return false;
                gz = GAt(impact);
            }
            impact = new V3(impact.X, impact.Y, gz);
            return true;
        }

        void DropBomb()
        {
            W.Bomb--;
            Bombs.Add(new Bomb { Id = ++nid, Pos = Pl.Pos + Pl.Up * -2, Vel = Pl.Vel });
            Ev(EventType.Drop);
        }

        void UpdateSolution(double dt)
        {
            if (!GroundSel || !Pl.Alive || Pl.Grounded) { Sol = null; Pickle = false; return; }
            WeaponKind kind = Sel;
            bool has = Predict(kind, out V3 impact, out double tof);
            double gs = Math.Sqrt(Pl.Vel.X * Pl.Vel.X + Pl.Vel.Y * Pl.Vel.Y);
            if (gs == 0) gs = 1;
            V3 tr = new V3(Pl.Vel.X / gs, Pl.Vel.Y / gs, 0), rt = new V3(tr.Y, -tr.X, 0);
            double range = has ? Math.Sqrt(Math.Pow(impact.X - Pl.Pos.X, 2) + Math.Pow(impact.Y - Pl.Pos.Y, 2)) : double.PositiveInfinity;
            var sol = new FireSolution
            {
                Kind = kind, HasImpact = has, Impact = impact, Tof = tof, Range = range,
                InRange = kind == WeaponKind.Bomb ? has : range >= RktMin && range <= RktMax,
                TooClose = kind == WeaponKind.Rkt && range < RktMin,
            };
            if (Tgt is GroundUnit t && has)
            {
                V3 aim = t.Moving ? t.Pos + new V3(t.MoveE * tof, t.MoveN * tof, 0) : t.Pos;
                V3 err = aim - impact, to = aim - Pl.Pos;
                sol.Target = t;
                sol.Along = Cm.Dot(err, tr); sol.Cross = Cm.Dot(err, rt);
                sol.Miss = Math.Sqrt(sol.Along * sol.Along + sol.Cross * sol.Cross);
                sol.Dist = Math.Sqrt(to.X * to.X + to.Y * to.Y);
                sol.Ttr = sol.Along / gs;
                sol.Steer = Math.Atan2(Cm.Dot(to, rt), Cm.Dot(to, tr));
                sol.Rad = (kind == WeaponKind.Bomb ? 22 : 7) + t.R;
                sol.OnTarget = sol.Miss < sol.Rad && sol.InRange;
                sol.Ccrp = kind == WeaponKind.Bomb && load.Sight.Ccrp && sol.Along > 0 && sol.Ttr < 90;
            }
            Sol = sol;
            if (Pickle)
            {
                pickleT += dt;
                if (!InPickle) { Pickle = false; if (kind == WeaponKind.Bomb && W.Bomb > 0 && pickleT < 0.3) DropBomb(); }
                else if (sol.Target == null || kind != WeaponKind.Bomb || W.Bomb <= 0) Pickle = false;
                else if (sol.Along <= 2)
                {
                    Pickle = false;
                    if (Math.Abs(sol.Cross) < sol.Rad) DropBomb();
                    else { Say("Fora do eixo: bomba retida", MsgKind.Warn); Ev(EventType.NoLock); }
                }
            }
        }

        public bool GunLead(out V3 dir, out double t, out double range)
        {
            dir = default; t = 0; range = 0;
            if (Tgt == null) return false;
            Cm.Lead(Pl.Pos, Pl.Vel, Tgt.Pos, Tgt.Vel, load.Gun.V, out dir, out t, out range);
            return true;
        }

        public void CycleWeapon(int d = 1)
        {
            int i = Avail.IndexOf(Sel);
            Sel = Avail[(i + d + Avail.Count) % Avail.Count];
            LockT = 0; LockOk = false;
            Ev(EventType.Select);
        }

        public void CycleTarget()
        {
            var list = new List<Entity>();
            double lim = GroundSel ? load.Sight.Range : 16000;
            if (!GroundSel) foreach (var a in Air) if (a.Team == Team.Red && a.Dying == 0 && Cm.Dist(a.Pos, Pl.Pos) < lim) list.Add(a);
            foreach (var g in Gnd) if (g.Alive && Cm.Dist(g.Pos, Pl.Pos) < lim) list.Add(g);
            if (list.Count == 0) { Tgt = null; return; }
            list.Sort((a, b) => Cm.Dist(a.Pos, Pl.Pos).CompareTo(Cm.Dist(b.Pos, Pl.Pos)));
            Tgt = list[(list.IndexOf(Tgt) + 1) % list.Count];
            LockT = 0; LockOk = false;
            Ev(EventType.Select);
        }

        public void Flare()
        {
            if (Pl.Alive && DropFlare(Pl)) Ev(EventType.Flare);
            else if (W.Flares == 0) Say("SEM CONTRAMEDIDAS", MsgKind.Bad);
        }

        public void Fire()
        {
            if (!Pl.Alive || Pl.Grounded) return;
            if (Sel == WeaponKind.Aam)
            {
                if (W.Aam <= 0) return;
                if (!LockOk || Tgt == null) { Say("SEM TRAVA", MsgKind.Warn); Ev(EventType.NoLock); return; }
                W.Aam--;
                V3 pos = Pl.Pos + Pl.Right * (W.Aam % 2 == 1 ? 3 : -3) + Pl.Up * -0.8;
                FireMissile(pos, Pl.Fwd, Math.Max(Pl.Spd, 100) + 40, Tgt, "aam", Team.Blue, "player");
                LockT = 0; LockOk = false;
            }
            else if (Sel == WeaponKind.Bomb)
            {
                if (W.Bomb <= 0) return;
                if (Sol != null && Sol.Ccrp && Sol.Along > 30) { Pickle = true; pickleT = 0; Ev(EventType.Select); return; }
                DropBomb();
            }
            else if (Sel == WeaponKind.Rkt)
            {
                if (W.Rkt <= 0) return;
                for (int sd = -1; sd <= 1; sd += 2)
                {
                    if (W.Rkt <= 0) break;
                    W.Rkt--;
                    Rockets.Add(new Rocket { Id = ++nid, Pos = Pl.Pos + Pl.Right * (sd * 3.2), Dir = Pl.Fwd, Inh = Pl.Vel, Vel = Pl.Vel, Spd = 40, Life = 6 });
                }
                Ev(EventType.Launch, Pl.Pos, kind: "rkt");
            }
        }

        void PlayerGun(double dt)
        {
            var g = load.Gun;
            Firing = InGun && W.Gun > 0 && Pl.Alive && !Pl.Grounded;
            if (!Firing) { W.GunAcc = 0; return; }
            W.GunAcc += g.Rate * dt;
            while (W.GunAcc >= 1 && W.Gun > 0)
            {
                W.GunAcc -= 1; W.Gun--; W.N++; Shots++;
                V3 dir = Cm.Norm(Pl.Fwd + Pl.Right * (Gauss() * g.Spread * 1.8) + Pl.Up * (Gauss() * g.Spread * 1.8));
                Bullets.Add(new Bullet { P = Pl.Pos + Pl.Fwd * 7, V = Pl.Vel + dir * g.V, Life = 2.3, Team = Team.Blue, Dmg = g.Dmg, Tracer = W.N % 4 == 0, Assist = Assist, By = "player" });
            }
        }

        static readonly (string kind, string name, double hp, double r, int pts, bool primary)[] GroundTypes =
        {
            ("hangar", "Hangar", 150, 20, 150, true), ("fuel", "Depósito de combustível", 90, 12, 150, true), ("radar", "Radar", 60, 7, 120, true),
            ("sam", "Bateria SAM", 70, 7, 120, true), ("aaa", "Antiaérea", 45, 4.5, 60, false), ("tank", "Blindado", 70, 5, 40, false), ("truck", "Caminhão", 30, 4.5, 25, false),
        };

        GroundUnit MkGround(V3 site, string kind, double dx, double dy, double yaw = 0, double moveE = 0)
        {
            var d = Array.Find(GroundTypes, x => x.kind == kind);
            var p = new V3(site.X + dx, site.Y + dy, 0);
            p = new V3(p.X, p.Y, GAt(p));
            var g = new GroundUnit { Id = ++nid, Kind = kind, Name = d.name, Pos = p, Yaw = yaw, Hp = d.hp, HpMax = d.hp, R = d.r, Pts = d.pts, Primary = d.primary, Cd = kind == "sam" ? Rnd(6, 14) : Rnd(1, 3), MoveE = moveE };
            Gnd.Add(g);
            if (g.Primary) { PrimLeft++; PrimTotal++; }
            return g;
        }

        void BuildBase(V3 s)
        {
            MkGround(s, "hangar", -60, 30); MkGround(s, "hangar", 10, 40); MkGround(s, "fuel", 80, -20); MkGround(s, "radar", -20, -70);
            MkGround(s, "sam", -150, -40); MkGround(s, "sam", 170, 60);
            MkGround(s, "aaa", -100, 110); MkGround(s, "aaa", 120, 130); MkGround(s, "aaa", 0, -140); MkGround(s, "aaa", 210, -60);
            for (int i = 0; i < 4; i++) MkGround(s, "tank", -90 + i * 28, -10 + (i % 2) * 12, 90);
            for (int i = 0; i < 3; i++) MkGround(s, "truck", -300 + i * 14, -230, 90, 7);
            baseBuilt = true;
            Say($"Base inimiga marcada no mapa: {PrimTotal} alvos principais", MsgKind.Good);
        }

        void SpawnWave()
        {
            if (mission.Endless)
            {
                int n = Math.Min(2 + (int)(Wave * 0.8), 8);
                Wave++;
                double brg = Math.Atan2(Pl.Fwd.X, Pl.Fwd.Y) + Rnd(-1.2, 1.2), D = Rnd(15000, 21000);
                var bp = new V3(Pl.Pos.X + Math.Sin(brg) * D, Pl.Pos.Y + Math.Cos(brg) * D, 0);
                for (int i = 0; i < n; i++)
                {
                    var p = new V3(bp.X + Rnd(-700, 700), bp.Y + Rnd(-700, 700), 0);
                    p = new V3(p.X, p.Y, Math.Max(Pl.Pos.Z + Rnd(-300, 500), GAt(p) + 900));
                    string tipo = i % 2 == 0 ? "Mirage III" : "F/A-18E";
                    SpawnUnit(Team.Red, tipo, tipo == "F/A-18E" ? "EUA" : "Argentina", $"{UnitProfile.Get(tipo).Name} {i + 1}", p, new V3(Pl.Pos.X - p.X, Pl.Pos.Y - p.Y, Rnd(-0.05, 0.05)), 215, Cm.Clamp(diff.Skill + Rnd(-0.12, 0.12), 0.35, 1.2));
                }
                Say($"Onda {Wave}: {n} caças inimigos", MsgKind.Warn);
                Ev(EventType.Radio);
                return;
            }
            var w = mission.Waves[Wave];
            Wave++;
            foreach (var g in w.Groups)
            {
                var pr = UnitProfile.Get(g.Tipo);
                for (int i = 0; i < g.Count; i++)
                {
                    var p = new V3(g.Pos.X + Rnd(-500, 500), g.Pos.Y + Rnd(-500, 500), g.Pos.Z);
                    p = new V3(p.X, p.Y, Math.Max(p.Z, GAt(p) + (pr.Armed ? 300 : 120)));
                    var dir = pr.Armed ? new V3(Pl.Pos.X - p.X, Pl.Pos.Y - p.Y, 0) : new V3(Rnd(-1, 1), Rnd(-1, 1), 0);
                    SpawnUnit(Team.Red, g.Tipo, g.Forca, $"{pr.Name} {i + 1}", p, dir, pr.Cruise, Cm.Clamp(diff.Skill + Rnd(-0.12, 0.12), 0.35, 1.2));
                }
            }
            if (!string.IsNullOrEmpty(w.Radio)) Say(w.Radio, MsgKind.Warn);
            Ev(EventType.Radio);
        }

        void SpawnWingman()
        {
            wingSpawned = true;
            V3 p = Pl.Pos + Pl.Right * -90 + Pl.Fwd * -60 + new V3(0, 0, 8);
            Wingman = SpawnUnit(Team.Blue, "Ala", "FAB", "Ala", p, new V3(Pl.Vel.X != 0 ? Pl.Vel.X : Pl.Fwd.X, Pl.Vel.Y != 0 ? Pl.Vel.Y : Pl.Fwd.Y, 0), Math.Max(Pl.Spd, 150), 0.85);
            Say("Ala ao seu lado: ele ataca os inimigos próximos", MsgKind.Good);
        }

        void Rearm()
        {
            W.Gun = load.Gun.Rounds; W.Aam = load.Aam; W.Bomb = load.Bomb; W.Rkt = load.Rkt; W.Flares = load.Flares;
            Pl.Hp = Pl.HpMax;
            Ev(EventType.Rearm);
            Say("Rearmado, reparado e reabastecido", MsgKind.Good);
        }

        public void Step(double dt)
        {
            if (dt <= 0) return;
            int n = Math.Max(1, (int)Math.Ceiling(dt / 0.034));
            double h = dt / n;
            for (int i = 0; i < n; i++) Sub(h);
        }

        void Sub(double dt)
        {
            T += dt; frame++;
            if (frame % 20 == 1) GAt(Pl.Pos);
            if (mission.GroundSite.HasValue && !baseBuilt && T > 0.2) BuildBase(mission.GroundSite.Value);
            BuildFoes();
            PlayerGun(dt);
            UpdateLockAndTarget(dt);
            UpdateSolution(dt);
            for (int i = 0; i < Air.Count; i++) { var e = Air[i]; if (e.Dying > 0) StepDying(e, dt); else AiJet(e, dt); }
            for (int i = 0; i < Gnd.Count; i++) StepGnd(Gnd[i], dt);
            for (int i = 0; i < Missiles.Count; i++) StepMissile(Missiles[i], dt);
            Missiles.RemoveAll(m => m.Dead);
            foreach (var f in Flares)
            {
                f.Age += dt; f.Life -= dt;
                f.Vel = new V3(f.Vel.X, f.Vel.Y, f.Vel.Z - 3 * dt);
                f.Pos += f.Vel * dt;
                f.Vel *= 1 - 0.5 * dt;
                if (f.Life <= 0) f.Dead = true;
            }
            Flares.RemoveAll(f => f.Dead);
            StepBullets(dt); StepRockets(dt); StepBombs(dt);
            for (int i = Air.Count - 1; i >= 0; i--)
            {
                if (!Air[i].Gone) continue;
                Ev(EventType.Gone, id: Air[i].Id);
                if (Air[i] == Wingman) Wingman = null;
                Air.RemoveAt(i);
            }
            UpdateWarn();
            if (Over != Outcome.Lose)
            {
                if (mission.Wingman && !wingSpawned && Pl.Alive && Pl.Agl > 150 && !Pl.Grounded) SpawnWingman();
                bool moreWaves = mission.Endless || Wave < mission.Waves.Count;
                if (T > 1 && !Pl.Grounded && Pl.Agl > 100 && moreWaves && (waveT -= dt) <= 0)
                {
                    int alive = 0;
                    foreach (var a in Air) if (a.Team == Team.Red && a.Dying == 0) alive++;
                    if (alive == 0 || Wave == 0)
                    {
                        SpawnWave();
                        waveT = !mission.Endless && Wave < mission.Waves.Count ? mission.Waves[Wave].Delay : 8;
                    }
                }
                if (T > 2 && Over == Outcome.None) CheckWin();
            }
            if (Pl.Alive && Pl.Grounded && Pl.Spd < 3 && Math.Sqrt(Math.Pow(Pl.Pos.X - Home.X, 2) + Math.Pow(Pl.Pos.Y - Home.Y, 2)) < 1800)
            {
                bool need = W.Gun < load.Gun.Rounds || W.Aam < load.Aam || W.Bomb < load.Bomb || W.Rkt < load.Rkt || W.Flares < load.Flares || Pl.Hp < Pl.HpMax;
                if ((rearmT += dt) > 4 && need) { Rearm(); rearmT = 0; }
            }
            else rearmT = 0;
        }
    }
}
