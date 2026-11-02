namespace FABSim.Core
{
    public sealed class AircraftInfo
    {
        public string Designation, Role, Manufacturer, EngineText, MaxSpeed, Ceiling, MaxWeight, Length, Span, Height, Squadrons;
    }

    public static class AircraftDb
    {
        static GearPoint Wheel(double x, double y, double z, double share, double steer = 0, bool brake = false) =>
            new GearPoint { P = new V3(x, y, z), Share = share, Steer = steer, Brake = brake };

        public static AircraftDef F39E() => new AircraftDef
        {
            Id = "f39e", Name = "F-39E Gripen",
            Mass0 = 8000, FuelMax = 3400, S = 30, B = 8.6, C = 4.5, Ix = 15000, Iy = 120000, Iz = 130000,
            CL0 = 0.05, CLa = 3.4, AStall = 0.6,
            CD0 = 0.017, K = 0.17, DCLf = 0.25, DCDf = 0.025, DCmf = -0.03, DAsf = 0,
            DCDgear = 0.02, DCDsb = 0.045, DCLsb = 0,
            Cm0 = 0, Cma = -0.35, Cmq = -6, Cmde = 0.55,
            Clb = -0.06, Clp = -0.25, Clr = 0.05, Clda = 0.14, Cldr = 0.01,
            Cnb = 0.1, Cnr = -0.25, Cnp = -0.03, Cndr = 0.06, Cnda = -0.005,
            CYb = -0.8, CYdr = 0.25,
            Control = ControlLaw.Fbw,
            Fbw = new FbwDef { NzMax = 9, NzMin = -3, AlphaLim = 0.45, PMax = 4.0, Lq = 6, Lp = 10, Lr = 4 },
            FlapNotches = new double[] { 0, 1, 2 }, FlapRate = 0.6, GearRate = 0.2,
            Engine = new EngineDef { Type = EngineType.Jet, Afterburner = true, T0 = 58000, TAB = 97900, Sx = 0.8, Km = 0.45, Idle = 0.3, Fi = 0.04, Tau = 1.2, Tsfc = 22e-6, TsfcAB = 57e-6, Dz = 0, NLabel = "N2" },
            Gear = new[] { Wheel(3.9, 0, 1.5, 0.2, 45), Wheel(-0.9, -1.15, 1.5, 0.4, 0, true), Wheel(-0.9, 1.15, 1.5, 0.4, 0, true) },
            Crash = new[] { new CrashPoint(-5.9, 0, 0.3, true), new CrashPoint(-1, -4.3, 0.4), new CrashPoint(-1, 4.3, 0.4), new CrashPoint(7, 0, 0.5), new CrashPoint(-3, 0, 0.5) },
            GearDefl = 0.18, GearStroke = 0.4, GearLoadLimit = 5, CgHeight = 1.32,
            NzLimitLow = -3, NzLimitHigh = 9,
            Wave = new WaveDef { Sup = true, Crit = 0.9, Peak = 0.02, Post = 0.012 },
            Stall = new StallDef { W = 0.05, On = 0.2, Off = 0.3, Cm0 = 0.1, Cm1 = 0.6, Rot = 0.6, Cd = 1.2 },
        };

        public static AircraftDef A29() => new AircraftDef
        {
            Id = "a29", Name = "A-29 Super Tucano",
            Mass0 = 3300, FuelMax = 600, S = 19.4, B = 11.14, C = 1.84, Ix = 4800, Iy = 8200, Iz = 11300,
            CL0 = 0.28, CLa = 4.8, AStall = 0.28,
            CD0 = 0.027, K = 0.058, DCLf = 0.7, DCDf = 0.07, DCmf = -0.07, DAsf = -0.02,
            DCDgear = 0.02, DCDsb = 0.03, DCLsb = 0,
            Cm0 = 0.05, Cma = -1.0, Cmq = -14, Cmde = 0.6,
            Clb = -0.09, Clp = -0.5, Clr = 0.1, Clda = 0.12, Cldr = 0.008,
            Cnb = 0.08, Cnr = -0.11, Cnp = -0.03, Cndr = 0.05, Cnda = -0.008,
            CYb = -0.33, CYdr = 0.16,
            Wave = new WaveDef { Mdd = 0.62, K = 25 },
            FlapNotches = new double[] { 0, 20, 40 }, FlapRate = 2, GearRate = 0.3,
            Engine = new EngineDef { Type = EngineType.Piston, Turbo = true, P = 1193e3, Eta = 0.8, V0 = 65, Bsfc = 0.3, Tau = 1.5, Dz = -0.1, NLabel = "RPM" },
            Gear = new[] { Wheel(2.6, 0, 1.35, 0.1, 25), Wheel(-0.3, -1.3, 1.35, 0.45, 0, true), Wheel(-0.3, 1.3, 1.35, 0.45, 0, true) },
            Crash = new[] { new CrashPoint(4.4, 0, 0.1), new CrashPoint(-6.3, 0, 0.4, true), new CrashPoint(0, -5.5, 0.55), new CrashPoint(0, 5.5, 0.55), new CrashPoint(0.5, 0, -0.95) },
            GearDefl = 0.14, GearStroke = 0.3, GearLoadLimit = 5, CgHeight = 1.2,
            NzLimitLow = -3, NzLimitHigh = 7, VRef = 70, GainE = 0.8, GainA = 0.75, GainR = 0.6,
            Stall = new StallDef { On = 0.25, Off = 0.5, Cm0 = 0.2, Cm1 = 1.2, Rot = 0.9, Rb = 0.8, Ail = 0.4, Bias = 0.012, Warn = 0.07 },
        };

        public static AircraftDef KC390() => new AircraftDef
        {
            Id = "kc390", Name = "KC-390 Millennium",
            Mass0 = 46000, FuelMax = 23000, S = 125, B = 35.05, C = 3.9, Ix = 5.5e6, Iy = 1.0e7, Iz = 1.35e7,
            CL0 = 0.22, CLa = 5.2, AStall = 0.27,
            CD0 = 0.025, K = 0.044, DCLf = 1.25, DCDf = 0.13, DCmf = -0.08, DAsf = 0, FlapExp = 0.5,
            DCDgear = 0.02, DCDsb = 0.035, DCLsb = -0.25,
            Cm0 = 0.02, Cma = -1.1, Cmq = -22, Cmde = 1.0,
            Clb = -0.12, Clp = -0.45, Clr = 0.12, Clda = 0.1, Cldr = 0.012,
            Cnb = 0.12, Cnr = -0.15, Cnp = -0.05, Cndr = 0.06, Cnda = -0.004,
            CYb = -0.7, CYdr = 0.2,
            Wave = new WaveDef { Mdd = 0.76, K = 20 },
            FlapNotches = new double[] { 0, 10, 20, 35 }, FlapRate = 1.5, GearRate = 0.12,
            Engine = new EngineDef { Type = EngineType.Jet, T0 = 278700, Sx = 0.78, Km = -0.5, Idle = 0.22, Fi = 0.05, Tau = 2.8, Tsfc = 17e-6, Dz = 1.0, NLabel = "N1" },
            Gear = new[] { Wheel(13.2, 0, 3.2, 0.08, 55), Wheel(-1.2, -2.4, 3.2, 0.46, 0, true), Wheel(-1.2, 2.4, 3.2, 0.46, 0, true) },
            Crash = new[] { new CrashPoint(-17.6, 0, -0.6, true), new CrashPoint(2, -17.5, -2.5), new CrashPoint(2, 17.5, -2.5), new CrashPoint(17.5, 0, 1.2) },
            GearDefl = 0.25, GearStroke = 0.5, GearLoadLimit = 3.6, CgHeight = 2.95,
            NzLimitLow = -1, NzLimitHigh = 2.5, VRef = 70, GainE = 0.75, GainA = 0.7, GainR = 0.6,
            Stall = new StallDef { On = 0.3, Off = 0.7, Cm0 = 0.15, Cm1 = 2.2, Rot = 1.1, Bias = 0.02, Warn = 0.05 },
        };

        public static AircraftInfo Info(string id)
        {
            switch (id)
            {
                case "f39e": return new AircraftInfo { Designation = "F-39E Gripen", Role = "Superioridade Aérea e Defesa Aeroespacial", Manufacturer = "Saab / Embraer", EngineText = "1x General Electric F414-GE-39E (22.000 lbf com pós-combustão)", MaxSpeed = "Mach 2.0 (~2.200 km/h)", Ceiling = "52.500 ft (~16.000 m)", MaxWeight = "16.500 kg", Length = "15,20 m", Span = "8,60 m", Height = "4,50 m", Squadrons = "1º GDA - Primeiro Grupo de Defesa Aérea Jaguar" };
                case "a29": return new AircraftInfo { Designation = "A-29 Super Tucano", Role = "Ataque, Reconhecimento Armado e Treinamento", Manufacturer = "Embraer", EngineText = "1x Pratt & Whitney Canada PT6A-68C (1.600 SHP)", MaxSpeed = "320 kt (~590 km/h)", Ceiling = "35.000 ft (~10.665 m)", MaxWeight = "5.400 kg", Length = "11,38 m", Span = "11,14 m", Height = "3,97 m", Squadrons = "2º/5º GAv Joker, 1º/3º GAv Escorpião e outros" };
                case "kc390": return new AircraftInfo { Designation = "KC-390 Millennium", Role = "Transporte Aerologístico, REVO, Combate a Incêndio e Lançamento PQD", Manufacturer = "Embraer", EngineText = "2x IAE V2500-E5 (31.330 lbf cada)", MaxSpeed = "Mach .80 (~850 km/h)", Ceiling = "36.000 ft (~11.000 m)", MaxWeight = "87.000 kg", Length = "35,20 m", Span = "35,05 m", Height = "11,92 m", Squadrons = "1º GTT Zeus e 1º/1º GT Gordo" };
            }
            return null;
        }
    }
}
