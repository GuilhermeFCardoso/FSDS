using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public static class FabInspect
{
    static string[] ModelPaths() => AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Art/Models" }).Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(p => p).ToArray();

    static Bounds Measure(GameObject go)
    {
        var b = new Bounds();
        bool first = true;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
        return b;
    }

    public static void Models()
    {
        var paths = ModelPaths();
        Debug.Log($"[FabInspect] {paths.Length} modelos");
        foreach (var path in paths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) continue;
            var go = Object.Instantiate(asset);
            var rs = go.GetComponentsInChildren<Renderer>(true);
            var b = Measure(go);
            var mats = rs.SelectMany(r => r.sharedMaterials).Where(x => x != null).Distinct().ToArray();
            string shaders = string.Join(",", mats.Select(x => x.shader.name).Distinct().Take(3));
            string names = string.Join(" ", go.GetComponentsInChildren<Transform>(true).Select(t => t.name).Where(n => Regex.IsMatch(n, "cockpit|canopy|gear|flap|aileron|rudder|elevator|prop|seat|stick|panel|mfd|hud", RegexOptions.IgnoreCase)).Distinct().Take(14));
            Debug.Log($"[FabInspect] {path} | tam {b.size.x:0.0}x{b.size.y:0.0}x{b.size.z:0.0} centro {b.center.x:0.0},{b.center.y:0.0},{b.center.z:0.0} | render {rs.Length} | mat {mats.Length} | {shaders} | partes: {names}");
            Object.DestroyImmediate(go);
        }
    }

    public static void RigPreview()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var sun = new GameObject("Sol").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.gameObject.AddComponent<HDAdditionalLightData>();
        sun.lightUnit = LightUnit.Lux;
        sun.intensity = 60000f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var env = profile.Add<VisualEnvironment>(true);
        env.skyType.Override((int)SkyType.Gradient);
        env.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
        var sky = profile.Add<GradientSky>(true);
        sky.top.Override(new Color(0.4f, 0.6f, 1f));
        sky.middle.Override(new Color(0.7f, 0.8f, 0.95f));
        sky.bottom.Override(new Color(0.45f, 0.4f, 0.3f));
        sky.multiplier.Override(14000f);
        var exp = profile.Add<Exposure>(true);
        exp.mode.Override(ExposureMode.Fixed);
        exp.fixedExposure.Override(13f);
        var vol = new GameObject("Vol").AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = profile;
        var cam = new GameObject("Cam").AddComponent<Camera>();
        var hd = cam.gameObject.AddComponent<HDAdditionalCameraData>();
        hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        hd.backgroundColorHDR = new Color(0.45f, 0.65f, 0.95f) * 2.5f;
        const int W = 640, H = 400;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        var keys = new[] { ("A29", FABSim.Core.AircraftDb.A29()), ("KC390", FABSim.Core.AircraftDb.KC390()), ("F39E", FABSim.Core.AircraftDb.F39E()) };
        Directory.CreateDirectory(Path.GetFullPath("Logs/preview"));
        for (int row = 0; row < keys.Length; row++)
        {
            var (key, ac) = keys[row];
            var img = new Texture2D(W * 3, H * 2, TextureFormat.RGB24, false);
            ac.Prepare();
            var fit = FABSim.Game.ModelFit.Spawn(key, null, AssetDatabase.LoadAssetAtPath<GameObject>(FABSim.Game.ModelFit.Table[key].Path));
            var rig = FABSim.Game.AircraftRig.Build(fit, key, ac);
            var neutral = new FABSim.Core.FlightState { Gear = 1 };
            var full = new FABSim.Core.FlightState { Gear = 1, De = 1, Da = 1, Dr = 1, Flaps = ac.FlapMax, Sb = 1, N = 0.5 };
            var up = new FABSim.Core.FlightState { Gear = 0 };
            float radius = (float)ac.B * (key == "F39E" ? 0.8f : 0.42f);
            var shots = new (FABSim.Core.FlightState st, Vector3 dir, bool eye)[]
            {
                (neutral, new Vector3(1, 0.08f, 0), false),
                (neutral, new Vector3(-0.6f, 0.55f, -0.8f), false),
                (full, new Vector3(-0.6f, 0.55f, -0.8f), false),
                (up, new Vector3(0.6f, -0.45f, 0.6f), false),
                (neutral, Vector3.forward, true),
                (up, new Vector3(0.45f, -0.88f, -0.15f), false),
            };
            for (int c = 0; c < shots.Length; c++)
            {
                var shot = shots[c];
                rig.Apply(shot.st, 0.1f);
                var dd = shot.dir.normalized;
                cam.orthographic = !shot.eye;
                cam.nearClipPlane = shot.eye ? 0.05f : 0.3f;
                cam.farClipPlane = 4000;
                cam.fieldOfView = 68;
                cam.orthographicSize = radius;
                cam.orthographicSize = c == 5 ? radius * 0.28f : radius;
                cam.transform.position = shot.eye ? fit.EyeLocal : (c == 5 ? new Vector3(key == "KC390" ? 2 : 0, 1, key == "KC390" ? -4 : 0) : Vector3.zero) + dd * radius * 8;
                cam.transform.rotation = Quaternion.LookRotation(shot.eye ? dd : -dd, Vector3.up);
                sun.transform.rotation = Quaternion.LookRotation(shot.eye ? new Vector3(0.3f, -0.7f, 0.4f) : (-dd + Vector3.down * 0.6f).normalized);
                for (int k = 0; k < 3; k++) cam.Render();
                RenderTexture.active = rt;
                img.ReadPixels(new Rect(0, 0, W, H), W * (c % 3), H * (1 - c / 3));
            }
            RenderTexture.active = null;
            img.Apply();
            File.WriteAllBytes(Path.GetFullPath($"Logs/preview/rig_{key}.png"), img.EncodeToPNG());
            Debug.Log($"[FabRig] {key} olho {fit.EyeLocal} cg {ac.CgHeight}");
            Object.DestroyImmediate(fit.Root);
        }
        Debug.Log("[FabRig] pronto");
    }

    public static void Parts()
    {
        foreach (var path in ModelPaths().Where(p => Regex.IsMatch(p, "Mirage3|/F39E/|KC390|A29|ARG_A4X")))
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var whole = Measure(go);
            Debug.Log($"[FabParts] == {path} tam {whole.size}");
            var list = go.GetComponentsInChildren<Renderer>(true).OrderByDescending(r => r.bounds.size.magnitude).ToArray();
            int shown = 0;
            foreach (var r in list)
            {
                var b = r.bounds;
                string mat = r.sharedMaterial != null ? r.sharedMaterial.name : "-";
                bool cockpit = Regex.IsMatch(r.name + mat, "cockpit|cabin|panel|seat|hud|mfd|stick|interior|instrument", RegexOptions.IgnoreCase);
                bool odd = (path.Contains("/F39E/") && b.max.x > 6.5f) || (path.Contains("KC390") && (b.min.z < -13.2f || Regex.IsMatch(r.name + mat, "text|under|constr|plane", RegexOptions.IgnoreCase)));
                if (odd) Debug.Log($"[FabOdd] {r.name} | mat {mat} | min {b.min.x:0.00},{b.min.y:0.00},{b.min.z:0.00} | max {b.max.x:0.00},{b.max.y:0.00},{b.max.z:0.00}");
                if (shown++ < 7 || cockpit) Debug.Log($"[FabParts] {r.name} | mat {mat} | tam {b.size.x:0.00}x{b.size.y:0.00}x{b.size.z:0.00} | centro {b.center.x:0.00},{b.center.y:0.00},{b.center.z:0.00}");
            }
            Object.DestroyImmediate(go);
        }
    }

    public static void PreviewFit()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var sun = new GameObject("Sol").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.gameObject.AddComponent<HDAdditionalLightData>();
        sun.lightUnit = LightUnit.Lux;
        sun.intensity = 60000f;
        sun.transform.rotation = Quaternion.Euler(50, 30, 0);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var env = profile.Add<VisualEnvironment>(true);
        env.skyType.Override((int)SkyType.Gradient);
        env.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
        var sky = profile.Add<GradientSky>(true);
        sky.top.Override(new Color(0.7f, 0.8f, 1f));
        sky.middle.Override(new Color(0.6f, 0.65f, 0.7f));
        sky.bottom.Override(new Color(0.5f, 0.5f, 0.5f));
        sky.multiplier.Override(14000f);
        var exp = profile.Add<Exposure>(true);
        exp.mode.Override(ExposureMode.Fixed);
        exp.fixedExposure.Override(13f);
        var vol = new GameObject("Vol").AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = profile;
        var cam = new GameObject("Cam").AddComponent<Camera>();
        var hd = cam.gameObject.AddComponent<HDAdditionalCameraData>();
        hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        hd.backgroundColorHDR = new Color(0.55f, 0.6f, 0.68f) * 2.5f;
        cam.orthographic = true;
        cam.nearClipPlane = 0.01f;
        const int W = 420, H = 260;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        var keys = FABSim.Game.ModelFit.Table.Keys.ToArray();
        var dirs = new[] { Vector3.right, Vector3.forward, Vector3.up };
        string outDir = Path.GetFullPath("Logs/preview");
        Directory.CreateDirectory(outDir);
        const int perSheet = 6;
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        var mm = new Material(Shader.Find("HDRP/Unlit"));
        mm.SetColor("_UnlitColor", Color.red);
        marker.GetComponent<Renderer>().sharedMaterial = mm;
        for (int sheet = 0; sheet * perSheet < keys.Length; sheet++)
        {
            var img = new Texture2D(W * dirs.Length, H * perSheet, TextureFormat.RGB24, false);
            for (int row = 0; row < perSheet; row++)
            {
                int idx = sheet * perSheet + row;
                if (idx >= keys.Length) break;
                var def = FABSim.Game.ModelFit.Table[keys[idx]];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(def.Path);
                var fit = FABSim.Game.ModelFit.Spawn(keys[idx], null, prefab);
                var b = new Bounds(); bool first = true;
                foreach (var r in fit.Root.GetComponentsInChildren<Renderer>(false)) { if (!r.enabled) continue; if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
                float radius = Mathf.Max(b.extents.magnitude, 1f);
                marker.transform.position = new Vector3(0, def.BottomY ?? b.min.y, 0);
                marker.transform.localScale = new Vector3(radius * 2.2f, radius * 0.008f, radius * 2.2f);
                marker.SetActive(def.BottomY.HasValue);
                var flame = fit.Flame;
                for (int v = 0; v < dirs.Length; v++)
                {
                    Vector3 dd = dirs[v];
                    cam.transform.position = dd * (radius * 3);
                    cam.transform.rotation = Quaternion.LookRotation(-dd, dd == Vector3.up ? Vector3.forward : Vector3.up);
                    cam.orthographicSize = radius * 0.72f;
                    cam.farClipPlane = radius * 8;
                    for (int k = 0; k < 3; k++) cam.Render();
                    RenderTexture.active = rt;
                    img.ReadPixels(new Rect(0, 0, W, H), W * v, H * (perSheet - 1 - row));
                }
                RenderTexture.active = null;
                Debug.Log($"[FabFit] folha {sheet} linha {row}: {keys[idx]} | tam {b.size.x:0.0}x{b.size.y:0.0}x{b.size.z:0.0} | y {b.min.y:0.00}..{b.max.y:0.00} | olho {fit.EyeLocal.x:0.00},{fit.EyeLocal.y:0.00},{fit.EyeLocal.z:0.00} | chama {(flame != null ? flame.name : "-")}");
                Object.DestroyImmediate(fit.Root);
            }
            img.Apply();
            File.WriteAllBytes(Path.Combine(outDir, $"fit{sheet}.png"), img.EncodeToPNG());
        }
        Debug.Log("[FabFit] pronto");
    }

    public static void Preview()
    {
        foreach (var p in ModelPaths().Where(x => x.EndsWith(".glb"))) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        Models();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var sun = new GameObject("Sol").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.gameObject.AddComponent<HDAdditionalLightData>();
        sun.lightUnit = LightUnit.Lux;
        sun.intensity = 60000f;
        sun.transform.rotation = Quaternion.Euler(50, 30, 0);

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var env = profile.Add<VisualEnvironment>(true);
        env.skyType.Override((int)SkyType.Gradient);
        env.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
        var sky = profile.Add<GradientSky>(true);
        sky.top.Override(new Color(0.7f, 0.8f, 1f));
        sky.middle.Override(new Color(0.6f, 0.65f, 0.7f));
        sky.bottom.Override(new Color(0.35f, 0.35f, 0.35f));
        sky.multiplier.Override(8000f);
        var exp = profile.Add<Exposure>(true);
        exp.mode.Override(ExposureMode.Fixed);
        exp.fixedExposure.Override(13f);
        var vol = new GameObject("Vol").AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = profile;

        var cam = new GameObject("Cam").AddComponent<Camera>();
        var hd = cam.gameObject.AddComponent<HDAdditionalCameraData>();
        hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        hd.backgroundColorHDR = new Color(0.55f, 0.6f, 0.68f) * 2.5f;
        cam.orthographic = true;
        cam.nearClipPlane = 0.01f;

        const int W = 420, H = 260;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        var paths = ModelPaths();
        var dirs = new[] { Vector3.forward, Vector3.right, Vector3.up, Vector3.back };
        string outDir = Path.GetFullPath("Logs/preview");
        Directory.CreateDirectory(outDir);
        const int perSheet = 4;
        for (int sheet = 0; sheet * perSheet < paths.Length; sheet++)
        {
            var img = new Texture2D(W * dirs.Length, H * perSheet, TextureFormat.RGB24, false);
            for (int row = 0; row < perSheet; row++)
            {
                int idx = sheet * perSheet + row;
                if (idx >= paths.Length) break;
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(paths[idx]);
                var go = Object.Instantiate(asset);
                var b = Measure(go);
                float radius = b.extents.magnitude;
                for (int v = 0; v < dirs.Length; v++)
                {
                    Vector3 d = dirs[v];
                    cam.transform.position = b.center + d * (radius * 3);
                    cam.transform.rotation = Quaternion.LookRotation(-d, d == Vector3.up ? Vector3.forward : Vector3.up);
                    cam.orthographicSize = radius * 0.72f;
                    cam.farClipPlane = radius * 8;
                    for (int k = 0; k < 4; k++) cam.Render();
                    RenderTexture.active = rt;
                    img.ReadPixels(new Rect(0, 0, W, H), W * v, H * (perSheet - 1 - row));
                }
                RenderTexture.active = null;
                Object.DestroyImmediate(go);
                Debug.Log($"[FabPreview] folha {sheet} linha {row}: {paths[idx]}");
            }
            img.Apply();
            File.WriteAllBytes(Path.Combine(outDir, $"folha{sheet}.png"), img.EncodeToPNG());
        }
        Debug.Log("[FabPreview] pronto");
    }
}
