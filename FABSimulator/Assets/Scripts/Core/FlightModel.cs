using System;

namespace FABSim.Core
{
    public struct V3
    {
        public double X, Y, Z;
        public V3(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static V3 operator +(V3 a, V3 b) => new V3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static V3 operator -(V3 a, V3 b) => new V3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static V3 operator *(V3 a, double k) => new V3(a.X * k, a.Y * k, a.Z * k);
        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
        public static V3 Cross(V3 a, V3 b) => new V3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
    }

    public struct Quat
    {
        public double W, X, Y, Z;
        public Quat(double w, double x, double y, double z) { W = w; X = x; Y = y; Z = z; }
    }

    public struct Mat3
    {
        public double M00, M01, M02, M10, M11, M12, M20, M21, M22;
        public static Mat3 From(Quat q)
        {
            double w = q.W, x = q.X, y = q.Y, z = q.Z;
            return new Mat3
            {
                M00 = 1 - 2 * (y * y + z * z), M01 = 2 * (x * y - w * z), M02 = 2 * (x * z + w * y),
                M10 = 2 * (x * y + w * z), M11 = 1 - 2 * (x * x + z * z), M12 = 2 * (y * z - w * x),
                M20 = 2 * (x * z - w * y), M21 = 2 * (y * z + w * x), M22 = 1 - 2 * (x * x + y * y),
            };
        }
        public V3 Mul(V3 v) => new V3(M00 * v.X + M01 * v.Y + M02 * v.Z, M10 * v.X + M11 * v.Y + M12 * v.Z, M20 * v.X + M21 * v.Y + M22 * v.Z);
        public V3 MulT(V3 v) => new V3(M00 * v.X + M10 * v.Y + M20 * v.Z, M01 * v.X + M11 * v.Y + M21 * v.Z, M02 * v.X + M12 * v.Y + M22 * v.Z);
    }

    public sealed class Env
    {
        public double Wn, We, Sigma, DT, K = 1;
    }

    public struct Atm
    {
        public double Rho, A, T;
    }

    public enum EngineType { Piston, Jet }
    public enum ControlLaw { Direct, Fbw }

    public sealed class EngineDef
    {
        public EngineType Type = EngineType.Jet;
        public bool Afterburner, Turbo;
        public double T0, TAB, Sx, Km, Idle, Fi, Tsfc, TsfcAB;
        public double P, Eta, V0, Bsfc;
        public double Tau, Dz;
        public string NLabel = "N1";
    }

    public sealed class GearPoint
    {
        public V3 P;
        public double Share, Steer;
        public bool Brake;
        public double K, C, N0;
    }

    public sealed class CrashPoint
    {
        public V3 P;
        public bool Tail;
        public CrashPoint(double x, double y, double z, bool tail = false) { P = new V3(x, y, z); Tail = tail; }
    }

    public sealed class StallDef
    {
        public double Wake = 0.45, Qd = 1, Knee = 0.09, W = 0.03, On = 0.2, Off = 0.5, Cm0 = 0.2, Cm1 = 1.5, Rot = 1.15, Rb = 1.5, Ail = 0.6, Cd = 1.6, Bias = 0.03, Warn = 0.045;
    }

    public sealed class WaveDef
    {
        public bool Sup;
        public double Crit, Peak, Post, Mdd, K;
    }

    public sealed class FbwDef
    {
        public double NzMax, NzMin, AlphaLim, PMax, Lq, Lp, Lr, NzFeedback, RollGLimit;
    }

