using System;
using System.IO;
using System.Threading.Tasks;
using CesiumForUnity;
using FABSim.Core;
using Unity.Mathematics;
using UnityEngine;

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

        CesiumGlobeAnchor anchor;
        double groundZ, lastHit, gmax = 1, smoothFps = 60;
        int frame;
        bool paused;
        float camYaw, camPitch = -0.16f, lookYaw, lookPitch;
        double startLat, startLon, startHdg;

        void Awake()
        {
            anchor = model.GetComponent<CesiumGlobeAnchor>();
            anchor.adjustOrientationForGlobeWhenMoving = false;
            anchor.detectTransformChanges = false;
            Ac = (aircraftId == "a29" ? AircraftDb.A29() : aircraftId == "kc390" ? AircraftDb.KC390() : AircraftDb.F39E()).Prepare();
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
            Status = $"Carregando {b.nome}…";
            groundZ = await SampleGround(b.elevacao_m);
            Spawn();
            Ready = true;
            Status = "";
        }

        async Task<double> SampleGround(double fallback)
        {
            try
            {
                var r = await terrain.SampleHeightMostDetailed(new double3(startLon * R2D, startLat * R2D, 0));
                if (r.sampleSuccess.Length > 0 && r.sampleSuccess[0]) return r.longitudeLatitudeHeightPositions[0].z;
            }
            catch (Exception e) { Debug.LogWarning("Amostragem do terreno falhou: " + e.Message); }
            return fallback;
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
            Input.Read(Ctl, S, Ac, dt);
            if (Input.ToggleMenu) paused = !paused;
            if (Input.ToggleCamera) Cockpit = !Cockpit;
            if (Input.Restart) Spawn();
            if (paused) return;
            if (S.Crashed.Length == 0)
            {
                UpdateGround((float)dt);
                int n = Math.Min(60, Math.Max(1, (int)Math.Ceiling(dt * 120)));
                double h = dt / n;
                for (int i = 0; i < n; i++) FlightModel.Step(S, Ac, Ctl, h, groundZ);
            }
            gmax = Math.Max(gmax, S.Diag.Nz);
            ApplyPose();
        }

        void ApplyPose()
        {
            anchor.longitudeLatitudeHeight = new double3(S.Lon * R2D, S.Lat * R2D, S.Z);
            var R = Mat3.From(S.Q);
            V3 f = R.Mul(new V3(1, 0, 0)), d = R.Mul(new V3(0, 0, 1));
            var fwd = new Vector3((float)f.Y, (float)-f.Z, (float)f.X);
            var up = new Vector3((float)-d.Y, (float)d.Z, (float)-d.X);
            anchor.rotationEastUpNorth = Quaternion.LookRotation(fwd, up);
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
                cam.fieldOfView = 55;
                cam.transform.position = model.TransformPoint(new Vector3(0, 0.84f, 3.0f));
                cam.transform.rotation = model.rotation * Quaternion.Euler(-lookPitch * Mathf.Rad2Deg, lookYaw * Mathf.Rad2Deg, 0);
                return;
            }
            cam.fieldOfView = 50;
            float dh = Mathf.DeltaAngle(camYaw * Mathf.Rad2Deg, (float)(psi * R2D)) * Mathf.Deg2Rad;
            camYaw += dh * (1 - Mathf.Exp(-4 * dt));
            camPitch += (-0.16f + (float)theta * 0.45f - camPitch) * (1 - Mathf.Exp(-3 * dt));
            float y = camYaw + lookYaw, p = camPitch - lookPitch * 0.6f, dist = (float)(Ac.B * 2.6);
            var dir = new Vector3(Mathf.Sin(y) * Mathf.Cos(p), Mathf.Sin(p), Mathf.Cos(y) * Mathf.Cos(p));
            cam.transform.position = model.position - dir * dist;
            cam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            style.normal.textColor = new Color(0.45f, 1f, 0.6f);
            if (!Ready) { GUI.Label(new Rect(20, 20, 900, 40), Status, style); return; }
            var d = S.Diag;
            string gear = Ac.GearFixed ? "FIXO" : S.Gear > 0.99 ? "BAIXO" : S.Gear < 0.01 ? "CIMA" : "MOV";
            string txt = $"{Ac.Name}   {Math.Round(smoothFps)} fps\n" +
                         $"IAS {d.Ias:0} kt   M {d.Mach:0.00}   G {d.Nz:0.0} (máx {gmax:0.0})\n" +
                         $"ALT {S.Z * FlightModel.FT:0} ft   AGL {d.Agl:0} m   VS {d.Vsi:0}\n" +
                         $"PROA {d.Hdg:000}   MANETE {Ctl.Throttle * 100:0}%{(S.Ab > 0.05 ? " A/B" : "")}{(Input.ThrottleLocked ? " TRAVADA" : "")}\n" +
                         $"TREM {gear}   FLAP {Ctl.FlapIdx}   SB {(S.Sb > 0.05 ? "ABERTO" : "FECH")}{(Ctl.Parking ? "   FREIO EST." : "")}";
            GUI.Label(new Rect(20, 20, 900, 150), txt, style);
            if (S.Crashed.Length > 0) { style.fontSize = 28; style.normal.textColor = new Color(1, 0.35f, 0.3f); GUI.Label(new Rect(20, 170, 1200, 50), S.Crashed + " — aperte R para reiniciar", style); }
            if (paused) { style.fontSize = 28; GUI.Label(new Rect(20, 170, 800, 50), "PAUSADO", style); }
        }
    }
}
