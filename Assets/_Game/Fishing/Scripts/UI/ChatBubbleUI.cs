using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Bubble chat world-space trên đầu nhân vật. Canvas world "Canvas_FishingWorld" (FishingIds.WorldCanvasName) tìm theo tên,
    /// không có thì tạo (WorldSpace, scale 0.01, sorting Foreground order 500). Mỗi bubble = PanelPaper 9-slice + TMP đen 28 (≈0.28 unit) + đuôi.
    /// Pool theo headAnchor (Dictionary), bám theo ở LateUpdate (+0.15 unit), tự ẩn sau seconds; Show lại chỉ reset giờ.
    /// isLocal → viền xanh nhạt. Dev A (RemotePlayerView) gọi Show cho người khác, ChatPanelUI gọi cho mình.
    /// </summary>
    public class ChatBubbleUI : MonoBehaviour
    {
        private const float MaxWidthPx = 280f;
        private const float PaddingPx = 18f;
        private const float FontSizePx = 28f;
        private const float HeadOffsetUnit = 0.15f;
        private const float CanvasScale = 0.01f;
        private const int SortingOrder = 500;
        private static readonly Color LocalBorder = new Color(0.55f, 0.8f, 1f, 1f);
        private static readonly Color RemoteBorder = new Color(1f, 1f, 1f, 0.9f);

        private static readonly Dictionary<Transform, ChatBubbleUI> Pool = new Dictionary<Transform, ChatBubbleUI>();
        private static Canvas _worldCanvas;

        [SerializeField] private Image imgBorder;
        [SerializeField] private Image imgPanel;
        [SerializeField] private Image imgTail;
        [SerializeField] private TextMeshProUGUI txt;

        private Transform _anchor;
        private float _hideAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Pool.Clear(); _worldCanvas = null; }

        /// <summary>Hiện/reset bubble trên headAnchor.</summary>
        public static void Show(Transform headAnchor, string text, float seconds, bool isLocal)
        {
            if (headAnchor == null || string.IsNullOrEmpty(text)) { return; }
            ChatBubbleUI b = GetOrCreate(headAnchor);
            if (b == null) { return; }
            b.Apply(text, seconds, isLocal);
        }

        /// <summary>Ẩn bubble của 1 anchor (nếu có).</summary>
        public static void Hide(Transform headAnchor)
        {
            if (headAnchor == null) { return; }
            ChatBubbleUI b;
            if (Pool.TryGetValue(headAnchor, out b) && b != null) { b.gameObject.SetActive(false); }
        }

        private static ChatBubbleUI GetOrCreate(Transform anchor)
        {
            ChatBubbleUI b;
            if (Pool.TryGetValue(anchor, out b))
            {
                if (b != null) { return b; }
                Pool.Remove(anchor); // fake-null (đã bị huỷ theo scene)
            }
            Canvas canvas = EnsureWorldCanvas();
            if (canvas == null) { return null; }
            var go = new GameObject("Bubble_" + anchor.name, typeof(RectTransform));
            go.layer = canvas.gameObject.layer;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            rt.pivot = new Vector2(0.5f, 0f);
            b = go.AddComponent<ChatBubbleUI>();
            b._anchor = anchor;
            b.BuildIfEmpty();
            Pool[anchor] = b;
            return b;
        }

        private static Canvas EnsureWorldCanvas()
        {
            if (_worldCanvas != null) { return _worldCanvas; }
            GameObject found = GameObject.Find(FishingIds.WorldCanvasName);
            if (found != null) { _worldCanvas = found.GetComponent<Canvas>(); }
            if (_worldCanvas == null)
            {
                var go = found != null ? found : new GameObject(FishingIds.WorldCanvasName);
                _worldCanvas = FishingUiKit.GetOrAdd<Canvas>(go);
                _worldCanvas.renderMode = RenderMode.WorldSpace;
                _worldCanvas.worldCamera = Camera.main;
                _worldCanvas.sortingLayerName = TouristSortingLayers.ResolveOrOverride("Foreground", TouristSortingLayers.Overlay);
                _worldCanvas.sortingOrder = SortingOrder;
                go.transform.localScale = Vector3.one * CanvasScale;
                var crt = go.GetComponent<RectTransform>();
                if (crt != null) { crt.sizeDelta = new Vector2(100f, 100f); }
                Debug.Log(FishingIds.LogTag + " Tạo " + FishingIds.WorldCanvasName + " (WorldSpace, scale 0.01).");
            }
            return _worldCanvas;
        }

        /// <summary>Dựng con của 1 bubble nếu thiếu.</summary>
        public void BuildIfEmpty()
        {
            Image border = FishingUiKit.Panel(transform, "Img_Border", new Vector2(120f, 60f), FishingUiKit.Rounded(22f), Vector2.zero, RemoteBorder);
            if (imgBorder == null) { imgBorder = border; }
            imgBorder.raycastTarget = false;
            FishingUiKit.Stretch(imgBorder.rectTransform, -4f);

            Image panel = FishingUiKit.Panel(transform, "Img_Panel", new Vector2(120f, 60f), UIStandardSprites.PanelPaper);
            if (imgPanel == null) { imgPanel = panel; }
            imgPanel.raycastTarget = false;
            FishingUiKit.Stretch(imgPanel.rectTransform, 0f);

            Image tail = FishingUiKit.Panel(transform, "Img_Tail", new Vector2(18f, 18f), FishingUiKit.Rounded(3f), new Vector2(0f, -4f), FishingUiKit.PanelFallback);
            if (imgTail == null) { imgTail = tail; }
            imgTail.raycastTarget = false;
            imgTail.rectTransform.anchorMin = new Vector2(0.5f, 0f); imgTail.rectTransform.anchorMax = new Vector2(0.5f, 0f); imgTail.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            imgTail.rectTransform.anchoredPosition = new Vector2(0f, -4f);
            imgTail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            if (imgTail.sprite != null) { imgTail.color = FishingUiKit.PanelFallback; } // đuôi cùng màu giấy

            TextMeshProUGUI t = FishingUiKit.Label(transform, "Txt", string.Empty, FontSizePx, TextAlignmentOptions.Center, new Vector2(100f, 40f), Vector2.zero, Color.black);
            if (txt == null) { txt = t; }
            txt.textWrappingMode = TextWrappingModes.Normal;
            txt.overflowMode = TextOverflowModes.Overflow;
            FishingUiKit.Stretch(txt.rectTransform, PaddingPx);
        }

        private void Apply(string text, float seconds, bool isLocal)
        {
            BuildIfEmpty();
            txt.text = text;
            Vector2 pref = txt.GetPreferredValues(text, MaxWidthPx - PaddingPx * 2f, 0f);
            float w = Mathf.Clamp(pref.x + PaddingPx * 2f, 60f, MaxWidthPx);
            float h = Mathf.Max(pref.y + PaddingPx * 2f, FontSizePx + PaddingPx * 2f);
            var rt = transform as RectTransform;
            rt.sizeDelta = new Vector2(w, h);
            if (imgBorder != null) { imgBorder.color = isLocal ? LocalBorder : RemoteBorder; }
            _hideAt = Time.time + Mathf.Max(0.5f, seconds);
            gameObject.SetActive(true);
            FollowAnchor();
        }

        private void LateUpdate()
        {
            if (_anchor == null)
            {
                // Anchor đã bị huỷ (người chơi rời phòng) → tự dọn.
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            if (Time.time >= _hideAt) { gameObject.SetActive(false); return; }
            FollowAnchor();
        }

        private void FollowAnchor()
        {
            if (_anchor == null) { return; }
            transform.position = _anchor.position + Vector3.up * HeadOffsetUnit;
            transform.rotation = Quaternion.identity;
        }

        private void OnDestroy()
        {
            if (_anchor != null) { ChatBubbleUI b; if (Pool.TryGetValue(_anchor, out b) && b == this) { Pool.Remove(_anchor); } }
        }
    }
}
