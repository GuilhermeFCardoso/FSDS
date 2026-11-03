using System;
using System.Collections.Generic;
using FABSim.Core;
using UnityEngine;

namespace FABSim.Game
{
    // Os modelos nao trazem rig: cada peca movel ganha um pivo, a partir de um no do modelo
    // ou de uma ilha separada da malha. Caixas em coordenadas do arquivo; o Unity inverte o X ao importar.
    public sealed class TelaParte { public Renderer R; public Mesh Malha; public Vector2[] Uv; }
    public sealed class TelaDef { public readonly List<TelaParte> Partes = new List<TelaParte>(); public float Aspect; public int Pagina; public bool Cicla; public Vector3 Centro; }

    public sealed class AircraftRig
    {
        sealed class Part
        {
            public Transform Pivot;
            public Vector3 Axis, Home, Slide;
            public Func<FlightState, float> Value;
            public float Sign = 1, Angle;
            public bool Spin;
        }

        static readonly Vector3 Down = Vector3.down, Back = Vector3.back, Right = Vector3.right, Fwd = Vector3.forward;

        readonly List<Part> parts = new List<Part>();
        public readonly List<TelaDef> Telas = new List<TelaDef>();
        // Centro das telas ao alcance do piloto: no KC-390 ignora as do copiloto.
        public Vector3 PainelPara(Vector3 olho)
        {
            var c = Vector3.zero;
            int n = 0;
            foreach (var t in Telas) if (Mathf.Abs(t.Centro.x - olho.x) < 0.6f) { c += t.Centro; n++; }
            return n > 0 ? c / n : olho + new Vector3(0, -0.45f, 0.8f);
        }
        readonly HashSet<Mesh> owned = new HashSet<Mesh>();
        readonly Transform root;
        readonly AircraftDef ac;
        readonly Transform prefab;
        Transform scope;
        GameObject gearGraft;
        bool prefabBoxes;

        AircraftRig(FittedModel fit, AircraftDef ac) { root = fit.Root.transform; prefab = root.GetChild(0).GetChild(0); scope = root; this.ac = ac; }

        public static AircraftRig Build(FittedModel fit, string key, AircraftDef ac)
        {
            var r = new AircraftRig(fit, ac);
            if (key == "A29") r.A29();
            else if (key == "KC390") r.KC390();
            else if (key == "F39E") r.F39E();
            else if (key == "EUA_MH47") r.Chinook();
            else if (key == "EUA_MH60") r.Seahawk();
            return r;
        }

        public void Apply(FlightState s, float dt)
        {
            foreach (var p in parts)
            {
                float v = p.Value(s);
                if (p.Slide != Vector3.zero) p.Pivot.localPosition = p.Home + p.Slide * v;
                if (p.Axis == Vector3.zero) continue;
                p.Angle = p.Spin ? Mathf.Repeat(p.Angle + v * dt, 360) : v;
                p.Pivot.localRotation = Quaternion.AngleAxis(p.Sign * p.Angle, p.Axis);
            }
            if (gearGraft != null) gearGraft.SetActive(s.Gear > 0.02);
        }

        static float Up(FlightState s) => (float)(1 - s.Gear);
        static float Elev(FlightState s) => -(float)s.De * 25;
        static float Rud(FlightState s) => (float)s.Dr * 25;
        float FlapDeg(FlightState s) => (float)(s.Flaps / ac.FlapMax) * 35;

