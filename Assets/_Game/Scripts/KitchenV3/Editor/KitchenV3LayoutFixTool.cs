// ============================================================================
//  KITCHEN V3 — CAN BO CUC (Edit mode = Play mode)
//  Tools > Kitchen V3 > 6. Can bo cuc: The don + Khay + So luong
// ----------------------------------------------------------------------------
//  Vi sao co tool nay (2026-09-23):
//    Tool cu "Kitchen Cozy V3 / 6 + 7" tim "Order_Banner" / "Grid_Ingredients" bang ten
//    tren TOAN scene => trung Kitchen_UI_v2 (dang TAT) nen sua nham cho, V3 dang chay khong
//    doi gi. Tool nay CHI lam tren canvas KitchenSceneV2UI DANG BAT (Kitchen_UI_v3).
//
//  Lam gi:
//    1. The don khach theo mau "Tomato Pasta": dia mon ben trai, ten mon o tren, hang chip
//       nguyen lieu (o kem bo goc + icon + 0/1) o duoi, chip dong ho neo sat mep phai.
//       Chip la GameObject THAT (Chip_Need_0..3, Chip_Time) — KitchenV3OrderChips luc Play
//       chi thay icon/so tren dung cac chip nay, khong tu dung moi.
//       An: Txt_Rewards, Img_Avatar, Chip_0..4 (hang cham cu).
//    2. Khay nguyen lieu / gia vi: "x0" dua VAO o nau Qty_Badge (truoc nam ngoai, 200x50
//       giua the). O trong + nut "Mo 7 o" chu nam gon trong o.
//
//  AN TOAN: sao luu file scene vao _Backup_Scene_TruocKhiVa truoc khi sua; Undo day du;
//  chi SetActive(false), khong xoa object nao. Tu chay 1 LAN khi mo scene bep (co the
//  chay lai bang menu bat cu luc nao — chay lai khong hong gi).
// ============================================================================
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using KitchenUIv2;
using KitchenUIv3;

[InitializeOnLoad]
public static class KitchenV3LayoutFixTool
{
    private const string ROOT_V3 = "Kitchen_UI_v3";

    private static readonly Color Brown     = new Color(0.36f, 0.20f, 0.09f);
    private static readonly Color Cream     = new Color(0.99f, 0.96f, 0.88f);
    private static readonly Color BadgeNau  = new Color(0.36f, 0.22f, 0.10f, 0.92f);

    // Kich thuoc chip theo mau (the don rong ~639): 4 chip nguyen lieu 96 + chip gio 92 neo phai
    private static readonly Vector2 ChipNguyenLieu = new Vector2(96f, 46f);
    private static readonly Vector2 ChipGio        = new Vector2(92f, 46f);
    // Do cao hang chip tinh tu day the don (the cao ~102). 18 = gan giua, van chua cho cho ten mon o tren.
    private const float HangChipY = 6f;     // hang chip sat day, nhuong phan tren cho khung ten mon

    static KitchenV3LayoutFixTool()
    {
        EditorApplication.delayCall += ThuTuChay;
        EditorSceneManager.sceneOpened += (s, m) => EditorApplication.delayCall += ThuTuChay;
    }

    // v2 (2026-09-23 chieu): them Bang chi tiet mon + icon dong mon + bang den + the don sau gia treo.
    private static string KhoaDaChay => "KitchenV3LayoutFix_v2|" + Application.dataPath;

