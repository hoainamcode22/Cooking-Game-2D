using UnityEngine;

/// <summary>
/// GHOST-PULSE cho công trình KHÔNG di chuyển (nhà village HouseGrowthController,
/// decor DecorGrowthController — bootstrap gắn theo TÊN class, không reference cứng):
/// mỗi LateUpdate so sprite của SpriteRenderer chính với lần trước — ĐỔI SPRITE
/// (đổi stage xây / bung hộp quà) ⇒ phun 1 bóng của chính công trình phóng to
/// 1.0 → pulseScaleMul, alpha pulseAlpha → 0 trong pulseLife (tái dùng pool ghost).
/// Cách sprite-watch phủ cả 2 hệ mà không đụng event/code của DEV khác.
/// Throttle tối thiểu 0.2s giữa 2 pulse.
/// </summary>
[DisallowMultipleComponent]
public class BuildingGhostPulse : MonoBehaviour
{
    private const float MinPulseGap = 0.2f;

    private AfterimageConfig _cfg;
    private SpriteRenderer   _sr;
    private Sprite _lastSprite;
    private float  _lastPulseTime = -999f;

    /// <summary>Gọi ngay sau AddComponent (bởi AfterimageBootstrap).</summary>
    public void Setup(AfterimageConfig cfg)
    {
        _cfg = cfg;
        _sr  = FindMainRenderer();
        // Ghi nhận sprite hiện tại — KHÔNG pulse ngay lúc gắn.
        _lastSprite = _sr != null ? _sr.sprite : null;
        if (_cfg == null || _sr == null) enabled = false;
    }

    private SpriteRenderer FindMainRenderer()
    {
        SpriteRenderer[] all = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            if (all[i].GetComponent<SpriteAfterimage>() != null) continue;
            return all[i];
        }
        return null;
    }

    private void LateUpdate()
    {
        if (_cfg == null || !_cfg.buildingPulse) return;
        if (_sr == null) { enabled = false; return; }

        Sprite s = _sr.sprite;
        if (ReferenceEquals(s, _lastSprite)) return;
        _lastSprite = s;

        if (s == null || !_sr.enabled) return;
        if (Time.time - _lastPulseTime < MinPulseGap) return;

        _lastPulseTime = Time.time;
        AfterimageBootstrap.SpawnPulse(_sr, _cfg);
    }
}
