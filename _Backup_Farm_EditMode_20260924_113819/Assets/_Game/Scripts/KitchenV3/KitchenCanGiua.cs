// ============================================================================
//  KitchenCanGiua — ra soat bep V3: CAN GIUA chu + icon, chu KHONG loi ra khoi khung,
//  o trong / nut mo o DUNG CHUNG 1 khung voi the nguyen lieu (2026-09-24)
//  Dung chung cho: Tools > Kitchen V3 > 13 (sua han trong scene, co Undo) va luc Play (KitchenJuiceFX
//  goi moi giay cho o/the sinh luc chay). Moi o chu / icon chi xu ly 1 lan.
//  Khong dong vao (giu can trai/phai co chu dich): hang Huong vi (Label/Value), tieu de muc,
//  chu trong chip don hang, bang den, lop hieu ung Fx_*.
// ============================================================================
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class KitchenCanGiua
{
    private static readonly HashSet<int> _daXong = new HashSet<int>();
    private static readonly List<TMP_Text> _dsChu = new List<TMP_Text>(256);
    private static readonly List<SelectableIngredientCard> _dsThe = new List<SelectableIngredientCard>(64);

    private static readonly string[] BoQuaTen = { "Txt_NeedTitle", "Txt_TasteTitle", "Txt_Chalk", "Txt_Title", "Label", "Value", "Txt_Time" };
    private static readonly string[] BoQuaCha = { "Flavor_Row_", "Chip_", "Fx_", "Chalkboard", "Need_Chips", "Title_Cooking" };

    /// <summary>Tra ve so muc da sua. choPhepLap = true (tool Editor): xu ly lai ca muc da lam.</summary>
    public static int ApDung(Transform goc, bool choPhepLap = false, bool giuTrongKhung = true)
    {
        if (goc == null) return 0;
        int n = CanKhungThongTinMon(goc);
        _dsChu.Clear();
        goc.GetComponentsInChildren(true, _dsChu);
        foreach (var t in _dsChu)
        {
            if (t == null) continue;
            int id = t.GetInstanceID();
            if (!choPhepLap && _daXong.Contains(id)) continue;
            _daXong.Add(id);
            if (BoQua(t.transform)) continue;
            if (CanGiuaChu(t)) n++;
            if (giuTrongKhung && GiuTrongKhung(t.rectTransform)) n++;
            if (giuTrongKhung && KeoVeGiua(t.rectTransform)) n++;
        }
        _dsChu.Clear();

        // Icon chinh cua the nguyen lieu: can giua ngang
        _dsThe.Clear();
        goc.GetComponentsInChildren(true, _dsThe);
        foreach (var the in _dsThe)
        {
            if (the == null) continue;
            var ic = the.transform.Find("Img_MainIcon") as RectTransform;
            if (ic == null) continue;
            int id = ic.GetInstanceID();
            if (!choPhepLap && _daXong.Contains(id)) continue;
            _daXong.Add(id);
            if (Mathf.Abs(ic.anchorMin.x - 0.5f) > 0.001f || Mathf.Abs(ic.anchorMax.x - 0.5f) > 0.001f || Mathf.Abs(ic.anchoredPosition.x) > 0.5f || Mathf.Abs(ic.pivot.x - 0.5f) > 0.001f)
            {
                ic.anchorMin = new Vector2(0.5f, ic.anchorMin.y);
                ic.anchorMax = new Vector2(0.5f, ic.anchorMax.y);
                ic.pivot = new Vector2(0.5f, ic.pivot.y);
                ic.anchoredPosition = new Vector2(0f, ic.anchoredPosition.y);
                n++;
            }
        }
        _dsThe.Clear();
        return n;
    }

    // ------------------------------------------------------------------
    //  Bang chi tiet mon: ten mon / do kho-cap / thuong NAM GIUA khung Card_Info
    //  (tu mep phai anh mon toi mep phai khung), can giua chu.
    // ------------------------------------------------------------------
    private static readonly string[] ChuThongTinMon = { "Txt_DishName", "Txt_DishMeta", "Txt_Rewards" };

    public static int CanKhungThongTinMon(Transform goc)
    {
        int n = 0;
        if (goc == null) return 0;
        foreach (var bdT in goc.GetComponentsInChildren<RectTransform>(true))
        {
            if (bdT.name != "Board_Detail") continue;
            var card = bdT.Find("Card_Info") as RectTransform;
            if (card == null) continue;
            var anh = bdT.Find("Img_Dish") as RectTransform;
            Rect rc = RectTrong(bdT, card);
            float trai = anh != null ? Mathf.Max(rc.xMin, RectTrong(bdT, anh).xMax) + 10f : rc.xMin + 14f;
            float phai = rc.xMax - 14f;
            if (phai - trai < 60f) continue;
            foreach (var ten in ChuThongTinMon)
            {
                var t = bdT.Find(ten) as RectTransform;
                if (t == null) continue;
                Rect r0 = RectTrong(bdT, t);
                t.anchorMin = t.anchorMax = new Vector2(0f, 1f);
                t.pivot = new Vector2(0f, 1f);
                t.sizeDelta = new Vector2(phai - trai, r0.height);
                t.anchoredPosition = new Vector2(trai - bdT.rect.xMin, r0.yMax - bdT.rect.yMax);
                var tmp = t.GetComponent<TMP_Text>();
                if (tmp != null) CanGiuaChu(tmp);
                n++;
            }
        }
        return n;
    }

    /// <summary>Hinh chu nhat cua con, tinh trong toa do cuc bo cua cha.</summary>
    private static Rect RectTrong(RectTransform cha, RectTransform con)
    {
        var goc = new Vector3[4];
        con.GetWorldCorners(goc);
        Vector3 a = cha.InverseTransformPoint(goc[0]), b = cha.InverseTransformPoint(goc[2]);
        return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }

    private static bool BoQua(Transform t)
    {
        foreach (var ten in BoQuaTen) if (t.name == ten) return true;
        for (var p = t.parent; p != null; p = p.parent)
            foreach (var c in BoQuaCha) if (p.name.StartsWith(c)) return true;
        return false;
    }

    private static bool CanGiuaChu(TMP_Text t)
    {
        var a = t.alignment;
        TextAlignmentOptions moi;
        switch (a)
        {
            case TextAlignmentOptions.Left: case TextAlignmentOptions.Right: case TextAlignmentOptions.Justified: moi = TextAlignmentOptions.Center; break;
            case TextAlignmentOptions.TopLeft: case TextAlignmentOptions.TopRight: moi = TextAlignmentOptions.Top; break;
            case TextAlignmentOptions.BottomLeft: case TextAlignmentOptions.BottomRight: moi = TextAlignmentOptions.Bottom; break;
            case TextAlignmentOptions.BaselineLeft: case TextAlignmentOptions.BaselineRight: moi = TextAlignmentOptions.Baseline; break;
            case TextAlignmentOptions.MidlineLeft: case TextAlignmentOptions.MidlineRight: moi = TextAlignmentOptions.Midline; break;
            case TextAlignmentOptions.CaplineLeft: case TextAlignmentOptions.CaplineRight: moi = TextAlignmentOptions.Capline; break;
            default: return false;
        }
        t.alignment = moi;
        return true;
    }

    /// <summary>O chu neo giua khung cha nhung bi lech ngang it (vd -13px) -> keo ve dung tam.</summary>
    private static bool KeoVeGiua(RectTransform rt)
    {
        var cha = rt.parent as RectTransform;
        if (cha == null || cha.GetComponent<Image>() == null || cha.GetComponent<LayoutGroup>() != null) return false;
        bool neoGiua = Mathf.Abs(rt.anchorMin.x - 0.5f) < 0.001f && Mathf.Abs(rt.anchorMax.x - 0.5f) < 0.001f;
        bool keoGian = rt.anchorMin.x < 0.001f && rt.anchorMax.x > 0.999f;
        if (!neoGiua && !keoGian) return false;
        float x = rt.anchoredPosition.x;
        if (Mathf.Abs(x) < 0.5f || Mathf.Abs(x) > 30f) return false;
        if (neoGiua && Mathf.Abs(rt.pivot.x - 0.5f) > 0.001f) return false;
        rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
        return true;
    }

    /// <summary>O chu thò ra ngoai khung cha (cha co Image) -> thu hep vao trong, le 10px.</summary>
    private static bool GiuTrongKhung(RectTransform rt)
    {
        var cha = rt.parent as RectTransform;
        if (cha == null || cha.GetComponent<Image>() == null) return false;
        if (cha.GetComponent<LayoutGroup>() != null) return false;          // do layout dieu khien
        if (Mathf.Abs(rt.anchorMin.x - rt.anchorMax.x) > 0.001f) return false; // chi xu ly o neo 1 diem
        Rect r = cha.rect;
        if (r.width < 20f) return false;
        const float LE = 10f;
        float w = rt.rect.width;
        float neoX = Mathf.Lerp(r.xMin, r.xMax, rt.anchorMin.x);
        float trai = neoX + rt.anchoredPosition.x - rt.pivot.x * w;
        float phai = trai + w;
        float traiMoi = Mathf.Max(trai, r.xMin + LE), phaiMoi = Mathf.Min(phai, r.xMax - LE);
        if (traiMoi <= trai + 0.5f && phaiMoi >= phai - 0.5f) return false;
        if (phaiMoi - traiMoi < 30f) return false;
        float wMoi = phaiMoi - traiMoi;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x + (wMoi - w), rt.sizeDelta.y);
        rt.anchoredPosition = new Vector2(traiMoi + rt.pivot.x * wMoi - neoX, rt.anchoredPosition.y);
        return true;
    }

    // ------------------------------------------------------------------
    //  O trong + nut mo o: cung khung voi the nguyen lieu, icon vang truoc gia
    // ------------------------------------------------------------------

    public static int DongBoOTrong(Transform goc)
    {
        if (goc == null) return 0;
        int n = 0;
        foreach (var ten in new[] { "Scroll_Grid_Ingredients", "Scroll_Grid_Seasonings" })
        {
            var g = TimSau(goc, ten);
            if (g == null) continue;
            var the = g.GetComponentInChildren<SelectableIngredientCard>(true);
            var imgThe = the != null ? the.GetComponent<Image>() : null;
            if (imgThe == null || imgThe.sprite == null) continue;

            var content = the.transform.parent;
            for (int i = 0; i < content.childCount; i++)
            {
                var c = content.GetChild(i);
                bool oTrong = c.name.StartsWith("Slot_Empty_");
                bool nutMua = c.name == "Btn_BuySlots";
                if (!oTrong && !nutMua) continue;
                var img = c.GetComponent<Image>();
                if (img == null) continue;
                if (img.sprite != imgThe.sprite)
                {
                    img.sprite = imgThe.sprite;
                    img.type = imgThe.type;
                    img.pixelsPerUnitMultiplier = imgThe.pixelsPerUnitMultiplier;
                    img.color = oTrong ? new Color(1f, 1f, 1f, 0.6f) : Color.white;
                    n++;
                }
                foreach (var t in c.GetComponentsInChildren<TMP_Text>(true))
                {
                    t.alignment = TextAlignmentOptions.Center;
                    if (nutMua) t.color = new Color(0x8B / 255f, 0x45 / 255f, 0x13 / 255f);
                }
                if (nutMua && c.Find("Row_Cost") == null) { TachDongGia(c); n++; }
            }
        }
        return n;
    }

    /// <summary>Nut "+ Mo 7 o / 500 vang" -> dong 1 chu, dong 2 = [icon vang] 500, ca 2 can giua.</summary>
    private static void TachDongGia(Transform nut)
    {
        var t = nut.GetComponentInChildren<TMP_Text>(true);
        if (t == null) return;
        string[] dong = t.text.Split('\n');
        if (dong.Length < 2) return;
        var m = Regex.Match(dong[1], @"[\d][\d.,]*");
        string gia = m.Success ? m.Value : dong[1];
        t.text = dong[0];
        var trt = t.rectTransform;
        trt.anchorMin = new Vector2(0f, 0.5f); trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = new Vector2(6f, 0f); trt.offsetMax = new Vector2(-6f, -6f);
        t.alignment = TextAlignmentOptions.Center;

        var row = new GameObject("Row_Cost", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.layer = nut.gameObject.layer;
        var rrt = (RectTransform)row.transform; rrt.SetParent(nut, false);
        rrt.anchorMin = new Vector2(0f, 0f); rrt.anchorMax = new Vector2(1f, 0.5f);
        rrt.offsetMin = new Vector2(6f, 8f); rrt.offsetMax = new Vector2(-6f, 0f);
        var hl = row.GetComponent<HorizontalLayoutGroup>();
        hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = 6f;
        hl.childControlWidth = false; hl.childControlHeight = false;
        hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;

        var cu = nut.Find("Img_Gold");
        Sprite vang = null;
        if (cu != null) { var ci = cu.GetComponent<Image>(); if (ci != null) vang = ci.sprite; cu.gameObject.SetActive(false); }
        if (vang == null) { var lib = RewardIconLibrary.Instance; if (lib != null) vang = lib.goldSprite; }
        if (vang == null) vang = Resources.Load<Sprite>("UI/Standard/icon_gold");
        var ig = new GameObject("Img_Gold", typeof(RectTransform), typeof(Image));
        ig.layer = nut.gameObject.layer;
        var irt = (RectTransform)ig.transform; irt.SetParent(rrt, false); irt.sizeDelta = new Vector2(26f, 26f);
        var ii = ig.GetComponent<Image>(); ii.sprite = vang; ii.preserveAspect = true; ii.raycastTarget = false;

        var tg = new GameObject("Txt_Cost", typeof(RectTransform));
        tg.layer = nut.gameObject.layer;
        var grt = (RectTransform)tg.transform; grt.SetParent(rrt, false);
        var tc = tg.AddComponent<TextMeshProUGUI>();
        tc.font = t.font; tc.fontSize = t.fontSize; tc.fontStyle = FontStyles.Bold;
        tc.color = new Color(0.72f, 0.5f, 0.06f); tc.alignment = TextAlignmentOptions.Left;
        tc.textWrappingMode = TextWrappingModes.NoWrap; tc.raycastTarget = false;
        tc.text = gia;
        Vector2 kt = tc.GetPreferredValues(gia);
        grt.sizeDelta = new Vector2(kt.x + 4f, 28f);
    }

    private static Transform TimSau(Transform cha, string ten)
    {
        if (cha == null) return null;
        for (int i = 0; i < cha.childCount; i++)
        {
            var c = cha.GetChild(i);
            if (c.name == ten) return c;
            var s = TimSau(c, ten);
            if (s != null) return s;
        }
        return null;
    }
}
