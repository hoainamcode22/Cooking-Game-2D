#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// ═══════════════════════════════════════════════════════════════════════════════════════
//  MillPopupBuilderTool — DỰNG TOÀN BỘ POPUP "MÁY XAY THỨC ĂN" BẰNG CODE
//
//  ┌─ NGUỒN THIẾT KẾ DUY NHẤT ────────────────────────────────────────────────────────┐
//  │ Assets/Assetsgame/popup/ui_mill_assets/full_mill_ui.html                          │
//  │ Mọi con số trong `MillDesign` đều kèm chú thích "HTML dòng N (.selector)".         │
//  │ KHÔNG ước lượng bằng mắt. Sửa số ở đây mà không sửa HTML = lệch video.            │
//  └──────────────────────────────────────────────────────────────────────────────────┘
//
//  ┌─ KHÔNG CÓ HỆ TAB ───────────────────────────────────────────────────────────────┐
//  │ Video có 3 tab (HTML dòng 150 `.tabs-row`, 481-486). Chủ dự án XÁC NHẬN đó là    │
//  │ lỗi thiết kế — mỗi máy một popup riêng. Tool KHÔNG dựng node tab nào; hàng trên   │
//  │ của panel chỉ còn chip số dư kim cương căn phải (HTML dòng 170 `.diamond-counter`)│
//  │ Hai sprite tab_active/tab_inactive.png CỐ Ý không dùng.                           │
//  └──────────────────────────────────────────────────────────────────────────────────┘
//
//  ┌─ CẠM BẪY ĐÃ GÂY LỖI THẬT TRONG DỰ ÁN, TOOL NÀY TRÁNH TUYỆT ĐỐI ─────────────────┐
//  │ 1. KHÔNG `GetComponent<T>() ?? AddComponent<T>()` — component thiếu là "fake-null"│
//  │    (khác null theo phép so của C# mà `??` dùng). Xem `MillUI.Comp<T>()`.          │
//  │ 2. KHÔNG `SetParent()` vào transform NẰM TRONG PREFAB ASSET. Tool dựng mọi thứ    │
//  │    trong SCENE rồi mới `SaveAsPrefabAsset`; chỗ nào buộc phải sửa prefab thì đi    │
//  │    qua `LoadPrefabContents → SaveAsPrefabAsset → UnloadPrefabContents` (finally). │
//  │ 3. KHÔNG `AssetDatabase.StartAssetEditing()` quanh đoạn import PNG — nó hoãn      │
//  │    import, `LoadAssetAtPath` ngay sau đó trả null.                                │
//  │ 4. KHÔNG báo "thành công" khi có bước lỗi — mọi lệnh đếm lỗi và LogError.          │
//  └──────────────────────────────────────────────────────────────────────────────────┘
//
//  CONTRACT RUNTIME: Assets/_Game/Farm/Scripts/MillPopup/ (Dev A). Toàn bộ field của
//  MillPopupUI / MillSlotUI / MillRecipeCardUI là `[SerializeField] private` ⇒ tool BUỘC
//  phải wire qua SerializedObject.FindProperty(tên field). Đổi tên field bên Dev A là tool
//  báo "KHÔNG có field này" ở lệnh 3 chứ không âm thầm bỏ qua.
// ═══════════════════════════════════════════════════════════════════════════════════════

namespace Farm.EditorTools.Mill
{
    /// <summary>
    /// Editor tool dựng popup "MÁY XAY THỨC ĂN" từ bản thiết kế HTML gốc và wire vào các
    /// component runtime của Dev A. Bốn lệnh nằm ở menu <c>Tools/Farm/Popup May Xay</c>.
    /// </summary>
    public static class MillPopupBuilderTool
    {
        internal const string LOG = "[MILLUI] ";

        // ── Đường dẫn asset do tool tạo/ghi ─────────────────────────────────────────
        internal const string PrefabFolder = "Assets/_Game/Farm/Prefabs/Mill";
        internal const string DataFolder   = "Assets/_Game/Farm/Data/Mill";
        internal const string PopupPrefabPath = PrefabFolder + "/MillPopup_Root.prefab";
        internal const string CardPrefabPath  = PrefabFolder + "/MillRecipeCard.prefab";
        internal const string ConfigPath      = DataFolder + "/MillConfig.asset";

        // Tên node gốc trong Canvas — dùng để tìm lại và cập nhật TẠI CHỖ (giữ GUID scene).
        internal const string RootName = "MillPopup_Root";

