using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CarpetCleaning
{
    public enum FxKind { Sparkle = 0, Glow = 1, Ring = 2, Confetti = 3, Coin = 4 }

    /// <summary>
    /// UI Toolkit particles for the payout card and the shop (Docs/ECONOMY.md 4.2): a fixed pool of plain VisualElements in
    /// the top-most #fx layer. The look (texture, tint, size) is a USS class per kind and tint, set once when a particle is
    /// born; per frame only translate / rotate / scale / opacity change, which never triggers a layout pass (usageHints
    /// DynamicTransform | DynamicColor). Motion is ANALYTIC in the particle's age (ballistic with linear drag, or a Bezier
    /// arc for the coins), so a timeline seek re-creates exactly the same frame: that is what makes the payout testable
    /// from the CLI, where frames do not tick. Times are on this system's own clock, Now.
    /// </summary>
    public sealed class UiFx
    {
        public const int PoolSize = 220;

        struct Particle
        {
            public int el;
            public FxKind kind;
            public float t0, life, fadeIn;
            public Vector2 p0, v0, g;
            public float drag;
            public bool bezier;
            public Vector2 c, p1;              // Bezier control and end point (coins)
            public float rot0, rotV;           // degrees, degrees / s
            public float s0, s1;
            public float flip, phase;          // confetti tumble rate (rad/s) and flutter phase
        }

        readonly VisualElement _layer;
        readonly VisualElement[] _pool = new VisualElement[PoolSize];
        readonly bool[] _visible = new bool[PoolSize];
        readonly int[] _owner = new int[PoolSize];            // particle serial that holds the element, 0 = free
        readonly List<Particle> _parts = new List<Particle>(256);
        readonly List<int> _serials = new List<int>(256);
        int _serial;
        int _nextEl;

        public float Now;
        public int Alive { get { return _parts.Count; } }

        public UiFx(VisualElement layer)
        {
            _layer = layer;
            for (int i = 0; i < PoolSize; i++)
            {
                var e = new VisualElement { pickingMode = PickingMode.Ignore, usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor };
                e.AddToClassList("fx");
                e.style.visibility = Visibility.Hidden;
                layer.Add(e);
                _pool[i] = e;
            }
        }

        public void Clear()
        {
            _parts.Clear();
            _serials.Clear();
            for (int i = 0; i < PoolSize; i++) { _owner[i] = 0; Hide(i); }
        }

        /// <summary>Layer-local position of an element's centre (where a burst should come from).</summary>
        public Vector2 CenterOf(VisualElement e)
        {
            if (e == null || e.panel == null) return Vector2.zero;
            return _layer.WorldToLocal(e.worldBound.center);
        }

        // ------------------------------------------------------------------ spawning
        static float Rand(ref uint s) { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return (s & 0xFFFFFF) / 16777216f; }

        /// <summary>A radial burst. dirDeg / spreadDeg in screen degrees (0 = right, -90 = up); speeds in px/s.</summary>
        public void Burst(FxKind kind, string tint, Vector2 origin, int count, float t0, uint seed,
                          float speedMin, float speedMax, float life, float sizeMin, float sizeMax,
                          float gravity = 520f, float drag = 2.2f, float dirDeg = -90f, float spreadDeg = 360f)
        {
            uint s = seed == 0 ? 0x9E3779B9u : seed;
            for (int i = 0; i < count; i++)
            {
                float a = (dirDeg + (Rand(ref s) - 0.5f) * spreadDeg) * Mathf.Deg2Rad;
                float sp = Mathf.Lerp(speedMin, speedMax, Rand(ref s));
                var p = new Particle
                {
                    kind = kind, t0 = t0 + Rand(ref s) * 0.04f, life = life * Mathf.Lerp(0.7f, 1.2f, Rand(ref s)), fadeIn = 0.04f,
                    p0 = origin + new Vector2(Rand(ref s) - 0.5f, Rand(ref s) - 0.5f) * 6f,
                    v0 = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp,
                    g = new Vector2(0f, gravity), drag = drag,
                    rot0 = Rand(ref s) * 360f, rotV = (Rand(ref s) - 0.5f) * (kind == FxKind.Confetti ? 900f : 240f),
                    s0 = Mathf.Lerp(sizeMin, sizeMax, Rand(ref s)),
                    flip = Mathf.Lerp(6f, 16f, Rand(ref s)), phase = Rand(ref s) * 6.283f
                };
                p.s1 = kind == FxKind.Sparkle ? p.s0 * 0.2f : p.s0;
                Add(p, tint);
            }
        }

        /// <summary>A single growing element: glow bloom or ring shockwave.</summary>
        public void Pulse(FxKind kind, string tint, Vector2 origin, float t0, float life, float s0, float s1)
        {
            Add(new Particle { kind = kind, t0 = t0, life = life, fadeIn = 0.03f, p0 = origin, s0 = s0, s1 = s1 }, tint);
        }

        /// <summary>A coin flying along a Bezier arc from `from` to `to`, spinning; lands at t0 + flight.</summary>
        public void CoinFly(Vector2 from, Vector2 to, float t0, float flight, uint seed)
        {
            uint s = seed == 0 ? 0x2545F491u : seed;
            Vector2 mid = (from + to) * 0.5f;
            Vector2 side = new Vector2(-(to - from).y, (to - from).x).normalized;
            var p = new Particle
            {
                kind = FxKind.Coin, t0 = t0, life = flight, fadeIn = 0.05f, bezier = true,
                p0 = from + new Vector2(Rand(ref s) - 0.5f, Rand(ref s) - 0.5f) * 30f, p1 = to,
                c = mid + side * Mathf.Lerp(-160f, 160f, Rand(ref s)) + new Vector2(0f, -Mathf.Lerp(120f, 260f, Rand(ref s))),
                rot0 = 0f, rotV = 0f, s0 = Mathf.Lerp(1.0f, 1.2f, Rand(ref s)), s1 = 0.55f,
                flip = Mathf.Lerp(10f, 18f, Rand(ref s)), phase = Rand(ref s) * 6.283f
            };
            Add(p, null);
        }

        void Add(Particle p, string tint)
        {
            int el = TakeElement();
            p.el = el;
            int serial = ++_serial;
            _owner[el] = serial;
            var e = _pool[el];
            e.ClearClassList();
            e.AddToClassList("fx");
            e.AddToClassList(KindClass(p.kind));
            if (!string.IsNullOrEmpty(tint)) e.AddToClassList(tint);
            _parts.Add(p);
            _serials.Add(serial);
        }

        int TakeElement()
        {
            for (int k = 0; k < PoolSize; k++)
            {
                int i = (_nextEl + k) % PoolSize;
                if (_owner[i] == 0) { _nextEl = (i + 1) % PoolSize; return i; }
            }
            // pool full: steal the oldest particle's element
            int oldest = 0;
            for (int k = 1; k < _parts.Count; k++) if (_parts[k].t0 < _parts[oldest].t0) oldest = k;
            int el = _parts[oldest].el;
            _parts.RemoveAt(oldest);
            _serials.RemoveAt(oldest);
            return el;
        }

        static string KindClass(FxKind k)
        {
            switch (k)
            {
                case FxKind.Sparkle: return "fx--sparkle";
                case FxKind.Glow: return "fx--glow";
                case FxKind.Ring: return "fx--ring";
                case FxKind.Confetti: return "fx--confetti";
                default: return "fx--coin";
            }
        }

        // ------------------------------------------------------------------ per frame
        public void Update()
        {
            for (int k = _parts.Count - 1; k >= 0; k--)
            {
                var p = _parts[k];
                float age = Now - p.t0;
                if (age > p.life)
                {
                    if (_owner[p.el] == _serials[k]) { _owner[p.el] = 0; Hide(p.el); }
                    _parts.RemoveAt(k); _serials.RemoveAt(k);
                    continue;
                }
                if (age < 0f) { Hide(p.el); continue; }
                Apply(p, age);
            }
        }

        void Apply(in Particle p, float age)
        {
            float u = age / Mathf.Max(p.life, 1e-4f);
            Vector2 pos;
            float rot = p.rot0 + p.rotV * age;
            float sx, sy;
            float alpha = Mathf.Min(1f, age / Mathf.Max(p.fadeIn, 1e-4f)) * (1f - Smooth(0.65f, 1f, u));

            if (p.bezier)
            {
                float b = UiEase.InOutSine(u);
                float ib = 1f - b;
                pos = ib * ib * p.p0 + 2f * ib * b * p.c + b * b * p.p1;
                float spin = Mathf.Cos(p.phase + age * p.flip);          // a coin spinning edge-on and back
                float s = Mathf.Lerp(p.s0, p.s1, b * b);
                sx = s * Mathf.Max(Mathf.Abs(spin), 0.12f); sy = s;
                alpha = Mathf.Min(1f, age / Mathf.Max(p.fadeIn, 1e-4f)) * (1f - Smooth(0.92f, 1f, u));
            }
            else
            {
                float k = Mathf.Max(p.drag, 1e-3f);
                float e = 1f - Mathf.Exp(-k * age);
                pos = p.p0 + p.v0 * (e / k) + p.g * ((age - e / k) / k);
                switch (p.kind)
                {
                    case FxKind.Sparkle:
                    {
                        float s = Mathf.Lerp(p.s0, p.s1, u) * Mathf.Sqrt(UiEase.Bell(Mathf.Min(u * 1.6f, 1f)) + 0.05f);
                        sx = sy = s;
                        break;
                    }
                    case FxKind.Confetti:
                    {
                        pos.x += Mathf.Sin(p.phase + age * 5f) * 14f * u;
                        sx = p.s0 * Mathf.Cos(p.phase + age * p.flip);
                        sy = p.s0;
                        break;
                    }
                    case FxKind.Ring:
                    {
                        float s = Mathf.Lerp(p.s0, p.s1, UiEase.OutCubic(u));
                        sx = sy = s;
                        alpha = Mathf.Min(1f, age / Mathf.Max(p.fadeIn, 1e-4f)) * (1f - UiEase.InQuad(u));
                        break;
                    }
                    case FxKind.Glow:
                    {
                        float s = Mathf.Lerp(p.s0, p.s1, UiEase.OutCubic(u));
                        sx = sy = s;
                        alpha = Mathf.Min(1f, age / Mathf.Max(p.fadeIn, 1e-4f)) * (1f - UiEase.OutCubic(u));
                        break;
                    }
                    default: sx = sy = p.s0; break;
                }
            }

            var el = _pool[p.el];
            if (!_visible[p.el]) { el.style.visibility = Visibility.Visible; _visible[p.el] = true; }
            el.style.translate = new Translate(new Length(pos.x, LengthUnit.Pixel), new Length(pos.y, LengthUnit.Pixel));
            el.style.rotate = new Rotate(new Angle(rot, AngleUnit.Degree));
            el.style.scale = new Scale(new Vector2(Mathf.Abs(sx) < 1e-3f ? 1e-3f : sx, sy));
            el.style.opacity = Mathf.Clamp01(alpha);
        }

        void Hide(int el)
        {
            if (!_visible[el]) return;
            _pool[el].style.visibility = Visibility.Hidden;
            _visible[el] = false;
        }

        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }
    }
}