    public sealed class AircraftDef
    {
        public string Id, Name;
        public double Mass0, FuelMax, S, B, C, Ix, Iy, Iz;
        public double CL0, CLa, AStall;
        public double CD0, K, DCLf, DCDf, DCmf, DAsf, FlapExp = 1, DCDgear, DCDsb, DCLsb;
        public double Cm0, Cma, Cmq, Cmde;
        public double Clb, Clp, Clr, Clda, Cldr;
        public double Cnb, Cnr, Cnp, Cndr, Cnda;
        public double CYb, CYdr;
        public double TrimAuth = 0.5;
        public ControlLaw Control = ControlLaw.Direct;
        public FbwDef Fbw;
        public double[] FlapNotches = { 0 };
        public double FlapRate = 1, GearRate = 0.2;
        public bool GearFixed;
        public EngineDef Engine;
        public GearPoint[] Gear;
        public CrashPoint[] Crash;
        public double GearDefl, GearStroke, GearLoadLimit, CgHeight;
        public double NzLimitLow, NzLimitHigh, VRef;
        public double GainE = 1, GainA = 1, GainR = 1;
        public WaveDef Wave;
        public StallDef Stall = new StallDef();
        public double FlapMax { get; private set; }
        bool ready;

        public AircraftDef Prepare()
        {
            if (ready) return this;
            FlapMax = FlapNotches[FlapNotches.Length - 1];
            double mref = Mass0 + FuelMax * 0.5;
            foreach (var g in Gear)
            {
                g.K = g.Share * mref * FlightModel.G / GearDefl;
                g.C = 2 * 0.35 * Math.Sqrt(g.K * g.Share * mref);
                g.N0 = g.Share * mref * FlightModel.G;
            }
            ready = true;
            return this;
        }
    }

    public sealed class Controls
    {
        public double Pitch, Roll, Yaw, Throttle, Trim, Brake, Speedbrake;
        public bool Parking, GearDown = true, Assist, StallProt;
        public int FlapIdx;
    }

    public sealed class Diag
    {
        public double Alpha, Beta, Ias, Tas, Mach, Qbar, Nz = 1, Ay, Gs, Hdg, Pitch, Roll, Vsi, Turn, Agl, NPct, Mass, Wkt, Wdir, Buf, Sep;
        public bool StallWarn;
    }

    public sealed class FlightState
    {
        public double Lat, Lon, Z;
        public V3 V, W;
        public Quat Q;
        public double StoreMass, Fuel, N, Ab, Flaps, Gear = 1, Sb, De, Da, Dr, Att = 1, StallDir, Buf, Thrust, Flow, OverG, Trim, Throttle;
        public V3 Bn, Turb;
        public bool Contact;
        public string Crashed = "";
        public Env Env;
        public Diag Diag = new Diag();
    }

    public static class FlightModel
    {
        public const double G = 9.80665, RE = 6371000, KT = 1.943844, FT = 3.28084;
        const double Deg = 180 / Math.PI;
        public static Func<double> Rand = DefaultRand;
        static readonly Random rng = new Random();
        static double DefaultRand() => rng.NextDouble();
        static readonly Env NoEnv = new Env();

