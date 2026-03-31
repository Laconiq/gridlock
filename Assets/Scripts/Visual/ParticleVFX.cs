using UnityEngine;

namespace Gridlock.Visual
{
    public static class ParticleVFX
    {
        private static Material _neonParticleMat;
        private static Mesh _cubeMesh;

        public static void MuzzleFlash(Vector3 position, Vector3 forward, Color color)
        {
            var go = new GameObject("MuzzleFlash");
            go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation(forward);

            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 12;
            main.playOnAwake = true;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.gravityModifier = 0f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 10) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.02f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.3f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            DisableUnusedModules(ps);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = GetCubeMesh();
            renderer.material = GetNeonMaterial();
            renderer.enableGPUInstancing = true;
        }

        public static void ImpactBurst(Vector3 position, Color color, float intensity = 1f)
        {
            var go = new GameObject("ImpactBurst");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 0.15f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f * intensity, 8f * intensity);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 20;
            main.playOnAwake = true;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.gravityModifier = 2f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            int count = Mathf.RoundToInt(Mathf.Lerp(6, 16, intensity));
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count, (short)(count + 4)) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(color, 0.2f),
                    new GradientColorKey(color * 0.3f, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.5f, 0.4f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);

            DisableUnusedModules(ps);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = GetCubeMesh();
            renderer.material = GetNeonMaterial();
            renderer.enableGPUInstancing = true;
        }

        private static void DisableUnusedModules(ParticleSystem ps)
        {
            var m0 = ps.collision; m0.enabled = false;
            var m1 = ps.subEmitters; m1.enabled = false;
            var m2 = ps.textureSheetAnimation; m2.enabled = false;
            var m3 = ps.lights; m3.enabled = false;
            var m4 = ps.customData; m4.enabled = false;
            var m5 = ps.trigger; m5.enabled = false;
            var m6 = ps.externalForces; m6.enabled = false;
            var m7 = ps.inheritVelocity; m7.enabled = false;
            var m8 = ps.forceOverLifetime; m8.enabled = false;
            var m9 = ps.rotationBySpeed; m9.enabled = false;
            var m10 = ps.colorBySpeed; m10.enabled = false;
            var m11 = ps.sizeBySpeed; m11.enabled = false;
            var m12 = ps.limitVelocityOverLifetime; m12.enabled = false;
            var m13 = ps.noise; m13.enabled = false;
            var m14 = ps.trails; m14.enabled = false;
        }

        private static Mesh GetCubeMesh()
        {
            if (_cubeMesh != null) return _cubeMesh;
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cubeMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(tmp);
            return _cubeMesh;
        }

        private static Material GetNeonMaterial()
        {
            if (_neonParticleMat != null) return _neonParticleMat;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            _neonParticleMat = new Material(shader);
            _neonParticleMat.SetFloat("_Surface", 1f);
            _neonParticleMat.SetFloat("_Blend", 1f);
            _neonParticleMat.SetColor("_BaseColor", Color.white);
            _neonParticleMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _neonParticleMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _neonParticleMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            _neonParticleMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            return _neonParticleMat;
        }
    }
}
