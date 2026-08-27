using UnityEngine;

/// <summary>
/// Đảm bảo KitchenTransferManager tồn tại ở MỌI scene (kể cả khi Play thẳng SampleScene).
///
/// VÌ SAO cần: manager này vốn chỉ được đặt sẵn trong SCN_Farm. Play thẳng scene bếp
/// → Instance == null → KitchenSceneV2UI.RefreshCardQuantities early-return → mọi thẻ
/// nguyên liệu kẹt "x0" và TrySelect chặn click (số lượng 0). Bootstrap này tạo manager
/// nếu chưa có; Awake của nó tự nạp save từ PlayerPrefs (KITCHEN_TRANSFER_SAVE) nên số
/// lượng hàng đã gửi từ nông trại hiện đúng. Khi vào từ Farm thì Instance đã có sẵn
/// (DontDestroyOnLoad) → bootstrap không làm gì. [Sếp 2026-08-27 — nối logic bếp mới]
/// </summary>
public static class KitchenTransferBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (KitchenTransferManager.Instance != null)
            return;

        var go = new GameObject("KitchenTransferManager (bootstrap)");
        go.AddComponent<KitchenTransferManager>(); // Awake tự SetParent(null) + DontDestroyOnLoad + LoadTransferData
    }
}
