using System.Collections;
using UnityEngine;

/// <summary>
/// MỘT CON THỢ BÚA — đập búa lúc đang xây, đứng im lúc hộp quà, nhảy lúc ăn mừng.
/// ═══════════════════════════════════════════════════════════════════════════════
///
/// KHÔNG tự tìm công trình, KHÔNG tự quyết mode. <see cref="BuilderWorkerCrew"/> hoặc
/// <see cref="HouseWorkerBridge"/> ra lệnh. Đây là "diễn viên", không phải "đạo diễn".
///
/// BỐN MODE (§6 CONTRACT):
///   Hidden      — renderer tắt, không tốn gì
///   Hammering   — hammerFrames @ hammerFps loop, lệch pha riêng; frame 8/9/10 bắn bụi + SFX
///   IdleAtGift  — ĐỔI sang celebrateFrames rồi ĐỨNG IM ở frame celebrateIdleFrameIndex
///   Celebrating — celebrateFrames @ celebrateFps loop
///
/// SORTING (§2 CONTRACT — sai là thợ biến mất sau nhà):
///   layer resolve qua <see cref="TouristSortingLayers"/> (KHÔNG hardcode "CongTrinh"),
///   order = Clamp(5000 - Round(y * 0.5f) + extraOrder, -8000, 8000), chỉ tính lại khi
///   vị trí THẬT SỰ đổi (không mỗi frame).
///
/// TỈ LỆ: sprite 300×298.667 px, PPU 100 ⇒ bounds.size.y ≈ 2.99 unit. Muốn thợ cao
/// 170 unit (bằng khách du lịch) thì scale ≈ 57. Con số to là BÌNH THƯỜNG với map này
/// (1 ô lưới = 100 unit).
///
/// [Worker]
/// </summary>
[DisallowMultipleComponent]
public class BuilderWorker : MonoBehaviour
{
    /// <summary>Trạng thái diễn của một con thợ.</summary>
    public enum WorkerMode
    {
        Hidden,
        Hammering,
        IdleAtGift,
        Celebrating
    }

    // ── Sorting (§2 CONTRACT) ────────────────────────────────────────────────
    private const int   BASE_ORDER   = 5000;
    private const float Y_SORT_FACTOR = 0.5f;
    private const int   Y_SORT_CLAMP = 8000;

    /// <summary>
    /// Chặn spam SFX cho TOÀN BỘ crew: 3 thợ × 3 frame impact × ~0.83 vòng/giây
    /// = ~7.5 tiếng búa/giây nếu không chặn → nghe như súng máy. Static nên dùng chung.
    /// </summary>
    private const float SFX_MIN_GAP = 0.25f;
    private static float _lastSfxUnscaledTime = -999f;

    private BuilderWorkerConfig  _cfg;
    private SpriteRenderer       _sr;
    private SpriteSequencePlayer _player;

    private int   _workerIndex;
    private bool  _faceLeft;
    private float _phase01;

    private WorkerMode _mode = WorkerMode.Hidden;

    private float _desiredHeight = 0f;
    private bool  _scaleApplied;

    private string _layerName = "Default";
    private int    _extraOrder;
    private Vector3 _lastSortPos = new Vector3(float.NaN, float.NaN, float.NaN);

    private Coroutine _fadeCo;
    private bool _dyingOut;
    private bool _frameHookAttached;

    /// <summary>Mode hiện tại.</summary>
    public WorkerMode Mode => _mode;

