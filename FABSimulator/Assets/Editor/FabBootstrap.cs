using CesiumForUnity;
using FABSim.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

public static class FabBootstrap
{
    const string Root = "Assets/Settings";
    const string ScenePath = "Assets/Scenes/Voo.unity";

    [MenuItem("FAB Simulator/Gerar cena de voo")]
    public static void Build()
    {
        EnsureFolder("Assets/Settings");
        EnsureFolder("Assets/Scenes");
        ConfigurePlayer();
        ConfigurePipeline();
        BuildScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[FabBootstrap] Projeto pronto.");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int i = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1));
    }

    static void ConfigurePlayer()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.companyName = "FAB Simulator";
        PlayerSettings.productName = "FAB Simulator - Defendendo a Soberania Nacional";
        var ps = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var handler = ps.FindProperty("activeInputHandler");
        if (handler != null) { handler.intValue = 2; ps.ApplyModifiedProperties(); }
    }

    static void ConfigurePipeline()
    {
        string path = Root + "/FabHDRP.asset";
        var hd = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(path);
        if (hd == null)
        {
            hd = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
            AssetDatabase.CreateAsset(hd, path);
        }
        var so = new SerializedObject(hd);
        foreach (var (prop, value) in new[]
        {
            ("m_RenderPipelineSettings.supportVolumetrics", true),
            ("m_RenderPipelineSettings.supportVolumetricClouds", true),
            ("m_RenderPipelineSettings.supportSSR", false),
            ("m_RenderPipelineSettings.supportSSGI", false),
            ("m_RenderPipelineSettings.supportWater", false),
            ("m_RenderPipelineSettings.supportRayTracing", false),
        })
        {
            var p = so.FindProperty(prop);
            if (p != null) p.boolValue = value; else Debug.LogWarning("[FabBootstrap] Sem a opção " + prop);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        GraphicsSettings.defaultRenderPipeline = hd;
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = hd;
        }
        EditorUtility.SetDirty(hd);
    }

    static VolumeProfile BuildProfile()
    {
        string path = Root + "/FabCeu.asset";
        var old = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (old != null) AssetDatabase.DeleteAsset(path);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);
        var env = profile.Add<VisualEnvironment>(true);
        env.skyType.Override((int)SkyType.PhysicallyBased);
        env.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
        profile.Add<PhysicallyBasedSky>(true);
        var fog = profile.Add<Fog>(true);
        fog.enabled.Override(true);
        fog.meanFreePath.Override(60000f);
        fog.baseHeight.Override(0f);
        fog.maximumHeight.Override(6000f);
        var clouds = profile.Add<VolumetricClouds>(true);
        clouds.enable.Override(true);
        var exposure = profile.Add<Exposure>(true);
        exposure.mode.Override(ExposureMode.AutomaticHistogram);
        foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var sun = new GameObject("Sol").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.gameObject.AddComponent<HDAdditionalLightData>();
        sun.lightUnit = LightUnit.Lux;
        sun.intensity = 100000f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(52, 35, 0);

        var volume = new GameObject("Ceu").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = BuildProfile();

        var geoGo = new GameObject("CesiumGeoreference");
        var geo = geoGo.AddComponent<CesiumGeoreference>();

        var terrainGo = new GameObject("Terreno");
        terrainGo.transform.SetParent(geoGo.transform, false);
        terrainGo.SetActive(false);
        var terrain = terrainGo.AddComponent<Cesium3DTileset>();
        terrain.tilesetSource = CesiumDataSource.FromCesiumIon;
        terrain.ionAssetID = 1;
        terrain.createPhysicsMeshes = true;
        terrain.maximumScreenSpaceError = 16f;
        terrain.maximumCachedBytes = 1024L * 1024 * 1024;
        var imagery = terrainGo.AddComponent<CesiumIonRasterOverlay>();
        imagery.ionAssetID = 2;

        var plane = new GameObject("Aeronave");
        plane.transform.SetParent(geoGo.transform, false);
        plane.AddComponent<CesiumGlobeAnchor>();
        var shift = plane.AddComponent<CesiumOriginShift>();
        shift.distance = 2000;
        BuildPlaceholder(plane.transform);

        var camGo = new GameObject("Camera");
        var cam = camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        camGo.AddComponent<HDAdditionalCameraData>();
        camGo.AddComponent<AudioListener>();
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000000f;

        var ctrl = new GameObject("Voo").AddComponent<FlightController>();
        ctrl.georeference = geo;
        ctrl.terrain = terrain;
        ctrl.model = plane.transform;
        ctrl.cam = cam;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    static void BuildPlaceholder(Transform parent)
    {
        var mat = new Material(Shader.Find("HDRP/Lit"));
        mat.SetColor("_BaseColor", new Color(0.42f, 0.47f, 0.52f));
        AssetDatabase.CreateAsset(mat, Root + "/FabProvisorio.mat");
        Part(parent, PrimitiveType.Capsule, new Vector3(0, 0, 0), new Vector3(1.3f, 7.6f, 1.3f), new Vector3(90, 0, 0), mat);
        Part(parent, PrimitiveType.Cube, new Vector3(0, -0.2f, -1.5f), new Vector3(8.6f, 0.12f, 3.6f), Vector3.zero, mat);
        Part(parent, PrimitiveType.Cube, new Vector3(0, 0.1f, 3.2f), new Vector3(4.3f, 0.08f, 1.2f), Vector3.zero, mat);
        Part(parent, PrimitiveType.Cube, new Vector3(0, 1.5f, -5.4f), new Vector3(0.12f, 2.8f, 2.2f), Vector3.zero, mat);
    }

    static void Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Vector3 euler, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.transform.localEulerAngles = euler;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
