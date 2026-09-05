using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SeedPopupController : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject seedItemPrefab;

    [Header("Container")]
    [SerializeField] private Transform content;

    [Header("Data List")]
    [SerializeField] private CropData[] cropDataList;

    [Header("Item Size")]
    [SerializeField] private float itemPreferredWidth  = 120f;
    // WP-C3: 150 → 170 khớp prefab Iteam_1 mới (120x170, có tên hạt). Scene cũ vẫn có thể giữ 150 (serialize) → Start() cảnh báo.
    [SerializeField] private float itemPreferredHeight = 170f;

    /// <summary>Chiều cao ô hạt tối thiểu để không cắt chữ tên/số lượng (khớp prefab Iteam_1 sau WP-C3).</summary>
    public const float MinItemPreferredHeight = 170f;

    private bool popupInputLockHeld;

    // â”€â”€ VÃ²ng Ä‘á»i Unity â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void OnEnable()
    {
        FarmInputLock.IsSeedPopupOpen = true;
        AcquirePopupInputBlock();
        // Khay hạt (Popup_seed) / khay hoa (Popup_hoa) trải đáy màn hình y 0→240, đè lên 4 nút
        // Tab_Shop/Warehouse/Market/Cooking của Canvas_HUD (y 22→180). Khay bán trong suốt nên nút
        // lộ ra và có thể nuốt tap ⇒ ẩn hẳn hàng nút khi khay mở. Đếm tham chiếu theo `this`
        // ⇒ 2 khay (hoặc card tutorial) chồng nhau vẫn trả HUD đúng lúc chủ cuối cùng đóng.
        HudNavHider.An(this, 0f);
        SpawnAllItems();
    }

    private void Start()
    {
        // WP-C3: nếu scene còn serialize giá trị cũ (150) thì ô hạt sẽ cắt chữ → nhắc chạy tool APPLY để đồng bộ lên 170.
        if (itemPreferredHeight < MinItemPreferredHeight)
        {
            Debug.LogWarning(
                $"[SeedPopup] itemPreferredHeight={itemPreferredHeight} < {MinItemPreferredHeight} trên '{name}' — ô hạt có thể bị cắt chữ. " +
                "Chạy menu Tools/Farm/UI/Sua panel hat giong + hoa - APPLY (ghi vao scene) rồi Ctrl+S.", this);
        }
    }

    private void OnDisable()
    {
        FarmInputLock.IsSeedPopupOpen = false;
        ReleasePopupInputBlock();
        HudNavHider.Hien(this);   // trả hàng nút HUD (chỉ thật sự hiện khi không còn ai khác giữ)
    }

    private void Update()
    {
        // Re-assert mỗi frame trong khi popup đang mở, phòng code bên ngoài
        // (VD: PlantDragController.CleanupPlantDragState) clear flag sai.
        FarmInputLock.IsSeedPopupOpen = true;

        if (FarmInputLock.IsDraggingSeed) return;
        if (!InputBridge.IsPointerDownThisFrame) return;

        // [TUTORIAL] Trong lúc đang tutorial, KHÔNG cho click ngoài rìa tắt popup hạt giống
        // để tránh trường hợp người chơi bấm trượt làm mất khay hạt và kẹt tiến trình tutorial.
        if (TutorialManager.Instance != null && TutorialManager.Instance.DangChayTutorial)
        {
            return;
        }

        bool onPopup = IsPointerOnThisPopup();


        if (onPopup) return;

        FarmUIManager.Instance?.HidePlantSelectPopup();
    }

    private bool IsPointerOnThisPopup()
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = InputBridge.PointerPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            if (r.gameObject == null) continue;

            if (r.gameObject.transform.IsChildOf(transform))
                return true;
        }

        return false;
    }

    // â”€â”€ Spawn items â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void SpawnAllItems()
    {
        if (content == null)
        {
            Debug.LogError("[SeedPopup] content chưa gán!");
            return;
        }

        if (cropDataList == null || cropDataList.Length == 0)
        {
            Debug.LogWarning("[SeedPopup] cropDataList rỗng — gán CropData trong inspector SeedPopupController.");
            return;
        }

        // Tái sử dụng item nếu đã sinh rồi để không bị khựng/lag mỗi lần click mở
        if (content.childCount == cropDataList.Length)
        {
            for (int i = 0; i < cropDataList.Length; i++)
            {
                var child = content.GetChild(i);
                if (child.TryGetComponent(out CanvasGroup cg))
                {
                    cg.blocksRaycasts = true;
                }
                var item = child.GetComponent<SeedDragItem>();
                if (item != null && cropDataList[i] != null)
                {
                    item.SetData(cropDataList[i]);
                }
            }
            return;
        }

        for (int i = content.childCount - 1; i >= 0; i--)
            DestroyImmediate(content.GetChild(i).gameObject);

        if (seedItemPrefab == null)
        {
            Debug.LogError("[SeedPopup] seedItemPrefab chưa gán!");
            return;
        }

        int count = 0;
        foreach (var data in cropDataList)
        {
            if (data == null) { Debug.LogWarning("[SeedPopup] Slot null trong cropDataList."); continue; }

            GameObject go = Instantiate(seedItemPrefab, content);

            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredWidth  = itemPreferredWidth;
            le.preferredHeight = itemPreferredHeight;

            SeedDragItem item = go.GetComponent<SeedDragItem>();
            if (item != null)
            {
                item.SetData(data);
                count++;
            }
            else
            {
                Debug.LogError($"[SeedPopup] Prefab thiếu SeedDragItem!");
            }
        }
    }

    private void AcquirePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(gameObject, true);

        if (popupInputLockHeld) return;
        FarmInputLock.RegisterPopupOpen();
        popupInputLockHeld = true;
    }

    private void ReleasePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(gameObject, false);

        if (!popupInputLockHeld) return;
        FarmInputLock.RegisterPopupClose();
        popupInputLockHeld = false;
    }
}
