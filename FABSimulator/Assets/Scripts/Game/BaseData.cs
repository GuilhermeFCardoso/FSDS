using System;
using System.IO;
using UnityEngine;

namespace FABSim.Game
{
    [Serializable] public class RunwayEnd { public string ident; public double lat, lon; }
    [Serializable] public class RunwayData { public string ident; public RunwayEnd cabeceira_a, cabeceira_b; public double comprimento_m, largura_m, rumo_verdadeiro; public bool estimado; }
    [Serializable] public class BaseData { public string icao, nome; public double lat, lon, elevacao_m; public RunwayData[] pistas; }
    [Serializable] public class BasesFile { public BaseData[] bases; }

    public static class BaseDb
    {
        static BasesFile file;

        public static BaseData[] All
        {
            get
            {
                if (file == null) file = JsonUtility.FromJson<BasesFile>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Data", "bases.json")));
                return file.bases;
            }
        }

        public static BaseData Find(string icao) => Array.Find(All, b => b.icao == icao);
    }
}
