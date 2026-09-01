using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// [JUICE T1 — 2026-08-31] Cầu nối: nghe ConstructionManager.OnConstructionComplete,
/// ghi nhớ công trình VỪA XÂY XONG; user CHẠM vào nó lần đầu → CelebrationTapFX.Play
/// (pháo hoa + confetti to, nổi trên công trình — tham chiếu video Township).
/// Tự khởi động, không cần kéo vào scene. Thuần cộng thêm — không sửa ConstructionManager.
/// </summary>
public class CelebrationTapWatcher : MonoBehaviour
{
    private const float TapRadiusCells = 2.2f;   // bán kính chạm quanh điểm neo (× CELL)
    private const float MemorySeconds  = 600f;   // nhớ công trình mới xây trong 10 phút

    private struct Entry { public Vector3 anchor; public float expireAt; }
    private readonly List<Entry> _fresh = new List<Entry>();
    private static CelebrationTapWatcher _instance;
    private ConstructionManager _subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject("CelebrationTapWatcher");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<CelebrationTapWatcher>();
    }

    private void Start() => StartCoroutine(SubscribeWhenReady());

    private IEnumerator SubscribeWhenReady()
    {
        // ConstructionManager có thể sinh sau — chờ tối đa 30s, nửa giây thử một lần.
        float deadline = Time.unscaledTime + 30f;
        while (ConstructionManager.Instance == null && Time.unscaledTime < deadline)
            yield return new WaitForSecondsRealtime(0.5f);
        var cm = ConstructionManager.Instance;
        if (cm == null) yield break;                 // scene không có xây dựng — nằm im, vô hại
        _subscribed = cm;
        cm.OnConstructionComplete += HandleComplete;
    }

    private void OnDestroy()
    {
        if (_subscribed != null) _subscribed.OnConstructionComplete -= HandleComplete;
    }

    private void HandleComplete(PlaceableItemData data, Vector3 anchor, int rotStep, int plotId)
    {
        _fresh.Add(new Entry { anchor = anchor, expireAt = Time.unscaledTime + MemorySeconds });
    }

    private void Update()
    {
        if (_fresh.Count == 0 || !Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 wp = cam.ScreenToWorldPoint(Input.mousePosition);
        wp.z = 0f;
        float radius = PlacementManager.CELL * TapRadiusCells;

        for (int i = _fresh.Count - 1; i >= 0; i--)
        {
            if (Time.unscaledTime > _fresh[i].expireAt) { _fresh.RemoveAt(i); continue; }
            Vector3 a = _fresh[i].anchor; a.z = 0f;
            if ((wp - a).sqrMagnitude <= radius * radius)
            {
                // Nổ hơi cao hơn neo một chút cho đúng "trên nóc công trình".
                CelebrationTapFX.Play(a + Vector3.up * PlacementManager.CELL * 0.8f, 1.3f);
                _fresh.RemoveAt(i);
                break;   // mỗi cú chạm ăn mừng đúng 1 công trình
            }
        }
    }
}
