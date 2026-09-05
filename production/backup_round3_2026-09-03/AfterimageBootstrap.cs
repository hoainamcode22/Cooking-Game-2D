using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Đầu não hệ bóng mờ lưu ảnh:
/// - Feature gate §9: Resources.Load AfterimageConfig — null hoặc !enabled ⇒ return ngay,
///   không tạo object, không cài hook, game chạy y như trước.
/// - Nhân vật (shipper/thợ/khách) spawn MUỘN lúc runtime nên không quét 1 lần rồi thôi:
///   runner DontDestroyOnLoad quét lại mỗi rescanInterval giây bằng
///   Object.FindObjectsByType&lt;MonoBehaviour&gt;(FindObjectsSortMode.None) (Unity 6),
///   lọc nhanh bằng HashSet tên type + HashSet instanceID đã gắn.
/// - Pool ghost (Stack) cap theo config; sceneLoaded ⇒ clear pool (ghost thuộc scene cũ).
/// - Root "AfterimageGhosts" scale (1,1,1), KHÔNG DontDestroyOnLoad — chết theo scene,
///   pool clear đồng bộ. Ghost KHÔNG BAO GIỜ được parent vào nhân vật.
/// </summary>
public class AfterimageBootstrap : MonoBehaviour
{
    private static AfterimageBootstrap _instance;
    private static AfterimageConfig _cfg;
    private static readonly Stack<SpriteAfterimage> _pool = new Stack<SpriteAfterimage>();
    private static Transform _ghostRoot;
    private static int _ghostCount; // tổng ghost đã tạo còn thuộc scene hiện tại (sống + trong pool)

    private readonly HashSet<string> _targetNames = new HashSet<string>();
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
        Debug.Log("[Afterimage] Hệ bóng mờ BẬT — target: " +
                  string.Join(", ", _cfg.targetTypeNames ?? new string[0]));
    }

    private void Awake()
    {
        if (_cfg != null && _cfg.targetTypeNames != null)
            for (int i = 0; i < _cfg.targetTypeNames.Length; i++)
                if (!string.IsNullOrEmpty(_cfg.targetTypeNames[i]))
                    _targetNames.Add(_cfg.targetTypeNames[i]);
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

    private void Update()
    {
        if (_cfg == null) return;
        if (Time.unscaledTime < _nextScanTime) return;
        _nextScanTime = Time.unscaledTime + Mathf.Max(0.5f, _cfg.rescanInterval);
        ScanAndAttach();
    }

    private void ScanAndAttach()
    {
        MonoBehaviour[] all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        int attached = 0;
        for (int i = 0; i < all.Length; i++)
        {
            MonoBehaviour mb = all[i];
            if (mb == null) continue;
            if (!_targetNames.Contains(mb.GetType().Name)) continue;

            int id = mb.gameObject.GetInstanceID();
            if (_attachedIds.Contains(id)) continue;
            _attachedIds.Add(id);

            if (mb.GetComponent<SpriteAfterimageEmitter>() == null)
            {
                mb.gameObject.AddComponent<SpriteAfterimageEmitter>().Setup(_cfg);
                attached++;
            }
        }
        if (!_loggedFirstScan)
        {
            _loggedFirstScan = true;
            Debug.Log("[Afterimage] Quét đầu: " + all.Length + " MonoBehaviour, gắn emitter cho "
                      + attached + " nhân vật (quét lại mỗi " + _cfg.rescanInterval + "s).");
        }
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

    /// <summary>Nhả 1 ghost chụp từ SpriteRenderer nguồn. Vượt poolCap thì bỏ qua (không cấp phát thêm).</summary>
    public static void SpawnGhost(SpriteRenderer source, AfterimageConfig cfg)
    {
        if (source == null || cfg == null || source.sprite == null) return;

        SpriteAfterimage ghost = null;
        while (_pool.Count > 0)
        {
            ghost = _pool.Pop();
            if (ghost != null) break; // entry có thể đã chết theo scene
            _ghostCount = Mathf.Max(0, _ghostCount - 1);
        }

        if (ghost == null)
        {
            if (_ghostCount >= Mathf.Max(1, cfg.poolCap)) return; // cap: bỏ lượt, không phình
            GameObject go = new GameObject("AfterimageGhost");
            go.transform.SetParent(GhostRoot(), false); // root scale 1 — KHÔNG phải nhân vật
            ghost = go.AddComponent<SpriteAfterimage>();
            _ghostCount++;
        }

        ghost.Snapshot(source, cfg);
    }

    /// <summary>Ghost hết đời tự gọi về đây. Không Destroy từng con — tái dùng, tránh GC.</summary>
    public static void ReturnGhost(SpriteAfterimage ghost)
    {
        if (ghost == null) return;
        _pool.Push(ghost);
    }
}
