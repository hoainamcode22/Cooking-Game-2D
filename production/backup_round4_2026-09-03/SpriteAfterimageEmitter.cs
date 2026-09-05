using UnityEngine;

/// <summary>
/// Gắn lên GameObject nhân vật/xe (bởi <see cref="AfterimageBootstrap"/>):
/// đo tốc độ world ở root mỗi frame, vượt ngưỡng thì nhả ghost bóng mờ theo nhịp spawnInterval.
/// Chế độ multi-SR (includeChildRenderers — tàu lửa đầu+toa, tàu thủy thân+buồm):
/// cache danh sách SR con lúc Setup, refresh theo chu kỳ nếu số lượng đổi,
/// mỗi nhịp nhả ghost cho TỐI ĐA 6 SR đang nhìn thấy (isVisible).
/// Không cấp phát mỗi frame ngoài lần refresh cache; mục tiêu bị Destroy ⇒ emitter chết cùng.
/// </summary>
[DisallowMultipleComponent]
public class SpriteAfterimageEmitter : MonoBehaviour
{
    private const int MaxGhostsPerBeat = 6;

    private AfterimageConfig _cfg;
    private SpriteRenderer[] _srs = System.Array.Empty<SpriteRenderer>();
    private bool    _includeChildren;
    private bool    _useTintOverride;
    private Color   _tintOverride = Color.white;
    private Vector3 _lastPos;
    private float   _timer;
    private bool    _hasLastPos;
    private float   _nextRefreshTime;

    /// <summary>Setup mặc định: 1 SpriteRenderer chính, tint chung của config.</summary>
    public void Setup(AfterimageConfig cfg)
    {
        Setup(cfg, false, false, Color.white);
    }

    /// <summary>Setup đầy đủ theo Entry của config (xe cộ nhiều SR con, tint riêng).</summary>
    public void Setup(AfterimageConfig cfg, bool includeChildRenderers, bool useTintOverride, Color tintOverride)
    {
        _cfg = cfg;
        _includeChildren = includeChildRenderers;
        _useTintOverride = useTintOverride;
        _tintOverride    = tintOverride;
        RefreshRenderers();
        _lastPos = transform.position;
        _hasLastPos = true;
        _timer = 0f;
        _nextRefreshTime = Time.unscaledTime + (_cfg != null ? Mathf.Max(0.5f, _cfg.rescanInterval) : 2f);
        if (_cfg == null || _srs.Length == 0) enabled = false;
    }

    /// <summary>
    /// Cache lại SR: bỏ SR nằm trên object có <see cref="SpriteAfterimage"/> (chống đệ quy).
    /// Đơn SR: chỉ giữ con đầu tiên hợp lệ. Multi: giữ tất cả con hợp lệ.
    /// </summary>
    public void RefreshRenderers()
    {
        SpriteRenderer[] all = GetComponentsInChildren<SpriteRenderer>(false);
        int n = 0;
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].GetComponent<SpriteAfterimage>() == null) n++;

        if (!_includeChildren) n = Mathf.Min(n, 1);
        SpriteRenderer[] list = new SpriteRenderer[n];
        int w = 0;
        for (int i = 0; i < all.Length && w < n; i++)
        {
            if (all[i] == null || all[i].GetComponent<SpriteAfterimage>() != null) continue;
            list[w++] = all[i];
        }
        _srs = list;
    }

    private void OnEnable()
    {
        _lastPos = transform.position;
        _hasLastPos = true;
        _timer = 0f;
    }

    private void Update()
    {
        if (_cfg == null) { enabled = false; return; }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Multi-SR: toa tàu / buồm có thể được gắn thêm-bớt lúc runtime — refresh cache theo chu kỳ.
        if (_includeChildren && Time.unscaledTime >= _nextRefreshTime)
        {
            _nextRefreshTime = Time.unscaledTime + Mathf.Max(0.5f, _cfg.rescanInterval);
            int liveCount = 0;
            for (int i = 0; i < _srs.Length; i++) if (_srs[i] != null) liveCount++;
            SpriteRenderer[] now = GetComponentsInChildren<SpriteRenderer>(false);
            int nowValid = 0;
            for (int i = 0; i < now.Length; i++)
                if (now[i] != null && now[i].GetComponent<SpriteAfterimage>() == null) nowValid++;
            if (nowValid != liveCount) RefreshRenderers();
        }

        if (_srs.Length == 0) { enabled = false; return; } // mất renderer (child bị destroy)

        Vector3 pos = transform.position;
        if (!_hasLastPos) { _lastPos = pos; _hasLastPos = true; return; }

        float speed = Vector3.Distance(pos, _lastPos) / dt;
        _lastPos = pos;

        // Cộng dồn timer nhưng KẸP TRẦN = spawnInterval + dt (giữ phần dư carry):
        // đứng im lâu rồi mới chạy thì chỉ nhả 1 nhịp ngay, không xả loạt dồn.
        _timer = Mathf.Min(_timer + dt, Mathf.Max(0.01f, _cfg.spawnInterval) + dt);

        if (speed < _cfg.minSpeed) return;

        // Dịch chuyển tức thời (teleport/warp qua map) — không phải chạy, bỏ qua.
        if (speed > _cfg.minSpeed * 50f) { _timer = 0f; return; }

        if (_timer < _cfg.spawnInterval) return;

        // Trừ interval (giữ phần dư) thay vì reset 0 — nhịp trung bình đúng bằng spawnInterval,
        // không bị làm tròn lên theo biên frame (0.07s không thành 0.083s ở 60fps).
        _timer -= _cfg.spawnInterval;

        Color tint = _useTintOverride ? _tintOverride : _cfg.tint;
        int emitted = 0;
        for (int i = 0; i < _srs.Length && emitted < MaxGhostsPerBeat; i++)
        {
            SpriteRenderer sr = _srs[i];
            if (sr == null || !sr.enabled || !sr.isVisible || sr.sprite == null) continue;
            AfterimageBootstrap.SpawnGhost(sr, _cfg, tint);
            emitted++;
        }
    }
}