        // ═══════════════════════════════════════════════════════════════════════════
        //  LỆNH 1 — DỰNG POPUP
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Dựng (hoặc dựng lại) toàn bộ popup máy xay vào Canvas UI của scene đang mở,
        /// tạo prefab card công thức, wire mọi field contract, rồi lưu MillPopup_Root.prefab.
        /// </summary>
        [MenuItem("Tools/Farm/Popup May Xay/1. Dung Popup (Scene + Prefab)", false, 1)]
        public static void DungPopup()
        {
            var rep = new MillReport("DỰNG POPUP MÁY XAY");
            try
            {
                MillSpriteFactory.Reset();
                MillSkin.XoaCache();

                // ── Canvas ───────────────────────────────────────────────────────────
                Canvas canvas = TimCanvasUI(rep);
                if (canvas == null)
                {
                    rep.Loi("Không tìm thấy Canvas nào trong scene đang mở. " +
                            "Mở scene có Canvas (SCN_Farm) rồi chạy lại.");
                    rep.KetThuc();
                    return;
                }

                Undo.SetCurrentGroupName("Dựng popup máy xay");
                int undoGroup = Undo.GetCurrentGroup();

                // ── Font ─────────────────────────────────────────────────────────────
                MillUI.Font = MillSpriteFactory.TimFont(rep);

                // ── Prefab card công thức (dựng trong scene rồi mới lưu) ─────────────
                MillRecipeCardUI cardPrefab = MillCardBuilder.TaoPrefabCard(rep);

                // ── Root popup: tìm lại để cập nhật tại chỗ ──────────────────────────
                GameObject root = TimHoacTaoRoot(canvas, rep);
                if (root == null) { rep.KetThuc(); return; }

                Undo.RegisterFullObjectHierarchyUndo(root, "Dựng popup máy xay");

                // Dọn sạch con cũ — giữ lại chính node root để mọi tham chiếu scene
                // (và component MillPopupUI cùng các field khác Dev A đã kéo tay) còn nguyên.
                MillUI.XoaHetCon(root.transform);

                // ── Dựng hierarchy ───────────────────────────────────────────────────
                var built = MillPopupBuilder.Dung(root, cardPrefab, rep);

                // ── Wire contract ────────────────────────────────────────────────────
                MillWiring.WirePopup(root, built, cardPrefab, rep);

                // ── Popup tắt sẵn: root ACTIVE (Awake phải chạy để set Instance),
                //    node con `PopupRoot` mới là node bị tắt. Đây đúng là lý do field
                //    `popupRoot` tồn tại (MillPopupUI.Open bật node đó).
                root.SetActive(true);
                if (built.popupRoot != null) built.popupRoot.SetActive(false);

                // ── Lưu prefab root ──────────────────────────────────────────────────
                LuuPrefabRoot(root, rep);

                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Undo.CollapseUndoOperations(undoGroup);

                // LƯU NGAY — xem chú thích khối "LƯU SCENE" bên dưới. Dựng vào scene mà
                // không ghi đĩa thì lần reload scene tới là mất sạch.
                LuuScene(rep);
                Selection.activeGameObject = root;

                rep.Can("Gán MillConfig.asset vào field `config` của MillPopupUI " +
                        "(chạy lệnh 2 để tạo config mẫu trước).");
                rep.Can("Điền icon thật cho 4 MillRecipeData (field icon / animalBadgeIcon / " +
                        "ingredients[].icon) — tool KHÔNG bịa icon.");
                rep.Can("Thay sprite placeholder của 2 bó cỏ trên băng tải " +
                        "(PopupRoot/Window/InnerPanel/Content/MainContent/RightColumn/AnimationBox/" +
                        "BeltItem_1|2) bằng icon lúa mì thật — MillPopupUI không tự gán sprite này.");
                rep.Can("Kiểm tra sort order: tool đặt Canvas con `MillPopup_Canvas` sortingOrder=" +
                        MillDesign.SortOrder + ". Nếu popup khác đã dùng số cao hơn thì nâng lên.");
                rep.Can("Chạy lệnh 3 để soát lại toàn bộ field contract sau khi gán config.");
            }
            catch (Exception e)
            {
                rep.Loi("Ngoại lệ: " + e.Message + "\n" + e.StackTrace);
            }
            rep.KetThuc();
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  LỆNH 2 — DATA MẪU
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo (hoặc cập nhật tại chỗ, giữ GUID) <c>MillConfig.asset</c> và 4 công thức
        /// đúng như video: Cám cho gà, Cám cho heo, Cỏ trộn cho bò, Cám cho bò sữa (khoá).
        /// </summary>
        [MenuItem("Tools/Farm/Popup May Xay/2. Tao Data Mau (4 cong thuc)", false, 2)]
        public static void TaoDataMau()
        {
            var rep = new MillReport("TẠO DATA MẪU MÁY XAY");
            try { MillDataBuilder.Tao(rep); }
            catch (Exception e) { rep.Loi("Ngoại lệ: " + e.Message + "\n" + e.StackTrace); }
            rep.KetThuc();
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  LỆNH 3 — KIỂM TRA (chạy khô)
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Chạy khô: liệt kê sprite tìm được / thiếu / sẽ generate, và MỌI field contract
        /// chưa wire được. Không sửa gì trong scene lẫn asset.
        /// </summary>
        [MenuItem("Tools/Farm/Popup May Xay/3. Kiem Tra (bao cao)", false, 3)]
        public static void KiemTra()
        {
            var rep = new MillReport("KIỂM TRA POPUP MÁY XAY (chạy khô)");
            try { MillAudit.ChayKho(rep); }
            catch (Exception e) { rep.Loi("Ngoại lệ: " + e.Message + "\n" + e.StackTrace); }
            rep.KetThuc();
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  LỆNH 4 — GẮN CLICK
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gắn <see cref="MillBuildingClick"/> lên công trình <c>MayThucAn_Anim</c> trong scene.
        /// </summary>
        [MenuItem("Tools/Farm/Popup May Xay/4. Gan Click Vao MayThucAn_Anim", false, 4)]
        public static void GanClick()
        {
            var rep = new MillReport("GẮN CLICK VÀO MayThucAn_Anim");
            try
            {
                var ungVien = new List<GameObject>();
                foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                      FindObjectsSortMode.None))
                {
                    if (tr == null) continue;
                    if (tr.name.IndexOf("MayThucAn", StringComparison.OrdinalIgnoreCase) >= 0)
                        ungVien.Add(tr.gameObject);
                }

                if (ungVien.Count == 0)
                {
                    rep.Loi("Không tìm thấy object nào tên chứa 'MayThucAn' trong scene đang mở. " +
                            "Mở SCN_Farm (hoặc scene có công trình máy xay) rồi chạy lại.");
                    rep.KetThuc();
                    return;
                }

                int gan = 0;
                foreach (GameObject go in ungVien)
                {
                    Undo.RegisterFullObjectHierarchyUndo(go, "Gắn MillBuildingClick");

                    // ⚠ Cạm bẫy #1: so tường minh `== null`, KHÔNG dùng `??`.
                    MillBuildingClick cl = go.GetComponent<MillBuildingClick>();
                    if (cl == null)
                    {
                        cl = Undo.AddComponent<MillBuildingClick>(go);
                        rep.Ok("Đã thêm MillBuildingClick lên '" + go.name + "'.");
                        gan++;
                    }
                    else
                    {
                        rep.Ok("'" + go.name + "' đã có MillBuildingClick — giữ nguyên.");
                    }

                    // TẠO COLLIDER NGAY BÂY GIỜ, KHÔNG ĐỢI Awake.
                    // Trước đây tool chỉ ghi "sẽ tự thêm lúc Awake" nên trong Edit Mode công
                    // trình KHÔNG có collider nào — chủ dự án mở Inspector không thấy vùng bấm,
                    // không biết phải bấm vào đâu, và không chỉnh được vùng bấm bằng tay.
                    // Tạo sẵn ở đây thì thấy ngay khung xanh trong Scene view, kéo/sửa được,
                    // và Ctrl+Z hoàn tác được. Runtime vẫn giữ nhánh tự thêm làm lưới an toàn.
                    if (go.GetComponent<Collider2D>() == null)
                    {
                        SpriteRenderer srBam = go.GetComponent<SpriteRenderer>();
                        if (srBam == null || srBam.sprite == null)
                        {
                            rep.Canh("'" + go.name + "' không có Collider2D lẫn SpriteRenderer (hoặc " +
                                     "SpriteRenderer chưa có sprite) ⇒ không suy ra được vùng bấm. " +
                                     "Thêm Collider2D bằng tay, nếu không bấm vào công trình sẽ không mở popup.");
                        }
                        else
                        {
                            BoxCollider2D box = Undo.AddComponent<BoxCollider2D>(go);

                            // sprite.bounds là bounds LOCAL (đã tính pivot + pixelsPerUnit) —
                            // đúng thứ size/offset cần. ĐỪNG dùng sr.bounds (bounds WORLD, đã
                            // nhân scale) vì object này đang scale 350 ⇒ hộp sẽ to sai 350 lần.
                            Bounds b = srBam.sprite.bounds;
                            box.size   = new Vector2(b.size.x, b.size.y);
                            box.offset = new Vector2(b.center.x, b.center.y);

                            Vector3 sc = go.transform.lossyScale;
                            rep.Ok("Đã thêm BoxCollider2D cho '" + go.name + "': size local " +
                                   box.size.x.ToString("0.00") + " x " + box.size.y.ToString("0.00") +
                                   " ⇒ vùng bấm thực tế " +
                                   (box.size.x * Mathf.Abs(sc.x)).ToString("0") + " x " +
                                   (box.size.y * Mathf.Abs(sc.y)).ToString("0") + " unit world " +
                                   "(scale " + Mathf.Abs(sc.x).ToString("0") + "). " +
                                   "Bấm ĐÚNG vào hình công trình trong Game view là mở popup.");
                        }
                    }
                    else
                    {
                        rep.Ok("'" + go.name + "' đã có Collider2D — giữ nguyên vùng bấm hiện tại.");
                    }

                    if (PrefabUtility.IsPartOfPrefabInstance(go))
                        rep.Canh("'" + go.name + "' là instance của prefab ⇒ component vừa thêm là " +
                                 "OVERRIDE trên instance. Muốn vào prefab gốc thì Apply thủ công " +
                                 "(tool KHÔNG tự Apply để không đè thay đổi khác của bạn).");

                    EditorUtility.SetDirty(go);
                }

                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                // Lưu VÔ ĐIỀU KIỆN, không phụ thuộc `gan > 0`: lần chạy trước có thể đã
                // thêm component (gan = 0 lần này) mà scene vẫn chưa từng được ghi đĩa.
                LuuScene(rep);

                rep.Can("Thêm `|| MillPopupUI.AnyOpen` vào cuối PopupManager.IsAnyPopupOpen() — " +
                        "Dev A không được sửa file có sẵn nên đã phơi cờ static thay thế.");
                rep.Can("Kéo prefab MillPopup_Root vào Canvas (hoặc chạy lệnh 1) — " +
                        "không có MillPopupUI trong scene thì bấm vào máy không mở được popup.");
            }
            catch (Exception e) { rep.Loi("Ngoại lệ: " + e.Message + "\n" + e.StackTrace); }
            rep.KetThuc();
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  LƯU SCENE — VÌ SAO PHẢI CÓ HÀM NÀY
        //
        //  ⚠ SỰ CỐ 20/08: chủ dự án chạy lệnh 1 + lệnh 4, log báo THÀNH CÔNG, nhưng vào
        //  Play Mode bấm vào máy xay không có gì xảy ra. Nguyên nhân: hai lệnh đó chỉ gọi
        //  `EditorSceneManager.MarkSceneDirty` — tức là "đánh dấu scene có thay đổi", KHÔNG
        //  ghi xuống đĩa. Sau đó Unity reload scene (đổi scene, reimport, hoặc bấm Don't Save)
        //  ⇒ toàn bộ node popup + component MillBuildingClick BỐC HƠI, chỉ còn lại
        //  MillPopup_Root.prefab (vì prefab là ASSET, được lưu riêng).
        //  Kiểm chứng: `grep` guid của MillPopupUI và MillBuildingClick trong SCN_Farm.unity
        //  đều ra 0 lần, trong khi MillPopup_Root.prefab vẫn wire đầy đủ.
        //
        //  Bài học: tool dựng vào SCENE thì tool phải TỰ LƯU SCENE. Không được giao việc
        //  Ctrl+S cho người dùng rồi coi là xong — quên một lần là mất sạch và rất khó đoán
        //  ra vì log vẫn xanh.
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Ghi scene đang mở xuống đĩa và flush mọi asset. Trả về true nếu lưu được.
        /// </summary>
        internal static bool LuuScene(MillReport rep)
        {
            AssetDatabase.SaveAssets();

            UnityEngine.SceneManagement.Scene sc = EditorSceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(sc.path))
            {
                rep.Canh("Scene đang mở CHƯA được lưu thành file (chưa có đường dẫn) ⇒ tool " +
                         "không lưu hộ được. Bấm File ▸ Save As… đặt tên scene rồi chạy lại lệnh này.");
                return false;
            }

            bool ok = EditorSceneManager.SaveScene(sc);

            if (ok) rep.Ok("ĐÃ LƯU SCENE '" + sc.name + "' xuống đĩa — thay đổi không còn bị mất " +
                           "khi Unity reload scene nữa.");
            else    rep.Loi("KHÔNG lưu được scene '" + sc.name + "'. Bấm Ctrl+S ngay bây giờ, " +
                            "nếu không toàn bộ popup vừa dựng sẽ mất.");

            return ok;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  LỆNH 0 — LÀM TẤT CẢ MỘT PHÁT
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Chạy trọn chuỗi: data ▸ dựng popup (tự gán config) ▸ gắn click ▸ LƯU SCENE.
        /// Có lệnh này để chủ dự án không phải nhớ thứ tự 4 lệnh và không thể quên Ctrl+S.
        /// </summary>
        [MenuItem("Tools/Farm/Popup May Xay/0. LAM TAT CA (Data + Popup + Click + Luu Scene)", false, 0)]
        public static void LamTatCa()
        {
            // Thứ tự BẮT BUỘC: data trước, vì WirePopup đọc MillConfig.asset để gán vào
            // field `config`. Không có config thì Update() của MillPopupUI return sớm ⇒
            // popup mở ra nhưng đứng im, đúng cái lỗi đã mất một buổi để tìm.
            TaoDataMau();
            DungPopup();
            GanClick();

            var rep = new MillReport("LƯU SCENE SAU KHI DỰNG");
            try { LuuScene(rep); }
            catch (Exception e) { rep.Loi("Ngoại lệ: " + e.Message + "\n" + e.StackTrace); }
            rep.KetThuc();
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  HẠ TẦNG DÙNG CHUNG CHO CÁC LỆNH
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tìm Canvas UI của scene: ưu tiên Canvas có CanvasScaler đặt reference resolution
        /// 1920×1080 (chuẩn dự án — xem BoatDockUnlockPopupUI.cs dòng 186-188), rồi tới
        /// Canvas có CanvasScaler bất kỳ, cuối cùng là Canvas đầu tiên.
        /// </summary>
        internal static Canvas TimCanvasUI(MillReport rep)
        {
            // ⚠ SỬA 20/08 — VÌ SAO PHẢI ƯU TIÊN THEO TÊN
            //
            // Bản trước chỉ lấy "Canvas đầu tiên có CanvasScaler 1920×1080". Thứ tự
            // FindObjectsByType là KHÔNG XÁC ĐỊNH, và scene này có 3 canvas cùng thoả:
            // Canvas_HUD, Canvas_Popup, Canvas_MarketPopup. Kết quả thực tế: popup máy xay
            // bị dựng vào Canvas_MarketPopup.
            //
            // Vì sao đó là bom hẹn giờ: Canvas_MarketPopup là canvas RIÊNG của popup chợ.
            // DisableStartupPopupsTool có nó trong POPUP_PARENT_NAMES và MarketManager.cs:174
            // ghi rõ "Canvas_MarketPopup có thể bị tool DisableStartupPopups tắt ở tầng trên".
            // Ngày nào chạy tool đó là popup máy xay bị tắt theo cả cụm cha ⇒ Awake không
            // chạy, Instance null, Update không chạy: bấm không mở, mà mở được cũng đứng im.
            //
            // Nay: ưu tiên Canvas_Popup (canvas popup DÙNG CHUNG của dự án), và loại hẳn các
            // canvas riêng của từng popup khác.
            string[] uuTienTen = { "Canvas_Popup" };
            string[] traTen    = { "Canvas_MarketPopup", "Canvas_StallPopup",
                                   "Canvas_OrderBoardPopup", "Canvas_HUD" };

            Canvas[] tatCa = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                             FindObjectsSortMode.None);
            Canvas theoTen = null, tot = null, coScaler = null, dauTien = null;

            foreach (Canvas c in tatCa)
            {
                if (c == null) continue;
                // Bỏ qua Canvas con lồng trong Canvas khác (vd chính Canvas của popup này).
                if (c.transform.parent != null && c.transform.parent.GetComponentInParent<Canvas>() != null)
                    continue;

                bool biTra = System.Array.IndexOf(traTen, c.name) >= 0;

                if (System.Array.IndexOf(uuTienTen, c.name) >= 0 && theoTen == null)
                    theoTen = c;

                if (!biTra && dauTien == null) dauTien = c;

                CanvasScaler sc = c.GetComponent<CanvasScaler>();
                if (sc == null) continue;
                if (!biTra && coScaler == null) coScaler = c;

                Vector2 r = sc.referenceResolution;
                if (!biTra && tot == null &&
                    Mathf.Approximately(r.x, 1920f) && Mathf.Approximately(r.y, 1080f))
                    tot = c;
            }

            Canvas chon = theoTen != null ? theoTen
                        : (tot != null ? tot
                        : (coScaler != null ? coScaler : dauTien));

            if (chon == null)
            {
                rep.Loi("Không tìm thấy Canvas nào dùng được trong scene đang mở.");
                return null;
            }

            CanvasScaler scChon = chon.GetComponent<CanvasScaler>();

            if (theoTen != null)
                rep.Ok("Canvas: '" + chon.name + "' — chọn theo TÊN (canvas popup dùng chung " +
                       "của dự án, không bị DisableStartupPopupsTool tắt).");
            else if (tot != null)
                rep.Ok("Canvas: '" + chon.name + "' (CanvasScaler 1920×1080 — đúng chuẩn dự án).");
            else if (coScaler != null)
                rep.Canh("Canvas: '" + chon.name + "' có CanvasScaler nhưng reference resolution " +
                         "KHÁC 1920×1080 (" + scChon.referenceResolution +
                         ") ⇒ popup sẽ lệch tỉ lệ so với video. Kiểm tra lại.");
            else
                rep.Canh("Canvas: '" + chon.name + "' KHÔNG có CanvasScaler ⇒ popup không co giãn " +
                         "theo độ phân giải. Thêm CanvasScaler 1920×1080.");

            if (System.Array.IndexOf(traTen, chon.name) >= 0)
                rep.Canh("Đang phải dùng '" + chon.name + "' vì scene không có canvas nào khác. " +
                         "Canvas này có thể bị tắt bởi DisableStartupPopupsTool hoặc bởi manager " +
                         "của popup sở hữu nó ⇒ popup máy xay sẽ tắt theo. Nên tạo một " +
                         "Canvas_Popup dùng chung.");

            return chon;
        }

        /// <summary>Tìm lại node root cũ (giữ GUID scene) hoặc tạo mới dưới Canvas.</summary>
        private static GameObject TimHoacTaoRoot(Canvas canvas, MillReport rep)
        {
            // Ưu tiên tìm theo COMPONENT: người dùng có thể đã đổi tên node.
            MillPopupUI[] co = Object.FindObjectsByType<MillPopupUI>(FindObjectsInactive.Include,
                                                                    FindObjectsSortMode.None);
            if (co != null && co.Length > 0)
            {
                if (co.Length > 1)
                    rep.Canh("Scene có " + co.Length + " MillPopupUI. Tool cập nhật cái đầu tiên " +
                             "('" + co[0].name + "') và KHÔNG xoá cái còn lại — xoá tay để tránh " +
                             "hai popup tranh nhau singleton Instance.");
                rep.Ok("Cập nhật TẠI CHỖ node '" + co[0].name + "' (giữ mọi tham chiếu scene).");
                return DoiChaNeuSaiCanvas(co[0].gameObject, canvas, rep);
            }

            Transform theoTen = canvas.transform.Find(RootName);
            if (theoTen != null)
            {
                rep.Ok("Cập nhật TẠI CHỖ node '" + RootName + "' đã có trong Canvas.");
                return theoTen.gameObject;
            }

            var go = new GameObject(RootName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Tạo MillPopup_Root");
            go.transform.SetParent(canvas.transform, false);   // Canvas là object SCENE — hợp lệ.
            rep.Ok("Tạo mới node '" + RootName + "' dưới Canvas '" + canvas.name + "'.");
            return go;
        }

        /// <summary>
        /// Nếu root đang nằm SAI canvas thì dời nó về canvas đã chọn, giữ nguyên layout.
        ///
        /// ⚠ SỰ CỐ 21/08: bản trước "cập nhật tại chỗ" nghĩa là root nằm đâu để nguyên đó.
        /// Kết hợp với lỗi chọn canvas cũ (vớ trúng Canvas_MarketPopup — canvas RIÊNG của
        /// popup chợ), popup máy xay bị kẹt vĩnh viễn trong canvas chợ: chạy lại lệnh bao
        /// nhiêu lần cũng không tự thoát ra, và chủ dự án nhìn Hierarchy tưởng tool đã phá
        /// popup chợ của họ. Nay: sai chỗ là DỜI, có Undo, và ghi rõ vào báo cáo.
        /// </summary>
        private static GameObject DoiChaNeuSaiCanvas(GameObject root, Canvas canvas, MillReport rep)
        {
            Canvas canvasHienTai = root.transform.parent != null
                ? root.transform.parent.GetComponentInParent<Canvas>()
                : null;

            if (canvasHienTai == canvas) return root;

            string tenCu = canvasHienTai != null ? canvasHienTai.name : "(ngoài canvas)";

            Undo.SetTransformParent(root.transform, canvas.transform, "Dời MillPopup_Root về đúng canvas");

            // SetParent với worldPositionStays mặc định giữ vị trí world — với UI full-screen
            // anchor 0-1 thì phải reset lại offset để nó ôm khít canvas mới.
            RectTransform rt = root.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            rep.Ok("ĐÃ DỜI '" + root.name + "' từ '" + tenCu + "' sang '" + canvas.name + "'. " +
                   "'" + tenCu + "' trở lại nguyên trạng chỉ chứa đồ của chính nó.");
            return root;
        }

        /// <summary>Lưu prefab root, giữ GUID nếu path đã tồn tại.</summary>
        private static void LuuPrefabRoot(GameObject root, MillReport rep)
        {
            MillUI.BaoDamThuMuc(PrefabFolder, rep);
            bool coSan = File.Exists(PopupPrefabPath);

            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, PopupPrefabPath, out bool ok);
            if (!ok || asset == null)
            {
                rep.Loi("Lưu prefab thất bại: " + PopupPrefabPath);
                return;
            }
            rep.Ok((coSan ? "Cập nhật (giữ GUID)" : "Tạo mới") + " prefab: " + PopupPrefabPath);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  MillDesign — MỌI CON SỐ ĐỌC TỪ full_mill_ui.html
    //
    //  Quy ước toạ độ: mọi hằng "left/top/right/bottom" là px CSS đo từ mép Ô CHỨA,
    //  y hệt CSS. Helper trong MillUI đổi sang anchoredPosition của Unity (đảo trục Y).
    //
    //  Tỉ lệ 1 px CSS = 1 unit UI. Canvas dự án 1920×1080 ⇒ popup 1000×680 chiếm 52%×63%
    //  màn hình, ĐÚNG cỡ các popup khác của dự án (ProfilePopupRedesignTool dùng board
    //  1000×640, ShopPopupRedesignTool dùng 1204×676). HTML không nói viewport lúc quay
    //  video nên đây là con số duy nhất tool phải TỰ QUYẾT — đổi `TiLeHienThi` là xong.
    // ═══════════════════════════════════════════════════════════════════════════════
    internal static class MillDesign
    {
        /// <summary>Nhân đều toàn bộ popup. 1 = đúng px CSS. HTML không quy định viewport.</summary>
        public const float TiLeHienThi = 1f;

        /// <summary>sortingOrder của Canvas con bọc popup — cao hơn popup khác của dự án.</summary>
        public const int SortOrder = 400;

        // ── Khung ngoài — HTML 44 `.popup-window` ────────────────────────────────────
        public const float PopupW = 1000f, PopupH = 680f;   // HTML 47-48
        public const float PopupPad = 30f;                  // HTML 58 padding
        public const float PopupRadius = 25f;               // HTML 55
        public const float RivetSize = 16f;                 // HTML 65 `.rivet`
        public const float RivetOff = 12f;                  // HTML 71-74 `.r-tl`…

        // ── Ruy băng — HTML 77 `.ribbon-container` ───────────────────────────────────
        public const float RibbonW = 480f, RibbonH = 90f;   // HTML 82-83
        public const float RibbonTop = -25f;                // HTML 79 top:-25px
        public const float RibbonFont = 32f;                // HTML 110 `.ribbon-text`

        // ── Nút X — HTML 118 `.btn-close` ────────────────────────────────────────────
        public const float CloseSize = 45f;                 // HTML 121
        public const float CloseOff = -10f;                 // HTML 120 top/right:-10px
        public const float CloseRadius = 12f;               // HTML 124

        // ── Panel trong — HTML 137 `.inner-panel` ────────────────────────────────────
        public const float PanelBorder = 3f;                // HTML 142
        public const float PanelPad = 15f;                  // HTML 143
        public const float PanelW = PopupW - PopupPad * 2f;             // 940
        public const float PanelH = PopupH - PopupPad * 2f;             // 620
        public const float ContentW = PanelW - PanelBorder * 2f - PanelPad * 2f;  // 904
        public const float Gap = 15f;                       // HTML 148 gap:15px

        // ── Hàng trên (CHỈ chip kim cương, ĐÃ BỎ TAB) — HTML 150 `.tabs-row` ─────────
        public const float TopRowH = 45f;                   // HTML 154 height:45px
        public const float ChipRadius = 20f;                // HTML 173 `.diamond-counter`
        public const float ChipPadX = 15f;                  // HTML 174
        public const float ChipGap = 8f;                    // HTML 178
        public const float ChipGemIcon = 18f;               // HTML 487 font-size:18px

        // ── Thân chính — HTML 184 `.main-content` ────────────────────────────────────
        public const float MainH = 490f;                    // HTML 188 height:490px
        public const float RecipeListW = 260f;              // HTML 193 width:260px
        public const float RightW = ContentW - RecipeListW - Gap;   // 629

        // ── Cột công thức — HTML 192 `.recipe-list-container` ────────────────────────
        public const float RlBorder = 3f;                   // HTML 195
        public const float RlPad = 10f;                     // HTML 199
        public const float RlInnerW = RecipeListW - RlBorder * 2f - RlPad * 2f;  // 234
        public const float RlInnerH = MainH - RlBorder * 2f - RlPad * 2f;        // 464
        public const float ListHeaderH = 22f;               // HTML 205 font-size:18 + line
        public const float ListHeaderMb = 10f;              // HTML 206 margin-bottom:10px
        public const float ListHeaderFont = 18f;            // HTML 205
        public const float BtnMainH = 43f;                  // HTML 285 `.btn-empty-slot` padding 12+12 + font 16
        public const float BtnMainMt = 10f;                 // HTML 286 margin-top:10px
        public const float BtnMainFont = 16f;               // HTML 291
        public const float BtnMainRadius = 12f;             // HTML 289
        public const float ScrollH = RlInnerH - ListHeaderH - ListHeaderMb - BtnMainMt - BtnMainH; // 379
        public const float ScrollPadRight = 5f;             // HTML 216 padding-right:5px
        public const float CardW = RlInnerW - ScrollPadRight;   // 229
        public const float CardGap = 10f;                   // HTML 214 gap:10px

        // ── Card công thức — HTML 219 `.recipe-card` ─────────────────────────────────
        // Cao = border 2*2 + padding 8*2 + icon 50 + margin-top cost 8 + chip 17 = 97 → 100.
        public const float CardH = 100f;
        public const float CardBorder = 2f;                 // HTML 220
        public const float CardPad = 8f;                    // HTML 222
        public const float CardRadius = 12f;                // HTML 221
        public const float CardIconCircle = 50f;            // HTML 233 `.recipe-icon-circle`
        public const float CardInfoGap = 10f;               // HTML 231
        public const float CardNameFont = 15f;              // HTML 239
        public const float CardTimeFont = 12f;              // HTML 240
        public const float CardTagTop = -8f;                // HTML 244 `.animal-tag` top:-8
        public const float CardTagRight = 8f;               // HTML 244
        public const float CardTagH = 16f;                  // HTML 246 padding 2+2 + font 10
        public const float CardTagFont = 10f;               // HTML 246
        public const float CardTagRadius = 10f;             // HTML 245
        public const float CardCostMt = 8f;                 // HTML 251 `.cost-row` margin-top
        public const float CardCostGap = 5f;                // HTML 251
        public const float CardChipH = 17f;                 // HTML 254 padding 2+2 + font 11
        public const float CardChipFont = 11f;              // HTML 254
        public const float CardChipRadius = 10f;            // HTML 253
        public const float CardLockTop = 25f;               // HTML 273 `.recipe-lock-overlay`
        public const float CardLockLeft = 15f;              // HTML 273
        public const float CardLockGlyph = 32f;             // HTML 278 `.big-lock` font-size
        public const float CardLockTextFont = 13f;          // HTML 281 `.lock-text`

        // ── Ô animation — HTML 307 `.animation-box` ──────────────────────────────────
        public const float AnimH = 250f;                    // HTML 308 height:250px
        public const float AnimBorder = 3f;                 // HTML 309
        public const float AnimRadius = 15f;                // HTML 310
        public const float SkyH = AnimH * 0.60f;            // HTML 317 height:60%  → 150
        public const float GroundH = AnimH * 0.40f;         // HTML 322 height:40%  → 100
        public const float GroundStripe = 30f;              // HTML 325 repeating 30px/60px

        // ── Badge trạng thái — HTML 327 `.status-badge` ──────────────────────────────
        public const float BadgeTop = 15f, BadgeLeft = 15f; // HTML 328
        public const float BadgePadX = 12f, BadgePadY = 6f; // HTML 331
        public const float BadgeFont = 14f;                 // HTML 331
        public const float BadgeRadius = 20f;               // HTML 330
        public const float BadgeGap = 8f;                   // HTML 332
        public const float DotSize = 12f;                   // HTML 333 `.status-dot`

        // ── Bong bóng nguyên liệu — HTML 336 `.input-bubble` ─────────────────────────
        public const float BubbleTop = 70f, BubbleLeft = 20f;   // HTML 337
        public const float BubblePadX = 20f, BubblePadY = 10f;  // HTML 339
        public const float BubbleFont = 18f;                    // HTML 340
        public const float BubbleRadius = 15f;                  // HTML 338
        public const float BubbleGap = 5f;                      // HTML 341
        public const float BubbleIcon = 20f;                    // suy từ font 18 (HTML không ghi)

        // ── Băng tải — HTML 345 `.conveyor-sys` ──────────────────────────────────────
        public const float BeltBottom = 40f, BeltLeft = 60f;    // HTML 346
        public const float BeltW = 380f, BeltH = 35f;           // HTML 347
        public const float BeltRadius = 15f;                    // HTML 349
        public const float BeltBorder = 2f;                     // HTML 350
        public const float WheelsBottom = 30f, WheelsLeft = 70f;// HTML 359 `.conveyor-wheels`
        public const float WheelsW = 360f;                      // HTML 360
        public const int   WheelCount = 4;                      // HTML 601 (4 div.wheel)
        public const float WheelSize = 14f;                     // HTML 363 `.wheel`
        public const float ItemBottom = 75f;                    // HTML 371 `.moving-item`
        public const float ItemLeft = 50f;                      // HTML 375-376 `.mi-1/.mi-2`
        public const float ItemFont = 24f;                      // HTML 372
        public const int   ItemCount = 2;                       // HTML 605-606 — ĐÚNG 2, không 3
        /// <summary>Chu kỳ hoa văn sọc chéo tính theo trục X = 42px (HTML 367 translateX(-42px)).</summary>
        public const int   BeltTileX = 42;

        // ── Máy xay — HTML 386 `.machine-wrapper` ────────────────────────────────────
        public const float MachineBottom = 35f, MachineRight = 140f;  // HTML 387
        public const float MachineSize = 180f;                        // HTML 388
        /// <summary>SVG máy dùng viewBox 200×200 (HTML 610) ⇒ quy đổi 180/200 = 0.9.</summary>
        public const float VbToMachine = MachineSize / 200f;
        // Bánh LỚN: tâm (65,125) r=45 trong viewBox 200 — HTML 620, 622.
        public const float GearLargeCx = 65f, GearLargeCy = 125f, GearLargeR = 45f;
        // Bánh NHỎ: tâm (145,135) r=30 — HTML 631, 632.
        public const float GearSmallCx = 145f, GearSmallCy = 135f, GearSmallR = 30f;
        // Sprite gear_large.svg: viewBox 100, bánh chiếm r=45 ⇒ 90/100. gear_small.svg: 60/70.
        public const float GearLargeSpriteRatio = 100f / 90f;
        public const float GearSmallSpriteRatio = 70f / 60f;

        // ── Bong bóng đầu ra — HTML 393 `.output-bubble` ─────────────────────────────
        public const float OutBottom = 50f, OutRight = 20f;      // HTML 394
        public const float OutSize = 80f;                        // HTML 395
        public const float OutBorder = 4f;                       // HTML 396
        public const float OutTagBottom = -12f;                  // HTML 401 `.output-tag`
        public const float OutTagH = 19f;                        // HTML 403 padding 2+2 + font 12
        public const float OutTagFont = 12f;                     // HTML 403
        public const float OutTagRadius = 10f;                   // HTML 402

        // ── Khu slot — HTML 407 `.slots-area` ────────────────────────────────────────
        public const float SlotsAreaH = MainH - AnimH - Gap;     // 225
        public const float SlotsHeaderH = 20f;                   // HTML 414 font-size:14 + line
        public const float SlotsHeaderFont = 14f;                // HTML 414
        public const float SlotsHeaderSpanFont = 13f;            // HTML 417 (span nhỏ hơn 1px)
        public const float SlotsHeaderGap = 10f;                 // HTML 411 gap:10px
        public const float SlotsContainerH = 180f;               // HTML 420 height:180px
        public const float SlotGap = 10f;                        // HTML 420 gap:10px
        public const int   SlotCount = 5;                        // HTML 653-698 (5 .slot-card)
        public static float SlotW => (RightW - SlotGap * (SlotCount - 1)) / SlotCount;  // 117.8

        // ── Card slot — HTML 422 `.slot-card` ────────────────────────────────────────
        public const float SlotBorder = 2f;                      // HTML 425
        public const float SlotRadius = 12f;                     // HTML 426
        public const float SlotPadV = 10f, SlotPadH = 5f;        // HTML 428 padding 10px 5px
        public const float SlotNumTop = 8f, SlotNumLeft = 8f;    // HTML 431 `.slot-num`
        public const float SlotNumFont = 12f;                    // HTML 431
        public const float SlotIconBg = 50f;                     // HTML 433 `.slot-icon-bg`
        public const float SlotIconMt = 15f;                     // HTML 436 margin-top
        public const float SlotIconMb = 10f;                     // HTML 436 margin-bottom
        public const float SlotNameFont = 13f;                   // HTML 438 `.slot-name`
        public const float SlotNameH = 30f;                      // 2 dòng font 13 line-height 1.1
        public const float SlotProgW = 0.90f;                    // HTML 441 width:90%
        public const float SlotProgH = 14f;                      // HTML 441
        public const float SlotProgRadius = 7f;                  // HTML 441
        public const float SlotProgMb = 8f;                      // HTML 441 margin-bottom
        public const float SlotTimerFont = 10f;                  // HTML 445 `.progress-time`
        public const float SlotBtnW = 0.90f;                      // HTML 448 width:90%
        public const float SlotBtnH = 31f;                        // HTML 449 padding 6+6 + font 14
        public const float SlotBtnFont = 14f;                     // HTML 449
        public const float SlotBtnRadius = 8f;                    // HTML 448
        public const float RedDotSize = 10f;                      // HTML 660 (inline style)
        public const float RedDotBottom = 35f, RedDotRight = 5f;  // HTML 660
        public const float SlotLockIcon = 40f;                    // HTML 459 `.lock-icon`
        public const float SlotLockMt = 25f;                      // HTML 459 margin-top:25px
        public const float SlotLockTextFont = 12f;                // HTML 460 `.locked-text`
        public const float SlotLockTextMt = 10f;                  // HTML 460 margin-top:10px
        public const float SlotLockedBtnW = 0.80f;                // HTML 461 width:80%
        public const float SlotLockedBtnH = 23f;                  // HTML 461 padding 4+4 + font 12
        public const float SlotLockedBtnRadius = 15f;             // HTML 461
        public const float SlotLockedBtnMb = 10f;                 // HTML 461 margin-bottom

        // ── Toast (HTML KHÔNG có — tool tự quyết, xem báo cáo) ───────────────────────
        public const float ToastW = 460f, ToastH = 56f;
        public const float ToastBottom = 110f;
        public const float ToastFont = 18f;
        public const float ToastRadius = 14f;

        // ── Bảng màu — HTML 10-30 `:root` ────────────────────────────────────────────
        public const string CWoodOuter   = "#C08C5D";
        public const string CWoodStripe  = "#B38052";
        public const string CWoodBorder  = "#8B5933";
        public const string CRibbonMain  = "#FDD157";
        public const string CRibbonEnd   = "#D9692A";
        public const string CInnerBg     = "#FFF6E5";
        public const string CTextBrown   = "#7D5133";
        public const string CTextLight   = "#A07455";
        public const string CSkyTop      = "#E6F3E6";
        public const string CSkyBottom   = "#CBE6CF";
        public const string CGroundMain  = "#B48D64";
        public const string CGroundStripe= "#A68058";
        public const string CMachineBody = "#A15234";
        public const string CFunnel      = "#8C472C";
        public const string CConveyor    = "#3F2C21";
        public const string CBtnGreen    = "#82C94F";
        public const string CBtnBlue     = "#40A4E5";
        public const string CLocked      = "#D9CDB9";
        // Màu chi tiết (không ở :root nhưng dùng trực tiếp trong CSS)
        public const string CPanelBorder = "#D6B798";   // HTML 142, 310
        public const string CRivet       = "#DDAE80";   // HTML 67
        public const string CBadgeBg     = "#F4E2C7";   // HTML 329
        public const string CBadgeBorder = "#C4A882";   // HTML 329
        public const string CDotGreen    = "#62E15D";   // HTML 333
        public const string CDotRing     = "#3DA239";   // HTML 333
        public const string CBubbleBg    = "#BA9054";   // HTML 337
        public const string CBubbleBorder= "#9A723D";   // HTML 337
        public const string CBeltBorder  = "#231812";   // HTML 350
        public const string CBeltStripe  = "#2A1D15";   // HTML 355
        public const string CWheel       = "#1C120C";   // HTML 363
        public const string CWheelRing   = "#4D3728";   // HTML 363
        public const string COutBg       = "#F8E6CA";   // HTML 396
        public const string COutBorder   = "#DFB980";   // HTML 396
        public const string CCardBorder  = "#E4D5C2";   // HTML 220
        public const string CCardIconBg  = "#EEDABB";   // HTML 234
        public const string CSlotIconBg  = "#F6E7D1";   // HTML 433
        public const string CSlotNum     = "#BDB09F";   // HTML 431
        public const string CProgTrack   = "#D9D9D9";   // HTML 441
        public const string CChipGreen   = "#82C94F";   // HTML 253
        public const string CChipGreenTx = "#5B9533";   // HTML 254
        public const string CCloseBg     = "#D45B4B";   // HTML 122
        public const string CBtnGrey     = "#B9B4AA";   // HTML 287
        public const string CBtnGreyLip  = "#9E9A91";   // HTML 294
        public const string CLockCircle  = "#645747";   // HTML 459
        public const string CLockedText  = "#726352";   // HTML 460
        public const string CLockedPill  = "#AFA28F";   // HTML 461
        public const string CSlotLockBg  = "#D9CDB9";   // HTML 458 (= --locked-bg)
        public const string CSlotLockBd  = "#C2B6A3";   // HTML 458
        public const string CRedDot      = "#FF4A4A";   // HTML 660
        public const string CBtnGreenLip = "#5E9B34";   // HTML 449 box-shadow
        public const string CBtnBlueLip  = "#287AB1";   // HTML 453 box-shadow
        public const string CItemGrain   = "#D9A85B";   // placeholder bó cỏ (HTML dùng emoji)
        public const string CItemGrainBd = "#A8763A";
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  MillReport — GOM BÁO CÁO. Không lệnh nào được báo "thành công" khi còn lỗi
    //  (cạm bẫy #4): `KetThuc()` chọn tiêu đề theo số lỗi và LogError khi > 0.
    // ═══════════════════════════════════════════════════════════════════════════════
    internal sealed class MillReport
    {
        private readonly string _tieuDe;
        private readonly List<string> _ok    = new List<string>();
        private readonly List<string> _canh  = new List<string>();
        private readonly List<string> _loi   = new List<string>();
        private readonly List<string> _can   = new List<string>();
        private readonly List<string> _wired = new List<string>();
        private readonly List<string> _chuaWire = new List<string>();
        private readonly List<string> _sprite = new List<string>();

        public MillReport(string tieuDe) { _tieuDe = tieuDe; }

        public int SoLoi => _loi.Count;

        public void Ok(string s)        { _ok.Add(s); }
        public void Canh(string s)      { _canh.Add(s); }
        public void Loi(string s)       { _loi.Add(s); }
        public void Can(string s)       { _can.Add(s); }
        public void Sprite(string s)    { _sprite.Add(s); }
        public void DaWire(string s)    { _wired.Add(s); }
        public void ChuaWire(string field, string liDo) { _chuaWire.Add(field + "  ← " + liDo); }

        /// <summary>In log chi tiết + hộp thoại tổng kết. Gọi ĐÚNG MỘT LẦN cuối mỗi lệnh.</summary>
        public void KetThuc()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(MillPopupBuilderTool.LOG + "═══ " + _tieuDe + " ═══");

            Khoi(sb, "SPRITE", _sprite);
            Khoi(sb, "ĐÃ WIRE (" + _wired.Count + ")", _wired);
            Khoi(sb, "CHƯA WIRE ĐƯỢC (" + _chuaWire.Count + ")", _chuaWire);
            Khoi(sb, "ĐÃ LÀM", _ok);
            Khoi(sb, "CẢNH BÁO", _canh);
            Khoi(sb, "LỖI", _loi);
            Khoi(sb, "CẦN BẠN LÀM", _can);

            string toanBo = sb.ToString();
            if (_loi.Count > 0) Debug.LogError(toanBo);
            else if (_canh.Count > 0 || _chuaWire.Count > 0) Debug.LogWarning(toanBo);
            else Debug.Log(toanBo);

            var hop = new System.Text.StringBuilder();
            hop.AppendLine(_loi.Count > 0
                ? "THẤT BẠI — có " + _loi.Count + " lỗi. Xem Console."
                : (_chuaWire.Count > 0 || _canh.Count > 0
                    ? "XONG NHƯNG CÓ VẤN ĐỀ — xem Console."
                    : "XONG."));
            hop.AppendLine();
            hop.AppendLine("Đã wire: " + _wired.Count + " field   |   Chưa wire: " + _chuaWire.Count);
            hop.AppendLine("Cảnh báo: " + _canh.Count + "   |   Lỗi: " + _loi.Count);

            if (_chuaWire.Count > 0)
            {
                hop.AppendLine();
                hop.AppendLine("CHƯA WIRE:");
                for (int i = 0; i < _chuaWire.Count && i < 8; i++) hop.AppendLine("  • " + _chuaWire[i]);
                if (_chuaWire.Count > 8) hop.AppendLine("  … còn " + (_chuaWire.Count - 8) + " dòng, xem Console.");
            }

            if (_loi.Count > 0)
            {
                hop.AppendLine();
                hop.AppendLine("LỖI:");
                for (int i = 0; i < _loi.Count && i < 5; i++) hop.AppendLine("  • " + _loi[i]);
            }

            if (_can.Count > 0)
            {
                hop.AppendLine();
                hop.AppendLine("CẦN BẠN LÀM:");
                for (int i = 0; i < _can.Count; i++) hop.AppendLine("  " + (i + 1) + ". " + _can[i]);
            }

            EditorUtility.DisplayDialog("[MILLUI] " + _tieuDe, hop.ToString(), "Đã hiểu");
        }

        private static void Khoi(System.Text.StringBuilder sb, string ten, List<string> ds)
        {
            if (ds.Count == 0) return;
            sb.AppendLine();
            sb.AppendLine("── " + ten + " ──");
            foreach (string s in ds) sb.AppendLine("  • " + s);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  MillSpriteFactory — TÌM SPRITE CÓ SẴN, THIẾU THÌ VẼ RA PNG THẬT
    //
    //  Thứ tự ưu tiên tra cứu (đúng yêu cầu lead):
    //    1. ui_mill_assets/generated_sprites      (bộ rasterize từ SVG của chính popup này)
    //    2. ui_svg_perfect/generated_sprites      (slot, progress, btn_close, circle_preview…)
    //    3. ui_shop_svg/generated_sprites         (toast, chip tiền, badge khoá)
    //    4. ui_building_svg/generated_sprites     (proc_* dự phòng)
    //    5. cả project (AssetDatabase.FindAssets)
    //    6. KHÔNG có ở đâu ⇒ VẼ theo đúng mã hex trong `MillDesign`, ghi ra PNG asset thật.
    //
    //  ⚠ Sprite generate BẮT BUỘC là PNG asset trên đĩa, KHÔNG dùng Sprite.Create in-memory:
    //    tham chiếu in-memory lưu vào prefab sẽ mất sạch khi đóng Editor.
    //  ⚠ KHÔNG bọc AssetDatabase.StartAssetEditing() quanh đoạn này (cạm bẫy #3) —
    //    import bị hoãn thì LoadAssetAtPath ngay sau đó trả null.
    // ═══════════════════════════════════════════════════════════════════════════════
    internal static class MillSpriteFactory
    {
        /// <summary>Thư mục PNG do tool vẽ ra.</summary>
        public const string GenFolder = "Assets/_Game/GeneratedUI/Mill";

        private static readonly string[] ThuMucUuTien =
        {
            "Assets/Assetsgame/popup/ui_mill_assets/generated_sprites",
            "Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites",
            "Assets/Assetsgame/popup/ui_shop_svg/generated_sprites",
            "Assets/Assetsgame/popup/ui_building_svg/generated_sprites",
        };

        /// <summary>Siêu lấy mẫu khi vẽ: texture = kích thước UI × 2, cho mép cong mượt.</summary>
        private const int SS = 2;

        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();
        private static MillReport _rep;

        /// <summary>Xoá cache phiên (gọi đầu mỗi lệnh).</summary>
        public static void Reset() { _cache.Clear(); _rep = null; }

        /// <summary>Gắn báo cáo để mọi lần tra cứu/vẽ đều được ghi lại.</summary>
        public static void GanBaoCao(MillReport rep) { _rep = rep; }

        // ── MÀU ──────────────────────────────────────────────────────────────────────

        /// <summary>Parse hex CSS "#A15234". Sai cú pháp → magenta để lộ ra ngay.</summary>
        public static Color Hex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.magenta;
            if (!hex.StartsWith("#")) hex = "#" + hex;
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
        }

        /// <summary>Hex kèm alpha riêng.</summary>
        public static Color Hex(string hex, float a) { Color c = Hex(hex); c.a = a; return c; }

        // ── TRA CỨU SPRITE CÓ SẴN ────────────────────────────────────────────────────

        /// <summary>
        /// Tìm sprite theo danh sách tên ứng viên, dừng ở tên đầu tiên tìm được.
        /// Trả null nếu không có ở đâu — nơi gọi phải tự vẽ thay thế.
        /// </summary>
        public static Sprite Tim(params string[] ten)
        {
            if (ten == null) return null;
            foreach (string n in ten)
            {
                if (string.IsNullOrEmpty(n)) continue;

                foreach (string folder in ThuMucUuTien)
                {
                    string p = folder + "/" + n + ".png";
                    Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                    if (s != null)
                    {
                        Ghi("TÌM ĐƯỢC  " + n + "  ← " + folder);
                        return s;
                    }
                }

                // Tìm rộng cả project — chỉ nhận khi TÊN FILE trùng khít.
                foreach (string guid in AssetDatabase.FindAssets(n + " t:Sprite"))
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.Equals(Path.GetFileNameWithoutExtension(p), n,
                                       StringComparison.OrdinalIgnoreCase)) continue;
                    Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                    if (s != null) { Ghi("TÌM ĐƯỢC  " + n + "  ← " + p + " (tìm rộng)"); return s; }
                }
            }
            return null;
        }

        /// <summary>Có sprite tên này trong project hay không — dùng cho lệnh 3 (chạy khô).</summary>
        public static string TimDuongDan(string ten)
        {
            foreach (string folder in ThuMucUuTien)
            {
                string p = folder + "/" + ten + ".png";
                if (AssetDatabase.LoadAssetAtPath<Sprite>(p) != null) return p;
            }
            foreach (string guid in AssetDatabase.FindAssets(ten + " t:Sprite"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(Path.GetFileNameWithoutExtension(p), ten,
                                  StringComparison.OrdinalIgnoreCase)) return p;
            }
            return null;
        }

        /// <summary>
        /// Áp lại import settings + spriteBorder 9-slice cho một sprite CÓ SẴN và TRẢ VỀ
        /// sprite ĐÃ NẠP LẠI.
        ///
        /// ⚠ VÌ SAO PHẢI TRẢ VỀ chứ không sửa tại chỗ: <c>SaveAndReimport()</c> huỷ và tạo
        /// lại đối tượng Sprite. Tham chiếu lấy TRƯỚC lời gọi đó trở thành object đã chết
        /// (hoặc còn `border` cũ = 0) ⇒ nơi gọi sẽ thấy sprite "không có border" và đặt
        /// Image.Type = Simple, làm khung/nút bị KÉO GIÃN méo góc mà không báo lỗi.
        ///
        /// <paramref name="vbW"/> là bề rộng viewBox của SVG gốc; slice truyền theo ĐƠN VỊ
        /// THIẾT KẾ (px CSS/SVG) rồi tự quy đổi sang texel theo tỉ lệ rasterize thật.
        /// </summary>
        public static Sprite ApSlice(Sprite sp, float vbW, float sl, float sb, float sr, float st)
        {
            if (sp == null) return null;
            string path = AssetDatabase.GetAssetPath(sp);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return sp;

            float texW = sp.texture != null ? sp.texture.width : vbW;
            float k = vbW > 0f ? texW / vbW : 1f;
            var border = new Vector4(Mathf.Round(sl * k), Mathf.Round(sb * k),
                                     Mathf.Round(sr * k), Mathf.Round(st * k));
            string ten = sp.name;

            if (ApDatImport(imp, border, TextureImporterType.Sprite, TextureWrapMode.Clamp))
                Ghi("ĐẶT 9-SLICE  " + ten + "  border=" + border + " (tỉ lệ raster ×" +
                    k.ToString("0.###", CultureInfo.InvariantCulture) + ")");

            // Nạp lại SAU khi reimport — bắt buộc, xem ghi chú ở đầu hàm.
            Sprite moi = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (moi == null)
            {
                Ghi("LỖI: sau khi đặt 9-slice không nạp lại được sprite tại " + path);
                return sp;
            }
            return moi;
        }

        // ── FONT ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Font TMP cho toàn popup. HTML dùng Nunito, nhưng chữ hiển thị là TIẾNG VIỆT có
        /// dấu ⇒ ƯU TIÊN font Việt của dự án (FontVo, xem BuildingProcessUIBuilderTool
        /// dòng 22) rồi mới tới Nunito/Baloo. Font thiếu dấu là lỗi nhìn thấy ngay.
        /// </summary>
        public static TMP_FontAsset TimFont(MillReport rep)
        {
            string[] uuTien = { "fontvo", "nunito", "baloo" };
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");

            TMP_FontAsset dauTien = null;
            var tatCa = new List<TMP_FontAsset>();
            foreach (string g in guids)
            {
                var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g));
                if (f == null) continue;
                tatCa.Add(f);
                if (dauTien == null) dauTien = f;
            }

            foreach (string key in uuTien)
                foreach (TMP_FontAsset f in tatCa)
                    if (f.name.ToLowerInvariant().Contains(key))
                    {
                        rep.Ok("Font: '" + f.name + "' (khớp ưu tiên '" + key + "').");
                        return f;
                    }

            if (dauTien != null)
            {
                rep.Canh("Không thấy font FontVo/Nunito/Baloo — dùng '" + dauTien.name +
                         "'. Kiểm tra font có đủ dấu tiếng Việt.");
                return dauTien;
            }

            rep.Canh("Project chưa có TMP_FontAsset nào ⇒ text dùng font mặc định của TMP, " +
                     "rất dễ mất dấu tiếng Việt.");
            return null;
        }

        // ── VẼ: KHỐI CHỮ NHẬT BO GÓC ────────────────────────────────────────────────

        /// <summary>Mô tả một khối chữ nhật bo góc để vẽ ra PNG 9-slice.</summary>
        public struct Khoi
        {
            public int w, h;                       // kích thước UI (px CSS)
            public float rTL, rTR, rBR, rBL;       // bo góc từng góc
            public float border;                   // độ dày viền
            public Color fillTop, fillBottom;      // gradient dọc (bằng nhau = màu đặc)
            public Color borderColor;
            public float stripePeriod;             // >0: sọc dọc chu kỳ 2×period (CSS repeating)
            public Color stripeColor;
            public float lipH;                     // dải đáy đậm (nút 3D). 0 = không có
            public Color lipColor;
            public bool  khongSlice;               // true: vẽ đúng kích thước, không 9-slice
        }

        /// <summary>Khối màu đặc bo 4 góc đều + viền.</summary>
        public static Khoi K(int w, int h, float r, float border, Color fill, Color borderColor)
        {
            return new Khoi
            {
                w = w, h = h, rTL = r, rTR = r, rBR = r, rBL = r,
                border = border, fillTop = fill, fillBottom = fill, borderColor = borderColor
            };
        }

        /// <summary>Vẽ (hoặc lấy lại) sprite từ mô tả khối. Idempotent theo <paramref name="id"/>.</summary>
        public static Sprite VeKhoi(string id, Khoi k)
        {
            if (_cache.TryGetValue(id, out Sprite san) && san != null) return san;

            int tw = Mathf.Max(4, k.w * SS);
            int th = Mathf.Max(4, k.h * SS);

            float rMax = Mathf.Max(Mathf.Max(k.rTL, k.rTR), Mathf.Max(k.rBR, k.rBL));
            Vector4 border = Vector4.zero;
            if (!k.khongSlice)
            {
                float sl = (rMax + k.border) * SS;
                float sb = (rMax + k.border + k.lipH) * SS;
                border = new Vector4(Mathf.Round(sl), Mathf.Round(sb), Mathf.Round(sl), Mathf.Round(sl));
                // 9-slice không được chiếm quá kích thước texture, nếu không Unity kẹp lại và méo.
                border.x = Mathf.Min(border.x, tw / 2f - 1f);
                border.z = Mathf.Min(border.z, tw / 2f - 1f);
                border.y = Mathf.Min(border.y, th / 2f - 1f);
                border.w = Mathf.Min(border.w, th / 2f - 1f);
            }

            Sprite sp = Ve(id, tw, th, border, TextureImporterType.Sprite, TextureWrapMode.Clamp,
                (x, y) => PixelKhoi(k, (x + 0.5f) / SS, (y + 0.5f) / SS));
            if (sp != null) _cache[id] = sp;
            return sp;
        }

        /// <summary>Một pixel của khối. (px,py) tính theo px CSS, gốc góc TRÁI-DƯỚI texture.</summary>
        private static Color PixelKhoi(Khoi k, float px, float py)
        {
            // Đổi sang hệ tâm, +y lên.
            var p = new Vector2(px - k.w * 0.5f, py - k.h * 0.5f);
            var half = new Vector2(k.w * 0.5f, k.h * 0.5f);

            float sd = SdBoGocRieng(p, half, k.rTL, k.rTR, k.rBR, k.rBL);
            float aOut = Mathf.Clamp01(0.5f - sd);          // 1px khử răng cưa
            if (aOut <= 0.002f) return Color.clear;

            // t: 0 ở mép TRÊN → 1 ở mép DƯỚI (đúng chiều gradient của CSS).
            float t = Mathf.Clamp01(1f - py / Mathf.Max(1f, k.h));
            Color fill = Color.Lerp(k.fillTop, k.fillBottom, t);

            // Sọc dọc: CSS repeating-linear-gradient(90deg, transparent 0→P, stripe P→2P).
            if (k.stripePeriod > 0f)
            {
                int o = Mathf.FloorToInt(px / k.stripePeriod);
                if (((o % 2) + 2) % 2 == 1) fill = k.stripeColor;
            }

            // Dải đáy đậm của nút 3D (CSS box-shadow 0 4px 0 <màu>).
            if (k.lipH > 0f && py < k.lipH) fill = k.lipColor;

            if (k.border <= 0f) { fill.a *= aOut; return fill; }

            float aIn = Mathf.Clamp01(0.5f - (sd + k.border));
            Color c;
            if (fill.a <= 0.002f)
            {
                // Khối "chỉ có viền" (vd khung ô animation: lòng trong suốt).
                c = k.borderColor;
                c.a *= aOut * (1f - aIn);
            }
            else
            {
                c = Color.Lerp(k.borderColor, fill, aIn);
                c.a *= aOut;
            }
            return c;
        }

        /// <summary>SDF chữ nhật bo góc, bán kính KHÁC NHAU từng góc.</summary>
        private static float SdBoGocRieng(Vector2 p, Vector2 b,
                                          float rTL, float rTR, float rBR, float rBL)
        {
            float r = p.x > 0f ? (p.y > 0f ? rTR : rBR) : (p.y > 0f ? rTL : rBL);
            r = Mathf.Min(r, Mathf.Min(b.x, b.y));
            var q = new Vector2(Mathf.Abs(p.x) - b.x + r, Mathf.Abs(p.y) - b.y + r);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        // ── VẼ: ĐĨA TRÒN ────────────────────────────────────────────────────────────

        /// <summary>Đĩa tròn màu đặc + vành viền. Không 9-slice.</summary>
        public static Sprite VeDia(string id, int size, Color fill, float ring, Color ringColor)
        {
            if (_cache.TryGetValue(id, out Sprite san) && san != null) return san;

            int n = Mathf.Max(8, size * SS);
            float R = n * 0.5f - 1f;
            float ringPx = ring * SS;

            Sprite sp = Ve(id, n, n, Vector4.zero, TextureImporterType.Sprite,
                TextureWrapMode.Clamp, (x, y) =>
                {
                    float dx = x + 0.5f - n * 0.5f, dy = y + 0.5f - n * 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float aOut = Mathf.Clamp01(R - d + 0.5f);
                    if (aOut <= 0.002f) return Color.clear;
                    if (ringPx <= 0f) { Color c0 = fill; c0.a *= aOut; return c0; }
                    float aIn = Mathf.Clamp01(R - ringPx - d + 0.5f);
                    Color c = Color.Lerp(ringColor, fill, aIn);
                    c.a *= aOut;
                    return c;
                });
            if (sp != null) _cache[id] = sp;
            return sp;
        }

        // ── VẼ: TEXTURE SỌC CHÉO CHO BĂNG TẢI (WRAP = REPEAT) ───────────────────────

        /// <summary>
        /// Texture sọc chéo −45° cho <see cref="UIScrollingTexture"/>.
        ///
        /// HTML 355: repeating-linear-gradient(-45deg, transparent 0→15px, #2A1D15 15px→30px)
        /// ⇒ chu kỳ 30px ĐO VUÔNG GÓC với sọc. Với sọc 45°, chu kỳ theo trục X là 30·√2 ≈ 42.43.
        /// HTML 367 lại cho biết một vòng animation trôi ĐÚNG 42px trong 1s ⇒ tác giả thiết kế
        /// coi chu kỳ ngang là 42px. Nên tile 42×42 với hoa văn theo (x+y) mod 42:
        ///   • tile liền mạch tuyệt đối khi Wrap = Repeat  (p(x+42,y) ≡ p(x,y));
        ///   • bề rộng sọc vuông góc = 42/√2 = 29.7px ≈ 30px, khớp CSS;
        ///   • texture rộng 42 ⇒ `UIScrollingTexture` để `dungChuKyHoaVan = FALSE` là ĐÚNG
        ///     (nó lấy tex.width làm px/vòng UV: 42px/s ÷ 42 = 1 vòng/s, khớp animation 1s).
        ///   ⚠ ĐỪNG bật `dungChuKyHoaVan` — nó sẽ lấy 30 và băng tải chạy nhanh hơn 1.4 lần.
        /// </summary>
        public static Texture2D VeTextureBangTai(string id)
        {
            int n = MillDesign.BeltTileX;                 // 42
            Color stripe = Hex(MillDesign.CBeltStripe);

            string path = GenFolder + "/" + id + ".png";
            bool ok = VeRaFile(path, n, n, (x, y) =>
            {
                float s = (((x + y) % n) + n) % n;         // 0..41, liền mạch qua biên tile
                float nua = n * 0.5f;                      // 21
                // 1px mềm hai mép sọc cho khỏi răng cưa khi cuộn.
                float a = Mathf.Clamp01(s - nua + 1f) * Mathf.Clamp01(n - s);
                if (s < nua) a = 0f;
                Color c = stripe;
                c.a *= Mathf.Clamp01(a);
                return c;
            });
            if (!ok) return null;

            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
                ApDatImport(imp, Vector4.zero, TextureImporterType.Default, TextureWrapMode.Repeat);

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) Ghi("LỖI: không load được texture băng tải tại " + path);
            else Ghi("VẼ (Texture, Wrap=Repeat)  " + id + "  " + n + "×" + n +
                     "  sọc " + MillDesign.CBeltStripe);
            return tex;
        }

        // ── VẼ: KÝ HIỆU (ổ khoá / dấu X / kim cương / bó cỏ) ────────────────────────

        /// <summary>Ổ khoá MÀU TRẮNG trên nền trong suốt — tô màu bằng Image.color.</summary>
        public static Sprite VeOKhoa(string id, int size)
        {
            if (_cache.TryGetValue(id, out Sprite san) && san != null) return san;
            int n = Mathf.Max(16, size * SS);
            Sprite sp = Ve(id, n, n, Vector4.zero, TextureImporterType.Sprite,
                TextureWrapMode.Clamp, (x, y) =>
                {
                    float u = (x + 0.5f) / n, v = (y + 0.5f) / n;      // 0..1, v=0 đáy
                    // Thân: chữ nhật bo góc chiếm 62% ngang, từ v 0.10 → 0.58
                    var p = new Vector2((u - 0.5f) * n, (v - 0.34f) * n);
                    float sdThan = SdBoGocRieng(p, new Vector2(0.31f * n, 0.24f * n),
                                                0.07f * n, 0.07f * n, 0.07f * n, 0.07f * n);
                    float a = Mathf.Clamp01(0.5f - sdThan);
                    // Càng khoá: nửa trên vành khuyên tâm (0.5, 0.60)
                    float dx = u - 0.5f, dy = v - 0.60f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * n;
                    float rNgoai = 0.24f * n, rTrong = 0.145f * n;
                    if (dy > -0.02f)
                    {
                        float aC = Mathf.Clamp01(rNgoai - d + 0.5f) * Mathf.Clamp01(d - rTrong + 0.5f);
                        a = Mathf.Max(a, aC);
                    }
                    return new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                });
            if (sp != null) _cache[id] = sp;
            return sp;
        }

        /// <summary>Dấu X trắng (nút đóng). HTML 126 dùng ký tự ✖ — vẽ lại để không phụ thuộc font.</summary>
        public static Sprite VeDauX(string id, int size)
        {
            if (_cache.TryGetValue(id, out Sprite san) && san != null) return san;
            int n = Mathf.Max(12, size * SS);
            float day = n * 0.14f;
            Sprite sp = Ve(id, n, n, Vector4.zero, TextureImporterType.Sprite,
                TextureWrapMode.Clamp, (x, y) =>
                {
                    float u = x + 0.5f - n * 0.5f, v = y + 0.5f - n * 0.5f;
                    float lim = n * 0.34f;
                    float d1 = Mathf.Abs(u - v) * 0.70710678f;
                    float d2 = Mathf.Abs(u + v) * 0.70710678f;
                    float trong = Mathf.Clamp01(lim - Mathf.Max(Mathf.Abs(u), Mathf.Abs(v)) + 0.5f);
                    float a1 = Mathf.Clamp01(day * 0.5f - d1 + 0.5f);
                    float a2 = Mathf.Clamp01(day * 0.5f - d2 + 0.5f);
                    return new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(a1, a2)) * trong);
                });
            if (sp != null) _cache[id] = sp;
            return sp;
        }

