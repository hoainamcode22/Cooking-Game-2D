using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PenBasketTrayController : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Refs")]
    [SerializeField] private Image basketIcon;
    [SerializeField] private RectTransform trayPanel;

    private PenMiniPanelUI _targetPen;
    private bool _isDragging;
    private Canvas _ghostCanvas;
    private GameObject _ghostObj;

    private void Awake()
    {
        if (basketIcon == null)
            basketIcon = GetComponentInChildren<Image>();
    }

    public void Open(PenMiniPanelUI pen)
    {
        _targetPen = pen;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        _targetPen = null;
        gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Nhấn xuống
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        FarmInputLock.IsDraggingSeed = true;

        // Tạo Ghost Canvas Screen-Space Overlay
        var canvasGo = new GameObject("_PenBasketGhostCanvas");
        _ghostCanvas = canvasGo.AddComponent<Canvas>();
        _ghostCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _ghostCanvas.sortingOrder = 9999;

        _ghostObj = new GameObject("GhostBasket");
        _ghostObj.transform.SetParent(canvasGo.transform, false);

        var ghostImg = _ghostObj.AddComponent<Image>();
        ghostImg.sprite = basketIcon != null ? basketIcon.sprite : null;
        ghostImg.preserveAspect = true;
        ghostImg.raycastTarget = false;

        var rt = _ghostObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 100f);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.position = eventData.position;

        var cg = _ghostObj.AddComponent<CanvasGroup>();
        cg.alpha = 0.9f;
        cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghostObj != null)
            _ghostObj.GetComponent<RectTransform>().position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        _isDragging = false;
        FarmInputLock.IsDraggingSeed = false;

        if (_ghostCanvas != null)
        {
            Destroy(_ghostCanvas.gameObject);
            _ghostCanvas = null;
            _ghostObj = null;
        }

        TryHarvestPen(eventData.position);
    }

    private void TryHarvestPen(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z)));
        worldPos.z = 0f;

        // 1. Kiểm tra targetPen trước
        if (_targetPen != null && _targetPen.CurrentState == PenMiniPanelUI.PenState.Ready)
        {
            float dist = Vector2.Distance(worldPos, _targetPen.transform.position);
            float checkDist = 350f;
            var box = _targetPen.GetComponent<BoxCollider2D>();
            if (box != null) checkDist = Mathf.Max(box.size.x, box.size.y) * 1.5f;

            if (dist <= checkDist || (box != null && box.OverlapPoint(worldPos)))
            {
                _targetPen.TryHarvest(worldPos);
                Close();
                return;
            }
        }

        // 2. Quét các chuồng khác
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
        if (hits != null && hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                var pen = hits[i].GetComponent<PenMiniPanelUI>() ?? hits[i].GetComponentInParent<PenMiniPanelUI>();
                if (pen == null)
                {
                    var dropTarget = hits[i].GetComponent<PenDropTarget>() ?? hits[i].GetComponentInParent<PenDropTarget>();
                    if (dropTarget != null) pen = dropTarget.GetComponentInParent<PenMiniPanelUI>();
                }

                if (pen != null && pen.CurrentState == PenMiniPanelUI.PenState.Ready)
                {
                    pen.TryHarvest(worldPos);
                    Close();
                    return;
                }
            }
        }

        Close();
    }
}
