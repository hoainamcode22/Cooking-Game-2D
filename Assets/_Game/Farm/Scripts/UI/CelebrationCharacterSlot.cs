using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [V3.1] MỘT SLOT "NHÂN VẬT ĂN MỪNG" trong popup Lên Cấp. Có 2 chế độ:
///
///   • PUPPET (khuyến nghị — "diễn như phim hoạt hình"): CODE diễn trên MỘT hình master
///     theo nguyên tắc idle-animation của film: nhân vật ĐỨNG YÊN NHƯNG THỞ — lồng ngực
///     phồng/xẹp rất chậm (chu kỳ ~3.2s, biên độ ~2%), người đung đưa vi tế, đầu nghiêng
///     nhẹ theo hơi thở; THỈNH THOẢNG (4–7s một lần, ngẫu nhiên) mới nhún một cái có đà
///     → không còn cảm giác "tấm hình nhún nhún" đều như máy. Pivot neo ĐÁY nên mọi
///     bóp giãn nở từ mặt đất lên. Gán thêm <see cref="blinkSprite"/> (cùng pose, mắt
///     nhắm) là nhân vật chớp mắt ngẫu nhiên — "có hồn" hơn hẳn.
///     Kích hoạt khi <see cref="puppetMaster"/> được gán, HOẶC frames chỉ có 1 hình.
///
///   • FRAMES (cũ, giữ tương thích V2): lật sprite-sheet ≥2 frame + bob sin.
///
/// AN TOÀN: không có gì để vẽ → slot tự SetActive(false). Toàn bộ chạy unscaled time.
/// KHÔNG DOTween — coroutine thuần theo chuẩn dự án.
/// Bản world-space cho NPC ngoài scene (khách du lịch…): xem <c>NpcBreathingIdle.cs</c>.
/// </summary>
public class CelebrationCharacterSlot : MonoBehaviour
{
    [Header("Sprite-sheet (chế độ FRAMES — giữ tương thích V2)")]
    [Tooltip("Image hiển thị nhân vật. Để trống → tự GetComponent<Image>().")]
    [SerializeField] private Image targetImage;

    [Tooltip("≥2 frame → lật sheet kiểu V2. ĐÚNG 1 frame → tự chuyển sang PUPPET.")]
    [SerializeField] private Sprite[] frames;

    [Tooltip("Tốc độ lật frame (frame/giây) cho chế độ FRAMES.")]
    [SerializeField] private float framesPerSecond = 12f;

    [Header("Puppet — nhịp THỞ (idle, chạy liên tục, rất chậm & nhỏ)")]
    [Tooltip("Hình master. Gán vào đây (hoặc để frames có đúng 1 hình) → bật PUPPET.")]
    [SerializeField] private Sprite puppetMaster;

    [Tooltip("TÙY CHỌN: cùng pose master nhưng MẮT NHẮM → chớp mắt ngẫu nhiên.")]
    [SerializeField] private Sprite blinkSprite;

    [Tooltip("Chu kỳ 1 hơi thở (giây). Film hoạt hình thường 2.5–4s.")]
    [SerializeField] private float breatheCycle = 3.2f;

    [Tooltip("Biên độ thở (0.022 = ngực phồng ~2.2%). Nhỏ mới giống thật.")]
    [SerializeField] private float breatheAmount = 0.022f;

    [Tooltip("Độ nghiêng đầu vi tế theo hơi thở (độ).")]
    [SerializeField] private float idleTiltDegrees = 1.1f;

    [Tooltip("Đung đưa ngang vi tế (px canvas).")]
    [SerializeField] private float idleSwayPixels = 1.6f;

    [Header("Puppet — cú NHÚN điểm nhấn (thưa, ngẫu nhiên)")]
    [Tooltip("Khoảng cách ngẫu nhiên giữa 2 cú nhún (giây): min.")]
    [SerializeField] private float bounceEveryMin = 4f;

    [Tooltip("Khoảng cách ngẫu nhiên giữa 2 cú nhún (giây): max.")]
    [SerializeField] private float bounceEveryMax = 7f;

    [Tooltip("Thời lượng 1 cú nhún (giây). Chậm rãi ~1.2s cho ra chất film.")]
    [SerializeField] private float bounceDuration = 1.2f;

    [Tooltip("Biên độ bóp giãn của cú nhún (0.06 = lún/vươn ~6%). Nhẹ thôi.")]
    [SerializeField] private float bounceSquash = 0.06f;

    [Tooltip("Độ nghiêng đầu thêm vào lúc nhún (độ).")]
    [SerializeField] private float bounceTiltDegrees = 2.2f;

    [Header("Nhún kiểu FRAMES (V2 cũ)")]
    [SerializeField] private float bobAmplitude = 6f;
    [SerializeField] private float bobFrequency = 1.4f;
    [SerializeField] private float squashAmount = 0.06f;

