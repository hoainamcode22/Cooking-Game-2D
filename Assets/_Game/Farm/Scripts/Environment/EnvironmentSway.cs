using UnityEngine;

[DisallowMultipleComponent]
public class EnvironmentSway : MonoBehaviour
{
    [SerializeField] private float swayAngle = 3.2f;
    [SerializeField] private float swaySpeed = 1.15f;
    [SerializeField] private float positionAmplitude = 0.035f;
    [SerializeField] private Vector2 positionAxis = Vector2.right;
    [SerializeField] private float scaleAmplitude = 0.006f;

    // ── [PERF F4.9 — 2026-09-17] ─────────────────────────────────────────────────
    // Do duoc: 34 instance EnvironmentSway trong SCN_Farm, moi instance ghi rotation +
    // position (+ scale) MOI FRAME, phan lon cho vat dang o NGOAI khung hinh. Cong them
    // ~26 instance sway cay trong (PlotCropVisual) la ~60 luot Update ghi transform / frame.
    //
    // KHONG BO HIEU UNG. Chi them hai cong:
    //   ① `chiChayKhiThayDuoc` — ngoai khung hinh thi bo qua. Vat khuat dung im o goc nghieng
    //      hien tai; phep sway la HAM THUAN cua Time.time nen khi hien lai no KHONG NHAY.
    //   ② `buocFrame`        — chay 1 lan moi N frame, moi instance mot offset rieng nen
    //      chung khong cung tick vao mot frame. Dao dong sin cham nen mat khong doc ra.
    [Header("Hiệu năng")]
    [SerializeField] private bool chiChayKhiThayDuoc = true;
    [Range(1, 4)]
    [SerializeField] private int  buocFrame = 2;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private float phase;

    // Renderer == null ⇒ khong co bounds ⇒ Unity coi luon hien ⇒ cong ① vo nghia, bo qua.
    private Renderer _renderer;
    private int      _offsetFrame;

    private void OnEnable()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        initialScale = transform.localScale;
        phase = BuildStablePhase();

        // [PERF F4.9] cache 1 lan; `true` = tinh ca Renderer dang tat (co the duoc bat sau).
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>(true);

        int b = Mathf.Clamp(buocFrame, 1, 4);
        _offsetFrame = (int)((uint)GetInstanceID() % (uint)b);   // on dinh trong 1 phien chay.
    }

    private void Update()
    {
        // [PERF F4.9] ① ngoai khung hinh => bo qua luot ghi transform.
        if (chiChayKhiThayDuoc && _renderer != null && !_renderer.isVisible)
            return;

        // [PERF F4.9] ② giam nhip. Ham thuan theo Time.time nen bo frame KHONG lam troi pha.
        int buoc = Mathf.Clamp(buocFrame, 1, 4);
        if (buoc > 1 && (Time.frameCount % buoc) != _offsetFrame)
            return;

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
        buocFrame = Mathf.Clamp(buocFrame, 1, 4);
        positionAmplitude = Mathf.Max(0f, positionAmplitude);
        scaleAmplitude = Mathf.Max(0f, scaleAmplitude);
    }
}
