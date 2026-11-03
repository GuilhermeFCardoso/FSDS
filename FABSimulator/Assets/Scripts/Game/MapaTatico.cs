using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace FABSim.Game
{
    public sealed class PontoRota { public double Lat, Lon; public string Nome; }

    // A rota sobrevive ao reinicio da missao, como no simulador do navegador.
    public static class Rota
    {
        const double R = 6371008.8, D2R = Math.PI / 180;
        public static readonly List<PontoRota> Pontos = new List<PontoRota>();
        public static int Ativo;

        public static PontoRota Atual => Ativo < Pontos.Count ? Pontos[Ativo] : null;
        public static string Nome(int i) => Pontos[i].Nome ?? $"WP{i + 1}";

        public static double Dist(double lat1, double lon1, double lat2, double lon2)
        {
            double a = Math.Pow(Math.Sin((lat2 - lat1) * D2R / 2), 2) + Math.Cos(lat1 * D2R) * Math.Cos(lat2 * D2R) * Math.Pow(Math.Sin((lon2 - lon1) * D2R / 2), 2);
            return 2 * R * Math.Asin(Math.Sqrt(a));
        }

        public static double Rumo(double lat1, double lon1, double lat2, double lon2)
        {
            double p1 = lat1 * D2R, p2 = lat2 * D2R, dn = (lon2 - lon1) * D2R;
            return (Math.Atan2(Math.Sin(dn) * Math.Cos(p2), Math.Cos(p1) * Math.Sin(p2) - Math.Sin(p1) * Math.Cos(p2) * Math.Cos(dn)) / D2R + 360) % 360;
        }

        public static void Avancar(double lat, double lon, double gs)
        {
            while (Atual != null && Dist(lat, lon, Atual.Lat, Atual.Lon) < Math.Max(900, gs * 8)) Ativo++;
        }

        public static void Remover(int i) { Pontos.RemoveAt(i); Ativo = Math.Min(Ativo, Pontos.Count); }
        public static void Limpar() { Pontos.Clear(); Ativo = 0; }

        public static string Tempo(double s) => s >= 3600 ? $"{(int)(s / 3600)} h {(int)(s / 60) % 60:00} min" : $"{(int)(s / 60)}:{(int)(s % 60):00}";
        public static string Km(double m) => m < 9950 ? $"{m / 1000:0.0} km" : $"{m / 1000:0} km";
    }

    // Camada de ladrilhos e entrada do mapa; os simbolos sao desenhados pelo Hud.
    public sealed class MapaTatico
    {
        const int Tile = 256, MinZoom = 3, MaxZoom = 14;
        static readonly (string nome, string url, string credito)[] Fontes =
        {
            ("Mapa", "https://tile.openstreetmap.org/{0}/{1}/{2}.png", "© OpenStreetMap"),
            ("Satélite", "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{0}/{2}/{1}", "Esri, Maxar"),
        };
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        static readonly HashSet<string> baixando = new HashSet<string>();
        static int fonte;

        readonly FlightController f;
        readonly Dictionary<string, VisualElement> ladrilhos = new Dictionary<string, VisualElement>();
        readonly HashSet<string> visiveis = new HashSet<string>();
        readonly VisualElement barra;
        readonly Button botaoFonte;
        bool arrastando, moveu;
        Vector2 inicio;
        float W, H;

        public VisualElement Camada { get; }
        public bool Aberto { get; private set; }
        public int Zoom { get; private set; } = 9;
        public double CentroLat { get; private set; }
        public double CentroLon { get; private set; }
        public Func<Vector2, PontoRota> PontoEm;
        public string Credito => Fontes[fonte].credito;

        public MapaTatico(VisualElement root, FlightController f)
        {
            this.f = f;
            Camada = new VisualElement();
            Camada.style.position = Position.Absolute;
            Camada.style.left = Camada.style.right = Camada.style.top = Camada.style.bottom = 0;
            Camada.style.backgroundColor = new Color(0.05f, 0.09f, 0.12f, 1);
            Camada.style.overflow = Overflow.Hidden;
            Camada.style.display = DisplayStyle.None;
            root.Insert(0, Camada);
            Camada.RegisterCallback<PointerDownEvent>(Apertou);
            Camada.RegisterCallback<PointerMoveEvent>(Moveu);
            Camada.RegisterCallback<PointerUpEvent>(Soltou);
            Camada.RegisterCallback<WheelEvent>(e => MudarZoom(e.delta.y < 0 ? 1 : -1));

            barra = new VisualElement();
            barra.style.position = Position.Absolute; barra.style.left = 0; barra.style.right = 0; barra.style.top = 0;
            barra.style.flexDirection = FlexDirection.Row; barra.style.alignItems = Align.Center;
            barra.style.paddingLeft = 16; barra.style.height = 44;
            barra.style.backgroundColor = new Color(0.02f, 0.05f, 0.07f, 0.88f);
            var titulo = new Label("Mapa e rota"); titulo.style.color = Color.white; titulo.style.fontSize = 18; titulo.style.marginRight = 18;
            barra.Add(titulo);
            Botao("Centralizar", Centralizar);
            botaoFonte = Botao(Fontes[1].nome, () => { fonte = (fonte + 1) % Fontes.Length; botaoFonte.text = Fontes[(fonte + 1) % Fontes.Length].nome; LimparLadrilhos(); });
            Botao("Desfazer último", () => { if (Rota.Pontos.Count > 0) Rota.Remover(Rota.Pontos.Count - 1); });
            Botao("Limpar rota", Rota.Limpar);
            Botao("Fechar (N)", Fechar);
            barra.style.display = DisplayStyle.None;
            root.Add(barra);
        }

        Button Botao(string texto, Action acao)
        {
            var b = new Button(acao) { text = texto };
            b.style.fontSize = 15; b.style.height = 30; b.style.marginRight = 6;
            barra.Add(b);
            return b;
        }

        public void Alternar() { if (Aberto) Fechar(); else Abrir(); }

        void Abrir()
        {
            Aberto = true;
            f.Paused = true;
            Centralizar();
            Camada.style.display = DisplayStyle.Flex;
            barra.style.display = DisplayStyle.Flex;
        }

        public void Fechar()
        {
            Aberto = false;
            f.Paused = false;
            Camada.style.display = DisplayStyle.None;
            barra.style.display = DisplayStyle.None;
        }

        void Centralizar() { CentroLat = f.S.Lat * 180 / Math.PI; CentroLon = f.S.Lon * 180 / Math.PI; }

        void MudarZoom(int d)
        {
            int z = Mathf.Clamp(Zoom + d, MinZoom, MaxZoom);
            if (z != Zoom) { Zoom = z; LimparLadrilhos(); }
        }

        void LimparLadrilhos()
        {
            foreach (var v in ladrilhos.Values) v.RemoveFromHierarchy();
            ladrilhos.Clear();
        }

        double Mundo => Tile * Math.Pow(2, Zoom);
        double Px(double lon) => (lon + 180) / 360 * Mundo;
        double Py(double lat)
        {
            double s = Math.Sin(lat * Math.PI / 180);
            return (0.5 - Math.Log((1 + s) / (1 - s)) / (4 * Math.PI)) * Mundo;
        }

        public Vector2 Tela(double lat, double lon) => new Vector2((float)(Px(lon) - Px(CentroLon)) + W / 2, (float)(Py(lat) - Py(CentroLat)) + H / 2);

        public void Geo(Vector2 tela, out double lat, out double lon)
        {
            double x = Px(CentroLon) + tela.x - W / 2, y = Py(CentroLat) + tela.y - H / 2;
            lon = x / Mundo * 360 - 180;
            lat = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / Mundo))) * 180 / Math.PI;
        }

        public double MetrosPorPx(double lat) => 156543.03392 * Math.Cos(lat * Math.PI / 180) / Math.Pow(2, Zoom);

        void Apertou(PointerDownEvent e)
        {
            if (e.button == 1) { RemoverPerto(e.localPosition); return; }
            if (e.button != 0) return;
            arrastando = true; moveu = false; inicio = e.localPosition;
            Camada.CapturePointer(e.pointerId);
        }

        void Moveu(PointerMoveEvent e)
        {
            if (!arrastando) return;
            Vector2 p = e.localPosition, d = p - inicio;
            if (!moveu && d.sqrMagnitude < 25) return;
            moveu = true;
            inicio = p;
            Geo(new Vector2(W / 2 - d.x, H / 2 - d.y), out double lat, out double lon);
            CentroLat = lat; CentroLon = lon;
        }

        void Soltou(PointerUpEvent e)
        {
            if (!arrastando) return;
            arrastando = false;
            Camada.ReleasePointer(e.pointerId);
            if (!moveu) Adicionar(e.localPosition);
        }

        void Adicionar(Vector2 tela)
        {
            var sobreBase = PontoEm?.Invoke(tela);
            if (sobreBase != null) { Rota.Pontos.Add(sobreBase); return; }
            Geo(tela, out double lat, out double lon);
            Rota.Pontos.Add(new PontoRota { Lat = lat, Lon = lon });
        }

        void RemoverPerto(Vector2 tela)
        {
            for (int i = 0; i < Rota.Pontos.Count; i++)
                if ((Tela(Rota.Pontos[i].Lat, Rota.Pontos[i].Lon) - tela).sqrMagnitude < 196) { Rota.Remover(i); return; }
        }

        public void Atualizar(float w, float h)
        {
            W = w; H = h;
            LerControles();
            int n = 1 << Zoom;
            double cx = Px(CentroLon), cy = Py(CentroLat);
            int x0 = (int)Math.Floor((cx - W / 2) / Tile), x1 = (int)Math.Floor((cx + W / 2) / Tile);
            int y0 = Math.Max(0, (int)Math.Floor((cy - H / 2) / Tile)), y1 = Math.Min(n - 1, (int)Math.Floor((cy + H / 2) / Tile));
            visiveis.Clear();
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    int wx = ((x % n) + n) % n;
                    string chave = $"{fonte}/{Zoom}/{wx}/{y}";
                    visiveis.Add(chave);
                    if (!ladrilhos.TryGetValue(chave, out var el))
                    {
                        el = new VisualElement { pickingMode = PickingMode.Ignore };
                        el.style.position = Position.Absolute; el.style.width = Tile; el.style.height = Tile;
                        Camada.Add(el); ladrilhos[chave] = el;
                        Baixar(chave, Zoom, wx, y);
                    }
                    if (el.style.backgroundImage.keyword != StyleKeyword.Undefined || el.style.backgroundImage.value.texture == null)
                        if (cache.TryGetValue(chave, out var tex) && tex != null) el.style.backgroundImage = new StyleBackground(tex);
                    el.style.left = (float)(x * Tile - cx + W / 2);
                    el.style.top = (float)(y * Tile - cy + H / 2);
                }
            foreach (var kv in ladrilhos) kv.Value.style.display = visiveis.Contains(kv.Key) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void LerControles()
        {
            float dt = Time.unscaledDeltaTime;
            var kb = Keyboard.current; var pad = Gamepad.current;
            Vector2 mov = default;
            if (kb != null)
            {
                mov += new Vector2((kb.rightArrowKey.isPressed ? 1 : 0) - (kb.leftArrowKey.isPressed ? 1 : 0), (kb.downArrowKey.isPressed ? 1 : 0) - (kb.upArrowKey.isPressed ? 1 : 0));
                if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) MudarZoom(1);
                if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) MudarZoom(-1);
                if (kb.cKey.wasPressedThisFrame) Centralizar();
            }
            if (pad != null)
            {
                var st = pad.leftStick.ReadValue();
                mov += new Vector2(st.x, -st.y);
                if (pad.rightShoulder.wasPressedThisFrame) MudarZoom(1);
                if (pad.leftShoulder.wasPressedThisFrame) MudarZoom(-1);
                if (pad.buttonSouth.wasPressedThisFrame) Adicionar(new Vector2(W / 2, H / 2));
                if (pad.buttonEast.wasPressedThisFrame && Rota.Pontos.Count > 0) Rota.Remover(Rota.Pontos.Count - 1);
                if (pad.buttonNorth.wasPressedThisFrame) Centralizar();
            }
            if (mov == Vector2.zero) return;
            Geo(new Vector2(W / 2, H / 2) + mov * (500 * dt), out double lat, out double lon);
            CentroLat = lat; CentroLon = lon;
        }

        static void Baixar(string chave, int z, int x, int y)
        {
            if (cache.ContainsKey(chave) || !baixando.Add(chave)) return;
            var url = string.Format(Fontes[fonte].url, z, x, y);
            var req = UnityWebRequestTexture.GetTexture(url);
            req.SetRequestHeader("User-Agent", "FABSimulator/1.0");
            req.SendWebRequest().completed += _ =>
            {
                baixando.Remove(chave);
                if (req.result == UnityWebRequest.Result.Success) cache[chave] = DownloadHandlerTexture.GetContent(req);
                else Debug.LogWarning($"[Mapa] {url}: {req.error}");
                req.Dispose();
            };
        }
    }
}
