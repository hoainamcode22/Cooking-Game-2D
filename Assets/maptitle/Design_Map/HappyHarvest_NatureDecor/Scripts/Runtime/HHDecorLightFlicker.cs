using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

public class HHDecorLightFlicker : MonoBehaviour
{
    [SerializeField] public float m_PositionJitterScale;
    [SerializeField] public float m_RotationJitterScale;
    [SerializeField] public float m_IntensityJitterScale;
    [SerializeField] public float m_Timescale = 1f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float initialIntensity;
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

        initialPosition = transform.position;
        initialRotation = transform.rotation;
        targetLight = GetComponent<Light2D>();
        initialIntensity = targetLight != null ? targetLight.intensity : 0f;
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

        float t = Time.time * m_Timescale;
        Vector3 noise = new Vector3(
            Mathf.PerlinNoise(t + xSeed, 0f),
            Mathf.PerlinNoise(t + ySeed, 0f),
            Mathf.PerlinNoise(t + zSeed, 0f)) * 2f - Vector3.one;

        transform.SetPositionAndRotation(
            initialPosition + noise * m_PositionJitterScale,
            initialRotation * Quaternion.Euler(noise * m_RotationJitterScale));
        targetLight.intensity = initialIntensity + noise.x * m_IntensityJitterScale;
    }
}
