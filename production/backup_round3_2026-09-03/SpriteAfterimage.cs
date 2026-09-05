using UnityEngine;

/// <summary>
/// 1 con ghost bóng mờ: chụp lại sprite của nhân vật tại 1 khoảnh khắc,
/// đứng yên tại chỗ, fade alpha tuyến tính về 0 (kèm co nhỏ nhẹ nếu bật)
/// rồi tự tắt và trả về pool của <see cref="AfterimageBootstrap"/>.
/// Ghost sống KHÔNG CHA nhân vật (chỉ nằm dưới root "AfterimageGhosts" scale (1,1,1))
/// — copy lossyScale/position/rotation THẾ GIỚI để không bao giờ bị nhân scale cha
/// (bài học prefab scale 100 → 17.000 unit che map).
/// Dùng Time.deltaTime (FX thuộc world, theo timescale).
/// </summary>
[DisallowMultipleComponent]
public class SpriteAfterimage : MonoBehaviour
{
    private SpriteRenderer _sr;
    private float   _age;
    private float   _life = 0.35f;
    private float   _startAlpha;
    private Color   _color;
    private Vector3 _startScale;
    private Vector3 _endScale;
    private bool    _shrink;

    /// <summary>SpriteRenderer riêng của ghost (tự tạo khi cần).</summary>
    public SpriteRenderer Renderer
    {
        get
        {
            if (_sr == null)
            {
                _sr = GetComponent<SpriteRenderer>();
                if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            }
            return _sr;
        }
    }

    /// <summary>
    /// Chụp trạng thái từ SpriteRenderer nguồn của nhân vật. Gọi bởi AfterimageBootstrap.
    /// Ghost phải đang nằm dưới root scale (1,1,1) (hoặc không cha) trước khi gọi.
    /// </summary>
    public void Snapshot(SpriteRenderer source, AfterimageConfig cfg)
    {
        if (source == null || cfg == null) return;

        SpriteRenderer sr = Renderer;
        sr.sprite = source.sprite;
        sr.flipX  = source.flipX;
        sr.flipY  = source.flipY;
        sr.sortingLayerID = source.sortingLayerID;
        sr.sortingOrder   = source.sortingOrder + cfg.sortingOrderOffset;

        Color c = cfg.multiplyTint ? source.color * cfg.tint : cfg.tint;
        _startAlpha = Mathf.Clamp01(cfg.startAlpha) * Mathf.Clamp01(source.color.a);
        c.a = _startAlpha;
        _color = c;
        sr.color = c;

        Transform src = source.transform;
        transform.position   = src.position;
        transform.rotation   = src.rotation;
        // Không cha (root scale 1) ⇒ localScale = lossyScale cho ra đúng kích thước world,
        // bất kể nhân vật nằm dưới cha scale 1 hay 100.
        transform.localScale = src.lossyScale;

        _startScale = src.lossyScale;
        _endScale   = cfg.shrink ? _startScale * cfg.endScaleMul : _startScale;
        _shrink     = cfg.shrink;
        _life       = Mathf.Max(0.01f, cfg.ghostLife);
        _age        = 0f;

        gameObject.SetActive(true);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    /// <summary>Tiến tuổi ghost; hết đời thì tự trả pool. Tách riêng để test logic.</summary>
    public void Tick(float dt)
    {
        _age += dt;
        float k = _life > 0f ? Mathf.Clamp01(_age / _life) : 1f;

        if (_sr != null)
        {
            Color c = _color;
            c.a = Mathf.Lerp(_startAlpha, 0f, k);
            _sr.color = c;
        }
        if (_shrink)
            transform.localScale = Vector3.Lerp(_startScale, _endScale, k);

        if (k >= 1f)
        {
            gameObject.SetActive(false);
            AfterimageBootstrap.ReturnGhost(this);
        }
    }
}
