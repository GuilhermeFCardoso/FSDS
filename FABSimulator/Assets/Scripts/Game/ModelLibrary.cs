using System;
using System.Collections.Generic;
using UnityEngine;

namespace FABSim.Game
{
    public sealed class ModelLibrary : ScriptableObject
    {
        [Serializable] public class Entry { public string key; public GameObject prefab; }
        public List<Entry> entries = new List<Entry>();
        public GameObject Find(string key) { foreach (var e in entries) if (e.key == key) return e.prefab; return null; }
    }
}