    private static void ThuTuChay()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (EditorPrefs.GetBool(KhoaDaChay, false)) return;
        var goc = TimGocV3();
        if (goc == null) return;               // chua mo scene bep -> doi lan mo sau
        if (ChayTatCa(goc, true)) EditorPrefs.SetBool(KhoaDaChay, true);
    }

    [MenuItem("Tools/Kitchen V3/6. Can bo cuc: The don + Khay + So luong (Edit = Play)", false, 30)]
    private static void Menu()
    {
        var goc = TimGocV3();
        if (goc == null)
        {
            EditorUtility.DisplayDialog("Kitchen V3", "Khong tim thay canvas bep DANG BAT (Kitchen_UI_v3).\nHay mo SampleScene truoc.", "OK");
            return;
        }
        ChayTatCa(goc, false);
    }

    // Hang chip nam trong HorizontalLayoutGroup => keo tung chip bang tay KHONG duoc (layout giu).
    // Muon dich ca hang: chon Order_Card/Need_Row roi sua Pos Y — hoac bam menu nay.
    [MenuItem("Tools/Kitchen V3/9. The don: khung ten bo goc + hang chip", false, 32)]
    private static void MenuNangHangChip()
    {
        var goc = TimGocV3();
        var card = goc != null ? Tim(goc, "Order_Banner/Order_Card") : null;
        if (card == null) { EditorUtility.DisplayDialog("Kitchen V3", "Khong tim thay Order_Banner/Order_Card. Hay mo SampleScene.", "OK"); return; }
        Undo.RegisterFullObjectHierarchyUndo(card.gameObject, "Khung ten the don");
        string kq = KhungTenVaHangChip(card);
        EditorSceneManager.MarkSceneDirty(card.gameObject.scene);
        Debug.Log("[Kitchen V3] " + kq + " Bam Ctrl+S de luu.");
    }

    /// <summary>
    /// Ten mon: dat trong 1 khung kem bo goc (Name_Pill) o phan tren the — truoc chu nam tran
    /// len mep go va bi thanh go che. Hang chip ha sat day the (HangChipY).
    /// Name_Pill la anh RIENG nam ngay SAU Txt_Name trong thu tu (ve phia sau chu);
    /// Txt_Name van la con truc tiep cua Order_Card (code Play tim theo ten nay).
    /// </summary>
    private static string KhungTenVaHangChip(Transform card)
    {
        var cardRt = (RectTransform)card;
        float w = cardRt.rect.width > 10f ? cardRt.rect.width : 639f;
        var row = card.Find("Need_Row");
        float x0 = row != null ? ((RectTransform)row).anchoredPosition.x : 114f;
        if (row != null) ((RectTransform)row).anchoredPosition = new Vector2(x0, HangChipY);

        var chips = row != null ? row.GetComponent<KitchenV3OrderChips>() : null;
        Sprite nen = chips != null ? chips.nenChip : null;

        const float caoKhung = 30f, tren = -12f;
        float rongKhung = Mathf.Min(360f, w - x0 - 8f);

        var ten = card.Find("Txt_Name");
        var pill = card.Find("Name_Pill");
        if (pill == null)
        {
            var go = new GameObject("Name_Pill", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Name_Pill");
            go.transform.SetParent(card, false);
            pill = go.transform;
        }
        Dat((RectTransform)pill, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0, tren), new Vector2(rongKhung, caoKhung));
        var pi = pill.GetComponent<Image>();
        pi.sprite = nen;
        pi.type = nen != null ? Image.Type.Sliced : Image.Type.Simple;
        pi.color = new Color(1f, 0.97f, 0.89f, 1f);
        pi.raycastTarget = false;
        if (ten != null) pill.SetSiblingIndex(ten.GetSiblingIndex());   // ngay truoc Txt_Name => nam duoi chu

        if (ten != null)
        {
            Dat((RectTransform)ten, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0 + 12f, tren), new Vector2(rongKhung - 24f, caoKhung));
            var t = ten.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.enableAutoSizing = true; t.fontSizeMin = 13f; t.fontSizeMax = 20f; t.fontSize = 20f;
                t.fontStyle = FontStyles.Bold;
                t.color = Brown;
                t.alignment = TextAlignmentOptions.Left;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.overflowMode = TextOverflowModes.Ellipsis;
            }
        }
        return "The don: khung ten " + rongKhung + "x" + caoKhung + " (bo goc), hang chip Pos Y = " + HangChipY + ".";
    }

    [MenuItem("Tools/Kitchen V3/10. Bang chi tiet mon: the bo goc + can chu", false, 33)]
    private static void MenuChiTiet()
    {
        var goc = TimGocV3();
        if (goc == null) { EditorUtility.DisplayDialog("Kitchen V3", "Hay mo SampleScene truoc.", "OK"); return; }
        var d = Tim(goc, "Recipe_Board/Board_Detail");
        bool tat = d != null && !d.gameObject.activeSelf;
        if (d != null) Undo.RegisterFullObjectHierarchyUndo(d.gameObject, "Bang chi tiet");
        string kq = CanSachChiTiet(goc);
        if (d != null) { Selection.activeTransform = d; }
        EditorSceneManager.MarkSceneDirty(goc.gameObject.scene);
        Debug.Log("[Kitchen V3] " + kq + (tat ? " (Board_Detail dang TAT — bat len de xem, nho tat lai truoc khi luu)" : "") + " Bam Ctrl+S de luu.");
    }

    [MenuItem("Tools/Kitchen V3/11. Dua o nguyen lieu (bang chi tiet) ra Hierarchy + xep mon theo cap", false, 34)]
    private static void MenuOChip()
    {
        var goc = TimGocV3();
        if (goc == null) { EditorUtility.DisplayDialog("Kitchen V3", "Hay mo SampleScene truoc.", "OK"); return; }
        var d = Tim(goc, "Recipe_Board/Board_Detail");
        var chips = d != null ? d.Find("Need_Chips") : null;
        string kq = "";
        if (chips != null)
        {
            Undo.RegisterFullObjectHierarchyUndo(chips.gameObject, "O nguyen lieu");
            var gl = chips.GetComponent<GridLayoutGroup>();
            Vector2 o = gl != null ? gl.cellSize : new Vector2(66f, 68f);
            var theNeed = d.Find("Card_Need");
            Sprite sp = theNeed != null && theNeed.GetComponent<Image>() != null ? theNeed.GetComponent<Image>().sprite
                      : AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/KitchenCozyV3/Cut/card_rounded_white.png");
            DishData mau = MonMau();
            // xoa o tam kieu cu (Chip_<id>) neu lo luu vao scene
            for (int i = chips.childCount - 1; i >= 0; i--)
            {
                var c = chips.GetChild(i);
                if (c.name.StartsWith("Chip_") && !c.name.StartsWith("Chip_Slot_")) Undo.DestroyObjectImmediate(c.gameObject);
            }
            const int SO_O = 6;
            for (int i = 0; i < SO_O; i++)
            {
                var slot = chips.Find("Chip_Slot_" + i);
                bool moi = slot == null;
                if (moi)
                {
                    var go = new GameObject("Chip_Slot_" + i, typeof(RectTransform), typeof(Image));
                    Undo.RegisterCreatedObjectUndo(go, "O nguyen lieu");
                    go.transform.SetParent(chips, false);
                    slot = go.transform;
                    var im = go.GetComponent<Image>();
                    im.sprite = sp; im.type = sp != null ? Image.Type.Sliced : Image.Type.Simple; im.color = Color.white; im.raycastTarget = false;

                    float icon = Mathf.Min(o.x - 12f, o.y - 26f);
                    var ig = new GameObject("Img", typeof(RectTransform), typeof(Image));
                    ig.transform.SetParent(slot, false);
                    var irt = (RectTransform)ig.transform;
                    irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 1f); irt.pivot = new Vector2(0.5f, 1f);
                    irt.anchoredPosition = new Vector2(0f, -4f); irt.sizeDelta = new Vector2(icon, icon);
                    var ii = ig.GetComponent<Image>(); ii.preserveAspect = true; ii.raycastTarget = false;

                    var tg = new GameObject("Txt", typeof(RectTransform));
                    tg.transform.SetParent(slot, false);
                    var t = tg.AddComponent<TextMeshProUGUI>();
                    var f = LayFont(); if (f != null) t.font = f;
                    t.fontSize = 13f; t.enableAutoSizing = true; t.fontSizeMin = 8f; t.fontSizeMax = 13f;
                    t.color = NauChu; t.alignment = TextAlignmentOptions.Center; t.textWrappingMode = TextWrappingModes.NoWrap;
                    t.raycastTarget = false;
                    var trt = t.rectTransform;
                    trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0f); trt.pivot = new Vector2(0.5f, 0f);
                    trt.anchoredPosition = new Vector2(0f, 3f); trt.sizeDelta = new Vector2(o.x - 6f, 18f);
                }
                slot.SetSiblingIndex(i);
                // du lieu mau de nhin trong Edit mode (Play se thay bang mon that)
                IngredientData ing = mau != null && mau.requiredIngredients != null && i < mau.requiredIngredients.Count ? mau.requiredIngredients[i] : null;
                var img = slot.Find("Img") != null ? slot.Find("Img").GetComponent<Image>() : null;
                if (img != null && ing != null) { img.sprite = ing.icon; img.enabled = ing.icon != null; }
                var txt = slot.Find("Txt") != null ? slot.Find("Txt").GetComponent<TMP_Text>() : null;
                if (txt != null && ing != null) txt.text = LocSafe(ing.displayName);
                slot.gameObject.SetActive(ing != null || i < 3);
            }
            kq += "Need_Chips: " + SO_O + " o Chip_Slot_0..5 (chinh tay duoc). ";
        }

        // Xep dong mon theo cap mo khoa (giong luc Play)
        var content = Tim(goc, "Recipe_Board/Board_List/Dish_Scroll/Content");
        if (content != null)
        {
            Undo.RegisterFullObjectHierarchyUndo(content.gameObject, "Xep mon theo cap");
            var cap = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var guid in AssetDatabase.FindAssets("t:DishData"))
            {
                var dd = AssetDatabase.LoadAssetAtPath<DishData>(AssetDatabase.GUIDToAssetPath(guid));
                if (dd != null && !string.IsNullOrEmpty(dd.dishId)) cap[dd.dishId] = dd.unlockLevel;
            }
            var rows = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < content.childCount; i++) if (content.GetChild(i).name.StartsWith("Row_")) rows.Add(content.GetChild(i));
            var goc2 = new System.Collections.Generic.Dictionary<Transform, int>();
            for (int i = 0; i < rows.Count; i++) goc2[rows[i]] = i;
            System.Func<Transform, int> lv = r => cap.TryGetValue(r.name.Substring(4), out int v) ? v : 999;
            rows.Sort((a, b) => lv(a) != lv(b) ? lv(a).CompareTo(lv(b)) : goc2[a].CompareTo(goc2[b]));
            for (int i = 0; i < rows.Count; i++) rows[i].SetSiblingIndex(i);
            kq += rows.Count + " dong mon xep theo cap mo khoa.";
        }
        EditorSceneManager.MarkSceneDirty(goc.gameObject.scene);
        Debug.Log("[Kitchen V3] " + kq + " Bam Ctrl+S de luu.");
    }

    [MenuItem("Tools/Kitchen V3/7. Chi can lai ICON mon trong Sach cong thuc", false, 31)]
    private static void MenuIcon()
    {
        var goc = TimGocV3();
        if (goc == null) { EditorUtility.DisplayDialog("Kitchen V3", "Hay mo SampleScene truoc.", "OK"); return; }
        Undo.RegisterFullObjectHierarchyUndo(goc.gameObject, "Can icon mon");
        string kq = CanIconDongMon(goc);
        EditorSceneManager.MarkSceneDirty(goc.gameObject.scene);
        EditorUtility.DisplayDialog("Kitchen V3", kq + "\n\nChinh tay tung mon: chon Row_<mon>/Img_Icon, keo/scale roi Ctrl+S - Play giu nguyen.", "OK");
    }

    // ------------------------------------------------------------------------

    private static Transform TimGocV3()
    {
        Transform duPhong = null;
        foreach (var ui in Object.FindObjectsByType<KitchenSceneV2UI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ui == null || !ui.gameObject.activeInHierarchy) continue;
            if (EditorUtility.IsPersistent(ui)) continue;
            if (ui.gameObject.name == ROOT_V3) return ui.transform;
            if (duPhong == null) duPhong = ui.transform;
        }
        return duPhong;
    }

    private static bool ChayTatCa(Transform goc, bool imLang)
    {
        var scene = goc.gameObject.scene;
        bool daBanTruoc = scene.isDirty;
        SaoLuu(scene.path);

        Undo.RegisterFullObjectHierarchyUndo(goc.gameObject, "Can bo cuc bep V3");
        string kqDon  = CanTheDon(goc);
        string kqKhay = CanKhay(goc);
        string kqSach = CanSachChiTiet(goc);
        string kqMon  = CanIconDongMon(goc);
        string kqBang = CanBangDen(goc);

        EditorSceneManager.MarkSceneDirty(scene);
        bool daLuu = false;
        if (!daBanTruoc) daLuu = EditorSceneManager.SaveScene(scene);

        string tomTat = "[Kitchen V3] Can bo cuc xong tren '" + goc.name + "'.\n" + kqDon + "\n" + kqKhay + "\n" + kqSach + "\n" + kqMon + "\n" + kqBang +
                        (daLuu ? "\nDa luu scene." : "\nScene dang co thay doi chua luu -> bam Ctrl+S de luu.");
        Debug.Log(tomTat);
        if (!imLang) EditorUtility.DisplayDialog("Kitchen V3", tomTat, "OK");
        return true;
    }

    private static void SaoLuu(string duongDanScene)
    {
        if (string.IsNullOrEmpty(duongDanScene) || !File.Exists(duongDanScene)) return;
        try
        {
            Directory.CreateDirectory("_Backup_Scene_TruocKhiVa");
            string dich = Path.Combine("_Backup_Scene_TruocKhiVa",
                Path.GetFileNameWithoutExtension(duongDanScene) + "_CanBoCuc_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
            File.Copy(duongDanScene, dich, true);
        }
        catch (System.Exception e) { Debug.LogWarning("[Kitchen V3] Sao luu scene loi: " + e.Message); }
    }

    private static Transform Tim(Transform goc, string duong)
    {
        return goc.Find(duong) ?? goc.Find("~SafeArea/" + duong);
    }

    private static Transform TimSau(Transform cha, string ten)
    {
        if (cha == null) return null;
        if (cha.name == ten) return cha;
        for (int i = 0; i < cha.childCount; i++)
        {
            var t = TimSau(cha.GetChild(i), ten);
            if (t != null) return t;
        }
        return null;
    }

    private static void Tat(Transform cha, string ten)
    {
        var t = cha.Find(ten);
        if (t != null && t.gameObject.activeSelf) t.gameObject.SetActive(false);
    }

    private static void Dat(RectTransform rt, Vector2 neo, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = neo;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void KeoGian(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = min; rt.offsetMax = max;
    }

    private static TMP_FontAsset LayFont()
    {
        try { return SkinKit.FontVo; } catch { return null; }
    }

    // ------------------------------------------------------------------------
    //  1. THE DON KHACH
    // ------------------------------------------------------------------------

    private static string CanTheDon(Transform goc)
    {
        var banner = Tim(goc, "Order_Banner");
        var card = banner != null ? banner.Find("Order_Card") : null;
        if (card == null) return "- The don: KHONG tim thay Order_Banner/Order_Card (bo qua).";

        var cardRt = (RectTransform)card;
        float w = cardRt.rect.width  > 10f ? cardRt.rect.width  : 639f;
        float h = cardRt.rect.height > 10f ? cardRt.rect.height : 102f;

        // An phan thua (khong xoa)
        Tat(card, "Txt_Rewards");
        Tat(card, "Img_Avatar");
        for (int i = 0; i < 5; i++) Tat(card, "Chip_" + i);

        DishData mau = MonMau();

        // Dia mon ben trai
        float o = Mathf.Min(h, 100f);
        var frame = card.Find("Dish_Frame");
        if (frame != null) Dat((RectTransform)frame, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(o, o));
        var dish = card.Find("Img_Dish");
        if (dish != null)
        {
            Dat((RectTransform)dish, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(o * 0.5f, 0f), new Vector2(o - 14f, o - 14f));
            var im = dish.GetComponent<Image>();
            if (im != null)
            {
                im.color = Color.white;                    // truoc de alpha 0.001 => dia vo hinh
                im.preserveAspect = true;
                if (im.sprite == null && mau != null) im.sprite = mau.dishSprite;
            }
        }

        // Ten mon o tren
        float x0 = o + 14f;
        var ten = card.Find("Txt_Name");
        if (ten != null)
        {
            Dat((RectTransform)ten, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0, -6f), new Vector2(w - x0 - 8f, 28f));
            var t = ten.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.enableAutoSizing = false;
                t.fontSize = 23f;
                t.fontStyle = FontStyles.Bold;
                t.color = Brown;
                t.alignment = TextAlignmentOptions.Left;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.overflowMode = TextOverflowModes.Ellipsis;
                if (mau != null) t.text = LocSafe(mau.dishName);
            }
        }

        // Hang chip o duoi
        var row = card.Find("Need_Row");
        if (row == null)
        {
            var go = new GameObject("Need_Row", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Need_Row");
            go.transform.SetParent(card, false);
            row = go.transform;
        }
        Dat((RectTransform)row, Vector2.zero, Vector2.zero, new Vector2(x0, HangChipY), new Vector2(w - x0 - 4f, ChipNguyenLieu.y + 2f));

        // Gia treo dung cu (Rack_Utensils) dang ve DE LEN mep phai the don => chip dong ho bi che
        // chi con chu "s". Dua the don ve SAU gia treo trong thu tu ve (gia nam phia sau bang).
        var rack = Tim(goc, "Rack_Utensils");
        if (rack != null && rack.parent == banner.parent && banner.GetSiblingIndex() < rack.GetSiblingIndex())
            banner.SetSiblingIndex(rack.GetSiblingIndex());

        var hl = row.GetComponent<HorizontalLayoutGroup>();
        if (hl == null) hl = Undo.AddComponent<HorizontalLayoutGroup>(row.gameObject);
        hl.padding = new RectOffset(0, 0, 0, 0);
        hl.spacing = 8f;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = false; hl.childControlHeight = false;
        hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;

        var chips = row.GetComponent<KitchenV3OrderChips>();
        if (chips == null) chips = Undo.AddComponent<KitchenV3OrderChips>(row.gameObject);
        Undo.RecordObject(chips, "Chip size");
        chips.kichThuocChip = ChipNguyenLieu;
        Sprite nen = chips.nenChip;
        Sprite dongHo = chips.iconDongHo;

        int soCan = mau != null && mau.requiredIngredients != null ? mau.requiredIngredients.Count : 3;
        for (int i = 0; i < 4; i++)
        {
            Sprite ic = (mau != null && mau.requiredIngredients != null && i < mau.requiredIngredients.Count && mau.requiredIngredients[i] != null)
                ? mau.requiredIngredients[i].icon : null;
            var c = TaoHoacLayChip(row, "Chip_Need_" + i, ChipNguyenLieu, nen, ic, "0/1", false);
            c.SetActive(i < Mathf.Min(4, soCan));
        }
        TaoHoacLayChip(row, "Chip_Time", ChipGio, nen, dongHo, "2m", true).SetActive(true);

        KhungTenVaHangChip(card);
        return "- The don: dia trai " + o + "px, khung ten bo goc, " + Mathf.Min(4, soCan) + " chip nguyen lieu + chip gio (neo phai).";
    }

    private static GameObject TaoHoacLayChip(Transform cha, string ten, Vector2 size, Sprite nen, Sprite icon, string chu, bool laGio)
    {
        var t = cha.Find(ten);
        GameObject go;
        if (t == null)
        {
            go = new GameObject(ten, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(go, ten);
            go.transform.SetParent(cha, false);
        }
        else go = t.gameObject;

        var rt = (RectTransform)go.transform;
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = Undo.AddComponent<LayoutElement>(go);
        le.preferredWidth = size.x; le.preferredHeight = size.y;
        le.ignoreLayout = laGio;                    // chip gio neo rieng sat mep phai nhu mau
        if (laGio) Dat(rt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, size);
        else rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        if (img == null) img = Undo.AddComponent<Image>(go);
        img.sprite = nen;
        img.type = nen != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = nen != null ? new Color(1f, 0.97f, 0.89f, 1f) : Cream;
        img.raycastTarget = false;

        // Icon
        var icT = go.transform.Find("Img");
        if (icT == null)
        {
            var ig = new GameObject("Img", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(ig, "Img");
            ig.transform.SetParent(go.transform, false);
            icT = ig.transform;
        }
        float cao = size.y - 12f;
        Dat((RectTransform)icT, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(cao, cao));
        var ii = icT.GetComponent<Image>();
        ii.preserveAspect = true; ii.raycastTarget = false;
        if (icon != null) ii.sprite = icon;
        ii.enabled = ii.sprite != null;

        // So "0/1" / "2m"
        var txT = go.transform.Find("Txt");
        TMP_Text tx;
        if (txT == null)
        {
            var tg = new GameObject("Txt", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(tg, "Txt");
            tg.transform.SetParent(go.transform, false);
            tx = tg.AddComponent<TextMeshProUGUI>();
            txT = tg.transform;
        }
        else tx = txT.GetComponent<TMP_Text>();
        KeoGian((RectTransform)txT, new Vector2(size.y + 2f, 0f), new Vector2(-6f, 0f));
        if (tx != null)
        {
            var f = LayFont(); if (f != null) tx.font = f;
            tx.enableAutoSizing = true; tx.fontSizeMin = 12f; tx.fontSizeMax = 20f; tx.fontSize = 20f;
            tx.fontStyle = FontStyles.Bold;
            tx.color = Brown;
            tx.alignment = TextAlignmentOptions.Left;
            tx.textWrappingMode = TextWrappingModes.NoWrap;
            tx.raycastTarget = false;
            if (string.IsNullOrEmpty(tx.text)) tx.text = chu;
        }
        return go;
    }

    private static DishData MonMau()
    {
        // Uu tien mon co 3 nguyen lieu (giong mau Tomato Pasta), khong co thi lay mon dau tien
        DishData dau = null;
        foreach (var guid in AssetDatabase.FindAssets("t:DishData"))
        {
            var d = AssetDatabase.LoadAssetAtPath<DishData>(AssetDatabase.GUIDToAssetPath(guid));
            if (d == null || d.dishSprite == null) continue;
            if (dau == null) dau = d;
            if (d.requiredIngredients != null && d.requiredIngredients.Count == 3) return d;
        }
        return dau;
    }

    private static string LocSafe(string vi)
    {
        try { return Loc.T(vi); } catch { return vi; }
    }

    // ------------------------------------------------------------------------
    //  2. KHAY NGUYEN LIEU / GIA VI
    // ------------------------------------------------------------------------

    private static string CanKhay(Transform goc)
    {
        var tray = Tim(goc, "Tray");
        if (tray == null) return "- Khay: KHONG tim thay Tray (bo qua).";

        Sprite nenNut = null;
        var tab = tray.Find("Tab_Ingredients");
        if (tab != null && tab.GetComponent<Image>() != null) nenNut = tab.GetComponent<Image>().sprite;

        int the = 0, o = 0;
        foreach (var tenLuoi in new[] { "Grid_Ingredients", "Grid_Seasonings" })
        {
            var luoi = TimSau(tray, tenLuoi);
            if (luoi == null) continue;

            Sprite nenBadge = null;
            for (int i = 0; i < luoi.childCount && nenBadge == null; i++)
            {
                var b = luoi.GetChild(i).Find("Qty_Badge");
                var bi = b != null ? b.GetComponent<Image>() : null;
                if (bi != null && bi.sprite != null) nenBadge = bi.sprite;
            }

            for (int i = 0; i < luoi.childCount; i++)
            {
                var c = luoi.GetChild(i);
                if (c.name.StartsWith("Card_")) { CanThe(c, nenBadge); the++; }
                else if (c.name.StartsWith("Slot_Empty_")) { CanOTrong(c); o++; }
                else if (c.name == "Btn_BuySlots") { CanNutMua(c, nenNut); o++; }
            }
        }
        return "- Khay: " + the + " the (so luong vao o nau), " + o + " o trong/nut mua can gon.";
    }

    private static void CanThe(Transform card, Sprite nenBadge)
    {
        var badge = card.Find("Qty_Badge");
        if (badge == null)
        {
            var bg = new GameObject("Qty_Badge", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(bg, "Qty_Badge");
            bg.transform.SetParent(card, false);
            badge = bg.transform;
        }
        Dat((RectTransform)badge, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 8f), new Vector2(48f, 26f));
        badge.SetAsLastSibling();
        var bi = badge.GetComponent<Image>();
        if (bi != null)
        {
            if (bi.sprite == null && nenBadge != null) { bi.sprite = nenBadge; bi.type = Image.Type.Simple; }
            bi.color = BadgeNau;
            bi.raycastTarget = false;
        }

        var qty = badge.Find("Txt_Quantity") ?? card.Find("Txt_Quantity");
        if (qty == null) return;
        if (qty.parent != badge) Undo.SetTransformParent(qty, badge, "So luong vao badge");
        KeoGian((RectTransform)qty, Vector2.zero, Vector2.zero);
        qty.localScale = Vector3.one;
        var t = qty.GetComponent<TMP_Text>();
        if (t != null)
        {
            t.enableAutoSizing = true; t.fontSizeMin = 9f; t.fontSizeMax = 15f; t.fontSize = 15f;
            t.fontStyle = FontStyles.Bold;
            t.color = Cream;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;

            var sel = card.GetComponent<SelectableIngredientCard>();
            if (sel != null)
            {
                var so = new SerializedObject(sel);
                var p = so.FindProperty("txtQuantity");
                if (p != null) { p.objectReferenceValue = t; so.ApplyModifiedProperties(); }
            }
        }
    }

    private static void CanOTrong(Transform cell)
    {
        var tx = cell.Find("Txt");
        if (tx == null) return;
        KeoGian((RectTransform)tx, new Vector2(6f, 6f), new Vector2(-6f, -6f));
        var t = tx.GetComponent<TMP_Text>();
        if (t == null) return;
        t.enableAutoSizing = true; t.fontSizeMin = 10f; t.fontSizeMax = 15f; t.fontSize = 15f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(0.55f, 0.45f, 0.35f);
    }

    private static void CanNutMua(Transform nut, Sprite nen)
    {
        var img = nut.GetComponent<Image>();
        if (img != null && nen != null)
        {
            img.sprite = nen;                       // truoc dung art nut "START COOKING" co mu dau bep -> tran o
            img.type = Image.Type.Sliced;
            img.preserveAspect = false;
            img.color = Color.white;
        }
        var lb = nut.Find("Txt_Label");
        if (lb != null)
        {
            KeoGian((RectTransform)lb, new Vector2(6f, 26f), new Vector2(-6f, -6f));
            var t = lb.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.enableAutoSizing = true; t.fontSizeMin = 10f; t.fontSizeMax = 15f; t.fontSize = 15f;
                t.fontStyle = FontStyles.Bold;
                t.color = Brown;
                t.alignment = TextAlignmentOptions.Center;
            }
        }
        var gold = nut.Find("Img_Gold");
        if (gold != null) Dat((RectTransform)gold, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(18f, 18f));
    }

    // ------------------------------------------------------------------------
    //  3. BANG CHI TIET MON (Recipe_Board/Board_Detail) — hien khi bam 1 mon
    //     Khung giay ~454x519, le trong 34: [< Back] / dia + ten + cap + thuong /
    //     "Ingredients needed" + o nguyen lieu / "Flavor" + 5 thanh / diem du kien o day.
    // ------------------------------------------------------------------------

    private static readonly Color NauChu     = new Color(0.36f, 0.20f, 0.09f);
    private static readonly Color NauNhat    = new Color(0.55f, 0.40f, 0.25f);
    private static readonly Color VangThuong = new Color(0.72f, 0.50f, 0.06f);

    private static string CanSachChiTiet(Transform goc)
    {
        var d = Tim(goc, "Recipe_Board/Board_Detail");
        if (d == null) return "- Sach chi tiet: KHONG tim thay Board_Detail (bo qua).";
        var drt = (RectTransform)d;
        float W = drt.rect.width > 10f ? drt.rect.width : 454f;
        const float L = 24f, P = 12f;              // le the so voi giay / le chu trong the
        float rong = W - L * 2f, trong = rong - P * 2f;
        var tl = new Vector2(0f, 1f);

        // Sprite the bo goc: lay cua chip the don (card_rounded_white) — cung bo voi ca man bep
        Sprite the = null;
        var chips0 = Tim(goc, "Order_Banner/Order_Card/Need_Row");
        var oc = chips0 != null ? chips0.GetComponent<KitchenV3OrderChips>() : null;
        if (oc != null) the = oc.nenChip;
        if (the == null) the = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/KitchenCozyV3/Cut/card_rounded_white.png");

        // 4 the bo goc (nam DUOI chu: dat dau danh sach con)
        TheBoGoc(d, "Card_Score",  the, new Vector2(0f, 0f), new Vector2(L, 12f),  new Vector2(rong, 40f), new Color(1f, 0.93f, 0.84f, 1f));
        TheBoGoc(d, "Card_Flavor", the, tl, new Vector2(L, -278f), new Vector2(rong, 170f), new Color(1f, 0.98f, 0.93f, 1f));
        TheBoGoc(d, "Card_Need",   the, tl, new Vector2(L, -166f), new Vector2(rong, 104f), new Color(1f, 0.98f, 0.93f, 1f));
        TheBoGoc(d, "Card_Info",   the, tl, new Vector2(L, -60f),  new Vector2(rong, 96f),  new Color(1f, 0.96f, 0.86f, 1f));

        var back = d.Find("Btn_OtherDish");
        if (back != null)
        {
            Dat((RectTransform)back, tl, tl, new Vector2(L, -14f), new Vector2(104f, 38f));
            ChuTMP(back.Find("Txt_Label"), 20f, NauChu, TextAlignmentOptions.Center, true);
        }

        // THE 1: dia + ten + cap + thuong
        var img = d.Find("Img_Dish");
        if (img != null)
        {
            Dat((RectTransform)img, tl, tl, new Vector2(L + P, -68f), new Vector2(80f, 80f));
            var im = img.GetComponent<Image>();
            if (im != null) { im.color = Color.white; im.preserveAspect = true; }
        }
        float xTen = L + P + 90f, rongTen = W - L - P - xTen;
        DatNeuCo(Rt(d, "Txt_DishName"), tl, tl, new Vector2(xTen, -68f), new Vector2(rongTen, 30f));
        ChuTMP(d.Find("Txt_DishName"), 24f, NauChu, TextAlignmentOptions.Left, true);
        DatNeuCo(Rt(d, "Txt_DishMeta"), tl, tl, new Vector2(xTen, -99f), new Vector2(rongTen, 22f));
        ChuTMP(d.Find("Txt_DishMeta"), 16f, NauNhat, TextAlignmentOptions.Left, false);
        DatNeuCo(Rt(d, "Txt_Rewards"), tl, tl, new Vector2(xTen, -123f), new Vector2(rongTen, 22f));
        ChuTMP(d.Find("Txt_Rewards"), 15f, VangThuong, TextAlignmentOptions.Left, true);

        // THE 2: nguyen lieu can
        DatNeuCo(Rt(d, "Txt_NeedTitle"), tl, tl, new Vector2(L + P, -172f), new Vector2(trong, 22f));
        ChuTMP(d.Find("Txt_NeedTitle"), 17f, NauChu, TextAlignmentOptions.Left, true);
        var chips = d.Find("Need_Chips");
        if (chips != null)
        {
            Dat((RectTransform)chips, tl, tl, new Vector2(L + P, -196f), new Vector2(trong, 68f));
            var gl = chips.GetComponent<GridLayoutGroup>();
            if (gl == null) gl = Undo.AddComponent<GridLayoutGroup>(chips.gameObject);
            if (gl != null)
            {
                gl.cellSize = new Vector2(66f, 68f);
                gl.spacing = new Vector2(8f, 8f);
                gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gl.constraintCount = Mathf.Max(1, Mathf.FloorToInt((trong + 8f) / 74f));
                gl.childAlignment = TextAnchor.UpperLeft;
                gl.padding = new RectOffset(0, 0, 0, 0);
            }
        }

        // THE 3: huong vi
        DatNeuCo(Rt(d, "Txt_TasteTitle"), tl, tl, new Vector2(L + P, -284f), new Vector2(trong, 22f));
        ChuTMP(d.Find("Txt_TasteTitle"), 17f, NauChu, TextAlignmentOptions.Left, true);
        var giua = new Vector2(0f, 0.5f);
        for (int i = 0; i < 5; i++)
        {
            var r = d.Find("Flavor_Row_" + i);
            if (r == null) continue;
            Dat((RectTransform)r, tl, tl, new Vector2(L + P, -310f - i * 27f), new Vector2(trong, 24f));
            var dot = r.Find("Dot");
            if (dot != null) Dat((RectTransform)dot, giua, new Vector2(0.5f, 0.5f), new Vector2(7f, 0f), new Vector2(12f, 12f));
            var lb = r.Find("Label");
            if (lb != null) { Dat((RectTransform)lb, giua, giua, new Vector2(20f, 0f), new Vector2(88f, 24f)); ChuTMP(lb, 16f, NauChu, TextAlignmentOptions.Left, false); }
            var tr = r.Find("Track");
            if (tr != null) Dat((RectTransform)tr, giua, giua, new Vector2(112f, 0f), new Vector2(trong - 112f - 56f, 14f));
            var va = r.Find("Value");
            if (va != null) { Dat((RectTransform)va, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(50f, 24f)); ChuTMP(va, 16f, NauChu, TextAlignmentOptions.Right, true); }
        }

        // THE 4: diem du kien
        var bl = new Vector2(0f, 0f);
        DatNeuCo(Rt(d, "Txt_Projection"), bl, bl, new Vector2(L + P, 16f), new Vector2(trong, 32f));
        ChuTMP(d.Find("Txt_Projection"), 17f, new Color(0.75f, 0.25f, 0.15f), TextAlignmentOptions.Center, true);

        return "- Sach chi tiet: 4 the bo goc (mon / nguyen lieu / huong vi / diem), chu can trong the.";
    }

    /// <summary>Tao/lay the bo goc ten 'ten' trong 'cha', dat o vi tri cho, dua xuong duoi cung (ve truoc chu).</summary>
    private static void TheBoGoc(Transform cha, string ten, Sprite sp, Vector2 neo, Vector2 pos, Vector2 size, Color mau)
    {
        var t = cha.Find(ten);
        if (t == null)
        {
            var go = new GameObject(ten, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, ten);
            go.transform.SetParent(cha, false);
            t = go.transform;
        }
        Dat((RectTransform)t, neo, neo, pos, size);
        var im = t.GetComponent<Image>();
        im.sprite = sp; im.type = sp != null ? Image.Type.Sliced : Image.Type.Simple;
        im.color = mau; im.raycastTarget = false;
        t.SetAsFirstSibling();
    }

    private static RectTransform Rt(Transform cha, string ten)
    {
        var t = cha.Find(ten);
        return t != null ? (RectTransform)t : null;
    }

    private static void DatNeuCo(RectTransform rt, Vector2 neo, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        if (rt != null) Dat(rt, neo, pivot, pos, size);
    }

    private static void ChuTMP(Transform t, float co, Color mau, TextAlignmentOptions canh, bool dam)
    {
        if (t == null) return;
        var x = t.GetComponent<TMP_Text>();
        if (x == null) return;
        x.enableAutoSizing = true; x.fontSizeMax = co; x.fontSizeMin = Mathf.Max(9f, co * 0.7f); x.fontSize = co;
        x.color = mau;
        x.alignment = canh;
        x.textWrappingMode = TextWrappingModes.NoWrap;
        x.overflowMode = TextOverflowModes.Ellipsis;
        x.fontStyle = dam ? FontStyles.Bold : FontStyles.Normal;
    }

    // ------------------------------------------------------------------------
    //  4. ICON MON TRONG SACH: moi anh mon co le trong khac nhau => cung scale 3.2 thi
    //     anh "to" de len dong tren/duoi. Dua ve scale 1 + hop co dinh + giu ti le.
    //     (Tu chay 1 lan; sau do Sep chinh tay tung Row_<mon>/Img_Icon thi Play giu nguyen.)
    // ------------------------------------------------------------------------

    // Thong so dong mon (don vi canvas 1600x900):
    //   DONG_CAO    = chieu cao moi dong (truoc 64)  -> du cho icon to ma KHONG tran ra ngoai dong
    //   ICON_THAY   = kich thuoc PHAN HINH THAT (bo vien trong suot) theo chieu dai nhat
    //                 (truoc ~47 nhin thay; Sep yeu cau +30 -> 78)
    //   DONG_CACH   = khoang trong giua 2 dong -> icon/nen 2 dong khong bao gio cham nhau
    //   ICON_LE     = cach mep trai dong
    private const float DONG_CAO  = 98f;    // 2026-09-23: +14 theo icon to them 1 nac
    private const float ICON_THAY = 92f;    // 78 -> 92
    private const float DONG_CACH = 10f;
    private const float ICON_LE   = 12f;

    private static string CanIconDongMon(Transform goc)
    {
        var content = Tim(goc, "Recipe_Board/Board_List/Dish_Scroll/Content");
        if (content == null) return "- Icon mon: KHONG tim thay danh sach mon (bo qua).";

        var vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            Undo.RecordObject(vlg, "Gian dong mon");
            vlg.spacing = DONG_CACH;
            vlg.padding = new RectOffset(vlg.padding.left, vlg.padding.right, 6, 6);
        }

        int n = 0, doDuoc = 0;
        for (int i = 0; i < content.childCount; i++)
        {
            var row = content.GetChild(i);
            if (!row.name.StartsWith("Row_")) continue;
            var rrt = (RectTransform)row;
            Undo.RecordObject(rrt, "Gian dong mon");
            float caoCu = rrt.rect.height > 10f ? rrt.rect.height : 64f;

            var le = row.GetComponent<LayoutElement>();
            if (le == null) le = Undo.AddComponent<LayoutElement>(row.gameObject);
            Undo.RecordObject(le, "Gian dong mon");
            if (le.preferredHeight > 0f) caoCu = le.preferredHeight;
            le.minHeight = DONG_CAO; le.preferredHeight = DONG_CAO;
            rrt.sizeDelta = new Vector2(rrt.sizeDelta.x, DONG_CAO);

            // Dong cao hon -> day ten + dong phu vao giua 1 khoang bang nhau (giu nguyen khoang cach giua 2 chu)
            float lech = (DONG_CAO - caoCu) * 0.5f;
            if (Mathf.Abs(lech) > 0.01f)
            {
                var tn = row.Find("Txt_Name") as RectTransform;
                if (tn != null && Mathf.Approximately(tn.anchorMin.y, 1f)) { Undo.RecordObject(tn, "Gian dong mon"); tn.anchoredPosition += new Vector2(0f, -lech); }
                var tm = row.Find("Txt_Meta") as RectTransform;
                if (tm != null && Mathf.Approximately(tm.anchorMin.y, 0f)) { Undo.RecordObject(tm, "Gian dong mon"); tm.anchoredPosition += new Vector2(0f, lech); }
            }

            var ic = row.Find("Img_Icon");
            if (ic == null) continue;
            var irt = (RectTransform)ic;
            var im = ic.GetComponent<Image>();
            Undo.RecordObject(irt, "Icon mon");
            irt.localScale = Vector3.one;
            irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);

            // Tam PHAN HINH THAT nam o (ICON_LE + ICON_THAY/2, 0) cua dong
            Vector2 tam = new Vector2(ICON_LE + ICON_THAY * 0.5f, 0f);
            Vector2 hop, lechTam;
            if (im != null && im.sprite != null && DoPhanHinhThat(im.sprite, ICON_THAY, out hop, out lechTam))
            {
                irt.sizeDelta = hop;
                irt.anchoredPosition = tam - lechTam;
                doDuoc++;
            }
            else
            {
                irt.sizeDelta = new Vector2(ICON_THAY, ICON_THAY);
                irt.anchoredPosition = tam;
            }
            if (im != null) { Undo.RecordObject(im, "Icon mon"); im.preserveAspect = true; }
            n++;
        }
        return "- Icon mon: " + n + " dong cao " + DONG_CAO + ", cach nhau " + DONG_CACH + ", hinh that " + ICON_THAY +
               "px (do vien trong suot chinh xac " + doDuoc + "/" + n + " anh) - khong dong nao cham nhau.";
    }

    /// <summary>
    /// Do vung CO HINH (alpha > 0.08) cua sprite, tinh kich thuoc hop Image sao cho phan hinh that
    /// dai nhat dung 'mucTieu' px, va do lech tam giua hop va phan hinh that (de can giua dung hinh).
    /// Doc thang file PNG goc (khong can bat Read/Write tren texture).
    /// </summary>
    private static bool DoPhanHinhThat(Sprite sp, float mucTieu, out Vector2 hop, out Vector2 lechTam)
    {
        hop = new Vector2(mucTieu, mucTieu); lechTam = Vector2.zero;
        try
        {
            string duong = AssetDatabase.GetAssetPath(sp.texture);
            if (string.IsNullOrEmpty(duong) || !File.Exists(duong)) return false;
            var tam = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tam.LoadImage(File.ReadAllBytes(duong))) { Object.DestroyImmediate(tam); return false; }

            float f = sp.texture.width > 0 ? tam.width / (float)sp.texture.width : 1f;   // anh bi giam size khi import
            Rect r = sp.rect;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(r.x * f), 0, tam.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(r.y * f), 0, tam.height - 1);
            int w  = Mathf.Clamp(Mathf.CeilToInt(r.width * f), 1, tam.width - x0);
            int h  = Mathf.Clamp(Mathf.CeilToInt(r.height * f), 1, tam.height - y0);
            Color32[] px = tam.GetPixels32();
            int tw = tam.width;
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                int dong = (y0 + y) * tw + x0;
                for (int x = 0; x < w; x++)
                {
                    if (px[dong + x].a <= 20) continue;
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                }
            }
            Object.DestroyImmediate(tam);
            if (maxX < 0) return false;

            float vw = maxX - minX + 1, vh = maxY - minY + 1;
            float k = mucTieu / Mathf.Max(vw, vh);
            hop = new Vector2(w * k, h * k);
            lechTam = new Vector2(((minX + maxX + 1) * 0.5f - w * 0.5f) * k, ((minY + maxY + 1) * 0.5f - h * 0.5f) * k);
            return true;
        }
        catch { return false; }
    }

    // ------------------------------------------------------------------------
    //  5. BANG DEN: chu mon hom nay to hon, nam gon giua bang (duoi tieu de)
    // ------------------------------------------------------------------------

    private static string CanBangDen(Transform goc)
    {
        var bang = Tim(goc, "Chalkboard");
        var chu = bang != null ? bang.Find("Txt_Chalk") : null;
        if (chu == null) return "- Bang den: KHONG tim thay Txt_Chalk (bo qua).";
        var rt = (RectTransform)chu;
        rt.anchorMin = new Vector2(0.13f, 0.17f);
        rt.anchorMax = new Vector2(0.87f, 0.58f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var t = chu.GetComponent<TMP_Text>();
        if (t != null)
        {
            t.enableAutoSizing = true; t.fontSizeMin = 14f; t.fontSizeMax = 26f; t.fontSize = 26f;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.fontStyle = FontStyles.Bold;
            t.lineSpacing = 8f;
            t.color = new Color(0.97f, 0.95f, 0.88f);
            if (string.IsNullOrEmpty(t.text)) t.text = "• Cabbage Fried Rice";
        }
        return "- Bang den: chu mon 14-26pt, can giua trong bang.";
    }
}
