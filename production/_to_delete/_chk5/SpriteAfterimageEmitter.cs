using UnityEngine;

/// <summary>
/// Gắn lên GameObject nhân vật/xe (bởi <see cref="AfterimageBootstrap"/>): nhả ghost bóng mờ
/// theo nhịp spawnInterval khi vật ĐANG DI CHUYỂN.
///
/// ── FIX 2026-09-03 (Lead): ĐO TỐC ĐỘ THEO TỪNG SpriteRenderer, KHÔNG đo ở root ──
/// Bản cũ đo `transform.position` của object mang emitter. Nhưng nhiều mover trong project
/// đứng trên object CHA bất động và di chuyển một Transform KHÁC:
///   · TrainPathFollower.cs:163 → Vector3.MoveTowards(trainRoot.position, …)
///   · FerryController / TouristBoatController: cùng pattern (script ở cha, thân xe là con)
/// ⇒ root emitter đứng yên ⇒ speed = 0 vĩnh viễn ⇒ tàu lửa/tàu thủy/phà KHÔNG BAO GIỜ có bóng
/// dù moveSpeed = 300 u/s (đúng lỗi Sếp báo). Nay mỗi SR giữ lastPos riêng và tự tính tốc độ
/// nên phủ được MỌI pattern mover: move ở root, ở con, hay Animator ghi transform.
///
/// Ngưỡng tốc độ: <see cref="AfterimageTag.minSpeedOverride"/> (nếu có tag) → nếu 0 thì
/// <see cref="AfterimageConfig.minSpeed"/>. NPC cảnh đi chậm ~20-40 u/s cần hạ ngưỡng riêng.
/// </summary>
[DisallowMultipleComponent]
public class SpriteAfterimageEmitter : MonoBehaviour
{
    private const int MaxGhostsPerBeat = 6;

    private AfterimageConfig _cfg;
    private SpriteRenderer[] _srs = System.Array.Empty<SpriteRenderer>();

    // Vị trí + cờ "đã có mốc" RIÊNG cho từng SR (song song _srs).
    private Vector3[] _lastSrPos = System.Array.Empty<Vector3>();
    private bool[]    _hasSrPos  = System.Array.Empty<bool>();

    private bool  _includeChildren;
    private bool  _useTintOverride;
    private Color _tintOverride = Color.white;
    private float _minSpeedOverride;
    private float _timer;
    private float _nextRefreshTime;

    public void Setup(AfterimageConfig cfg)
    {
        Setup(cfg, false, false, Color.white, 0f);
    }

    public void Setup(AfterimageConfig cfg, bool includeChildRenderers, bool useTintOverride, Color tintOverride)
    {
        Setup(cfg, includeChildRenderers, useTintOverride, tintOverride, 0f);
    }

    /// <param name="minSpeedOverride">&gt;0 = ngưỡng riêng (u/s); 0 = dùng config.minSpeed.</param>
    public void Setup(AfterimageConfig cfg, bool includeChildRenderers, bool useTintOverride,
                      Color tintOverride, float minSpeedOverride)
    {
        _cfg = cfg;
        _includeChildren = includeChildRenderers;
        _useTintOverride = useTintOverride;
        _tintOverride    = tintOverride;
        _minSpeedOverride = minSpeedOverride;

        // Tag trên chính object thắng mọi cấu hình khác (Sếp gắn tay cho NPC cảnh).
        AfterimageTag tag = GetComponent<AfterimageTag>();
        if (tag != null)
        {
            _includeChildren = tag.includeChildren;
            if (tag.minSpeedOverride > 0f) _minSpeedOverride = tag.minSpeedOverride;
        }

        RefreshRenderers();
        _timer = 0f;
        _nextRefreshTime = Time.unscaledTime + (_cfg != null ? Mathf.Max(0.5f, _cfg.rescanInterval) : 2f);
        if (_cfg == null || _srs.Length == 0) enabled = false;
    }

    /// <summary>Ngưỡng tốc độ đang áp dụng (u/s) — QA/tool đọc để chẩn đoán.</summary>
    public float MinSpeedInUse => _minSpeedOverride > 0f ? _minSpeedOverride
                                : (_cfg != null ? _cfg.minSpeed : 0f);

