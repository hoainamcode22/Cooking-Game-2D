using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════════════════
///  TOOL — THỨC ĂN CAO CẤP (TÚI CÁM TỪ MÁY XAY) CHO 4 CHUỒNG GIA SÚC
/// ═══════════════════════════════════════════════════════════════════════════════════
///
///  Ý TƯỞNG VÒNG LẶP: ruộng → máy xay thức ăn → túi cám vào kho → cho gia súc ăn.
///  Chuồng VẪN ăn được nông sản thô như cũ (không phá save, không chặn người chơi mới);
///  túi cám là đường CAO CẤP: nuôi nhanh hơn + ra nhiều sản phẩm hơn + nhiều EXP hơn.
///
///  ═══ VÌ SAO CHỈ SỬA MỘT PREFAB ═══
///  Pen_01..Pen_04 KHÔNG chứa bản sao riêng của panel — cả 4 đều là NESTED INSTANCE của
///  Assets/_Game/Farm/Prefabs/PF_PenMiniPanel.prefab (guid 9bba0636a85f99c449b552a70632b5d8),
///  chỉ override đúng một field `config`. Nên thêm ô thức ăn thứ 3 vào prefab GỐC là cả 4
///  chuồng có ngay, không cần sửa 4 lần và không có nguy cơ 4 bản lệch nhau.
///
///  ═══ BẪY ĐÃ TRÁNH (đều là lỗi thật đã gặp trong dự án này) ═══
///  1. `Transform.SetParent()` vào transform nằm trong PREFAB ASSET bị Unity CẤM ⇒ object
///     mới rơi ra gốc scene và tool vẫn báo "thành công". Bắt buộc dùng
///     LoadPrefabContents → sửa → SaveAsPrefabAsset → UnloadPrefabContents (finally).
///  2. `onClick.AddListener()` trong Editor tạo listener KHÔNG PERSISTENT ⇒ không được ghi
///     vào prefab, chạy game là mất. Bằng chứng: m_Calls trong PF_PenMiniPanel.prefab đang
///     `[]` dù BuildingProcessUIBuilderTool đã "gắn" cho Slot_Food1/2 — tức từ trước tới nay
///     BẤM vào ô thức ăn không có tác dụng, chỉ KÉO-THẢ mới cho ăn được.
///     Tool này dùng UnityEventTools.AddPersistentListener và gắn LẠI cho cả 3 ô.
///  3. `GetComponent&lt;T&gt;() ?? AddComponent&lt;T&gt;()` KHÔNG chạy đúng vì Unity trả
///     "fake null". Phải so `== null` tường minh (xem LayHoacThem).
///  4. Field của PenMiniPanelUI là `[SerializeField] private` ⇒ chỉ gán được qua
///     SerializedObject.FindProperty. Sai tên field là tool báo lỗi ngay, không âm thầm null.
/// </summary>
public static class PenPremiumFeedSetupTool
{
    private const string PrefabPanel = "Assets/_Game/Farm/Prefabs/PF_PenMiniPanel.prefab";
    private const string LOG         = "[CamGiaSuc] ";

    // penId → recipeId của công thức trong MillConfig (khớp spec của Edric:
    // Bò lúa · Gà lúa+bắp · Heo cà rốt+bắp cải · Bò sữa lúa).
    private static readonly Dictionary<string, string> MapCam = new Dictionary<string, string>
    {
        { "pen_01", "co_tron_bo"  },   // Chuồng bò thịt  ← Cỏ trộn cho bò
        { "pen_02", "cam_heo"     },   // Chuồng heo      ← Cám cho heo
        { "pen_03", "cam_ga"      },   // Chuồng gà       ← Cám cho gà
        { "pen_04", "cam_bo_sua"  },   // Chuồng bò sữa   ← Cám cho bò sữa
    };

    // Bố cục PanelContent: trước 270×136 với 2 ô ở x = ±60. Ba ô thì rộng 390, x = -110/0/110.
    private const float PanelW  = 390f;
    private const float PanelH  = 136f;
    private const float SlotW   = 96f;
    private const float SlotH   = 110f;
    private const float SlotX   = 110f;