        void A29()
        {
            foreach (var n in new[] { "aileron_l", "aileron_r" }) Surface(Nodes(n), 'x', (s, side) => -side * (float)s.Da * 22, Down);
            foreach (var n in new[] { "elevator_l", "elevator_r" }) Surface(Nodes(n), 'x', (s, _) => Elev(s), Down);
            Surface(Nodes("rudder"), 'y', (s, _) => Rud(s), Right);
            Swing(Nodes("airbrake_l"), new Vector3(0.5f, 0.5f, 1), Right, Back, _ => Down, s => (float)s.Sb * 55);
            Swing(Nodes("gear_f", "gear_f.001", "wheel_lf"), new Vector3(0.5f, 1, 0.5f), Right, Down, _ => Back, s => Up(s) * 88);
            Swing(Nodes("gear_rl", "wheel_lr"), new Vector3(0.5f, 1, 0.5f), Fwd, Down, side => Right * -side, s => Up(s) * 88);
            Swing(Nodes("gear_rr", "wheel_rr"), new Vector3(0.5f, 1, 0.5f), Fwd, Down, side => Right * -side, s => Up(s) * 88);
            Spin(Nodes("prop_1_slow"), Fwd, s => (float)s.N * 2400);
            Tela(Nodes("dials"), CockpitDisplays.Reserva, false);
            TelaQuad(new Vector3(-0.184f, 0.741f, 2.38f), 0.143f, 0.155f, CockpitDisplays.Pfd, false);
            TelaQuad(new Vector3(0.002f, 0.782f, 2.38f), 0.096f, 0.086f, CockpitDisplays.Eng, false);
            TelaQuad(new Vector3(0.181f, 0.741f, 2.38f), 0.167f, 0.155f, CockpitDisplays.Nav, true);
        }

        void KC390()
        {
            prefabBoxes = true;
            foreach (var sx in new[] { 1, -1 })
            {
                string wing = sx > 0 ? "KC390.016" : "KC390.017";
                Surface(Island("KC390.015", sx, 0.3f, 6.2f, 9.9f, 10.2f, 19.8f, 22.6f), 'x', (s, _) => Elev(s), Down);
                Surface(Island(wing, sx, 13f, 16.9f, 2.7f, 3.4f, 6.6f, 8.4f), 'x', (s, side) => -side * (float)s.Da * 20, Down);
                Surface(Island(wing, sx, 2.1f, 8.1f, 3.1f, 4.35f, 3.1f, 6.1f), 'x', (s, _) => FlapDeg(s), Down);
                Surface(Island(wing, sx, 7.5f, 13.1f, 3f, 3.9f, 4.8f, 7.5f), 'x', (s, _) => FlapDeg(s), Down);
                Spin(Island("KC390.003", sx, 4.4f, 5.9f, 1.8f, 3.3f, -2.5f, -1.7f), Fwd, s => (float)s.N * 1800);
                // Dobradica no alto da carenagem, na ponta interna da chapa curva; as tampas laterais giram junto.
                var door = Island("KC390.019", sx, 3.2f, 4.25f, -0.75f, 0.9f, 2.4f, 5.85f);
                if (door.Count == 0) continue;
                var bd = BoundsOf(Points(door));
                door.AddRange(Island("KC390.013", sx, 2.9f, 4.15f, -0.65f, 0.8f, 2.45f, 5.8f));
                float ladoPorta = Mathf.Sign(bd.center.x);
                Add(door, new Vector3(ladoPorta > 0 ? bd.min.x : bd.max.x, bd.max.y, bd.center.z), Fwd, Down, Right * -ladoPorta, s => Up(s) * 91);
            }
            Surface(Island("KC390.014", 1, -0.3f, 0.3f, 4.6f, 9.9f, 16.9f, 22.4f), 'y', (s, _) => Rud(s), Right);
            Slide(Nodes("KC3902", "KC3902.001", "KC3902.002"), Vector3.up * 1.6f, Up);
            Slide(Nodes("KC3903", "KC3902.003", "KC3902.004"), Vector3.up * 1.6f, Up);
            // A perna dianteira gira no munhao, numa malha separada das rodas, e entra no alojamento a frente.
            var munhao = Island("KC390.001", 1, -0.62f, 0.63f, 0.92f, 1.17f, -6.98f, -6.73f);
            if (munhao.Count > 0)
            {
                var pivoNariz = BoundsOf(Points(munhao)).center;
                munhao.AddRange(Island("KC390.001", 1, -0.62f, 0.63f, 0.05f, 1.32f, -7.62f, -6.33f));
                munhao.AddRange(Nodes("KC3901"));
                Add(munhao, pivoNariz, Right, Down, Fwd, s => Up(s) * 100);
            }
            foreach (var sx in new[] { 1, -1 })
            {
                var porta = Island("KC390", sx, 0.55f, 0.94f, -0.03f, 0.59f, -7.48f, -6.22f);
                porta.AddRange(Island("KC390.013", sx, 0.55f, 0.94f, -0.03f, 0.59f, -7.48f, -6.22f));
                Swing(porta, new Vector3(-1, 1, 0.5f), Fwd, Down, side => Right * -side, s => Up(s) * 118);
            }
            Tela(Island("KC390.010", 1, 0.39f, 0.73f, 2.61f, 2.81f, -7.92f, -7.86f), CockpitDisplays.Pfd, false);
            Tela(Island("KC390.010", 1, 0.05f, 0.39f, 2.61f, 2.81f, -7.92f, -7.86f), CockpitDisplays.Nav, false);
            // Esta tela vem partida: cada triangulo do quadrado esta numa malha.
            var motorKc = Island("KC390.010", 1, -0.38f, -0.04f, 2.61f, 2.81f, -7.92f, -7.86f);
            motorKc.AddRange(Island("KC390.007", 1, -0.38f, -0.04f, 2.62f, 2.81f, -7.91f, -7.86f));
            Tela(motorKc, CockpitDisplays.Eng, false);
            Tela(Island("KC390.010", 1, -0.72f, -0.38f, 2.61f, 2.81f, -7.92f, -7.86f), CockpitDisplays.Pfd, false);
            Tela(Island("KC390.010", 1, -0.21f, 0.13f, 2.46f, 2.61f, -7.86f, -7.72f), CockpitDisplays.Sms, true);
        }

