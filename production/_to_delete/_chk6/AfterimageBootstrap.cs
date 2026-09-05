using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Đầu não hệ bóng mờ lưu ảnh:
/// - Feature gate §9: Resources.Load AfterimageConfig — null hoặc !enabled ⇒ return ngay,
///   không tạo object, không cài hook, game chạy y như trước.
/// - Mục tiêu (shipper/thợ/khách/xe cộ) spawn MUỘN lúc runtime nên không quét 1 lần rồi thôi:
///   runner DontDestroyOnLoad quét lại mỗi rescanInterval giây bằng
///   Object.FindObjectsByType&lt;MonoBehaviour&gt;(FindObjectsSortMode.None) (Unity 6),
///   lọc nhanh bằng Dictionary tên type + HashSet instanceID đã gắn.
/// - Nhận 3 loại mục tiêu: Entry theo tên class (targetEntries + legacy targetTypeNames),
///   marker <see cref="AfterimageTag"/> (NPC cảnh không có script riêng),
///   và công trình (buildingTypeNames) — gắn <see cref="BuildingGhostPulse"/> thay emitter.
/// - Pool ghost (Stack) cap theo config; sceneLoaded ⇒ clear pool (ghost thuộc scene cũ).
/// - Root "AfterimageGhosts" scale (1,1,1), KHÔNG DontDestroyOnLoad — chết theo scene,
///   pool clear đồng bộ. Ghost KHÔNG BAO GIỜ được parent vào nhân vật/xe/công trình.
/// </summary>
public class AfterimageBootstrap : MonoBehaviour
{
    private static AfterimageBootstrap _instance;
    private static AfterimageConfig _cfg;
    private static readonly Stack<SpriteAfterimage> _pool = new Stack<SpriteAfterimage>();
    private static Transform _ghostRoot;
    private static int _ghostCount; // tổng ghost đã tạo còn thuộc scene hiện tại (sống + trong pool)

    private readonly Dictionary<string, AfterimageConfig.Entry> _targets =
        new Dictionary<string, AfterimageConfig.Entry>();
    private readonly HashSet<string> _buildingNames = new HashSet<string>();
    private readonly HashSet<int> _attachedIds = new HashSet<int>();
    private float _nextScanTime;
    private bool  _loggedFirstScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (_instance != null) return;

        _cfg = Resources.Load<AfterimageConfig>("AfterimageConfig");
        if (_cfg == null || !_cfg.enabled) return; // gate: chưa ★ SETUP hoặc đã TẮT ⇒ hệ không sống