        /// <summary>Viên kim cương (hình thoi) — dự phòng khi project không có icon kim cương.</summary>
        public static Sprite VeKimCuong(string id, int size, Color mau)
        {
            if (_cache.TryGetValue(id, out Sprite san) && san != null) return san;
            int n = Mathf.Max(12, size * SS);
            Sprite sp = Ve(id, n, n, Vector4.zero, TextureImporterType.Sprite,
                TextureWrapMode.Clamp, (x, y) =>
                {
                    float u = (x + 0.5f) / n * 2f - 1f;
                    float v = (y + 0.5f) / n * 2f - 1f;
                    // Hình thoi hơi dẹt phía trên: |u|/0.86 + |v|/0.94 <= 1
                    float f = Mathf.Abs(u) / 0.86f + Mathf.Abs(v) / 0.94f;
                    float a = Mathf.Clamp01((1f - f) * n * 0.35f);
                    if (a <= 0.002f) return Color.clear;
                    // Mặt trên sáng hơn cho có khối.
                    Color c = v > 0.18f ? Color.Lerp(mau, Color.white, 0.35f) : mau;
                    c.a *= a;
                    return c;
                });
            if (sp != null) _cache[id] = sp;
            return sp;
        }

        // ── HẠ TẦNG GHI PNG ─────────────────────────────────────────────────────────

        private delegate Color PixelFn(int x, int y);