        void Chinook()
        {
            Spin(Nodes("Object_4", "Object_7"), Vector3.up, _ => 1100);
            Spin(Nodes("Object_5", "Object_6"), Vector3.up, _ => -1100);
        }

        void Seahawk()
        {
            var pas = Island("Object_16", 1, 34, 552, -180, 588, 399, 434);
            pas.AddRange(Island("Object_20", 1, -549, -18, -1102, -321, 400, 443));
            pas.AddRange(Island("Object_21", 1, 67, 848, -807, -276, 400, 443));
            pas.AddRange(Island("Object_22", 1, -843, -76, -224, 294, 400, 431));
            Spin(pas, Vector3.up, _ => 1100);
            var cauda = Island("Object_17", 1, 30, 40, 690, 925, 240, 650);
            cauda.AddRange(Island("Object_18", 1, 30, 40, 880, 1100, 340, 460));
            Spin(cauda, Right, _ => 2400);
        }

        void F39E()
        {
            foreach (var sy in new[] { 1, -1 })
            {
                float lo = sy > 0 ? 0.85f : -2.62f, hi = sy > 0 ? 2.62f : -0.85f;
                Func<FlightState, float, float> elevon = (s, side) => Elev(s) - side * (float)s.Da * 20;
                Surface(Island("Object_52", 1, -3.45f, -2.3f, lo, hi, -0.36f, 0), 'x', elevon, Down);
                float olo = sy > 0 ? 2.58f : -4.17f, ohi = sy > 0 ? 4.17f : -2.58f;
                Surface(Island(sy > 0 ? "Object_52" : "Object_43", 1, -3.1f, -2.5f, olo, ohi, -0.24f, -0.13f), 'x', elevon, Down);
                float clo = sy > 0 ? 0.88f : -2.33f, chi = sy > 0 ? 2.33f : -0.88f;
                Swing(Island("Object_43", 1, 1.6f, 4.3f, clo, chi, 0.06f, 0.35f), new Vector3(0.5f, 0.5f, 0.5f), Right, Back, _ => Down, s => (float)s.De * 14 - (float)s.Sb * 50);
            }
            Surface(Island("Object_59", 1, -5f, -4.1f, -0.1f, 0.1f, 0.6f, 2f), 'y', (s, _) => Rud(s), Right);
            // Trilhos soltos que vieram no arquivo do modelo, longe do aviao.
            foreach (var t in Island("Object_70", 1, -5.3f, 12.3f, 0.5f, 0.85f, -4.4f, -4.25f)) t.gameObject.SetActive(false);
            foreach (var y in new[] { (lo: -0.33f, hi: -0.12f), (lo: 0.12f, hi: 0.33f) })
            {
                var g = Island("Object_16", 1, 5.5f, 5.57f, y.lo, y.hi, 0.13f, 0.285f);
                if (g.Count == 0) continue;
                bool direita = BoundsOf(Points(g)).center.x > 0;
                Tela(g, direita ? CockpitDisplays.Eng : CockpitDisplays.Nav, direita);
            }
            Tela(Island("Object_16", 1, 5.49f, 5.57f, -0.09f, 0.08f, 0.03f, 0.24f), CockpitDisplays.Pfd, false);
            GraftGear();
        }