    /// <summary>Số SpriteRenderer đang theo dõi — tool Kiểm tra đọc.</summary>
    public int TrackedRendererCount => _srs != null ? _srs.Length : 0;

    /// <summary>
    /// Cache lại SR: bỏ SR nằm trên object có <see cref="SpriteAfterimage"/> (chống đệ quy).
    /// Đơn SR: chỉ giữ con đầu tiên hợp lệ. Multi: giữ tất cả con hợp lệ.
    /// Mốc vị trí của SR MỚI = vị trí hiện tại ⇒ không nhả ghost oan ở frame đầu.
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

        // Giữ lại mốc cũ của SR đã theo dõi từ trước; SR mới khởi tạo mốc = vị trí hiện tại.
        Vector3[] pos = new Vector3[n];
        bool[] has = new bool[n];
        for (int i = 0; i < n; i++)
        {
            int cu = -1;
            for (int j = 0; j < _srs.Length; j++)
                if (_srs[j] == list[i]) { cu = j; break; }

            if (cu >= 0 && cu < _lastSrPos.Length)
            {
                pos[i] = _lastSrPos[cu];
                has[i] = cu < _hasSrPos.Length && _hasSrPos[cu];
            }
            else
            {
                pos[i] = list[i] != null ? list[i].transform.position : Vector3.zero;
                has[i] = true;   // có mốc ngay ⇒ frame sau mới tính được tốc độ
            }
        }

        _srs = list; _lastSrPos = pos; _hasSrPos = has;
    }

    private void OnEnable()
    {
        // Vào lại: đặt mốc theo vị trí hiện tại để không nhả loạt ghost do delta tích tụ.
        for (int i = 0; i < _srs.Length; i++)
        {
            if (_srs[i] == null) continue;
            _lastSrPos[i] = _srs[i].transform.position;
            _hasSrPos[i] = true;
        }
        _timer = 0f;
    }

    private void Update()
    {
        if (_cfg == null) { enabled = false; return; }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        if (_includeChildren && Time.unscaledTime >= _nextRefreshTime)
        {
            _nextRefreshTime = Time.unscaledTime + Mathf.Max(0.5f, _cfg.rescanInterval);
            int song = 0;
            for (int i = 0; i < _srs.Length; i++) if (_srs[i] != null) song++;
            SpriteRenderer[] now = GetComponentsInChildren<SpriteRenderer>(false);
            int nowValid = 0;
            for (int i = 0; i < now.Length; i++)
                if (now[i] != null && now[i].GetComponent<SpriteAfterimage>() == null) nowValid++;
            if (nowValid != song) RefreshRenderers();
        }

        if (_srs.Length == 0) { enabled = false; return; }

        float nguong = MinSpeedInUse;
        float tranTeleport = nguong * 50f;

        // ── Đo tốc độ TỪNG SR + đánh dấu SR nào đủ điều kiện nhả ────────────────
        bool coAiChay = false;
        int n = _srs.Length;
        for (int i = 0; i < n; i++)
        {
            SpriteRenderer sr = _srs[i];
            if (sr == null) continue;

            Vector3 p = sr.transform.position;
            if (!_hasSrPos[i]) { _lastSrPos[i] = p; _hasSrPos[i] = true; continue; }

            float speed = Vector3.Distance(p, _lastSrPos[i]) / dt;
            _lastSrPos[i] = p;

            // Teleport/warp: không phải chạy.
            if (speed >= nguong && speed <= tranTeleport) coAiChay = true;
        }

        _timer = Mathf.Min(_timer + dt, Mathf.Max(0.01f, _cfg.spawnInterval) + dt);
        if (!coAiChay) return;
        if (_timer < _cfg.spawnInterval) return;
        _timer -= _cfg.spawnInterval;

        Color tint = _useTintOverride ? _tintOverride : _cfg.tint;
        int emitted = 0;
        for (int i = 0; i < n && emitted < MaxGhostsPerBeat; i++)
        {
            SpriteRenderer sr = _srs[i];
            if (sr == null || !sr.enabled || !sr.isVisible || sr.sprite == null) continue;
            AfterimageBootstrap.SpawnGhost(sr, _cfg, tint);
            emitted++;
        }
    }
}
