using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>256x256 tileable RGBA32 noise: R fbm, G Worley F1, B cell hash, A second fbm (section 3.2).</summary>
    public static class NoiseTextureGenerator
    {
        const int N = 256;

        static float Hash(int x, int y, int seed)
        {
            uint h = (uint)(x * 374761393) ^ (uint)(y * 668265263) ^ (uint)(seed * 1274126177);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }

        static float Smooth(float t) { return t * t * (3f - 2f * t); }

        // tileable value noise on an integer lattice of period `period`
        static float ValueNoise(float x, float y, int period, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            int x0 = ((xi % period) + period) % period, y0 = ((yi % period) + period) % period;
            int x1 = (x0 + 1) % period, y1 = (y0 + 1) % period;
            float a = Hash(x0, y0, seed), b = Hash(x1, y0, seed), c = Hash(x0, y1, seed), d = Hash(x1, y1, seed);
            float sx = Smooth(fx), sy = Smooth(fy);
            return Mathf.Lerp(Mathf.Lerp(a, b, sx), Mathf.Lerp(c, d, sx), sy);
        }

        static float Fbm(float u, float v, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            int period = 4;
            for (int o = 0; o < 4; o++)
            {
                sum += amp * ValueNoise(u * period, v * period, period, seed + o * 17);
                norm += amp; amp *= 0.5f; period *= 2;
            }
            return Mathf.Clamp01(sum / norm);
        }

        static void Worley(float u, float v, int period, int seed, out float f1, out float cellHash)
        {
            float px = u * period, py = v * period;
            int cx = Mathf.FloorToInt(px), cy = Mathf.FloorToInt(py);
            f1 = 1e9f; cellHash = 0f;
            for (int j = -1; j <= 1; j++)
            for (int i = -1; i <= 1; i++)
            {
                int gx = cx + i, gy = cy + j;
                int wx = ((gx % period) + period) % period, wy = ((gy % period) + period) % period;
                float ox = Hash(wx, wy, seed), oy = Hash(wx, wy, seed + 101);
                float dx = gx + ox - px, dy = gy + oy - py;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d < f1) { f1 = d; cellHash = Hash(wx, wy, seed + 202); }
            }
        }

        public static Texture2D Generate(int seed = 7)
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true, true)
            {
                name = "CarpetNoise", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear
            };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N, v = (y + 0.5f) / N;
                float r = Fbm(u, v, seed);
                Worley(u, v, 8, seed + 33, out float f1, out float ch);
                float g = Mathf.Clamp01(f1 / 1.2f);
                float a = Fbm(u + 0.37f, v + 0.61f, seed + 500);
                px[y * N + x] = new Color32((byte)(r * 255f), (byte)(g * 255f), (byte)(ch * 255f), (byte)(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(true, false);
            return tex;
        }
    }
}
