using CesiumForUnity;
using FABSim.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class FabBootstrap
{
    const string Root = "Assets/Settings";
    const string ScenePath = "Assets/Scenes/Voo.unity";
    const string LobbyPath = "Assets/Scenes/Saguao.unity";

    [MenuItem("FAB Simulator/Gerar cena de voo")]
    public static void Build()
    {
        EnsureFolder("Assets/Settings");
        EnsureFolder("Assets/Scenes");
        ConfigurePlayer();
        BuildLibrary();
        ConfigurePipeline();
        BuildPanel();
        BuildLobby();
        BuildScene();
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(LobbyPath, true), new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[FabBootstrap] Projeto pronto.");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int i = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1));
    }

    static void BuildLibrary()
    {
        EnsureFolder("Assets/Resources");
        Transparent("FabChama", 1);
        Transparent("FabFumaca", 0);
        var tracer = new Material(Shader.Find("HDRP/Unlit"));
        tracer.SetColor("_UnlitColor", new Color(6f, 4f, 1.6f));
        SaveMat(tracer, "FabTracer");
        var vidro = new Material(Shader.Find("HDRP/Lit"));
        vidro.SetColor("_BaseColor", new Color(0.75f, 0.85f, 0.92f, 0.12f));
        vidro.SetFloat("_Smoothness", 0.95f);
        HDMaterial.SetSurfaceType(vidro, true);
        HDMaterial.ValidateMaterial(vidro);
        SaveMat(vidro, "FabVidro");
        var tela = new Material(Shader.Find("HDRP/Unlit"));
        tela.SetColor("_UnlitColor", Color.white);
        HDMaterial.ValidateMaterial(tela);
        SaveMat(tela, "FabTela");
        Lit("FabMetal", new Color(0.42f, 0.44f, 0.46f), 0.4f);
        Lit("FabDestroco", new Color(0.03f, 0.03f, 0.03f), 0.05f);
        string path = "Assets/Resources/FabModels.asset";
        var lib = AssetDatabase.LoadAssetAtPath<ModelLibrary>(path);
        if (lib == null) { lib = ScriptableObject.CreateInstance<ModelLibrary>(); AssetDatabase.CreateAsset(lib, path); }
        lib.entries.Clear();
        foreach (var kv in ModelFit.Table)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(kv.Value.Path);
            if (prefab == null) { Debug.LogError("[FabBootstrap] Modelo nao encontrado: " + kv.Value.Path); continue; }
            lib.entries.Add(new ModelLibrary.Entry { key = kv.Key, prefab = prefab });
        }
        EditorUtility.SetDirty(lib);
    }

    static void SaveMat(Material m, string name)
    {
        string p = $"Assets/Resources/{name}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(p) != null) AssetDatabase.DeleteAsset(p);
        AssetDatabase.CreateAsset(m, p);
    }

    static void Transparent(string name, int blend)
    {
        var m = new Material(Shader.Find("HDRP/Unlit"));
        m.SetFloat("_BlendMode", blend);
        m.SetFloat("_DoubleSidedEnable", 1);
        m.SetFloat("_ZWrite", 0);
        m.SetFloat("_TransparentZWrite", 0);
        m.SetColor("_UnlitColor", Color.white);
        HDMaterial.SetSurfaceType(m, true);
        HDMaterial.ValidateMaterial(m);
        SaveMat(m, name);
    }

    static void Lit(string name, Color c, float smooth)
    {
        var m = new Material(Shader.Find("HDRP/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        HDMaterial.ValidateMaterial(m);
        SaveMat(m, name);
    }

    static void BuildPanel()
    {
        EnsureFolder("Assets/UI");
        string p = "Assets/UI/FabPainel.asset";
        if (AssetDatabase.LoadAssetAtPath<PanelSettings>(p) != null) AssetDatabase.DeleteAsset(p);
        var s = ScriptableObject.CreateInstance<PanelSettings>();
        s.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/FabTema.tss");
        if (s.themeStyleSheet == null) Debug.LogError("[FabBootstrap] Tema de UI ausente");
        s.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2Int(1920, 1080);
        s.match = 0.5f;
        AssetDatabase.CreateAsset(s, p);
    }

    static UIDocument AddUi(GameObject go)
    {
        var d = go.AddComponent<UIDocument>();
        d.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/FabPainel.asset");
        return d;
    }

    static void BuildLobby()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeSun();
        var volume = new GameObject("Ceu").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = BuildProfile("FabSaguaoCeu", false);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Patio";
        floor.transform.position = new Vector3(0, -0.5f, 0);
        floor.transform.localScale = new Vector3(600, 1, 600);
        Lit("FabPatio", new Color(0.06f, 0.065f, 0.07f), 0.55f);
        floor.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/FabPatio.mat");
        var table = new GameObject("Vitrine");
        var camGo = new GameObject("Camera");
        var cam = camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        camGo.AddComponent<HDAdditionalCameraData>();
        camGo.AddComponent<AudioListener>();
        cam.fieldOfView = 32;
        cam.transform.position = new Vector3(0, 6, -30);
        var ctrl = new GameObject("Saguao");
        AddUi(ctrl);
        var sag = ctrl.AddComponent<Saguao>();
        sag.turntable = table.transform;
        sag.cam = cam;
        EditorSceneManager.SaveScene(scene, LobbyPath);
    }

    static void ConfigurePlayer()
    {
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });
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

    static VolumeProfile BuildProfile(string name, bool withClouds)
    {
        string path = $"{Root}/{name}.asset";
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
        if (withClouds)
        {
            var clouds = profile.Add<VolumetricClouds>(true);
            clouds.enable.Override(true);
            clouds.bottomAltitude.Override(3000f);
            clouds.altitudeRange.Override(2200f);
        }
        var blur = profile.Add<MotionBlur>(true);
        blur.intensity.Override(0f);
        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.12f);
        var shadows = profile.Add<HDShadowSettings>(true);
        shadows.maxShadowDistance.Override(1500f);
        var exposure = profile.Add<Exposure>(true);
        exposure.mode.Override(ExposureMode.AutomaticHistogram);
        foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    static void MakeSun()
    {
        var sun = new GameObject("Sol").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.gameObject.AddComponent<HDAdditionalLightData>();
        sun.lightUnit = LightUnit.Lux;
        sun.intensity = 100000f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(52, 35, 0);
    }

    static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        MakeSun();

        var volume = new GameObject("Ceu").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = BuildProfile("FabCeu", true);

        var geoGo = new GameObject("CesiumGeoreference");
        var geo = geoGo.AddComponent<CesiumGeoreference>();

        var terrainGo = new GameObject("Terreno");
        terrainGo.transform.SetParent(geoGo.transform, false);
        terrainGo.SetActive(false);
        var terrain = terrainGo.AddComponent<Cesium3DTileset>();
        terrain.tilesetSource = CesiumDataSource.FromCesiumIon;
        terrain.ionAssetID = 1;
        terrain.createPhysicsMeshes = true;
        terrain.maximumScreenSpaceError = 8f;
        terrain.maximumCachedBytes = 1024L * 1024 * 1024;
        var imagery = terrainGo.AddComponent<CesiumIonRasterOverlay>();
        imagery.ionAssetID = 2;

        var plane = new GameObject("Aeronave");
        plane.transform.SetParent(geoGo.transform, false);
        plane.AddComponent<CesiumGlobeAnchor>();
        var shift = plane.AddComponent<CesiumOriginShift>();
        shift.distance = 2000;

        var camGo = new GameObject("Camera");
        var cam = camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        camGo.AddComponent<HDAdditionalCameraData>();
        camGo.AddComponent<AudioListener>();
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000000f;

        var ctrl = new GameObject("Voo").AddComponent<FlightController>();
        AddUi(ctrl.gameObject);
        ctrl.gameObject.AddComponent<Combate>();
        ctrl.gameObject.AddComponent<Hud>();
        ctrl.georeference = geo;
        ctrl.terrain = terrain;
        ctrl.model = plane.transform;
        ctrl.cam = cam;
        Application.runInBackground = true;
        PlayerSettings.runInBackground = true;

        EditorSceneManager.SaveScene(scene, ScenePath);
    }
}
