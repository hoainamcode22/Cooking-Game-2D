using UnityEngine;

[DisallowMultipleComponent]
public class EnvironmentSway : MonoBehaviour
{
    [SerializeField] private float swayAngle = 3.2f;
    [SerializeField] private float swaySpeed = 1.15f;
    [SerializeField] private float positionAmplitude = 0.035f;
    [SerializeField] private Vector2 positionAxis = Vector2.right;
    [SerializeField] private float scaleAmplitude = 0.006f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private float phase;

    private void OnEnable()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        initialScale = transform.localScale;
        phase = BuildStablePhase();
    }

    private void Update()
    {
        float wave = Mathf.Sin((Time.time * swaySpeed) + phase);
        float softWave = Mathf.Sin((Time.time * swaySpeed * 0.63f) + phase);

        transform.localRotation = initialRotation * Quaternion.Euler(0f, 0f, wave * swayAngle);
        transform.localPosition = initialPosition + (Vector3)(positionAxis.normalized * (softWave * positionAmplitude));

        if (scaleAmplitude > 0f)
        {
            float scaleOffset = 1f + (Mathf.Sin((Time.time * swaySpeed * 0.47f) + phase) * scaleAmplitude);
            transform.localScale = initialScale * scaleOffset;
        }
    }

    private float BuildStablePhase()
    {
        Vector3 p = transform.position;
        float seed = (p.x * 12.9898f) + (p.y * 78.233f) + (GetInstanceID() * 0.017f);
        return Mathf.Repeat(Mathf.Sin(seed) * 43758.5453f, Mathf.PI * 2f);
    }

    private void OnDisable()
    {
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;
        transform.localScale = initialScale;
    }

    private void OnValidate()
    {
        swaySpeed = Mathf.Max(0f, swaySpeed);
        positionAmplitude = Mathf.Max(0f, positionAmplitude);
        scaleAmplitude = Mathf.Max(0f, scaleAmplitude);
    }
}
