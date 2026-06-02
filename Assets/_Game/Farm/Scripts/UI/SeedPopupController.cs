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

    // â”€â”€ VÃ²ng Ä‘á»i Unity â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
        // Re-assert má»—i frame trong khi popup Ä‘ang má»Ÿ, phÃ²ng code bÃªn ngoÃ i
        // (VD: PlantDragController.CleanupPlantDragState) clear flag sai.
        FarmInputLock.IsSeedPopupOpen = true;

        if (FarmInputLock.IsDraggingSeed) return;
        if (!InputBridge.IsPointerDownThisFrame) return;

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
            Debug.LogError("[SeedPopup] content chÆ°a gÃ¡n!");
            return;
        }

        // DestroyImmediate: xÃ³a item cÅ© Ä‘á»“ng bá»™ trÆ°á»›c khi spawn má»›i.
        for (int i = content.childCount - 1; i >= 0; i--)
            DestroyImmediate(content.GetChild(i).gameObject);

        if (seedItemPrefab == null)
        {
            Debug.LogError("[SeedPopup] seedItemPrefab chÆ°a gÃ¡n!");
            return;
        }

        if (cropDataList == null || cropDataList.Length == 0)
        {
            Debug.LogWarning("[SeedPopup] cropDataList rá»—ng â€” gÃ¡n CropData trong inspector SeedPopupController.");
            return;
        }


        int count = 0;
        foreach (var data in cropDataList)
        {
            if (data == null) { Debug.LogWarning("[SeedPopup] Slot null trong cropDataList."); continue; }

            GameObject go = Instantiate(seedItemPrefab, content);

            // Inject LayoutElement Ä‘á»ƒ HorizontalLayoutGroup khÃ´ng set item width = 0.
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
                Debug.LogError($"[SeedPopup] Prefab thiáº¿u SeedDragItem!");
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
