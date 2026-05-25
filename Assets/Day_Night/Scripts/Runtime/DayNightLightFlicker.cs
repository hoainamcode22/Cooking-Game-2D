using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace Day_Night
{
    public class DayNightLightFlicker : MonoBehaviour
    {
        [SerializeField] public float m_PositionJitterScale;
        [SerializeField] public float m_RotationJitterScale;
        [SerializeField] public float m_IntensityJitterScale;
        [SerializeField] public float m_Timescale = 1f;

        private Vector3 initialPosition;
        private float initialIntensity;
        private Quaternion initialRotation;
        private float xSeed;
        private float ySeed;
        private float zSeed;
        private Light2D targetLight;

        private void Start()
        {
            Random.InitState(gameObject.GetInstanceID());
            xSeed = Random.value * 248f;
            ySeed = Random.value * 248f;
            zSeed = Random.value * 248f;

            targetLight = GetComponent<Light2D>();
            if (targetLight != null)
            {
                initialIntensity = targetLight.intensity;
            }

            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void Update()
        {
            if (targetLight == null)
            {
                targetLight = GetComponent<Light2D>();
                if (targetLight == null)
                {
                    return;
                }
            }

            float x = Time.time * m_Timescale + xSeed;
            float y = Time.time * m_Timescale + ySeed;
            float z = Time.time * m_Timescale + zSeed;

            Vector3 noise = PerlinNoise3D(new Vector3(x, y, z), 2, 1f) * 2f - Vector3.one;
            transform.SetPositionAndRotation(
                initialPosition + noise * m_PositionJitterScale,
                initialRotation * Quaternion.Euler(noise * m_RotationJitterScale));

            targetLight.intensity = initialIntensity + noise.x * m_IntensityJitterScale;
        }

        private static Vector3 PerlinNoise3D(Vector3 uv, int octaves, float frequency)
        {
            Vector3 output = Vector3.zero;
            for (int i = 0; i < octaves; i++)
            {
                float octaveFrequency = frequency * (i + 1);
                output.x += Mathf.PerlinNoise(uv.x * octaveFrequency, 0f);
                output.y += Mathf.PerlinNoise(uv.y * octaveFrequency, 0f);
                output.z += Mathf.PerlinNoise(uv.z * octaveFrequency, 0f);
            }

            return output / Mathf.Max(1, octaves);
        }
    }
}
