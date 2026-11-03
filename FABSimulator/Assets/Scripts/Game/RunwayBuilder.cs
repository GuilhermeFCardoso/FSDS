using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;

namespace FABSim.Game
{
    public sealed class BuiltRunway
    {
        public RunwayData Data;
        public double Lat0, Lon0, Hdg, Length, Width;
        public double[] Profile;

        public void ToLocal(double latRad, double lonRad, out double along, out double cross)
        {
            double n = (latRad - Lat0) * RunwayBuilder.RE, e = (lonRad - Lon0) * RunwayBuilder.RE * Math.Cos(Lat0);
            along = n * Math.Cos(Hdg) + e * Math.Sin(Hdg);
            cross = -n * Math.Sin(Hdg) + e * Math.Cos(Hdg);
        }

        public bool TryHeight(double latRad, double lonRad, out double h)
        {
            ToLocal(latRad, lonRad, out double a, out double c);
            h = 0;
            double margin = RunwayBuilder.Overrun;
            if (a < -margin || a > Length + margin || Math.Abs(c) > Width / 2 + RunwayBuilder.Shoulder) return false;
            h = HeightAt(a);
            return true;
        }

        public double HeightAt(double along)
        {
            double t = Math.Max(0, Math.Min(1, along / Length)) * (Profile.Length - 1);
            int i = Math.Min((int)t, Profile.Length - 2);
            return Profile[i] + (Profile[i + 1] - Profile[i]) * (t - i);
        }
    }

    public static class RunwayBuilder
    {
        public const double RE = 6371000, Shoulder = 12, Overrun = 60;
        const int Stations = 61;
        const double D2R = Math.PI / 180, R2D = 180 / Math.PI;

        static readonly Dictionary<char, string> Digits = new Dictionary<char, string>
        {
            ['0'] = "abcdef", ['1'] = "bc", ['2'] = "abged", ['3'] = "abgcd", ['4'] = "fgbc", ['5'] = "afgcd", ['6'] = "afgedc", ['7'] = "abc", ['8'] = "abcdefg", ['9'] = "abcdfg",
        };

        public static async Task<BuiltRunway> Build(Cesium3DTileset terrain, CesiumGeoreference geo, RunwayData rw, double fallback)
        {
            double latA = rw.cabeceira_a.lat * D2R, lonA = rw.cabeceira_a.lon * D2R, latB = rw.cabeceira_b.lat * D2R, lonB = rw.cabeceira_b.lon * D2R;
            double n = (latB - latA) * RE, e = (lonB - lonA) * RE * Math.Cos(latA);
            double L = Math.Sqrt(n * n + e * e), hdg = Math.Atan2(e, n), W = rw.largura_m > 5 ? rw.largura_m : 45;
            var r = new BuiltRunway { Data = rw, Lat0 = latA, Lon0 = lonA, Hdg = hdg, Length = L, Width = W };

            double cl = Math.Cos(latA);
            var points = new double3[Stations * 3];
            for (int i = 0; i < Stations; i++)
            {
                double a = L * i / (Stations - 1);
                for (int k = 0; k < 3; k++)
                {
                    double c = (k - 1) * (W / 2 + Shoulder);
                    double dn = a * Math.Cos(hdg) - c * Math.Sin(hdg), de = a * Math.Sin(hdg) + c * Math.Cos(hdg);
                    points[i * 3 + k] = new double3((lonA + de / (RE * cl)) * R2D, (latA + dn / RE) * R2D, 0);
                }
            }
            var raw = new double[Stations];
            for (int i = 0; i < Stations; i++) raw[i] = double.NegativeInfinity;
            try
            {
                var res = await terrain.SampleHeightMostDetailed(points);
                for (int i = 0; i < points.Length; i++) if (res.sampleSuccess[i]) raw[i / 3] = Math.Max(raw[i / 3], res.longitudeLatitudeHeightPositions[i].z);
            }
            catch (Exception ex) { Debug.LogWarning("[Pista] Amostragem falhou: " + ex.Message); }
            for (int i = 0; i < Stations; i++) if (double.IsInfinity(raw[i])) raw[i] = fallback;

            const int span = 4;
            var env = new double[Stations];
            for (int i = 0; i < Stations; i++)
            {
                double m = double.NegativeInfinity;
                for (int j = Math.Max(0, i - span); j <= Math.Min(Stations - 1, i + span); j++) m = Math.Max(m, raw[j]);
                env[i] = m;
            }
            r.Profile = new double[Stations];
            for (int i = 0; i < Stations; i++)
            {
                double s = 0; int c = 0;
                for (int j = Math.Max(0, i - span); j <= Math.Min(Stations - 1, i + span); j++) { s += env[j]; c++; }
                r.Profile[i] = s / c + 0.4;
            }

            var go = new GameObject("Pista " + rw.ident);
            go.transform.SetParent(geo.transform, false);
            double midLat = latA + L / 2 * Math.Cos(hdg) / RE, midLon = lonA + L / 2 * Math.Sin(hdg) / (RE * cl), midH = r.HeightAt(L / 2);
            var anchor = go.AddComponent<CesiumGlobeAnchor>();
            anchor.adjustOrientationForGlobeWhenMoving = false;
            anchor.longitudeLatitudeHeight = new double3(midLon * R2D, midLat * R2D, midH);
            anchor.rotationEastUpNorth = Quaternion.Euler(0, (float)(hdg * R2D), 0);
            anchor.detectTransformChanges = false;
            go.AddComponent<MeshFilter>().sharedMesh = BuildMesh(r, midH);
            var mr = go.AddComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("HDRP/Lit"));
            mat.SetTexture("_BaseColorMap", BuildTexture(rw, L, W));
            mat.SetFloat("_Smoothness", 0.15f);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return r;
        }