        // O F-39 do jogador nao tem trem modelado: usa o do Gripen da ala, recomendado para esse fim.
        void GraftGear()
        {
            var gear = ModelFit.Spawn("F39E_ALA", root);
            if (gear == null) return;
            gearGraft = gear.Root;
            gear.Root.transform.localScale = Vector3.one * 1.1f;
            var rs = gear.Root.GetComponentsInChildren<Renderer>(true);
            Renderer struts = null;
            float noseGear = float.MinValue;
            foreach (var r in rs)
            {
                if (r.name == "Object_4") noseGear = RootBounds(r).max.z;
                r.enabled = r.name == "Object_14";
                if (r.enabled) struts = r;
            }
            float noseBody = float.MinValue;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (r.name == "Object_51" && !r.transform.IsChildOf(gear.Root.transform)) noseBody = RootBounds(r).max.z;
            var pos = gear.Root.transform.localPosition;
            pos.z += noseBody - noseGear;
            pos.y += -(float)ac.CgHeight - RootBounds(struts).min.y;
            gear.Root.transform.localPosition = pos;
            scope = gear.Root.transform;
            Swing(Island("Object_14", 1, 60f, 68f, -2.6f, 2.6f, 4.5f, 20f), new Vector3(0.5f, 1, 0.5f), Right, Down, _ => Back, s => Up(s) * 88);
            Swing(Island("Object_14", 1, 6.5f, 15f, 9.6f, 16f, 3.2f, 21f), new Vector3(0.5f, 1, 0.5f), Fwd, Down, side => Right * -side, s => Up(s) * 88);
            Swing(Island("Object_14", 1, 6.5f, 15f, -16f, -9.6f, 3.2f, 21f), new Vector3(0.5f, 1, 0.5f), Fwd, Down, side => Right * -side, s => Up(s) * 88);
            scope = root;
        }

        // A propria ilha da malha vira a tela, com UV plano no espaco do aviao.
        void Tela(List<Transform> group, int pagina, bool cicla)
        {
            if (group.Count == 0) return;
            var b = BoundsOf(Points(group));
            var tela = new TelaDef { Aspect = b.size.x / b.size.y, Pagina = pagina, Cicla = cicla, Centro = b.center };
            foreach (var t in group)
            {
                var mesh = t.GetComponent<MeshFilter>().sharedMesh;
                var m = root.worldToLocalMatrix * t.localToWorldMatrix;
                var v = mesh.vertices;
                var uv = new Vector2[v.Length];
                for (int i = 0; i < v.Length; i++)
                {
                    var q = m.MultiplyPoint3x4(v[i]);
                    uv[i] = new Vector2(Mathf.Clamp01((q.x - b.min.x) / b.size.x), Mathf.Clamp01((q.y - b.min.y) / b.size.y));
                }
                tela.Partes.Add(new TelaParte { R = t.GetComponent<Renderer>(), Malha = mesh, Uv = uv });
            }
            Telas.Add(tela);
        }

        // Para paineis que sao so uma foto no modelo: um quad sobre a tela, voltado para o piloto.
        void TelaQuad(Vector3 centro, float largura, float altura, int pagina, bool cicla)
        {
            var go = new GameObject("Tela");
            go.transform.SetParent(root, false);
            go.transform.localPosition = centro;
            var mesh = new Mesh { name = "Tela" };
            float hw = largura / 2, hh = altura / 2;
            mesh.vertices = new[] { new Vector3(-hw, -hh), new Vector3(-hw, hh), new Vector3(hw, hh), new Vector3(hw, -hh) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Resources.Load<Material>("FabTela");
            r.enabled = false;
            var tela = new TelaDef { Aspect = largura / altura, Pagina = pagina, Cicla = cicla, Centro = centro };
            tela.Partes.Add(new TelaParte { R = r, Malha = mesh, Uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) } });
            Telas.Add(tela);
        }

