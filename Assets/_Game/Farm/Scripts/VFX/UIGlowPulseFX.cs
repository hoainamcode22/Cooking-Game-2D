using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiệu ứng hào quang phát sáng nhấp nháy (Glow Pulse / Halo FX) bao quanh các nút hoặc mục tiêu cần chỉ dẫn.
/// Dùng cho Tutorial hoặc các nút Call-To-Action (Nấu ăn, Nhận thưởng, Mở kho, Nâng cấp...).
/// Có thể kéo thả vào Prefab hoặc gọi code 1 dòng: UIGlowPulseFX.Attach(myButtonRect, Color.yellow);
/// </summary>
[DisallowMultipleComponent]
public class UIGlowPulseFX : MonoBehaviour
{
    [Header("◆ Cấu hình Hào Quang")]
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.2f, 0.85f);
    [SerializeField] private float pulseSpeed = 2.2f;
    [SerializeField] private float minScale = 0.98f;
    [SerializeField] private float maxScale = 1.18f;
    [SerializeField] private float minAlpha = 0.25f;
    [SerializeField] private float maxAlpha = 0.85f;
    [SerializeField] private Sprite customGlowSprite;
    [SerializeField] private Vector2 padding = new Vector2(24f, 24f);

    private RectTransform _targetRect;
    private RectTransform _glowRect;
    private Image _glowImage;
    private bool _isPlaying = true;
    private static Sprite _defaultGlowSprite;

    public static UIGlowPulseFX Attach(RectTransform target, Color? color = null, float speed = 2.2f)
    {
        if (target == null) return null;
        var existing = target.GetComponent<UIGlowPulseFX>();
        if (existing != null)
        {
            existing.SetColor(color ?? existing.glowColor);
            existing.Play();
            return existing;
        }

        var fx = target.gameObject.AddComponent<UIGlowPulseFX>();
        if (color.HasValue) fx.glowColor = color.Value;
        fx.pulseSpeed = speed;
        return fx;
    }

    public static void Remove(RectTransform target)
    {
        if (target == null) return;
        var existing = target.GetComponent<UIGlowPulseFX>();
        if (existing != null)
        {
            Destroy(existing);
        }
    }

    private void Awake()
    {
        _targetRect = GetComponent<RectTransform>();
        BuildGlowVisual();
    }

    private void OnEnable()
    {
        _isPlaying = true;
        StartCoroutine(RoutinePulse());
    }

    private void OnDisable()
    {
        _isPlaying = false;
    }

    private void OnDestroy()
    {
        if (_glowRect != null)
            Destroy(_glowRect.gameObject);
    }

    public void SetColor(Color col)
    {
        glowColor = col;
        if (_glowImage != null) _glowImage.color = col;
    }

    public void Play()
    {
        _isPlaying = true;
        if (_glowRect != null) _glowRect.gameObject.SetActive(true);
    }

    public void Stop()
    {
        _isPlaying = false;
        if (_glowRect != null) _glowRect.gameObject.SetActive(false);
    }

    private void BuildGlowVisual()
    {
        if (_glowRect != null || _targetRect == null) return;

        var go = new GameObject("FX_GlowHalo", typeof(RectTransform));
        go.transform.SetParent(_targetRect, false);
        go.transform.SetAsFirstSibling(); // Nằm sau lưng nút chính

        _glowRect = (RectTransform)go.transform;
        _glowRect.anchorMin = Vector2.zero;
        _glowRect.anchorMax = Vector2.one;
        _glowRect.offsetMin = -padding;
        _glowRect.offsetMax = padding;

        _glowImage = go.AddComponent<Image>();
        _glowImage.sprite = GetGlowSprite();
        _glowImage.type = Image.Type.Sliced;
        _glowImage.color = glowColor;
        _glowImage.raycastTarget = false;
    }

    private Sprite GetGlowSprite()
    {
        if (customGlowSprite != null) return customGlowSprite;
        if (_defaultGlowSprite != null) return _defaultGlowSprite;

#if UNITY_EDITOR
        _defaultGlowSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/TutorialV2/vfx/tut_glow_ring.png")
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites/circle_preview.png");
#endif

        if (_defaultGlowSprite == null)
        {
            // Tạo runtime texture hào quang mềm mại nếu thiếu asset
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float a = Mathf.Clamp01(1f - (dist / radius));
                    a = Mathf.Pow(a, 1.8f); // Soft falloff
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            _defaultGlowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
        }

        return _defaultGlowSprite;
    }

    private IEnumerator RoutinePulse()
    {
        while (_isPlaying)
        {
            float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f; // 0..1
            float scale = Mathf.Lerp(minScale, maxScale, t);
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

            if (_glowRect != null)
                _glowRect.localScale = Vector3.one * scale;

            if (_glowImage != null)
                _glowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * alpha);

            yield return null;
        }
    }
}
