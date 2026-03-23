using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class SeedDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Seed Data")]
    [SerializeField] private CropData cropData;

    [Header("UI Refs")]
    [SerializeField] private Canvas rootCanvas;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 startAnchoredPosition;
    private bool canDrag;

    public string CropId => cropData != null ? cropData.cropId : string.Empty;
    public CropData CropData => cropData;

    // Cache component UI cần dùng.
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();
    }

    // Bắt đầu kéo item hạt giống.
    public void OnBeginDrag(PointerEventData eventData)
    {
        canDrag = cropData != null;

        if (!canDrag)
        {
            Debug.LogWarning($"{name} chưa gán CropData");
            return;
        }

        // Phải chọn ô đất trước rồi mới cho kéo.
        if (FarmManager.Instance == null)
        {
            canDrag = false;
            Debug.LogError("FarmManager.Instance NULL");
            return;
        }

        if (FarmManager.Instance.SelectedPlot == null)
        {
            canDrag = false;
            Debug.LogWarning("Chưa chọn ô đất");
            FarmUIManager.Instance?.ShowHint("Hãy click ô đất trước.");
            return;
        }

        startAnchoredPosition = rectTransform.anchoredPosition;

        canvasGroup.alpha = 0.75f;
        canvasGroup.blocksRaycasts = false;

        Debug.Log($"BEGIN DRAG: {cropData.displayName} | selectedPlot = {FarmManager.Instance.SelectedPlot.name}");
    }

    // Kéo icon seed theo chuột trong canvas.
    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag || rootCanvas == null)
            return;

        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    // Khi thả, trồng vào đúng ô đã click trước đó.
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag)
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        bool planted = false;

        if (FarmManager.Instance != null)
        {
            PlotController selectedPlot = FarmManager.Instance.SelectedPlot;
            Debug.Log($"END DRAG TRY PLANT | crop={(cropData != null ? cropData.displayName : "NULL")} | selectedPlot={(selectedPlot != null ? selectedPlot.name : "NULL")}");

            planted = FarmManager.Instance.TryPlantToSelectedPlot(cropData);
        }

        // Dù thành công hay không cũng trả item về vị trí cũ.
        rectTransform.anchoredPosition = startAnchoredPosition;

        Debug.Log("END DRAG planted = " + planted);

        if (planted)
        {
            FarmUIManager.Instance?.ShowHint($"Đã trồng {cropData.displayName}");
            FarmUIManager.Instance?.HidePlantSelectPopup();
        }
        else
        {
            FarmUIManager.Instance?.ShowHint("Không thể trồng vào ô đất đã chọn.");
        }
    }
}