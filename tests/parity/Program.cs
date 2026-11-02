using System;
using System.Globalization;
using System.Linq;
using FABSim.Core;

static class Program
{
    static GearPoint W(double x, double y, double z, double share, double steer = 0, bool brake = false) => new GearPoint { P = new V3(x, y, z), Share = share, Steer = steer, Brake = brake };

    static AircraftDef Gripen() => new AircraftDef
    {
        Id = "gripen", Mass0 = 7100, FuelMax = 2270, S = 30, B = 8.4, C = 4.5, Ix = 12000, Iy = 95000, Iz = 100000,
        CL0 = 0.05, CLa = 3.4, AStall = 0.6, CD0 = 0.017, K = 0.17, DCLf = 0.25, DCDf = 0.025, DCmf = -0.03, DAsf = 0,
        DCDgear = 0.02, DCDsb = 0.045, DCLsb = 0, Cm0 = 0, Cma = -0.35, Cmq = -6, Cmde = 0.55,
        Clb = -0.06, Clp = -0.25, Clr = 0.05, Clda = 0.14, Cldr = 0.01, Cnb = 0.1, Cnr = -0.25, Cnp = -0.03, Cndr = 0.06, Cnda = -0.005,
        CYb = -0.8, CYdr = 0.25, Control = ControlLaw.Fbw,
        Fbw = new FbwDef { NzMax = 8.5, NzMin = -3, AlphaLim = 0.45, PMax = 4.0, Lq = 6, Lp = 10, Lr = 4 },
        FlapNotches = new double[] { 0, 1, 2 }, FlapRate = 0.6, GearRate = 0.2,
        Engine = new EngineDef { Type = EngineType.Jet, Afterburner = true, T0 = 54e3, TAB = 80e3, Sx = 0.8, Km = 0.45, Idle = 0.3, Fi = 0.04, Tau = 1.2, Tsfc = 22e-6, TsfcAB = 57e-6, Dz = 0 },
        Gear = new[] { W(3.6, 0, 1.5, 0.2, 45), W(-0.9, -1.1, 1.5, 0.4, 0, true), W(-0.9, 1.1, 1.5, 0.4, 0, true) },
        Crash = new[] { new CrashPoint(-5.5, 0, 0.3, true), new CrashPoint(-1, -4.2, 0.4), new CrashPoint(-1, 4.2, 0.4), new CrashPoint(6.5, 0, 0.5), new CrashPoint(-3, 0, 0.5) },
        GearDefl = 0.18, GearStroke = 0.4, GearLoadLimit = 5, CgHeight = 1.32, NzLimitLow = -3, NzLimitHigh = 8.5,
        Wave = new WaveDef { Sup = true, Crit = 0.9, Peak = 0.02, Post = 0.012 },
        Stall = new StallDef { W = 0.05, On = 0.2, Off = 0.3, Cm0 = 0.1, Cm1 = 0.6, Rot = 0.6, Cd = 1.2 },
    };

    static AircraftDef B737() => new AircraftDef
    {
        Id = "b737", Mass0 = 52000, FuelMax = 16700, S = 125, B = 34.3, C = 3.9, Ix = 4.8e6, Iy = 9.0e6, Iz = 1.15e7,
        CL0 = 0.2, CLa = 5.3, AStall = 0.26, CD0 = 0.022, K = 0.042, DCLf = 1.1, DCDf = 0.12, DCmf = -0.08, DAsf = 0, FlapExp = 0.5,
        DCDgear = 0.017, DCDsb = 0.035, DCLsb = -0.25, Cm0 = 0.02, Cma = -1.1, Cmq = -22, Cmde = 1.0,
        Clb = -0.12, Clp = -0.45, Clr = 0.12, Clda = 0.1, Cldr = 0.012, Cnb = 0.12, Cnr = -0.15, Cnp = -0.05, Cndr = 0.06, Cnda = -0.004,
        CYb = -0.7, CYdr = 0.2, FlapNotches = new double[] { 0, 1, 5, 15, 30, 40 }, FlapRate = 1.5, GearRate = 0.12,
        Engine = new EngineDef { Type = EngineType.Jet, T0 = 210e3, Sx = 0.78, Km = -0.5, Idle = 0.22, Fi = 0.05, Tau = 2.8, Tsfc = 17e-6, Dz = 1.0 },
        Gear = new[] { W(14.4, 0, 3.0, 0.08, 55), W(-1.2, -2.86, 3.0, 0.46, 0, true), W(-1.2, 2.86, 3.0, 0.46, 0, true) },
        Crash = new[] { new CrashPoint(-17.2, 0, -0.3, true), new CrashPoint(2.3, -5.75, 2.5), new CrashPoint(2.3, 5.75, 2.5), new CrashPoint(-1, -17.2, -0.2), new CrashPoint(-1, 17.2, -0.2), new CrashPoint(17, 0, 1.2) },
        GearDefl = 0.25, GearStroke = 0.5, GearLoadLimit = 3.6, CgHeight = 2.75, NzLimitLow = -1, NzLimitHigh = 2.5, VRef = 85, GainE = 0.75, GainA = 0.7, GainR = 0.6,
        Wave = new WaveDef { Mdd = 0.78, K = 20 },
        Stall = new StallDef { On = 0.3, Off = 0.7, Cm0 = 0.15, Cm1 = 2.2, Rot = 1.1, Bias = 0.02, Warn = 0.05 },
    };

