using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Tool 7: thêm TAB THỨ 4 "CÔNG CỤ" vào Shop farm (SCN_Farm đang mở). CHỦ FILE: Dev E (Dev B bổ sung búa/kéo 09/09).
    /// (a) 3 asset ToolData ở Assets/_Game/Fishing/Data/Tools/: Tool_axe (rìu, Sếp chốt 08/09) · Tool_hammer (búa) · Tool_scissors (kéo)
    ///     — cả 3 CHƯA có chức năng, mua xong chỉ vào kho; búa/kéo Sếp chốt 09/09 "dùng cho việc khác sau này".
    /// (b) Btn_Tab_Tool = bản NHÂN BẢN của nút tab Decor (lấy qua field btnTabDecor của ShopManager), đặt cạnh phải,
    ///     xoá sạch persistent listener, TMP con đổi thành "CÔNG CỤ", GIỮ NGUYÊN Image/sprite (Sếp thay icon sau).
    /// (c) Wire btnTabTool / imgTabTool / txtTabTool vào ShopManager qua SerializedObject — CHỈ khi ô đang trống.
    /// (d) Đổ toolList: 4 RodData của FishingDatabase (sort theo tier) + rìu + búa + kéo (đúng thứ tự bảng ToolDefs).
    ///     Chỉ THÊM phần còn thiếu, không xoá/không sắp lại thứ Sếp đã kéo tay → chạy lại bao nhiêu lần cũng chỉ bù món thiếu.
    /// Undo.RegisterCreatedObjectUndo mọi object mới, MarkSceneDirty, KHÔNG SaveScene (Sếp Ctrl+S). Có DRY-RUN (7b).
    /// </summary>
    public static class FishingShopToolTabSetupTool
    {
        private const string MenuRoot = "Tools/Farm Game/Hồ Câu/";
        private const string MenuApply = MenuRoot + "7. Thêm tab CÔNG CỤ vào Shop (+ rìu, búa, kéo)";
        private const string MenuDry = MenuRoot + "7b. Thêm tab CÔNG CỤ vào Shop (DRY-RUN, chỉ báo)";
        private const string UndoLabel = "Hồ Câu — Tab CÔNG CỤ trong Shop";

        /// <summary>Thư mục 3 asset ToolData (FishingCheckTool đọc để kiểm).</summary>
        public const string ToolFolder = FishingIds.DataRoot + "/Tools";
        private const string TabButtonName = "Btn_Tab_Tool";
        private const string TabLabelVi = "CÔNG CỤ";
        private const float DefaultTabGap = 12f;

        /// <summary>Một dòng trong bảng công cụ đề xuất. Asset = ToolFolder/Tool_{kind}.asset.</summary>
        private struct ToolDef
        {
            public string kind;        // toolKind + đuôi tên file (axe / hammer / scissors)
            public string itemId;      // itemID lưu kho
            public string nameVi;      // itemName hiển thị
            public int gold;           // giá VÀNG (diamondPrice luôn 0 — bán bằng vàng)
            public int unlock;         // unlockLevel — ShopItemUI đọc bằng reflection theo đúng tên field này
            public int durability;     // durabilityUses — số để dành, chưa hệ nào đọc
            public string description;
        }

        // ── BẢNG ĐỀ XUẤT — Sếp duyệt. Tool KHÔNG ghi đè số nếu asset đã có (chỉ bù field rỗng). ──
        //   Rìu  1200 vàng · lv5 · bền 50  — Sếp chốt 08/09.
        //   Búa   900 vàng · lv4 · bền 50  — ĐỀ XUẤT Dev B 09/09, CHỜ SẾP DUYỆT (rẻ hơn rìu vì mở sớm hơn 1 cấp).
        //   Kéo   600 vàng · lv3 · bền 50  — ĐỀ XUẤT Dev B 09/09, CHỜ SẾP DUYỆT (công cụ nhỏ nhất, mở sớm nhất).
        //   Cả 3 CHƯA có chức năng — mua xong chỉ vào kho. Thứ tự mảng = thứ tự đổ vào toolList sau 4 cần.
        private static readonly ToolDef[] ToolDefs =
        {
            new ToolDef { kind = "axe", itemId = "tool_axe", nameVi = "Rìu", gold = 1200, unlock = 5, durability = 50,
                description = "Rìu đốn củi. CHƯA có chức năng — sau này dùng cho việc khác (Sếp chốt 08/09)." },
            new ToolDef { kind = "hammer", itemId = "tool_hammer", nameVi = "Búa", gold = 900, unlock = 4, durability = 50,
                description = "Búa gỗ. CHƯA có chức năng — dùng cho việc khác sau này (Sếp chốt 09/09)." },
            new ToolDef { kind = "scissors", itemId = "tool_scissors", nameVi = "Kéo", gold = 600, unlock = 3, durability = 50,
                description = "Kéo cắt. CHƯA có chức năng — dùng cho việc khác sau này (Sếp chốt 09/09)." },
        };

        /// <summary>Số công cụ trong bảng đề xuất (FishingCheckTool).</summary>
        public static int ToolCount { get { return ToolDefs.Length; } }

        /// <summary>Đường dẫn asset công cụ thứ i theo bảng ToolDefs (FishingCheckTool).</summary>
        public static string ToolAssetPathAt(int i)
        {
            return ToolAssetPath(ToolDefs[i]);
        }

        /// <summary>itemID mong đợi của công cụ thứ i (FishingCheckTool).</summary>
        public static string ToolItemIdAt(int i)
        {
            return ToolDefs[i].itemId;
        }

        [MenuItem(MenuApply, false, 70)]
        private static void MenuRunApply() { Run(true); }

        [MenuItem(MenuDry, false, 71)]
        private static void MenuRunDry() { Run(false); }

        /// <summary>apply=false: DRY-RUN chỉ liệt kê sẽ làm gì, không đổi asset/scene. Trả báo cáo text.</summary>
        public static string Run(bool apply)
        {
            var report = new StringBuilder();
            report.AppendLine(apply ? "── TAB CÔNG CỤ TRONG SHOP (APPLY) ──" : "── TAB CÔNG CỤ TRONG SHOP (DRY-RUN, không đổi gì) ──");

            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Hồ Câu", "Thoát Play Mode rồi chạy tool.", "OK"); return "Đang Play."; }

            var scene = SceneManager.GetActiveScene();
            if (scene.name != FishingIds.FarmSceneName)
            {
                string loi = "Scene đang mở là '" + scene.name + "', cần mở " + FishingIds.FarmSceneName + " (Assets/_Game/Scenes/SCN_Farm.unity) rồi chạy lại menu này.";
                EditorUtility.DisplayDialog("Hồ Câu — Tab CÔNG CỤ", loi, "OK");
                return loi;
            }

            if (apply && !EditorUtility.DisplayDialog("Thêm tab CÔNG CỤ vào Shop",
                "Sẽ thêm vào scene ĐANG MỞ + project:\n• 3 asset ToolData ở " + ToolFolder + "/: Tool_axe (rìu) · Tool_hammer (búa) · Tool_scissors (kéo) — chỉ tạo cái còn thiếu\n  (giá/cấp búa 900v·lv4, kéo 600v·lv3 là số ĐỀ XUẤT chờ Sếp duyệt — sửa trên asset, tool không đè)\n• Nút " + TabButtonName + " = bản sao nút tab Trang trí, đặt cạnh phải\n• Wire btnTabTool/imgTabTool/txtTabTool vào ShopManager (chỉ ô đang trống)\n• Đổ toolList = 4 cần câu + rìu + búa + kéo (chỉ thêm phần thiếu)\n\nKhông xoá/đổi gì đã có. Scene KHÔNG tự lưu — Sếp Ctrl+S. Ctrl+Z hoàn tác.", "Chạy", "Huỷ"))
            { return "Huỷ."; }

            if (apply) { Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName(UndoLabel); }
            var thieu = new List<string>();
            GameObject ping = null;
            try
            {
                List<ToolData> tools = StepToolAssets(apply, report, thieu);
                ShopManager shop = FindShopManager();
                if (shop == null)
                {
                    report.AppendLine("✖ Không thấy ShopManager trong scene — bỏ qua phần nút và danh sách.");
                    thieu.Add("Không thấy ShopManager trong " + FishingIds.FarmSceneName + " — Sếp mở đúng scene farm có Shop rồi chạy lại");
                }
                else
                {
                    GameObject btn = StepTabButton(apply, shop, report, thieu);
                    if (btn != null) { ping = btn; }
                    StepWireFields(apply, shop, btn, report, thieu);
                    StepFillToolList(apply, shop, tools, report, thieu);
                }
                if (apply) { EditorSceneManager.MarkSceneDirty(scene); }
            }
            catch (Exception e)
            {
                report.AppendLine("✖ LỖI NGOÀI DỰ KIẾN: " + e.Message);
                Debug.LogException(e);
            }
            finally
            {
                if (apply) { Undo.CollapseUndoOperations(Undo.GetCurrentGroup()); }
            }

            var head = new StringBuilder();
            head.AppendLine(apply ? (thieu.Count == 0 ? "✔ Đã thêm tab CÔNG CỤ vào Shop (rìu + búa + kéo)." : "⚠ Đã chạy nhưng còn thiếu:") : "DRY-RUN xong (chưa đổi gì).");
            for (int i = 0; i < thieu.Count; i++) { head.AppendLine("• " + thieu[i]); }
            if (apply)
            {
                head.AppendLine();
                head.AppendLine("SẾP LÀM: 1) Ctrl+S lưu scene. 2) Kéo icon cần câu vào Source Image của " + TabButtonName + " khi đội vẽ giao. 3) Kéo icon rìu/búa/kéo vào ô Item Icon của 3 asset Tool_* khi đội vẽ giao (V4). 4) Duyệt giá/cấp búa (900v·lv4) và kéo (600v·lv3) — sửa thẳng trên asset. 5) Kéo " + TabButtonName + " tới vị trí muốn nếu hàng tab chật (tool chạy lại KHÔNG đè). 6) Play → mở Shop, bấm tab CÔNG CỤ.");
            }
            head.AppendLine("Chi tiết ở Console (lọc FishingSetup).");
            Debug.Log(FishingIds.SetupLogTag + " Tab CÔNG CỤ trong Shop:\n" + report);
            EditorUtility.DisplayDialog("Hồ Câu — Tab CÔNG CỤ", head.ToString(), "OK");
            if (apply && ping != null) { Selection.activeGameObject = ping; EditorGUIUtility.PingObject(ping); }
            return report.ToString();
        }

        // ─────────────────────────────────────────────────────────────────
        //  (a) Asset công cụ (rìu · búa · kéo)
        // ─────────────────────────────────────────────────────────────────

        private static string ToolAssetPath(ToolDef def)
        {
            return ToolFolder + "/Tool_" + def.kind + ".asset";
        }

        /// <summary>Duyệt bảng ToolDefs theo thứ tự; asset nào có rồi thì giữ số, chỉ bù field rỗng. Trả danh sách asset (DRY-RUN chỉ trả cái đã có).</summary>
        private static List<ToolData> StepToolAssets(bool apply, StringBuilder report, List<string> thieu)
        {
            var ketQua = new List<ToolData>(ToolDefs.Length);
            bool folderOk = true;
            for (int i = 0; i < ToolDefs.Length; i++)
            {
                ToolData t = StepOneToolAsset(apply, ToolDefs[i], ref folderOk, report, thieu);
                if (t != null) { ketQua.Add(t); }
            }
            return ketQua;
        }

        private static ToolData StepOneToolAsset(bool apply, ToolDef def, ref bool folderOk, StringBuilder report, List<string> thieu)
        {
            string path = ToolAssetPath(def);
            report.AppendLine("(a) " + path);
            var have = AssetDatabase.LoadAssetAtPath<ToolData>(path);
            if (have != null)
            {
                report.AppendLine("  · Đã có — KHÔNG ghi đè số Sếp đã chỉnh, chỉ bù field rỗng.");
                if (!apply) { return have; }
                bool doi = false;
                if (string.IsNullOrEmpty(have.itemID)) { have.itemID = def.itemId; doi = true; }
                if (string.IsNullOrEmpty(have.itemName)) { have.itemName = def.nameVi; doi = true; }
                if (string.IsNullOrEmpty(have.toolKind)) { have.toolKind = def.kind; doi = true; }
                if (string.IsNullOrEmpty(have.description)) { have.description = def.description; doi = true; }
                if (doi) { EditorUtility.SetDirty(have); AssetDatabase.SaveAssets(); report.AppendLine("    + bù field rỗng (itemID/itemName/toolKind/description)"); }
                return have;
            }

            report.AppendLine("  + Sẽ tạo ToolData: itemID=\"" + def.itemId + "\", tên \"" + def.nameVi + "\", giá " + def.gold.ToString(CultureInfo.InvariantCulture) + " vàng, mở cấp " + def.unlock.ToString(CultureInfo.InvariantCulture) + ", bền " + def.durability.ToString(CultureInfo.InvariantCulture) + " (số ĐỀ XUẤT — Sếp duyệt).");
            if (!apply) { return null; }

            if (!folderOk) { return null; }
            if (!FishingDataSetupTool.EnsureFolder(ToolFolder))
            {
                folderOk = false;
                report.AppendLine("  ✖ Không tạo được thư mục " + ToolFolder);
                thieu.Add("Không tạo được thư mục " + ToolFolder + " — Sếp tạo tay rồi chạy lại");
                return null;
            }

            var tool = ScriptableObject.CreateInstance<ToolData>();
            tool.itemID = def.itemId;
            tool.itemName = def.nameVi;
            tool.goldPrice = def.gold;
            tool.diamondPrice = 0;   // bán bằng VÀNG
            tool.unlockLevel = def.unlock;
            tool.toolKind = def.kind;
            tool.durabilityUses = def.durability;
            tool.description = def.description;
            AssetDatabase.CreateAsset(tool, path);
            EditorUtility.SetDirty(tool);
            AssetDatabase.SaveAssets();
            report.AppendLine("  ✓ Tạo " + path + " — " + def.nameVi.ToUpperInvariant() + " CHƯA CÓ CHỨC NĂNG, mua xong chỉ vào kho. Sếp kéo icon vào ô Item Icon.");
            return tool;
        }

        // ─────────────────────────────────────────────────────────────────
        //  (b) Nút tab
        // ─────────────────────────────────────────────────────────────────

        private static GameObject StepTabButton(bool apply, ShopManager shop, StringBuilder report, List<string> thieu)
        {
            report.AppendLine("(b) " + TabButtonName);

            var so = new SerializedObject(shop);
            var pTool = so.FindProperty("btnTabTool");
            if (pTool != null && pTool.objectReferenceValue != null)
            {
                var daCo = pTool.objectReferenceValue as Button;
                if (daCo != null)
                {
                    report.AppendLine("  · ShopManager.btnTabTool đã trỏ tới '" + daCo.name + "' — giữ nguyên, không tạo nút mới.");
                    return daCo.gameObject;
                }
            }

            var decor = so.FindProperty("btnTabDecor") != null ? so.FindProperty("btnTabDecor").objectReferenceValue as Button : null;
            if (decor == null)
            {
                report.AppendLine("  ✖ ShopManager.btnTabDecor đang trống — không có nút mẫu để nhân bản.");
                thieu.Add("ShopManager.btnTabDecor trống — Sếp kéo nút tab Trang trí vào rồi chạy lại tool");
                return null;
            }

            Transform parent = decor.transform.parent;
            Transform exist = parent != null ? parent.Find(TabButtonName) : null;
            if (exist != null)
            {
                report.AppendLine("  · " + TabButtonName + " đã có trong hierarchy — giữ nguyên vị trí, chỉ bổ sung Button nếu thiếu.");
                if (apply && exist.GetComponent<Button>() == null) { Undo.AddComponent<Button>(exist.gameObject); report.AppendLine("    + Button"); }
                return exist.gameObject;
            }

            // Vị trí: cạnh phải nút Decor, khoảng cách đo từ Seed→Building (đúng nhịp của hàng tab hiện có).
            var rtDecor = decor.transform as RectTransform;
            bool hasLayout = parent != null && parent.GetComponent<LayoutGroup>() != null;
            float gap = DoGap(so, DefaultTabGap);
            Vector2 pos = rtDecor != null ? rtDecor.anchoredPosition + new Vector2(rtDecor.rect.width + gap, 0f) : Vector2.zero;

            report.AppendLine("  + Sẽ nhân bản '" + decor.name + "' → " + TabButtonName + (hasLayout
                ? " (cha có LayoutGroup → chỉ xếp sau nút Decor, không đặt toạ độ)"
                : " tại anchoredPosition " + Fmt(pos) + " (gap " + gap.ToString("0.#", CultureInfo.InvariantCulture) + " đo từ Seed→Building)"));
            if (!apply) { return null; }

            GameObject copy = UnityEngine.Object.Instantiate(decor.gameObject, parent, false);
            Undo.RegisterCreatedObjectUndo(copy, UndoLabel);
            copy.name = TabButtonName;
            // [Reviewer L10] UnpackPrefabInstance ném ArgumentException nếu copy là instance nhưng KHÔNG phải root ngoài cùng.
            if (PrefabUtility.IsPartOfPrefabInstance(copy) && PrefabUtility.IsOutermostPrefabInstanceRoot(copy)) { PrefabUtility.UnpackPrefabInstance(copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); }
            copy.SetActive(true);

            var rt = copy.transform as RectTransform;
            if (rt != null && rtDecor != null && !hasLayout)
            {
                rt.anchorMin = rtDecor.anchorMin; rt.anchorMax = rtDecor.anchorMax; rt.pivot = rtDecor.pivot;
                rt.sizeDelta = rtDecor.sizeDelta; rt.localScale = rtDecor.localScale;
                rt.anchoredPosition = pos;
            }
            copy.transform.SetSiblingIndex(decor.transform.GetSiblingIndex() + 1);

            // Xoá SẠCH persistent listener — không kéo onClick ShowTab(2) của nút Decor sang.
            // ShopManager.Start() tự AddListener ShowTab(3) khi btnTabTool được gán.
            var btn = copy.GetComponent<Button>();
            if (btn == null)
            {
                btn = copy.AddComponent<Button>();
                var img0 = copy.GetComponent<Image>();
                if (img0 != null) { btn.targetGraphic = img0; }
            }
            btn.onClick = new Button.ButtonClickedEvent();
            var conBtn = copy.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < conBtn.Length; i++) { if (conBtn[i] != btn) { conBtn[i].onClick = new Button.ButtonClickedEvent(); } }

            // Nhãn — GIỮ NGUYÊN Image/sprite, Sếp thay icon sau.
            var tmp = copy.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) { tmp.text = TabLabelVi; }
            else
            {
                var txt = copy.GetComponentInChildren<Text>(true);
                if (txt != null) { txt.text = TabLabelVi; }
                else { thieu.Add(TabButtonName + " không có TMP/Text con — Sếp gõ chữ \"" + TabLabelVi + "\" tay"); }
            }

            EditorUtility.SetDirty(copy);
            report.AppendLine("  ✓ Tạo " + Path(copy.transform) + " — onClick trống, nhãn \"" + TabLabelVi + "\", Image/sprite giữ nguyên.");
            return copy;
        }

        /// <summary>Khoảng cách giữa 2 tab, đo từ Seed→Building. Không đo được thì trả mặc định.</summary>
        private static float DoGap(SerializedObject so, float macDinh)
        {
            var pSeed = so.FindProperty("btnTabSeed");
            var pBuild = so.FindProperty("btnTabBuilding");
            var seed = pSeed != null ? pSeed.objectReferenceValue as Button : null;
            var build = pBuild != null ? pBuild.objectReferenceValue as Button : null;
            if (seed == null || build == null) { return macDinh; }
            var rtSeed = seed.transform as RectTransform;
            var rtBuild = build.transform as RectTransform;
            if (rtSeed == null || rtBuild == null) { return macDinh; }
            float g = rtBuild.anchoredPosition.x - rtSeed.anchoredPosition.x - rtSeed.rect.width;
            if (g > 0f && g < 200f) { return g; }
            return macDinh;
        }

        // ─────────────────────────────────────────────────────────────────
        //  (c) Wire field — CHỈ khi ô đang trống
        // ─────────────────────────────────────────────────────────────────

        private static void StepWireFields(bool apply, ShopManager shop, GameObject btnGo, StringBuilder report, List<string> thieu)
        {
            report.AppendLine("(c) Wire ShopManager.btnTabTool / imgTabTool / txtTabTool");
            if (btnGo == null)
            {
                report.AppendLine("  · Chưa có nút — bỏ qua (DRY-RUN hoặc không nhân bản được).");
                return;
            }
            if (!apply) { report.AppendLine("  + Sẽ gán 3 ô còn trống trỏ tới " + btnGo.name); return; }

            var so = new SerializedObject(shop);
            int gan = 0;

            gan += GanNeuTrong(so, "btnTabTool", btnGo.GetComponent<Button>(), report);
            gan += GanNeuTrong(so, "imgTabTool", btnGo.GetComponent<Image>(), report);
            gan += GanNeuTrong(so, "txtTabTool", btnGo.GetComponentInChildren<TMP_Text>(true), report);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(shop);
            report.AppendLine("  ✓ Gán " + gan.ToString(CultureInfo.InvariantCulture) + " ô (ô nào đã có tham chiếu thì giữ nguyên).");
            if (btnGo.GetComponentInChildren<TMP_Text>(true) == null) { thieu.Add("Không thấy TMP_Text con trong " + TabButtonName + " — Sếp kéo tay vào ô txtTabTool"); }
        }

        private static int GanNeuTrong(SerializedObject so, string fieldName, UnityEngine.Object value, StringBuilder report)
        {
            var p = so.FindProperty(fieldName);
            if (p == null)
            {
                report.AppendLine("  ✖ Không thấy field " + fieldName + " trên ShopManager (script cũ chưa cập nhật?).");
                return 0;
            }
            if (p.objectReferenceValue != null) { report.AppendLine("  · " + fieldName + " đã có — giữ nguyên."); return 0; }
            if (value == null) { report.AppendLine("  ⚠ " + fieldName + ": không tìm được component để gán."); return 0; }
            p.objectReferenceValue = value;
            report.AppendLine("  + " + fieldName + " = " + value.name);
            return 1;
        }

        // ─────────────────────────────────────────────────────────────────
        //  (d) Đổ toolList
        // ─────────────────────────────────────────────────────────────────

        private static void StepFillToolList(bool apply, ShopManager shop, List<ToolData> tools, StringBuilder report, List<string> thieu)
        {
            report.AppendLine("(d) ShopManager.toolList");

            var muon = new List<BaseItemData>();
            var db = AssetDatabase.LoadAssetAtPath<FishingDatabase>(FishingDataSetupTool.DatabasePath);
            if (db == null || db.rods == null || db.rods.Count == 0)
            {
                report.AppendLine("  ⚠ Chưa có FishingDatabase (" + FishingDataSetupTool.DatabasePath + ") hoặc chưa có cần nào — chạy menu 1 trước.");
                thieu.Add("Chưa có 4 cần câu trong FishingDatabase — chạy \"1. Tạo/cập nhật Data\" rồi chạy lại tool 7");
            }
            else
            {
                var rods = new List<RodData>();
                for (int i = 0; i < db.rods.Count; i++) { if (db.rods[i] != null) { rods.Add(db.rods[i]); } }
                rods.Sort((a, b) => a.tier.CompareTo(b.tier));
                for (int i = 0; i < rods.Count; i++) { muon.Add(rods[i]); }
            }
            // Công cụ xếp SAU 4 cần, theo thứ tự bảng ToolDefs: rìu → búa → kéo.
            if (tools != null)
            {
                for (int i = 0; i < tools.Count; i++) { if (tools[i] != null) { muon.Add(tools[i]); } }
            }
            if (tools == null || tools.Count < ToolDefs.Length)
            {
                report.AppendLine("  · Mới có " + (tools != null ? tools.Count : 0).ToString(CultureInfo.InvariantCulture) + "/" + ToolDefs.Length.ToString(CultureInfo.InvariantCulture) + " asset công cụ (DRY-RUN chưa tạo, hoặc tạo lỗi) — món thiếu sẽ được bù ở lần APPLY sau.");
            }

            var so = new SerializedObject(shop);
            var list = so.FindProperty("toolList");
            if (list == null)
            {
                report.AppendLine("  ✖ Không thấy field toolList trên ShopManager (script cũ chưa cập nhật?).");
                thieu.Add("ShopManager chưa có field toolList — kiểm tra lại ShopManager.cs");
                return;
            }

            var daCo = new HashSet<UnityEngine.Object>();
            for (int i = 0; i < list.arraySize; i++)
            {
                var v = list.GetArrayElementAtIndex(i).objectReferenceValue;
                if (v != null) { daCo.Add(v); }
            }

            var them = new List<BaseItemData>();
            for (int i = 0; i < muon.Count; i++)
            {
                if (muon[i] == null || daCo.Contains(muon[i])) { continue; }
                them.Add(muon[i]);
                daCo.Add(muon[i]);
            }

            if (them.Count == 0)
            {
                report.AppendLine("  · Đủ hàng rồi (" + list.arraySize.ToString(CultureInfo.InvariantCulture) + " món) — không thêm gì.");
                return;
            }

            var ten = new List<string>();
            for (int i = 0; i < them.Count; i++) { ten.Add(string.IsNullOrEmpty(them[i].itemName) ? them[i].name : them[i].itemName); }
            report.AppendLine("  + Sẽ thêm " + them.Count.ToString(CultureInfo.InvariantCulture) + " món: " + string.Join(", ", ten));
            if (!apply) { return; }

            for (int i = 0; i < them.Count; i++)
            {
                int idx = list.arraySize;
                list.arraySize = idx + 1;
                list.GetArrayElementAtIndex(idx).objectReferenceValue = them[i];
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(shop);
            report.AppendLine("  ✓ toolList giờ có " + list.arraySize.ToString(CultureInfo.InvariantCulture) + " món (chỉ THÊM, không xoá/sắp lại thứ Sếp đã kéo tay).");
        }

        // ─────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────

        private static ShopManager FindShopManager()
        {
            return UnityEngine.Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        }

        private static string Path(Transform t)
        {
            string p = t.name;
            while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
            return p;
        }

        private static string Fmt(Vector2 v)
        {
            return "(" + v.x.ToString("0.#", CultureInfo.InvariantCulture) + ", " + v.y.ToString("0.#", CultureInfo.InvariantCulture) + ")";
        }
    }
}
