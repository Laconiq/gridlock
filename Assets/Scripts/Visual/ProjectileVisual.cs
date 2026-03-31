using Gridlock.Mods.Pipeline;
using UnityEngine;

namespace Gridlock.Visual
{
    public static class ProjectileVisual
    {
        private static Material _projectileMat;
        private static Material _trailMat;

        private static readonly int Color0Id = Shader.PropertyToID("_Color0");
        private static readonly int Color1Id = Shader.PropertyToID("_Color1");
        private static readonly int Color2Id = Shader.PropertyToID("_Color2");
        private static readonly int Color3Id = Shader.PropertyToID("_Color3");
        private static readonly int ColorCountId = Shader.PropertyToID("_ColorCount");
        private static readonly int EmissionIntensityId = Shader.PropertyToID("_EmissionIntensity");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");

        static readonly Color DefaultColor = new(0f, 1f, 1f);
        static readonly Color BurnColor = new(1f, 0.3f, 0.05f);
        static readonly Color FrostColor = new(0.2f, 0.6f, 1f);
        static readonly Color ShockColor = new(1f, 0.95f, 0.2f);
        static readonly Color VoidColor = new(0.6f, 0.1f, 1f);
        static readonly Color LeechColor = new(0.1f, 1f, 0.4f);

        public static void Apply(GameObject projectile, ModContext ctx)
        {
            var colors = GetElementColors(ctx.Tags);
            float baseScale = Mathf.Lerp(0.2f, 0.5f, Mathf.Clamp01(ctx.Damage / 40f));
            projectile.transform.localScale = Vector3.one * baseScale;

            ApplyMaterial(projectile, colors, ctx.Damage);
            AttachTrail(projectile, colors, baseScale);
        }

        private static void ApplyMaterial(GameObject projectile, Color[] colors, float damage)
        {
            EnsureProjectileMaterial();

            var renderer = projectile.GetComponentInChildren<MeshRenderer>();
            if (renderer == null) return;

            renderer.sharedMaterial = _projectileMat;

            var mpb = new MaterialPropertyBlock();
            mpb.SetColor(Color0Id, colors[0]);
            if (colors.Length > 1) mpb.SetColor(Color1Id, colors[1]);
            if (colors.Length > 2) mpb.SetColor(Color2Id, colors[2]);
            if (colors.Length > 3) mpb.SetColor(Color3Id, colors[3]);
            mpb.SetFloat(ColorCountId, colors.Length);

            float intensity = Mathf.Lerp(8f, 25f, Mathf.Clamp01(damage / 40f));
            mpb.SetFloat(EmissionIntensityId, intensity);

            bool hasElement = colors[0] != DefaultColor;
            mpb.SetFloat(PulseSpeedId, hasElement ? 6f : 0f);

            renderer.SetPropertyBlock(mpb);
        }

        private static void AttachTrail(GameObject projectile, Color[] colors, float scale)
        {
            EnsureTrailMaterial();

            var trail = projectile.GetComponent<TrailRenderer>();
            if (trail == null)
                trail = projectile.AddComponent<TrailRenderer>();

            trail.time = 0.2f;
            trail.minVertexDistance = 0.08f;
            trail.widthMultiplier = scale * 0.6f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.numCornerVertices = 2;
            trail.numCapVertices = 2;
            trail.material = _trailMat;

            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0f)
            );

            var colorKeys = new System.Collections.Generic.List<GradientColorKey>();
            colorKeys.Add(new GradientColorKey(Color.white, 0f));

            if (colors.Length == 1)
            {
                colorKeys.Add(new GradientColorKey(colors[0], 0.15f));
                colorKeys.Add(new GradientColorKey(colors[0] * 0.3f, 1f));
            }
            else
            {
                float usable = 0.85f;
                float step = usable / colors.Length;
                for (int i = 0; i < colors.Length; i++)
                    colorKeys.Add(new GradientColorKey(colors[i], 0.1f + step * i));
                colorKeys.Add(new GradientColorKey(colors[colors.Length - 1] * 0.3f, 1f));
            }

            var gradient = new Gradient();
            gradient.SetKeys(
                colorKeys.ToArray(),
                new[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.6f, 0.4f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trail.colorGradient = gradient;
        }

        public static Color[] GetElementColors(ModTags tags)
        {
            var list = new System.Collections.Generic.List<Color>(4);

            if (tags.HasFlag(ModTags.Burn)) list.Add(BurnColor);
            if (tags.HasFlag(ModTags.Frost)) list.Add(FrostColor);
            if (tags.HasFlag(ModTags.Shock)) list.Add(ShockColor);
            if (tags.HasFlag(ModTags.Void)) list.Add(VoidColor);
            if (tags.HasFlag(ModTags.Leech)) list.Add(LeechColor);

            if (list.Count == 0) list.Add(DefaultColor);
            return list.ToArray();
        }

        private static void EnsureProjectileMaterial()
        {
            if (_projectileMat != null) return;
            var shader = Shader.Find("Custom/NeonProjectile");
            if (shader == null)
            {
                shader = Shader.Find("Custom/VectorGlow");
                Debug.LogWarning("[ProjectileVisual] NeonProjectile shader not found, falling back to VectorGlow");
            }
            _projectileMat = new Material(shader);
        }

        private static void EnsureTrailMaterial()
        {
            if (_trailMat != null) return;
            var shader = Shader.Find("Custom/NeonTrail");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                Debug.LogWarning("[ProjectileVisual] NeonTrail shader not found, falling back to Particles/Unlit");
            }
            _trailMat = new Material(shader);
        }
    }
}
