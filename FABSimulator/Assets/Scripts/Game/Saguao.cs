using System;
using System.Collections.Generic;
using FABSim.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace FABSim.Game
{
    public sealed class Saguao : MonoBehaviour
    {
        public Transform turntable;
        public Camera cam;

        static readonly string[] Ids = { "f39e", "a29", "kc390" };
        static readonly Color Azul = new Color(0.27f, 0.62f, 1f), Verde = new Color(0.35f, 1f, 0.55f), Amb = new Color(1f, 0.78f, 0.25f), Verm = new Color(1f, 0.32f, 0.26f), Fundo = new Color(0.02f, 0.05f, 0.08f, 0.86f);

        public int Tela { get; private set; }
        int acIdx, row, modelFor = -1;
        VisualElement root, left, right;
        Label titulo, passo;
        GameObject modelRoot;
        float modelLen = 15;
        string aviso;
        readonly List<string> missoesVis = new List<string>();

        void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.left = 0; root.style.right = 0; root.style.top = 0; root.style.bottom = 0;
            root.pickingMode = PickingMode.Ignore;

            var head = new VisualElement { pickingMode = PickingMode.Ignore };
            head.style.position = Position.Absolute; head.style.left = 40; head.style.top = 26;
            titulo = new Label("FAB SIMULATOR") { pickingMode = PickingMode.Ignore }; titulo.style.fontSize = 40; titulo.style.color = Color.white; titulo.style.unityFontStyleAndWeight = FontStyle.Bold;
            var sub = new Label("Defendendo a Soberania Nacional") { pickingMode = PickingMode.Ignore }; sub.style.fontSize = 17; sub.style.color = Azul;
            head.Add(titulo); head.Add(sub); root.Add(head);

            passo = new Label { pickingMode = PickingMode.Ignore };
            passo.style.position = Position.Absolute; passo.style.left = 40; passo.style.bottom = 22; passo.style.fontSize = 16; passo.style.color = new Color(.8f, .86f, .92f);
            root.Add(passo);

            left = Panel(); left.style.left = 40; left.style.top = 118; left.style.width = 470; left.style.bottom = 64;
            right = Panel(); right.style.right = 40; right.style.top = 118; right.style.width = 470; right.style.bottom = 64;
            root.Add(left); root.Add(right);

            GameSession.EnsureStations();
            acIdx = Math.Max(0, Array.IndexOf(Ids, GameSession.AircraftId));
            Build();
        }

        static VisualElement Panel()
        {
            var p = new VisualElement();
            p.style.position = Position.Absolute;
            p.style.backgroundColor = Fundo;
            p.style.paddingLeft = 22; p.style.paddingRight = 22; p.style.paddingTop = 18; p.style.paddingBottom = 18;
            p.style.borderTopLeftRadius = 8; p.style.borderTopRightRadius = 8; p.style.borderBottomLeftRadius = 8; p.style.borderBottomRightRadius = 8;
            return p;
        }

        static Label Lbl(VisualElement parent, string text, float size, Color color, bool bold = false, float mb = 0)
        {
            var l = new Label(text);
            l.style.fontSize = size; l.style.color = color; l.style.whiteSpace = WhiteSpace.Normal; l.style.marginBottom = mb;
            if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold;
            parent.Add(l);
            return l;
        }

        VisualElement Row(VisualElement parent, bool selected, int index, Action click)
        {
            var r = new VisualElement();
            r.style.backgroundColor = selected ? new Color(Azul.r, Azul.g, Azul.b, 0.28f) : new Color(1, 1, 1, 0.05f);
            r.style.borderLeftWidth = 4; r.style.borderLeftColor = selected ? Azul : new Color(0, 0, 0, 0);
            r.style.paddingLeft = 12; r.style.paddingRight = 10; r.style.paddingTop = 7; r.style.paddingBottom = 7; r.style.marginBottom = 5;
            r.style.borderTopRightRadius = 5; r.style.borderBottomRightRadius = 5;
            r.RegisterCallback<ClickEvent>(_ => { row = index; click?.Invoke(); Build(); });
            parent.Add(r);
            return r;
        }

        static void Stat(VisualElement parent, string k, string v)
        {
            var r = new VisualElement();
            r.style.flexDirection = FlexDirection.Row; r.style.justifyContent = Justify.SpaceBetween; r.style.marginBottom = 7;
            r.style.borderBottomWidth = 1; r.style.borderBottomColor = new Color(1, 1, 1, 0.09f); r.style.paddingBottom = 5;
            var a = new Label(k); a.style.color = new Color(.65f, .72f, .8f); a.style.fontSize = 15; a.style.flexShrink = 0;
            var b = new Label(v); b.style.color = Color.white; b.style.fontSize = 15; b.style.whiteSpace = WhiteSpace.Normal; b.style.unityTextAlign = TextAnchor.MiddleRight; b.style.maxWidth = 280;
            r.Add(a); r.Add(b); parent.Add(r);
        }

        static void Bar(VisualElement parent, string nome, double v, double max, string txt)
        {
            Lbl(parent, $"{nome}: {txt}", 15, v > max ? Verm : Color.white, false, 3);
            var bg = new VisualElement(); bg.style.height = 11; bg.style.backgroundColor = new Color(1, 1, 1, 0.1f); bg.style.marginBottom = 12;
            var fg = new VisualElement(); fg.style.height = 11;
            fg.style.width = Length.Percent((float)Math.Min(100, v / max * 100));
            fg.style.backgroundColor = v > max ? Verm : v > max * 0.85 ? Amb : Verde;
            bg.Add(fg); parent.Add(bg);
        }

        void Build()
        {
            left.Clear(); right.Clear();
            ShowModel();
            if (Tela == 0) BuildAeronave();
            else if (Tela == 1) BuildArmamento();
            else BuildMissao();
            passo.text = (Tela == 0 ? "1 de 3 · Aeronave" : Tela == 1 ? "2 de 3 · Armamento" : "3 de 3 · Missão") + "      ↑↓ / D-pad escolher    ←→ alterar    Enter / A avançar    Esc / B voltar";
        }

        void ShowModel()
        {
            if (modelFor == acIdx) return;
            modelFor = acIdx;
            if (modelRoot != null) Destroy(modelRoot);
            modelRoot = new GameObject("Vitrine");
            modelRoot.transform.SetParent(turntable, false);
            string key = Ids[acIdx] == "a29" ? "A29" : Ids[acIdx] == "kc390" ? "KC390" : "F39E";
            var fit = ModelFit.Spawn(key, modelRoot.transform);
            if (fit != null) AircraftRig.Build(fit, key, (key == "A29" ? AircraftDb.A29() : key == "KC390" ? AircraftDb.KC390() : AircraftDb.F39E()).Prepare()).Apply(new FlightState(), 0);
            modelLen = fit != null ? fit.Length : 15;
            float minY = float.MaxValue;
            foreach (var r in modelRoot.GetComponentsInChildren<Renderer>()) if (r.enabled) minY = Mathf.Min(minY, r.bounds.min.y);
            if (minY < float.MaxValue) modelRoot.transform.position += Vector3.up * -minY;
        }

        void BuildAeronave()
        {
            Lbl(left, "Escolha a aeronave", 24, Color.white, true, 12);
            for (int i = 0; i < Ids.Length; i++)
            {
                int idx = i;
                var info = AircraftDb.Info(Ids[i]);
                var r = Row(left, acIdx == i, i, () => Select(idx));
                Lbl(r, info.Designation, 21, Color.white, true);
                Lbl(r, info.Role, 14, new Color(.72f, .8f, .88f));
            }
            var a = AircraftDb.Info(Ids[acIdx]);
            Lbl(right, a.Designation, 27, Color.white, true, 2);
            Lbl(right, a.Manufacturer, 15, Azul, false, 14);
            Stat(right, "Motor", a.EngineText);
            Stat(right, "Velocidade máxima", a.MaxSpeed);
            Stat(right, "Teto", a.Ceiling);
            Stat(right, "Peso máx. decolagem", a.MaxWeight);
            Stat(right, "Comprimento", a.Length);
            Stat(right, "Envergadura", a.Span);
            Stat(right, "Altura", a.Height);
            Stat(right, "Unidade", a.Squadrons);
            var l = AircraftDb.DefaultLoadout(Ids[acIdx]);
            Stat(right, "Armamento interno", l.Gun.Name);
            Stat(right, "Mira", l.Sight.Name);
            Lbl(right, "Dados do site da FAB.", 13, new Color(.55f, .62f, .7f));
        }

        void Select(int i)
        {
            acIdx = i;
            GameSession.AircraftId = Ids[i];
            GameSession.EnsureStations();
        }

        void BuildArmamento()
        {
            var ac = GameData.Aeronave(GameSession.AircraftId);
            Lbl(left, "Armamento", 24, Color.white, true, 10);
            var pts = ac.pontos_de_fixacao;
            if (pts == null || pts.Length == 0)
            {
                Lbl(left, "O KC-390 não leva armamento: só contramedidas. Na FAB ele faz transporte, reabastecimento em voo, lançamento e combate a incêndio.", 17, new Color(.85f, .9f, .95f));
                BuildPesos(ac, 0);
                return;
            }
            var scroll = new ScrollView(); scroll.style.flexGrow = 1;
            left.Add(scroll);
            for (int i = 0; i < pts.Length; i++)
            {
                var p = pts[i];
                int idx = i;
                var r = Row(scroll, row == i, i, () => CycleStation(p, 1));
                r.style.flexDirection = FlexDirection.Row; r.style.justifyContent = Justify.SpaceBetween;
                var col = new VisualElement(); col.style.flexShrink = 1;
                Lbl(col, $"{p.id} · {p.posicao}", 13, new Color(.65f, .72f, .8f));
                var w = GameData.Arma(GameSession.Stations.TryGetValue(p.id, out var id) ? id : "");
                Lbl(col, w != null ? w.nome : "Vazio", 16, w != null ? Color.white : new Color(.5f, .55f, .6f), w != null);
                r.Add(col);
                var d = new VisualElement();
                Lbl(d, w != null ? $"{w.peso_kg:0} kg" : "", 15, Color.white).style.unityTextAlign = TextAnchor.MiddleRight;
                Lbl(d, $"lim {p.limite_kg:0}", 12, new Color(.55f, .62f, .7f)).style.unityTextAlign = TextAnchor.MiddleRight;
                r.Add(d);
            }
            BuildPesos(ac, GameSession.StoreMass());
        }

        void BuildPesos(AeronaveArm ac, double stores)
        {
            Lbl(right, "Peso e capacidade", 24, Color.white, true, 14);
            var def = AircraftDb.Get(GameSession.AircraftId);
            if (ac.carga_externa_max_kg != null && ac.carga_externa_max_kg.valor > 0)
            {
                Bar(right, "Carga externa", stores, ac.carga_externa_max_kg.valor, $"{stores:0} / {ac.carga_externa_max_kg.valor:0} kg");
                double fuel = def.FuelMax * 0.6 + GameSession.ExtraFuel();
                double tow = def.Mass0 + fuel + stores;
                double mtow = ac.peso_maximo_decolagem_kg.valor;
                Bar(right, "Peso de decolagem", tow, mtow, $"{tow:0} / {mtow:0} kg");
                Stat(right, "Vazia", $"{def.Mass0:0} kg");
                Stat(right, "Combustível (60% + tanques)", $"{fuel:0} kg");
                aviso = stores > ac.carga_externa_max_kg.valor ? "Carga externa acima do limite" : tow > mtow ? "Peso de decolagem acima do limite" : null;
            }
            else { Stat(right, "Vazia", $"{def.Mass0:0} kg"); aviso = null; }
            var l = GameSession.BuildLoadout();
            Lbl(right, "Carga de combate", 18, Azul, true, 8).style.marginTop = 10;
            Stat(right, l.Gun.Name, $"{l.Gun.Rounds} tiros");
            if (l.Aam > 0) Stat(right, "Mísseis ar-ar", l.Aam.ToString());
            if (l.Bomb > 0) Stat(right, "Bombas", l.Bomb.ToString());
            if (l.Rkt > 0) Stat(right, "Foguetes 70 mm", l.Rkt.ToString());
            Stat(right, "Contramedidas", l.Flares.ToString());
            Stat(right, "Mira", l.Sight.Name);
            if (aviso != null) Lbl(right, aviso, 18, Verm, true);
            Lbl(right, "Limites por ponto são simplificações de jogo.", 13, new Color(.55f, .62f, .7f)).style.marginTop = 6;
        }

        void CycleStation(PontoDef p, int d)
        {
            var opts = new List<string> { "" };
            foreach (var w in p.armas_permitidas) { var a = GameData.Arma(w); if (a != null && a.peso_kg <= p.limite_kg) opts.Add(w); }
            string cur = GameSession.Stations.TryGetValue(p.id, out var id) ? id : "";
            int i = opts.IndexOf(cur);
            GameSession.Stations[p.id] = opts[(Math.Max(i, 0) + d + opts.Count) % opts.Count];
        }

        void BuildMissao()
        {
            Lbl(left, "Missão", 24, Color.white, true, 10);
            missoesVis.Clear();
            string jid = GameData.JsonId(GameSession.AircraftId);
            foreach (var m in GameData.Missoes) if (Array.IndexOf(m.aeronaves_permitidas, jid) >= 0) missoesVis.Add(m.id);
            int cur = missoesVis.IndexOf(GameSession.MissaoId);
            if (cur < 0) { cur = 0; GameSession.MissaoId = missoesVis[0]; }
            for (int i = 0; i < missoesVis.Count; i++)
            {
                var m = GameData.Missao(missoesVis[i]);
                string id = m.id;
                var r = Row(left, i == cur, i, () => GameSession.MissaoId = id);
                Lbl(r, m.titulo, 19, Color.white, true);
                Lbl(r, m.tema + " · " + m.base_de_saida, 13, new Color(.72f, .8f, .88f));
            }
            var d = GameData.Missao(GameSession.MissaoId);
            Lbl(right, d.titulo, 27, Color.white, true, 8);
            Lbl(right, d.briefing, 16, new Color(.88f, .92f, .96f), false, 12);
            if (d.objetivos != null)
            {
                Lbl(right, "Objetivos", 17, Azul, true, 4);
                foreach (var o in d.objetivos) Lbl(right, "• " + o.descricao, 14, new Color(.82f, .87f, .92f), false, 3);
            }
            bool forced = d.dificuldade_minima == "veterano";
            string dif = forced ? "veterano" : GameSession.Dificuldade;
            var box = new VisualElement(); box.style.flexDirection = FlexDirection.Row; box.style.marginTop = 16;
            foreach (var x in GameSession.Dificuldades)
            {
                string dd = x;
                bool on = x == dif;
                var b = new Label(GameSession.NomeDificuldade(x));
                b.style.fontSize = 16; b.style.color = on ? Color.black : Color.white; b.style.backgroundColor = on ? Azul : new Color(1, 1, 1, 0.08f);
                b.style.paddingLeft = 14; b.style.paddingRight = 14; b.style.paddingTop = 7; b.style.paddingBottom = 7; b.style.marginRight = 6;
                if (!forced) b.RegisterCallback<ClickEvent>(_ => { GameSession.Dificuldade = dd; Build(); });
                box.Add(b);
            }
            right.Add(box);
            if (forced) Lbl(right, "Esta missão exige dificuldade Veterano: F-22 Raptor em combate.", 14, Amb).style.marginTop = 8;
            var go = new Label("DECOLAR") { };
            go.style.fontSize = 24; go.style.color = Color.black; go.style.backgroundColor = Verde; go.style.unityTextAlign = TextAnchor.MiddleCenter; go.style.height = 54; go.style.marginTop = 18; go.style.unityFontStyleAndWeight = FontStyle.Bold;
            go.RegisterCallback<ClickEvent>(_ => Next());
            right.Add(go);
        }

        public void Next()
        {
            if (Tela == 1 && aviso != null) return;
            if (Tela == 2) { titulo.text = "Carregando…"; SceneManager.LoadScene("Voo"); return; }
            Tela++; row = 0;
            Build();
        }

        public void Back() { if (Tela > 0) { Tela--; row = 0; Build(); } }

        void Move(int d)
        {
            if (Tela == 0) Select((acIdx + d + Ids.Length) % Ids.Length);
            else if (Tela == 1)
            {
                var p = GameData.Aeronave(GameSession.AircraftId).pontos_de_fixacao;
                if (p == null || p.Length == 0) return;
                row = (row + d + p.Length) % p.Length;
            }
            else
            {
                int i = missoesVis.IndexOf(GameSession.MissaoId);
                GameSession.MissaoId = missoesVis[(i + d + missoesVis.Count) % missoesVis.Count];
            }
            Build();
        }

        void Alter(int d)
        {
            if (Tela == 1)
            {
                var p = GameData.Aeronave(GameSession.AircraftId).pontos_de_fixacao;
                if (p != null && p.Length > 0) CycleStation(p[row], d);
            }
            else if (Tela == 2)
            {
                int i = Array.IndexOf(GameSession.Dificuldades, GameSession.Dificuldade);
                GameSession.Dificuldade = GameSession.Dificuldades[(i + d + 3) % 3];
            }
            else return;
            Build();
        }

        void Update()
        {
            if (root == null) return;
            if (turntable != null) turntable.Rotate(0, 12 * Time.deltaTime, 0);
            if (cam != null)
            {
                float dist = modelLen * 1.75f;
                var target = Vector3.up * 1.2f;
                cam.transform.position = Vector3.Lerp(cam.transform.position, target + new Vector3(0, modelLen * 0.28f, -dist), 1 - Mathf.Exp(-Time.deltaTime * 4));
                cam.transform.rotation = Quaternion.LookRotation(target - cam.transform.position);
            }
            var pad = Gamepad.current; var kb = Keyboard.current;
            if (pad != null)
            {
                if (pad.dpad.down.wasPressedThisFrame) Move(1);
                if (pad.dpad.up.wasPressedThisFrame) Move(-1);
                if (pad.dpad.right.wasPressedThisFrame) Alter(1);
                if (pad.dpad.left.wasPressedThisFrame) Alter(-1);
                if (pad.buttonSouth.wasPressedThisFrame) Next();
                if (pad.buttonEast.wasPressedThisFrame) Back();
            }
            if (kb != null)
            {
                if (kb.downArrowKey.wasPressedThisFrame) Move(1);
                if (kb.upArrowKey.wasPressedThisFrame) Move(-1);
                if (kb.rightArrowKey.wasPressedThisFrame) Alter(1);
                if (kb.leftArrowKey.wasPressedThisFrame) Alter(-1);
                if (kb.enterKey.wasPressedThisFrame) Next();
                if (kb.escapeKey.wasPressedThisFrame) Back();
            }
        }
    }
}
