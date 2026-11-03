using System;
using System.Collections.Generic;
using System.IO;
using FABSim.Core;
using UnityEngine;

namespace FABSim.Game
{
    [Serializable] public class Valor { public double valor; public bool estimado; }
    [Serializable] public class ArmaDef { public string id, nome, categoria; public double peso_kg; public bool estimado; }
    [Serializable] public class PontoDef { public string id, posicao; public double limite_kg; public string[] armas_permitidas; }
    [Serializable] public class AeronaveArm { public string id, nome; public Valor peso_maximo_decolagem_kg, peso_vazio_kg, carga_externa_max_kg; public PontoDef[] pontos_de_fixacao; }
    [Serializable] public class ArmamentoFile { public ArmaDef[] armas; public AeronaveArm[] aeronaves; }

    [Serializable] public class PosGeo { public double lat, lon, altitude_m; }
    [Serializable] public class AtivoDef { public string id, nome; public PosGeo posicao; public double raio_de_seguranca_km; }
    [Serializable] public class ObjetivoDef { public string id, tipo, descricao; public bool obrigatorio; }
    [Serializable] public class InimigoDef { public string tipo, comportamento, forca; public int quantidade; public PosGeo posicao; }
    [Serializable]
    public class MissaoDef
    {
        public string id, titulo, tema, briefing, base_de_saida, dificuldade_minima;
        public string[] aeronaves_permitidas;
        public AtivoDef[] ativos_a_proteger;
        public ObjetivoDef[] objetivos;
        public InimigoDef[] inimigos;
        [NonSerialized] public bool AlvosSolo, Invulneravel, SemAla, Reabastecer, Noite, Rapto;
        [NonSerialized] public string Destino;
        [NonSerialized] public double EncontroLat, EncontroLon, CombustivelFrac;
    }
    [Serializable] public class MissoesFile { public MissaoDef[] missoes; }

    [Serializable] public class Fala { public string de, texto; }
    [Serializable] public class Dialogo { public string id; public Fala[] dialogo; }
    [Serializable] public class RadioEventos { public Dialogo[] acionamento, taxi, decolagem, briefing_em_voo, alvo_detectado, missil_lancado, objetivo_cumprido, pouso, pane; }
    [Serializable] public class RadioFile { public RadioEventos eventos; }

    public static class GameData
    {
        static T Load<T>(string name) => JsonUtility.FromJson<T>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Data", name)));

        static ArmamentoFile arm;
        static List<MissaoDef> missoes;
        static RadioEventos radio;

        public static ArmamentoFile Armamento => arm ??= Load<ArmamentoFile>("armamento.json");
        public static RadioEventos Radio => radio ??= Load<RadioFile>("radio.json").eventos;

        public static List<MissaoDef> Missoes
        {
            get
            {
                if (missoes != null) return missoes;
                missoes = new List<MissaoDef>(Load<MissoesFile>("missoes.json").missoes);
                missoes.Add(new MissaoDef
                {
                    id = "treino-tiro", titulo = "Treino de Tiro ao Solo", tema = "treinamento", base_de_saida = "SBAN",
                    briefing = "Estande de tiro a 15 km da cabeceira, na proa da pista. Use as miras CCIP e CCRP com bombas e foguetes e o canhão contra os alvos marcados. Sem risco para a aeronave.",
                    aeronaves_permitidas = new[] { "F-39E", "A-29" }, AlvosSolo = true, Invulneravel = true, SemAla = true,
                    objetivos = new[] { new ObjetivoDef { id = "t1", descricao = "Destruir os alvos principais do estande.", obrigatorio = true } },
                });
                missoes.Add(new MissaoDef
                {
                    id = "nao-vai-ter-golpe", titulo = "Não vai ter Golpe", tema = "cenário fictício: interceptação noturna contra forças dos EUA", base_de_saida = "SBAN", Noite = true, Rapto = true,
                    briefing = "Cenário hipotético de jogo. Noite em Brasília: helicópteros de operações especiais dos EUA voam baixo rumo à capital para capturar o presidente da República, com caças de escolta. Decole de Anápolis, encontre a formação no radar e derrube os helicópteros antes que cheguem ao Palácio do Planalto. Se um deles chegar, a missão fracassa.",
                    aeronaves_permitidas = new[] { "F-39E", "A-29" },
                    ativos_a_proteger = new[] { new AtivoDef { id = "planalto", nome = "Palácio do Planalto", posicao = new PosGeo { lat = -15.7995, lon = -47.8606, altitude_m = 1100 }, raio_de_seguranca_km = 2 } },
                    objetivos = new[]
                    {
                        new ObjetivoDef { id = "g1", descricao = "Derrubar os MH-47G e MH-60M antes do Palácio.", obrigatorio = true },
                        new ObjetivoDef { id = "g2", descricao = "Neutralizar os caças de escolta.", obrigatorio = true },
                        new ObjetivoDef { id = "g3", descricao = "Pousar em Anápolis.", obrigatorio = true },
                    },
                });
                missoes.Add(new MissaoDef
                {
                    id = "voo-rio-revo", titulo = "Anápolis–Rio com Reabastecimento", tema = "navegação e reabastecimento em voo", base_de_saida = "SBAN",
                    briefing = "Traslado do F-39E de Anápolis ao Galeão. Você decola sem combustível para o trecho inteiro: um KC-390 aguarda em órbita a 6.100 m, no meio da rota, sobre Minas Gerais. Siga a rota no mapa, estenda a sonda, encaixe na cesta e receba combustível. Depois continue e pouse no Galeão.",
                    aeronaves_permitidas = new[] { "F-39E" }, SemAla = true, Reabastecer = true, Destino = "SBGL", EncontroLat = -19.52, EncontroLon = -46.11, CombustivelFrac = 0.5,
                    objetivos = new[]
                    {
                        new ObjetivoDef { id = "r1", descricao = "Encontrar o KC-390 no ponto REVO, a 6.100 m.", obrigatorio = true },
                        new ObjetivoDef { id = "r2", descricao = "Conectar a sonda na cesta e reabastecer.", obrigatorio = true },
                        new ObjetivoDef { id = "r3", descricao = "Pousar no Galeão (SBGL).", obrigatorio = true },
                    },
                });
                missoes.Add(new MissaoDef
                {
                    id = "voo-livre", titulo = "Voo Livre", tema = "sem inimigos", base_de_saida = "SBAN",
                    briefing = "Voo livre sem inimigos a partir de Anápolis. Todas as bases mapeadas têm pista pronta para pouso.",
                    aeronaves_permitidas = new[] { "F-39E", "A-29", "KC-390" }, SemAla = true,
                });
                return missoes;
            }
        }

