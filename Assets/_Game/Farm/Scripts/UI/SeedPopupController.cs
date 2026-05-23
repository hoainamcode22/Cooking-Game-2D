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
    [SerializeField] private float itemPreferredHeight = 150f;

    private bool popupInputLockHeld;

    // ── Vòng đời Unity ───────────────────────────────────────────────────────

    private void OnEnable()
    {
        FarmInputLock.IsSeedPopupOpen = true;
        AcquirePopupInputBlock();
        SpawnAllItems();
    }

    private void OnDisable()
    {
        FarmInputLock.IsSeedPopupOpen = false;
        ReleasePopupInputBlock();
    }

    private void Update()
    {
        // Re-assert mỗi frame trong khi popup đang mở, phòng code bên ngoài
        // (VD: PlantDragController.CleanupPlantDragState) clear flag sai.
        FarmInputLock.IsSeedPopupOpen = true;

        if (FarmInputLock.IsDraggingSeed) return;
        if (!InputBridge.IsPointerDownThisFrame) return;

        bool onPopup = IsPointerOnThisPopup();

        Debug.Log($"[SeedPopup] ClickOutside check | onPopup={onPopup} | topUI={InputBridge.GetTopUINameUnderPointer()}");

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

    // ── Spawn items ───────────────────────────────────────────────────────────

    public void SpawnAllItems()
    {
        if (content == null)
        {
            Debug.LogError("[SeedPopup] content chưa gán!");
            return;
        }

        // DestroyImmediate: xóa item cũ đồng bộ trước khi spawn mới.
        for (int i = content.childCount - 1; i >= 0; i--)
            DestroyImmediate(content.GetChild(i).gameObject);

        if (seedItemPrefab == null)
        {
            Debug.LogError("[SeedPopup] seedItemPrefab chưa gán!");
            return;
        }

        if (cropDataList == null || cropDataList.Length == 0)
        {
            Debug.LogWarning("[SeedPopup] cropDataList rỗng — gán CropData trong inspector SeedPopupController.");
            return;
        }

        Debug.Log($"[SeedPopup] Spawn {cropDataList.Length} items...");

        int count = 0;
        foreach (var data in cropDataList)
        {
            if (data == null) { Debug.LogWarning("[SeedPopup] Slot null trong cropDataList."); continue; }

            GameObject go = Instantiate(seedItemPrefab, content);

            // Inject LayoutElement để HorizontalLayoutGroup không set item width = 0.
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredWidth  = itemPreferredWidth;
            le.preferredHeight = itemPreferredHeight;

            SeedDragItem item = go.GetComponent<SeedDragItem>();
            if (item != null)
            {
                item.SetData(data);
                count++;
                Debug.Log($"[SeedPopup] #{count} crop={data.cropId} | icon={(data.icon != null ? data.icon.name : "NULL")}");
            }
            else
            {
                Debug.LogError($"[SeedPopup] Prefab thiếu SeedDragItem!");
            }
        }

        Debug.Log($"[SeedPopup] Xong: {count}/{cropDataList.Length} items.");
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
