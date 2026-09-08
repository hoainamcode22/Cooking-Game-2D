using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Joystick ảo UGUI (nền tròn + núm) nằm trong Canvas_FishingHUD. Dự án chưa có joystick nào nên viết mới.
    /// Root nhận pointer (Image trong suốt raycastTarget), con Bg/Knob chỉ để vẽ. Direction đã trừ dead-zone, độ dài 0..1.
    /// </summary>
    public class VirtualJoystickUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public const string BgName = "Bg";
        public const string KnobName = "Knob";
        private const float BgSizePx = 260f;
        private const float KnobSizePx = 110f;
        private static readonly Vector2 DefaultAnchoredPos = new Vector2(200f, 200f);

        [Tooltip("Để trống = tự tìm con 'Bg' hoặc dựng bằng BuildIfEmpty.")]
        [SerializeField] private RectTransform bg;
        [Tooltip("Để trống = tự tìm con 'Knob' hoặc dựng bằng BuildIfEmpty.")]
        [SerializeField] private RectTransform knob;
        [Tooltip("0 = lấy nửa chiều rộng của Bg.")]
        [SerializeField] private float radiusOverride = 0f;

        public static VirtualJoystickUI Instance { get; private set; }
        public Vector2 Direction { get; private set; }
        public bool IsActive { get; private set; }

        private FishingConfig _cfg;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }

        private void Awake()
        {
            Instance = this;
            _cfg = FishingDatabase.ConfigOrDefault;
            BuildIfEmpty();
        }

        private void OnDisable() { Release(); }

        private void OnDestroy()
        {
            if (Instance == this) { Instance = null; }
        }

        /// <summary>Dựng nền + núm nếu chưa có con Bg/Knob (tool + runtime). Không huỷ/ghi đè con có sẵn.</summary>
        public void BuildIfEmpty()
        {
            var rt = transform as RectTransform;
            if (rt == null) { Debug.LogWarning(FishingIds.LogTag + " VirtualJoystickUI phải nằm trên RectTransform (trong Canvas)."); return; }

            // Root cần một Graphic để EventSystem raycast tới; trong suốt nên không vẽ gì.
            var rootImg = GetComponent<Image>();
            if (rootImg == null)
            {
                rootImg = gameObject.AddComponent<Image>();
                rootImg.color = new Color(1f, 1f, 1f, 0f);
            }
            rootImg.raycastTarget = true;

            if (bg == null) { bg = FindChildRect(rt, BgName); }
            if (knob == null) { knob = FindChildRect(rt, KnobName); }
            bool built = false;

            if (bg == null)
            {
                // Chỉ khi dựng mới toanh thì mới đặt anchor/kích thước root — tránh ghi đè vị trí Sếp đã kéo.
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = DefaultAnchoredPos;
                rt.sizeDelta = new Vector2(BgSizePx, BgSizePx);

                Sprite bgSprite = UIStandardSprites.SlotNormal;
                bg = CreateImageChild(rt, BgName, bgSprite, new Color(1f, 1f, 1f, 0.55f), new Vector2(BgSizePx, BgSizePx));
                built = true;
            }
            if (knob == null)
            {
                Sprite knobSprite = UIStandardSprites.CheckBadge;
                if (knobSprite == null) { knobSprite = UIStandardSprites.AvatarBase; }
                knob = CreateImageChild(bg, KnobName, knobSprite, new Color(1f, 1f, 1f, 0.9f), new Vector2(KnobSizePx, KnobSizePx));
                built = true;
            }
            if (built) { Debug.Log(FishingIds.LogTag + " VirtualJoystickUI đã dựng Bg/Knob."); }
        }

        // ── Pointer ──

        public void OnPointerDown(PointerEventData eventData)
        {
            IsActive = true;
            UpdateFromPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsActive) { IsActive = true; }
            UpdateFromPointer(eventData);
        }

        public void OnPointerUp(PointerEventData eventData) { Release(); }

        private void UpdateFromPointer(PointerEventData eventData)
        {
            if (bg == null) { return; }
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bg, eventData.position, eventData.pressEventCamera, out local)) { return; }

            float radius = radiusOverride > 0f ? radiusOverride : bg.rect.width * 0.5f;
            if (radius <= 0.001f) { radius = BgSizePx * 0.5f; }

            // Bù pivot của Bg (nếu tool đặt pivot khác 0.5) để tâm vòng luôn là (0,0).
            Vector2 center = new Vector2((0.5f - bg.pivot.x) * bg.rect.width, (0.5f - bg.pivot.y) * bg.rect.height);
            Vector2 v = (local - center) / radius;
            float mag = v.magnitude;
            if (mag > 1f) { v /= mag; mag = 1f; }

            if (knob != null) { knob.anchoredPosition = v * radius; }

            float dz = _cfg != null ? _cfg.joystickDeadZone : 0.15f;
            if (mag <= dz) { Direction = Vector2.zero; return; }
            // Trừ dead-zone rồi kéo dãn lại 0..1 để đẩy nhẹ vẫn có phản hồi mượt.
            float scaled = (mag - dz) / Mathf.Max(0.0001f, 1f - dz);
            Direction = v / mag * Mathf.Clamp01(scaled);
        }

        private void Release()
        {
            IsActive = false;
            Direction = Vector2.zero;
            if (knob != null) { knob.anchoredPosition = Vector2.zero; }
        }

        // ── Dựng UI ──

        private static RectTransform FindChildRect(Transform parent, string childName)
        {
            // Tìm cả cháu (Knob thường là con của Bg).
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (c.name == childName) { return c as RectTransform; }
                var deeper = FindChildRect(c, childName);
                if (deeper != null) { return deeper; }
            }
            return null;
        }

        private static RectTransform CreateImageChild(RectTransform parent, string childName, Sprite sprite, Color fallbackColor, Vector2 size)
        {
            var go = new GameObject(childName, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;   // để root nhận toàn bộ pointer
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                img.preserveAspect = true;
                img.color = fallbackColor.a < 1f ? new Color(1f, 1f, 1f, fallbackColor.a) : Color.white;
            }
            else
            {
                img.color = fallbackColor;   // không có sprite → khối màu bán trong suốt vẫn nhìn ra vị trí
            }
            return rt;
        }
    }
}
