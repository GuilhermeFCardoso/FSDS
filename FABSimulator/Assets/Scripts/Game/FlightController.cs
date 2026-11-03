using System;
using System.Collections.Generic;
using System.IO;
using CesiumForUnity;
using FABSim.Core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace FABSim.Game
{
    public sealed class FlightController : MonoBehaviour
    {
        const double R2D = 180 / Math.PI, D2R = Math.PI / 180;

        public CesiumGeoreference georeference;
        public Cesium3DTileset terrain;
        public Transform model;
        public Camera cam;
        public string baseIcao = "SBAN";
        public int runwayIndex;
        public string aircraftId = "f39e";

        public AircraftDef Ac { get; private set; }
        public FlightState S { get; private set; }
        public Controls Ctl { get; } = new Controls();
        public FabInput Input { get; } = new FabInput();
        public bool Ready { get; private set; }
        public string Status { get; private set; } = "Carregando o terreno…";
        public bool Cockpit { get; private set; }
        public bool Paused { get; set; }
        public double Fps => smoothFps;
        public double? PitchOverride, ThrottleOverride;
        public void SetCockpit(bool on) { Cockpit = on; ApplyPose(); LateUpdate(); }
        public bool ProbeOut { get; set; }
        public bool AutoPilot { get; private set; }
        public bool LandingLight { get; private set; }
        public bool MasterArm { get; private set; } = true;
        public bool TemArmamento => aircraftId != "kc390";
        public float Zoom { get; set; }
        public float CameraDist { get; set; } = 1;
        public AircraftRig Rig => rig;
        public FittedModel Fit => fit;
        public void Teleport(double lat, double lon, double z, double hdg, double v)
        {
            double fuel = S.Fuel;
            S = FlightModel.SpawnAir(Ac, lat, lon, z, groundZ, hdg, v);
            S.Fuel = fuel;
            Ctl.GearDown = false; Ctl.Parking = false;
            ApplyPose();
        }

        CesiumGlobeAnchor anchor;
        FittedModel fit;
        AircraftRig rig;
        Combate combate;
        Light farol;
        double apAlt, apHdg, apInt;
        float panYaw, olharCima;
        readonly List<BuiltRunway> runways = new List<BuiltRunway>();
        readonly HashSet<string> builtBases = new HashSet<string>();
        bool buildingBase;
        Transform flame;
        VisualEnvironment skyEnv;
        Fog skyFog;
        double skyAlt = double.MaxValue;
        double groundZ, lastHit, gmax = 1, smoothFps = 60;
        int frame;
        float camYaw, camPitch = -0.16f, lookYaw, lookPitch;
        double startLat, startLon, startHdg;

        void Awake()
        {
            aircraftId = GameSession.AircraftId;
            combate = GetComponent<Combate>();
            var missao = GameData.Missao(GameSession.MissaoId);
            if (missao != null) baseIcao = missao.base_de_saida;
            anchor = model.GetComponent<CesiumGlobeAnchor>();
            anchor.adjustOrientationForGlobeWhenMoving = false;
            anchor.detectTransformChanges = false;
            fit = ModelFit.Spawn(aircraftId == "a29" ? "A29" : aircraftId == "kc390" ? "KC390" : "F39E", model);
            if (fit != null && fit.Flame != null) flame = FlameBuilder.Build(fit.Root.transform, fit.Nozzle);
            var vol = FindFirstObjectByType<Volume>();
            if (vol != null) { vol.profile.TryGet(out skyEnv); vol.profile.TryGet(out skyFog); }
            Ac = (aircraftId == "a29" ? AircraftDb.A29() : aircraftId == "kc390" ? AircraftDb.KC390() : AircraftDb.F39E()).Prepare();
            if (fit != null) rig = AircraftRig.Build(fit, aircraftId == "a29" ? "A29" : aircraftId == "kc390" ? "KC390" : "F39E", Ac);
            string tokenPath = Path.Combine(Application.dataPath, "..", "cesium-ion-token.txt");
            string token = File.Exists(tokenPath) ? File.ReadAllText(tokenPath).Trim() : "";
            if (token.Length == 0) Status = "Token do Cesium não encontrado em cesium-ion-token.txt";
            terrain.ionAccessToken = token;
            foreach (var overlay in terrain.GetComponents<CesiumIonRasterOverlay>()) overlay.ionAccessToken = token;
            terrain.gameObject.SetActive(true);
        }

        async void Start()
        {
            var b = BaseDb.Find(baseIcao);
            var rw = b.pistas[Mathf.Clamp(runwayIndex, 0, b.pistas.Length - 1)];
            startLat = rw.cabeceira_a.lat * D2R;
            startLon = rw.cabeceira_a.lon * D2R;
            startHdg = rw.rumo_verdadeiro * D2R;
            double along = 120;
            startLat += along * Math.Cos(startHdg) / FlightModel.RE;
            startLon += along * Math.Sin(startHdg) / (FlightModel.RE * Math.Cos(startLat));
            georeference.SetOriginLongitudeLatitudeHeight(startLon * R2D, startLat * R2D, b.elevacao_m);
            anchor.longitudeLatitudeHeight = new double3(startLon * R2D, startLat * R2D, b.elevacao_m + 100);
            anchor.rotationEastUpNorth = Quaternion.Euler(0, (float)(startHdg * R2D), 0);
            Status = $"Preparando as pistas de {b.nome}…";
            await BuildBase(b);
            groundZ = runways.Find(r => r.Data == rw).HeightAt(along);
            Spawn();
            GetComponent<Combate>()?.Begin(b, rw, startLat, startLon);
            Ready = true;
            Status = "";
        }

        async System.Threading.Tasks.Task BuildBase(BaseData b)
        {
            builtBases.Add(b.icao);
            foreach (var r in b.pistas) runways.Add(await RunwayBuilder.Build(terrain, georeference, r, b.elevacao_m));
        }

        async void BuildNearbyBases()
        {
            if (buildingBase) return;
            foreach (var b in BaseDb.All)
            {
                if (builtBases.Contains(b.icao)) continue;
                double n = (b.lat * D2R - S.Lat) * FlightModel.RE, e = (b.lon * D2R - S.Lon) * FlightModel.RE * Math.Cos(S.Lat);
                if (n * n + e * e > 90000.0 * 90000.0) continue;
                buildingBase = true;
                await BuildBase(b);
                buildingBase = false;
                return;
            }
        }

        void Aviso(string texto) => combate?.Toasts.Add(texto);

        void Comandos()
        {
            if (Input.ToggleProbe)
            {
                if (aircraftId == "a29") Aviso("O A-29 não tem sonda de reabastecimento");
                else { ProbeOut = !ProbeOut; Aviso(ProbeOut ? "Sonda estendida" : "Sonda recolhida"); }
            }
            if (Input.ToggleAp) AlternarPiloto();
            if (Input.ToggleLight)
            {
                LandingLight = !LandingLight;
                if (farol == null) farol = CriarFarol();
                farol.gameObject.SetActive(LandingLight);
            }
            if (Input.ToggleArm && TemArmamento) { MasterArm = !MasterArm; Aviso(MasterArm ? "Armamento ARMADO" : "Armamento SEGURO"); }
            if (Input.Jettison && TemArmamento) combate?.Alijar();
        }

        public void Noite()
        {
            var sol = GameObject.Find("Sol")?.GetComponent<Light>();
            if (sol != null)
            {
                sol.transform.rotation = Quaternion.Euler(38, 210, 0);
                sol.color = new Color(0.62f, 0.72f, 1f);
                sol.intensity = 0.6f;
            }
            var perfil = FindFirstObjectByType<Volume>()?.profile;
            if (perfil == null) return;
            if (!perfil.TryGet(out Exposure exp)) exp = perfil.Add<Exposure>(true);
            exp.mode.Override(ExposureMode.Fixed);
            exp.fixedExposure.Override(-1.6f);
        }

        public void AlternarPiloto()
        {
            if (aircraftId == "a29") { Aviso("Piloto automático indisponível no A-29"); return; }
            if (S.Contact) { Aviso("Piloto automático só em voo"); return; }
            AutoPilot = !AutoPilot;
            FlightModel.Euler(S.Q, out _, out _, out apHdg);
            apAlt = S.Z;
            apInt = 0;
            Aviso(AutoPilot ? (Rota.Atual != null ? "A/P ligado: seguindo a rota" : "A/P ligado: mantém proa e altitude") : "A/P desligado");
        }

        Light CriarFarol()
        {
            var go = new GameObject("Farol");
            go.transform.SetParent(model, false);
            go.transform.localPosition = new Vector3(0, -0.4f, (float)Ac.B * 0.3f);
            go.transform.localRotation = Quaternion.Euler(4, 0, 0);
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            go.AddComponent<HDAdditionalLightData>();
            l.spotAngle = 28;
            l.range = 1500;
            l.lightUnit = LightUnit.Lumen;
            l.intensity = 900000;
            return l;
        }

        void PilotoAutomatico()
        {
            if (Math.Abs(Ctl.Pitch) > 0.25 || Math.Abs(Ctl.Roll) > 0.25 || S.Contact) { AutoPilot = false; Aviso("A/P desligado pelo manche"); return; }
            FlightModel.Euler(S.Q, out double phi, out double theta, out double psi);
            var wp = Rota.Atual;
            double hdg = wp != null ? Rota.Rumo(S.Lat * R2D, S.Lon * R2D, wp.Lat, wp.Lon) * D2R : apHdg;
            double phiCmd = Cm.Clamp(Math.IEEERemainder(hdg - psi, 2 * Math.PI) * 1.4, -0.5, 0.5);
            Ctl.Roll = Cm.Clamp((phiCmd - phi) * 1.5 - S.W.X * 0.4, -0.6, 0.6);
            double vs = Cm.Clamp((apAlt - S.Z) * 0.25, -12, 12);
            double thetaCmd = Math.Asin(Cm.Clamp(vs / Math.Max(S.Diag.Tas, 60), -0.25, 0.25));
            // O integral vence a pouca resposta do manche em avioes sem controle fly-by-wire e em curva.
            apInt = Cm.Clamp(apInt + (thetaCmd - theta) * Time.unscaledDeltaTime * 1.5, -0.6, 0.6);
            Ctl.Pitch = Cm.Clamp((thetaCmd - theta) * 4 + apInt - S.W.Y * 1.2, -0.8, 0.8);
        }

        void UpdateSky()
        {
            if (skyEnv == null || Math.Abs(S.Z - skyAlt) < 25) return;
            skyAlt = S.Z;
            float seaWorld = (float)(model.position.y - S.Z);
            skyEnv.centerMode.Override(VisualEnvironment.PlanetMode.Manual);
            skyEnv.planetCenter.Override(new Vector3(0, -skyEnv.planetRadius.value + seaWorld / 1000f, 0));
            if (skyFog != null) { skyFog.baseHeight.Override(seaWorld); skyFog.maximumHeight.Override(seaWorld + 6000f); }
        }

        void Spawn()
        {
            S = FlightModel.SpawnGround(Ac, startLat, startLon, groundZ, startHdg, 0.6);
            Ctl.Throttle = 0; Ctl.GearDown = true; Ctl.FlapIdx = Math.Min(1, Ac.FlapNotches.Length - 1); Ctl.Parking = true; Ctl.Trim = 0;
            S.Flaps = Ac.FlapNotches[Ctl.FlapIdx];
            gmax = 1;
            camYaw = (float)startHdg;
            ApplyPose();
        }

        void UpdateGround(float dt)
        {
            foreach (var r in runways) if (r.TryHeight(S.Lat, S.Lon, out double rh)) { groundZ = rh; return; }
            double agl = S.Diag.Agl;
            if (agl < 600 || frame % 8 == 0)
            {
                Vector3 from = model.position + Vector3.up * 40f;
                if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 40000f))
                {
                    double target = S.Z + 40 - hit.distance;
                    groundZ = Math.Abs(target - groundZ) > 2 ? target : groundZ + (target - groundZ) * 0.35;
                    lastHit = hit.distance;
                }
            }
        }

        void Update()
        {
            if (!Ready) return;
            frame++;
            double dt = Math.Min(Time.unscaledDeltaTime, 0.5);
            smoothFps += (1 / Math.Max(dt, 1e-3) - smoothFps) * 0.1;
            bool gearCmd = Ctl.GearDown; int flapCmd = Ctl.FlapIdx;
            Input.Read(Ctl, S, Ac, dt);
            if (Paused) { Ctl.GearDown = gearCmd; Ctl.FlapIdx = flapCmd; }
            else Comandos();
            Input.LookZoomed = Cockpit && Zoom > 0.03f;
            if (ThrottleOverride.HasValue) { Ctl.Throttle = ThrottleOverride.Value; Ctl.Parking = false; }
            if (PitchOverride.HasValue) Ctl.Pitch = PitchOverride.Value;
            if (Input.ToggleCamera) Cockpit = !Cockpit;
            if (frame % 240 == 0) BuildNearbyBases();
            UpdateSky();
            if (Paused) return;
            if (S.Crashed.Length == 0)
            {
                if (AutoPilot) PilotoAutomatico();
                UpdateGround((float)dt);
                int n = Math.Min(60, Math.Max(1, (int)Math.Ceiling(dt * 120)));
                double h = dt / n;
                for (int i = 0; i < n; i++) FlightModel.Step(S, Ac, Ctl, h, groundZ);
            }
            gmax = Math.Max(gmax, S.Diag.Nz);
            ApplyPose();
            rig?.Apply(S, (float)dt);
        }

        void ApplyPose()
        {
            anchor.longitudeLatitudeHeight = new double3(S.Lon * R2D, S.Lat * R2D, S.Z);
            var R = Mat3.From(S.Q);
            V3 f = R.Mul(new V3(1, 0, 0)), d = R.Mul(new V3(0, 0, 1));
            var fwd = new Vector3((float)f.Y, (float)-f.Z, (float)f.X);
            var up = new Vector3((float)-d.Y, (float)d.Z, (float)-d.X);
            anchor.rotationEastUpNorth = Quaternion.LookRotation(fwd, up);
            anchor.Sync();
            if (flame != null)
            {
                bool on = S.Ab > 0.05 && !Cockpit;
                flame.gameObject.SetActive(on);
                if (on) flame.localScale = new Vector3(1, 1, (float)(0.45 + 0.55 * S.Ab) * UnityEngine.Random.Range(0.93f, 1.07f));
            }
        }

        void LateUpdate()
        {
            if (!Ready || cam == null) return;
            float dt = Time.unscaledDeltaTime;
            FlightModel.Euler(S.Q, out _, out double theta, out double psi);
            lookYaw = Mathf.Lerp(lookYaw, (float)Input.LookYaw * 2.3f, 1 - Mathf.Exp(-dt * 8));
            lookPitch = Mathf.Lerp(lookPitch, (float)Input.LookPitch * 0.9f, 1 - Mathf.Exp(-dt * 8));
            if (Cockpit)
            {
                // Direcional direito: frente aproxima dos instrumentos, tras afasta e depois olha para cima.
                float sy = (float)(Input.ZoomAxis + Input.LookPitch), sx = (float)Input.LookYaw;
                Zoom = Mathf.Clamp01(Zoom + sy * dt * 0.9f);
                olharCima = Mathf.MoveTowards(olharCima, sy < 0 && Zoom <= 0 ? -sy : 0, dt * 2.5f);
                panYaw = Zoom > 0.03f ? Mathf.Clamp(panYaw + sx * dt * 0.5f, -0.55f, 0.55f) : 0;
                float yawLivre = Zoom > 0.03f ? 0 : lookYaw;
                Vector3 eye = fit != null ? fit.EyeLocal : new Vector3(0, 0.84f, 3.0f);
                Vector3 painel = rig != null ? rig.PainelPara(eye) : eye + new Vector3(0, -0.45f, 0.8f);
                Vector3 toPanel = painel - eye;
                float aoPainel = Zoom * (1 - Mathf.SmoothStep(0.3f, 0.7f, Mathf.Abs(yawLivre)));
                var olhar = Quaternion.Euler(-olharCima * 40, yawLivre * Mathf.Rad2Deg, 0);
                var instrumentos = Quaternion.LookRotation(toPanel) * Quaternion.Euler(0, panYaw * Mathf.Rad2Deg, 0);
                cam.fieldOfView = Mathf.Lerp(68, 38, Zoom);
                cam.transform.position = model.TransformPoint(eye + toPanel * (0.3f * aoPainel));
                cam.transform.rotation = model.rotation * Quaternion.Slerp(olhar, instrumentos, aoPainel);
                return;
            }
            cam.fieldOfView = 50;
            if (!Paused) CameraDist = Mathf.Clamp(CameraDist * Mathf.Exp(-(float)Input.ZoomAxis * dt * 1.2f), 0.45f, 5f);
            float dh = Mathf.DeltaAngle(camYaw * Mathf.Rad2Deg, (float)(psi * R2D)) * Mathf.Deg2Rad;
            camYaw += dh * (1 - Mathf.Exp(-4 * dt));
            camPitch += (-0.16f + (float)theta * 0.45f - camPitch) * (1 - Mathf.Exp(-3 * dt));
            float y = camYaw + lookYaw, p = camPitch - lookPitch * 0.6f, dist = (float)(Ac.B * 2.6) * CameraDist;
            var dir = new Vector3(Mathf.Sin(y) * Mathf.Cos(p), Mathf.Sin(p), Mathf.Cos(y) * Mathf.Cos(p));
            cam.transform.position = model.position - dir * dist;
            cam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }

    public static class FlameBuilder
    {
        public static Transform Build(Transform parent, Vector3 nozzle)
        {
            var go = new GameObject("Chama");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = nozzle;
            var tex = new Texture2D(4, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
            {
                float k = y / 63f, a = Mathf.Pow(1 - k, 1.6f);
                for (int x = 0; x < 4; x++) tex.SetPixel(x, y, new Color(1, 1 - k * 0.35f, 1 - k * 0.8f, a));
            }
            tex.Apply();
            var mat = new Material(Resources.Load<Material>("FabChama"));
            mat.SetTexture("_UnlitColorMap", tex);
            mat.SetColor("_UnlitColor", new Color(3.2f, 1.7f, 0.7f, 1));
            Cone(go.transform, mat, 0.36f, 5.5f);
            Cone(go.transform, mat, 0.22f, 3.2f);
            return go.transform;
        }

        static void Cone(Transform parent, Material mat, float radius, float length)
        {
            const int Seg = 16, Rings = 8;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int r = 0; r <= Rings; r++)
            {
                float k = r / (float)Rings, rad = radius * (1 - k * k) * (1 + 0.25f * Mathf.Sin(k * 18));
                for (int i = 0; i <= Seg; i++)
                {
                    float a = i * Mathf.PI * 2 / Seg;
                    v.Add(new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad, -k * length));
                    uv.Add(new Vector2(i / (float)Seg, k));
                }
            }
            for (int r = 0; r < Rings; r++)
                for (int i = 0; i < Seg; i++)
                {
                    int a = r * (Seg + 1) + i, b = a + Seg + 1;
                    t.Add(a); t.Add(b); t.Add(a + 1); t.Add(a + 1); t.Add(b); t.Add(b + 1);
                }
            var m = new Mesh { name = "Chama" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds();
            var go = new GameObject("Cone");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
