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

    static KitchenV3LayoutFixTool()
    {
        EditorApplication.delayCall += ThuTuChay;
        EditorSceneManager.sceneOpened += (s, m) => EditorApplication.delayCall += ThuTuChay;
    }

    private static string KhoaDaChay => "KitchenV3LayoutFix_v1|" + Application.dataPath;

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

        EditorSceneManager.MarkSceneDirty(scene);
        bool daLuu = false;
        if (!daBanTruoc) daLuu = EditorSceneManager.SaveScene(scene);

        string tomTat = "[Kitchen V3] Can bo cuc xong tren '" + goc.name + "'.\n" + kqDon + "\n" + kqKhay +
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

    private static TMP_FontAsset Font()
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
            Dat((RectTransform)ten, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0, -4f), new Vector2(w - x0 - 8f, 34f));
            var t = ten.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.enableAutoSizing = false;
                t.fontSize = 24f;
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
        Dat((RectTransform)row, Vector2.zero, Vector2.zero, new Vector2(x0, 6f), new Vector2(w - x0 - 4f, ChipNguyenLieu.y + 2f));

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

        return "- The don: dia trai " + o + "px, ten 24pt, " + Mathf.Min(4, soCan) + " chip nguyen lieu + chip gio (neo phai).";
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
        img.color = nen != null ? Color.white : Cream;
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
            var f = Font(); if (f != null) tx.font = f;
            tx.enableAutoSizing = true; tx.fontSizeMin = 12f; tx.fontSizeMax = 19f; tx.fontSize = 19f;
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
}
