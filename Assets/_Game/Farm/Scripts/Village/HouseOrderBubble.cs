using UnityEngine;
using UnityEngine.UI;

namespace Village
{
    /// <summary>
    /// Bubble hiển thị order trên đầu nhà.
    /// Kiến trúc World Space: bubble là child của OrderAnchor bên trong house,
    /// nên tự động di chuyển theo nhà — không cần LateUpdate track vị trí.
    /// </summary>
    public class HouseOrderBubble : MonoBehaviour
    {
        [Header("UI refs — wire các Image trong OrderPopup2 prefab")]
        [SerializeField] private Image      iconItem1;
        [SerializeField] private GameObject slot2Root;
        [SerializeField] private Image      iconItem2;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        public void Show(Sprite icon1, Sprite icon2 = null)
        {
            // Active các parent trong bubble chain, DỪNG khi gặp HouseOrderController
            // để không vô tình kích hoạt lại house placeholder đã bị tắt.
            Transform p = transform.parent;
            while (p != null)
            {
                if (p.GetComponent<HouseOrderController>() != null) break;
                p.gameObject.SetActive(true);
                p = p.parent;
            }

            gameObject.SetActive(true);

            // Reset nếu bị ẩn bằng CanvasGroup hoặc scale = 0
            if (TryGetComponent<CanvasGroup>(out var cg))
            { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
            if (transform.localScale == Vector3.zero)
                transform.localScale = Vector3.one;

            if (iconItem1 != null)
            {
                iconItem1.sprite  = icon1;
                iconItem1.enabled = icon1 != null;
            }

            bool has2 = icon2 != null;
            if (slot2Root != null)
                slot2Root.SetActive(has2);

            if (has2 && iconItem2 != null)
            {
                iconItem2.sprite  = icon2;
                iconItem2.enabled = true;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