        GameObject go = new GameObject("AfterimageBootstrap");
        Object.DontDestroyOnLoad(go);
        _instance = go.AddComponent<AfterimageBootstrap>();
        SceneManager.sceneLoaded += OnSceneLoaded; // chỉ cài khi hệ bật — không rò rỉ khi tắt
    }

    private void Awake()
    {
        if (_cfg == null) return;

        if (_cfg.targetEntries != null)
            for (int i = 0; i < _cfg.targetEntries.Length; i++)
            {
                AfterimageConfig.Entry e = _cfg.targetEntries[i];
                if (e != null && !string.IsNullOrEmpty(e.typeName) && !_targets.ContainsKey(e.typeName))
                    _targets.Add(e.typeName, e);
            }

        // LEGACY: mảng tên trần của asset cũ — coi như Entry mặc định (đơn SR, tint chung).
        if (_cfg.targetTypeNames != null)
            for (int i = 0; i < _cfg.targetTypeNames.Length; i++)
            {
                string n = _cfg.targetTypeNames[i];
                if (!string.IsNullOrEmpty(n) && !_targets.ContainsKey(n))
                    _targets.Add(n, new AfterimageConfig.Entry { typeName = n });
            }

        if (_cfg.buildingPulse && _cfg.buildingTypeNames != null)
            for (int i = 0; i < _cfg.buildingTypeNames.Length; i++)
                if (!string.IsNullOrEmpty(_cfg.buildingTypeNames[i]))
                    _buildingNames.Add(_cfg.buildingTypeNames[i]);

        Debug.Log("[Afterimage] Hệ bóng mờ BẬT — " + _targets.Count + " type mục tiêu, "
                  + _buildingNames.Count + " type công trình pulse, + marker AfterimageTag.");
        _nextScanTime = 0f; // quét ngay frame đầu
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ghost + root thuộc scene cũ đã bị hủy — bỏ mọi tham chiếu chết.
        _pool.Clear();
        _ghostRoot = null;
        _ghostCount = 0;
        if (_instance != null)
        {
            _instance._attachedIds.Clear();
            _instance._nextScanTime = 0f; // scene mới: quét lại ngay
        }
    }

    // [TỐI ƯU 2026-09-03 — Sếp báo "kéo map không mượt như ngày xưa"]
    // Nguyên nhân: FindObjectsByType<MonoBehaviour> quét TOÀN scene mỗi 2 giây.
    // SCN_Farm có 1.517 MonoBehaviour trong file scene (chưa kể decor/thợ/ghost sinh lúc
    // chạy) ⇒ mỗi lần quét cấp phát mảng ~2.000 phần tử + duyệt + type-check ⇒ giật nhẹ
    // ĐỊNH KỲ, đúng cảm giác kéo map bị rít.
    // Cách chữa: rescanInterval 2s → 10s, CỘNG backoff — quét mà không thấy mục tiêu mới
    // thì giãn dần ×1.5 tới trần 30s. Xe cộ/NPC cảnh có sẵn từ lúc load nên vài lượt quét
    // đầu là đủ; thợ xây KHÔNG phụ thuộc vòng quét này (đi qua event OnControllerSpawned).
    private const float ScanBackoffMax = 30f;
    private float _scanBackoff;

    private void Update()
    {
        if (_cfg == null) return;
        if (Time.unscaledTime < _nextScanTime) return;

        float baseInterval = Mathf.Max(0.5f, _cfg.rescanInterval);
        if (_scanBackoff < baseInterval) _scanBackoff = baseInterval;

        int truoc = _attachedIds.Count;
        ScanAndAttach();
        bool coMoi = _attachedIds.Count > truoc;

        // Thấy mục tiêu mới ⇒ về nhịp gốc (phản ứng nhanh khi Sếp mua công trình/đặt NPC).
        // Không thấy gì ⇒ giãn ra để thôi ngốn CPU trên scene đã ổn định.
        _scanBackoff = coMoi ? baseInterval : Mathf.Min(_scanBackoff * 1.5f, ScanBackoffMax);
        _nextScanTime = Time.unscaledTime + _scanBackoff;
    }

    private void ScanAndAttach()
    {
        MonoBehaviour[] all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        int emitters = 0, pulses = 0;
        for (int i = 0; i < all.Length; i++)
        {
            MonoBehaviour mb = all[i];
            if (mb == null) continue;

            // 1) Marker cho NPC cảnh không có script riêng (bà lão/quân nhân/nhân viên tàu).
            if (mb is AfterimageTag tag)
            {
                if (MarkAttached(tag.gameObject) && tag.GetComponent<SpriteAfterimageEmitter>() == null)
                {
                    tag.gameObject.AddComponent<SpriteAfterimageEmitter>()
                       .Setup(_cfg, tag.includeChildren, false, Color.white);
                    emitters++;
                }
                continue;
            }

            string typeName = mb.GetType().Name;

            // 2) Nhân vật/xe theo Entry.
            AfterimageConfig.Entry entry;
            if (_targets.TryGetValue(typeName, out entry))
            {
                if (MarkAttached(mb.gameObject) && mb.GetComponent<SpriteAfterimageEmitter>() == null)
                {
                    mb.gameObject.AddComponent<SpriteAfterimageEmitter>()
                      .Setup(_cfg, entry.includeChildRenderers, entry.useTintOverride, entry.tintOverride);
                    emitters++;
                }
                continue;
            }

            // 3) Công trình (nhà village/decor) — pulse lúc đổi sprite stage.
            if (_buildingNames.Contains(typeName))
            {
                if (MarkAttached(mb.gameObject) && mb.GetComponent<BuildingGhostPulse>() == null)
                {
                    mb.gameObject.AddComponent<BuildingGhostPulse>().Setup(_cfg);
                    pulses++;
                }
            }
        }
        if (!_loggedFirstScan)
        {
            _loggedFirstScan = true;
            Debug.Log("[Afterimage] Quét đầu: " + all.Length + " MonoBehaviour — gắn " + emitters +
                      " emitter, " + pulses + " building-pulse (quét lại mỗi " + _cfg.rescanInterval + "s).");
        }
    }

    /// <summary>true nếu GameObject này CHƯA được xử lý (và đánh dấu luôn).</summary>
    private bool MarkAttached(GameObject go)
    {
        int id = go.GetInstanceID();
        if (_attachedIds.Contains(id)) return false;
        _attachedIds.Add(id);
        return true;
    }

    private static Transform GhostRoot()
    {
        if (_ghostRoot == null)
        {
            GameObject root = new GameObject("AfterimageGhosts");
            root.transform.position = Vector3.zero;
            root.transform.localScale = Vector3.one; // scale (1,1,1) — ghost giữ đúng lossyScale
            _ghostRoot = root.transform;             // KHÔNG DontDestroyOnLoad: chết theo scene
        }
        return _ghostRoot;
    }

    /// <summary>Lấy ghost từ pool (hoặc tạo mới nếu chưa chạm poolCap). null = cap đầy, bỏ lượt.</summary>
    private static SpriteAfterimage ObtainGhost(AfterimageConfig cfg)
    {
        SpriteAfterimage ghost = null;
        while (_pool.Count > 0)
        {
            ghost = _pool.Pop();
            if (ghost != null) break; // entry có thể đã chết theo scene
            _ghostCount = Mathf.Max(0, _ghostCount - 1);
        }

        if (ghost == null)
        {
            if (_ghostCount >= Mathf.Max(1, cfg.poolCap)) return null; // cap: bỏ lượt, không phình
            GameObject go = new GameObject("AfterimageGhost");
            go.transform.SetParent(GhostRoot(), false); // root scale 1 — KHÔNG phải nhân vật
            ghost = go.AddComponent<SpriteAfterimage>();
            _ghostCount++;
        }
        return ghost;
    }

    /// <summary>Nhả 1 speed-ghost với tint mặc định của config.</summary>
    public static void SpawnGhost(SpriteRenderer source, AfterimageConfig cfg)
    {
        if (cfg == null) return;
        SpawnGhost(source, cfg, cfg.tint);
    }

    /// <summary>Nhả 1 speed-ghost với tint chỉ định (Entry.tintOverride của xe cộ).</summary>
    public static void SpawnGhost(SpriteRenderer source, AfterimageConfig cfg, Color tint)
    {
        if (source == null || cfg == null || source.sprite == null) return;
        SpriteAfterimage ghost = ObtainGhost(cfg);
        if (ghost == null) return;
        ghost.Snapshot(source, cfg, tint);
    }

    /// <summary>Nhả 1 ghost-pulse công trình: phóng to 1.0→pulseScaleMul, alpha pulseAlpha→0 trong pulseLife.</summary>
    public static void SpawnPulse(SpriteRenderer source, AfterimageConfig cfg)
    {
        if (source == null || cfg == null || source.sprite == null) return;
        SpriteAfterimage ghost = ObtainGhost(cfg);
        if (ghost == null) return;
        // Tint trắng multiply — pulse là bóng CỦA CHÍNH công trình, không nhuộm lạnh.
        ghost.SnapshotStyled(source, cfg.pulseLife, cfg.pulseAlpha, cfg.pulseScaleMul,
                             Color.white, true, cfg.sortingOrderOffset);
    }

    /// <summary>Ghost hết đời tự gọi về đây. Không Destroy từng con — tái dùng, tránh GC.</summary>
    public static void ReturnGhost(SpriteAfterimage ghost)
    {
        if (ghost == null) return;
        _pool.Push(ghost);
    }
}
