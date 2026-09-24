using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenUIv3
{
    /// <summary>
    /// Hang chip NGUYEN LIEU tren the don khach (giong mau): moi chip = o kem bo goc + icon
    /// nguyen lieu + "da chon/can" (vd 0/1), cuoi hang la chip dong ho + thoi gian nau.
    ///
    /// Nguon du lieu y het V2 RefreshStatic(): mon = TouristVisitorManager.GetFrontWaitingTourist().Dish,
    /// khong co khach thi = CookingChallengeManager.CurrentDish. "Da chon" = nguyen lieu do dang duoc
    /// chon trong khay (CookingSelectionManager.GetSelectedIngredientCards()).
    /// Chip duoc TAI SU DUNG (khong xoa/tao moi frame), 4 lan/giay.
    /// </summary>
    public class KitchenV3OrderChips : MonoBehaviour
    {
        [Header("Khung chip (tool gan)")]
        public Sprite nenChip;                 // card_rounded_white
        public Sprite iconDongHo;              // anim_clock_01
        public Vector2 kichThuocChip = new Vector2(112f, 44f);
        public float nhip = 0.25f;

        private readonly List<GameObject> _chips = new List<GameObject>();
        private GameObject _chipGio;
        private float _t;
        private CookingChallengeManager _challenge;
        private CookingSelectionManager _selection;
        private HorizontalLayoutGroup _hl;

        private void Awake()
        {
            // [2026-09-23] DUNG LAI chip da dung san trong Edit mode (tool "Can bo cuc bep") —
            // Sep chinh tay vi tri/kich thuoc o Edit mode thi Play giu nguyen, chi thay icon + so.
            for (int i = 0; ; i++)
            {
                var c = transform.Find("Chip_Need_" + i);
                if (c == null) break;
                _chips.Add(c.gameObject);
            }
            var gio = transform.Find("Chip_Time");
            if (gio != null) _chipGio = gio.gameObject;

            _hl = GetComponent<HorizontalLayoutGroup>();
            if (_hl == null)
            {
                _hl = gameObject.AddComponent<HorizontalLayoutGroup>();
                _hl.spacing = 8f; _hl.childAlignment = TextAnchor.MiddleLeft;
                _hl.childControlWidth = false; _hl.childControlHeight = false;
                _hl.childForceExpandWidth = false; _hl.childForceExpandHeight = false;
            }
        }

        private void Start()
        {
            _challenge = FindFirstObjectByType<CookingChallengeManager>(FindObjectsInactive.Include);
            _selection = FindFirstObjectByType<CookingSelectionManager>(FindObjectsInactive.Include);
            CapNhat();
        }

        private void Update()
        {
            _t += Time.unscaledDeltaTime;
            if (_t < nhip) return;
            _t = 0f; CapNhat();
        }

        private DishData MonDangDat()
        {
            var tm = TouristVisitorManager.Instance;
            var khach = tm != null ? tm.GetFrontWaitingTourist() : null;
            if (khach != null && khach.Dish != null) return khach.Dish;
            return _challenge != null ? _challenge.CurrentDish : null;
        }

        private HashSet<string> DaChon()
        {
            var set = new HashSet<string>();
            if (_selection == null) return set;
            try
            {
                foreach (var c in _selection.GetSelectedIngredientCards())
                {
                    var d = c != null ? c.GetIngredientData() : null;
                    if (d != null && !string.IsNullOrEmpty(d.id)) set.Add(d.id.Trim().ToLower());
                }
                foreach (var c in _selection.GetSelectedSeasoningCards())
                {
                    var d = c != null ? c.GetIngredientData() : null;
                    if (d != null && !string.IsNullOrEmpty(d.id)) set.Add(d.id.Trim().ToLower());
                }
            }
            catch { }
            return set;
        }

        private void CapNhat()
        {
            var mon = MonDangDat();
            var can = mon != null ? mon.requiredIngredients : null;
            int n = can != null ? can.Count : 0;
            var chon = DaChon();

            while (_chips.Count < n) _chips.Add(TaoChip("Chip_Need_" + _chips.Count));
            CoGianChip(n);
            for (int i = 0; i < _chips.Count; i++)
            {
                bool hien = i < n && can[i] != null;
                if (_chips[i].activeSelf != hien) _chips[i].SetActive(hien);
                if (!hien) continue;
                var ing = can[i];
                GanChip(_chips[i], ing.icon, (chon.Contains((ing.id ?? "").Trim().ToLower()) ? 1 : 0) + "/1");
            }

            // chip dong ho: thoi gian nau (CookingChallengeManager.cookSubmitDelay)
            if (_chipGio == null) _chipGio = TaoChip("Chip_Time");
            bool coMon = mon != null;
            if (_chipGio.activeSelf != coMon) _chipGio.SetActive(coMon);
            if (coMon)
            {
                // Chip gio da neo rieng (ignoreLayout) thi giu nguyen cho; chip tu sinh thi dua ve cuoi hang.
                var le = _chipGio.GetComponent<LayoutElement>();
                Sprite dongHo = iconDongHo;
                if (dongHo == null) { var im = _chipGio.transform.Find("Img")?.GetComponent<Image>(); if (im != null) dongHo = im.sprite; }
                GanChip(_chipGio, dongHo, DinhDangGio(ThoiGianNau()));
                if (le == null || !le.ignoreLayout) _chipGio.transform.SetAsLastSibling();
            }
        }

        /// <summary>
        /// [2026-09-23] 4-5 nguyen lieu thi chip cuoi de len chip dong ho (neo phai). Tinh be ngang con
        /// trong = rong hang - chip gio - khoang cach, chia deu cho n chip (toi da = kich thuoc thiet ke).
        /// </summary>
        private void CoGianChip(int n)
        {
            if (n <= 0) return;
            var rt = (RectTransform)transform;
            float rong = rt.rect.width;
            if (rong < 10f) return;
            float gian = _hl != null ? _hl.spacing : 8f;
            float gio = 0f;
            if (_chipGio != null)
            {
                var le = _chipGio.GetComponent<LayoutElement>();
                gio = ((RectTransform)_chipGio.transform).sizeDelta.x + gian + 4f;
                if (le == null || !le.ignoreLayout) gio = 0f;   // chip gio nam trong hang -> da tinh trong n
            }
            int soO = (_chipGio != null && gio == 0f) ? n + 1 : n;
            float w = Mathf.Min(kichThuocChip.x, (rong - gio - gian * (soO - 1)) / soO);
            w = Mathf.Max(56f, w);
            for (int i = 0; i < _chips.Count; i++)
            {
                var c = _chips[i]; if (c == null) continue;
                var crt = (RectTransform)c.transform;
                if (Mathf.Abs(crt.sizeDelta.x - w) < 0.5f) continue;
                crt.sizeDelta = new Vector2(w, crt.sizeDelta.y);
                var le = c.GetComponent<LayoutElement>();
                if (le != null) le.preferredWidth = w;
            }
        }

        private float ThoiGianNau()
        {
            // [2026-09-23] Dang nau -> dem nguoc giay con lai; chua nau -> thoi gian nau cua mon.
            if (_challenge != null && _challenge.IsCooking) return Mathf.Ceil(_challenge.CookRemaining);
            var mon = MonDangDat();
            if (mon != null) return CookingChallengeManager.CookTimeOf(mon);
            if (_challenge == null) return 0f;
            try
            {
                var f = typeof(CookingChallengeManager).GetField("cookSubmitDelay", BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) return (float)f.GetValue(_challenge);
            }
            catch { }
            return 0f;
        }

        private static string DinhDangGio(float s)
        {
            if (s >= 60f) { int m = (int)(s / 60f); int r = Mathf.RoundToInt(s - m * 60); return r > 0 ? $"{m}m{r:00}" : $"{m}m"; }
            return Mathf.CeilToInt(s) + "s";
        }

        private GameObject TaoChip(string ten)
        {
            var go = new GameObject(ten, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform; rt.sizeDelta = kichThuocChip;
            var le = go.GetComponent<LayoutElement>(); le.preferredWidth = kichThuocChip.x; le.preferredHeight = kichThuocChip.y;
            var img = go.GetComponent<Image>(); img.sprite = nenChip; img.type = Image.Type.Sliced; img.raycastTarget = false;
            if (nenChip == null) img.color = new Color(1f, 0.97f, 0.90f);

            var ic = new GameObject("Img", typeof(RectTransform), typeof(Image)); ic.transform.SetParent(go.transform, false);
            var irt = (RectTransform)ic.transform; irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f); irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(8f, 0f); irt.sizeDelta = new Vector2(kichThuocChip.y - 12f, kichThuocChip.y - 12f);
            var ii = ic.GetComponent<Image>(); ii.preserveAspect = true; ii.raycastTarget = false;

            var tx = new GameObject("Txt", typeof(RectTransform)); tx.transform.SetParent(go.transform, false);
            var trt = (RectTransform)tx.transform; trt.anchorMin = new Vector2(0f, 0f); trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(kichThuocChip.y + 2f, 0f); trt.offsetMax = new Vector2(-6f, 0f);
            var t = tx.AddComponent<TextMeshProUGUI>(); t.fontSize = 17; t.fontStyle = FontStyles.Bold; t.color = new Color(0.36f, 0.20f, 0.09f);
            t.alignment = TextAlignmentOptions.Left; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.NoWrap;
            try { var f = SkinKit.FontVo; if (f != null) t.font = f; } catch { }
            return go;
        }

        private static void GanChip(GameObject chip, Sprite icon, string chu)
        {
            var ii = chip.transform.Find("Img")?.GetComponent<Image>();
            if (ii != null) { ii.sprite = icon; ii.enabled = icon != null; }
            var t = chip.transform.Find("Txt")?.GetComponent<TMP_Text>();
            if (t != null && t.text != chu) t.text = chu;
        }
    }
}