        Bounds RootBounds(Renderer r)
        {
            var mesh = r.GetComponent<MeshFilter>().sharedMesh;
            var m = root.worldToLocalMatrix * r.transform.localToWorldMatrix;
            var b = new Bounds(m.MultiplyPoint3x4(mesh.bounds.center), Vector3.zero);
            var c = mesh.bounds.center; var e = mesh.bounds.extents;
            for (int i = 0; i < 8; i++)
                b.Encapsulate(m.MultiplyPoint3x4(c + Vector3.Scale(e, new Vector3((i & 1) * 2 - 1, (i >> 1 & 1) * 2 - 1, (i >> 2 & 1) * 2 - 1))));
            return b;
        }

        List<Transform> Nodes(params string[] names)
        {
            var list = new List<Transform>();
            foreach (var n in names)
            {
                var mf = Find(n);
                if (mf != null) list.Add(mf.transform);
            }
            return list;
        }

        MeshFilter Find(string node)
        {
            foreach (var mf in scope.GetComponentsInChildren<MeshFilter>(true)) if (mf.name == node) return mf;
            Debug.LogError("[AircraftRig] No ausente: " + node);
            return null;
        }

        // Ilhas de malha totalmente dentro da caixa viram uma malha propria. mirrorX espelha a caixa em X.
        List<Transform> Island(string node, int mirrorX, float x0, float x1, float y0, float y1, float z0, float z1)
        {
            var list = new List<Transform>();
            var mf = Find(node);
            if (mf == null) return list;
            if (mirrorX < 0) { float t = x0; x0 = -x1; x1 = -t; }
            var box = new Bounds();
            box.SetMinMax(new Vector3(-x1 - 0.02f, y0 - 0.02f, z0 - 0.02f), new Vector3(-x0 + 0.02f, y1 + 0.02f, z1 + 0.02f));
            if (!owned.Contains(mf.sharedMesh)) { mf.sharedMesh = UnityEngine.Object.Instantiate(mf.sharedMesh); owned.Add(mf.sharedMesh); }
            var src = mf.sharedMesh;
            // Nos FBX a caixa vale no espaco do modelo; nos glTF, no espaco da malha.
            var toBox = scope == root && prefabBoxes ? prefab.worldToLocalMatrix * mf.transform.localToWorldMatrix : Matrix4x4.identity;
            var inside = IslandsInside(src, box, toBox);
            var part = UnityEngine.Object.Instantiate(src);
            int moved = 0;
            for (int sub = 0; sub < src.subMeshCount; sub++)
            {
                var tris = src.GetTriangles(sub);
                var keep = new List<int>(tris.Length); var take = new List<int>();
                for (int i = 0; i < tris.Length; i += 3)
                {
                    var dest = inside[tris[i]] ? take : keep;
                    dest.Add(tris[i]); dest.Add(tris[i + 1]); dest.Add(tris[i + 2]);
                }
                src.SetTriangles(keep, sub); part.SetTriangles(take, sub);
                moved += take.Count / 3;
            }
            if (moved == 0) { Debug.LogError($"[AircraftRig] Caixa vazia em {node}: {box.min} {box.max}"); return list; }
            var go = new GameObject(node + "_peca");
            go.transform.SetParent(mf.transform.parent, false);
            go.transform.localPosition = mf.transform.localPosition;
            go.transform.localRotation = mf.transform.localRotation;
            go.transform.localScale = mf.transform.localScale;
            go.AddComponent<MeshFilter>().sharedMesh = part;
            var from = mf.GetComponent<Renderer>();
            var to = go.AddComponent<MeshRenderer>();
            to.sharedMaterials = from.sharedMaterials;
            to.shadowCastingMode = from.shadowCastingMode;
            to.enabled = from.enabled;
            list.Add(go.transform);
            return list;
        }