    [Tooltip("Lệch pha [0..1] để 4 con không thở/nhún cùng nhịp: 0 / 0.25 / 0.5 / 0.75.")]
    [Range(0f, 1f)]
    [SerializeField] private float phaseOffset = 0f;

    // ── Runtime ──────────────────────────────────────────────────────────────
    private RectTransform _rt;
    private Coroutine     _loop;
    private Vector2       _basePos;
    private Vector3       _baseScale = Vector3.one;
    private Quaternion    _baseRot   = Quaternion.identity;
    private Vector2       _basePivot = new Vector2(0.5f, 0.5f);
    private bool          _baseCaptured;
    private bool          _pivotShifted;

    private void Awake()
    {
        _rt = transform as RectTransform;
        if (targetImage == null) targetImage = GetComponent<Image>();
        if (targetImage != null) targetImage.raycastTarget = false; // không chặn nút Nhận Quà
    }

    private void OnEnable() => Play();

    private void OnDisable() => StopAndReset();

    /// <summary>Thay bộ frame lúc runtime / từ tool (giữ tương thích V2).</summary>
    public void SetFrames(Sprite[] newFrames, float fps = -1f)
    {
        frames = newFrames;
        if (fps > 0f) framesPerSecond = fps;
        if (isActiveAndEnabled) Play();
    }

    /// <summary>Gán master (+ blink tùy chọn) và bật chế độ PUPPET.</summary>
    public void SetMaster(Sprite master, Sprite blink = null)
    {
        puppetMaster = master;
        blinkSprite  = blink;
        if (isActiveAndEnabled) Play();
    }

    /// <summary>Bắt đầu (hoặc chạy lại) loop. PUPPET nếu có master/1 frame; FRAMES nếu ≥2 frame.</summary>
    public void Play()
    {
        if (_rt == null) _rt = transform as RectTransform;
        if (targetImage == null) targetImage = GetComponent<Image>();

        Sprite master = ResolvePuppetMaster();
        bool hasSheet = master == null && CountNonNullFrames() >= 2;

        if ((master == null && !hasSheet) || targetImage == null || _rt == null)
        {
            gameObject.SetActive(false);   // không có gì để vẽ → dẹp slot, không hiện ô trống
            return;
        }
        if (!isActiveAndEnabled) return;

        if (!_baseCaptured)
        {
            _basePos      = _rt.anchoredPosition;
            _baseScale    = _rt.localScale;
            _baseRot      = _rt.localRotation;
            _basePivot    = _rt.pivot;
            _baseCaptured = true;
        }

        if (_loop != null) StopCoroutine(_loop);
        _loop = master != null ? StartCoroutine(PuppetLoop(master))
                               : StartCoroutine(DanceLoop());
    }

    /// <summary>Dừng loop, trả pivot/vị trí/scale/xoay/sprite về gốc. Gọi khi popup đóng.</summary>
    public void StopAndReset()
    {
        if (_loop != null) { StopCoroutine(_loop); _loop = null; }
        if (_baseCaptured && _rt != null)
        {
            RestorePivotIfShifted();
            _rt.anchoredPosition = _basePos;
            _rt.localScale       = _baseScale;
            _rt.localRotation    = _baseRot;
        }
        Sprite master = ResolvePuppetMaster();
        if (targetImage != null && master != null) targetImage.sprite = master;
    }

    private Sprite ResolvePuppetMaster()
    {
        if (puppetMaster != null) return puppetMaster;
        if (frames != null && CountNonNullFrames() == 1)
            for (int i = 0; i < frames.Length; i++)
                if (frames[i] != null) return frames[i];
        return null;
    }

    private int CountNonNullFrames()
    {
        int n = 0;
        if (frames != null)
            for (int i = 0; i < frames.Length; i++)
                if (frames[i] != null) n++;
        return n;
    }

    // =========================================================================
    //  PUPPET — thở chậm liên tục + thỉnh thoảng nhún, pivot neo đáy
    // =========================================================================

    private void ShiftPivotToBottom()
    {
        if (_pivotShifted) return;
        Vector2 delta = new Vector2(0.5f, 0f) - _rt.pivot;
        _rt.anchoredPosition += new Vector2(
            delta.x * _rt.rect.width  * _rt.localScale.x,
            delta.y * _rt.rect.height * _rt.localScale.y);
        _rt.pivot = new Vector2(0.5f, 0f);
        _pivotShifted = true;
    }

    private void RestorePivotIfShifted()
    {
        if (!_pivotShifted) return;
        Vector2 delta = _basePivot - _rt.pivot;
        _rt.anchoredPosition += new Vector2(
            delta.x * _rt.rect.width  * _baseScale.x,
            delta.y * _rt.rect.height * _baseScale.y);
        _rt.pivot = _basePivot;
        _pivotShifted = false;
    }

