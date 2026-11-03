using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FABSim.Game
{
    public sealed class FittedModel
    {
        public GameObject Root;
        public Vector3 EyeLocal;
        public Renderer Flame;
        public Vector3 Nozzle;
        public float Length;
    }

    public static class ModelFit
    {
        public sealed class Def
        {
            public string Key, Path, Hide, Flame, Glass;
            public Vector3 Euler, PostEuler;
            public float Length, Scale;
            public Vector3? Center;
            public float? BottomY;
            public Vector3? EyeModel;
        }

        public static readonly Dictionary<string, Def> Table = new Dictionary<string, Def>
        {
            ["F39E"] = new Def { Key = "F39E", Path = "Assets/Art/Models/F39E/saab_jas_39a_gripen_detailed_cockpit.glb", Euler = new Vector3(0, 90, 0), Scale = 1.06f, Center = new Vector3(0, 0, 0.8f), BottomY = -4.62f, Flame = "Object_7", Hide = "^Object_19$", EyeModel = new Vector3(-4.98f, 0.62f, 0) },
            ["F39E_ALA"] = new Def { Key = "F39E_ALA", Path = "Assets/Art/Models/F39E_ALA/saab_jas_39_gripen_bohmerang.glb", Euler = new Vector3(0, 90, 0), Length = 15.2f, Hide = "^Object_1[45]$" },
            ["A29"] = new Def { Key = "A29", Path = "Assets/Art/Models/A29/source/A-29/A29SUPERTUCANO.fbx", Euler = new Vector3(0, 180, 0), Scale = 1.04f, BottomY = -1.2f, Glass = "^Glass$", EyeModel = new Vector3(0, 1.42f, -1.75f) },
            ["KC390"] = new Def { Key = "KC390", Path = "Assets/Art/Models/KC390/source/Embraer KC-390/KC-390.fbx", Euler = new Vector3(0, 180, 0), Length = 35.2f, Hide = "^Text$", Glass = "^(Glass|hud)$", BottomY = -2.95f, EyeModel = new Vector3(-0.55f, 3.12f, -6.85f) },
            ["EUA_FA18"] = new Def { Key = "EUA_FA18", Path = "Assets/Art/Models/EUA_FA18/boeing_fa-18ef_super_hornet.glb", Euler = new Vector3(0, 90, 0), Length = 18.3f },
            ["EUA_F22"] = new Def { Key = "EUA_F22", Path = "Assets/Art/Models/EUA_F22/lockheed_martin_f-22_raptor.glb", Euler = new Vector3(0, -90, 0), Length = 18.9f },
            ["EUA_MH47"] = new Def { Key = "EUA_MH47", Path = "Assets/Art/Models/EUA_CH47/boeing_ch-47_chinook_military_transport_aircraft.glb", Length = 30f },
            ["EUA_MH60"] = new Def { Key = "EUA_MH60", Path = "Assets/Art/Models/EUA_SH60B/sh-60b_seahawk_helicopter.glb", Length = 19.8f },
            ["EUA_P8"] = new Def { Key = "EUA_P8", Path = "Assets/Art/Models/EUA_P8/boeing_p-8_poseidon.glb", Euler = new Vector3(0, 180, 0), Length = 39.5f },
            ["ARG_Mirage3"] = new Def { Key = "ARG_Mirage3", Path = "Assets/Art/Models/ARG_Mirage3/dassault_mirage_iii.glb", Euler = new Vector3(0, 0, 0), Length = 15f, Hide = "^Plane_0$" },
            ["ARG_A4X"] = new Def { Key = "ARG_A4X", Path = "Assets/Art/Models/ARG_A4X/a-4x_ultra_skyhawk.glb", Euler = new Vector3(0, 108.4f, 0), PostEuler = new Vector3(2, 0, -20.7f), Length = 12.2f },
            ["NARCO_Cessna172"] = new Def { Key = "NARCO_Cessna172", Path = "Assets/Art/Models/NARCO_Cessna172/plane.glb", Euler = new Vector3(0, 168.7f, 0), Length = 8.28f },
            ["NARCO_Cessna421"] = new Def { Key = "NARCO_Cessna421", Path = "Assets/Art/Models/NARCO_Cessna421/cessna_421_golden_eagle.glb", Euler = new Vector3(0, -90, 0), Length = 11.1f },
            ["NAVIO_Tamandare"] = new Def { Key = "NAVIO_Tamandare", Path = "Assets/Art/Models/NAVIO_Tamandare/tamandare_frigate_sem_heli.glb", Euler = new Vector3(0, 180, 0), Length = 107f },
        };

        static ModelLibrary library;
        public static ModelLibrary Library => library != null ? library : library = Resources.Load<ModelLibrary>("FabModels");

        // Os FBX importam o vidro como opaco, o que tapa a vista da cabine e faz sombra no interior.
        static void ApplyGlass(Renderer r, Regex glass)
        {
            var mats = r.sharedMaterials;
            bool any = false;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null && glass.IsMatch(mats[i].name)) { mats[i] = Resources.Load<Material>("FabVidro"); any = true; }
            if (!any) return;
            r.sharedMaterials = mats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public static FittedModel Spawn(string key, Transform parent, GameObject prefabOverride = null)
        {
            var def = Table[key];
            var prefab = prefabOverride != null ? prefabOverride : Library != null ? Library.Find(key) : null;
            if (prefab == null) { Debug.LogError("[ModelFit] Modelo ausente: " + key); return null; }
            var root = new GameObject("Modelo_" + key);
            var pivot = new GameObject("Pivo").transform;
            pivot.SetParent(root.transform, false);
            pivot.localRotation = Quaternion.Euler(def.PostEuler) * Quaternion.Euler(def.Euler);
            var inner = UnityEngine.Object.Instantiate(prefab, pivot, false);
            inner.name = key;

            var hide = def.Hide != null ? new Regex(def.Hide) : null;
            var glass = def.Glass != null ? new Regex(def.Glass) : null;
            Renderer flame = null;
            foreach (var r in inner.GetComponentsInChildren<Renderer>(true))
            {
                if (hide != null && hide.IsMatch(r.name)) { r.enabled = false; continue; }
                if (def.Flame != null && r.name == def.Flame) flame = r;
                if (glass != null) ApplyGlass(r, glass);
            }

            Bounds Measure()
            {
                var b = new Bounds(); bool first = true;
                foreach (var r in inner.GetComponentsInChildren<Renderer>(true))
                {
                    if (!r.enabled || r == flame) continue;
                    if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
                }
                return b;
            }

            var measured = Measure();
            float scale = def.Scale > 0 ? def.Scale : def.Length > 0 && measured.size.z > 0 ? def.Length / measured.size.z : 1;
            pivot.localScale = Vector3.one * scale;
            measured = Measure();
            Vector3 c = def.Center.HasValue ? def.Center.Value * scale : measured.center;
            if (def.Center.HasValue) c.y = measured.center.y;
            float y = def.BottomY.HasValue ? def.BottomY.Value - measured.min.y : -c.y;
            pivot.localPosition = new Vector3(-c.x, y, -c.z);

            var fit = new FittedModel { Root = root, Flame = flame, Length = Measure().size.z };
            if (def.EyeModel.HasValue) fit.EyeLocal = pivot.TransformPoint(def.EyeModel.Value);
            if (flame != null) { var fb = flame.bounds; fit.Nozzle = new Vector3(fb.center.x, fb.center.y, fb.max.z); flame.enabled = false; }
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            return fit;
        }
    }
}
