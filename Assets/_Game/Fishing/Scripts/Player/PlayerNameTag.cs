using TMPro;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Tên + (Lv) nổi trên HeadAnchor của người chơi khác. TextMeshPro 3D (không cần canvas), font SkinKit.FontVo,
    /// đọc vị trí ở LateUpdate để không rung khi nội suy vị trí ở Update.
    /// </summary>
    public class PlayerNameTag : MonoBehaviour
    {
        private const float DefaultWorldSize = 0.12f;

        [SerializeField] private Transform anchor;
        [Tooltip("Chiều cao chữ (world unit). TMP 3D: fontSize ≈ worldSize × 10.")]
        [SerializeField] private float worldSize = DefaultWorldSize;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.06f, 0f);
        [SerializeField] private int sortingOrder = 500;

        private TextMeshPro _tmp;

        /// <summary>Tạo nhãn con của parent, bám theo anchor. Trả về component để cập nhật sau.</summary>
        public static PlayerNameTag Create(Transform parent, Transform followAnchor, string displayName, int level)
        {
            var go = new GameObject("NameTag");
            go.transform.SetParent(parent, false);
            var tag = go.AddComponent<PlayerNameTag>();
            tag.anchor = followAnchor;
            tag.EnsureText();
            tag.SetInfo(displayName, level);
            tag.SnapToAnchor();
            return tag;
        }

        public void SetAnchor(Transform followAnchor) { anchor = followAnchor; }

        public void SetInfo(string displayName, int level)
        {
            EnsureText();
            string n = string.IsNullOrEmpty(displayName) ? "?" : displayName;
            _tmp.text = n + " (Lv" + Mathf.Max(1, level) + ")";
        }

        private void Awake() { EnsureText(); }

        private void LateUpdate() { SnapToAnchor(); }

        private void SnapToAnchor()
        {
            if (anchor == null) { return; }
            transform.position = anchor.position + offset;
        }

        private void EnsureText()
        {
            if (_tmp != null) { return; }
            _tmp = GetComponent<TextMeshPro>();
            if (_tmp == null) { _tmp = gameObject.AddComponent<TextMeshPro>(); }

            var font = SkinKit.FontVo;
            if (font != null) { _tmp.font = font; }
            _tmp.fontSize = worldSize * 10f;
            _tmp.enableAutoSizing = false;
            _tmp.alignment = TextAlignmentOptions.Center;
            _tmp.fontStyle = FontStyles.Bold;
            _tmp.color = Color.white;
            _tmp.textWrappingMode = TextWrappingModes.NoWrap;
            _tmp.overflowMode = TextOverflowModes.Overflow;
            _tmp.rectTransform.sizeDelta = new Vector2(3f, 0.4f);
            _tmp.rectTransform.pivot = new Vector2(0.5f, 0f);   // chữ mọc lên từ anchor
            // Viền đen mỏng để đọc được trên nền nước sáng.
            _tmp.outlineWidth = 0.2f;
            _tmp.outlineColor = new Color32(0, 0, 0, 220);

            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = TouristSortingLayers.Resolve(TouristSortingLayers.Overlay);
                mr.sortingOrder = sortingOrder;
            }
        }
    }
}
