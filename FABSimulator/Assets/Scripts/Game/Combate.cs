using System;
using System.Collections.Generic;
using FABSim.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using EventType = FABSim.Core.EventType;

namespace FABSim.Game
{
    [DefaultExecutionOrder(10)]
    public sealed class Combate : MonoBehaviour
    {
        const double RE = FlightModel.RE;
        const double AmeacaInicialKm = 90, VelAmeacaKmS = 0.2;

        public FlightController F;
        public CombatSession Sess { get; private set; }
        public MissaoDef Missao { get; private set; }
        public RadioDirector Radio { get; private set; }
        public FabAudio Audio { get; private set; }
        public readonly List<string> Toasts = new List<string>();
        public double AmeacaKm { get; private set; } = AmeacaInicialKm;
        public Reabastecimento Reab { get; private set; }
        public AtivoDef Ativo => Missao?.ativos_a_proteger != null && Missao.ativos_a_proteger.Length > 0 ? Missao.ativos_a_proteger[0] : null;
        public string Falha { get; private set; }
        public bool Encerrada { get; private set; }

        double lat0, lon0, cosLat0, baseElev;
        double massAam, massBomb, massRkt, massFixed;
        string origemRapto;
        Fx fx;
        Loadout load;
        bool started, wonSaid, pending;
        readonly List<(SpawnSpec spec, double brg, double dist)> relativos = new List<(SpawnSpec, double, double)>();

        readonly Dictionary<long, (double h, int frame)> groundCache = new Dictionary<long, (double, int)>();
        int rayBudget;

        readonly Dictionary<int, GameObject> airVis = new Dictionary<int, GameObject>();
        readonly Dictionary<int, AircraftRig> rotores = new Dictionary<int, AircraftRig>();
        readonly FlightState estadoRotor = new FlightState();
        readonly Dictionary<int, GameObject> gndVis = new Dictionary<int, GameObject>();

        void Awake()
        {
            if (F == null) F = GetComponent<FlightController>();
            Audio = gameObject.AddComponent<FabAudio>();
            fx = new Fx(this);
        }

        public V3 ToEnu(double lat, double lon, double z) => new V3((lon - lon0) * RE * cosLat0, (lat - lat0) * RE, z);
        public void FromEnu(V3 p, out double latDeg, out double lonDeg) { latDeg = (lat0 + p.Y / RE) * 180 / Math.PI; lonDeg = (lon0 + p.X / (RE * cosLat0)) * 180 / Math.PI; }

        public Vector3 World(V3 p)
        {
            var c = Sess != null ? Sess.Pl.Pos : p;
            double dx = p.X - c.X, dy = p.Y - c.Y, dz = p.Z - c.Z;
            double drop = (dx * dx + dy * dy) / (2 * RE);
            return F.model.position + new Vector3((float)dx, (float)(dz - drop), (float)dy);
        }

        public static Vector3 Dir(V3 d) => new Vector3((float)d.X, (float)d.Z, (float)d.Y);