        static Mesh BuildMesh(BuiltRunway r, double midH)
        {
            double L = r.Length, halfW = r.Width / 2 + Shoulder;
            int extra = 2, rows = Stations + extra * 2;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int i = 0; i < rows; i++)
            {
                double a = i < extra ? -Overrun * (extra - i) / extra : i >= Stations + extra ? L + Overrun * (i - Stations - extra + 1) / extra : L * (i - extra) / (Stations - 1);
                double dz = a - L / 2, h = r.HeightAt(a) - midH - dz * dz / (2 * RE);
                float v = (float)(a / L);
                float u0 = (float)(-Shoulder / r.Width), u1 = (float)(1 + Shoulder / r.Width);
                verts.Add(new Vector3((float)-halfW, (float)h - 25f, (float)dz)); uvs.Add(new Vector2(u0, v));
                verts.Add(new Vector3((float)-halfW, (float)h, (float)dz)); uvs.Add(new Vector2(u0, v));
                verts.Add(new Vector3((float)halfW, (float)h, (float)dz)); uvs.Add(new Vector2(u1, v));
                verts.Add(new Vector3((float)halfW, (float)h - 25f, (float)dz)); uvs.Add(new Vector2(u1, v));
                if (i == 0) continue;
                int p = (i - 1) * 4, q = i * 4;
                for (int k = 0; k < 3; k++)
                {
                    tris.Add(p + k); tris.Add(q + k); tris.Add(q + k + 1);
                    tris.Add(p + k); tris.Add(q + k + 1); tris.Add(p + k + 1);
                }
            }
            var mesh = new Mesh { name = "PistaMalha", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Texture2D BuildTexture(RunwayData rw, double L, double W)
        {
            const int TW = 192, TH = 4096;
            var px = new Color32[TW * TH];
            var rnd = new System.Random(rw.ident.GetHashCode());
            for (int i = 0; i < px.Length; i++)
            {
                byte g = (byte)(46 + rnd.Next(0, 12));
                px[i] = new Color32(g, g, (byte)(g + 2), 255);
            }
            int PxU(double m) => (int)(m / W * TW);
            int PxV(double m) => (int)(m / L * TH);
            void Fill(double uA, double uB, double vA, double vB, Color32 c)
            {
                int x0 = Mathf.Clamp(PxU(uA + W / 2), 0, TW - 1), x1 = Mathf.Clamp(Mathf.Max(PxU(uB + W / 2), x0 + 1), 0, TW);
                int y0 = Mathf.Clamp(PxV(vA), 0, TH - 1), y1 = Mathf.Clamp(Mathf.Max(PxV(vB), y0 + 1), 0, TH);
                for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++) px[y * TW + x] = c;
            }
            var white = new Color32(232, 232, 228, 255);
            Fill(-W / 2 + 0.6, -W / 2 + 1.5, 0, L, white);
            Fill(W / 2 - 1.5, W / 2 - 0.6, 0, L, white);
            for (double a = 70; a + 30 < L - 70; a += 50) Fill(-0.6, 0.6, a, a + 30, white);
            string[] ids = (rw.ident ?? "00/00").Split('/');
            for (int end = 0; end < 2; end++)
            {
                void EndRect(double a0, double b0, double a1, double b1)
                {
                    if (end == 0) Fill(Math.Min(b0, b1), Math.Max(b0, b1), a0, a1, white);
                    else Fill(Math.Min(-b0, -b1), Math.Max(-b0, -b1), L - a1, L - a0, white);
                }
                int stripes = W >= 45 ? 12 : W >= 30 ? 8 : 6;
                double step = (W - 5) / (stripes - 1);
                for (int i = 0; i < stripes; i++) { double b = -(W - 5) / 2 + i * step; EndRect(6, b - 0.9, 36, b + 0.9); }
                string num = end < ids.Length ? ids[end] : "";
                double dw = 4.5, dh = 12, dt = 1.4, total = num.Length * dw + (num.Length - 1) * 2;
                for (int k = 0; k < num.Length; k++)
                {
                    if (!Digits.TryGetValue(num[k], out string segs)) continue;
                    double x0 = -total / 2 + k * (dw + 2);
                    foreach (char s in segs)
                    {
                        double[] q;
                        switch (s)
                        {
                            case 'a': q = new[] { 0, dh - dt, dw, dh }; break;
                            case 'b': q = new[] { dw - dt, dh / 2, dw, dh }; break;
                            case 'c': q = new[] { dw - dt, 0, dw, dh / 2 }; break;
                            case 'd': q = new[] { 0, 0, dw, dt }; break;
                            case 'e': q = new[] { 0, 0, dt, dh / 2 }; break;
                            case 'f': q = new[] { 0, dh / 2, dt, dh }; break;
                            default: q = new[] { 0, dh / 2 - dt / 2, dw, dh / 2 + dt / 2 }; break;
                        }
                        EndRect(46 + q[1], x0 + q[0], 46 + q[3], x0 + q[2]);
                    }
                }
                double aim = Math.Min(300, L * 0.25);
                foreach (int sg in new[] { -1, 1 })
                {
                    EndRect(aim, sg * W * 0.17, aim + 45, sg * (W * 0.17 + 4));
                    EndRect(150, sg * 7, 172, sg * 9);
                    if (L > 1200) EndRect(450, sg * 7, 472, sg * 9);
                }
            }
            var tex = new Texture2D(TW, TH, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, anisoLevel = 16, filterMode = FilterMode.Trilinear };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }
    }
}