    // ── Vòng đời ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        EnsureComponents();
    }

    private void LateUpdate()
    {
        // Sprite có thể tới muộn (tool đổ frame sau, prefab load async) → thử lại
        if (!_scaleApplied) ApplyScaleIfPossible();

        UpdateYSort(false);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        DetachFrameHook();
    }

    // ── API công khai ────────────────────────────────────────────────────────

    /// <summary>
    /// Nạp cấu hình cho con thợ này. Gọi NGAY sau khi Instantiate, TRƯỚC
    /// <see cref="SetMode"/> và <see cref="PlaceAt"/>.
    /// </summary>
    /// <param name="cfg">Cấu hình chung (null ⇒ bỏ qua êm, thợ nằm im ở Hidden).</param>
    /// <param name="workerIndex">Số thứ tự trong crew (0..2) — dùng để log và tie-break.</param>
    /// <param name="faceLeft">true = lật ngang (sheet vẽ nhìn sang PHẢI).</param>
    /// <param name="phaseOffset01">Lệch pha 0..1 để không đập cùng nhịp với đồng nghiệp.</param>
    public void Setup(BuilderWorkerConfig cfg, int workerIndex, bool faceLeft, float phaseOffset01)
    {
        EnsureComponents();

        _cfg         = cfg;
        _workerIndex = workerIndex;
        _phase01     = Mathf.Repeat(phaseOffset01, 1f);

        SetFaceLeft(faceLeft);

        if (cfg != null) _desiredHeight = cfg.workerWorldHeight;
        _scaleApplied = false;

        AttachFrameHook();

        // Bắt đầu ở Hidden: người điều phối (Crew/Bridge) sẽ ra lệnh mode thật.
        _mode = WorkerMode.Hidden;
        if (_sr != null) _sr.enabled = false;
        if (_player != null) _player.Stop();
    }

    /// <summary>Đổi mode diễn. Gọi lại cùng mode = không làm gì (không reset animation).</summary>
    public void SetMode(WorkerMode mode)
    {
        if (_dyingOut) return;
        if (_mode == mode) return;

        WorkerMode prev = _mode;
        _mode = mode;

        EnsureComponents();

        if (mode == WorkerMode.Hidden)
        {
            if (_player != null) _player.Stop();
            if (_sr != null) _sr.enabled = false;
            return;
        }

        if (_cfg == null)
        {
            // Không có cấu hình thì không có frame nào để diễn — nằm im, đừng crash.
            if (_sr != null) _sr.enabled = false;
            return;
        }

        HienRaVaFadeNeuCan(prev);

        // ── NGUỒN THỜI GIAN THEO MODE (bug A6) ──────────────────────────────────
        // Hammering  → Time.deltaTime  : đập búa là hoạt động GAMEPLAY. Mở popup làm
        //              timeScale = 0 thì công trình dừng tiến độ, thợ dừng theo mới đúng.
        // IdleAtGift /
        // Celebrating → Time.unscaledDeltaTime : đây là FX ăn mừng, chạy song song với
        //              ConstructionCelebrationFX (WaitForSecondsRealtime) và với
        //              HouseWorkerBridge.AnMungRoiRutQuan (unscaledDeltaTime). Nếu để
        //              deltaTime thì popup đặt timeScale = 0 sẽ cho ra cảnh "pháo hoa vẫn
        //              nổ mà thợ đứng chết cứng", rồi hết 3.5s thợ fade mất mà chưa nhảy
        //              phát nào — đúng kịch bản QA A6.
        _player.useUnscaledTime = (mode != WorkerMode.Hammering);

        switch (mode)
        {
            case WorkerMode.Hammering:
                if (!CoFrame(_cfg.hammerFrames)) { CanhBaoThieuFrame("hammerFrames"); return; }
                _player.SetFrames(_cfg.hammerFrames, _cfg.hammerFps, true, false);
                _player.SetPhaseOffset01(_phase01);
                _player.Play();
                break;

            case WorkerMode.IdleAtGift:
                if (!CoFrame(_cfg.celebrateFrames)) { CanhBaoThieuFrame("celebrateFrames"); return; }
                // ĐỔI sheet trước, RỒI đứng im ở frame chỉ định (§5.3 — frame 0 = đứng thẳng)
                _player.SetFrames(_cfg.celebrateFrames, _cfg.celebrateFps, true, false);
                _player.PauseAtFrame(_cfg.celebrateIdleFrameIndex);
                break;

            case WorkerMode.Celebrating:
                if (!CoFrame(_cfg.celebrateFrames)) { CanhBaoThieuFrame("celebrateFrames"); return; }
                _player.SetFrames(_cfg.celebrateFrames, _cfg.celebrateFps, true, false);
                _player.SetPhaseOffset01(_phase01);
                _player.Play();
                break;
        }

        _scaleApplied = false;      // sheet mới có thể khác kích thước ô
        ApplyScaleIfPossible();
    }

    /// <summary>
    /// Đặt thợ vào world + chốt sorting layer. <paramref name="sortingOrder"/> là
    /// ĐỘ LỆCH cộng thêm vào y-sort (crew truyền 0,1,2 để 3 thợ không tranh nhau order).
    /// </summary>
    public void PlaceAt(Vector3 worldPos, string sortingLayer, int sortingOrder)
    {
        EnsureComponents();

        transform.position = worldPos;
        _extraOrder = sortingOrder;

        // KHÔNG hardcode tên layer — §2/§7 CONTRACT (bug "CongTrinh" im lặng về Default)
        _layerName = TouristSortingLayers.ResolveOrOverride(sortingLayer, TouristSortingLayers.Visitor);

        if (_sr != null) _sr.sortingLayerName = _layerName;

        UpdateYSort(true);
    }

    /// <summary>Mờ dần rồi tự Destroy. Gọi nhiều lần cũng chỉ chạy một lần.</summary>
    public void FadeOutAndDestroy()
    {
        if (_dyingOut) return;
        _dyingOut = true;

        float dur = _cfg != null ? _cfg.fadeOutSeconds : 0.35f;

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeRoutine(AlphaHienTai(), 0f, dur, true));
    }

    /// <summary>
    /// Scale cần thiết để thợ cao đúng <paramref name="desiredHeight"/> world unit.
    /// Tự động bù trừ scale của công trình cha (host) để kích thước trong game luôn bằng nhân vật shipper/khách.
    /// </summary>
    public float ScaleForWorldHeight(float desiredHeight)
    {
        Sprite s = FrameDauTien();
        if (s == null) return 1f;

        float h = s.bounds.size.y;
        if (h <= 0.0001f) return 1f;

        float parentLossyY = (transform.parent != null && Mathf.Abs(transform.parent.lossyScale.y) > 0.0001f)
            ? Mathf.Abs(transform.parent.lossyScale.y)
            : 1f;

        return (desiredHeight / h) / parentLossyY;
    }

    /// <summary>Lật ngang. Sheet vẽ thợ nhìn sang PHẢI nên faceLeft ⇒ flipX = true.</summary>
    public void SetFaceLeft(bool faceLeft)
    {
        _faceLeft = faceLeft;
        if (_sr != null) _sr.flipX = faceLeft;
    }

    // ── Nội bộ ───────────────────────────────────────────────────────────────

    private void EnsureComponents()
    {
        if (_sr == null)
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
        }

        if (_player == null)
        {
            _player = GetComponent<SpriteSequencePlayer>();
            if (_player == null) _player = gameObject.AddComponent<SpriteSequencePlayer>();
        }

        _player.target       = _sr;
        _player.playOnEnable = false;              // mode do Crew điều khiển, không tự chạy

        // useUnscaledTime KHÔNG chốt ở đây nữa — nó phụ thuộc MODE, xem SetMode() (bug A6).
        // Giá trị khởi tạo false chỉ là mặc định an toàn cho lúc chưa có mode nào.
        _player.useUnscaledTime = false;

        if (_sr != null) _sr.flipX = _faceLeft;
    }

    private void AttachFrameHook()
    {
        if (_frameHookAttached || _player == null) return;
        _player.OnFrameEntered += HandleFrameEntered;
        _frameHookAttached = true;
    }

    private void DetachFrameHook()
    {
        if (!_frameHookAttached || _player == null) return;
        _player.OnFrameEntered -= HandleFrameEntered;
        _frameHookAttached = false;
    }

    private void HandleFrameEntered(SpriteSequencePlayer p, int frame)
    {
        if (_mode != WorkerMode.Hammering) return;
        if (_cfg == null || _dyingOut) return;
        if (!LaFrameChamDat(frame)) return;

        SpawnDust();
        TryPlayHammerSfx();
    }

    private bool LaFrameChamDat(int frame)
    {
        int[] ds = _cfg.hammerImpactFrames;
        if (ds == null) return false;

        for (int i = 0; i < ds.Length; i++)
            if (ds[i] == frame) return true;

        return false;
    }

    private void SpawnDust()
    {
        if (_cfg.dustVfxPrefab == null) return;   // nullable — thiếu thì bỏ qua êm

        // Pivot sprite nhân vật là Bottom-Center (§2) ⇒ transform.position CHÍNH LÀ chân
        GameObject fx = Instantiate(_cfg.dustVfxPrefab, transform.position, Quaternion.identity);
        Destroy(fx, 2f);                          // phòng prefab không tự dọn → rác scene
    }

    private void TryPlayHammerSfx()
    {
        float now = Time.unscaledTime;
        if (now - _lastSfxUnscaledTime < SFX_MIN_GAP) return;   // static: chặn cho CẢ crew
        _lastSfxUnscaledTime = now;

        if (_cfg != null && _cfg.hammerSfx != null)
        {
            AudioSource.PlayClipAtPoint(_cfg.hammerSfx, transform.position,
                                        Mathf.Clamp01(_cfg.hammerSfxVolume));
        }
        else
        {
            AudioManager.Instance?.PlayBuildingHammer();
        }
    }

    private Sprite FrameDauTien()
    {
        if (_sr != null && _sr.sprite != null) return _sr.sprite;

        if (_cfg != null)
        {
            if (CoFrame(_cfg.hammerFrames))    return _cfg.hammerFrames[0];
            if (CoFrame(_cfg.celebrateFrames)) return _cfg.celebrateFrames[0];
        }

        return null;
    }

    private void ApplyScaleIfPossible()
    {
        if (_desiredHeight <= 0f) return;
        if (FrameDauTien() == null) return;

        float s = ScaleForWorldHeight(_desiredHeight);
        transform.localScale = new Vector3(s, s, 1f);
        _scaleApplied = true;
    }

    private void UpdateYSort(bool force)
    {
        if (_sr == null) return;

        Vector3 p = transform.position;
        if (!force)
        {
            float dx = p.x - _lastSortPos.x;
            float dy = p.y - _lastSortPos.y;
            if (dx * dx + dy * dy < 0.01f) return;   // đứng yên → không tính lại
        }

        _lastSortPos = p;
        _sr.sortingOrder = Mathf.Clamp(
            BASE_ORDER - Mathf.RoundToInt(p.y * Y_SORT_FACTOR) + _extraOrder,
            -Y_SORT_CLAMP, Y_SORT_CLAMP);
    }

    private void HienRaVaFadeNeuCan(WorkerMode prev)
    {
        if (_sr == null) return;

        bool vuaHienRa = (prev == WorkerMode.Hidden) || !_sr.enabled;
        _sr.enabled = true;

        if (!vuaHienRa) return;

        float dur = _cfg != null ? _cfg.fadeInSeconds : 0.25f;
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeRoutine(0f, 1f, dur, false));
    }

    private float AlphaHienTai()
    {
        return _sr != null ? _sr.color.a : 1f;
    }

    /// <summary>Fade alpha bằng Time.unscaledDeltaTime (§0.6 CONTRACT — FX không bị timeScale khoá).</summary>
    private IEnumerator FadeRoutine(float from, float to, float duration, bool destroyAtEnd)
    {
        if (_sr == null)
        {
            if (destroyAtEnd) Destroy(gameObject);
            yield break;
        }

        SetAlpha(from);

        if (duration > 0.001f)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                if (_sr == null) yield break;                 // bị Destroy giữa chừng
                SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
                yield return null;
            }
        }

        SetAlpha(to);
        _fadeCo = null;

        if (destroyAtEnd) Destroy(gameObject);
    }

    private void SetAlpha(float a)
    {
        if (_sr == null) return;
        Color c = _sr.color;
        c.a = Mathf.Clamp01(a);
        _sr.color = c;
    }

    private static bool CoFrame(Sprite[] arr)
    {
        return arr != null && arr.Length > 0 && arr[0] != null;
    }

    private void CanhBaoThieuFrame(string tenMang)
    {
        Debug.LogWarning($"[Worker] Thợ #{_workerIndex} không có '{tenMang}' trong BuilderWorkerConfig — " +
                         "bỏ qua mode này. Chạy Editor Tool cắt spritesheet nhân vật để đổ frame.");
        if (_sr != null) _sr.enabled = false;
    }
}
