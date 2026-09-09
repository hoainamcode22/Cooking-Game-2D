using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Bộ helper dựng UGUI bằng code cho module Hồ Câu (CHỦ: Dev C).
    /// Nguyên tắc: MỌI hàm là find-or-create theo TÊN con trực tiếp của parent — gọi 2 lần không tạo trùng,
    /// KHÔNG bao giờ Destroy con có sẵn, chỉ gán sprite khi vừa tạo hoặc Image chưa có sprite (Sếp đã gắn art thì giữ).
    /// Sprite qua UIStandardSprites; border == 0 thì Simple, null thì màu phẳng. Font SkinKit.FontVo. Chữ đã qua Loc.T do caller lo.
    /// </summary>
    public static class FishingUiKit
    {
        // ── Màu dùng chung ──
        public static readonly Color TextDark = new Color32(70, 45, 25, 255);
        public static readonly Color TextLight = Color.white;
        public static readonly Color TextMuted = new Color32(120, 100, 80, 255);
        public static readonly Color PanelFallback = new Color32(245, 235, 205, 255);
        public static readonly Color FrameFallback = new Color32(140, 95, 50, 255);
        public static readonly Color ButtonFallback = new Color32(90, 170, 80, 255);
        public static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);
        /// <summary>Dim nhạt hơn cho panel bên trong HUD scene câu (vẫn thấy hồ phía sau).</summary>
        public static readonly Color DimLight = new Color(0f, 0f, 0f, 0.35f);
        public static readonly Color DisabledTint = new Color(0.62f, 0.62f, 0.62f, 0.9f);
        public static readonly Color OnlineGreen = new Color32(80, 200, 90, 255);
        public static readonly Color OfflineGray = new Color32(150, 150, 150, 255);

        /// <summary>Nút tối thiểu 92 px (đầu ngón tay). Nút đóng chuẩn 64 px là ngoại lệ.</summary>
        public const float MinButtonSize = 92f;
        public const int UiLayer = 5;

        // ─────────────────────────────────────────────────────────────────────
        //  TÌM / TẠO RECT
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Con trực tiếp theo tên, null nếu không có.</summary>
        public static RectTransform FindChild(Transform parent, string name)
        {
            if (parent == null || string.IsNullOrEmpty(name)) { return null; }
            Transform t = parent.Find(name);
            return t != null ? t as RectTransform : null;
        }

        /// <summary>Find-or-create một RectTransform con (anchor giữa). created = true nếu vừa tạo.</summary>
        public static RectTransform Child(Transform parent, string name, Vector2 size, Vector2 anchoredPos, out bool created)
        {
            created = false;
            RectTransform rt = FindChild(parent, name);
            if (rt != null) { return rt; }
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : UiLayer;
            rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            created = true;
            return rt;
        }

        public static RectTransform Child(Transform parent, string name, Vector2 size, Vector2 anchoredPos = default)
        {
            bool created;
            return Child(parent, name, size, anchoredPos, out created);
        }

        /// <summary>Đặt anchor = pivot = 1 điểm (vd (1,0) = góc phải-dưới) và vị trí. Chỉ nên gọi khi vừa tạo.</summary>
        public static RectTransform SetAnchor(RectTransform rt, Vector2 anchor, Vector2 anchoredPos)
        {
            if (rt == null) { return null; }
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            return rt;
        }

        /// <summary>Kéo full cha, chừa lề.</summary>
        public static RectTransform Stretch(RectTransform rt, float padding = 0f)
        {
            if (rt == null) { return null; }
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(padding, padding); rt.offsetMax = new Vector2(-padding, -padding);
            return rt;
        }

        public static T GetOrAdd<T>(GameObject go) where T : Component
        {
            if (go == null) { return null; }
            T c = go.GetComponent<T>();
            if (c == null) { c = go.AddComponent<T>(); }
            return c;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SPRITE
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Gán sprite: có border → Sliced, không → Simple, null → màu phẳng fallback (không lỗi). Tint trắng lên art thật.</summary>
        public static void SetSlicedOrSimple(Image img, Sprite sprite, Color? fallbackColor = null)
        {
            if (img == null) { return; }
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                img.color = Color.white;
            }
            else
            {
                img.sprite = null;
                img.type = Image.Type.Simple;
                img.color = fallbackColor ?? PanelFallback;
            }
        }

        /// <summary>Sprite tròn vẽ code (fallback cho avatar, chấm online, vòng cắn).</summary>
        public static Sprite Circle() { return SkinKit.HinhTron(); }

        /// <summary>Sprite bo góc vẽ code (tint được, dùng cho nút màu theo cfg).</summary>
        public static Sprite Rounded(float radius) { return SkinKit.BoGoc(radius); }

        /// <summary>Avatar hồ sơ farm theo index (Resources/Avatars/avatar_npc_i). Null → caller giữ khung AvatarBase.</summary>
        public static Sprite AvatarSprite(int index)
        {
            if (index < 0) { index = 0; }
            return Resources.Load<Sprite>("Avatars/avatar_npc_" + index.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Màu viền theo độ hiếm cá.</summary>
        public static Color RarityColor(FishRarity r)
        {
            switch (r)
            {
                case FishRarity.Uncommon: return new Color32(90, 200, 100, 255);
                case FishRarity.Rare: return new Color32(80, 150, 255, 255);
                case FishRarity.Epic: return new Color32(190, 90, 230, 255);
                default: return new Color32(200, 200, 200, 255);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PANEL / ICON / LABEL
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Image nền find-or-create. Sprite chỉ gán khi vừa tạo hoặc chưa có sprite.</summary>
        public static Image Panel(Transform parent, string name, Vector2 size, Sprite sprite, Vector2 anchoredPos = default, Color? fallback = null)
        {
            bool created;
            RectTransform rt = Child(parent, name, size, anchoredPos, out created);
            Image img = GetOrAdd<Image>(rt.gameObject);
            if (created || img.sprite == null) { SetSlicedOrSimple(img, sprite, fallback); }
            return img;
        }

        /// <summary>Icon giữ tỉ lệ, không chặn raycast. Sprite null → ô màu.</summary>
        public static Image Icon(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 anchoredPos = default, Color? fallback = null)
        {
            bool created;
            RectTransform rt = Child(parent, name, size, anchoredPos, out created);
            Image img = GetOrAdd<Image>(rt.gameObject);
            if (created)
            {
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
            if (created || img.sprite == null) { SetSlicedOrSimple(img, sprite, fallback ?? new Color(1f, 1f, 1f, 0.35f)); }
            return img;
        }

        /// <summary>Đổi icon runtime (không đụng type/border): sprite null → ô màu fallback.</summary>
        public static void SetIcon(Image img, Sprite sprite, Color fallback)
        {
            if (img == null) { return; }
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Simple; img.color = Color.white; }
            else { img.sprite = null; img.color = fallback; }
        }

        /// <summary>TMP label find-or-create, font SkinKit.FontVo, không chặn raycast. Text luôn được gán (caller truyền Loc.T).</summary>
        public static TextMeshProUGUI Label(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align, Vector2 size, Vector2 anchoredPos = default, Color? color = null, bool bold = false)
        {
            bool created;
            RectTransform rt = Child(parent, name, size, anchoredPos, out created);
            TextMeshProUGUI tmp = GetOrAdd<TextMeshProUGUI>(rt.gameObject);
            if (created)
            {
                tmp.fontSize = fontSize;
                tmp.alignment = align;
                tmp.color = color ?? TextDark;
                tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
                tmp.raycastTarget = false;
                tmp.textWrappingMode = TextWrappingModes.Normal;
                tmp.overflowMode = TextOverflowModes.Ellipsis;
            }
            ApplyFont(tmp);
            tmp.text = text ?? string.Empty;
            return tmp;
        }

        public static void ApplyFont(TMP_Text tmp)
        {
            if (tmp == null) { return; }
            TMP_FontAsset f = SkinKit.FontVo;
            if (f != null && tmp.font != f)
            {
                tmp.font = f;
                if (f.material != null) { tmp.fontSharedMaterial = f.material; }
            }
        }

        /// <summary>Bóng chữ nhẹ cho label trên nền art (không đụng label đã có Shadow).</summary>
        public static void AddShadow(Graphic g, Color color, Vector2 distance)
        {
            if (g == null) { return; }
            Shadow s = g.GetComponent<Shadow>();
            if (s == null) { s = g.gameObject.AddComponent<Shadow>(); s.effectColor = color; s.effectDistance = distance; }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  NÚT
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Nút find-or-create: Image (sprite 9-slice) + Button + TMP con "Txt". Kích thước kẹp ≥ 92 px mỗi chiều.
        /// onClick chỉ AddListener khi khác null (BuildIfEmpty truyền null, Awake mới gắn — tránh listener runtime bị mất khi serialize).
        /// </summary>
        public static Button Button(Transform parent, string name, string text, Vector2 size, Sprite sprite, UnityAction onClick = null, Vector2 anchoredPos = default, float fontSize = 30f, Color? textColor = null, Color? fallback = null)
        {
            size = new Vector2(Mathf.Max(size.x, MinButtonSize), Mathf.Max(size.y, MinButtonSize));
            bool created;
            RectTransform rt = Child(parent, name, size, anchoredPos, out created);
            Image img = GetOrAdd<Image>(rt.gameObject);
            if (created || img.sprite == null) { SetSlicedOrSimple(img, sprite, fallback ?? ButtonFallback); }
            img.raycastTarget = true;
            Button btn = GetOrAdd<Button>(rt.gameObject);
            if (btn.targetGraphic == null) { btn.targetGraphic = img; }
            if (!string.IsNullOrEmpty(text) || FindChild(rt, "Txt") != null)
            {
                TextMeshProUGUI tmp = Label(rt, "Txt", text, fontSize, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero, textColor ?? TextLight, true);
                if (created) { Stretch(tmp.rectTransform, 6f); AddShadow(tmp, new Color(0f, 0f, 0f, 0.45f), new Vector2(1f, -2f)); }
            }
            if (onClick != null) { btn.onClick.AddListener(onClick); }
            return btn;
        }

        /// <summary>Đổi chữ trên nút do Button() tạo.</summary>
        public static void SetButtonText(Button btn, string text)
        {
            if (btn == null) { return; }
            RectTransform t = FindChild(btn.transform, "Txt");
            if (t == null) { return; }
            TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null) { tmp.text = text ?? string.Empty; }
        }

        /// <summary>Bật/tắt nút: interactable + tint mờ ảnh (ColorTint mặc định quá nhạt trên nền art).</summary>
        public static void SetEnabled(Button btn, bool on)
        {
            if (btn == null) { return; }
            btn.interactable = on;
            Image img = btn.targetGraphic as Image;
            if (img != null && img.sprite != null) { img.color = on ? Color.white : DisabledTint; }
            RectTransform t = FindChild(btn.transform, "Txt");
            if (t != null)
            {
                TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
                if (tmp != null) { Color c = tmp.color; c.a = on ? 1f : 0.55f; tmp.color = c; }
            }
        }

        /// <summary>Nút đóng chuẩn: sprite UIStandardSprites.Close + TMP "X" trắng đậm 26, kích thước CloseSize, neo góc phải-trên cha.</summary>
        public static Button CloseButton(Transform parent, UnityAction onClick = null, Vector2? anchoredPos = null)
        {
            bool created;
            RectTransform rt = Child(parent, "Btn_Close", UIStandardSprites.CloseSize, Vector2.zero, out created);
            if (created) { SetAnchor(rt, new Vector2(1f, 1f), anchoredPos ?? new Vector2(10f, 10f)); rt.sizeDelta = UIStandardSprites.CloseSize; }
            Image img = GetOrAdd<Image>(rt.gameObject);
            if (created || img.sprite == null)
            {
                Sprite close = UIStandardSprites.Close;
                if (close != null) { SetSlicedOrSimple(img, close); img.preserveAspect = true; }
                else { SetSlicedOrSimple(img, Circle(), (Color)new Color32(210, 55, 60, 255)); }
            }
            Button btn = GetOrAdd<Button>(rt.gameObject);
            if (btn.targetGraphic == null) { btn.targetGraphic = img; }
            TextMeshProUGUI x = Label(rt, "Txt_X", "X", UIStandardSprites.CloseGlyphSize, TextAlignmentOptions.Center, UIStandardSprites.CloseSize - new Vector2(10f, 10f), new Vector2(0f, 1f), TextLight, true);
            x.overflowMode = TextOverflowModes.Overflow;
            if (onClick != null) { btn.onClick.AddListener(onClick); }
            return btn;
        }

        /// <summary>Nền mờ full-screen chặn raycast; có Button (transition None) để chạm nền là đóng.</summary>
        public static Button DimBackground(Transform parent, string name, UnityAction onClick = null)
        {
            bool created;
            RectTransform rt = Child(parent, name, Vector2.zero, Vector2.zero, out created);
            if (created) { Stretch(rt); }
            Image img = GetOrAdd<Image>(rt.gameObject);
            if (created) { img.sprite = null; img.color = DimColor; img.raycastTarget = true; }
            Button btn = GetOrAdd<Button>(rt.gameObject);
            if (created) { btn.transition = Selectable.Transition.None; }
            if (onClick != null) { btn.onClick.AddListener(onClick); }
            return btn;
        }

        /// <summary>
        /// Nền mờ chắn CẢ MÀN HÌNH nằm SAU nội dung của một panel KHÔNG full-screen (Panel_Basket / Panel_Friends / Panel_Chat trong HUD):
        /// con "Img_Dim" cỡ 6000 px neo giữa panel (phủ hết canvas dù panel nằm lệch), đưa xuống sibling đầu để vẽ dưới khung.
        /// Có Button (transition None) để chạm ra ngoài panel = đóng. Chỉ SetAsFirstSibling khi vừa tạo (không đảo thứ tự Sếp đã xếp).
        /// </summary>
        public static Button DimBehindPanel(Transform panel, UnityAction onClick = null, Color? color = null)
        {
            bool created;
            RectTransform rt = Child(panel, "Img_Dim", new Vector2(6000f, 6000f), Vector2.zero, out created);
            Image img = GetOrAdd<Image>(rt.gameObject);
            if (created) { img.sprite = null; img.color = color ?? DimLight; img.raycastTarget = true; rt.SetAsFirstSibling(); }
            Button btn = GetOrAdd<Button>(rt.gameObject);
            if (created) { btn.transition = Selectable.Transition.None; }
            if (onClick != null) { btn.onClick.AddListener(onClick); }
            return btn;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  DANH SÁCH CUỘN / HÀNG
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>ScrollRect dọc + RectMask2D, con "Content" có VerticalLayoutGroup + ContentSizeFitter. Hàng con cần LayoutElement.preferredHeight.</summary>
        public static ScrollRect ScrollList(Transform parent, string name, Vector2 size, out RectTransform content, Vector2 anchoredPos = default, float spacing = 10f, int padding = 12)
        {
            bool created;
            RectTransform rt = Child(parent, name, size, anchoredPos, out created);
            Image bg = GetOrAdd<Image>(rt.gameObject);
            if (created) { bg.sprite = null; bg.color = new Color(0f, 0f, 0f, 0.06f); bg.raycastTarget = true; }
            GetOrAdd<RectMask2D>(rt.gameObject);
            ScrollRect sr = GetOrAdd<ScrollRect>(rt.gameObject);

            bool contentCreated;
            content = Child(rt, "Content", Vector2.zero, Vector2.zero, out contentCreated);
            if (contentCreated)
            {
                content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
                content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
                content.sizeDelta = new Vector2(0f, 0f);
            }
            VerticalLayoutGroup vlg = GetOrAdd<VerticalLayoutGroup>(content.gameObject);
            if (contentCreated)
            {
                vlg.spacing = spacing;
                vlg.padding = new RectOffset(padding, padding, padding, padding);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true; vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            }
            ContentSizeFitter csf = GetOrAdd<ContentSizeFitter>(content.gameObject);
            if (contentCreated) { csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize; csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; }

            if (created)
            {
                sr.content = content; sr.viewport = rt;
                sr.horizontal = false; sr.vertical = true;
                sr.movementType = ScrollRect.MovementType.Clamped;
                sr.scrollSensitivity = 30f;
            }
            if (sr.content == null) { sr.content = content; }
            return sr;
        }

        /// <summary>Hàng trong danh sách cuộn: Image nền + LayoutElement chiều cao cố định. Con bên trong neo theo mép hàng.</summary>
        public static RectTransform Row(Transform content, string name, float height, Sprite sprite, Color? fallback = null)
        {
            bool created;
            RectTransform rt = Child(content, name, new Vector2(0f, height), Vector2.zero, out created);
            Image img = GetOrAdd<Image>(rt.gameObject);
            if (created || img.sprite == null) { SetSlicedOrSimple(img, sprite, fallback ?? new Color(1f, 1f, 1f, 0.35f)); }
            LayoutElement le = GetOrAdd<LayoutElement>(rt.gameObject);
            le.preferredHeight = height; le.minHeight = height;
            return rt;
        }

        /// <summary>Con neo mép trái (giữa dọc) của hàng.</summary>
        public static RectTransform AnchorLeft(RectTransform rt, float x, float y = 0f)
        {
            if (rt == null) { return null; }
            rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(0f, 0.5f); rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            return rt;
        }

        /// <summary>Con neo mép phải (giữa dọc) của hàng.</summary>
        public static RectTransform AnchorRight(RectTransform rt, float x, float y = 0f)
        {
            if (rt == null) { return null; }
            rt.anchorMin = new Vector2(1f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f); rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-x, y);
            return rt;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Ô NHẬP
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>TMP_InputField dựng code: Text Area (RectMask2D) + Text + Placeholder. Mobile tự bật bàn phím khi focus.</summary>
        public static TMP_InputField InputField(Transform parent, string name, Vector2 size, string placeholder, int charLimit, Vector2 anchoredPos = default, float fontSize = 28f)
        {
            bool created;
            RectTransform rt = Child(parent, name, size, anchoredPos, out created);
            Image bg = GetOrAdd<Image>(rt.gameObject);
            if (created || bg.sprite == null) { SetSlicedOrSimple(bg, UIStandardSprites.RowDark, (Color)new Color32(60, 50, 40, 230)); }

            bool areaCreated;
            RectTransform area = Child(rt, "Text Area", Vector2.zero, Vector2.zero, out areaCreated);
            if (areaCreated) { Stretch(area, 12f); }
            GetOrAdd<RectMask2D>(area.gameObject);

            TextMeshProUGUI txt = Label(area, "Text", string.Empty, fontSize, TextAlignmentOptions.Left, Vector2.zero, Vector2.zero, TextLight);
            TextMeshProUGUI ph = Label(area, "Placeholder", placeholder, fontSize, TextAlignmentOptions.Left, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.45f));
            if (created)
            {
                Stretch(txt.rectTransform); Stretch(ph.rectTransform);
                txt.textWrappingMode = TextWrappingModes.NoWrap; txt.overflowMode = TextOverflowModes.Overflow;
                ph.textWrappingMode = TextWrappingModes.NoWrap; ph.overflowMode = TextOverflowModes.Overflow;
                ph.fontStyle = FontStyles.Italic;
            }
            txt.text = string.Empty;

            TMP_InputField input = GetOrAdd<TMP_InputField>(rt.gameObject);
            if (input.textViewport == null) { input.textViewport = area; }
            if (input.textComponent == null) { input.textComponent = txt; }
            if (input.placeholder == null) { input.placeholder = ph; }
            if (input.targetGraphic == null) { input.targetGraphic = bg; }
            if (SkinKit.FontVo != null) { input.fontAsset = SkinKit.FontVo; }
            input.pointSize = fontSize;
            input.characterLimit = charLimit;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            return input;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  TIỆN ÍCH KHÁC
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Điểm màn hình (pixel) của 1 UI trong Canvas overlay/camera — dùng cho RewardFlyFX.GoiYDiemXuatPhat.</summary>
        public static Vector2 ScreenPointOf(Component ui)
        {
            if (ui == null) { return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); }
            Canvas canvas = ui.GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
            return RectTransformUtility.WorldToScreenPoint(cam, ui.transform.position);
        }

        /// <summary>Định dạng số nguyên theo InvariantCulture (không dấu chấm/phẩy lệch máy).</summary>
        public static string Num(int v) { return v.ToString(CultureInfo.InvariantCulture); }

        /// <summary>"x.x kg" theo InvariantCulture.</summary>
        public static string Kg(float kg) { return kg.ToString("0.0", CultureInfo.InvariantCulture) + " kg"; }

        /// <summary>Chuỗi sao theo tier 1..4.</summary>
        public static string Stars(int tier, int max = 4)
        {
            var sb = new System.Text.StringBuilder(max);
            for (int i = 0; i < max; i++) { sb.Append(i < tier ? '★' : '☆'); }
            return sb.ToString();
        }

        /// <summary>Bật ancestor đang tắt, dừng ở Canvas gần nhất (mẫu TrainLoadPopupUI).</summary>
        public static void ActivateUpToCanvas(Transform t)
        {
            Transform p = t != null ? t.parent : null;
            while (p != null)
            {
                if (!p.gameObject.activeSelf) { p.gameObject.SetActive(true); }
                if (p.GetComponent<Canvas>() != null) { break; }
                p = p.parent;
            }
        }
    }
}