    static string P(double x) => x.ToString("G12", CultureInfo.InvariantCulture);

    static void Row(string name, double t, FlightState s) => Console.WriteLine(string.Join(",", new[] { name, t.ToString("F2", CultureInfo.InvariantCulture) }.Concat(new[] { s.Lat, s.Lon, s.Z, s.V.X, s.V.Y, s.V.Z, s.Q.W, s.Q.X, s.Q.Y, s.Q.Z, s.W.X, s.W.Y, s.W.Z, s.N, s.Ab, s.Fuel, s.Gear, s.Flaps, s.Crashed.Length > 0 ? 1 : 0 }.Select(P))));

    static void Run(string name, AircraftDef a, FlightState s, Controls c, double gz, Action<double, Controls, FlightState> script)
    {
        const double h = 1.0 / 120;
        for (int i = 0; i <= 120 * 40; i++)
        {
            double t = i * h;
            if (i % 60 == 0) Row(name, t, s);
            script(t, c, s);
            FlightModel.Step(s, a, c, h, gz);
        }
    }

    static void Air(string name, AircraftDef a, double V, double z)
    {
        a.Prepare();
        var s = FlightModel.SpawnAir(a, -0.4, -0.8, z, 0, 1, V, 0.6);
        var c = new Controls { Throttle = s.Throttle, Trim = s.Trim, GearDown = a.GearFixed };
        Run(name, a, s, c, 0, (t, k, _) =>
        {
            k.Pitch = t > 2 && t < 5 ? 0.6 : t > 8 && t < 10 ? -0.4 : 0;
            k.Roll = t > 5 && t < 8 ? 0.7 : t > 12 && t < 14 ? -0.8 : 0;
            k.Yaw = t > 15 && t < 17 ? 0.5 : 0;
            k.Speedbrake = t > 25 && t < 28 ? 1 : 0;
            if (t > 18 && t < 24) k.Throttle = 1;
            if (t >= 24) k.Throttle = 0.5;
        });
    }

    static void Ground(string name, AircraftDef a, double vr)
    {
        a.Prepare();
        var s = FlightModel.SpawnGround(a, -0.4, -0.8, 500, 1, 0.6);
        var c = new Controls { GearDown = true, FlapIdx = 1 };
        s.Flaps = a.FlapNotches[1];
        Run(name, a, s, c, 500, (t, k, st) =>
        {
            k.Throttle = t > 1 ? 1 : 0;
            k.Pitch = st.Diag.Ias > vr && t < 28 ? 0.45 : 0;
            if (t > 26) k.GearDown = false;
            k.Brake = t < 1 ? 1 : 0;
        });
    }

    static void Main()
    {
        FlightModel.Rand = () => 0.5;
        Air("gripen_air", Gripen(), 180, 3000);
        Air("tucano_air", AircraftDb.A29(), 85, 2000);
        Air("b737_air", B737(), 130, 3000);
        Ground("gripen_to", Gripen(), 135);
        Ground("tucano_to", AircraftDb.A29(), 85);
        Ground("b737_to", B737(), 150);
    }
}
