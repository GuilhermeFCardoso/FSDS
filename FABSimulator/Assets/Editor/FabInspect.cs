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
