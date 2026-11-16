using System;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>Generated complete handheld tools. Existing surface heads remain in ToolModelSet.</summary>
    [CreateAssetMenu(menuName = "CarpetCleaning/First Person Tool Set", fileName = "FirstPersonToolSet")]
    public sealed class FirstPersonToolSet : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public ToolId id;
            public GameObject prefab;
            public Vector3 cameraPosition;
            public Vector3 eulerOffset;
            public float scale;
            public Vector3 nozzlePosition;
        }

        public Entry[] entries = Array.Empty<Entry>();
        public Material sprayMaterial;

        public bool TryGet(ToolId id, out Entry entry)
        {
            for (int i = 0; i < entries.Length; i++)
                if (entries[i].id == id && entries[i].prefab != null) { entry = entries[i]; return true; }
            entry = default;
            return false;
        }
    }
}
