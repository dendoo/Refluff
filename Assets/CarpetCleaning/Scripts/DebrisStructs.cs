using System.Runtime.InteropServices;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>48-byte emission request, mirrors DebrisEmit in Compute/DebrisCommon.hlsl.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DebrisEmit
    {
        public Vector3 pos;      // CMM x, height mm, CMM y
        public float size;       // mm
        public Vector3 vel;      // mm/s
        public uint kind;        // 0 chunk, 1 dust, 2 droplet
        public float amount;
        public float seed;
        public float dirt;
        public float pad;

        public const int Size = 48;
    }

    /// <summary>80-byte GPU particle, mirrors DebrisParticle in Compute/DebrisCommon.hlsl.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DebrisParticle
    {
        public Vector3 pos;
        public float size;
        public Vector3 vel;
        public float life;
        public Vector4 rot;      // quaternion
        public Vector3 angVel;   // or suction target while state == 4
        public uint kindState;   // kind | (state << 8)
        public float amount;
        public float seed;
        public float dirt;       // chunk: plate shade, droplet: slurry C, dust: ground height mm under the puff (K_DebSim, low-passed ~0.12 s)
        public float pad;        // dust: lifeMax

        public const int Size = 80;
    }

    /// <summary>Floor / debris frame constants shared with DebrisCommon.hlsl (keep in sync).</summary>
    public static class FloorConsts
    {
        public const int FloorW = 1280;
        public const float FloorSizeMm = 12000f;
        public const float FloorDxMm = FloorSizeMm / FloorW;      // 9.375 mm: unchanged when the active floor expands to all four walls
        public static readonly Vector2 FloorMinMm = (CarpetSimulation.CarpetSizeMm - Vector2.one * FloorSizeMm) * 0.5f;
        public const float FloorYMm = -3f;
        public const int EmitCapacity = 8192;
        public const int ParticleCount = 16384;
        public const float InjectScale = 10000f;
    }
}