        double GroundAt(double x, double y)
        {
            long key = ((long)Math.Floor(x / 400) << 32) ^ (uint)Math.Floor(y / 400);
            bool have = groundCache.TryGetValue(key, out var e);
            if (have && (Time.frameCount - e.frame < 900 || rayBudget <= 0)) return e.h;
            if (rayBudget <= 0) return baseElev;
            rayBudget--;
            var from = World(new V3(x, y, Sess.Pl.Pos.Z));
            double dr = (x - Sess.Pl.Pos.X) * (x - Sess.Pl.Pos.X) + (y - Sess.Pl.Pos.Y) * (y - Sess.Pl.Pos.Y);
            from.y += 15000;
            if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit, 60000f)) { if (have) return e.h; return baseElev; }
            double h = hit.point.y - F.model.position.y + Sess.Pl.Pos.Z + dr / (2 * RE);
            groundCache[key] = (h, Time.frameCount);
            return h;
        }

        MissionSpec BuildSpec(MissaoDef m, BaseData b, RunwayData rw)
        {
            double hdg = rw.rumo_verdadeiro * Math.PI / 180;
            var spec = new MissionSpec { Id = m.id, Title = m.titulo, Wingman = !m.SemAla && F.aircraftId != "kc390", SemInimigos = !m.AlvosSolo && (m.inimigos == null || m.inimigos.Length == 0) };
            if (m.Rapto) { MontarRapto(m, spec); return spec; }
            if (m.AlvosSolo) spec.GroundSite = new V3(Math.Sin(hdg) * 15000, Math.Cos(hdg) * 15000, 0);
            if (m.inimigos == null || m.inimigos.Length == 0) return spec;
            var w1 = new WaveSpec { Delay = 35 };
            var w2 = new WaveSpec { Delay = 12 };
            foreach (var e in m.inimigos)
            {
                string c = e.comportamento ?? "";
                var wave = c.Contains("segunda onda") || c.Contains("reforço") ? w2 : w1;
                double n = (e.posicao.lat - b.lat) * Math.PI / 180 * RE, ea = (e.posicao.lon - b.lon) * Math.PI / 180 * RE * cosLat0;
                var s = new SpawnSpec { Tipo = e.tipo, Forca = e.forca ?? "Hostil", Count = e.quantidade };
                relativos.Add((s, Math.Atan2(ea, n), wave == w1 ? 38000 : 30000));
                s.Pos = new V3(0, 0, e.posicao.altitude_m);
                wave.Groups.Add(s);
            }
            spec.Waves.Add(w1);
            if (w2.Groups.Count > 0) spec.Waves.Add(w2);
            return spec;
        }

        // Os helicopteros aparecem a 90 km do alvo, na direcao real da origem sorteada.
        void MontarRapto(MissaoDef m, MissionSpec spec)
        {
            bool costa = UnityEngine.Random.value < 0.5f;
            double oLat = costa ? -16.6 : -27.45, oLon = costa ? -38.6 : -59.05;
            origemRapto = costa ? "de um porta-aviões na costa da Bahia" : "de uma base na Argentina";
            var alvo = ToEnu(m.ativos_a_proteger[0].posicao.lat * Math.PI / 180, m.ativos_a_proteger[0].posicao.lon * Math.PI / 180, 0);
            var origem = ToEnu(oLat * Math.PI / 180, oLon * Math.PI / 180, 0);
            double brg = Math.Atan2(origem.X - alvo.X, origem.Y - alvo.Y), sb = Math.Sin(brg), cb = Math.Cos(brg);
            spec.AlvoHeli = alvo;
            var onda = new WaveSpec { Delay = 20, Radio = "Formação inimiga no radar, rumo a Brasília" };
            onda.Groups.Add(new SpawnSpec { Tipo = "MH-47G", Forca = "EUA", Count = 2, Pos = new V3(alvo.X + sb * 90000, alvo.Y + cb * 90000, 0) });
            onda.Groups.Add(new SpawnSpec { Tipo = "MH-60M", Forca = "EUA", Count = 4, Pos = new V3(alvo.X + sb * 86000, alvo.Y + cb * 86000, 0) });
            onda.Groups.Add(new SpawnSpec { Tipo = "F/A-18E", Forca = "EUA", Count = 2, Pos = new V3(alvo.X + sb * 60000, alvo.Y + cb * 60000, 3000) });
            if (GameSession.Dificuldade == "veterano") onda.Groups.Add(new SpawnSpec { Tipo = "F-22A", Forca = "EUA", Count = 1, Pos = new V3(alvo.X + sb * 55000, alvo.Y + cb * 55000, 5000) });
            spec.Waves.Add(onda);
        }

        public void Begin(BaseData b, RunwayData rw, double startLat, double startLon)
        {
            lat0 = b.lat * Math.PI / 180; lon0 = b.lon * Math.PI / 180; cosLat0 = Math.Cos(lat0); baseElev = b.elevacao_m;
            Missao = GameData.Missao(GameSession.MissaoId) ?? GameData.Missao("voo-livre");
            load = GameSession.BuildLoadout();
            var spec = BuildSpec(Missao, b, rw);
            string dif = Missao.dificuldade_minima == "veterano" ? "veterano" : GameSession.Dificuldade;
            Sess = new CombatSession(GroundAt, load, spec, GameSession.Diff(dif)) { Invuln = Missao.Invulneravel };
            Sess.Home = ToEnu(startLat, startLon, baseElev);
            massAam = GameSession.MassPerRound(WeaponKind.Aam); massBomb = GameSession.MassPerRound(WeaponKind.Bomb); massRkt = GameSession.MassPerRound(WeaponKind.Rkt);
            massFixed = GameSession.StoreMass() - load.Aam * massAam - load.Bomb * massBomb - load.Rkt * massRkt;
            F.S.Fuel += GameSession.ExtraFuel();
            SyncPlayer();
            Radio = new RadioDirector(this, b, rw);
            if (Missao.Noite) F.Noite();
            if (Missao.Rapto) Radio.Diz("Controle", $"{{indicativo}}, alerta máximo. Helicópteros dos EUA decolaram {origemRapto} rumo a Brasília. Querem o presidente. Decole e intercepte.");
            if (Missao.Reabastecer)
            {
                double capacidade = F.Ac.FuelMax + GameSession.ExtraFuel();
                F.S.Fuel = capacidade * Missao.CombustivelFrac;
                Reab = new Reabastecimento(this, F, Missao, capacidade);
                var destino = BaseDb.Find(Missao.Destino);
                Rota.Limpar();
                Rota.Pontos.Add(new PontoRota { Lat = Missao.EncontroLat, Lon = Missao.EncontroLon, Nome = "REVO" });
                Rota.Pontos.Add(new PontoRota { Lat = destino.lat, Lon = destino.lon, Nome = destino.icao });
            }
            Audio.Bind(F);
            started = true;
        }

        void SyncPlayer()
        {
            var s = F.S;
            var R = Mat3.From(s.Q);
            V3 f = R.Mul(new V3(1, 0, 0)), r = R.Mul(new V3(0, 1, 0)), d = R.Mul(new V3(0, 0, 1));
            Sess.SetPlayer(ToEnu(s.Lat, s.Lon, s.Z), new V3(s.V.Y, s.V.X, -s.V.Z), new V3(f.Y, f.X, -f.Z), new V3(-d.Y, -d.X, d.Z), new V3(r.Y, r.X, -r.Z), s.Diag.Agl, s.Contact);
        }

        void Update()
        {
            if (!started || !F.Ready) return;
            if (F.Input.Restart) { SceneManager.LoadScene(SceneManager.GetActiveScene().name); return; }
            if (F.Paused) return;
            float dt = Math.Min(Time.deltaTime, 0.25f);
            rayBudget = 48;
            SyncPlayer();
            UpdateRelativeSpawns();
            var inp = F.Input;
            if (Sess.Pl.Alive && !Encerrada)
            {
                Sess.InGun = inp.Gun && F.MasterArm; Sess.InPickle = inp.FireHeld && F.MasterArm;
                if (inp.NextWeapon) { Sess.CycleWeapon(1); Audio.Click(); }
                if (inp.PrevWeapon) { Sess.CycleWeapon(-1); Audio.Click(); }
                if (inp.NextTarget) { Sess.CycleTarget(); Audio.Click(); }
                if (inp.Flare) Sess.Flare();
                if (inp.Fire && F.MasterArm) Sess.Fire();
            }
            else { Sess.InGun = false; Sess.InPickle = false; }
            Sess.Step(dt);
            F.S.StoreMass = massFixed + Sess.W.Aam * massAam + Sess.W.Bomb * massBomb + Sess.W.Rkt * massRkt;
            if (!Sess.Pl.Alive && F.S.Crashed.Length == 0) F.S.Crashed = "Aeronave abatida";
            if (F.S.Crashed.Length > 0) { Sess.Pl.Alive = false; if (!Encerrada) { fx.Explosion(Sess.Pl.Pos, 16); Audio.Boom(1); } Encerrar(F.S.Crashed); }
            UpdateThreat(dt);
            Reab?.Atualizar(dt);
            if (Reab != null && !Encerrada && F.S.Contact && Sess.Pl.Spd < 3)
            {
                var d = BaseDb.Find(Missao.Destino);
                if (Rota.Dist(F.S.Lat * 180 / Math.PI, F.S.Lon * 180 / Math.PI, d.lat, d.lon) < 6000) Encerrar(Reab.Feito ? null : "Pouso no destino sem o reabastecimento em voo");
            }
            DrainEvents();
            Radio.Update(dt);
            if (Sess.Over == Outcome.Win && !wonSaid) { wonSaid = true; Radio.Say("objetivo_cumprido"); pending = true; }
            if (pending && F.S.Contact && Sess.Pl.Spd < 3) { pending = false; Encerrar(null); }
            Audio.Warn(Sess.WarnMsl != null, Sess.LockOk, Sess.Firing);
        }

        public void Alijar()
        {
            Sess.W.Aam = Sess.W.Bomb = Sess.W.Rkt = 0;
            massFixed = 0;
            F.S.Fuel = Math.Min(F.S.Fuel, F.Ac.FuelMax);
            Toasts.Add("Cargas externas alijadas");
        }

        void UpdateRelativeSpawns()
        {
            var p = Sess.Pl.Pos;
            foreach (var (spec, brg, dist) in relativos)
                spec.Pos = new V3(p.X + Math.Sin(brg) * dist, p.Y + Math.Cos(brg) * dist, spec.Pos.Z);
        }

        void UpdateThreat(float dt)
        {
            if (Missao.Rapto)
            {
                double perto = 1e9;
                var alvo = Sess.Air.Count > 0 ? ToEnu(Ativo.posicao.lat * Math.PI / 180, Ativo.posicao.lon * Math.PI / 180, 0) : default;
                foreach (var a in Sess.Air)
                    if (a.Profile.Heli && a.Dying == 0) perto = Math.Min(perto, Cm.Dist(new V3(a.Pos.X, a.Pos.Y, 0), alvo));
                if (perto < 1e9) AmeacaKm = perto / 1000;
                if (Sess.HeliNoAlvo && !Encerrada) Encerrar("Os helicópteros chegaram ao Palácio: o presidente foi capturado");
                return;
            }
            if (Ativo == null || Sess.Wave == 0 || Sess.Over != Outcome.None || Encerrada) return;
            bool hostil = false;
            foreach (var a in Sess.Air) if (a.Team == Team.Red && a.Dying == 0) { hostil = true; break; }
            if (!hostil) return;
            AmeacaKm -= VelAmeacaKmS * dt;
            if (AmeacaKm > Ativo.raio_de_seguranca_km) return;
            Encerrar($"Os contatos alcançaram {Ativo.nome}");
        }

        public void Encerrar(string motivo)
        {
            if (Encerrada) return;
            Encerrada = true;
            Falha = motivo;
            if (motivo != null && Sess.Over == Outcome.None) Sess.Over = Outcome.Lose;
        }

        void DrainEvents()
        {
            foreach (var e in Sess.Events)
            {
                switch (e.Type)
                {
                    case EventType.Boom: fx.Explosion(e.Pos, e.Size > 0 ? e.Size : 8); Audio.Boom((float)Cm.Clamp(1 - Cm.Dist(e.Pos, Sess.Pl.Pos) / 15000, 0.08, 1)); break;
                    case EventType.Spark: fx.Sparks(e.Pos); break;
                    case EventType.Dust: fx.Dust(e.Pos); break;
                    case EventType.Launch:
                        Audio.Launch();
                        if (Cm.Dist(e.Pos, Sess.Pl.Pos) < 30) Radio.Say("missil_lancado", 0);
                        break;
                    case EventType.Hurt: Audio.Hit(); fx.ShakeT = 0.35f; fx.HurtT = 0.5f; break;
                    case EventType.Hit: Audio.Click(); break;
                    case EventType.Flare: Audio.Flare(); break;
                    case EventType.NoLock: Audio.Deny(); break;
                    case EventType.Rearm: F.S.Fuel = Math.Max(F.S.Fuel, F.Ac.FuelMax + GameSession.ExtraFuel()); break;
                    case EventType.Radio: Radio.OnWave(); break;
                }
            }
            Sess.Events.Clear();
            foreach (var m in Sess.Msgs)
            {
                Toasts.Add(m.Text);
                if (m.Text.StartsWith("MÍSSIL LANÇADO")) Radio.Say("missil_lancado", 2);
            }
            Sess.Msgs.Clear();
            while (Toasts.Count > 6) Toasts.RemoveAt(0);
        }

        void LateUpdate()
        {
            if (!started || !F.Ready) return;
            SyncVisuals();
            fx.Tick(Time.deltaTime);
            if (fx.ShakeT > 0 && F.cam != null)
            {
                float a = fx.ShakeT * 0.05f;
                F.cam.transform.position += new Vector3(UnityEngine.Random.Range(-a, a), UnityEngine.Random.Range(-a, a), UnityEngine.Random.Range(-a, a));
            }
        }

        public float HurtFlash => fx.HurtT;

        void SyncVisuals()
        {
            foreach (var a in Sess.Air)
            {
                if (!airVis.TryGetValue(a.Id, out var go))
                {
                    go = new GameObject("Unidade " + a.Name);
                    var fit = ModelFit.Spawn(a.Model, go.transform);
                    if (a.Profile.Heli && fit != null) rotores[a.Id] = AircraftRig.Build(fit, a.Model, null);
                    airVis[a.Id] = go;
                }
                if (rotores.TryGetValue(a.Id, out var rotor)) rotor.Apply(estadoRotor, Time.deltaTime);
                go.transform.position = World(a.Pos);
                var fwd = Dir(a.Dir);
                var up = Quaternion.AngleAxis((float)(-a.Roll * 57.2958), fwd) * Vector3.up;
                if (fwd.sqrMagnitude > 1e-4f) go.transform.rotation = Quaternion.LookRotation(fwd, up);
            }
            if (airVis.Count > Sess.Air.Count)
            {
                var gone = new List<int>();
                foreach (var kv in airVis) if (!Sess.Air.Exists(u => u.Id == kv.Key)) gone.Add(kv.Key);
                foreach (int id in gone) { Destroy(airVis[id]); airVis.Remove(id); }
            }
            foreach (var g in Sess.Gnd)
            {
                if (!gndVis.TryGetValue(g.Id, out var go)) gndVis[g.Id] = go = fx.MakeGround(g);
                go.transform.position = World(g.Pos);
                go.transform.rotation = Quaternion.Euler(0, (float)(g.Yaw * 57.2958), 0);
                fx.SetWrecked(go, !g.Alive);
            }
            fx.Sync(Sess);
        }
    }

    public sealed class RadioLine { public string De, Texto; public float T; }

    public sealed class RadioDirector
    {
        readonly Combate c;
        readonly Dictionary<string, string> vars = new Dictionary<string, string>();
        readonly Queue<RadioLine> queue = new Queue<RadioLine>();
        public RadioLine Current { get; private set; }
        public readonly List<RadioLine> History = new List<RadioLine>();
        float hold, cooldownMsl;
        bool saidClimb, saidBrief, saidFinal;
        readonly BaseData b;

        public RadioDirector(Combate c, BaseData b, RunwayData rw)
        {
            this.c = c; this.b = b;
            string nome = b.nome.Replace("Base Aérea de ", "").Replace("Aeroporto de ", "").Replace("Aeroporto do ", "").Split(' ')[0];
            vars["{indicativo}"] = "Falcão 1";
            vars["{torre}"] = "Torre " + nome;
            vars["{controle}"] = "Controle de Defesa Aérea";
            vars["{pista}"] = (rw.cabeceira_a?.ident ?? rw.ident).Split('/')[0];
            vars["{vento}"] = "090 graus, 8 nós";
            vars["{qnh}"] = "1015";
            vars["{altitude}"] = "15 mil pés";
            vars["{rumo}"] = ((int)rw.rumo_verdadeiro).ToString("000");
            vars["{alvo}"] = "contato hostil";
            vars["{posicao}"] = "à frente";
            vars["{arma}"] = "míssil";
            vars["{base}"] = nome;
            var m = c.Missao;
            Fala("controle", $"{{indicativo}}, {{controle}}. Missão {m.titulo}.");
            if (m.objetivos != null && m.objetivos.Length > 0) Fala("controle", "Objetivo: " + m.objetivos[0].descricao);
            Say("decolagem", 0);
        }

        string Fill(string s) { foreach (var kv in vars) s = s.Replace(kv.Key, kv.Value); return s; }

        public void Diz(string de, string texto) => queue.Enqueue(new RadioLine { De = de, Texto = Fill(texto) });

        void Fala(string de, string texto) => queue.Enqueue(new RadioLine { De = de == "torre" ? vars["{torre}"] : de == "controle" ? "Controle" : vars["{indicativo}"], Texto = Fill(texto) });

        Dialogo[] Eventos(string ev)
        {
            var r = GameData.Radio;
            switch (ev)
            {
                case "decolagem": return r.decolagem;
                case "briefing_em_voo": return r.briefing_em_voo;
                case "alvo_detectado": return r.alvo_detectado;
                case "missil_lancado": return r.missil_lancado;
                case "objetivo_cumprido": return r.objetivo_cumprido;
                case "pouso": return r.pouso;
                default: return null;
            }
        }

        public void Say(string ev, int idx = -1)
        {
            var list = Eventos(ev);
            if (list == null || list.Length == 0) return;
            UpdateContext();
            var d = list[idx >= 0 ? Math.Min(idx, list.Length - 1) : 0];
            if (ev == "missil_lancado")
            {
                if (cooldownMsl > 0) return;
                cooldownMsl = 8;
                if (idx == 0) { vars["{arma}"] = c.Sess.Sel == WeaponKind.Aam ? "míssil" : "foguete"; while (queue.Count > 2) queue.Dequeue(); }
            }
            foreach (var f in d.dialogo) Fala(f.de, f.texto);
        }

        void UpdateContext()
        {
            var s = c.Sess;
            AirUnit near = null; double best = 1e12;
            foreach (var a in s.Air) { if (a.Team != Team.Red || a.Dying > 0) continue; double dist = Cm.Dist(a.Pos, s.Pl.Pos); if (dist < best) { best = dist; near = a; } }
            vars["{rumo}"] = ((int)Math.Round(c.F.S.Diag.Hdg)).ToString("000");
            if (near == null) return;
            V3 r = near.Pos - s.Pl.Pos;
            double brg = Math.Atan2(r.X, r.Y) * 180 / Math.PI;
            double rel = ((brg - c.F.S.Diag.Hdg) % 360 + 360) % 360;
            int hora = (int)Math.Round(rel / 30) % 12; if (hora == 0) hora = 12;
            vars["{rumo}"] = ((int)((brg + 360) % 360)).ToString("000");
            vars["{posicao}"] = $"{hora} horas, {best / 1000:0} quilômetros, {near.Pos.Z * 3.281 / 100:0}00 pés";
            vars["{alvo}"] = near.Profile.Armed ? $"{near.Name} hostil, força {near.Forca}" : $"{near.Name} não cooperativo";
        }

        public void OnWave()
        {
            UpdateContext();
            Say("briefing_em_voo", 0);
            c.Audio.RadioBlip();
        }

        public void Update(float dt)
        {
            var d = c.F.S.Diag;
            cooldownMsl -= dt;
            if (!saidClimb && d.Agl > 100) { saidClimb = true; Say("decolagem", 1); }
            if (!saidBrief && d.Agl > 500) { saidBrief = true; if (c.Ativo != null) { vars["{altitude}"] = "15 mil pés"; Say("briefing_em_voo", 1); } }
            if (!saidFinal && saidClimb && c.F.Ctl.GearDown && d.Agl < 1000 && !c.F.S.Contact && c.Sess.Pl.Spd < 130) { saidFinal = true; Say("pouso", 0); }
            hold -= dt;
            if (Current != null && hold <= 0) { History.Add(Current); if (History.Count > 4) History.RemoveAt(0); Current = null; hold = 0.5f; }
            if (Current == null && hold <= 0 && queue.Count > 0)
            {
                Current = queue.Dequeue();
                hold = Mathf.Clamp(Current.Texto.Length * 0.065f, 2.5f, 8f);
                c.Audio.RadioBlip();
            }
        }
    }
}