        public static string JsonId(string id) => id == "a29" ? "A-29" : id == "kc390" ? "KC-390" : "F-39E";
        public static AeronaveArm Aeronave(string id) => Array.Find(Armamento.aeronaves, a => a.id == JsonId(id));
        public static ArmaDef Arma(string id) => Array.Find(Armamento.armas, a => a.id == id);
        public static MissaoDef Missao(string id) => Missoes.Find(m => m.id == id);
    }

    public static class GameSession
    {
        public static string AircraftId = Environment.GetEnvironmentVariable("FAB_AC") ?? "f39e", MissaoId = Environment.GetEnvironmentVariable("FAB_MISSAO") ?? "m04-defesa-da-hidreletrica", Dificuldade = "normal";
        public static readonly Dictionary<string, string> Stations = new Dictionary<string, string>();
        static string stationsFor;

        public static readonly string[] Dificuldades = { "facil", "normal", "veterano" };
        public static string NomeDificuldade(string d) => d == "facil" ? "Fácil" : d == "veterano" ? "Veterano" : "Normal";
        public static Difficulty Diff(string d) => d == "facil" ? Difficulty.Facil : d == "veterano" ? Difficulty.Veterano : Difficulty.Normal;

        public static void EnsureStations()
        {
            if (stationsFor == AircraftId) return;
            stationsFor = AircraftId;
            Stations.Clear();
            var ac = GameData.Aeronave(AircraftId);
            if (ac?.pontos_de_fixacao == null) return;
            foreach (var p in ac.pontos_de_fixacao)
            {
                string w = "";
                foreach (var pref in Preferidas(AircraftId, p.id)) if (Array.IndexOf(p.armas_permitidas, pref) >= 0) { w = pref; break; }
                Stations[p.id] = w;
            }
        }

        static string[] Preferidas(string ac, string st)
        {
            if (ac == "a29") return st == "P3" ? new[] { "tanque-300l" } : new[] { "casulo-foguetes-70", "mk82" };
            if (st == "P9") return new[] { "tanque-1100l" };
            if (st == "P5" || st == "P6") return new[] { "mk82" };
            if (st == "P10") return new[] { "casulo-sensor" };
            if (st == "P3" || st == "P4") return new[] { "a-darter" };
            return new[] { "iris-t", "meteor" };
        }

        public static double StoreMass()
        {
            EnsureStations();
            double m = 0;
            foreach (var kv in Stations) { var w = GameData.Arma(kv.Value); if (w != null) m += w.peso_kg; }
            return m;
        }

        public static double ExtraFuel()
        {
            EnsureStations();
            double f = 0;
            foreach (var kv in Stations) f += kv.Value == "tanque-1100l" ? 880 : kv.Value == "tanque-300l" ? 240 : 0;
            return f;
        }

        public static Loadout BuildLoadout()
        {
            EnsureStations();
            var l = AircraftDb.DefaultLoadout(AircraftId);
            if (AircraftId == "kc390") return l;
            l.Aam = l.Bomb = l.Rkt = 0;
            foreach (var kv in Stations)
            {
                var w = GameData.Arma(kv.Value);
                if (w == null) continue;
                if (w.categoria.StartsWith("ar-ar")) l.Aam++;
                else if (w.categoria.Contains("bomba")) l.Bomb++;
                else if (w.categoria.Contains("foguete")) l.Rkt += 7;
            }
            return l;
        }

        public static double MassPerRound(WeaponKind k)
        {
            string prefix = k == WeaponKind.Aam ? "ar-ar" : k == WeaponKind.Bomb ? "bomba" : "foguete";
            double m = 0; int n = 0;
            foreach (var kv in Stations)
            {
                var w = GameData.Arma(kv.Value);
                if (w == null || !w.categoria.Contains(prefix)) continue;
                m += w.peso_kg; n += k == WeaponKind.Rkt ? 7 : 1;
            }
            return n > 0 ? m / n : 0;
        }
    }
}