        static bool[] IslandsInside(Mesh mesh, Bounds box, Matrix4x4 toBox)
        {
            var v = mesh.vertices;
            var parent = new int[v.Length];
            var weld = new Dictionary<Vector3Int, int>(v.Length);
            var rep = new int[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                parent[i] = i;
                var k = Vector3Int.RoundToInt(toBox.MultiplyPoint3x4(v[i]) * 2000f);
                if (!weld.TryGetValue(k, out rep[i])) { weld[k] = i; rep[i] = i; }
            }
            int Root(int a) { while (parent[a] != a) a = parent[a] = parent[parent[a]]; return a; }
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                var t = mesh.GetTriangles(sub);
                for (int i = 0; i < t.Length; i += 3)
                {
                    int a = Root(rep[t[i]]);
                    parent[Root(rep[t[i + 1]])] = a;
                    parent[Root(rep[t[i + 2]])] = a;
                }
            }
            var outside = new HashSet<int>();
            for (int i = 0; i < v.Length; i++) if (!box.Contains(toBox.MultiplyPoint3x4(v[i]))) outside.Add(Root(rep[i]));
            var result = new bool[v.Length];
            for (int i = 0; i < v.Length; i++) result[i] = !outside.Contains(Root(rep[i]));
            return result;
        }

        List<Vector3> Points(List<Transform> group)
        {
            var pts = new List<Vector3>();
            foreach (var t in group)
            {
                var mesh = t.GetComponent<MeshFilter>().sharedMesh;
                var m = root.worldToLocalMatrix * t.localToWorldMatrix;
                var v = mesh.vertices;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    foreach (int i in mesh.GetTriangles(sub)) pts.Add(m.MultiplyPoint3x4(v[i]));
            }
            return pts;
        }

        static Bounds BoundsOf(List<Vector3> pts)
        {
            var b = new Bounds(pts[0], Vector3.zero);
            foreach (var p in pts) b.Encapsulate(p);
            return b;
        }

        // Dobradica na borda de ataque da peca, seguindo o enflechamento.
        void Surface(List<Transform> group, char span, Func<FlightState, float, float> deg, Vector3 move)
        {
            if (group.Count == 0) return;
            var pts = Points(group);
            var b = BoundsOf(pts);
            int ax = span == 'x' ? 0 : 1;
            float band = (b.max[ax] - b.min[ax]) * 0.08f;
            Vector3 p0 = default, p1 = default;
            float f0 = float.MinValue, f1 = float.MinValue;
            foreach (var p in pts)
            {
                if (p[ax] <= b.min[ax] + band && p.z > f0) { f0 = p.z; p0 = p; }
                if (p[ax] >= b.max[ax] - band && p.z > f1) { f1 = p.z; p1 = p; }
            }
            float side = Mathf.Sign(b.center.x);
            Add(group, p0, (p1 - p0).normalized, Back, move, s => deg(s, side));
        }

        // pivotFrac e a posicao do pivo na caixa da peca; x = -1 escolhe o lado voltado para a fuselagem.
        void Swing(List<Transform> group, Vector3 pivotFrac, Vector3 axis, Vector3 lever, Func<float, Vector3> move, Func<FlightState, float> deg)
        {
            if (group.Count == 0) return;
            var b = BoundsOf(Points(group));
            float side = Mathf.Sign(b.center.x);
            if (pivotFrac.x < 0) pivotFrac.x = side > 0 ? 0 : 1;
            Add(group, b.min + Vector3.Scale(b.size, pivotFrac), axis, lever, move(side), deg);
        }

        void Add(List<Transform> group, Vector3 pivot, Vector3 axis, Vector3 lever, Vector3 move, Func<FlightState, float> value)
        {
            var part = Attach(group, pivot);
            part.Axis = axis;
            part.Sign = Mathf.Sign(Vector3.Dot(Vector3.Cross(axis, lever), move));
            part.Value = value;
        }

        void Spin(List<Transform> group, Vector3 axis, Func<FlightState, float> degPerSecond)
        {
            if (group.Count == 0) return;
            var part = Attach(group, BoundsOf(Points(group)).center);
            part.Axis = axis; part.Spin = true; part.Value = degPerSecond;
        }

        void Slide(List<Transform> group, Vector3 offset, Func<FlightState, float> value)
        {
            if (group.Count == 0) return;
            var part = Attach(group, BoundsOf(Points(group)).center);
            part.Slide = offset; part.Value = value;
        }

        Part Attach(List<Transform> group, Vector3 pivot)
        {
            var go = new GameObject("Pivo_" + group[0].name).transform;
            go.SetParent(scope, false);
            go.position = root.TransformPoint(pivot);
            go.rotation = root.rotation;
            foreach (var t in group) t.SetParent(go, true);
            // Eixos e deslocamentos sao definidos no espaco do aviao, mesmo dentro do trem enxertado.
            var part = new Part { Pivot = go, Home = go.localPosition };
            parts.Add(part);
            return part;
        }
    }
}
