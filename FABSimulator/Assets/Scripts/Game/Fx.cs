using System.Collections.Generic;
using FABSim.Core;
using UnityEngine;

namespace FABSim.Game
{
    public sealed class Fx
    {
        sealed class Puff { public Transform T; public Renderer R; public V3 P, V; public float Age, Life, S0, S1; public Color C; public bool On; }

        readonly Combate c;
        readonly List<Puff> puffs = new List<Puff>();
        readonly List<Renderer> tracers = new List<Renderer>();
        readonly Dictionary<int, GameObject> stores = new Dictionary<int, GameObject>();
        readonly List<GameObject> storePool = new List<GameObject>();
        readonly HashSet<int> seen = new HashSet<int>();
        readonly MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        readonly Material add, smoke, metal, tracer, flareMat, wreck;
        readonly Mesh quad;
        float trailT;
        public float ShakeT, HurtT;
        const int MaxPuffs = 220;
        static readonly int UnlitColor = Shader.PropertyToID("_UnlitColor");

        public Fx(Combate c)
        {
            this.c = c;
            var soft = SoftTexture();
            add = Clone("FabChama", soft);
            smoke = Clone("FabFumaca", soft);
            tracer = Resources.Load<Material>("FabTracer");
            flareMat = add;
            metal = Resources.Load<Material>("FabMetal");
            wreck = Resources.Load<Material>("FabDestroco");
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(tmp);
        }

        static Material Clone(string name, Texture2D tex)
        {
            var m = new Material(Resources.Load<Material>(name));
            m.SetTexture("_UnlitColorMap", tex);
            return m;
        }

        static Texture2D SoftTexture()
        {
            const int N = 64;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2 - 1, dy = (y + 0.5f) / N * 2 - 1, r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1 - r); a = a * a * (3 - 2 * a);
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            t.Apply();
            return t;
        }

        Puff Get()
        {
            foreach (var p in puffs) if (!p.On) return p;
            if (puffs.Count >= MaxPuffs) { var old = puffs[0]; foreach (var p in puffs) if (p.Age / p.Life > old.Age / old.Life) old = p; return old; }
            var go = new GameObject("Puff");
            go.AddComponent<MeshFilter>().sharedMesh = quad;
            var r = go.AddComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var np = new Puff { T = go.transform, R = r };
            puffs.Add(np);
            return np;
        }

        void Emit(V3 pos, V3 vel, float life, float s0, float s1, Color col, Material mat)
        {
            var p = Get();
            p.P = pos; p.V = vel; p.Age = 0; p.Life = life; p.S0 = s0; p.S1 = s1; p.C = col; p.On = true;
            p.R.sharedMaterial = mat;
            p.R.enabled = true;
        }

        static float R(float a) => Random.Range(-a, a);

        public void Explosion(V3 p, double size)
        {
            float s = Mathf.Clamp((float)size, 4, 60);
            Emit(p, default, 0.25f, s * 1.5f, s * 5, new Color(4, 3, 2, 1), add);
            for (int i = 0; i < 12; i++) Emit(p, new V3(R(s), R(s), R(s)), Random.Range(0.5f, 1.1f), s * 0.8f, s * 2.6f, new Color(3, 1.4f, 0.35f, 1), add);
            for (int i = 0; i < 12; i++) Emit(p, new V3(R(s * 0.6f), R(s * 0.6f), Random.Range(1, s * 0.6f)), Random.Range(3, 7), s, s * 5, new Color(0.08f, 0.075f, 0.07f, 0.7f), smoke);
        }

        public void Sparks(V3 p) { for (int i = 0; i < 4; i++) Emit(p, new V3(R(25), R(25), R(25)), 0.25f, 0.6f, 1.6f, new Color(4, 2.5f, 1, 1), add); }