        public static double Clamp(double x, double a, double b) => x < a ? a : x > b ? b : x;
        static double Smooth(double a, double b, double x) { double t = Clamp((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t); }
        static double Randn() => Rand() + Rand() + Rand() - 1.5;
        static double Hypot(double a, double b) => Math.Sqrt(a * a + b * b);
        static double SignOr1(double x) => x > 0 ? 1 : x < 0 ? -1 : 1;

        public static Atm Isa(double h, Env env = null)
        {
            env = env ?? NoEnv;
            double hh = Clamp(h, -500, 25000);
            double T = hh < 11000 ? 288.15 - 0.0065 * hh : 216.65;
            double rho = hh < 11000 ? 1.225 * Math.Pow(T / 288.15, 4.2559) : 0.3639 * Math.Exp(-(hh - 11000) / 6341.6);
            if (env.DT != 0 || env.K != 1) { rho *= (T / (T + env.DT)) * env.K; T += env.DT; }
            return new Atm { Rho = rho, A = Math.Sqrt(401.87 * T), T = T };
        }

        public static void WindAt(Env env, double hAgl, out double wn, out double we)
        {
            double f = Math.Pow(Clamp(hAgl, 2, 300) / 10, 0.14);
            wn = env.Wn * f; we = env.We * f;
        }

        static double LiftSlope(double M)
        {
            double sub = 1 + 0.5 * (1 / Math.Sqrt(1 - Math.Pow(Math.Min(M, 0.85), 2)) - 1);
            if (M < 0.85) return sub;
            double sup = Clamp(1.05 / Math.Sqrt(Math.Max(M * M - 1, 0.01)), 0.55, 1.1);
            return sub + (sup - sub) * Smooth(0.85, 1.2, M);
        }

        public static Quat QMul(Quat a, Quat b) => new Quat(
            a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z,
            a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
            a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
            a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W);

        public static Quat QFromEuler(double phi, double theta, double psi)
        {
            double cr = Math.Cos(phi / 2), sr = Math.Sin(phi / 2), cp = Math.Cos(theta / 2), sp = Math.Sin(theta / 2), cy = Math.Cos(psi / 2), sy = Math.Sin(psi / 2);
            return new Quat(cr * cp * cy + sr * sp * sy, sr * cp * cy - cr * sp * sy, cr * sp * cy + sr * cp * sy, cr * cp * sy - sr * sp * cy);
        }

        public static void Euler(Quat q, out double phi, out double theta, out double psi)
        {
            phi = Math.Atan2(2 * (q.W * q.X + q.Y * q.Z), 1 - 2 * (q.X * q.X + q.Y * q.Y));
            theta = Math.Asin(Clamp(2 * (q.W * q.Y - q.Z * q.X), -1, 1));
            psi = Math.Atan2(2 * (q.W * q.Z + q.X * q.Y), 1 - 2 * (q.Y * q.Y + q.Z * q.Z));
        }

        static void ThrustCalc(AircraftDef ac, double n, double ab, Atm atm, double V, bool hasFuel, out double T, out double flow)
        {
            var e = ac.Engine;
            double sigma = atm.Rho / 1.225;
            T = 0; flow = 0;
            if (!hasFuel) return;
            if (e.Type == EngineType.Piston)
            {
                double P = e.P * (0.04 + 0.96 * n) * (e.Turbo ? Math.Pow(sigma, 0.85) : sigma - (1 - sigma) / 7.55);
                T = Math.Max(0, e.Eta * P / (V + e.V0));
                flow = e.Bsfc * Math.Max(P, 0) / 3.6e6;
                return;
            }
            double M = V / atm.A;
            double sg = Math.Pow(sigma, e.Sx) * Math.Max(0.3, 1 + e.Km * M);
            double tf = Clamp((n - e.Idle) / (1 - e.Idle), 0, 1);
            double dry = e.T0 * (e.Fi + (1 - e.Fi) * Math.Pow(tf, 1.4)) * sg;
            flow = dry * e.Tsfc;
            T = dry;
            if (e.Afterburner && ab > 0)
            {
                double extra = (e.TAB - e.T0) * sg * ab;
                T += extra;
                flow += extra * e.TsfcAB;
            }
        }

        static FlightState NewState(AircraftDef ac, double lat, double lon, double z, double hdg, double fuelFrac, Env env)
        {
            ac.Prepare();
            return new FlightState
            {
                Lat = lat, Lon = lon, Z = z, Q = QFromEuler(0, 0, hdg),
                Fuel = ac.FuelMax * fuelFrac,
                N = ac.Engine.Type == EngineType.Piston ? 0 : ac.Engine.Idle,
                Env = env ?? NoEnv,
            };
        }

        public static FlightState SpawnGround(AircraftDef ac, double lat, double lon, double groundZ, double hdg, double fuelFrac = 0.5, Env env = null)
        {
            var s = NewState(ac, lat, lon, groundZ + ac.CgHeight, hdg, fuelFrac, env);
            s.Contact = true;
            return s;
        }

        public static FlightState SpawnAir(AircraftDef ac, double lat, double lon, double z, double groundZ, double hdg, double V, double fuelFrac = 0.5, Env env = null)
        {
            var s = NewState(ac, lat, lon, z, hdg, fuelFrac, env);
            var atm = Isa(z, s.Env);
            double m = ac.Mass0 + s.Fuel + s.StoreMass;
            double qS = 0.5 * atm.Rho * V * V * ac.S;
            s.Gear = ac.GearFixed ? 1 : 0;
            s.Flaps = 0;
            double alpha = (m * G / qS - ac.CL0) / ac.CLa;
            s.Q = QFromEuler(0, alpha, hdg);
            WindAt(s.Env, z - groundZ, out double wn, out double we);
            s.V = new V3(V * Math.Cos(hdg) + wn, V * Math.Sin(hdg) + we, 0);
            s.Trim = 0;
            if (ac.Control == ControlLaw.Direct)
            {
                double cm = ac.Cm0 + ac.Cma * alpha;
                s.Trim = Clamp(-cm / ac.Cmde / ac.TrimAuth, -1, 1);
            }
            double CD = ac.CD0 + ac.K * Math.Pow(ac.CL0 + ac.CLa * alpha, 2);
            double D = qS * CD;
            var e = ac.Engine;
            double lo = 0, hi = 1;
            for (int i = 0; i < 30; i++)
            {
                double mid = (lo + hi) / 2;
                double nn = e.Type == EngineType.Piston ? mid : e.Idle + (1 - e.Idle) * Clamp(mid / (e.Afterburner ? 0.88 : 1), 0, 1);
                ThrustCalc(ac, nn, 0, atm, V, true, out double T, out _);
                if (T < D) lo = mid; else hi = mid;
            }
            s.Throttle = hi;
            s.N = e.Type == EngineType.Piston ? hi : e.Idle + (1 - e.Idle) * Clamp(hi / (e.Afterburner ? 0.88 : 1), 0, 1);
            s.Contact = false;
            return s;
        }

        public static void Step(FlightState s, AircraftDef ac, Controls ctl, double dt, double groundZ)
        {
            if (s.Crashed.Length > 0) return;
            double m = ac.Mass0 + s.Fuel + s.StoreMass;
            var env = s.Env;
            var atm = Isa(s.Z, env);
            double hw = s.Z - groundZ;
            WindAt(env, hw, out double wmN, out double wmE);
            if (env.Sigma > 0)
            {
                double kt = Math.Exp(-dt / 2.5), sg = env.Sigma * (1 + 0.4 * Math.Exp(-Math.Max(hw, 0) / 150)) * Math.Sqrt(1 - kt * kt) * 2;
                s.Turb.X = s.Turb.X * kt + sg * Randn();
                s.Turb.Y = s.Turb.Y * kt + sg * Randn();
                s.Turb.Z = s.Turb.Z * kt + sg * 0.6 * Randn();
            }
            var wind = new V3(wmN + s.Turb.X, wmE + s.Turb.Y, s.Turb.Z);
            var R = Mat3.From(s.Q);
            var vb = R.MulT(s.V - wind);
            double V = vb.Length;
            double alpha = V > 1 ? Math.Atan2(vb.Z, vb.X) : 0;
            double beta = V > 1 ? Math.Asin(Clamp(vb.Y / V, -1, 1)) : 0;
            double Vn = Math.Max(V, 12);
            double qbar = 0.5 * atm.Rho * V * V;
            double qS = qbar * ac.S;
            double qbEff = Math.Max(qbar, 400);
            double M = V / atm.A, kM = LiftSlope(M), sup = ac.Wave != null && ac.Wave.Sup ? Smooth(0.9, 1.3, M) : 0;
            double Cma = ac.Cma * (1 + sup), Cmde = ac.Cmde * (1 - 0.3 * Smooth(1, 1.6, M));
            double p = s.W.X, q = s.W.Y, r = s.W.Z;
            double ph = p * ac.B / (2 * Vn), qh = q * ac.C / (2 * Vn), rh = r * ac.B / (2 * Vn);
            Euler(s.Q, out double ePhi, out double eTheta, out double ePsi);
            double f = s.Flaps / ac.FlapMax;

            double flapTarget = ac.FlapNotches[ctl.FlapIdx];
            s.Flaps += Clamp(flapTarget - s.Flaps, -ac.FlapRate * dt, ac.FlapRate * dt);
            if (!ac.GearFixed)
            {
                double gTarget = ctl.GearDown || s.Contact ? 1 : 0;
                s.Gear += Clamp(gTarget - s.Gear, -ac.GearRate * dt, ac.GearRate * dt);
            }
            s.Sb += Clamp(ctl.Speedbrake - s.Sb, -dt, dt);

            var st = ac.Stall;
            double aAbs = Math.Abs(alpha), sgnA = alpha < 0 ? -1 : 1;
            double a0 = ac.AStall + ac.DAsf * f;
            double x0 = 0.5 * (1 - Math.Tanh((aAbs - a0 - 0.01) / st.W));
            s.Att += (x0 - s.Att) * (1 - Math.Exp(-dt / (x0 < s.Att ? st.On : st.Off)));
            double sep = 1 - s.Att;
            if (sep > 0.15 && s.StallDir == 0) s.StallDir = SignOr1(ctl.Roll * 0.4 + ctl.Yaw * 0.8 - beta * 3 + (Rand() - 0.5) * 0.5);
            else if (sep < 0.03) s.StallDir = 0;
            double buf = V > 8 && !s.Contact ? Clamp(Math.Max((aAbs - a0 + 0.1) / 0.1, sep), 0, 1) : 0;
            s.Buf = buf;
            double kbn = 1 - Math.Exp(-dt * 10);
            s.Bn.X += (Rand() * 2 - 1 - s.Bn.X) * kbn;
            s.Bn.Y += (Rand() * 2 - 1 - s.Bn.Y) * kbn;
            s.Bn.Z += (Rand() * 2 - 1 - s.Bn.Z) * kbn;
            double Clda = ac.Clda * (1 - st.Ail * sep);

            double cm0 = ac.Cm0 + ac.DCmf * f + Cma * alpha + ac.Cmq * (1 + st.Qd * sep) * qh - sep * (st.Cm0 * sgnA + st.Cm1 * alpha);
            double cl0 = ac.Clb * (1 - st.Rb * sep) * beta + ac.Clp * (1 - st.Rot * sep) * ph + ac.Clr * rh + sep * st.Bias * s.StallDir + 0.1 * buf * s.Bn.X;
            double cn0 = ac.Cnb * (1 - 0.6 * sep) * beta + ac.Cnr * rh + ac.Cnp * ph + 0.02 * buf * s.Bn.Z;
            double Vc = Math.Max(V, 55);

            double YawLaw(double lr, double daC)
            {
                double rCmd = G / Vc * Math.Sin(ePhi) * Math.Cos(eTheta) + p * alpha;
                double bCmd = -ctl.Yaw * 0.15;
                double rdot = lr * (rCmd - r) + (ac.Control == ControlLaw.Fbw ? 2 : 6) * (beta - bCmd);
                return Clamp((ac.Iz * rdot / (qbEff * ac.S * ac.B) - cn0 - ac.Cnda * daC) / ac.Cndr, -1, 1);
            }

            double deC, daCmd, drC;
            if (ac.Control == ControlLaw.Fbw)
            {
                var F = ac.Fbw;
                double nzUp = ctl.StallProt ? Math.Max(1, Math.Min(F.NzMax, qS * (ac.CL0 + ac.DCLf * f + ac.CLa * kM * F.AlphaLim) / (m * G))) : F.NzMax;
                double nzDn = ctl.StallProt ? Clamp(qS * (ac.CL0 + ac.DCLf * f - ac.CLa * kM * 0.35) / (m * G), F.NzMin, 1) : F.NzMin;
                double nzCmd = ctl.Pitch >= 0 ? 1 + ctl.Pitch * (nzUp - 1) : 1 + ctl.Pitch * (1 - nzDn);
                double nzErr = s.Contact ? 0 : nzCmd - s.Diag.Nz;
                double qCmd = G * (nzCmd - Math.Cos(eTheta) * Math.Cos(ePhi) + F.NzFeedback * nzErr) / Vc + p * Math.Tan(beta);
                deC = Clamp((ac.Iy * F.Lq * (qCmd - q) / (qbEff * ac.S * ac.C) - cm0) / Cmde, -1, 1);
                double pCmd = ctl.Roll * F.PMax * Clamp(V / 120, 0.3, 1) * (1 - F.RollGLimit * Clamp(Math.Abs(s.Diag.Nz - 1) / (F.NzMax - 1), 0, 1));
                daCmd = Clamp((ac.Ix * F.Lp * (pCmd - p) / (qbEff * ac.S * ac.B) - cl0) / Clda, -1, 1);
                drC = s.Contact || qbar < 600 ? ctl.Yaw : YawLaw(F.Lr, daCmd);
            }
            else
            {
                double ka = Clamp(Math.Pow(ac.VRef / Math.Max(V, 10), 2), 0.25, 1);
                deC = Clamp(ctl.Pitch * ka * ac.GainE + ctl.Trim * ac.TrimAuth, -1, 1);
                daCmd = ctl.Roll * ka * ac.GainA;
                drC = ctl.Assist && !s.Contact && qbar > 600 ? YawLaw(4, daCmd) : ctl.Yaw * (s.Contact ? 1 : ka * ac.GainR);
            }
            double Kd(double x, double c, double tau) => x + (c - x) * (1 - Math.Exp(-dt / tau));
            s.De = Kd(s.De, deC, 0.07);
            s.Da = Kd(s.Da, daCmd, 0.07);
            s.Dr = Kd(s.Dr, drC, 0.08);

            double dCLf = ac.DCLf * Math.Pow(f, ac.FlapExp);
            double kn = a0 - st.Knee, alin = aAbs > kn ? sgnA * (kn + 0.6 * (aAbs - kn)) : alpha;
            double CLlin = ac.CL0 + dCLf + ac.CLa * kM * alin + ac.DCLsb * s.Sb;
            double sa = Math.Sin(alpha), ca = Math.Cos(alpha);
            double wfp = Clamp((aAbs - a0 - 0.25) / 0.35, 0, 1);
            double CLs = CLlin * Math.Pow((1 + Math.Sqrt(s.Att)) / 2, 2);
            double CL = (1 - wfp) * CLs + wfp * 0.8 * Math.Sin(2 * alpha);

            double wave = 0;
            var wv = ac.Wave;
            if (wv != null && wv.Sup) wave = wv.Peak * Math.Exp(-Math.Pow((M - 1.05) / 0.12, 2)) + wv.Post * Clamp((M - wv.Crit) / 0.3, 0, 1);
            else if (wv != null && M > wv.Mdd) wave = wv.K * Math.Pow(M - wv.Mdd, 4);
            double hAgl = s.Z - groundZ;
            double gh = Math.Max(hAgl - 1, 0.5) * 16 / ac.B;
            double geF = gh * gh / (1 + gh * gh);
            double CDlin = ac.CD0 + ac.DCDf * f * f + ac.DCDgear * s.Gear + ac.DCDsb * s.Sb + wave + ac.K * geF * CLs * CLs;
            double CD = CDlin + sep * st.Cd * sa * sa;
            double CY = ac.CYb * beta + ac.CYdr * s.Dr;

            double L = qS * CL, D = qS * CD, Y = qS * CY;
            double Fx = -D * ca + L * sa, Fy = Y, Fz = -D * sa - L * ca;

            var e = ac.Engine;
            bool hasFuel = s.Fuel > 0;
            double nCmd, abCmd = 0;
            if (e.Type == EngineType.Piston) nCmd = hasFuel ? ctl.Throttle : 0;
            else
            {
                nCmd = hasFuel ? e.Idle + (1 - e.Idle) * Clamp(ctl.Throttle / (e.Afterburner ? 0.88 : 1), 0, 1) : 0;
                if (e.Afterburner && hasFuel) abCmd = Clamp((ctl.Throttle - 0.9) / 0.08, 0, 1);
            }
            s.N = Kd(s.N, nCmd, e.Tau);
            s.Ab = Kd(s.Ab, abCmd, 0.8);
            ThrustCalc(ac, s.N, s.Ab, atm, V, hasFuel, out double thT, out double thFlow);
            s.Thrust = thT;
            s.Flow = thFlow;
            s.Fuel = Math.Max(0, s.Fuel - thFlow * dt);
            Fx += thT;

            double Mx = qS * ac.B * (cl0 + Clda * s.Da + ac.Cldr * s.Dr);
            double My = qS * ac.C * (cm0 + Cmde * (1 - st.Wake * sep) * s.De + 0.07 * buf * s.Bn.Y) + thT * e.Dz;
            double Mz = qS * ac.B * (cn0 + ac.Cndr * s.Dr + ac.Cnda * s.Da);
            if (e.Type == EngineType.Piston) Mz -= 0.1 * thT / (1 + V / 40);

            var gravB = R.MulT(new V3(0, 0, m * G));
            Fx += gravB.X; Fy += gravB.Y; Fz += gravB.Z;
            var nonGrav = new V3(Fx - gravB.X, Fy - gravB.Y, Fz - gravB.Z);

            bool contact = false;
            string crash = "";
            double brake = Math.Max(ctl.Brake, ctl.Parking ? 1 : 0);
            bool wheelsOn = ac.GearFixed || s.Gear > 0.95;

            void Apply(V3 rb, V3 Fned)
            {
                var Fb = R.MulT(Fned);
                Fx += Fb.X; Fy += Fb.Y; Fz += Fb.Z;
                nonGrav += Fb;
                var mm = V3.Cross(rb, Fb);
                Mx += mm.X; My += mm.Y; Mz += mm.Z;
            }

            void ContactWheel(V3 rb, double k, double c, double brk, double steer, double N0, double stroke)
            {
                var rn = R.Mul(rb);
                double depth = groundZ - (s.Z - rn.Z);
                if (depth <= 0) return;
                var vp = s.V + R.Mul(V3.Cross(s.W, rb));
                double N = k * depth * (1 + (stroke != 0 ? 2 * Math.Pow(depth / stroke, 2) : 0)) + c * vp.Z;
                if (N <= 0) return;
                double dst = steer * Math.PI / 180;
                var fn = R.Mul(new V3(Math.Cos(dst), Math.Sin(dst), 0));
                double fh = Hypot(fn.X, fn.Y);
                if (fh == 0) fh = 1;
                double fx = fn.X / fh, fy = fn.Y / fh, lx = -fy, ly = fx;
                double vf = vp.X * fx + vp.Y * fy, vl = vp.X * lx + vp.Y * ly;
                double Ff = -N * (0.02 + 0.55 * brk) * Math.Tanh(vf / 0.15);
                double Fl = -N * 0.9 * Math.Tanh(vl / 0.25);
                Apply(rb, new V3(Ff * fx + Fl * lx, Ff * fy + Fl * ly, -N));
                contact = true;
                if (N0 != 0 && N / N0 > ac.GearLoadLimit) crash = "Trem de pouso colapsou no impacto";
            }

            void ContactHull(V3 rb, double k, double c, double mu)
            {
                var rn = R.Mul(rb);
                double depth = groundZ - (s.Z - rn.Z);
                if (depth <= 0) return;
                var vp = s.V + R.Mul(V3.Cross(s.W, rb));
                double N = k * depth + c * vp.Z;
                if (N <= 0) return;
                double sp = Hypot(vp.X, vp.Y);
                double Ff = sp > 1e-3 ? -N * mu * Math.Tanh(sp / 0.5) * vp.X / sp : 0;
                double Fl = sp > 1e-3 ? -N * mu * Math.Tanh(sp / 0.5) * vp.Y / sp : 0;
                Apply(rb, new V3(Ff, Fl, -N));
            }

            if (wheelsOn)
            {
                foreach (var g in ac.Gear)
                {
                    double steer = g.Steer != 0 ? ctl.Yaw * g.Steer / (1 + Hypot(s.V.X, s.V.Y) / 12) : 0;
                    ContactWheel(g.P, g.K, g.C, g.Brake ? brake : 0, steer, g.N0, ac.GearStroke);
                }
            }
            double kc = m * G / 0.03, cc = Math.Sqrt(kc * m);
            foreach (var pt in ac.Crash)
            {
                var rn = R.Mul(pt.P);
                double depth = groundZ - (s.Z - rn.Z);
                if (depth <= 0.02) continue;
                double spd = Hypot(s.V.X, s.V.Y);
                double vdown = s.V.Z + R.Mul(V3.Cross(s.W, pt.P)).Z;
                if (pt.Tail ? depth > 0.4 || vdown > 3 : spd > 12 || vdown > 3 || depth > 0.3) crash = "Colisão com o solo";
                ContactHull(pt.P, kc, cc, 0.5);
            }

            s.Contact = contact;
            if (crash.Length > 0) s.Crashed = crash;

            var acc = R.Mul(new V3(Fx / m, Fy / m, Fz / m));
            s.V += acc * dt;

            double dp = (Mx + (ac.Iy - ac.Iz) * q * r) / ac.Ix;
            double dq = (My + (ac.Iz - ac.Ix) * p * r) / ac.Iy;
            double dr = (Mz + (ac.Ix - ac.Iy) * p * q) / ac.Iz;
            s.W += new V3(dp, dq, dr) * dt;

            double wm = s.W.Length;
            if (wm > 1e-9)
            {
                double h = wm * dt / 2, kq = Math.Sin(h) / wm;
                var nq = QMul(s.Q, new Quat(Math.Cos(h), s.W.X * kq, s.W.Y * kq, s.W.Z * kq));
                double nm = Math.Sqrt(nq.W * nq.W + nq.X * nq.X + nq.Y * nq.Y + nq.Z * nq.Z);
                s.Q = new Quat(nq.W / nm, nq.X / nm, nq.Y / nm, nq.Z / nm);
            }

            s.Lat += s.V.X / (RE + s.Z) * dt;
            s.Lon += s.V.Y / ((RE + s.Z) * Math.Cos(s.Lat)) * dt;
            s.Z -= s.V.Z * dt;

            double sigma = atm.Rho / 1.225;
            double nzNow = -nonGrav.Z / (m * G);
            if (!s.Contact && V > 20 && (nzNow > ac.NzLimitHigh * 1.5 || nzNow < ac.NzLimitLow * 1.5)) s.OverG += dt; else s.OverG = 0;
            if (s.OverG > 0.15 && s.Crashed.Length == 0) s.Crashed = "Estrutura rompeu por sobrecarga de G";

            var d = s.Diag;
            d.Alpha = alpha; d.Beta = beta; d.Ias = V * Math.Sqrt(sigma) * KT; d.Tas = V * KT; d.Mach = M; d.Qbar = qbar;
            d.Nz = nzNow; d.Ay = nonGrav.Y / m; d.Gs = Hypot(s.V.X, s.V.Y);
            d.Hdg = ((ePsi * Deg) % 360 + 360) % 360; d.Pitch = eTheta; d.Roll = ePhi;
            d.Vsi = -s.V.Z * 196.85;
            d.Turn = (q * Math.Sin(ePhi) + r * Math.Cos(ePhi)) / Math.Max(Math.Cos(eTheta), 0.1);
            d.Agl = s.Z - groundZ; d.NPct = s.N * 100; d.Mass = m;
            d.Wkt = Hypot(wmN, wmE) * KT;
            d.Wdir = ((Math.Atan2(-wmE, -wmN) * Deg) % 360 + 360) % 360;
            d.Buf = buf; d.Sep = sep;
            d.StallWarn = !s.Contact && aAbs > a0 - st.Warn;
        }
    }
}