    private IEnumerator PuppetLoop(Sprite master)
    {
        ShiftPivotToBottom();
        targetImage.sprite = master;
        targetImage.preserveAspect = true;

        float t = 0f;
        float nextBlinkAt  = Random.Range(1.2f, 3.0f);
        float nextBounceAt = Random.Range(1.5f, 3.5f) + phaseOffset * 2f; // 4 con không nhún cùng lúc
        float bounceStart  = -999f;
        const float BlinkTime = 0.09f;

        while (true)
        {
            t += Time.unscaledDeltaTime;

            // ── 1) LỚP THỞ (luôn chạy, rất chậm & nhỏ — "đứng im mà sống") ──
            float cyc = Mathf.Max(0.5f, breatheCycle);
            float u = (t / cyc + phaseOffset) * Mathf.PI * 2f;
            float breathe = Mathf.Sin(u);                       // -1..1, mượt tuyệt đối
            float sy = 1f + breatheAmount * breathe;
            float sx = 1f - breatheAmount * 0.6f * breathe;     // ngực phồng dọc thì hóp ngang nhẹ
            float tilt = idleTiltDegrees * Mathf.Sin(u * 0.5f + 0.8f); // đầu nghiêng chậm gấp đôi
            float sway = idleSwayPixels  * Mathf.Sin(u * 0.5f);

            // ── 2) LỚP NHÚN điểm nhấn (thưa, ngẫu nhiên, chậm rãi) ──
            if (t >= nextBounceAt) { bounceStart = t; nextBounceAt = t + Random.Range(bounceEveryMin, bounceEveryMax); }
            float b = (t - bounceStart) / Mathf.Max(0.3f, bounceDuration);
            if (b >= 0f && b < 1f)
            {
                float k = BounceScaleOffset(b);                 // -1..+1 dạng lún→vươn→hồi
                sy += bounceSquash * k;
                sx -= bounceSquash * 0.75f * k;
                tilt += bounceTiltDegrees * Mathf.Sin(b * Mathf.PI * 2f) * (1f - b);
            }

            _rt.localScale       = new Vector3(_baseScale.x * sx, _baseScale.y * sy, _baseScale.z);
            _rt.localRotation    = Quaternion.Euler(0f, 0f, tilt);
            _rt.anchoredPosition = new Vector2(_basePos.x + sway, _rt.anchoredPosition.y);

            // ── 3) Chớp mắt ngẫu nhiên (chỉ khi có blinkSprite) ──
            if (blinkSprite != null)
            {
                bool blinking = t >= nextBlinkAt && t < nextBlinkAt + BlinkTime;
                Sprite want = blinking ? blinkSprite : master;
                if (targetImage.sprite != want) targetImage.sprite = want;
                if (t >= nextBlinkAt + BlinkTime) nextBlinkAt = t + Random.Range(2.2f, 3.8f);
            }

            yield return null;
        }
    }

    /// <summary>Đường cong 1 cú nhún chậm rãi, p ∈ [0..1]:
    /// lún lấy đà (0→30%) → vươn quá đích (30→60%) → hồi đàn hồi tắt dần (60→100%).
    /// Trả về hệ số -1..+1 (âm = lún, dương = vươn).</summary>
    private static float BounceScaleOffset(float p)
    {
        if (p < 0.30f)
        {
            float q = p / 0.30f;
            return -(q * q * (3f - 2f * q));                    // ease-in-out xuống -1
        }
        if (p < 0.60f)
        {
            float q = (p - 0.30f) / 0.30f;
            return -1f + 2.05f * (q * q * (3f - 2f * q));       // vươn lên ~+1.05 (overshoot nhẹ)
        }
        float r = (p - 0.60f) / 0.40f;
        return 1.05f * (1f - r) * Mathf.Cos(r * Mathf.PI * 1.5f); // dư chấn tắt dần về 0
    }

    // =========================================================================
    //  FRAMES — chế độ V2 cũ (giữ nguyên để tương thích)
    // =========================================================================

    private IEnumerator DanceLoop()
    {
        float t = 0f;
        float frameOffset = phaseOffset * frames.Length;

        while (true)
        {
            t += Time.unscaledDeltaTime;

            float fps = Mathf.Max(0.01f, framesPerSecond);
            int idx = (int)(t * fps + frameOffset) % frames.Length;
            if (idx < 0) idx = 0;
            Sprite frame = frames[idx];
            if (frame != null && targetImage.sprite != frame)
            {
                targetImage.sprite = frame;
                targetImage.preserveAspect = true;
            }

            float sin = Mathf.Sin((t * bobFrequency + phaseOffset) * Mathf.PI * 2f);
            _rt.anchoredPosition = _basePos + new Vector2(0f, sin * bobAmplitude);

            float k = squashAmount * sin;
            _rt.localScale = new Vector3(
                _baseScale.x * (1f - k),
                _baseScale.y * (1f + k),
                _baseScale.z);

            yield return null;
        }
    }
}
