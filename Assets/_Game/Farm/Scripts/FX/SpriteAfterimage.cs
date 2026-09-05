using UnityEngine;

/// <summary>
/// 1 con ghost bóng mờ: chụp lại sprite của nguồn tại 1 khoảnh khắc, đứng yên tại chỗ,
/// fade alpha tuyến tính về 0 (kèm co nhỏ/phóng to nếu endScaleMul != 1) rồi tự tắt
/// và trả về pool của <see cref="AfterimageBootstrap"/>.
/// Dùng cho cả speed-ghost nhân vật/xe (co 0.92) lẫn ghost-pulse công trình (phóng 1.12).
/// Ghost sống KHÔNG CHA nguồn (chỉ nằm dưới root "AfterimageGhosts" scale (1,1,1))
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
    private bool    _scaleAnim;

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

    /// <summary>Speed-ghost chuẩn theo config (nhân vật/xe). Tint mặc định của config.</summary>
    public void Snapshot(SpriteRenderer source, AfterimageConfig cfg)
    {
        if (cfg == null) return;
        Snapshot(source, cfg, cfg.tint);
    }

    /// <summary>Speed-ghost với tint riêng (Entry.tintOverride của xe cộ).</summary>
    public void Snapshot(SpriteRenderer source, AfterimageConfig cfg, Color tint)
    {
        if (cfg == null) return;
        SnapshotStyled(source,
            cfg.ghostLife, cfg.startAlpha,
            cfg.shrink ? cfg.endScaleMul : 1f,
            tint, cfg.multiplyTint, cfg.sortingOrderOffset);
    }

    /// <summary>
    /// Chụp trạng thái từ SpriteRenderer nguồn với thông số tùy ý. Gọi bởi AfterimageBootstrap.
    /// Ghost phải đang nằm dưới root scale (1,1,1) (hoặc không cha) trước khi gọi.
    /// endScaleMul &lt; 1: co (speed-ghost); &gt; 1: phóng to (pulse công trình).
    /// </summary>
    public void SnapshotStyled(SpriteRenderer source, float life, float startAlpha,
                               float endScaleMul, Color tint, bool multiplyTint, int sortingOffset)
    {
        if (source == null) return;

        SpriteRenderer sr = Renderer;
        sr.sprite = source.sprite;
        sr.flipX  = source.flipX;
        sr.flipY  = source.flipY;
        sr.sortingLayerID = source.sortingLayerID;
        sr.sortingOrder   = source.sortingOrder + sortingOffset;

        Color c = multiplyTint ? source.color * tint : tint;
        _startAlpha = Mathf.Clamp01(startAlpha) * Mathf.Clamp01(source.color.a);
        c.a = _startAlpha;
        _color = c;
        sr.color = c;

        Transform src = source.transform;
        transform.position   = src.position;
        transform.rotation   = src.rotation;
        // Không cha (root scale 1) ⇒ localScale = lossyScale cho ra đúng kích thước world,
        // bất kể nguồn nằm dưới cha scale 1 hay 100.
        transform.localScale = src.lossyScale;

        _startScale = src.lossyScale;
        _endScale   = _startScale * endScaleMul;
        _scaleAnim  = !Mathf.Approximately(endScaleMul, 1f);
        _life       = Mathf.Max(0.01f, life);
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
        if (_scaleAnim)
            transform.localScale = Vector3.Lerp(_startScale, _endScale, k);

        if (k >= 1f)
        {
            gameObject.SetActive(false);
            AfterimageBootstrap.ReturnGhost(this);
        }
    }
}