        private static Sprite Ve(string id, int w, int h, Vector4 border,
                                 TextureImporterType type, TextureWrapMode wrap, PixelFn fn)
        {
            string path = GenFolder + "/" + id + ".png";
            if (!VeRaFile(path, w, h, fn)) return null;

            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null) ApDatImport(imp, border, type, wrap);

            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null) Ghi("LỖI: import sprite thất bại tại " + path);
            else Ghi("VẼ  " + id + "  " + w + "×" + h + (border == Vector4.zero
                     ? "" : "  9-slice=" + border));
            return sp;
        }

        /// <summary>
        /// Vẽ pixel → ghi PNG (chỉ ghi khi BYTE khác file cũ, để không làm git rác) → import
        /// ĐỒNG BỘ. Không dùng StartAssetEditing (cạm bẫy #3).
        /// </summary>
        private static bool VeRaFile(string path, int w, int h, PixelFn fn)
        {
            BaoDamThuMucGen();

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = fn(x, y);
            tex.SetPixels(px);
            tex.Apply();
            byte[] moi = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string abs = Path.Combine(Directory.GetCurrentDirectory(), path);
            bool giongCu = false;
            if (File.Exists(abs))
            {
                byte[] cu = File.ReadAllBytes(abs);
                giongCu = cu.Length == moi.Length;
                if (giongCu)
                    for (int i = 0; i < cu.Length; i++)
                        if (cu[i] != moi[i]) { giongCu = false; break; }
            }

            if (!giongCu)
            {
                try { File.WriteAllBytes(abs, moi); }
                catch (Exception e) { Ghi("LỖI: không ghi được " + path + " — " + e.Message); return false; }
                AssetDatabase.ImportAsset(path,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }
            return true;
        }

        /// <summary>Áp import settings; trả true nếu có thay đổi và đã reimport.</summary>
        private static bool ApDatImport(TextureImporter imp, Vector4 border,
                                        TextureImporterType type, TextureWrapMode wrap)
        {
            bool doi = imp.textureType != type
                    || imp.spriteBorder != border
                    || imp.wrapMode != wrap
                    || imp.filterMode != FilterMode.Bilinear
                    || !imp.alphaIsTransparency
                    || imp.mipmapEnabled
                    || !Mathf.Approximately(imp.spritePixelsPerUnit, 100f)
                    || imp.textureCompression != TextureImporterCompression.Uncompressed;

            imp.textureType        = type;
            imp.spriteImportMode   = type == TextureImporterType.Sprite
                                     ? SpriteImportMode.Single : SpriteImportMode.None;
            imp.spritePixelsPerUnit= 100f;                       // yêu cầu: PPU 100
            imp.alphaIsTransparency= true;
            imp.mipmapEnabled      = false;
            imp.filterMode         = FilterMode.Bilinear;        // yêu cầu: Bilinear
            imp.wrapMode           = wrap;
            imp.textureCompression = TextureImporterCompression.Uncompressed;  // yêu cầu: None
            imp.spriteBorder       = border;

            // FullRect: 9-slice cần mesh chữ nhật đầy; Tight cắt mất vùng giãn.
            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            if (type == TextureImporterType.Sprite && st.spriteMeshType != SpriteMeshType.FullRect)
            {
                st.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(st);
                doi = true;
            }

            if (doi) imp.SaveAndReimport();
            return doi;
        }

        private static void BaoDamThuMucGen()
        {
            string abs = Path.Combine(Directory.GetCurrentDirectory(), GenFolder);
            if (Directory.Exists(abs)) return;
            Directory.CreateDirectory(abs);
            AssetDatabase.Refresh();
        }

        private static void Ghi(string s) { if (_rep != null) _rep.Sprite(s); }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  MillUI — ĐỒ NGHỀ DỰNG UGUI, đặt node theo ĐÚNG CÚ PHÁP CSS
    //
    //  Mọi hàm đặt vị trí nhận tham số y như CSS (left/top/right/bottom đo từ mép ô chứa,
    //  trục Y hướng XUỐNG) rồi tự đổi sang anchor + anchoredPosition của Unity. Nhờ vậy
    //  code dựng đọc gần như 1-1 với file HTML, ai soát cũng đối chiếu được từng dòng.
    // ═══════════════════════════════════════════════════════════════════════════════
    internal static class MillUI
    {
        /// <summary>Font TMP dùng cho mọi text của popup. Lệnh 1 gán trước khi dựng.</summary>
        public static TMP_FontAsset Font;

        /// <summary>
        /// false khi đang dựng object TẠM để lưu prefab: các node đó bị DestroyImmediate ngay
        /// sau khi lưu nên KHÔNG được đưa vào Undo stack (undo sẽ "hồi sinh" object đã chết).
        /// </summary>
        public static bool DungUndo = true;

        // ── COMPONENT ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Lấy component, chưa có thì thêm.
        /// ⚠ CẠM BẪY #1: KHÔNG viết <c>GetComponent&lt;T&gt;() ?? AddComponent&lt;T&gt;()</c>.
        /// Component thiếu trả về tham chiếu "fake-null": KHÁC null theo phép so của C#
        /// (mà <c>??</c> dùng đúng phép so đó) nhưng == null theo toán tử Unity nạp chồng.
        /// Kết quả: <c>??</c> tưởng đã có nên không thêm, dòng sau nổ MissingComponentException.
        /// Luôn so tường minh <c>== null</c> như dưới đây.
        /// </summary>
        public static T Comp<T>(GameObject go) where T : Component
        {
            if (go == null) return null;
            T c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        /// <summary>Xoá toàn bộ con của một transform (an toàn với Undo).</summary>
        public static void XoaHetCon(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(t.GetChild(i).gameObject);
        }

        /// <summary>Tạo thư mục asset (kể cả nhiều cấp) nếu chưa có.</summary>
        public static void BaoDamThuMuc(string folder, MillReport rep)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string abs = Path.Combine(Directory.GetCurrentDirectory(), folder);
            try
            {
                Directory.CreateDirectory(abs);
                AssetDatabase.Refresh();
                rep.Ok("Tạo thư mục " + folder);
            }
            catch (Exception e) { rep.Loi("Không tạo được thư mục " + folder + " — " + e.Message); }
        }

        // ── TẠO NODE ─────────────────────────────────────────────────────────────────

        /// <summary>Node RectTransform rỗng. <paramref name="parent"/> PHẢI thuộc scene.</summary>
        public static RectTransform Node(Transform parent, string ten)
        {
            var go = new GameObject(ten, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);          // parent LUÔN thuộc scene — xem cạm bẫy #2
            rt.localScale = Vector3.one;
            if (DungUndo) Undo.RegisterCreatedObjectUndo(go, "Tạo " + ten);
            return rt;
        }

        // ── ĐẶT VỊ TRÍ THEO CÚ PHÁP CSS ─────────────────────────────────────────────

        /// <summary>position:absolute; left:L; top:T — neo góc TRÁI-TRÊN của ô chứa.</summary>
        public static RectTransform TL(RectTransform rt, float w, float h, float left, float top)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(left, -top);
            return rt;
        }

        /// <summary>position:absolute; right:R; top:T.</summary>
        public static RectTransform TR(RectTransform rt, float w, float h, float right, float top)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>position:absolute; left:L; bottom:B.</summary>
        public static RectTransform BL(RectTransform rt, float w, float h, float left, float bottom)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(left, bottom);
            return rt;
        }

        /// <summary>position:absolute; right:R; bottom:B.</summary>
        public static RectTransform BR(RectTransform rt, float w, float h, float right, float bottom)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-right, bottom);
            return rt;
        }

        /// <summary>Căn giữa ngang, cách mép TRÊN <paramref name="top"/> px.</summary>
        public static RectTransform TC(RectTransform rt, float w, float h, float top)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0f, -top);
            return rt;
        }

        /// <summary>Căn giữa ngang, cách mép DƯỚI <paramref name="bottom"/> px.</summary>
        public static RectTransform BC(RectTransform rt, float w, float h, float bottom)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0f, bottom);
            return rt;
        }

        /// <summary>Căn giữa hoàn toàn, lệch (dx,dy) — dy DƯƠNG là đi LÊN (hệ Unity).</summary>
        public static RectTransform CC(RectTransform rt, float w, float h, float dx, float dy)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(dx, dy);
            return rt;
        }

        /// <summary>Giãn kín ô chứa, thụt vào theo từng mép (đơn vị px, giống CSS inset).</summary>
        public static RectTransform Stretch(RectTransform rt, float l, float t, float r, float b)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        // ── ẢNH ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Image mới. Tự chọn Sliced khi sprite có spriteBorder ≠ 0 (nếu không Unity cảnh báo
        /// "Sliced but has no border"), Simple khi không có.
        /// </summary>
        public static Image Img(RectTransform rt, Sprite sp, Color mau, bool raycast = false)
        {
            var img = Comp<Image>(rt.gameObject);
            img.sprite = sp;
            img.color = mau;
            img.raycastTarget = raycast;
            img.type = (sp != null && sp.border.sqrMagnitude > 0.001f)
                       ? Image.Type.Sliced : Image.Type.Simple;
            return img;
        }

        /// <summary>Image con nhanh: tạo node + đặt sprite.</summary>
        public static Image ImgNode(Transform parent, string ten, Sprite sp, Color mau)
        {
            return Img(Node(parent, ten), sp, mau);
        }

        // ── CHỮ ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// TMP_Text mới. HTML dùng Nunito weight 800/900 ⇒ luôn bật Bold.
        /// Tắt auto-size để chữ đúng cỡ px như bản thiết kế, wrap tuỳ chọn.
        /// </summary>
        public static TextMeshProUGUI Txt(RectTransform rt, string noiDung, float coChu,
                                          Color mau, TextAlignmentOptions canLe, bool wrap = false)
        {
            var t = Comp<TextMeshProUGUI>(rt.gameObject);
            if (Font != null) t.font = Font;
            t.text = noiDung;
            t.fontSize = coChu;
            t.color = mau;
            t.alignment = canLe;
            t.fontStyle = FontStyles.Bold;               // xấp xỉ font-weight 800/900
            t.enableAutoSizing = false;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.margin = Vector4.zero;
            return t;
        }

        /// <summary>Text con nhanh, giãn kín ô chứa.</summary>
        public static TextMeshProUGUI TxtStretch(Transform parent, string ten, string noiDung,
            float coChu, Color mau, TextAlignmentOptions canLe, bool wrap = false)
        {
            RectTransform rt = Stretch(Node(parent, ten), 0f, 0f, 0f, 0f);
            return Txt(rt, noiDung, coChu, mau, canLe, wrap);
        }

        // ── NÚT ─────────────────────────────────────────────────────────────────────

        /// <summary>Button trên một Image có sẵn, xoá listener cũ để chạy lại tool không cộng dồn.</summary>
        public static Button Btn(Image nen)
        {
            var b = Comp<Button>(nen.gameObject);
            b.targetGraphic = nen;
            nen.raycastTarget = true;
            b.transition = Selectable.Transition.ColorTint;
            var cb = b.colors;
            cb.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            cb.disabledColor = new Color(1f, 1f, 1f, 0.55f);
            cb.fadeDuration = 0.06f;
            b.colors = cb;
            b.onClick = new Button.ButtonClickedEvent();   // xoá sạch listener persistent cũ
            return b;
        }

        // ── LAYOUT (dùng cho chỗ CSS là flexbox tự giãn) ─────────────────────────────

        /// <summary>
        /// HorizontalLayoutGroup + ContentSizeFitter — tương đương
        /// <c>display:flex; align-items:center; gap:G</c> có bề rộng tự co theo nội dung.
        /// </summary>
        public static HorizontalLayoutGroup HangNgang(RectTransform rt, float gap,
            float padL, float padR, float padT, float padB, bool tuCoBeRong)
        {
            var h = Comp<HorizontalLayoutGroup>(rt.gameObject);
            h.spacing = gap;
            h.padding = new RectOffset((int)padL, (int)padR, (int)padT, (int)padB);
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            h.childScaleWidth = false;
            h.childScaleHeight = false;

            if (tuCoBeRong)
            {
                var f = Comp<ContentSizeFitter>(rt.gameObject);
                f.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                f.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
            return h;
        }

        /// <summary>Đặt kích thước cố định cho một phần tử nằm trong LayoutGroup.</summary>
        public static LayoutElement CoDinh(RectTransform rt, float w, float h)
        {
            var le = Comp<LayoutElement>(rt.gameObject);
            le.preferredWidth = w;
            le.preferredHeight = h;
            le.minWidth = w;
            le.minHeight = h;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;
            return le;
        }

        /// <summary>Bề rộng ưu tiên tự tính từ text (cho chip/badge co theo chữ).</summary>
        public static LayoutElement CoDinhCao(RectTransform rt, float h)
        {
            var le = Comp<LayoutElement>(rt.gameObject);
            le.preferredHeight = h;
            le.minHeight = h;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;
            return le;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  MillSkin — GOM TOÀN BỘ SPRITE POPUP CẦN (tìm được thì dùng, không thì vẽ)
    //
    //  Bảng đối chiếu node → sprite → dòng HTML nằm ngay tại từng dòng gán dưới đây.
    //  Sprite `tab_active` / `tab_inactive` CỐ Ý bỏ — dự án đã bỏ hệ tab.
    // ═══════════════════════════════════════════════════════════════════════════════
    internal sealed class MillSkin
    {
        // Có sẵn trong ui_mill_assets/generated_sprites
        public Sprite popupBoard, panelInner, ribbon, machineBody, gearLarge, gearSmall;
        public Sprite cardActive, cardInactive, cardLocked, btnGreen, btnBlue;

        // Ưu tiên lấy ở ui_svg_perfect / ui_shop_svg, thiếu thì vẽ
        public Sprite btnClose, progTrack, progFill, circleCard, circleSlot, toastBg;
        public Sprite chipGemBg, lockBadge, gemIcon;

        // Luôn vẽ (không folder nào có)
        public Sprite sky, ground, animFrame, panelWhite, badgeBg, dotGreen, bubbleInput;
        public Sprite beltBase, wheel, outBubble, outTagBg, itemGrain, tagWhite, chipGreen;
        public Sprite slotCard, slotCardLocked, btnTintable, btnGrey, lockedPill;
        public Sprite lockCircle, lockGlyph, closeGlyph, redDot, rivet;
        public Texture2D beltTex;

        // Bộ sprite chỉ giải MỘT LẦN cho mỗi lần chạy lệnh: prefab card và popup đều cần
        // nó, giải hai lần thì báo cáo bị nhân đôi và tốn thêm một lượt import.
        private static MillSkin _phien;

        /// <summary>Xoá cache phiên. Gọi ở đầu mỗi lệnh.</summary>
        public static void XoaCache() { _phien = null; }

        /// <summary>Giải toàn bộ sprite. Ghi mọi bước vào báo cáo.</summary>
        public static MillSkin Tao(MillReport rep)
        {
            if (_phien != null) return _phien;
            MillSpriteFactory.GanBaoCao(rep);
            var s = new MillSkin();
            Color Hex(string h) => MillSpriteFactory.Hex(h);
            Color Hex2(string h, float a) => MillSpriteFactory.Hex(h, a);

            // ── 1. SPRITE CÓ SẴN (ui_mill_assets) + đặt 9-slice theo radius trong CSS ──

            // HTML 44 `.popup-window` 1000×680 radius 25 — SVG rect inset 20 + rx 25 ⇒ slice 45
            s.popupBoard = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("popup_board"), 1000f, 45f, 45f, 45f, 45f);

            // HTML 137 `.inner-panel` radius 15 — SVG inset 5 + rx 15 + nửa stroke 3 ⇒ slice 23
            s.panelInner = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("panel_inner"), 900f, 25f, 25f, 25f, 25f);

            // HTML 77 `.ribbon-container` 480×90 — CÓ ĐUÔI CỜ hai bên ⇒ KHÔNG 9-slice.
            s.ribbon = MillSpriteFactory.Tim("ribbon_header");

            // HTML 219 `.recipe-card` radius 12 — SVG inset 2 + rx 12 + nửa stroke 2 ⇒ slice 16
            s.cardActive = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("recipe_card_active"),   240f, 16f, 16f, 16f, 16f);
            s.cardInactive = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("recipe_card_inactive"), 240f, 16f, 16f, 16f, 16f);
            s.cardLocked = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("recipe_card_locked"),   240f, 16f, 16f, 16f, 16f);

            // HTML 447 `.btn-thu` / 451 `.btn-speed` radius 8 + box-shadow 0 4px 0.
            // SVG bake luôn dải đáy (y 30→40 trên viewBox 40) ⇒ slice đáy 12, ba mép còn lại 8.
            s.btnGreen = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("btn_green"), 120f, 8f, 12f, 8f, 8f);
            s.btnBlue = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("btn_blue"),  120f, 8f, 12f, 8f, 8f);

            // HTML 610-616 (SVG máy: phễu + thân + highlight) — ảnh nguyên khối, không slice.
            s.machineBody = MillSpriteFactory.Tim("machine_body");
            s.gearLarge   = MillSpriteFactory.Tim("gear_large");   // HTML 619-626
            s.gearSmall   = MillSpriteFactory.Tim("gear_small");   // HTML 629-635

            // ── 2. SPRITE Ở FOLDER KHÁC — thiếu thì vẽ theo hex CSS ───────────────

            // HTML 118 `.btn-close` 45×45 nền #D45B4B viền 3 trắng radius 12
            s.btnClose = MillSpriteFactory.Tim("btn_close");
            if (s.btnClose == null)
                s.btnClose = MillSpriteFactory.VeKhoi("mill_btn_close",
                    MillSpriteFactory.K(40, 40, MillDesign.CloseRadius, 3f,
                        Hex(MillDesign.CCloseBg), Color.white));
            else s.btnClose = MillSpriteFactory.ApSlice(s.btnClose, 45f, 15f, 15f, 15f, 15f);

            // HTML 440 `.slot-progress` nền #D9D9D9 radius 7
            s.progTrack = MillSpriteFactory.Tim("progress_track");
            if (s.progTrack == null)
                s.progTrack = MillSpriteFactory.VeKhoi("mill_prog_track",
                    MillSpriteFactory.K(22, 22, MillDesign.SlotProgRadius, 0f,
                        Hex(MillDesign.CProgTrack), Color.clear));

            // HTML 444 `.progress-bar-fill` #82C94F
            s.progFill = MillSpriteFactory.Tim("progress_fill");
            if (s.progFill == null)
                s.progFill = MillSpriteFactory.VeKhoi("mill_prog_fill",
                    MillSpriteFactory.K(22, 22, MillDesign.SlotProgRadius, 0f,
                        Hex(MillDesign.CBtnGreen), Color.clear));

            // HTML 232 `.recipe-icon-circle` 50×50 nền #EEDABB
            s.circleCard = MillSpriteFactory.Tim("circle_preview");
            if (s.circleCard == null)
                s.circleCard = MillSpriteFactory.VeDia("mill_circle_card", 50,
                    Hex(MillDesign.CCardIconBg), 0f, Color.clear);

            // HTML 432 `.slot-icon-bg` 50×50 nền #F6E7D1
            s.circleSlot = MillSpriteFactory.VeDia("mill_circle_slot", 50,
                Hex(MillDesign.CSlotIconBg), 0f, Color.clear);

            // Toast — HTML KHÔNG có node này (quyết định của tool, xem báo cáo)
            s.toastBg = MillSpriteFactory.Tim("shop_toast");
            if (s.toastBg == null)
                s.toastBg = MillSpriteFactory.VeKhoi("mill_toast",
                    MillSpriteFactory.K(44, 44, MillDesign.ToastRadius, 2f,
                        Hex2(MillDesign.CConveyor, 0.94f), Hex(MillDesign.CBeltBorder)));

            // HTML 170 `.diamond-counter` nền trắng viền 2 #D6B798 radius 20
            s.chipGemBg = MillSpriteFactory.Tim("shop_currency_chip");
            if (s.chipGemBg == null)
                s.chipGemBg = MillSpriteFactory.VeKhoi("mill_chip_gem",
                    MillSpriteFactory.K(48, 48, MillDesign.ChipRadius, 2f,
                        Color.white, Hex(MillDesign.CPanelBorder)));
            else s.chipGemBg = MillSpriteFactory.ApSlice(s.chipGemBg, 120f, 22f, 22f, 22f, 22f);

            s.lockBadge = MillSpriteFactory.Tim("shop_lock_badge");

            // Icon kim cương: ưu tiên art thật của dự án (BuildingProcessUIBuilderTool dòng 30)
            s.gemIcon = MillSpriteFactory.Tim("kimcuong", "kimcuong-removebg-preview",
                                              "icon_gem", "gem", "diamond");
            if (s.gemIcon == null)
                s.gemIcon = MillSpriteFactory.VeKimCuong("mill_gem", 24, Hex(MillDesign.CBtnBlue));

            // ── 3. SPRITE PHẢI VẼ (không folder nào có) ───────────────────────────

            int gw = Mathf.RoundToInt(MillDesign.RightW);   // 629 — vào id để tự vô hiệu khi đổi

            // HTML 315 `.anim-sky` 60% cao, gradient #E6F3E6 → #CBE6CF. Bo 2 GÓC TRÊN cho
            // khớp radius 15 của `.animation-box` (trừ 3px viền ⇒ 12).
            s.sky = MillSpriteFactory.VeKhoi("mill_sky_" + gw + "x" + (int)MillDesign.SkyH,
                new MillSpriteFactory.Khoi
                {
                    w = gw, h = (int)MillDesign.SkyH,
                    rTL = 12f, rTR = 12f, rBR = 0f, rBL = 0f,
                    fillTop = Hex(MillDesign.CSkyTop), fillBottom = Hex(MillDesign.CSkyBottom),
                    borderColor = Color.clear, khongSlice = true
                });

            // HTML 320 `.anim-ground` 40% cao, nền #B48D64 + sọc dọc #A68058 chu kỳ 30/60px.
            s.ground = MillSpriteFactory.VeKhoi("mill_ground_" + gw + "x" + (int)MillDesign.GroundH,
                new MillSpriteFactory.Khoi
                {
                    w = gw, h = (int)MillDesign.GroundH,
                    rTL = 0f, rTR = 0f, rBR = 12f, rBL = 12f,
                    fillTop = Hex(MillDesign.CGroundMain), fillBottom = Hex(MillDesign.CGroundMain),
                    stripePeriod = MillDesign.GroundStripe, stripeColor = Hex(MillDesign.CGroundStripe),
                    borderColor = Color.clear, khongSlice = true
                });

            // HTML 307 `.animation-box` — CHỈ viền 3px #D6B798, lòng TRONG SUỐT, radius 15.
            s.animFrame = MillSpriteFactory.VeKhoi("mill_anim_frame",
                MillSpriteFactory.K(44, 44, MillDesign.AnimRadius, MillDesign.AnimBorder,
                    Color.clear, Hex(MillDesign.CPanelBorder)));

            // HTML 192 `.recipe-list-container` nền TRẮNG viền 3 #D6B798 radius 15
            s.panelWhite = MillSpriteFactory.VeKhoi("mill_panel_white",
                MillSpriteFactory.K(44, 44, 15f, MillDesign.RlBorder,
                    Color.white, Hex(MillDesign.CPanelBorder)));

            // HTML 327 `.status-badge` nền #F4E2C7 viền 2 #C4A882 radius 20
            s.badgeBg = MillSpriteFactory.VeKhoi("mill_badge",
                MillSpriteFactory.K(48, 48, MillDesign.BadgeRadius, 2f,
                    Hex(MillDesign.CBadgeBg), Hex(MillDesign.CBadgeBorder)));

            // HTML 333 `.status-dot` 12×12 #62E15D viền 2 #3DA239 — TÔ MÀU lúc runtime
            // (MillPopupUI đổi imgStatusDot.color) ⇒ vẽ TRẮNG để tint ra đúng màu.
            s.dotGreen = MillSpriteFactory.VeDia("mill_dot", 16, Color.white, 2f,
                new Color(0f, 0f, 0f, 0.28f));

            // HTML 336 `.input-bubble` nền #BA9054 viền 3 #9A723D radius 15
            s.bubbleInput = MillSpriteFactory.VeKhoi("mill_bubble_in",
                MillSpriteFactory.K(40, 40, MillDesign.BubbleRadius, 3f,
                    Hex(MillDesign.CBubbleBg), Hex(MillDesign.CBubbleBorder)));

            // HTML 345 `.conveyor-sys` nền #3F2C21 viền 2 #231812 radius 15.
            // (conveyor_base.png CÓ trong folder nhưng BAKE luôn 4 bánh lăn ở toạ độ cố định
            //  ⇒ 9-slice sẽ kéo giãn khoảng cách bánh; HTML 345/358 lại tách hẳn hai lớp với
            //  offset khác nhau. Nên vẽ riêng base + bánh, đúng bản thiết kế.)
            s.beltBase = MillSpriteFactory.VeKhoi("mill_belt_base",
                MillSpriteFactory.K(38, 38, MillDesign.BeltRadius, MillDesign.BeltBorder,
                    Hex(MillDesign.CConveyor), Hex(MillDesign.CBeltBorder)));

            // HTML 353-355 hoa văn sọc chéo + HTML 367 trôi 42px/giây
            s.beltTex = MillSpriteFactory.VeTextureBangTai("mill_belt_stripes");

            // HTML 362 `.wheel` 14×14 nền #1C120C viền 2 #4D3728
            s.wheel = MillSpriteFactory.VeDia("mill_wheel", 14, Hex(MillDesign.CWheel), 2f,
                Hex(MillDesign.CWheelRing));

            // HTML 393 `.output-bubble` 80×80 nền #F8E6CA viền 4 #DFB980
            s.outBubble = MillSpriteFactory.VeDia("mill_out_bubble", 80, Hex(MillDesign.COutBg),
                MillDesign.OutBorder, Hex(MillDesign.COutBorder));

            // HTML 400 `.output-tag` / 243 `.animal-tag` — viên thuốc trắng viền 2 #D6B798 r10
            s.tagWhite = MillSpriteFactory.VeKhoi("mill_tag_white",
                MillSpriteFactory.K(28, 28, MillDesign.OutTagRadius, 2f,
                    Color.white, Hex(MillDesign.CPanelBorder)));
            s.outTagBg = s.tagWhite;

            // HTML 252 `.cost-chip` trắng viền 2 #82C94F radius 10
            s.chipGreen = MillSpriteFactory.VeKhoi("mill_chip_green",
                MillSpriteFactory.K(28, 28, MillDesign.CardChipRadius, 2f,
                    Color.white, Hex(MillDesign.CChipGreen)));

            // HTML 422 `.slot-card` trắng viền 2 #E4D5C2 radius 12
            s.slotCard = MillSpriteFactory.Tim("slot_normal");
            if (s.slotCard == null)
                s.slotCard = MillSpriteFactory.VeKhoi("mill_slot_card",
                    MillSpriteFactory.K(36, 36, MillDesign.SlotRadius, MillDesign.SlotBorder,
                        Color.white, Hex(MillDesign.CCardBorder)));
            else s.slotCard = MillSpriteFactory.ApSlice(s.slotCard, 120f, 14f, 14f, 14f, 14f);

            // HTML 458 `.slot-locked` nền #D9CDB9 viền 2 #C2B6A3
            s.slotCardLocked = MillSpriteFactory.Tim("slot_empty");
            if (s.slotCardLocked == null)
                s.slotCardLocked = MillSpriteFactory.VeKhoi("mill_slot_card_locked",
                    MillSpriteFactory.K(36, 36, MillDesign.SlotRadius, MillDesign.SlotBorder,
                        Hex(MillDesign.CSlotLockBg), Hex(MillDesign.CSlotLockBd)));
            else s.slotCardLocked = MillSpriteFactory.ApSlice(
                s.slotCardLocked, 120f, 14f, 14f, 14f, 14f);

            // HTML 285 `.btn-empty-slot` — nút LỚN. MillPopupUI dòng 538 TÔ MÀU nút này
            // (mauNutBamDuoc #82C94F / mauNutKhoa #D9CDB9) ⇒ sprite phải TRẮNG để tint đúng.
            // Dải đáy vẽ bằng xám 0.72 nên sau khi tint vẫn ra "màu đậm hơn" của cùng tông.
            s.btnTintable = MillSpriteFactory.VeKhoi("mill_btn_tintable",
                new MillSpriteFactory.Khoi
                {
                    w = 40, h = 44, rTL = MillDesign.BtnMainRadius, rTR = MillDesign.BtnMainRadius,
                    rBR = MillDesign.BtnMainRadius, rBL = MillDesign.BtnMainRadius,
                    fillTop = Color.white, fillBottom = Color.white, borderColor = Color.clear,
                    lipH = 4f, lipColor = new Color(0.72f, 0.72f, 0.72f, 1f)
                });

            // HTML 287 `.btn-empty-slot` màu chết #B9B4AA + shadow #9E9A91 (khi popup chưa
            // gán imgMainButtonBg thì không dùng; giữ để nút "Cấp 18" của slot khoá dùng)
            s.btnGrey = MillSpriteFactory.VeKhoi("mill_btn_grey",
                new MillSpriteFactory.Khoi
                {
                    w = 40, h = 44, rTL = MillDesign.BtnMainRadius, rTR = MillDesign.BtnMainRadius,
                    rBR = MillDesign.BtnMainRadius, rBL = MillDesign.BtnMainRadius,
                    fillTop = Hex(MillDesign.CBtnGrey), fillBottom = Hex(MillDesign.CBtnGrey),
                    borderColor = Color.clear, lipH = 4f, lipColor = Hex(MillDesign.CBtnGreyLip)
                });

            // HTML 461 `.locked-btn` nền #AFA28F radius 15
            s.lockedPill = MillSpriteFactory.VeKhoi("mill_locked_pill",
                MillSpriteFactory.K(36, 36, MillDesign.SlotLockedBtnRadius, 0f,
                    Hex(MillDesign.CLockedPill), Color.clear));

            // HTML 459 `.lock-icon` 40×40 nền #645747
            s.lockCircle = MillSpriteFactory.VeDia("mill_lock_circle", 40,
                Hex(MillDesign.CLockCircle), 0f, Color.clear);

            // Ổ khoá trắng (HTML 459/277 dùng emoji 🔒 — tool vẽ lại để không lệ thuộc font)
            s.lockGlyph  = MillSpriteFactory.VeOKhoa("mill_glyph_lock", 32);
            // HTML 126 ký tự ✖
            s.closeGlyph = MillSpriteFactory.VeDauX("mill_glyph_x", 22);

            // HTML 660 chấm đỏ 10×10 #FF4A4A viền 2 trắng
            s.redDot = MillSpriteFactory.VeDia("mill_reddot", 12, Hex(MillDesign.CRedDot), 2f,
                Color.white);

            // HTML 63 `.rivet` 16×16 nền #DDAE80 viền 2 #8B5933
            s.rivet = MillSpriteFactory.VeDia("mill_rivet", 16, Hex(MillDesign.CRivet), 2f,
                Hex(MillDesign.CWoodBorder));

            // Bó cỏ trên băng tải: HTML 605-606 dùng emoji 🌾 ⇒ tool vẽ hạt màu placeholder,
            // lead thay bằng icon lúa mì thật (đã ghi vào CẦN BẠN LÀM).
            s.itemGrain = MillSpriteFactory.VeKhoi("mill_item_grain",
                MillSpriteFactory.K(24, 24, 10f, 2f, Hex(MillDesign.CItemGrain),
                    Hex(MillDesign.CItemGrainBd)));

            // ── Kiểm tra thiếu ────────────────────────────────────────────────────
            s.SoatThieu(rep);
            _phien = s;
            return s;
        }

        /// <summary>Báo cáo sprite bắt buộc mà vẫn null sau khi tìm + vẽ.</summary>
        private void SoatThieu(MillReport rep)
        {
            void C(Sprite sp, string ten, bool batBuoc)
            {
                if (sp != null) return;
                if (batBuoc) rep.Loi("THIẾU sprite bắt buộc '" + ten + "' — node dùng nó sẽ trống.");
                else rep.Canh("Thiếu sprite '" + ten + "' — node dùng nó để trống, không chặn.");
            }
            C(popupBoard, "popup_board", true);
            C(panelInner, "panel_inner", true);
            C(ribbon, "ribbon_header", true);
            C(machineBody, "machine_body", true);
            C(gearLarge, "gear_large", true);
            C(gearSmall, "gear_small", true);
            C(cardActive, "recipe_card_active", true);
            C(cardInactive, "recipe_card_inactive", true);
            C(cardLocked, "recipe_card_locked", true);
            C(btnGreen, "btn_green", true);
            C(btnBlue, "btn_blue", true);
            if (beltTex == null)
                rep.Loi("THIẾU texture sọc băng tải — UIScrollingTexture sẽ không chạy " +
                        "(nó cần RawImage CÓ texture, Wrap = Repeat).");
            C(lockBadge, "shop_lock_badge", false);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  MillBuilt — mọi node cần wire vào contract, thu về sau khi dựng.
    // ═══════════════════════════════════════════════════════════════════════════════
    internal sealed class MillBuilt
    {
        public GameObject popupRoot;
        public Transform  recipeContainer;
        public MillSlotUI[] slots;
        public RotatingGear gearLarge, gearSmall;
        public UIScrollingTexture belt;
        public ConveyorItem[] beltItems;

        public TMP_Text txtTitle, txtStatusBadge, txtSlotSummary, txtGemBalance;
        public TMP_Text txtMainButton, txtInputBubble, txtOutputTag;
        public Image imgStatusDot, imgOutputIcon, imgInputIcon, imgMainButtonBg;
        public Button btnClose, btnMain;
        public GameObject toastRoot;
        public TMP_Text toastText;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  MillPopupBuilder — DỰNG HIERARCHY. Đọc song song với full_mill_ui.html:
    //  mỗi khối dưới đây ghi rõ selector + dòng HTML tương ứng.
    // ═══════════════════════════════════════════════════════════════════════════════
    internal static class MillPopupBuilder
    {
        public static MillBuilt Dung(GameObject root, MillRecipeCardUI cardPrefab, MillReport rep)
        {
            var b = new MillBuilt();
            MillSkin sk = MillSkin.Tao(rep);

            // ── ROOT: giãn kín Canvas + Canvas con để đặt sortingOrder riêng ─────────
            var rootRt = MillUI.Comp<RectTransform>(root);
            MillUI.Stretch(rootRt, 0f, 0f, 0f, 0f);
            rootRt.localScale = Vector3.one;

            var cv = MillUI.Comp<Canvas>(root);
            cv.overrideSorting = true;
            cv.sortingOrder = MillDesign.SortOrder;   // cao hơn popup khác của dự án

            MillUI.Comp<MillPopupUI>(root);           // ⚠ Comp dùng `== null`, không dùng `??`

            // ── PopupRoot: node BỊ TẮT/BẬT (root phải luôn active để Awake set Instance) ──
            RectTransform popupRoot = MillUI.Stretch(MillUI.Node(root.transform, "PopupRoot"),
                                                     0f, 0f, 0f, 0f);
            b.popupRoot = popupRoot.gameObject;

            // Lớp tối phía sau — HTML KHÔNG có (body chỉ là nền xanh #2D4329). Quy ước popup
            // của dự án nên tool tự thêm; nó CHẶN click xuống world khi popup mở.
            Image dim = MillUI.Img(MillUI.Stretch(MillUI.Node(popupRoot, "Dim"), 0f, 0f, 0f, 0f),
                                   null, new Color(0f, 0f, 0f, 0.55f), true);
            dim.sprite = null;

            // ── WINDOW — HTML 44 `.popup-window` 1000×680, radius 25, sọc gỗ ─────────
            RectTransform win = MillUI.CC(MillUI.Node(popupRoot, "Window"),
                                          MillDesign.PopupW, MillDesign.PopupH, 0f, 0f);
            win.localScale = Vector3.one * MillDesign.TiLeHienThi;
            MillUI.Img(win, sk.popupBoard, Color.white, true);

            // Đinh tán — HTML 63-74 `.rivet` / `.r-tl`…
            float rv = MillDesign.RivetSize, ro = MillDesign.RivetOff;
            MillUI.Img(MillUI.TL(MillUI.Node(win, "Rivet_TL"), rv, rv, ro, ro), sk.rivet, Color.white);
            MillUI.Img(MillUI.TR(MillUI.Node(win, "Rivet_TR"), rv, rv, ro, ro), sk.rivet, Color.white);
            MillUI.Img(MillUI.BL(MillUI.Node(win, "Rivet_BL"), rv, rv, ro, ro), sk.rivet, Color.white);
            MillUI.Img(MillUI.BR(MillUI.Node(win, "Rivet_BR"), rv, rv, ro, ro), sk.rivet, Color.white);

            // ── INNER PANEL — HTML 137 `.inner-panel` padding 30 của window ──────────
            RectTransform panel = MillUI.Stretch(MillUI.Node(win, "InnerPanel"),
                MillDesign.PopupPad, MillDesign.PopupPad, MillDesign.PopupPad, MillDesign.PopupPad);
            MillUI.Img(panel, sk.panelInner, Color.white);

            // Ô nội dung: trừ viền 3 + padding 15 (HTML 142-143) ⇒ 904 × 584
            float inset = MillDesign.PanelBorder + MillDesign.PanelPad;
            RectTransform content = MillUI.Stretch(MillUI.Node(panel, "Content"),
                                                   inset, inset, inset, inset);

            DungHangTren(content, sk, b);                    // HTML 150 (ĐÃ BỎ TAB)
            RectTransform main = MillUI.TL(MillUI.Node(content, "MainContent"),
                MillDesign.ContentW, MillDesign.MainH, 0f, MillDesign.TopRowH + MillDesign.Gap);
            DungCotCongThuc(main, sk, b);                    // HTML 192
            DungCotPhai(main, sk, b, rep);                   // HTML 299

            // ── RUY BĂNG + NÚT X + TOAST: tạo SAU panel để vẽ ĐÈ LÊN (CSS z-index 10) ──
            DungRuyBang(win, sk, b);                         // HTML 77
            DungNutDong(win, sk, b);                         // HTML 118
            DungToast(win, sk, b);                           // HTML không có — tool tự quyết

            if (cardPrefab == null)
                rep.Canh("Prefab card công thức chưa dựng được ⇒ danh sách công thức sẽ TRỐNG " +
                         "lúc runtime (MillPopupUI.DungDanhSachCard cần recipeCardPrefab).");

            rep.Ok("Dựng xong hierarchy: " + DemNode(root.transform) + " node.");
            return b;
        }

        // ═════════════════════ HÀNG TRÊN (chỉ chip kim cương) ═════════════════════

        /// <summary>
        /// HTML 150 `.tabs-row`: video có 3 tab + chip kim cương. Chủ dự án CHỐT bỏ hệ tab
        /// ⇒ chỉ dựng chip kim cương, căn phải, giữ nguyên chiều cao hàng 45px để layout
        /// bên dưới không xê dịch một pixel nào so với bản thiết kế.
        /// </summary>
        private static void DungHangTren(RectTransform content, MillSkin sk, MillBuilt b)
        {
            RectTransform row = MillUI.TL(MillUI.Node(content, "TopRow"),
                                          MillDesign.ContentW, MillDesign.TopRowH, 0f, 0f);

            // HTML 170 `.diamond-counter` nền trắng viền 2 #D6B798 radius 20, padding 0 15, gap 8
            RectTransform chip = MillUI.TR(MillUI.Node(row, "Chip_Gem"),
                                           120f, MillDesign.TopRowH, 0f, 0f);
            MillUI.Img(chip, sk.chipGemBg, Color.white);
            MillUI.HangNgang(chip, MillDesign.ChipGap,
                             MillDesign.ChipPadX, MillDesign.ChipPadX, 0f, 0f, true);

            RectTransform ic = MillUI.Node(chip, "Icon_Gem");
            MillUI.Img(ic, sk.gemIcon, Color.white);
            MillUI.CoDinh(ic, MillDesign.ChipGemIcon, MillDesign.ChipGemIcon);

            // HTML 488 số "27" — font-weight 900, màu --text-brown
            RectTransform tx = MillUI.Node(chip, "Txt_GemBalance");
            b.txtGemBalance = MillUI.Txt(tx, "0", 18f, MillSpriteFactory.Hex(MillDesign.CTextBrown),
                                         TextAlignmentOptions.Left);
            MillUI.CoDinhCao(tx, 24f);
        }

        // ═════════════════════ CỘT CÔNG THỨC (trái) ═════════════════════

        private static void DungCotCongThuc(RectTransform main, MillSkin sk, MillBuilt b)
        {
            // HTML 192 `.recipe-list-container` 260 rộng, nền TRẮNG, viền 3 #D6B798, radius 15
            RectTransform box = MillUI.TL(MillUI.Node(main, "RecipeList"),
                                          MillDesign.RecipeListW, MillDesign.MainH, 0f, 0f);
            MillUI.Img(box, sk.panelWhite, Color.white);

            float ins = MillDesign.RlBorder + MillDesign.RlPad;      // 13
            RectTransform in2 = MillUI.Stretch(MillUI.Node(box, "RL_Content"), ins, ins, ins, ins);

            // HTML 202 `.list-header` "CÔNG THỨC" 18px w900 canh giữa
            MillUI.Txt(MillUI.TL(MillUI.Node(in2, "Txt_ListHeader"),
                                 MillDesign.RlInnerW, MillDesign.ListHeaderH, 0f, 0f),
                       "CÔNG THỨC", MillDesign.ListHeaderFont,
                       MillSpriteFactory.Hex(MillDesign.CTextBrown), TextAlignmentOptions.Center);

            // HTML 209 `.scroll-area` overflow-y auto, gap 10, padding-right 5
            float top = MillDesign.ListHeaderH + MillDesign.ListHeaderMb;
            RectTransform sv = MillUI.TL(MillUI.Node(in2, "ScrollView"),
                                         MillDesign.RlInnerW, MillDesign.ScrollH, 0f, top);
            var scroll = MillUI.Comp<ScrollRect>(sv.gameObject);

            RectTransform vp = MillUI.Stretch(MillUI.Node(sv, "Viewport"), 0f, 0f, 0f, 0f);
            MillUI.Comp<RectMask2D>(vp.gameObject);

            RectTransform ct = MillUI.Node(vp, "Content");
            ct.anchorMin = new Vector2(0f, 1f);
            ct.anchorMax = new Vector2(1f, 1f);
            ct.pivot = new Vector2(0.5f, 1f);
            ct.offsetMin = new Vector2(0f, 0f);
            ct.offsetMax = new Vector2(0f, 0f);

            var vlg = MillUI.Comp<VerticalLayoutGroup>(ct.gameObject);
            vlg.spacing = MillDesign.CardGap;                                  // HTML 214 gap:10px
            vlg.padding = new RectOffset(0, (int)MillDesign.ScrollPadRight, 0, 0); // HTML 216
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var csf = MillUI.Comp<ContentSizeFitter>(ct.gameObject);
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = ct;
            scroll.viewport = vp;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;
            scroll.scrollSensitivity = 25f;
            scroll.inertia = true;
            b.recipeContainer = ct;

            // HTML 285 `.btn-empty-slot` — NÚT LỚN, chữ do MillPopupUI đổi theo trạng thái
            // ("XAY NGAY" / "THIẾU NGUYÊN LIỆU" / "HẾT SLOT TRỐNG" / "CHƯA CHỌN CÔNG THỨC").
            float btnTop = MillDesign.RlInnerH - MillDesign.BtnMainH - 4f;
            RectTransform bm = MillUI.TL(MillUI.Node(in2, "Btn_Main"),
                MillDesign.RlInnerW, MillDesign.BtnMainH + 4f, 0f, btnTop);
            b.imgMainButtonBg = MillUI.Img(bm, sk.btnTintable, Color.white, true);
            b.btnMain = MillUI.Btn(b.imgMainButtonBg);

            // Chữ nằm ở phần THÂN nút (trừ 4px dải đáy) ⇒ lệch lên 2px.
            b.txtMainButton = MillUI.Txt(MillUI.CC(MillUI.Node(bm, "Txt_MainButton"),
                                                   MillDesign.RlInnerW - 12f, MillDesign.BtnMainH,
                                                   0f, 2f),
                "HẾT SLOT TRỐNG", MillDesign.BtnMainFont, Color.white, TextAlignmentOptions.Center);
        }

        private static int DemNode(Transform t)
        {
            int n = 1;
            for (int i = 0; i < t.childCount; i++) n += DemNode(t.GetChild(i));
            return n;
        }

        // ═════════════════════ CỘT PHẢI: Ô ANIMATION + KHU SLOT ═════════════════════

        private static void DungCotPhai(RectTransform main, MillSkin sk, MillBuilt b,
                                        MillReport rep)
        {
            // HTML 299 `.right-column` flex-grow, gap 15
            RectTransform col = MillUI.TL(MillUI.Node(main, "RightColumn"),
                MillDesign.RightW, MillDesign.MainH,
                MillDesign.RecipeListW + MillDesign.Gap, 0f);

            DungOAnimation(col, sk, b, rep);
            DungKhuSlot(col, sk, b, rep);
        }

        /// <summary>HTML 307 `.animation-box` — 629×250, viền 3 #D6B798, radius 15.</summary>
        private static void DungOAnimation(RectTransform col, MillSkin sk, MillBuilt b,
                                           MillReport rep)
        {
            float W = MillDesign.RightW;
            RectTransform box = MillUI.TL(MillUI.Node(col, "AnimationBox"), W, MillDesign.AnimH,
                                          0f, 0f);

            // ⚠ `overflow:hidden` của CSS: KHÔNG cần Mask. Mọi node bên trong (máy ở right 140
            //   + rộng 180 = hết 320 < 629; bó cỏ chạy tối đa 50+250 = 300) đều nằm gọn trong
            //   ô. Thêm Mask là tốn thêm một lượt stencil mà không đổi hình.
            //   Bù lại, sprite trời/đất đã được BO SẴN 2 góc tương ứng nên mép cong vẫn khớp.

            // HTML 315 `.anim-sky` 60% cao, gradient #E6F3E6 → #CBE6CF
            MillUI.Img(MillUI.TL(MillUI.Node(box, "Sky"), W, MillDesign.SkyH, 0f, 0f),
                       sk.sky, Color.white);

            // HTML 320 `.anim-ground` 40% cao, #B48D64 + sọc dọc #A68058 chu kỳ 30px
            MillUI.Img(MillUI.BL(MillUI.Node(box, "Ground"), W, MillDesign.GroundH, 0f, 0f),
                       sk.ground, Color.white);

            // ── HTML 327 `.status-badge` top 15 left 15 ────────────────────────────
            RectTransform badge = MillUI.TL(MillUI.Node(box, "Badge_Status"), 180f, 30f,
                                            MillDesign.BadgeLeft, MillDesign.BadgeTop);
            MillUI.Img(badge, sk.badgeBg, Color.white);
            MillUI.HangNgang(badge, MillDesign.BadgeGap,
                             MillDesign.BadgePadX, MillDesign.BadgePadX,
                             MillDesign.BadgePadY, MillDesign.BadgePadY, true);

            RectTransform dot = MillUI.Node(badge, "Img_StatusDot");
            // Sprite chấm vẽ TRẮNG: MillPopupUI dòng 745 tự tô mauDotDangXay #62E15D /
            // mauDotRanh khi đổi trạng thái ⇒ để trắng thì tint ra đúng màu CSS.
            b.imgStatusDot = MillUI.Img(dot, sk.dotGreen,
                                        MillSpriteFactory.Hex(MillDesign.CDotGreen));
            MillUI.CoDinh(dot, MillDesign.DotSize, MillDesign.DotSize);

            RectTransform bt = MillUI.Node(badge, "Txt_StatusBadge");
            b.txtStatusBadge = MillUI.Txt(bt, "Máy đang rảnh", MillDesign.BadgeFont,
                MillSpriteFactory.Hex(MillDesign.CTextBrown), TextAlignmentOptions.Left);
            MillUI.CoDinhCao(bt, 18f);

            // ── HTML 336 `.input-bubble` top 70 left 20 ────────────────────────────
            RectTransform bub = MillUI.TL(MillUI.Node(box, "Bubble_Input"), 110f,
                MillDesign.BubblePadY * 2f + 22f, MillDesign.BubbleLeft, MillDesign.BubbleTop);
            MillUI.Img(bub, sk.bubbleInput, Color.white);
            MillUI.HangNgang(bub, MillDesign.BubbleGap,
                             MillDesign.BubblePadX, MillDesign.BubblePadX,
                             MillDesign.BubblePadY, MillDesign.BubblePadY, true);

            RectTransform bi = MillUI.Node(bub, "Img_InputIcon");
            b.imgInputIcon = MillUI.Img(bi, null, Color.white);
            b.imgInputIcon.enabled = false;          // MillPopupUI bật khi công thức có icon
            MillUI.CoDinh(bi, MillDesign.BubbleIcon, MillDesign.BubbleIcon);

            RectTransform bx = MillUI.Node(bub, "Txt_InputBubble");
            b.txtInputBubble = MillUI.Txt(bx, "x0", MillDesign.BubbleFont, Color.white,
                                          TextAlignmentOptions.Left);
            MillUI.CoDinhCao(bx, 22f);

            // ── HTML 345 `.conveyor-sys` 380×35 bottom 40 left 60 ──────────────────
            RectTransform belt = MillUI.BL(MillUI.Node(box, "Conveyor"),
                MillDesign.BeltW, MillDesign.BeltH, MillDesign.BeltLeft, MillDesign.BeltBottom);
            Image beltImg = MillUI.Img(belt, sk.beltBase, Color.white);

            // Mask THEO ALPHA SPRITE (không phải RectMask2D): hai đầu băng tải bo radius 15,
            // RectMask2D chỉ cắt hình chữ nhật nên sọc sẽ tràn ra góc cong.
            var mask = MillUI.Comp<Mask>(belt.gameObject);
            mask.showMaskGraphic = true;
            beltImg.raycastTarget = false;

            // HTML 353 `.conveyor-stripes-anim` — RawImage (Image KHÔNG có uvRect nên
            // không cuộn được), texture Wrap = Repeat.
            RectTransform st = MillUI.Stretch(MillUI.Node(belt, "Belt_Stripes"),
                MillDesign.BeltBorder, MillDesign.BeltBorder,
                MillDesign.BeltBorder, MillDesign.BeltBorder);
            var raw = MillUI.Comp<RawImage>(st.gameObject);
            raw.texture = sk.beltTex;
            raw.color = Color.white;
            raw.raycastTarget = false;
            // uvRect map 1 texel = 1 px màn hình ⇒ hoa văn đúng cỡ CSS và tốc độ đúng 42px/s.
            float uw = (MillDesign.BeltW - MillDesign.BeltBorder * 2f) / MillDesign.BeltTileX;
            float uh = (MillDesign.BeltH - MillDesign.BeltBorder * 2f) / MillDesign.BeltTileX;
            raw.uvRect = new Rect(0f, 0f, uw, uh);

            b.belt = MillUI.Comp<UIScrollingTexture>(st.gameObject);
            b.belt.pixelsPerSecond = 42f;            // HTML 367 translateX(-42px) / 1s
            b.belt.stripePeriodPx = 30f;             // HTML 355 (chỉ dùng nếu bật cờ dưới)
            b.belt.dungChuKyHoaVan = false;          // ⚠ texture rộng ĐÚNG 42 ⇒ phải để FALSE
            b.belt.cuonTheoTrucDoc = false;
            b.belt.autoStart = false;                // MillPopupUI điều khiển qua SetRunning

            // ── HTML 358 `.conveyor-wheels` bottom 30 left 70, rộng 360, space-between ──
            RectTransform wheels = MillUI.BL(MillUI.Node(box, "Wheels"),
                MillDesign.WheelsW, MillDesign.WheelSize,
                MillDesign.WheelsLeft, MillDesign.WheelsBottom);
            float buoc = (MillDesign.WheelsW - MillDesign.WheelSize) / (MillDesign.WheelCount - 1);
            for (int i = 0; i < MillDesign.WheelCount; i++)
                MillUI.Img(MillUI.TL(MillUI.Node(wheels, "Wheel_" + (i + 1)),
                    MillDesign.WheelSize, MillDesign.WheelSize, i * buoc, 0f), sk.wheel, Color.white);

            // ── HTML 370 `.moving-item` — ĐÚNG 2 bó cỏ (HTML 605-606), lệch pha 1.5s ──
            b.beltItems = new ConveyorItem[MillDesign.ItemCount];
            for (int i = 0; i < MillDesign.ItemCount; i++)
            {
                RectTransform it = MillUI.BL(MillUI.Node(box, "BeltItem_" + (i + 1)),
                    MillDesign.ItemFont + 4f, MillDesign.ItemFont + 4f,
                    MillDesign.ItemLeft, MillDesign.ItemBottom);
                MillUI.Img(it, sk.itemGrain, Color.white);

                var ci = MillUI.Comp<ConveyorItem>(it.gameObject);
                ci.cycleSeconds = 3f;                    // HTML 373
                ci.delaySeconds = i * 1.5f;              // HTML 375-376 (MillPopupUI ghi đè)
                ci.travelPx = 230f;                      // HTML 380
                ci.overshootPx = 20f;                    // HTML 381 (250 − 230)
                ci.dropPx = 10f;                         // HTML 381 translateY(10px)
                ci.mocChay = 0.80f;                      // HTML 380
                ci.mocRoi = 0.85f;                       // HTML 381
                ci.autoStart = false;
                b.beltItems[i] = ci;
            }

            // ── HTML 386 `.machine-wrapper` 180×180 bottom 35 right 140 ────────────
            RectTransform mac = MillUI.BR(MillUI.Node(box, "Machine"),
                MillDesign.MachineSize, MillDesign.MachineSize,
                MillDesign.MachineRight, MillDesign.MachineBottom);

            // Phễu + thân + highlight nằm chung machine_body.png (HTML 611-616)
            MillUI.Img(MillUI.Stretch(MillUI.Node(mac, "Body"), 0f, 0f, 0f, 0f),
                       sk.machineBody, Color.white);

            b.gearLarge = DungBanhRang(mac, "Gear_Large", sk.gearLarge,
                MillDesign.GearLargeCx, MillDesign.GearLargeCy, MillDesign.GearLargeR,
                MillDesign.GearLargeSpriteRatio, rep);
            b.gearSmall = DungBanhRang(mac, "Gear_Small", sk.gearSmall,
                MillDesign.GearSmallCx, MillDesign.GearSmallCy, MillDesign.GearSmallR,
                MillDesign.GearSmallSpriteRatio, rep);

            // ── HTML 393 `.output-bubble` 80×80 bottom 50 right 20 ─────────────────
            RectTransform ob = MillUI.BR(MillUI.Node(box, "Output_Bubble"),
                MillDesign.OutSize, MillDesign.OutSize, MillDesign.OutRight, MillDesign.OutBottom);
            MillUI.Img(ob, sk.outBubble, Color.white);

            RectTransform oi = MillUI.CC(MillUI.Node(ob, "Img_OutputIcon"), 46f, 46f, 0f, 0f);
            b.imgOutputIcon = MillUI.Img(oi, null, Color.white);
            b.imgOutputIcon.enabled = false;

            // HTML 400 `.output-tag` bottom -12 ⇒ nhô XUỐNG dưới đáy bong bóng 12px
            RectTransform ot = MillUI.BC(MillUI.Node(ob, "Output_Tag"), 90f, MillDesign.OutTagH,
                                          MillDesign.OutTagBottom);
            MillUI.Img(ot, sk.outTagBg, Color.white);
            MillUI.HangNgang(ot, 0f, 10f, 10f, 0f, 0f, true);

            RectTransform otx = MillUI.Node(ot, "Txt_OutputTag");
            b.txtOutputTag = MillUI.Txt(otx, "", MillDesign.OutTagFont,
                MillSpriteFactory.Hex(MillDesign.CTextBrown), TextAlignmentOptions.Center);
            MillUI.CoDinhCao(otx, 15f);

            // ── KHUNG VIỀN: tạo CUỐI CÙNG để vẽ đè lên trời/đất (CSS border nằm trên
            //    background của các phần tử con định vị absolute).
            Image fr = MillUI.Img(MillUI.TL(MillUI.Node(box, "AnimBox_Frame"),
                MillDesign.RightW, MillDesign.AnimH, 0f, 0f), sk.animFrame, Color.white);
            fr.raycastTarget = false;
        }

        /// <summary>
        /// Một bánh răng. Toạ độ trong HTML là hệ viewBox 200×200 của SVG máy (HTML 610);
        /// wrapper chỉ 180×180 nên phải nhân 0.9. Sprite gear_*.png lại có lề riêng
        /// (bánh chiếm 90/100 với bánh lớn, 60/70 với bánh nhỏ) nên kích thước sprite phải
        /// nhân thêm tỉ lệ đó, nếu không bánh răng bị nhỏ hơn thiết kế.
        /// </summary>
        private static RotatingGear DungBanhRang(RectTransform mac, string ten, Sprite sp,
            float cx, float cy, float r, float tiLeSprite, MillReport rep)
        {
            float duongKinhTrenManHinh = r * 2f * MillDesign.VbToMachine;
            float coSprite = duongKinhTrenManHinh * tiLeSprite;

            // Lệch so với TÂM wrapper: CSS +y xuống, Unity +y lên ⇒ đảo dấu trục Y.
            float dx = (cx - 100f) * MillDesign.VbToMachine;
            float dy = -(cy - 100f) * MillDesign.VbToMachine;

            RectTransform rt = MillUI.CC(MillUI.Node(mac, ten), coSprite, coSprite, dx, dy);
            MillUI.Img(rt, sp, Color.white);

            // RotatingGear là file CÓ SẴN của dự án (Scripts/FeedMill/RotatingGear.cs).
            // Popup tự gọi Configure() lúc Open ⇒ chỉ cần tắt playOnStart.
            var g = MillUI.Comp<RotatingGear>(rt.gameObject);
            MillWiring.DatBool(g, "playOnStart", false, rep);
            return g;
        }

        /// <summary>HTML 407 `.slots-area` — tiêu đề + 5 card slot.</summary>
        private static void DungKhuSlot(RectTransform col, MillSkin sk, MillBuilt b,
                                        MillReport rep)
        {
            RectTransform area = MillUI.TL(MillUI.Node(col, "SlotsArea"),
                MillDesign.RightW, MillDesign.SlotsAreaH, 0f, MillDesign.AnimH + MillDesign.Gap);

            // HTML 413 `.slots-header` "SLOT XAY" + <span> tổng kết
            MillUI.Txt(MillUI.TL(MillUI.Node(area, "Txt_SlotsHeader"), 80f,
                                 MillDesign.SlotsHeaderH, 0f, 0f),
                       "SLOT XAY", MillDesign.SlotsHeaderFont,
                       MillSpriteFactory.Hex(MillDesign.CTextBrown), TextAlignmentOptions.Left);

            // HTML 417 span: màu --text-light-brown, không in hoa, weight 800
            b.txtSlotSummary = MillUI.Txt(MillUI.TL(MillUI.Node(area, "Txt_SlotSummary"),
                    MillDesign.RightW - 86f, MillDesign.SlotsHeaderH, 86f, 0f),
                "", MillDesign.SlotsHeaderSpanFont,
                MillSpriteFactory.Hex(MillDesign.CTextLight), TextAlignmentOptions.Left);

            // HTML 419 `.slots-container` cao 180, gap 10, 5 card chia đều
            RectTransform cont = MillUI.TL(MillUI.Node(area, "SlotsContainer"),
                MillDesign.RightW, MillDesign.SlotsContainerH, 0f,
                MillDesign.SlotsHeaderH + MillDesign.SlotsHeaderGap);

            float w = MillDesign.SlotW;
            b.slots = new MillSlotUI[MillDesign.SlotCount];
            for (int i = 0; i < MillDesign.SlotCount; i++)
                b.slots[i] = MillSlotBuilder.Dung(cont, i, w, sk, rep);
        }

        // ═════════════════════ RUY BĂNG / NÚT X / TOAST ═════════════════════

        /// <summary>HTML 77 `.ribbon-container` 480×90, top −25, căn giữa; z-index 10.</summary>
        private static void DungRuyBang(RectTransform win, MillSkin sk, MillBuilt b)
        {
            RectTransform rb = MillUI.TC(MillUI.Node(win, "Ribbon"),
                MillDesign.RibbonW, MillDesign.RibbonH, MillDesign.RibbonTop);
            MillUI.Img(rb, sk.ribbon, Color.white);

            // HTML 95 `.ribbon-center` cao 75 (phần chữ), 15px còn lại là đuôi cờ.
            var t = MillUI.Txt(MillUI.TL(MillUI.Node(rb, "Txt_Title"),
                                          MillDesign.RibbonW, 75f, 0f, 0f),
                "MÁY XAY THỨC ĂN", MillDesign.RibbonFont, Color.white,
                TextAlignmentOptions.Center);
            // HTML 111-112: -webkit-text-stroke 1.5px #A1591A + text-shadow 0 4px 0 #A1591A
            t.outlineWidth = 0.16f;
            t.outlineColor = MillSpriteFactory.Hex("#A1591A");
            b.txtTitle = t;
        }

        /// <summary>HTML 118 `.btn-close` 45×45, top/right −10, nền #D45B4B viền 3 trắng.</summary>
        private static void DungNutDong(RectTransform win, MillSkin sk, MillBuilt b)
        {
            RectTransform rt = MillUI.TR(MillUI.Node(win, "Btn_Close"),
                MillDesign.CloseSize, MillDesign.CloseSize,
                MillDesign.CloseOff, MillDesign.CloseOff);
            Image img = MillUI.Img(rt, sk.btnClose, Color.white, true);
            b.btnClose = MillUI.Btn(img);

            // HTML 126 ký tự ✖ — vẽ thành sprite để không phụ thuộc font có glyph đó.
            Image g = MillUI.Img(MillUI.CC(MillUI.Node(rt, "Glyph_X"), 22f, 22f, 0f, 0f),
                                 sk.closeGlyph, Color.white);
            g.raycastTarget = false;
        }

        /// <summary>
        /// Toast — HTML KHÔNG có node này. Tool tự quyết: viên thuốc nâu đậm căn giữa đáy
        /// Window, dùng lại màu băng tải (#3F2C21 / viền #231812) cho khỏi lạc bảng màu.
        /// Tắt sẵn; MillPopupUI tự thêm CanvasGroup và fade.
        /// </summary>
        private static void DungToast(RectTransform win, MillSkin sk, MillBuilt b)
        {
            RectTransform rt = MillUI.BC(MillUI.Node(win, "Toast"),
                MillDesign.ToastW, MillDesign.ToastH, MillDesign.ToastBottom);
            MillUI.Img(rt, sk.toastBg, Color.white);
            MillUI.Comp<CanvasGroup>(rt.gameObject);

            b.toastText = MillUI.TxtStretch(rt, "Txt_Toast", "", MillDesign.ToastFont,
                Color.white, TextAlignmentOptions.Center, true);
            b.toastText.rectTransform.offsetMin = new Vector2(16f, 6f);
            b.toastText.rectTransform.offsetMax = new Vector2(-16f, -6f);

            b.toastRoot = rt.gameObject;
            rt.gameObject.SetActive(false);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  MillSlotBuilder — MỘT CARD SLOT XAY (HTML 422 `.slot-card`, 5 cái)
    //
    //  ⚠ VÌ SAO icon/tên/ổ khoá/chấm đỏ NẰM NGOÀI 5 root trạng thái:
    //  `MillSlotUI.SetMode` (dòng 165) bật ĐÚNG MỘT root và tắt 4 root còn lại, nhưng nó
    //  điều khiển `imgLockIcon` bằng `.enabled` và `redDot` bằng SetActive RIÊNG. Nếu ta
    //  nhét ổ khoá vào trong `rootLockedLevel` thì ở trạng thái `UnlockGem` root đó tắt ⇒
    //  ổ khoá vô hình dù enabled = true. Còn `imgIcon`/`txtName` do `DatCongThuc` xử lý
    //  (null ⇒ enabled=false / chuỗi rỗng) nên cũng phải sống ngoài root.
    //
    //  ⚠ ĐĨA KEM sau icon thì NGƯỢC LẠI: nó phải TẮT ở hai trạng thái khoá (HTML 458 slot
    //  khoá không có `.slot-icon-bg`) mà contract KHÔNG có field nào cho nó ⇒ đặt một bản
    //  copy trong mỗi root Running/Ready/Empty. Ba node giống nhau, đổi lại là đúng thiết kế.
    //
    //  Thứ tự con quyết định thứ tự vẽ: 5 root trước → icon/ổ khoá/chữ/chấm đỏ sau, để lớp
    //  nền khoá (phủ kín card) không che số thứ tự và tên.
    // ═══════════════════════════════════════════════════════════════════════════════
    internal static class MillSlotBuilder
    {
        public static MillSlotUI Dung(RectTransform parent, int idx, float w,
                                      MillSkin sk, MillReport rep)
        {
            float bx = MillDesign.SlotBorder;                        // 2
            float padV = MillDesign.SlotPadV, padH = MillDesign.SlotPadH;
            float noiW = w - bx * 2f - padH * 2f;                     // bề rộng ô nội dung
            float H = MillDesign.SlotsContainerH;                     // 180

            RectTransform card = MillUI.TL(MillUI.Node(parent, "Slot_" + (idx + 1)), w, H,
                idx * (w + MillDesign.SlotGap), 0f);
            Image bg = MillUI.Img(card, sk.slotCard, Color.white);

            var slot = MillUI.Comp<MillSlotUI>(card.gameObject);
            var so = new SerializedObject(slot);
            string own = "MillSlotUI[" + (idx + 1) + "]";

            // ── Toạ độ dùng chung ────────────────────────────────────────────────
            float iconTop = bx + padV + MillDesign.SlotIconMt;                    // 27
            float nameTop = iconTop + MillDesign.SlotIconBg + MillDesign.SlotIconMb; // 87
            float btnH = MillDesign.SlotBtnH + 4f;                                 // 31 + dải đáy
            float btnBottom = bx + padV;                                           // 12
            float progBottom = btnBottom + btnH + MillDesign.SlotProgMb;           // 55
            float progW = noiW * MillDesign.SlotProgW;
            float btnW = noiW * MillDesign.SlotBtnW;
            float lockTop = bx + padV + MillDesign.SlotLockMt;                     // 37

            // ═══ ROOT 1: ĐANG XAY ═══════════════════════════════════════════════
            RectTransform rRun = MillUI.Stretch(MillUI.Node(card, "Root_Running"), 0, 0, 0, 0);
            DiaKem(rRun, sk, iconTop);

            // HTML 440 `.slot-progress` 90% × 14, radius 7, nền #D9D9D9
            RectTransform tr = MillUI.BC(MillUI.Node(rRun, "Progress_Track"),
                                          progW, MillDesign.SlotProgH, progBottom);
            MillUI.Img(tr, sk.progTrack, Color.white);
            MillUI.Comp<RectMask2D>(tr.gameObject);   // giữ đầu thanh fill trong rãnh bo góc

            // HTML 444 `.progress-bar-fill` #82C94F
            RectTransform fi = MillUI.Stretch(MillUI.Node(tr, "Progress_Fill"), 0, 0, 0, 0);
            Image fill = MillUI.Img(fi, sk.progFill, Color.white);
            // ⚠ BẮT BUỘC: Filled / Horizontal / Left. Để Simple thì `fillAmount` không có
            //   tác dụng và thanh ĐỨNG YÊN mà Unity KHÔNG báo lỗi gì — bug rất khó thấy.
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;

            // HTML 445 `.progress-time` chữ trắng 10px nằm GIỮA thanh
            var txtTimer = MillUI.TxtStretch(tr, "Txt_Timer", "", MillDesign.SlotTimerFont,
                Color.white, TextAlignmentOptions.Center);

            // HTML 451 `.btn-speed` nền #40A4E5, gap 4, "💎 x6"
            RectTransform bs = MillUI.BC(MillUI.Node(rRun, "Btn_SpeedUp"), btnW, btnH, btnBottom);
            Image bsImg = MillUI.Img(bs, sk.btnBlue, Color.white, true);
            Button btnSpeed = MillUI.Btn(bsImg);
            MillUI.HangNgang(bs, 4f, 6f, 6f, 0f, 4f, false);
            RectTransform bsIc = MillUI.Node(bs, "Icon_Gem");
            MillUI.Img(bsIc, sk.gemIcon, Color.white);
            MillUI.CoDinh(bsIc, 14f, 14f);
            RectTransform bsTx = MillUI.Node(bs, "Txt_SpeedUpCost");
            var txtSpeed = MillUI.Txt(bsTx, "x0", MillDesign.SlotBtnFont, Color.white,
                                      TextAlignmentOptions.Center);
            MillUI.CoDinhCao(bsTx, 18f);

            // ═══ ROOT 2: CHỜ THU ════════════════════════════════════════════════
            RectTransform rRdy = MillUI.Stretch(MillUI.Node(card, "Root_Ready"), 0, 0, 0, 0);
            DiaKem(rRdy, sk, iconTop);

            // HTML 447 `.btn-thu` nền #82C94F chữ "THU"
            RectTransform bc = MillUI.BC(MillUI.Node(rRdy, "Btn_Collect"), btnW, btnH, btnBottom);
            Image bcImg = MillUI.Img(bc, sk.btnGreen, Color.white, true);
            Button btnCollect = MillUI.Btn(bcImg);
            MillUI.Txt(MillUI.CC(MillUI.Node(bc, "Txt_Collect"), btnW, MillDesign.SlotBtnH, 0f, 2f),
                       "THU", MillDesign.SlotBtnFont, Color.white, TextAlignmentOptions.Center);

            // ═══ ROOT 3: TRỐNG ══════════════════════════════════════════════════
            // HTML KHÔNG vẽ trạng thái này (video chỉ có slot đang xay / chờ thu / khoá).
            // Tool tự quyết: chữ "Trống" xám nhạt giữa card, không nút.
            RectTransform rEmp = MillUI.Stretch(MillUI.Node(card, "Root_Empty"), 0, 0, 0, 0);
            DiaKem(rEmp, sk, iconTop);
            MillUI.Txt(MillUI.TL(MillUI.Node(rEmp, "Txt_Empty"), noiW, 20f,
                                 bx + padH, nameTop),
                       "Trống", MillDesign.SlotNameFont,
                       MillSpriteFactory.Hex(MillDesign.CSlotNum), TextAlignmentOptions.Center);

            // ═══ ROOT 4: CHƯA MỞ — MUA BẰNG KIM CƯƠNG ═══════════════════════════
            // HTML chỉ vẽ slot #5 "chưa đủ cấp". Trạng thái mua bằng kim cương (video: slot
            // #4, 15 kim cương) không có CSS riêng ⇒ tool tái dùng nền khoá #D9CDB9 (HTML 458)
            // + nút xanh dương #40A4E5 (màu kim cương của bản thiết kế, HTML 487).
            RectTransform rGem = MillUI.Stretch(MillUI.Node(card, "Root_UnlockGem"), 0, 0, 0, 0);
            MillUI.Img(MillUI.Stretch(MillUI.Node(rGem, "Bg_Locked"), 0, 0, 0, 0),
                       sk.slotCardLocked, Color.white);
            MillUI.Txt(MillUI.TL(MillUI.Node(rGem, "Txt_UnlockHint"), noiW, 18f,
                                 bx + padH, nameTop),
                       "Mở slot", MillDesign.SlotLockTextFont,
                       MillSpriteFactory.Hex(MillDesign.CLockedText), TextAlignmentOptions.Center);

            RectTransform bu = MillUI.BC(MillUI.Node(rGem, "Btn_UnlockGem"), btnW, btnH, btnBottom);
            Image buImg = MillUI.Img(bu, sk.btnBlue, Color.white, true);
            Button btnUnlock = MillUI.Btn(buImg);
            MillUI.HangNgang(bu, 4f, 6f, 6f, 0f, 4f, false);
            RectTransform buIc = MillUI.Node(bu, "Icon_Gem");
            MillUI.Img(buIc, sk.gemIcon, Color.white);
            MillUI.CoDinh(buIc, 14f, 14f);
            RectTransform buTx = MillUI.Node(bu, "Txt_GemCost");
            var txtGemCost = MillUI.Txt(buTx, "0", MillDesign.SlotBtnFont, Color.white,
                                        TextAlignmentOptions.Center);
            MillUI.CoDinhCao(buTx, 18f);

            // ═══ ROOT 5: CHƯA ĐỦ CẤP ════════════════════════════════════════════
            RectTransform rLvl = MillUI.Stretch(MillUI.Node(card, "Root_LockedLevel"), 0, 0, 0, 0);
            MillUI.Img(MillUI.Stretch(MillUI.Node(rLvl, "Bg_Locked"), 0, 0, 0, 0),
                       sk.slotCardLocked, Color.white);

            // HTML 460 `.locked-text` "Chưa đủ cấp" 12px #726352
            var txtLockLabel = MillUI.Txt(MillUI.TL(MillUI.Node(rLvl, "Txt_LockLabel"), noiW, 18f,
                                                     bx + padH, nameTop),
                "Chưa đủ cấp", MillDesign.SlotLockTextFont,
                MillSpriteFactory.Hex(MillDesign.CLockedText), TextAlignmentOptions.Center);

            // HTML 461 `.locked-btn` 80% nền #AFA28F radius 15, chữ trắng "Cấp 18"
            RectTransform pill = MillUI.BC(MillUI.Node(rLvl, "Locked_Pill"),
                noiW * MillDesign.SlotLockedBtnW, MillDesign.SlotLockedBtnH,
                bx + MillDesign.SlotLockedBtnMb);
            MillUI.Img(pill, sk.lockedPill, Color.white);
            var txtLvl = MillUI.TxtStretch(pill, "Txt_LockLevelValue", "Cấp 18",
                MillDesign.SlotLockTextFont, Color.white, TextAlignmentOptions.Center);

            // ═══ NODE DÙNG CHUNG (vẽ SAU 5 root) ════════════════════════════════

            // Icon sản phẩm — MillSlotUI tự bật/tắt theo công thức.
            RectTransform ii = MillUI.TC(MillUI.Node(card, "Img_Icon"), 40f, 40f, iconTop + 5f);
            Image imgIcon = MillUI.Img(ii, null, Color.white);
            imgIcon.enabled = false;

            // HTML 459 `.lock-icon` 40×40 nền #645747 + ổ khoá trắng.
            RectTransform li = MillUI.TC(MillUI.Node(card, "Img_LockIcon"),
                MillDesign.SlotLockIcon, MillDesign.SlotLockIcon, lockTop);
            Image imgLock = MillUI.Img(li, sk.lockCircle, Color.white);
            imgLock.enabled = false;              // SetMode bật khi ở hai trạng thái khoá
            Image lockGlyph = MillUI.Img(MillUI.CC(MillUI.Node(li, "Glyph_Lock"), 22f, 22f, 0f, 0f),
                                          sk.lockGlyph, Color.white);
            lockGlyph.raycastTarget = false;

            // HTML 431 `.slot-num` "#1" top 8 left 8 (đo từ mép TRONG viền) 12px #BDB09F
            var txtIndex = MillUI.Txt(MillUI.TL(MillUI.Node(card, "Txt_Index"), 30f, 16f,
                                                 bx + MillDesign.SlotNumLeft,
                                                 bx + MillDesign.SlotNumTop),
                "#" + (idx + 1), MillDesign.SlotNumFont,
                MillSpriteFactory.Hex(MillDesign.CSlotNum), TextAlignmentOptions.Left);

            // HTML 438 `.slot-name` 13px w900 #7D5133, canh giữa, 2 dòng
            var txtName = MillUI.Txt(MillUI.TL(MillUI.Node(card, "Txt_Name"), noiW,
                                                MillDesign.SlotNameH, bx + padH, nameTop),
                "", MillDesign.SlotNameFont, MillSpriteFactory.Hex(MillDesign.CTextBrown),
                TextAlignmentOptions.Top, true);

            // HTML 660 chấm đỏ 10×10 bottom 35 right 5
            RectTransform rd = MillUI.BR(MillUI.Node(card, "RedDot"),
                MillDesign.RedDotSize, MillDesign.RedDotSize,
                MillDesign.RedDotRight, MillDesign.RedDotBottom);
            MillUI.Img(rd, sk.redDot, Color.white);
            rd.gameObject.SetActive(false);

            // ═══ WIRE ═══════════════════════════════════════════════════════════
            MillWiring.W(so, "txtIndex", txtIndex, rep, own);
            MillWiring.W(so, "txtName", txtName, rep, own);
            MillWiring.W(so, "txtTimer", txtTimer, rep, own);
            MillWiring.W(so, "txtGemCost", txtGemCost, rep, own);
            MillWiring.W(so, "txtLockLabel", txtLockLabel, rep, own);
            MillWiring.W(so, "txtLockLevelValue", txtLvl, rep, own);
            MillWiring.W(so, "txtSpeedUpCost", txtSpeed, rep, own);
            MillWiring.W(so, "imgBg", bg, rep, own);
            MillWiring.W(so, "imgIcon", imgIcon, rep, own);
            MillWiring.W(so, "imgProgressFill", fill, rep, own);
            MillWiring.W(so, "imgLockIcon", imgLock, rep, own);
            MillWiring.W(so, "rootRunning", rRun.gameObject, rep, own);
            MillWiring.W(so, "rootReady", rRdy.gameObject, rep, own);
            MillWiring.W(so, "rootEmpty", rEmp.gameObject, rep, own);
            MillWiring.W(so, "rootUnlockGem", rGem.gameObject, rep, own);
            MillWiring.W(so, "rootLockedLevel", rLvl.gameObject, rep, own);
            MillWiring.W(so, "btnCollect", btnCollect, rep, own);
            MillWiring.W(so, "btnSpeedUp", btnSpeed, rep, own);
            MillWiring.W(so, "btnUnlockGem", btnUnlock, rep, own);
            MillWiring.W(so, "redDot", rd.gameObject, rep, own);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Chỉ để lại root "Trống" bật sẵn — MillPopupUI đặt lại mode ở frame đầu mỗi
            // lần Open (nó xoá hàng rào `_modeDaVe`), nên trạng thái ban đầu chỉ để prefab
            // trông sạch trong Editor, không ảnh hưởng runtime.
            rRun.gameObject.SetActive(false);
            rRdy.gameObject.SetActive(false);
            rEmp.gameObject.SetActive(true);
            rGem.gameObject.SetActive(false);
            rLvl.gameObject.SetActive(false);

            return slot;
        }

        /// <summary>HTML 432 `.slot-icon-bg` 50×50 nền #F6E7D1 — bản copy trong từng root.</summary>
        private static void DiaKem(RectTransform root, MillSkin sk, float iconTop)
        {
            MillUI.Img(MillUI.TC(MillUI.Node(root, "Icon_Circle"),
                MillDesign.SlotIconBg, MillDesign.SlotIconBg, iconTop), sk.circleSlot, Color.white);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  MillCardBuilder — PREFAB CARD CÔNG THỨC (HTML 219 `.recipe-card`)
    //
    //  ⚠ CẠM BẪY #2: dựng TOÀN BỘ trong SCENE rồi mới SaveAsPrefabAsset. KHÔNG bao giờ
    //  SetParent vào transform nằm trong prefab asset — Unity chặn và object rơi ra gốc
    //  scene thành rác (đã xảy ra thật, để lại 4 object rác trong SCN_Farm).
    // ═══════════════════════════════════════════════════════════════════════════════
    internal static class MillCardBuilder
    {
        public static MillRecipeCardUI TaoPrefabCard(MillReport rep)
        {
            MillSkin sk = MillSkin.Tao(rep);
            MillUI.BaoDamThuMuc(MillPopupBuilderTool.PrefabFolder, rep);

            GameObject temp = null;
            bool undoCu = MillUI.DungUndo;
            MillUI.DungUndo = false;      // object tạm sẽ bị xoá ngay ⇒ không ghi vào Undo
            try
            {
                temp = new GameObject("MillRecipeCard", typeof(RectTransform));
                var rt = temp.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(MillDesign.CardW, MillDesign.CardH);
                rt.localScale = Vector3.one;

                float bx = MillDesign.CardBorder, pd = MillDesign.CardPad;
                float ins = bx + pd;                                     // 10
                float noiW = MillDesign.CardW - ins * 2f;                // 209

                Image bg = MillUI.Img(rt, sk.cardInactive, Color.white, true);
                var card = MillUI.Comp<MillRecipeCardUI>(temp);
                MillUI.Comp<CanvasGroup>(temp);
                Button btn = MillUI.Btn(bg);

                // Chiều cao cố định cho VerticalLayoutGroup của danh sách.
                MillUI.CoDinh(rt, MillDesign.CardW, MillDesign.CardH);

                // ── HTML 232 `.recipe-icon-circle` 50×50 nền #EEDABB ──────────────
                RectTransform ic = MillUI.TL(MillUI.Node(rt, "Icon_Circle"),
                    MillDesign.CardIconCircle, MillDesign.CardIconCircle, ins, ins);
                MillUI.Img(ic, sk.circleCard, Color.white);
                RectTransform ii = MillUI.CC(MillUI.Node(ic, "Img_Icon"), 38f, 38f, 0f, 0f);
                Image imgIcon = MillUI.Img(ii, null, Color.white);
                imgIcon.enabled = false;

                // ── HTML 239-240 tên + thời gian ủ, cách icon 10px ────────────────
                float tx = ins + MillDesign.CardIconCircle + MillDesign.CardInfoGap;   // 70
                var txtName = MillUI.Txt(MillUI.TL(MillUI.Node(rt, "Txt_Name"),
                        MillDesign.CardW - tx - ins - 4f, 20f, tx, ins + 2f),
                    "Cám cho gà", MillDesign.CardNameFont,
                    MillSpriteFactory.Hex(MillDesign.CTextBrown), TextAlignmentOptions.Left);

                var txtTime = MillUI.Txt(MillUI.TL(MillUI.Node(rt, "Txt_BrewTime"),
                        MillDesign.CardW - tx - ins - 4f, 16f, tx, ins + 24f),
                    "Ủ 2p00", MillDesign.CardTimeFont,
                    MillSpriteFactory.Hex(MillDesign.CTextLight), TextAlignmentOptions.Left);

                // ── HTML 251 `.cost-row` margin-top 8, gap 5 ─────────────────────
                float costTop = ins + MillDesign.CardIconCircle + MillDesign.CardCostMt;   // 68
                RectTransform row = MillUI.TL(MillUI.Node(rt, "Cost_Row"), noiW,
                    MillDesign.CardChipH, ins, costTop);
                var h = MillUI.HangNgang(row, MillDesign.CardCostGap, 0f, 0f, 0f, 0f, false);
                h.childAlignment = TextAnchor.MiddleLeft;

                Image ing1, ing2; TMP_Text tIng1, tIng2;
                Chip(row, "Chip_1", sk, out ing1, out tIng1);
                Chip(row, "Chip_2", sk, out ing2, out tIng2);

                // ── HTML 243 `.animal-tag` top −8 right 8, viên thuốc trắng ──────
                RectTransform tag = MillUI.TR(MillUI.Node(rt, "Badge_Animal"), 60f,
                    MillDesign.CardTagH, MillDesign.CardTagRight, MillDesign.CardTagTop);
                MillUI.Img(tag, sk.tagWhite, Color.white);
                MillUI.HangNgang(tag, 4f, 8f, 8f, 0f, 0f, true);
                RectTransform tgi = MillUI.Node(tag, "Img_Badge");
                Image imgBadge = MillUI.Img(tgi, null, Color.white);
                imgBadge.enabled = false;
                MillUI.CoDinh(tgi, 12f, 12f);
                RectTransform tgt = MillUI.Node(tag, "Txt_Badge");
                var txtBadge = MillUI.Txt(tgt, "Gà", MillDesign.CardTagFont,
                    MillSpriteFactory.Hex(MillDesign.CTextBrown), TextAlignmentOptions.Center);
                MillUI.CoDinhCao(tgt, 12f);

                // ── HTML 271 `.recipe-lock-overlay` top 25 left 15 ───────────────
                RectTransform ov = MillUI.Stretch(MillUI.Node(rt, "Lock_Overlay"), 0, 0, 0, 0);
                Image glyph = MillUI.Img(MillUI.TL(MillUI.Node(ov, "Big_Lock"),
                        MillDesign.CardLockGlyph, MillDesign.CardLockGlyph,
                        MillDesign.CardLockLeft, MillDesign.CardLockTop),
                    sk.lockGlyph, new Color(0.16f, 0.15f, 0.13f, 0.95f));
                glyph.raycastTarget = false;
                var txtLock = MillUI.Txt(MillUI.TL(MillUI.Node(ov, "Txt_LockText"), 150f, 18f,
                        4f, MillDesign.CardLockTop + MillDesign.CardLockGlyph + 5f),
                    "Mở ở cấp 14", MillDesign.CardLockTextFont,
                    MillSpriteFactory.Hex("#423E37"), TextAlignmentOptions.Center);
                ov.gameObject.SetActive(false);

                // ── WIRE (làm TRƯỚC khi lưu prefab) ─────────────────────────────
                var so = new SerializedObject(card);
                const string own = "MillRecipeCardUI(prefab)";
                MillWiring.W(so, "imgBg", bg, rep, own);
                MillWiring.W(so, "imgIcon", imgIcon, rep, own);
                MillWiring.W(so, "imgBadge", imgBadge, rep, own);
                MillWiring.W(so, "imgIng1", ing1, rep, own);
                MillWiring.W(so, "imgIng2", ing2, rep, own);
                MillWiring.W(so, "txtName", txtName, rep, own);
                MillWiring.W(so, "txtBrewTime", txtTime, rep, own);
                MillWiring.W(so, "txtBadge", txtBadge, rep, own);
                MillWiring.W(so, "txtIng1", tIng1, rep, own);
                MillWiring.W(so, "txtIng2", tIng2, rep, own);
                MillWiring.W(so, "btnSelect", btn, rep, own);
                MillWiring.W(so, "spriteActive", sk.cardActive, rep, own);
                MillWiring.W(so, "spriteInactive", sk.cardInactive, rep, own);
                MillWiring.W(so, "spriteLocked", sk.cardLocked, rep, own);
                MillWiring.W(so, "lockOverlay", ov.gameObject, rep, own);
                MillWiring.W(so, "txtLockText", txtLock, rep, own);
                so.ApplyModifiedPropertiesWithoutUndo();

                bool coSan = File.Exists(MillPopupBuilderTool.CardPrefabPath);
                GameObject asset = PrefabUtility.SaveAsPrefabAsset(temp,
                    MillPopupBuilderTool.CardPrefabPath, out bool ok);
                if (!ok || asset == null)
                {
                    rep.Loi("Lưu prefab card thất bại: " + MillPopupBuilderTool.CardPrefabPath);
                    return null;
                }
                rep.Ok((coSan ? "Cập nhật (giữ GUID)" : "Tạo mới") + " prefab card: " +
                       MillPopupBuilderTool.CardPrefabPath);

                var comp = asset.GetComponent<MillRecipeCardUI>();
                if (comp == null) rep.Loi("Prefab card vừa lưu KHÔNG có MillRecipeCardUI.");
                return comp;
            }
            finally
            {
                // Dọn object tạm trong MỌI trường hợp — không để lại rác trong scene.
                if (temp != null) Object.DestroyImmediate(temp);
                MillUI.DungUndo = undoCu;
            }
        }

        /// <summary>HTML 252 `.cost-chip` trắng viền 2 #82C94F radius 10, icon + "x3".</summary>
        private static void Chip(RectTransform row, string ten, MillSkin sk,
                                 out Image img, out TMP_Text txt)
        {
            RectTransform chip = MillUI.Node(row, ten);
            chip.sizeDelta = new Vector2(46f, MillDesign.CardChipH);
            MillUI.Img(chip, sk.chipGreen, Color.white);
            // KHÔNG ContentSizeFitter ở đây: chip nằm TRONG một LayoutGroup (Cost_Row) đang
            // childControlWidth ⇒ hai thứ cùng đặt sizeDelta sẽ giành nhau. HorizontalLayoutGroup
            // của chính chip đã cung cấp preferredWidth nên cha tự co đúng bề rộng.
            MillUI.HangNgang(chip, 4f, 6f, 6f, 0f, 0f, false);
            MillUI.CoDinhCao(chip, MillDesign.CardChipH);

            RectTransform ic = MillUI.Node(chip, "Img_Ing");
            img = MillUI.Img(ic, null, Color.white);
            img.enabled = false;
            MillUI.CoDinh(ic, 11f, 11f);

            RectTransform tx = MillUI.Node(chip, "Txt_Ing");
            txt = MillUI.Txt(tx, "", MillDesign.CardChipFont,
                MillSpriteFactory.Hex(MillDesign.CChipGreenTx), TextAlignmentOptions.Center);
            MillUI.CoDinhCao(tx, 13f);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  MillWiring — GÁN FIELD CONTRACT
    //
    //  Toàn bộ field của MillPopupUI / MillSlotUI / MillRecipeCardUI là
    //  `[SerializeField] private` ⇒ KHÔNG gán trực tiếp được từ code editor. Phải đi qua
    //  SerializedObject.FindProperty(tên field). Ưu điểm: Dev A đổi tên field là tool báo
    //  "KHÔNG có field này" ngay ở lệnh 3, thay vì âm thầm để null.
    // ═══════════════════════════════════════════════════════════════════════════════
    internal static class MillWiring
    {
        /// <summary>Gán một field object. Ghi kết quả (được / không được + lý do) vào báo cáo.</summary>
        public static bool W(SerializedObject so, string field, Object value,
                             MillReport rep, string owner)
        {
            if (so == null) { rep.ChuaWire(owner + "." + field, "không có SerializedObject"); return false; }

            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                rep.ChuaWire(owner + "." + field,
                    "SCRIPT KHÔNG CÓ FIELD NÀY (Dev A đổi tên?) — kiểm tra lại contract");
                return false;
            }
            if (p.propertyType != SerializedPropertyType.ObjectReference)
            {
                rep.ChuaWire(owner + "." + field, "field không phải tham chiếu object");
                return false;
            }
            if (value == null)
            {
                rep.ChuaWire(owner + "." + field, "tool không dựng được node/asset tương ứng");
                return false;
            }
            p.objectReferenceValue = value;
            rep.DaWire(owner + "." + field + " = " + value.name);
            return true;
        }

        /// <summary>Gán một field mảng object (slots[], beltItems[]).</summary>
        public static bool WArr(SerializedObject so, string field, Object[] values,
                                MillReport rep, string owner)
        {
            SerializedProperty p = so != null ? so.FindProperty(field) : null;
            if (p == null)
            {
                rep.ChuaWire(owner + "." + field, "SCRIPT KHÔNG CÓ FIELD NÀY");
                return false;
            }
            if (!p.isArray)
            {
                rep.ChuaWire(owner + "." + field, "field không phải mảng");
                return false;
            }
            if (values == null || values.Length == 0)
            {
                rep.ChuaWire(owner + "." + field, "tool không dựng được phần tử nào");
                return false;
            }

            p.arraySize = values.Length;
            int thieu = 0;
            for (int i = 0; i < values.Length; i++)
            {
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                if (values[i] == null) thieu++;
            }
            if (thieu > 0)
                rep.ChuaWire(owner + "." + field, thieu + "/" + values.Length + " phần tử bị null");
            else
                rep.DaWire(owner + "." + field + "[" + values.Length + "]");
            return thieu == 0;
        }

        /// <summary>
        /// Đặt một field bool bất kể public hay private (vd `RotatingGear.playOnStart` —
        /// file có sẵn của dự án, tool không được sửa nên không biết chắc mức truy cập).
        /// </summary>
        public static void DatBool(Component c, string field, bool giaTri, MillReport rep)
        {
            if (c == null) { rep.ChuaWire("?." + field, "component null"); return; }
            var so = new SerializedObject(c);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                rep.Canh(c.GetType().Name + "." + field + ": không tìm thấy field " +
                         "(có thể tên khác) — kiểm tra tay trong Inspector.");
                return;
            }
            p.boolValue = giaTri;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void DatMau(SerializedObject so, string field, Color mau)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p != null && p.propertyType == SerializedPropertyType.Color) p.colorValue = mau;
        }

        /// <summary>Wire toàn bộ MillPopupUI.</summary>
        public static void WirePopup(GameObject root, MillBuilt b, MillRecipeCardUI cardPrefab,
                                     MillReport rep)
        {
            var ui = root.GetComponent<MillPopupUI>();
            if (ui == null) { rep.Loi("Root không có MillPopupUI ⇒ không wire được gì."); return; }

            var so = new SerializedObject(ui);
            const string own = "MillPopupUI";

            // config: dùng lại asset đã có, KHÔNG tự tạo ở lệnh này (lệnh 2 lo việc đó).
            var cfg = AssetDatabase.LoadAssetAtPath<MillConfig>(MillPopupBuilderTool.ConfigPath);
            if (cfg != null) W(so, "config", cfg, rep, own);
            else rep.ChuaWire(own + ".config",
                    "chưa có " + MillPopupBuilderTool.ConfigPath + " — chạy lệnh 2 rồi gán");

            W(so, "popupRoot", b.popupRoot, rep, own);
            W(so, "recipeCardPrefab", cardPrefab, rep, own);
            W(so, "recipeContainer", b.recipeContainer, rep, own);
            WArr(so, "slots", b.slots, rep, own);
            W(so, "gearLarge", b.gearLarge, rep, own);
            W(so, "gearSmall", b.gearSmall, rep, own);
            W(so, "belt", b.belt, rep, own);
            WArr(so, "beltItems", b.beltItems, rep, own);
            W(so, "txtTitle", b.txtTitle, rep, own);
            W(so, "txtStatusBadge", b.txtStatusBadge, rep, own);
            W(so, "txtSlotSummary", b.txtSlotSummary, rep, own);
            W(so, "txtGemBalance", b.txtGemBalance, rep, own);
            W(so, "txtMainButton", b.txtMainButton, rep, own);
            W(so, "txtInputBubble", b.txtInputBubble, rep, own);
            W(so, "txtOutputTag", b.txtOutputTag, rep, own);
            W(so, "imgStatusDot", b.imgStatusDot, rep, own);
            W(so, "imgOutputIcon", b.imgOutputIcon, rep, own);
            W(so, "imgInputIcon", b.imgInputIcon, rep, own);
            W(so, "btnClose", b.btnClose, rep, own);
            W(so, "btnMain", b.btnMain, rep, own);
            W(so, "toastRoot", b.toastRoot, rep, own);
            W(so, "toastText", b.toastText, rep, own);
            W(so, "imgMainButtonBg", b.imgMainButtonBg, rep, own);

            // Màu tuỳ chọn — gán lại đúng hex CSS để không phụ thuộc default của script.
            DatMau(so, "mauNutBamDuoc", MillSpriteFactory.Hex(MillDesign.CBtnGreen));   // HTML 27
            DatMau(so, "mauNutKhoa",    MillSpriteFactory.Hex(MillDesign.CLocked));     // HTML 29
            DatMau(so, "mauDotDangXay", MillSpriteFactory.Hex(MillDesign.CDotGreen));   // HTML 333
            DatMau(so, "mauDotRanh",    MillSpriteFactory.Hex("#BAB1A4"));              // xám (tự quyết)

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  MillDataBuilder — 4 CÔNG THỨC + CONFIG ĐÚNG NHƯ VIDEO
    //
    //  ⚠ itemId / outputItemId để CHUỖI RỖNG kèm TODO. Bịa id kho là lỗi tệ nhất có thể:
    //  MillInventoryBridge sẽ luôn báo "thiếu nguyên liệu" mà không ai hiểu tại sao, còn
    //  MillRecipeCardUI.DatChipNguyenLieu (dòng ~205) ẨN chip khi itemId rỗng nên chủ dự án
    //  THẤY NGAY là chưa điền.
    // ═══════════════════════════════════════════════════════════════════════════════
    internal static class MillDataBuilder
    {
        private const string TODO = "TODO: điền itemId thật trong kho (WarehouseManager)";

        public static void Tao(MillReport rep)
        {
            MillUI.BaoDamThuMuc("Assets/_Game/Farm/Data", rep);
            MillUI.BaoDamThuMuc(MillPopupBuilderTool.DataFolder, rep);

            // HTML 497-513 card 1 — "Cám cho gà", badge "Gà", Ủ 2p00, chip x3 + x2
            MillRecipeData ga = Recipe(rep, "MillRecipe_CamGa", "cam_ga", "Cám cho gà", "Gà",
                                       2f, 1, new[] { 3, 2 });
            // HTML 520-533 card 2 — "Cám cho heo", badge "Heo", Ủ 4p00, chip x4 + x3
            MillRecipeData heo = Recipe(rep, "MillRecipe_CamHeo", "cam_heo", "Cám cho heo", "Heo",
                                        4f, 1, new[] { 4, 3 });
            // HTML 535-548 card 3 — "Cỏ trộn cho bò", badge "Bò", Ủ 6p00, chip x5 + x4
            MillRecipeData bo = Recipe(rep, "MillRecipe_CoTronBo", "co_tron_bo", "Cỏ trộn cho bò",
                                       "Bò", 6f, 1, new[] { 5, 4 });
            // HTML 551-571 card 4 KHOÁ — "Cám cho bò sữa", badge "Bò sữa", Ủ 10p00,
            // chip x6 + x6 + x4, chữ khoá "Mở ở cấp 14" ⇒ unlockLevel = 14.
            MillRecipeData sua = Recipe(rep, "MillRecipe_CamBoSua", "cam_bo_sua", "Cám cho bò sữa",
                                        "Bò sữa", 10f, 14, new[] { 6, 6, 4 });

            // ── Config ────────────────────────────────────────────────────────────
            bool moi;
            MillConfig cfg = LayHoacTao<MillConfig>(MillPopupBuilderTool.ConfigPath, out moi);
            if (cfg == null) { rep.Loi("Không tạo được MillConfig.asset"); return; }

            cfg.title = "MÁY XAY THỨC ĂN";               // HTML 476 `.ribbon-text`
            cfg.slotCount = 5;                            // HTML 653-698: 5 `.slot-card`
            cfg.slotsUnlockedAtStart = 3;                 // video: 3 slot mở sẵn
            cfg.gemCostUnlockSlot = 15;                   // video: 15 kim cương / slot
            cfg.levelRequiredLastSlot = 18;               // HTML 697 "Cấp 18"
            cfg.gearLargeDegPerSec = 90f;                 // HTML 621 dur=4s  ⇒ 360/4
            cfg.gearSmallDegPerSec = 144f;                // HTML 630 dur=2.5s ⇒ 360/2.5
            cfg.beltScrollPxPerSec = 42f;                 // HTML 367
            cfg.beltStripePeriodPx = 30f;                 // HTML 355
            cfg.itemCycleSeconds = 3f;                    // HTML 373
            cfg.itemTravelPx = 230f;                      // HTML 380
            cfg.itemStaggerSeconds = 1.5f;                // HTML 376
            // Video slot #3 còn 1p56 (116s) mà nút ghi "x6" ⇒ ceil(116/60)=2 phút × 3 = 6.
            cfg.gemPerMinuteSpeedUp = 3;
            cfg.recipes = new[] { ga, heo, bo, sua };

            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            rep.Ok((moi ? "Tạo mới" : "Cập nhật (giữ GUID)") + " " + MillPopupBuilderTool.ConfigPath);
            rep.Can("ĐIỀN itemId cho 4 công thức: mỗi MillIngredient.itemId và " +
                    "outputItemId đang là chuỗi rỗng (" + TODO + "). Chip nguyên liệu trên card " +
                    "CỐ Ý bị ẩn cho tới khi bạn điền — đó là cách tool báo 'chưa xong'.");
            rep.Can("Gán icon: MillRecipeData.icon (đĩa tròn), animalBadgeIcon (icon con vật), " +
                    "ingredients[].icon (chip x3/x2).");
            rep.Can("Gán MillConfig.asset vào field `config` của MillPopupUI trong scene.");
            rep.Can("gemPerMinuteSpeedUp = 3 là con số suy từ video (1p56 → x6). " +
                    "Designer chốt lại nếu khác.");
        }

        private static MillRecipeData Recipe(MillReport rep, string tenFile, string id,
            string ten, string badge, float phut, int capMo, int[] soLuong)
        {
            string path = MillPopupBuilderTool.DataFolder + "/" + tenFile + ".asset";
            bool moi;
            MillRecipeData r = LayHoacTao<MillRecipeData>(path, out moi);
            if (r == null) { rep.Loi("Không tạo được " + path); return null; }

            r.recipeId = id;
            r.displayName = ten;
            r.animalTag = badge;
            r.brewMinutes = phut;
            r.unlockLevel = capMo;
            r.outputAmount = 1;

            // ⚠ ĐỪNG bịa id kho. Giữ rỗng để chủ dự án điền.
            if (string.IsNullOrEmpty(r.outputItemId)) r.outputItemId = string.Empty;

            // Giữ nguyên itemId/icon người dùng đã điền; chỉ đặt lại SỐ LƯỢNG theo video.
            var ds = new MillIngredient[soLuong.Length];
            for (int i = 0; i < soLuong.Length; i++)
            {
                MillIngredient cu = (r.ingredients != null && i < r.ingredients.Length)
                                    ? r.ingredients[i] : null;
                ds[i] = new MillIngredient
                {
                    itemId = cu != null ? cu.itemId : string.Empty,
                    amount = soLuong[i],
                    icon = cu != null ? cu.icon : null
                };
            }
            r.ingredients = ds;

            EditorUtility.SetDirty(r);
            rep.Ok((moi ? "Tạo mới" : "Cập nhật (giữ GUID)") + " " + tenFile +
                   " — \"" + ten + "\" badge \"" + badge + "\" ủ " +
                   MillTimeFormat.PhutGiay(Mathf.RoundToInt(phut * 60f)) +
                   " nguyên liệu " + MoTaSoLuong(soLuong) +
                   (capMo > 1 ? " (KHOÁ tới cấp " + capMo + ")" : ""));
            return r;
        }

        private static string MoTaSoLuong(int[] n)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < n.Length; i++) { if (i > 0) sb.Append(" + "); sb.Append("x").Append(n[i]); }
            return sb.ToString();
        }

        /// <summary>Load asset, chưa có thì tạo. Có rồi thì SỬA TẠI CHỖ để giữ nguyên GUID.</summary>
        private static T LayHoacTao<T>(string path, out bool moi) where T : ScriptableObject
        {
            T a = AssetDatabase.LoadAssetAtPath<T>(path);
            moi = false;
            if (a != null) return a;

            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            moi = true;
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  MillAudit — LỆNH 3, CHẠY KHÔ. KHÔNG sửa scene, KHÔNG ghi asset.
    //
    //  Rủi ro số 1 của việc dựng UI bằng code là "một field im lặng để null" — popup vẫn
    //  chạy, chỉ thiếu một mảnh mà không ai báo. Lệnh này soát TỪNG field của contract.
    // ═══════════════════════════════════════════════════════════════════════════════
    internal static class MillAudit
    {
        /// <summary>Tên sprite tool sẽ TÌM, kèm mô tả cách vẽ thay thế nếu không có.</summary>
        private static readonly string[,] BangSprite =
        {
            { "popup_board",         "BẮT BUỘC (ui_mill_assets) — không vẽ thay thế" },
            { "panel_inner",         "BẮT BUỘC (ui_mill_assets) — không vẽ thay thế" },
            { "ribbon_header",       "BẮT BUỘC (ui_mill_assets) — không vẽ thay thế" },
            { "recipe_card_active",  "BẮT BUỘC (ui_mill_assets)" },
            { "recipe_card_inactive","BẮT BUỘC (ui_mill_assets)" },
            { "recipe_card_locked",  "BẮT BUỘC (ui_mill_assets)" },
            { "btn_green",           "BẮT BUỘC (ui_mill_assets)" },
            { "btn_blue",            "BẮT BUỘC (ui_mill_assets)" },
            { "machine_body",        "BẮT BUỘC (ui_mill_assets)" },
            { "gear_large",          "BẮT BUỘC (ui_mill_assets)" },
            { "gear_small",          "BẮT BUỘC (ui_mill_assets)" },
            { "btn_close",           "thiếu ⇒ VẼ #D45B4B viền 3 trắng r12 (HTML 118)" },
            { "progress_track",      "thiếu ⇒ VẼ #D9D9D9 r7 (HTML 440)" },
            { "progress_fill",       "thiếu ⇒ VẼ #82C94F r7 (HTML 444)" },
            { "circle_preview",      "thiếu ⇒ VẼ đĩa #EEDABB 50px (HTML 232)" },
            { "slot_normal",         "thiếu ⇒ VẼ trắng viền 2 #E4D5C2 r12 (HTML 422)" },
            { "slot_empty",          "thiếu ⇒ VẼ #D9CDB9 viền 2 #C2B6A3 r12 (HTML 458)" },
            { "shop_toast",          "thiếu ⇒ VẼ #3F2C21 a.94 viền #231812 r14 (HTML không có)" },
            { "shop_currency_chip",  "thiếu ⇒ VẼ trắng viền 2 #D6B798 r20 (HTML 170)" },
            { "shop_lock_badge",     "TUỲ CHỌN — không dùng nếu thiếu" },
            { "kimcuong",            "thiếu ⇒ VẼ hình thoi #40A4E5 (HTML 487)" },
            { "tab_active",          "CỐ Ý KHÔNG DÙNG — dự án đã bỏ hệ tab" },
            { "tab_inactive",        "CỐ Ý KHÔNG DÙNG — dự án đã bỏ hệ tab" },
            { "conveyor_base",       "CỐ Ý KHÔNG DÙNG — bake sẵn 4 bánh lăn, 9-slice sẽ giãn " +
                                     "khoảng cách bánh; HTML 345/358 tách hai lớp offset khác nhau" },
        };

        /// <summary>Sprite tool LUÔN vẽ (không folder nào có).</summary>
        private static readonly string[] LuonVe =
        {
            "mill_sky          gradient #E6F3E6 → #CBE6CF, bo 2 góc trên (HTML 315)",
            "mill_ground       #B48D64 + sọc dọc #A68058 chu kỳ 30px, bo 2 góc dưới (HTML 320)",
            "mill_anim_frame   viền 3px #D6B798 r15, lòng trong suốt (HTML 307)",
            "mill_panel_white  trắng viền 3 #D6B798 r15 (HTML 192)",
            "mill_badge        #F4E2C7 viền 2 #C4A882 r20 (HTML 327)",
            "mill_dot          đĩa TRẮNG 12px (MillPopupUI tự tô #62E15D / xám) (HTML 333)",
            "mill_bubble_in    #BA9054 viền 3 #9A723D r15 (HTML 336)",
            "mill_belt_base    #3F2C21 viền 2 #231812 r15 (HTML 345)",
            "mill_belt_stripes TEXTURE 42×42 sọc chéo #2A1D15, Wrap = REPEAT (HTML 355 + 367)",
            "mill_wheel        đĩa #1C120C viền 2 #4D3728 14px (HTML 362)",
            "mill_out_bubble   đĩa #F8E6CA viền 4 #DFB980 80px (HTML 393)",
            "mill_tag_white    trắng viền 2 #D6B798 r10 (HTML 243 / 400)",
            "mill_chip_green   trắng viền 2 #82C94F r10 (HTML 252)",
            "mill_btn_tintable TRẮNG r12 + dải đáy xám (MillPopupUI tô màu nút lớn) (HTML 285)",
            "mill_locked_pill  #AFA28F r15 (HTML 461)",
            "mill_lock_circle  đĩa #645747 40px (HTML 459)",
            "mill_glyph_lock   ổ khoá trắng (HTML 459/277 dùng emoji 🔒)",
            "mill_glyph_x      dấu X trắng (HTML 126 dùng ký tự ✖)",
            "mill_reddot       đĩa #FF4A4A viền 2 trắng 10px (HTML 660)",
            "mill_rivet        đĩa #DDAE80 viền 2 #8B5933 16px (HTML 63)",
            "mill_circle_slot  đĩa #F6E7D1 50px (HTML 432)",
            "mill_item_grain   PLACEHOLDER bó cỏ #D9A85B (HTML 605 dùng emoji 🌾)",
        };

        public static void ChayKho(MillReport rep)
        {
            MillSpriteFactory.Reset();
            MillSkin.XoaCache();

            // ── 1. SPRITE ────────────────────────────────────────────────────────
            int coSan = 0, thieu = 0;
            for (int i = 0; i < BangSprite.GetLength(0); i++)
            {
                string ten = BangSprite[i, 0], ghiChu = BangSprite[i, 1];
                string path = MillSpriteFactory.TimDuongDan(ten);
                if (path != null) { rep.Sprite("CÓ      " + ten + "  ← " + path); coSan++; }
                else { rep.Sprite("THIẾU   " + ten + "  ⇒ " + ghiChu); thieu++; }

                if (path == null && ghiChu.StartsWith("BẮT BUỘC"))
                    rep.Loi("Thiếu sprite BẮT BUỘC '" + ten + "'. Kiểm tra thư mục " +
                            "Assets/Assetsgame/popup/ui_mill_assets/generated_sprites.");
            }
            foreach (string s in LuonVe) rep.Sprite("SẼ VẼ   " + s);
            rep.Ok("Sprite: " + coSan + " tìm được, " + thieu + " thiếu, " +
                   LuonVe.Length + " sẽ vẽ bằng code.");

            // ── 2. FONT ──────────────────────────────────────────────────────────
            MillSpriteFactory.TimFont(rep);

            // ── 3. CANVAS ────────────────────────────────────────────────────────
            MillPopupBuilderTool.TimCanvasUI(rep);

            // ── 4. DATA ──────────────────────────────────────────────────────────
            var cfg = AssetDatabase.LoadAssetAtPath<MillConfig>(MillPopupBuilderTool.ConfigPath);
            if (cfg == null)
                rep.Canh("Chưa có " + MillPopupBuilderTool.ConfigPath + " — chạy lệnh 2.");
            else
            {
                string loi;
                if (!cfg.KiemTraHopLe(out loi)) rep.Loi("MillConfig không hợp lệ: " + loi);
                else rep.Ok("MillConfig hợp lệ: " + (cfg.recipes != null ? cfg.recipes.Length : 0) +
                            " công thức, " + cfg.slotCount + " slot.");

                if (cfg.recipes != null)
                    foreach (MillRecipeData r in cfg.recipes)
                    {
                        if (r == null) { rep.Canh("MillConfig.recipes có ô NULL."); continue; }
                        if (string.IsNullOrEmpty(r.outputItemId))
                            rep.Canh("Công thức '" + r.displayName + "': outputItemId RỖNG — " +
                                     "bấm THU sẽ không cộng được gì vào kho.");
                        if (r.ingredients != null)
                            for (int i = 0; i < r.ingredients.Length; i++)
                                if (r.ingredients[i] == null ||
                                    string.IsNullOrEmpty(r.ingredients[i].itemId))
                                    rep.Canh("Công thức '" + r.displayName + "': ingredients[" + i +
                                             "].itemId RỖNG — chip bị ẩn và luôn coi là thiếu NL.");
                        if (r.icon == null)
                            rep.Canh("Công thức '" + r.displayName + "': chưa có icon.");
                    }
            }

            // ── 5. PREFAB CARD ───────────────────────────────────────────────────
            var cardAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
                MillPopupBuilderTool.CardPrefabPath);
            if (cardAsset == null)
                rep.Canh("Chưa có prefab card " + MillPopupBuilderTool.CardPrefabPath +
                         " — chạy lệnh 1.");
            else
            {
                var c = cardAsset.GetComponent<MillRecipeCardUI>();
                if (c == null) rep.Loi("Prefab card không có MillRecipeCardUI.");
                else SoatFields(new SerializedObject(c), "MillRecipeCardUI(prefab)", new[]
                {
                    "imgBg","imgIcon","imgBadge","imgIng1","imgIng2","txtName","txtBrewTime",
                    "txtBadge","txtIng1","txtIng2","btnSelect","spriteActive","spriteInactive",
                    "spriteLocked","lockOverlay","txtLockText"
                }, rep);
            }

            // ── 6. POPUP TRONG SCENE ─────────────────────────────────────────────
            var ui = Object.FindFirstObjectByType<MillPopupUI>(FindObjectsInactive.Include);
            if (ui == null)
            {
                // LỖI chứ không phải cảnh báo: thiếu MillPopupUI trong scene thì bấm vào
                // công trình KHÔNG THỂ mở popup, dù prefab MillPopup_Root có wire đủ 100%.
                // Prefab là ASSET, nó không tự có mặt trong scene.
                rep.Loi("SCENE ĐANG MỞ KHÔNG CÓ MillPopupUI ⇒ bấm vào máy xay sẽ KHÔNG mở được popup. Chạy lệnh 0 (LAM TAT CA) để dựng lại và lưu scene.");
            }
            else
            {
                SoatFields(new SerializedObject(ui), "MillPopupUI", new[]
                {
                    "config","popupRoot","recipeCardPrefab","recipeContainer","slots","gearLarge",
                    "gearSmall","belt","beltItems","txtTitle","txtStatusBadge","txtSlotSummary",
                    "txtGemBalance","txtMainButton","txtInputBubble","txtOutputTag","imgStatusDot",
                    "imgOutputIcon","imgInputIcon","btnClose","btnMain","toastRoot","toastText",
                    "imgMainButtonBg"
                }, rep);

                SoatSlot(ui, rep);
                SoatAnimation(ui, rep);
            }

            // ── 7. CHUỖI CLICK TRÊN CÔNG TRÌNH ───────────────────────────────────
            //  Ba mắt phải đủ CẢ BA, thiếu một là bấm không ra gì:
            //    (a) object tên chứa "MayThucAn" có trong scene
            //    (b) object đó có MillBuildingClick
            //    (c) object đó có Collider2D (MillBuildingClick dùng OverlapPoint để bắt)
            int soMay = 0, soThieuClick = 0, soThieuCollider = 0;

            foreach (Transform tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                        FindObjectsSortMode.None))
            {
                if (tr == null) continue;
                if (tr.name.IndexOf("MayThucAn", StringComparison.OrdinalIgnoreCase) < 0) continue;

                soMay++;
                GameObject go = tr.gameObject;

                bool coClick    = go.GetComponent<MillBuildingClick>() != null;
                bool coCollider = go.GetComponent<Collider2D>() != null;

                if (!coClick)    soThieuClick++;
                if (!coCollider) soThieuCollider++;

                if (coClick && coCollider)
                {
                    Collider2D col = go.GetComponent<Collider2D>();
                    Bounds bw = col.bounds;   // bounds WORLD — đúng thứ cần để biết vùng bấm thật
                    rep.Ok("Click OK trên '" + go.name + "': MillBuildingClick + " +
                           col.GetType().Name + ", vùng bấm world " +
                           bw.size.x.ToString("0") + " x " + bw.size.y.ToString("0") + " unit.");
                }
                else
                {
                    rep.Loi("'" + go.name + "' thiếu " +
                            (!coClick ? "MillBuildingClick " : "") +
                            (!coCollider ? "Collider2D " : "") +
                            "⇒ bấm vào công trình này KHÔNG mở popup. Chạy lệnh 4 (hoặc lệnh 0).");
                }
            }

            if (soMay == 0)
                rep.Loi("Scene đang mở KHÔNG có object nào tên chứa 'MayThucAn' ⇒ không có gì để bấm. " +
                        "Mở đúng scene SCN_Farm, hoặc kéo công trình máy thức ăn vào scene trước.");
            else
                rep.Ok("Tìm thấy " + soMay + " công trình 'MayThucAn' — thiếu click: " + soThieuClick +
                       ", thiếu collider: " + soThieuCollider + ".");

            // ── 8. SCENE ĐÃ LƯU CHƯA ─────────────────────────────────────────────
            //  Đây là mắt từng làm mất cả buổi: log lệnh 1/lệnh 4 báo xanh nhưng scene chưa
            //  ghi đĩa, Unity reload là mất sạch. Nay lệnh 1/4 tự lưu, nhưng vẫn soát ở đây
            //  để bắt trường hợp người dùng sửa tay xong chưa Ctrl+S.
            UnityEngine.SceneManagement.Scene scDangMo = EditorSceneManager.GetActiveScene();
            if (scDangMo.isDirty)
                rep.Canh("Scene '" + scDangMo.name + "' ĐANG CÓ THAY ĐỔI CHƯA LƯU. Bấm Ctrl+S — " +
                         "nếu không, lần Unity reload scene tới là mất hết.");
            else
                rep.Ok("Scene '" + scDangMo.name + "' đã lưu, không còn thay đổi treo lơ lửng.");

            rep.Can("Mọi dòng 'CHƯA WIRE' ở trên là một mảnh UI sẽ IM LẶNG không hoạt động " +
                    "lúc chạy — sửa hết trước khi bàn giao.");
            rep.Can("Kiểm tra bằng mắt: mở popup trong Play mode, so với full_mill_ui.html " +
                    "mở song song trên trình duyệt ở đúng 1000×680.");
        }

        /// <summary>Soát null của một danh sách field trên SerializedObject.</summary>
        private static void SoatFields(SerializedObject so, string owner, string[] fields,
                                       MillReport rep)
        {
            foreach (string f in fields)
            {
                SerializedProperty p = so.FindProperty(f);
                if (p == null) { rep.ChuaWire(owner + "." + f, "SCRIPT KHÔNG CÓ FIELD NÀY"); continue; }

                if (p.isArray && p.propertyType != SerializedPropertyType.String)
                {
                    if (p.arraySize == 0) { rep.ChuaWire(owner + "." + f, "mảng RỖNG"); continue; }
                    int nul = 0;
                    for (int i = 0; i < p.arraySize; i++)
                        if (p.GetArrayElementAtIndex(i).objectReferenceValue == null) nul++;
                    if (nul > 0) rep.ChuaWire(owner + "." + f, nul + "/" + p.arraySize + " phần tử NULL");
                    else rep.DaWire(owner + "." + f + "[" + p.arraySize + "]");
                    continue;
                }

                if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (p.objectReferenceValue == null) rep.ChuaWire(owner + "." + f, "đang NULL");
                else rep.DaWire(owner + "." + f);
            }
        }

        /// <summary>Soát 5 MillSlotUI + bẫy "imgProgressFill để Simple".</summary>
        private static void SoatSlot(MillPopupUI ui, MillReport rep)
        {
            SerializedProperty ds = new SerializedObject(ui).FindProperty("slots");
            if (ds == null || !ds.isArray) return;

            string[] fields =
            {
                "txtIndex","txtName","txtTimer","txtGemCost","txtLockLabel","txtLockLevelValue",
                "txtSpeedUpCost","imgBg","imgIcon","imgProgressFill","imgLockIcon","rootRunning",
                "rootReady","rootEmpty","rootUnlockGem","rootLockedLevel","btnCollect","btnSpeedUp",
                "btnUnlockGem","redDot"
            };

            for (int i = 0; i < ds.arraySize; i++)
            {
                var slot = ds.GetArrayElementAtIndex(i).objectReferenceValue as MillSlotUI;
                if (slot == null) { rep.ChuaWire("MillPopupUI.slots[" + i + "]", "NULL"); continue; }

                var sso = new SerializedObject(slot);
                SoatFields(sso, "MillSlotUI[" + (i + 1) + "]", fields, rep);

                // ⚠ BẪY IM LẶNG: Image Type phải là Filled/Horizontal/Left, nếu để Simple thì
                //   `fillAmount` KHÔNG có tác dụng và Unity KHÔNG báo lỗi gì.
                var fill = sso.FindProperty("imgProgressFill");
                var img = fill != null ? fill.objectReferenceValue as Image : null;
                if (img == null) continue;

                if (img.type != Image.Type.Filled)
                    rep.Loi("Slot " + (i + 1) + ": imgProgressFill Type = " + img.type +
                            " (phải là Filled) ⇒ thanh tiến độ ĐỨNG YÊN mà không báo lỗi.");
                else if (img.fillMethod != Image.FillMethod.Horizontal)
                    rep.Loi("Slot " + (i + 1) + ": imgProgressFill Fill Method = " +
                            img.fillMethod + " (phải là Horizontal).");
                else if (img.fillOrigin != (int)Image.OriginHorizontal.Left)
                    rep.Loi("Slot " + (i + 1) + ": imgProgressFill Fill Origin phải là Left.");
                else
                    rep.Ok("Slot " + (i + 1) + ": imgProgressFill đúng Filled/Horizontal/Left.");
            }
        }

        /// <summary>Soát bánh răng / băng tải / bó cỏ — nơi hay sai âm thầm nhất.</summary>
        private static void SoatAnimation(MillPopupUI ui, MillReport rep)
        {
            var so = new SerializedObject(ui);

            var belt = so.FindProperty("belt") != null
                     ? so.FindProperty("belt").objectReferenceValue as UIScrollingTexture : null;
            if (belt != null)
            {
                var raw = belt.GetComponent<RawImage>();
                if (raw == null)
                    rep.Loi("UIScrollingTexture KHÔNG nằm trên RawImage ⇒ băng tải không chạy " +
                            "(Image không có uvRect).");
                else if (raw.texture == null)
                    rep.Loi("RawImage băng tải chưa có Texture ⇒ băng tải đứng yên.");
                else if (raw.texture.wrapMode != TextureWrapMode.Repeat)
                    rep.Loi("Texture băng tải Wrap Mode = " + raw.texture.wrapMode +
                            " (phải REPEAT) ⇒ phần tràn bị kéo giãn thành vệt màu.");
                else
                    rep.Ok("Băng tải: RawImage + texture Wrap = Repeat, đúng.");

                if (belt.dungChuKyHoaVan)
                    rep.Canh("UIScrollingTexture.dungChuKyHoaVan đang BẬT. Texture do tool vẽ " +
                             "rộng ĐÚNG 42px = một chu kỳ ⇒ phải TẮT, nếu không băng tải nhanh " +
                             "hơn thiết kế 1.4 lần.");
                if (belt.autoStart)
                    rep.Canh("UIScrollingTexture.autoStart đang BẬT — MillPopupUI điều khiển " +
                             "qua SetRunning nên nên TẮT.");
            }

            foreach (string ten in new[] { "gearLarge", "gearSmall" })
            {
                var p = so.FindProperty(ten);
                var g = p != null ? p.objectReferenceValue as RotatingGear : null;
                if (g == null) continue;
                var gso = new SerializedObject(g);
                var pos = gso.FindProperty("playOnStart");
                if (pos != null && pos.boolValue)
                    rep.Canh("RotatingGear '" + g.name + "'.playOnStart đang BẬT — " +
                             "MillPopupUI gọi Configure/SetRunning lúc Open nên nên TẮT.");
            }

            var items = so.FindProperty("beltItems");
            if (items != null && items.isArray)
            {
                if (items.arraySize != MillDesign.ItemCount)
                    rep.Canh("beltItems có " + items.arraySize + " phần tử. HTML 605-606 chỉ có " +
                             "ĐÚNG " + MillDesign.ItemCount + " bó cỏ (lệch pha 1.5s trên chu kỳ 3s).");
                for (int i = 0; i < items.arraySize; i++)
                {
                    var ci = items.GetArrayElementAtIndex(i).objectReferenceValue as ConveyorItem;
                    if (ci == null) continue;
                    if (ci.GetComponent<Graphic>() == null)
                        rep.Loi("ConveyorItem '" + ci.name + "' không có Image/TMP_Text ⇒ " +
                                "không hiện được và không mờ được.");
                    if (ci.autoStart)
                        rep.Canh("ConveyorItem '" + ci.name + "'.autoStart đang BẬT — nên TẮT.");
                }
            }
        }
    }
}
#endif