        public void Dust(V3 p) { for (int i = 0; i < 3; i++) Emit(p, new V3(R(3), R(3), Random.Range(1, 6)), 1.4f, 2, 8, new Color(0.45f, 0.38f, 0.28f, 0.5f), smoke); }

        public void Tick(float dt)
        {
            ShakeT = Mathf.Max(0, ShakeT - dt);
            HurtT = Mathf.Max(0, HurtT - dt);
            var cam = c.F.cam.transform;
            foreach (var p in puffs)
            {
                if (!p.On) continue;
                p.Age += dt;
                if (p.Age >= p.Life) { p.On = false; p.R.enabled = false; continue; }
                p.P += p.V * dt;
                p.V *= 1 - 0.6 * dt;
                float k = p.Age / p.Life;
                var w = c.World(p.P);
                p.T.position = w;
                p.T.rotation = Quaternion.LookRotation(w - cam.position, cam.up);
                p.T.localScale = Vector3.one * Mathf.Lerp(p.S0, p.S1, Mathf.Sqrt(k));
                var col = p.C; col.a *= 1 - k * k;
                mpb.SetColor(UnlitColor, col);
                p.R.SetPropertyBlock(mpb);
            }
        }

        public void Sync(CombatSession s)
        {
            int n = 0;
            var pl = s.Pl.Pos;
            foreach (var b in s.Bullets)
            {
                if (!(b.Tracer || b.Team == Team.Blue)) continue;
                if (n >= 40) break;
                if (n >= tracers.Count)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(go.GetComponent<Collider>());
                    var r = go.GetComponent<Renderer>();
                    r.sharedMaterial = tracer;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    tracers.Add(r);
                }
                var t = tracers[n++];
                t.enabled = true;
                var dir = Combate.Dir(b.V);
                double dist = Cm.Dist(b.P, pl);
                float w = Mathf.Clamp((float)dist / 700, 0.1f, 1.2f);
                t.transform.SetPositionAndRotation(c.World(b.P), Quaternion.LookRotation(dir));
                t.transform.localScale = new Vector3(w, w, Mathf.Clamp(dir.magnitude * 0.018f, 3, 22));
            }
            for (int i = n; i < tracers.Count; i++) tracers[i].enabled = false;

            seen.Clear();
            bool trail = (trailT -= Time.deltaTime) <= 0;
            if (trail) trailT = 0.04f;
            foreach (var m in s.Missiles)
            {
                seen.Add(m.Id);
                var go = Store(m.Id, 0);
                go.transform.SetPositionAndRotation(c.World(m.Pos), Quaternion.LookRotation(Combate.Dir(m.Dir)));
                if (trail)
                {
                    Emit(m.Pos, default, 2.8f, 1.2f, 7, new Color(0.75f, 0.75f, 0.75f, 0.5f), smoke);
                    Emit(m.Pos, default, 0.12f, 1.6f, 1.0f, new Color(5, 3, 1.3f, 1), add);
                }
            }
            foreach (var r in s.Rockets)
            {
                seen.Add(r.Id);
                var go = Store(r.Id, 1);
                go.transform.SetPositionAndRotation(c.World(r.Pos), Quaternion.LookRotation(Combate.Dir(r.Dir)));
                if (trail) { Emit(r.Pos, default, 1.1f, 0.5f, 2.5f, new Color(0.7f, 0.7f, 0.7f, 0.4f), smoke); Emit(r.Pos, default, 0.08f, 0.9f, 0.5f, new Color(5, 3, 1.3f, 1), add); }
            }
            foreach (var b in s.Bombs)
            {
                seen.Add(b.Id);
                var go = Store(b.Id, 2);
                var v = Combate.Dir(b.Vel);
                go.transform.SetPositionAndRotation(c.World(b.Pos), v.sqrMagnitude > 1 ? Quaternion.LookRotation(v) : Quaternion.identity);
            }
            if (trail) foreach (var f in s.Flares) { Emit(f.Pos, default, 0.35f, 5, 1.5f, new Color(6, 5, 3.5f, 1), flareMat); Emit(f.Pos, default, 1.8f, 1.4f, 5, new Color(0.8f, 0.8f, 0.8f, 0.28f), smoke); }

