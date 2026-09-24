using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KitchenUIv3
{
    /// <summary>
    /// Keo mon tren dia (Plating_Table) tha vao hop kho (Warehouse_Box) = cat vao kho.
    /// Cham (click) van hoat dong nhu cu qua Button cua Plating_Table (V2 dang xu ly).
    /// Component nay chi THEM cach thu hai: keo-tha. Khi tha trung Warehouse_Box no goi
    /// dung Button.onClick cua Plating_Table -> di qua y het duong logic cu (bay + cat kho).
    /// Chi cho keo khi Button dang interactable (tuc dia dang co mon).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class KitchenV3PlateDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("Hop kho — tha vao day. Bo trong thi tool/Start tu tim object ten Warehouse_Box.")]
        public RectTransform hopKho;
        [Tooltip("Icon mon tren dia (Plating_Table/Dish_Visual). Bo trong thi tu tim.")]
        public Image iconMon;
        public float nguongKeo = 12f;

        private Button _btn;
        private RectTransform _ghost;
        private Canvas _canvas;
        private bool _dangKeo;

        private void Awake()
        {
            _btn = GetComponent<Button>();
            _canvas = GetComponentInParent<Canvas>();
            if (iconMon == null)
            {
                var t = transform.Find("Dish_Visual");
                if (t != null) iconMon = t.GetComponent<Image>();
            }
            if (hopKho == null && _canvas != null)
                hopKho = TimSau(_canvas.transform, "Warehouse_Box") as RectTransform;
        }

        private static Transform TimSau(Transform goc, string ten)
        {
            if (goc == null) return null;
            if (goc.name == ten) return goc;
            for (int i = 0; i < goc.childCount; i++)
            {
                var r = TimSau(goc.GetChild(i), ten);
                if (r != null) return r;
            }
            return null;
        }

        private bool CoMon => _btn != null && _btn.interactable && iconMon != null && iconMon.sprite != null;

        public void OnBeginDrag(PointerEventData e)
        {
            if (!CoMon) { _dangKeo = false; return; }
            _dangKeo = true;
            var go = new GameObject("FX_PlateDrag", typeof(RectTransform), typeof(Image));
            _ghost = (RectTransform)go.transform;
            _ghost.SetParent(_canvas != null ? _canvas.transform : transform.root, false);
            _ghost.SetAsLastSibling();
            var img = go.GetComponent<Image>();
            img.sprite = iconMon.sprite; img.preserveAspect = true; img.raycastTarget = false;
            _ghost.sizeDelta = iconMon.rectTransform.sizeDelta * 1.15f;
            // [2026-09-24] giu dung do to Sep chinh (scale cua Dish_Visual)
            if (_ghost.parent != null && _ghost.parent.lossyScale.x != 0f)
                _ghost.localScale = Vector3.one * (iconMon.rectTransform.lossyScale.x / _ghost.parent.lossyScale.x);
            CapNhatViTri(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_dangKeo || _ghost == null) return;
            CapNhatViTri(e);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!_dangKeo) return;
            _dangKeo = false;
            bool trung = hopKho != null &&
                         RectTransformUtility.RectangleContainsScreenPoint(hopKho, e.position, e.pressEventCamera);
            if (_ghost != null) Destroy(_ghost.gameObject);
            _ghost = null;
            if (trung && _btn != null && _btn.interactable)
                _btn.onClick.Invoke();   // cung duong voi cham dia: bay vao kho + CollectCookedDishToWarehouse
        }

        private void CapNhatViTri(PointerEventData e)
        {
            if (_ghost == null) return;
            var cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_ghost.parent as RectTransform, e.position, cam, out var w))
                _ghost.position = w;
        }
    }
}
