using System.Runtime.InteropServices;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>48-byte tool stamp, mirrors the HLSL struct in SimCommon.hlsl (section 3.4).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ToolStamp
    {
        public Vector2 uv;        // stamp centre in carpet UV
        public Vector2 dir;       // unit stroke direction in mm space
        public float radiusMm;    // disc radius, or blade half-thickness along the stroke
        public float lengthMm;    // blade full length across the stroke (0 for discs)
        public float pressure;    // 0..1
        public float speed;       // mm/s (0 for a hold stamp)
        public float dtS;         // seconds this stamp represents
        public int tool;          // ToolId
        public Vector2 vStroke;   // dir * speed [mm/s]

        public const int Size = 48;
    }
}
