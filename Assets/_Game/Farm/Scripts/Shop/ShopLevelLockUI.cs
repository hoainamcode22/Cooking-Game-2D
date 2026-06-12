using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Overlay lock hiển thị trên ShopItemUI khi item chưa đủ level mở khóa.
/// Thêm component này vào cùng GameObject với ShopItemUI.
///
/// Cách dùng:
///   1. Trong prefab shop item, tạo child GameObject "LockOverlay" với Image tối.
///   2. Kéo tham chiếu vào Inspector.
///   3. ShopItemUI.Setup() sẽ tự gọi Refresh() sau khi init.
/// </summary>
[RequireComponent(typeof(ShopItemUI))]
public class ShopLevelLockUI : MonoBehaviour
{
    [Header("Lock Overlay")]
    [Tooltip("Root của overlay — bật/tắt toàn bộ lock UI")]
    [SerializeField] private GameObject lockOverlayRoot;

    [Tooltip("Image icon ổ khóa (tùy chọn)")]
    [SerializeField] private Image lockIcon;

    [Tooltip("Text 'Mở ở cấp X'")]
    [SerializeField] private TextMeshProUGUI lockLevelText;

    [Header("Màu overlay")]
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.65f);

    private Image _overlayImage;
    private ShopItemUI _shopItemUI;

    private void Awake()
    {
        _shopItemUI   = GetComponent<ShopItemUI>();
        _overlayImage = lockOverlayRoot != null ? lockOverlayRoot.GetComponent<Image>() : null;

        if (_overlayImage != null)
            _overlayImage.color = overlayColor;
    }

    /// <summary>
    /// Gọi bởi ShopItemUI.Setup() sau khi item data được gán.
    /// Ẩn/hiện overlay dựa trên unlockLevel so với level người chơi hiện tại.
    /// </summary>
    public void Refresh(BaseItemData data)
    {
        if (data == null || lockOverlayRoot == null) return;

        int playerLevel = PlayerProgressManager.Instance != null
            ? PlayerProgressManager.Instance.Level
            : 1;

        // unlockLevel <= 0 hoặc <= 1 đều có nghĩa là luôn mở khóa
        int itemUnlockLevel = GetUnlockLevel(data);
        bool isLocked = itemUnlockLevel > 1 && playerLevel < itemUnlockLevel;

        lockOverlayRoot.SetActive(isLocked);

        if (isLocked)
        {
            if (lockLevelText != null)
                lockLevelText.text = $"Mở ở cấp {itemUnlockLevel}";

            // Vô hiệu hóa các nút mua để tránh bypass qua code
            if (_shopItemUI != null)
            {
                if (_shopItemUI.btnBuy   != null) _shopItemUI.btnBuy.interactable   = false;
                if (_shopItemUI.btnPlus  != null) _shopItemUI.btnPlus.interactable  = false;
                if (_shopItemUI.btnMinus != null) _shopItemUI.btnMinus.interactable = false;
            }
        }
        else
        {
            // Đảm bảo nút mua được kích hoạt lại khi mở khóa
            if (_shopItemUI != null)
            {
                if (_shopItemUI.btnBuy   != null) _shopItemUI.btnBuy.interactable   = true;
                if (_shopItemUI.btnPlus  != null) _shopItemUI.btnPlus.interactable  = true;
                if (_shopItemUI.btnMinus != null) _shopItemUI.btnMinus.interactable = true;
            }
        }
    }

    private static int GetUnlockLevel(BaseItemData item)
    {
        if (item == null) return 1;
        var field = item.GetType().GetField("unlockLevel",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(int))
            return Mathf.Max(1, (int)field.GetValue(item));
        return 1;
    }

#if UNITY_EDITOR
    [ContextMenu("Preview Lock (Editor Only)")]
    private void PreviewLock()
    {
        if (lockOverlayRoot != null) lockOverlayRoot.SetActive(true);
        if (lockLevelText   != null) lockLevelText.text = "Mở ở cấp 5";
        Debug.Log("[ShopLevelLockUI] Preview lock ON");
    }

    [ContextMenu("Preview Unlock (Editor Only)")]
    private void PreviewUnlock()
    {
        if (lockOverlayRoot != null) lockOverlayRoot.SetActive(false);
        Debug.Log("[ShopLevelLockUI] Preview lock OFF");
    }
#endif
}
