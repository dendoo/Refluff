using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>Reproducible atmospheric motion, with dust restricted to the physical window portals.</summary>
    [DefaultExecutionOrder(190)]
    public sealed class WorkshopAtmosphere : MonoBehaviour
    {
        public Renderer[] shafts;
        public ParticleSystem[] motes;
        public Transform[] foliage;
        public Quaternion[] foliageRestRotations;
        [Min(0f)] public float motionSpeed = 1f;
        [Range(0f, 1f)] public float moteOpacity = .27f;
        const int MotesPerShaft = 32;
        static readonly int ClockId = Shader.PropertyToID("_AtmosphereClock");
        MaterialPropertyBlock block;
        ParticleSystem.Particle[][] particles;
        float previewTime = -1f;

        void Awake()
        {
            // No domain or scene reload in this project: recreate all private runtime caches.
            block = new MaterialPropertyBlock();
            particles = null;
            previewTime = -1f;
            if (motes != null) foreach (var system in motes)
            {
                if (system == null) continue;
                system.Play(false);
                system.Pause(false);
            }
            Apply(0f);
        }

        void LateUpdate() => Apply(previewTime >= 0f ? previewTime : Time.time * motionSpeed);

        public void SetPreviewTime(float seconds)
        {
            previewTime = Mathf.Max(0f, seconds);
            Apply(previewTime);
        }

        public void ResumeTime() => previewTime = -1f;

        void Apply(float seconds)
        {
            if (foliage != null && foliageRestRotations != null)
                for (int i = 0; i < Mathf.Min(foliage.Length, foliageRestRotations.Length); i++)
                {
                    if (foliage[i] == null) continue;
                    float sign = (i & 1) == 0 ? 1f : -1f;
                    float phase = seconds * .17f + i * 1.91f;
                    foliage[i].localRotation = foliageRestRotations[i] * Quaternion.Euler(
                        Mathf.Sin(phase * .71f) * 2.2f, Mathf.Sin(phase * .83f) * 3f,
                        sign * Mathf.Sin(phase) * 5.5f);
                }
            if (shafts == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            if (particles == null || particles.Length != shafts.Length)
                particles = new ParticleSystem.Particle[shafts.Length][];
            for (int s = 0; s < shafts.Length; s++)
            {
                Renderer shaft = shafts[s];
                if (shaft == null || shaft.sharedMaterial == null) continue;
                shaft.GetPropertyBlock(block);
                block.SetVector(ClockId, new Vector4(seconds, 1f, 0f, 0f));
                shaft.SetPropertyBlock(block);
                if (motes == null || s >= motes.Length || motes[s] == null) continue;
                if (particles[s] == null) particles[s] = new ParticleSystem.Particle[MotesPerShaft];
                Material material = shaft.sharedMaterial;
                Vector3 origin = material.GetVector("_WindowOrigin");
                Vector4 right = material.GetVector("_WindowRight");
                Vector4 up = material.GetVector("_WindowUp");
                Vector4 direction = material.GetVector("_BeamDirection");
                for (int i = 0; i < MotesPerShaft; i++)
                {
                    int seed = 31 + s * 53 + i * 7;
                    float travel = Mathf.Repeat(Random01(seed) + seconds * .004f, 1f);
                    float x = (Random01(seed + 1) * 2f - 1f) * right.w * .84f;
                    float y = (Random01(seed + 2) * 2f - 1f) * up.w * .84f;
                    // Keep the particles out of the mullion's unlit strip.
                    y = Mathf.Sign(y) * Mathf.Max(Mathf.Abs(y), .085f);
                    Vector3 position = origin + (Vector3)right * x + (Vector3)up * y
                        + (Vector3)direction * (travel * direction.w);
                    position += new Vector3(Mathf.Sin(seconds * .23f + seed),
                        Mathf.Sin(seconds * .17f + seed * .8f), Mathf.Cos(seconds * .19f + seed)) * .016f;
                    float fade = Mathf.SmoothStep(0f, 1f, travel * 8f)
                        * Mathf.SmoothStep(0f, 1f, (1f - travel) * 8f)
                        * Mathf.SmoothStep(0f, 1f, (position.y - .03f) * 4f);
                    var particle = new ParticleSystem.Particle();
                    particle.position = position;
                    particle.startSize = Mathf.Lerp(.004f, .012f, Random01(seed + 3));
                    particle.startColor = new Color(1f, .91f, .72f, fade * moteOpacity);
                    particle.startLifetime = 100f;
                    particle.remainingLifetime = 100f;
                    particle.randomSeed = (uint)seed;
                    particles[s][i] = particle;
                }
                motes[s].SetParticles(particles[s], MotesPerShaft);
            }
        }

        void OnDisable()
        {
            if (shafts == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            foreach (var shaft in shafts)
            {
                if (shaft == null) continue;
                shaft.GetPropertyBlock(block);
                block.SetVector(ClockId, Vector4.zero);
                shaft.SetPropertyBlock(block);
            }
            if (motes != null) foreach (var system in motes)
                if (system != null) system.Clear();
            if (foliage != null && foliageRestRotations != null)
                for (int i = 0; i < Mathf.Min(foliage.Length, foliageRestRotations.Length); i++)
                    if (foliage[i] != null) foliage[i].localRotation = foliageRestRotations[i];
        }

        static float Random01(int seed)
        {
            uint h = unchecked((uint)seed * 747796405u + 2891336453u);
            h = ((h >> (int)((h >> 28) + 4)) ^ h) * 277803737u;
            h = (h >> 22) ^ h;
            return (h & 0x00ffffffu) / 16777216f;
        }
    }
}
