using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Hiệu ứng "HOÀN HẢO!" khi thả nút QUĂNG trong vùng đỏ: chữ TMP vàng đồng viền nâu punch 0.2→1.25→1.0 (0.35 s), lắc ±4°, mờ sau 1.2 s;
    /// "bùm bùm" 2 nhịp, mỗi nhịp 10-12 hạt tròn vàng/trắng/cam bay ra bán kính 140-220 px rồi rơi nhẹ, thu nhỏ + fade 0.6 s;
    /// 2 vòng ring giãn 40→260 px fade. Tất cả hạt/ring/chữ pool trong object "PerfectCastFX_Pool" con của Canvas HUD (find-or-create),
    /// tái dùng, không Destroy. Gọi: PerfectCastFX.Play(markerRect). Kèm AudioManager.PlaySuccess + JuicyPulseFX.Play(anchor).
    /// </summary>
    public class PerfectCastFX : MonoBehaviour
    {
        public static PerfectCastFX Instance { get; private set; }

        public const string PoolName = "PerfectCastFX_Pool";

        private const int BurstCount = 2;
        private const float BurstGap = 0.18f;
        private const int ParticlesPerBurstMin = 10;
        private const int ParticlesPerBurstMax = 12;
        private const float ParticleLife = 0.6f;
        private const float ParticleRadiusMin = 140f;
        private const float ParticleRadiusMax = 220f;
        private const float ParticleGravity = 90f;
        private const float RingFrom = 40f;
        private const float RingTo = 260f;
        private const float RingLife = 0.5f;
        private const float TextPunchSeconds = 0.35f;
        private const float TextHoldSeconds = 1.2f;
        private const float TextFadeSeconds = 0.3f;
        private const float TextWobbleDeg = 4f;
        private const float TextRise = 90f;

        private static readonly Color TextGold = new Color32(217, 164, 65, 255);   // #D9A441
        private static readonly Color TextBrown = new Color32(90, 50, 20, 255);
        private static readonly Color[] ParticleColors =
        {
            new Color32(255, 220, 90, 255),
            new Color32(255, 255, 255, 255),
            new Color32(250, 150, 60, 255),
            new Color32(255, 200, 70, 255),
        };

        private readonly List<Image> _particles = new List<Image>();
        private readonly List<Image> _rings = new List<Image>();
        private TextMeshProUGUI _text;
        private Coroutine _textRoutine;

        /// <summary>HUD/pool bị tắt giữa chừng: coroutine chết nhưng hạt còn active → tắt hết để pool tái dùng được.</summary>
        private void OnDisable()
        {
            for (int i = 0; i < _particles.Count; i++) { if (_particles[i] != null) { _particles[i].gameObject.SetActive(false); } }
            for (int i = 0; i < _rings.Count; i++) { if (_rings[i] != null) { _rings[i].gameObject.SetActive(false); } }
            if (_text != null) { _text.gameObject.SetActive(false); }
            _textRoutine = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Debug.LogWarning(FishingIds.LogTag + " PerfectCastFX trùng tại '" + name + "' — tự ẩn, không huỷ."); gameObject.SetActive(false); return; }
            Instance = this;
        }

        private void OnDestroy() { if (Instance == this) { Instance = null; } }

        // ─────────────────────────────────────────────────────────────────────
        //  API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Bắn FX tại vị trí anchor (con trượt). anchor null → giữa canvas HUD.</summary>
        public static void Play(RectTransform anchor)
        {
            PerfectCastFX fx = Resolve(anchor);
            if (fx == null) { return; }
            fx.PlayAt(anchor);
        }

        private static PerfectCastFX Resolve(RectTransform anchor)
        {
            if (Instance != null) { return Instance; }
            Transform canvasRoot = null;
            if (anchor != null)
            {
                Canvas c = anchor.GetComponentInParent<Canvas>();
                if (c != null) { canvasRoot = c.rootCanvas.transform; }
            }
            if (canvasRoot == null && FishingHudUI.Instance != null) { canvasRoot = FishingHudUI.Instance.transform; }
            if (canvasRoot == null)
            {
                FishingHudUI hud = FindFirstObjectByType<FishingHudUI>(FindObjectsInactive.Include);
                if (hud != null) { canvasRoot = hud.transform; }
            }
            if (canvasRoot == null) { Debug.Log(FishingIds.LogTag + " Không có Canvas HUD để bắn PerfectCastFX."); return null; }

            bool created;
            RectTransform pool = FishingUiKit.Child(canvasRoot, PoolName, Vector2.zero, Vector2.zero, out created);
            if (created) { FishingUiKit.Stretch(pool); }
            PerfectCastFX inst = FishingUiKit.GetOrAdd<PerfectCastFX>(pool.gameObject);
            Instance = inst;
            return inst;
        }

        private void PlayAt(RectTransform anchor)
        {
            RectTransform pool = transform as RectTransform;
            if (pool == null) { return; }
            FishingUiKit.ActivateUpToCanvas(transform);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            Vector2 center = Vector2.zero;
            if (anchor != null) { Vector3 local = pool.InverseTransformPoint(anchor.position); center = new Vector2(local.x, local.y); }

            StartCoroutine(BurstSequence(center));
            ShowText(center);

            if (AudioManager.Instance != null) { AudioManager.Instance.PlaySuccess(); }
            if (anchor != null) { JuicyPulseFX.Play(anchor, 1.15f, 0.25f); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  BÙM BÙM: hạt + ring
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator BurstSequence(Vector2 center)
        {
            for (int b = 0; b < BurstCount; b++)
            {
                int count = Random.Range(ParticlesPerBurstMin, ParticlesPerBurstMax + 1);
                for (int i = 0; i < count; i++)
                {
                    Image p = TakeParticle();
                    if (p == null) { continue; }
                    float ang = Random.Range(0f, Mathf.PI * 2f);
                    float radius = Random.Range(ParticleRadiusMin, ParticleRadiusMax);
                    float size = Random.Range(14f, 26f);
                    Color col = ParticleColors[Random.Range(0, ParticleColors.Length)];
                    StartCoroutine(ParticleRoutine(p, center, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius, size, col));
                }
                Image ring = TakeRing();
                if (ring != null) { StartCoroutine(RingRoutine(ring, center, b == 0 ? new Color(1f, 0.92f, 0.6f, 0.55f) : new Color(1f, 0.7f, 0.35f, 0.45f))); }
                if (b < BurstCount - 1) { yield return new WaitForSeconds(BurstGap); }
            }
        }

        private IEnumerator ParticleRoutine(Image p, Vector2 from, Vector2 offset, float size, Color col)
        {
            RectTransform rt = p.rectTransform;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = from;
            p.color = col;
            p.gameObject.SetActive(true);
            float t = 0f;
            while (t < ParticleLife)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / ParticleLife);
                float ease = 1f - (1f - k) * (1f - k) * (1f - k);   // ease-out cubic
                Vector2 pos = from + offset * ease;
                pos.y -= ParticleGravity * k * k;                   // rơi nhẹ
                rt.anchoredPosition = pos;
                float s = size * (1f - 0.75f * k);
                rt.sizeDelta = new Vector2(s, s);
                Color c = col; c.a = 1f - k * k;
                p.color = c;
                yield return null;
            }
            p.gameObject.SetActive(false);
        }

        private IEnumerator RingRoutine(Image ring, Vector2 center, Color col)
        {
            RectTransform rt = ring.rectTransform;
            rt.anchoredPosition = center;
            rt.sizeDelta = new Vector2(RingFrom, RingFrom);
            ring.color = col;
            ring.gameObject.SetActive(true);
            float t = 0f;
            while (t < RingLife)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / RingLife);
                float ease = 1f - (1f - k) * (1f - k);
                float d = Mathf.Lerp(RingFrom, RingTo, ease);
                rt.sizeDelta = new Vector2(d, d);
                Color c = col; c.a = col.a * (1f - k);
                ring.color = c;
                yield return null;
            }
            ring.gameObject.SetActive(false);
        }

        private Image TakeParticle()
        {
            for (int i = 0; i < _particles.Count; i++)
            {
                Image p = _particles[i];
                if (p != null && !p.gameObject.activeSelf) { return p; }
            }
            Image created = FishingUiKit.Icon(transform, "P_" + FishingUiKit.Num(_particles.Count), FishingUiKit.Circle(), new Vector2(20f, 20f), Vector2.zero, Color.white);
            created.preserveAspect = false;
            created.raycastTarget = false;
            created.gameObject.SetActive(false);
            _particles.Add(created);
            return created;
        }

        private Image TakeRing()
        {
            for (int i = 0; i < _rings.Count; i++)
            {
                Image r = _rings[i];
                if (r != null && !r.gameObject.activeSelf) { return r; }
            }
            Image created = FishingUiKit.Icon(transform, "Ring_" + FishingUiKit.Num(_rings.Count), FishingUiKit.Circle(), new Vector2(RingFrom, RingFrom), Vector2.zero, Color.white);
            created.preserveAspect = false;
            created.raycastTarget = false;
            created.gameObject.SetActive(false);
            _rings.Add(created);
            return created;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  CHỮ "HOÀN HẢO!"
        // ─────────────────────────────────────────────────────────────────────

        private void ShowText(Vector2 center)
        {
            if (_text == null)
            {
                bool created = FishingUiKit.FindChild(transform, "Txt_Perfect") == null;
                _text = FishingUiKit.Label(transform, "Txt_Perfect", Loc.T("HOÀN HẢO!"), 64f, TextAlignmentOptions.Center, new Vector2(700f, 110f), Vector2.zero, TextGold, true);
                _text.overflowMode = TextOverflowModes.Overflow;
                _text.textWrappingMode = TextWrappingModes.NoWrap;
                if (created)
                {
                    FishingUiKit.AddShadow(_text, new Color(0.2f, 0.1f, 0.02f, 0.6f), new Vector2(2f, -4f));
                    Material mat = _text.fontMaterial;   // TMP tự tạo instance riêng cho label này
                    if (mat != null) { mat.EnableKeyword(ShaderUtilities.Keyword_Outline); }
                    _text.outlineColor = TextBrown;
                    _text.outlineWidth = 0.22f;
                    _text.UpdateMeshPadding();
                }
            }
            _text.text = Loc.T("HOÀN HẢO!");
            if (_textRoutine != null) { StopCoroutine(_textRoutine); }
            _textRoutine = StartCoroutine(TextRoutine(_text, center + new Vector2(0f, TextRise)));
        }

        private IEnumerator TextRoutine(TextMeshProUGUI tmp, Vector2 pos)
        {
            RectTransform rt = tmp.rectTransform;
            rt.anchoredPosition = pos;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one * 0.2f;
            Color baseCol = TextGold;
            tmp.color = baseCol;
            tmp.gameObject.SetActive(true);

            // Punch 0.2 → 1.25 → 1.0
            float t = 0f;
            while (t < TextPunchSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / TextPunchSeconds);
                float s = k < 0.6f ? Mathf.Lerp(0.2f, 1.25f, EaseOut(k / 0.6f)) : Mathf.Lerp(1.25f, 1f, (k - 0.6f) / 0.4f);
                rt.localScale = Vector3.one * s;
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI * 3f) * TextWobbleDeg);
                yield return null;
            }
            rt.localScale = Vector3.one;

            // Giữ + lắc nhẹ tắt dần
            t = 0f;
            while (t < TextHoldSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / TextHoldSeconds);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 14f) * TextWobbleDeg * (1f - k));
                yield return null;
            }
            rt.localRotation = Quaternion.identity;

            // Mờ dần + trôi lên
            t = 0f;
            while (t < TextFadeSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / TextFadeSeconds);
                Color c = baseCol; c.a = 1f - k;
                tmp.color = c;
                rt.anchoredPosition = pos + new Vector2(0f, 30f * k);
                yield return null;
            }
            tmp.gameObject.SetActive(false);
            tmp.color = baseCol;
            _textRoutine = null;
        }

        private static float EaseOut(float k)
        {
            k = Mathf.Clamp01(k);
            return 1f - (1f - k) * (1f - k);
        }
    }
}