            if (stores.Count > seen.Count)
            {
                var gone = new List<int>();
                foreach (var kv in stores) if (!seen.Contains(kv.Key)) gone.Add(kv.Key);
                foreach (int id in gone) { Object.Destroy(stores[id]); stores.Remove(id); }
            }
        }

        GameObject Store(int id, int kind)
        {
            if (stores.TryGetValue(id, out var go)) return go;
            go = new GameObject("Carga");
            float len = kind == 0 ? 3f : kind == 1 ? 1.4f : 2.2f, rad = kind == 0 ? 0.08f : kind == 1 ? 0.04f : 0.14f;
            Part(go, PrimitiveType.Cylinder, Vector3.zero, new Vector3(rad * 2, len / 2, rad * 2), Quaternion.Euler(90, 0, 0));
            Part(go, PrimitiveType.Sphere, new Vector3(0, 0, len / 2), new Vector3(rad * 2, rad * 2, rad * 5), Quaternion.identity);
            for (int i = 0; i < 4; i++)
            {
                var q = Quaternion.Euler(0, 0, i * 90 + 45);
                Part(go, PrimitiveType.Cube, q * new Vector3(rad * 2.2f, 0, 0) + new Vector3(0, 0, -len * 0.42f), new Vector3(rad * 3, 0.02f, len * 0.16f), q);
            }
            stores[id] = go;
            return go;
        }

        GameObject Part(GameObject parent, PrimitiveType t, Vector3 pos, Vector3 scale, Quaternion rot)
        {
            var p = GameObject.CreatePrimitive(t);
            Object.Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(parent.transform, false);
            p.transform.localPosition = pos; p.transform.localScale = scale; p.transform.localRotation = rot;
            p.GetComponent<Renderer>().sharedMaterial = metal;
            return p;
        }

        public GameObject MakeGround(GroundUnit g)
        {
            var go = new GameObject("Alvo " + g.Kind);
            Vector3 size;
            switch (g.Kind)
            {
                case "hangar": size = new Vector3(40, 12, 30); break;
                case "fuel": size = new Vector3(16, 9, 16); break;
                case "radar": size = new Vector3(8, 6, 8); break;
                case "sam": size = new Vector3(7, 3, 9); break;
                case "aaa": size = new Vector3(4, 2.2f, 5); break;
                case "tank": size = new Vector3(3.6f, 2.4f, 7); break;
                default: size = new Vector3(2.5f, 3, 7); break;
            }
            Part(go, g.Kind == "fuel" ? PrimitiveType.Cylinder : PrimitiveType.Cube, new Vector3(0, size.y / 2, 0), g.Kind == "fuel" ? new Vector3(size.x, size.y / 2, size.z) : size, Quaternion.identity);
            if (g.Kind == "radar") Part(go, PrimitiveType.Sphere, new Vector3(0, size.y + 3, 0), Vector3.one * 7, Quaternion.identity);
            if (g.Kind == "sam" || g.Kind == "aaa") Part(go, PrimitiveType.Cylinder, new Vector3(0, size.y + 1, 1), new Vector3(0.6f, 2, 0.6f), Quaternion.Euler(50, 0, 0));
            if (g.Kind == "tank") Part(go, PrimitiveType.Cube, new Vector3(0, size.y + 0.5f, 0), new Vector3(2.4f, 1, 3), Quaternion.identity);
            return go;
        }

        public void SetWrecked(GameObject go, bool wrecked)
        {
            if (!wrecked || wreck == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r.sharedMaterial == wreck) return;
                r.sharedMaterial = wreck;
            }
            go.transform.localScale = new Vector3(1, 0.35f, 1);
        }
    }
}