    // ═════════════════════════════════════════════════════════════════════════════════
    //  LỆNH 1 — THÊM Ô THỨC ĂN THỨ 3 VÀO PANEL CHUỒNG
    // ═════════════════════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Farm/Thuc An Cao Cap/1. Them O Cam Vao Panel Chuong", false, 1)]
    public static void ThemOCam()
    {
        var bc = new StringBuilder();
        bc.AppendLine("═══ LỆNH 1 — THÊM Ô THỨC ĂN CÁM VÀO PANEL CHUỒNG ═══");

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPanel) == null)
        {
            Debug.LogError(LOG + "KHÔNG thấy prefab " + PrefabPanel + " — dừng, không sửa gì.");
            return;
        }

        GameObject goc = null;
        bool luu = false;

        try
        {
            goc = PrefabUtility.LoadPrefabContents(PrefabPanel);
            if (goc == null)
            {
                Debug.LogError(LOG + "Không mở được nội dung prefab.");
                return;
            }

            PenMiniPanelUI penUI = goc.GetComponentInChildren<PenMiniPanelUI>(true);
            if (penUI == null)
            {
                Debug.LogError(LOG + "Prefab không có PenMiniPanelUI.");
                return;
            }

            Transform root = penUI.transform;
            Transform panelContent = root.Find("PanelContent") ?? root.Find("panelContent");
            if (panelContent == null)
            {
                Debug.LogError(LOG + "Không thấy node 'PanelContent' trong prefab — dừng.");
                return;
            }

            // ── 1. Nới rộng khay để chứa 3 ô ────────────────────────────────────────
            RectTransform pcRect = panelContent.GetComponent<RectTransform>();
            if (pcRect != null)
            {
                pcRect.sizeDelta = new Vector2(PanelW, PanelH);
                bc.AppendLine("  ✔ PanelContent → " + PanelW + "×" + PanelH + " (trước 270×136)");
            }

            Transform slot1 = panelContent.Find("Slot_Food1");
            Transform slot2 = panelContent.Find("Slot_Food2");

            if (slot1 == null || slot2 == null)
            {
                Debug.LogError(LOG + "Không thấy Slot_Food1 / Slot_Food2 — prefab khác cấu trúc mong đợi, dừng.");
                return;
            }

            // ── 2. Ô thứ 3: NHÂN BẢN Slot_Food2 để thừa hưởng y nguyên đĩa lót, font,
            //      cỡ chữ, thứ tự con… Dựng tay là chắc chắn lệch một chi tiết nào đó.
            Transform slot3 = panelContent.Find("Slot_Food3");
            if (slot3 == null)
            {
                GameObject clone = Object.Instantiate(slot2.gameObject, panelContent);
                clone.name = "Slot_Food3";
                slot3 = clone.transform;
                bc.AppendLine("  ✔ Tạo Slot_Food3 (nhân bản từ Slot_Food2)");
            }
            else
            {
                bc.AppendLine("  • Slot_Food3 đã có — chỉ cập nhật lại, KHÔNG tạo trùng");
            }

            // ── 3. Xếp lại 3 ô cho cân ──────────────────────────────────────────────
            DatO(slot1, -SlotX);
            DatO(slot2, 0f);
            DatO(slot3, SlotX);
            bc.AppendLine("  ✔ Xếp 3 ô tại x = -" + SlotX + " / 0 / +" + SlotX);

            // ── 4. Button PERSISTENT cho cả 3 ô ─────────────────────────────────────
            //  ⚠ Đây là chỗ sửa một lỗi CŨ: m_Calls trong prefab đang rỗng nên BẤM vào ô
            //     thức ăn từ trước tới nay không có tác dụng (chỉ kéo-thả mới cho ăn được).
            GanClick(slot1, penUI, 1, bc);
            GanClick(slot2, penUI, 2, bc);
            GanClick(slot3, penUI, 3, bc);

            // ── 5. Wire 3 field slot3* vào PenMiniPanelUI ───────────────────────────
            Image  ic  = TimCon<Image>(slot3, "Icon");
            TMP_Text tx = TimCon<TMP_Text>(slot3, "AmountText");

            var so = new SerializedObject(penUI);
            bool ok = true;
            ok &= Wire(so, "slot3Root",   slot3.gameObject, bc);
            ok &= Wire(so, "slot3Icon",   ic,               bc);
            ok &= Wire(so, "slot3Amount", tx,               bc);
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!ok)
                bc.AppendLine("  ⚠ Có field wire KHÔNG thành công — xem dòng LỖI phía trên.");

            PrefabUtility.SaveAsPrefabAsset(goc, PrefabPanel);
            luu = true;
        }
        finally
        {
            if (goc != null) PrefabUtility.UnloadPrefabContents(goc);
        }

        if (luu)
        {
            AssetDatabase.SaveAssets();
            bc.AppendLine("  ✔ ĐÃ LƯU " + PrefabPanel);
            bc.AppendLine("  → Pen_01..Pen_04 là nested instance của prefab này nên có ngay ô mới.");
            bc.AppendLine();
            bc.AppendLine("BƯỚC TIẾP: chạy lệnh 2 để điền data cám vào 4 PenConfig.");
        }

        Debug.Log(LOG + bc.ToString());
    }

    // ═════════════════════════════════════════════════════════════════════════════════
    //  LỆNH 2 — ĐIỀN DATA CÁM VÀO 4 PENCONFIG
    // ═════════════════════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Farm/Thuc An Cao Cap/2. Dien Data Cam + Dang Ky Vao Kho", false, 2)]
    public static void DienDataCam()
    {
        var bc = new StringBuilder();
        bc.AppendLine("═══ LỆNH 2 — ĐIỀN DATA CÁM VÀO PENCONFIG ═══");

        // Gom công thức máy xay theo recipeId
        var congThuc = new Dictionary<string, MillRecipeData>();
        foreach (string guid in AssetDatabase.FindAssets("t:MillRecipeData"))
        {
            var r = AssetDatabase.LoadAssetAtPath<MillRecipeData>(AssetDatabase.GUIDToAssetPath(guid));
            if (r != null && !string.IsNullOrEmpty(r.recipeId)) congThuc[r.recipeId] = r;
        }
        bc.AppendLine("  Tìm thấy " + congThuc.Count + " công thức máy xay.");

        // Icon dự phòng: bao thóc có sẵn trong project — dùng tạm cho tới khi Edric vẽ
        // xong art bao cám riêng cho từng loại.
        Sprite baoThoc = TimSprite("Assets/Assetsgame/img_BaoThoc.png");

        int soXong = 0, soThieu = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:PenMiniPanelConfig"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg = AssetDatabase.LoadAssetAtPath<PenMiniPanelConfig>(path);
            if (cfg == null) continue;

            if (!MapCam.TryGetValue(cfg.penId, out string recipeId))
            {
                bc.AppendLine("  • Bỏ qua " + cfg.name + " (penId '" + cfg.penId + "' không có trong bảng cám)");
                continue;
            }

            if (!congThuc.TryGetValue(recipeId, out MillRecipeData r))
            {
                bc.AppendLine("  ✘ " + cfg.name + ": KHÔNG thấy công thức '" + recipeId +
                              "'. Chạy 'Tools/Farm/Popup May Xay/2. Tao Data Mau' trước.");
                soThieu++;
                continue;
            }

            Undo.RecordObject(cfg, "Dien data cam");

            cfg.premiumFoodItemId = string.IsNullOrEmpty(r.outputItemId) ? recipeId : r.outputItemId;

            // Icon: ưu tiên icon của chính công thức; chưa có thì tạm bao thóc; giữ nguyên
            // nếu đã có người gán tay (KHÔNG ghi đè công sức của designer).
            if (cfg.premiumFoodIcon == null)
                cfg.premiumFoodIcon = (r.icon != null) ? r.icon : baoThoc;

            cfg.premiumFoodAmountPerFeed = 1;      // cám đã cô đặc nhiều nông sản
            cfg.premiumSpeedMultiplier   = 2f;     // nuôi nhanh gấp đôi
            cfg.premiumProductBonus      = 1;      // +1 sản phẩm
            cfg.premiumExpBonus          = Mathf.Max(5, cfg.expReward / 2);

            EditorUtility.SetDirty(cfg);
            soXong++;

            bc.AppendLine("  ✔ " + cfg.name + " (" + cfg.penId + ") ← " + cfg.premiumFoodItemId +
                          "  ×" + cfg.premiumFoodAmountPerFeed +
                          " · nhanh ×" + cfg.premiumSpeedMultiplier +
                          " · +" + cfg.premiumProductBonus + " sp" +
                          " · +" + cfg.premiumExpBonus + " exp" +
                          (cfg.premiumFoodIcon == null ? "   ⚠ CHƯA CÓ ICON" : ""));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── Đăng ký túi cám vào popup KHO ────────────────────────────────────────
        // Không có bước này thì kho vẫn CỘNG đúng số lượng, nhưng ô hiện ra TRẮNG TÊN,
        // TRẮNG ICON: WarehousePopupUI phân giải tên/icon qua StallItemCatalog → CropData
        // → extraItemDatabase → OrderBoardIconResolver, và `cam_ga` không nằm trong cái nào.
        DangKyCamVaoKho(congThuc, baoThoc, bc);

        bc.AppendLine();
        bc.AppendLine("  Xong " + soXong + " chuồng" + (soThieu > 0 ? ", THIẾU công thức: " + soThieu : "") + ".");
        if (baoThoc == null)
            bc.AppendLine("  ⚠ Không thấy img_BaoThoc.png để làm icon tạm — ô cám sẽ trống icon.");
        else
            bc.AppendLine("  Icon đang dùng TẠM là img_BaoThoc. Vẽ xong art bao cám thì gán lại " +
                          "vào field 'Premium Food Icon' của từng PenConfig (hoặc vào 'icon' của công thức).");

        Debug.Log(LOG + bc.ToString());
    }

    // ═════════════════════════════════════════════════════════════════════════════════
    //  LỆNH 3 — KIỂM TRA
    // ═════════════════════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Farm/Thuc An Cao Cap/3. Kiem Tra (bao cao)", false, 3)]
    public static void KiemTra()
    {
        var bc = new StringBuilder();
        bc.AppendLine("═══ LỆNH 3 — KIỂM TRA THỨC ĂN CAO CẤP ═══");
        int loi = 0;

        // ── A. Prefab panel ─────────────────────────────────────────────────────────
        GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPanel);
        if (pf == null) { bc.AppendLine("✘ Không thấy " + PrefabPanel); loi++; }
        else
        {
            PenMiniPanelUI ui = pf.GetComponentInChildren<PenMiniPanelUI>(true);
            if (ui == null) { bc.AppendLine("✘ Prefab không có PenMiniPanelUI"); loi++; }
            else
            {
                var so = new SerializedObject(ui);
                foreach (string ten in new[] { "slot3Root", "slot3Icon", "slot3Amount" })
                {
                    SerializedProperty pr = so.FindProperty(ten);
                    if (pr == null) { bc.AppendLine("✘ Không có field '" + ten + "' (script chưa cập nhật?)"); loi++; }
                    else if (pr.objectReferenceValue == null) { bc.AppendLine("✘ " + ten + " = NULL — chạy lệnh 1"); loi++; }
                    else bc.AppendLine("✔ " + ten + " → " + pr.objectReferenceValue.name);
                }

                Transform pc = ui.transform.Find("PanelContent") ?? ui.transform.Find("panelContent");
                if (pc != null)
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        Transform s = pc.Find("Slot_Food" + i);
                        if (s == null) { bc.AppendLine("✘ Thiếu Slot_Food" + i); loi++; continue; }

                        Button b = s.GetComponent<Button>();
                        if (b == null) { bc.AppendLine("✘ Slot_Food" + i + " không có Button"); loi++; continue; }

                        int n = b.onClick.GetPersistentEventCount();
                        if (n == 0)
                        {
                            bc.AppendLine("✘ Slot_Food" + i + ": onClick RỖNG (m_Calls: []) ⇒ bấm vào không có tác dụng — chạy lệnh 1");
                            loi++;
                        }
                        else
                        {
                            bc.AppendLine("✔ Slot_Food" + i + ": onClick → " + b.onClick.GetPersistentMethodName(0));
                        }
                    }
                }
            }
        }

        // ── B. Data ─────────────────────────────────────────────────────────────────
        bc.AppendLine();
        foreach (string guid in AssetDatabase.FindAssets("t:PenMiniPanelConfig"))
        {
            var cfg = AssetDatabase.LoadAssetAtPath<PenMiniPanelConfig>(AssetDatabase.GUIDToAssetPath(guid));
            if (cfg == null) continue;

            if (string.IsNullOrEmpty(cfg.premiumFoodItemId))
            {
                bc.AppendLine("✘ " + cfg.name + ": premiumFoodItemId TRỐNG ⇒ ô cám sẽ tự ẩn — chạy lệnh 2");
                loi++;
                continue;
            }

            // Có công thức nào của máy xay thực sự SẢN XUẤT ra id này không?
            bool coNguon = false;
            foreach (string g2 in AssetDatabase.FindAssets("t:MillRecipeData"))
            {
                var r = AssetDatabase.LoadAssetAtPath<MillRecipeData>(AssetDatabase.GUIDToAssetPath(g2));
                if (r != null && r.outputItemId == cfg.premiumFoodItemId) { coNguon = true; break; }
            }

            if (!coNguon)
            {
                bc.AppendLine("✘ " + cfg.name + ": '" + cfg.premiumFoodItemId +
                              "' KHÔNG có công thức nào xay ra ⇒ người chơi không bao giờ có món này");
                loi++;
            }
            else
            {
                bc.AppendLine("✔ " + cfg.name + ": " + cfg.premiumFoodItemId +
                              " ×" + cfg.premiumFoodAmountPerFeed +
                              " · nhanh ×" + cfg.premiumSpeedMultiplier +
                              " · +" + cfg.premiumProductBonus + " sp" +
                              (cfg.premiumFoodIcon == null ? "   ⚠ chưa có icon" : ""));
            }
        }

        // ── C. Túi cám có hiện được TÊN + ICON trong popup Kho chưa ─────────────────
        bc.AppendLine();
        var kho = Object.FindFirstObjectByType<WarehousePopupUI>(FindObjectsInactive.Include);
        if (kho == null)
        {
            bc.AppendLine("• Không thấy WarehousePopupUI trong scene đang mở — bỏ qua mục kiểm tra Kho.");
        }
        else
        {
            var soKho = new SerializedObject(kho);
            SerializedProperty arr = soKho.FindProperty("extraItemDatabase");

            foreach (string guid in AssetDatabase.FindAssets("t:PenMiniPanelConfig"))
            {
                var cfg = AssetDatabase.LoadAssetAtPath<PenMiniPanelConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (cfg == null || string.IsNullOrEmpty(cfg.premiumFoodItemId)) continue;

                bool coTrongKho = false;

                if (arr != null && arr.isArray)
                {
                    for (int i = 0; i < arr.arraySize; i++)
                    {
                        var d = arr.GetArrayElementAtIndex(i).objectReferenceValue as InventoryItemData;
                        if (d != null && d.itemId == cfg.premiumFoodItemId) { coTrongKho = true; break; }
                    }
                }

                if (coTrongKho)
                    bc.AppendLine("✔ Kho nhận diện được '" + cfg.premiumFoodItemId + "' (có tên + icon)");
                else
                {
                    bc.AppendLine("✘ '" + cfg.premiumFoodItemId + "' KHÔNG có trong extraItemDatabase ⇒ " +
                                  "ô trong popup Kho sẽ TRẮNG TÊN, TRẮNG ICON — chạy lệnh 2 rồi Ctrl+S");
                    loi++;
                }
            }
        }

        bc.AppendLine();
        bc.AppendLine(loi == 0 ? "══ KHÔNG CÓ LỖI ══" : "══ CÒN " + loi + " LỖI ══");
        if (loi == 0) Debug.Log(LOG + bc.ToString());
        else          Debug.LogWarning(LOG + bc.ToString());
    }

    // ═════════════════════════════════════════════════════════════════════════════════
    //  ĐĂNG KÝ TÚI CÁM VÀO POPUP KHO
    //
    //  WarehousePopupUI.GetItemDisplayName / GetItemIcon phân giải theo 4 nguồn:
    //      1. StallItemCatalog        2. CropData lookup
    //      3. extraItemDatabase       4. OrderBoardIconResolver
    //  `cam_ga` / `cam_heo` / `co_tron_bo` / `cam_bo_sua` không có ở nguồn nào ⇒ ô kho hiện
    //  ra không tên không icon. Tạo InventoryItemData rồi nhét vào extraItemDatabase là
    //  đường ít xâm lấn nhất: không sửa code kho, không đụng CropData.
    // ═════════════════════════════════════════════════════════════════════════════════
    private const string ThuMucItem = "Assets/_Game/Farm/data/Item_Kho_Cook";

    private static void DangKyCamVaoKho(Dictionary<string, MillRecipeData> congThuc,
                                        Sprite iconDuPhong, StringBuilder bc)
    {
        bc.AppendLine();
        bc.AppendLine("  ── Đăng ký túi cám vào popup KHO ──");

        // 1. Tạo / cập nhật InventoryItemData cho từng công thức trong bảng cám
        var canDangKy = new List<InventoryItemData>();

        foreach (var kv in MapCam)
        {
            if (!congThuc.TryGetValue(kv.Value, out MillRecipeData r) || r == null) continue;

            string itemId = string.IsNullOrEmpty(r.outputItemId) ? r.recipeId : r.outputItemId;
            if (string.IsNullOrEmpty(itemId)) continue;

            string path = ThuMucItem + "/Item_" + TenFile(itemId) + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<InventoryItemData>(path);

            bool moi = (data == null);
            if (moi)
            {
                data = ScriptableObject.CreateInstance<InventoryItemData>();
                AssetDatabase.CreateAsset(data, path);
            }

            Undo.RecordObject(data, "Dang ky cam vao kho");

            data.itemId = itemId;
            if (string.IsNullOrEmpty(data.displayName))
                data.displayName = string.IsNullOrEmpty(r.displayName) ? itemId : r.displayName;
            if (data.icon == null)
                data.icon = (r.icon != null) ? r.icon : iconDuPhong;

            EditorUtility.SetDirty(data);
            canDangKy.Add(data);

            bc.AppendLine("    " + (moi ? "✔ TẠO  " : "• có sẵn ") + path +
                          "  (" + itemId + " · \"" + data.displayName + "\"" +
                          (data.icon == null ? " · ⚠ chưa icon" : "") + ")");
        }

        AssetDatabase.SaveAssets();

        // 2. Nhét vào extraItemDatabase của WarehousePopupUI trong scene đang mở
        var khoList = Object.FindObjectsByType<WarehousePopupUI>(FindObjectsInactive.Include,
                                                                FindObjectsSortMode.None);

        if (khoList == null || khoList.Length == 0)
        {
            bc.AppendLine("    ⚠ Không thấy WarehousePopupUI trong scene ĐANG MỞ.");
            bc.AppendLine("      → Mở SCN_Farm rồi chạy lại lệnh 2, nếu không kho sẽ hiện ô trắng tên.");
            return;
        }

        foreach (WarehousePopupUI kho in khoList)
        {
            var so = new SerializedObject(kho);
            SerializedProperty arr = so.FindProperty("extraItemDatabase");

            if (arr == null || !arr.isArray)
            {
                bc.AppendLine("    ✘ LỖI: WarehousePopupUI không có field mảng 'extraItemDatabase'.");
                continue;
            }

            int themMoi = 0;

            foreach (InventoryItemData d in canDangKy)
            {
                // Chống trùng: chạy lệnh nhiều lần không được nhân bản danh sách.
                bool daCo = false;
                for (int i = 0; i < arr.arraySize; i++)
                {
                    if (arr.GetArrayElementAtIndex(i).objectReferenceValue == d) { daCo = true; break; }
                }
                if (daCo) continue;

                arr.InsertArrayElementAtIndex(arr.arraySize);
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = d;
                themMoi++;
            }

            so.ApplyModifiedProperties();

            if (themMoi > 0)
            {
                EditorUtility.SetDirty(kho);
                bc.AppendLine("    ✔ " + kho.name + ": thêm " + themMoi +
                              " item vào extraItemDatabase (tổng " + arr.arraySize + ")");
                bc.AppendLine("      ⚠ NHỚ Ctrl+S để lưu scene — đây là sửa trong scene, không phải asset.");
            }
            else
            {
                bc.AppendLine("    • " + kho.name + ": đã đủ, không thêm gì.");
            }
        }
    }

    /// <summary>cam_bo_sua → CamBoSua (đặt tên file asset cho dễ đọc trong Project).</summary>
    private static string TenFile(string itemId)
    {
        var sb = new StringBuilder();
        bool hoa = true;

        foreach (char c in itemId)
        {
            if (c == '_' || c == '-' || c == ' ') { hoa = true; continue; }
            sb.Append(hoa ? char.ToUpperInvariant(c) : c);
            hoa = false;
        }

        return sb.ToString();
    }

    // ═════════════════════════════════════════════════════════════════════════════════
    //  TIỆN ÍCH
    // ═════════════════════════════════════════════════════════════════════════════════

    private static void DatO(Transform o, float x)
    {
        if (o == null) return;
        RectTransform rt = o.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta = new Vector2(SlotW, SlotH);
    }

    /// <summary>
    /// Gắn onClick PERSISTENT (ghi được vào prefab). AddListener thường KHÔNG persistent
    /// nên chạy game là mất — đó chính là lý do bấm ô thức ăn trước giờ không ăn thua gì.
    /// </summary>
    private static void GanClick(Transform o, PenMiniPanelUI penUI, int soO, StringBuilder bc)
    {
        if (o == null) return;

        Button b = LayHoacThem<Button>(o.gameObject);

        // Xoá sạch persistent cũ để chạy lệnh nhiều lần không dồn 5 listener trùng nhau.
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(b.onClick, i);

        b.onClick.RemoveAllListeners();

        switch (soO)
        {
            case 1: UnityEventTools.AddPersistentListener(b.onClick, penUI.OnSlot1Clicked); break;
            case 2: UnityEventTools.AddPersistentListener(b.onClick, penUI.OnSlot2Clicked); break;
            default: UnityEventTools.AddPersistentListener(b.onClick, penUI.OnSlot3Clicked); break;
        }

        bc.AppendLine("  ✔ Slot_Food" + soO + " → onClick = OnSlot" + soO + "Clicked (PERSISTENT)");
    }

    /// <summary>
    /// GetComponent trả "fake null" của Unity nên `?? AddComponent` KHÔNG chạy đúng.
    /// Phải so `== null` tường minh.
    /// </summary>
    private static T LayHoacThem<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    private static T TimCon<T>(Transform cha, string ten) where T : Component
    {
        if (cha == null) return null;

        Transform t = cha.Find(ten);
        if (t != null)
        {
            T c = t.GetComponent<T>();
            if (c != null) return c;
        }

        // Dự phòng: quét cả cây con, lấy cái đầu tiên đúng loại.
        return cha.GetComponentInChildren<T>(true);
    }

    private static bool Wire(SerializedObject so, string ten, Object giaTri, StringBuilder bc)
    {
        SerializedProperty p = so.FindProperty(ten);
        if (p == null)
        {
            bc.AppendLine("  ✘ LỖI: PenMiniPanelUI KHÔNG có field '" + ten + "'");
            return false;
        }

        if (giaTri == null)
        {
            bc.AppendLine("  ✘ LỖI: không tìm được object cho '" + ten + "'");
            return false;
        }

        p.objectReferenceValue = giaTri;
        bc.AppendLine("  ✔ " + ten + " → " + giaTri.name);
        return true;
    }

    private static Sprite TimSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;

        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite sp) return sp;

        return null;
    }
}
