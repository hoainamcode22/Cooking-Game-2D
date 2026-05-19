using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Khởi tạo UI bếp khi scene load.
///
/// Nguồn dữ liệu DUY NHẤT cho số lượng slot = LeftPanelSpawner (đã spawn sẵn tất cả card).
/// CookingBoot chỉ làm 1 việc: đọc số lượng từ KitchenTransferManager rồi cập nhật
/// lên từng card theo ingredient ID — không filter, không ẩn slot nào.
/// </summary>
public class CookingBoot : MonoBehaviour
{
    [Header("Test Mode")]
    [Tooltip("True = giữ nguyên toàn bộ card do LeftPanelSpawner spawn (dùng khi test).\n" +
             "False = cập nhật số lượng từ kho Farm chuyển sang.")]
    public bool useTestData = true;

    [Header("Refs")]
    public CookingSelectionManager selection;
    public LeftPanelRefs leftRefs;

    /// <summary>
    /// Database phụ — vẫn giữ để Editor tool (ExpandCookingSlots) có thể append vào.
    /// Không còn dùng làm filter gate ở runtime nữa.
    /// </summary>
    [Header("Cooking Item Database (tham khảo, không filter runtime)")]
    public List<InventoryItemData> cookingInventoryItems = new List<InventoryItemData>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        cookingInventoryItems.RemoveAll(item => item == null);
    }
#endif

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private IEnumerator Start()
    {
        // Chờ 1 frame để LeftPanelSpawner.Start() → SpawnAll() hoàn tất trước
        yield return null;

        if (selection == null || leftRefs == null)
        {
            Debug.LogError("[CookingBoot] Thiếu reference selection hoặc leftRefs.");
            yield break;
        }

        if (!useTestData)
            SyncQuantitiesFromKitchen();
        else
            Debug.Log("[CookingBoot] Test Mode: giữ nguyên toàn bộ card từ LeftPanelSpawner.");

        // Đăng ký tất cả card vào CookingSelectionManager (sau khi quantity đã cập nhật)
        selection.RegisterAllLeftCards(
            leftRefs.ingredientsContent,
            leftRefs.seasoningsContent
        );
    }

    // ─── Core: đồng bộ số lượng từ kho bếp ──────────────────────────────────

    /// <summary>
    /// Duyệt tất cả card do LeftPanelSpawner tạo ra.
    /// Card nào có itemId trùng với item Farm chuyển qua → cập nhật số lượng thực.
    /// Card nào không được chuyển → hiển thị x0 (không ẩn).
    /// </summary>
    private void SyncQuantitiesFromKitchen()
    {
        // Build map: itemId → quantity từ KitchenTransferManager
        // KHÔNG filter qua cookingInventoryItems — dùng dữ liệu gốc từ Farm
        var transferMap = BuildTransferMap();

        Debug.Log($"[CookingBoot] Farm đã chuyển {transferMap.Count} loại nguyên liệu/gia vị vào bếp.");

        ApplyQuantitiesToCards(leftRefs.ingredientsContent, transferMap);
        ApplyQuantitiesToCards(leftRefs.seasoningsContent,  transferMap);

        // Làm mới layout để ScrollView nhận diện đúng kích thước nội dung
        RefreshLayouts();
    }

    /// <summary>
    /// Đọc tất cả item đã chuyển từ KitchenTransferManager.
    /// Key = itemId (lowercase, trimmed). Value = số lượng.
    /// </summary>
    private static Dictionary<string, int> BuildTransferMap()
    {
        var map = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

        if (KitchenTransferManager.Instance == null)
            return map;

        foreach (var kv in KitchenTransferManager.Instance.GetTransferredItems())
        {
            if (kv.Value <= 0) continue;

            string key = kv.Key?.Trim();
            if (string.IsNullOrEmpty(key)) continue;

            // Cộng dồn nếu trùng key (phòng trường hợp dữ liệu thừa)
            if (map.ContainsKey(key))
                map[key] += kv.Value;
            else
                map[key] = kv.Value;
        }

        return map;
    }

    /// <summary>
    /// Duyệt tất cả card trong contentRoot.
    /// Mỗi card lấy ID từ SelectableIngredientCard.GetItemId() rồi tra trong transferMap.
    /// Luôn SetActive(true) — không bao giờ ẩn slot.
    /// </summary>
    private void ApplyQuantitiesToCards(Transform contentRoot, Dictionary<string, int> transferMap)
    {
        if (contentRoot == null) return;

        int shown = 0;

        foreach (Transform child in contentRoot)
        {
            if (!child.TryGetComponent(out SelectableIngredientCard card)) continue;

            string cardId = card.GetItemId()?.Trim();

            // Bỏ qua sample card (không có ID và không có ingredientData)
            if (string.IsNullOrEmpty(cardId) && card.GetIngredientData() == null)
                continue;

            // Chỉ hiển thị slot khi có số lượng thực tế > 0
            int qty = (!string.IsNullOrEmpty(cardId) && transferMap.TryGetValue(cardId, out int found))
                ? found
                : 0;

            bool visible = qty > 0;
            child.gameObject.SetActive(visible);
            if (!visible) continue;

            shown++;
            card.SetQuantityFromKitchen(qty);
        }

        Debug.Log($"[CookingBoot] {contentRoot.name}: hiển thị {shown} slot.");
    }

    // ─── Layout refresh ───────────────────────────────────────────────────────

    private void RefreshLayouts()
    {
        // Buộc Canvas cập nhật ngay để ScrollView tính đúng chiều cao content
        Canvas.ForceUpdateCanvases();

        ForceRebuild(leftRefs.ingredientsContent);
        ForceRebuild(leftRefs.seasoningsContent);

        // Một lần nữa sau khi rebuild để chắc chắn
        Canvas.ForceUpdateCanvases();
    }

    private static void ForceRebuild(Transform t)
    {
        if (t == null) return;

        RectTransform rt = t as RectTransform ?? t.GetComponent<RectTransform>();
        if (rt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }
}
