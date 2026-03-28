using UnityEngine;

namespace Gridlock.Visual
{
    public class ImpactFlash : MonoBehaviour
    {
        private float _lifetime;
        private float _maxLifetime;
        private MeshRenderer _renderer;
        private Light _light;

        public static void Spawn(Vector3 position, Color color, float size = 0.4f, float duration = 0.15f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ImpactFlash";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * size;
            Destroy(go.GetComponent<Collider>());

            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Custom/VectorGlow"));
            mr.material.SetColor("_Color", color);
            mr.material.SetColor("_EmissionColor", color);
            mr.material.SetFloat("_EmissionIntensity", 8f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var light = go.AddComponent<Light>();
            light.color = color;
            light.intensity = 3f;
            light.range = 3f;
            light.type = LightType.Point;

            var flash = go.AddComponent<ImpactFlash>();
            flash._maxLifetime = duration;
            flash._renderer = mr;
            flash._light = light;
        }

        private void Update()
        {
            _lifetime += Time.deltaTime;
            float t = _lifetime / _maxLifetime;

            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            float fade = 1f - t * t;
            float scale = 1f + t * 2f;
            transform.localScale = Vector3.one * scale * 0.4f;

            if (_renderer != null)
            {
                var color = _renderer.material.GetColor("_EmissionColor");
                _renderer.material.SetFloat("_EmissionIntensity", 8f * fade);
                _renderer.material.color = new Color(color.r, color.g, color.b, fade);
            }

            if (_light != null)
            {
                _light.intensity = 3f * fade;
                _light.range = 3f + t * 2f;
            }
        }
    }
}
