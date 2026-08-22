#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;                 // OfType<Sprite>() — nạp sprite của texture Sprite Mode = Multiple
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// ═══════════════════════════════════════════════════════════════════════════════════════
//  MillPopupBuilderTool — DỰNG TOÀN BỘ POPUP "MÁY XAY THỨC ĂN" BẰNG CODE
//
//  ┌─ NGUỒN THIẾT KẾ DUY NHẤT (ĐỔI NGÀY 21/08 — ĐỌC KỸ) ─────────────────────────────┐
//  │ /tmp/mill/mock.py  →  ảnh duyệt /tmp/mill/prev/mock_new.png                       │
//  │ Bản mockup dựng ĐÚNG 1920×1080 bằng CHÍNH sprite PNG của chủ dự án và font         │
//  │ Baloo2.ttf, đã được chủ dự án XEM VÀ DUYỆT. Mọi con số trong `MillDesign` kèm     │
//  │ chú thích tên biến tương ứng trong mock.py.                                       │
//  │                                                                                   │
//  │ ⚠ KHÔNG DÙNG full_mill_ui.html LÀM NGUỒN SỐ NỮA. File đó được vẽ ở viewport       │
//  │   1000×680, còn CanvasScaler của dự án là 1920×1080 — bản trước copy 1:1 từ nó     │
//  │   nên popup chỉ chiếm 52% bề rộng màn hình và chữ thân bài rơi xuống 10–15px.      │
//  │   Đó là nguyên nhân gốc của điểm 5/10, không phải lỗi chọn art.                    │
//  │   HTML chỉ còn dùng để tra BẢNG MÀU gốc (:root) khi cần suy màu mới.               │
//  │                                                                                   │
//  │ ⚠ HAI CHỖ CỐ Ý LỆCH KHỎI mock.py (mock.py bị lỗi che biến): bề rộng + mốc dọc     │
//  │   khu slot. Lý do đầy đủ ở khối ghi chú đầu `MillDesign`.                          │
//  │                                                                                   │
//  │ ⚠ `MillPopupBuilder.SoatHinhHoc()` chạy MỖI LẦN dựng và biến toàn bộ phép soát    │
//  │   tràn thành LỖI ĐỎ. Chú thích không chạy được — hàm đó chạy được.                 │
//  └──────────────────────────────────────────────────────────────────────────────────┘
//
//  ┌─ KHÔNG CÓ HỆ TAB ───────────────────────────────────────────────────────────────┐
//  │ Video có 3 tab. Chủ dự án XÁC NHẬN đó là lỗi thiết kế — mỗi máy một popup riêng.  │
//  │ Tool KHÔNG dựng node tab nào; hàng trên của panel chỉ còn chip số dư kim cương    │
//  │ căn phải. Hai sprite tab_active/tab_inactive.png CỐ Ý không dùng.                 │
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
        /// <summary>Nhân đều toàn bộ popup. 1 = đúng px của bản mockup 1920×1080.</summary>
        public const float TiLeHienThi = 1f;

        /// <summary>sortingOrder của Canvas con bọc popup — cao hơn popup khác của dự án.</summary>
        public const int SortOrder = 400;

        // ═══════════════════════════════════════════════════════════════════════════════
        //  ⚠⚠ ĐỌC TRƯỚC KHI SỬA BẤT KỲ SỐ NÀO Ở ĐÂY ⚠⚠
        //
        //  BẢN CŨ LẤY SỐ TỪ ĐÂU VÀ VÌ SAO SAI:
        //    Mọi hằng số của bản trước được copy 1:1 từ mockup HTML
        //    (Assets/Assetsgame/popup/ui_mill_assets/full_mill_ui.html) — một file được vẽ ở
        //    viewport 1000×680. Nhưng CanvasScaler của dự án (Canvas_Popup, xem
        //    MillPopupBuilderTool.TimCanvasUI) có referenceResolution = 1920×1080. Hệ quả đo
        //    được: popup chỉ chiếm 1000/1920 = 52% bề rộng màn hình, chữ thân bài rơi xuống
        //    10–15px. Đó CHÍNH LÀ lý do bản trước bị chấm 5/10 ("popup bé, chữ tí, art không
        //    dùng"). Không phải lỗi chọn art, mà lỗi ĐƠN VỊ.
        //
        //  BẢN NÀY LẤY SỐ TỪ ĐÂU:
        //    /tmp/mill/mock.py — bản mockup dựng ĐÚNG 1920×1080 bằng CHÍNH sprite PNG của chủ
        //    dự án và font Baloo2.ttf, đã được chủ dự án XEM VÀ DUYỆT. Mỗi khối dưới đây ghi rõ
        //    tên biến tương ứng trong mock.py. Sửa số ở đây mà không sửa mock.py = lệch bản
        //    duyệt. KHÔNG copy lại số từ file HTML nữa: nó thuộc hệ 1000×680.
        //
        //  HAI CHỖ CỐ Ý LỆCH KHỎI mock.py (mock.py bị lỗi che biến, xem báo cáo):
        //    • Khu slot lấy bề rộng/mốc đáy của KHUNG NGOÀI (RightW 974 / AnimH 418), còn
        //      mock.py vô tình dùng lại biến RW2/AH đã bị gán lại thành kích thước LÒNG khung
        //      (934 / 378) sau khi vẽ khung gỗ. Vì mock.py vẫn đặt slot đầu tiên ở RX (mép
        //      TRÁI khung ngoài), hàng slot trong ảnh duyệt bị ngắn 40px so với khung
        //      animation ngay phía trên — nhìn ra là lỗi canh lề. Bản này cho hàng slot phủ
        //      đúng 974 để hai khối thẳng lề nhau.
        //    • Mốc dọc khu slot vì thế là AnimH + 22 = 440 (22px dưới ĐÁY KHUNG GỖ) thay vì
        //      420 của mock.py (chỉ 2px dưới đáy khung — hụt hơi).
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>Khung tham chiếu của CanvasScaler dự án — mọi số dưới đây thuộc hệ này.</summary>
        public const float ManHinhW = 1920f, ManHinhH = 1080f;

        // ── Khung ngoài — mock.py WW/WH ──────────────────────────────────────────────
        //  1560×900 = 81% × 83% màn hình. Vì sao KHÔNG lấy full-screen: 1560/900 = 1.733;
        //  ở 4:3 (1440×1080 vùng an toàn) popup vẫn còn 1560 > 1440 ⇒ phải chọn 1560 để
        //  CanvasScaler ở chế độ Expand không cắt mép; ở 21:9 popup không bị kéo dài thành
        //  dải ngang. Đây là ô lớn nhất an toàn từ 4:3 tới 21:9.
        public const float PopupW = 1560f, PopupH = 900f;

        /// <summary>
        /// Dịch popup XUỐNG 10px so với tâm màn hình (mock.py WY = 100, tâm sẽ là 90).
        /// ⚠ ĐỪNG bỏ: ruy băng tiêu đề nhô 92px LÊN TRÊN mép Window, đặt Window đúng tâm thì
        /// đỉnh ruy băng ở y = −2, tức BỊ CẮT khỏi màn hình. 10px này là toàn bộ khoảng an toàn.
        /// </summary>
        public const float PopupLechY = -10f;

        public const float PopupPad = 44f;                              // mock.py PAD
        public const float PanelW = PopupW - PopupPad * 2f;             // 1472
        public const float PanelH = PopupH - PopupPad * 2f;             // 812

        // ── Ruy băng tiêu đề — mock.py RW/RH ────────────────────────────────────────
        //  ⚠ SPRITE LÀ `shop_banner_ribbon`, KHÔNG PHẢI `ribbon_header`. Đã soi cả hai ảnh:
        //    • ribbon_header.png       1440×270 (thân thật 1440×237, tỉ lệ 6.08) — bản chất là
        //      một KHỐI CHỮ NHẬT VÀNG PHẲNG bo góc với hai cái đuôi tí xíu. Dù wire đúng, nó
        //      vẫn đọc ra "tấm nền", đúng thứ chủ dự án phàn nàn.
        //    • shop_banner_ribbon.png   480×120 (thân thật 461×103, tỉ lệ 4.47) — ruy băng
        //      thật: thân gradient, vệt sáng trên, HAI ĐẦU ĐUÔI CÁ. Cùng asset dùng cho header
        //      "CÔNG THỨC" nên tiêu đề và header đồng bộ.
        //  VẼ ĐÚNG TỈ LỆ GỐC: 720/161 = 4.47 = tỉ lệ thân thật ⇒ đuôi ruy băng KHÔNG bị bóp.
        //  Đổi RW thì phải đổi RH theo (RH = RW / 4.472), nếu không đuôi méo.
        public const float RibbonW = 720f, RibbonH = 161f;
        /// <summary>Đỉnh ruy băng nhô 92px LÊN TRÊN mép trên Window (mock.py WY − 92).</summary>
        public const float RibbonTop = -92f;
        public const float RibbonFont = 54f;
        /// <summary>
        /// Ô chữ chỉ cao 88% ruy băng ⇒ tâm chữ nằm ở 44% chiều cao (mock.py RH*0.44).
        /// Art có phần đáy là bóng/đuôi cờ, canh giữa tuyệt đối là chữ bị tụt.
        /// </summary>
        public const float RibbonTiLeChu = 0.88f;

        // ── Nút X — mock.py CB ──────────────────────────────────────────────────────
        //  Tâm nút trùng GÓC PHẢI-TRÊN của Window: nút tràn 46.8px ra ngoài mép phải và
        //  43.68px lên trên mép trên (mock.py WX+WW−CB*0.55, WY−CB*0.42).
        //  Soát tràn màn hình: mép phải nút = 180+1560+46.8 = 1786.8 < 1920 ✔
        public const float CloseSize = 104f;
        public const float CloseOffRight = -CloseSize * 0.45f;   // −46.8
        public const float CloseOffTop   = -CloseSize * 0.42f;   // −43.68

        // ── Chip kim cương — mock.py GW/GH ──────────────────────────────────────────
        //  Neo góc PHẢI-TRÊN của InnerPanel. Bề rộng CỐ ĐỊNH 224 (không ContentSizeFitter):
        //  21 + 46 + 11 + chữ + 21; "20.018" cỡ 36 ≈ 114px ⇒ tổng ≈ 213 ≤ 224 ✔
        public const float ChipW = 224f, ChipH = 68f;
        public const float ChipRight = 24f, ChipTop = 22f;
        public const float ChipPadX = 21f, ChipGap = 11f;
        public const float ChipGemIcon = 46f;
        public const float ChipFont = 36f;

        // ── Ô nội dung — mock.py CX/CY/CW/CH ───────────────────────────────────────
        public const float ContentLeft = 26f;
        public const float ContentTop  = ChipH + 34f;                       // 102
        public const float ContentW = PanelW - ContentLeft * 2f;            // 1420
        public const float ContentH = PanelH - ContentTop - ContentLeft;    // 684
        public const float ColGap = 26f;                                    // mock.py GAP
        public const float RecipeListW = 420f;                              // mock.py LW
        public const float RightW = ContentW - RecipeListW - ColGap;        // 974 (mock.py RW2)

        // ── Cột công thức (trái) — mock.py HDR_H / LIST_* / HINT_H ─────────────────
        public const float ListHeaderW = 340f, ListHeaderH = 76f;
        public const float ListHeaderFont = 32f;
        /// <summary>Ô chữ header = 88% ruy băng, cùng lý do với RibbonTiLeChu.</summary>
        public const float ListHeaderTiLeChu = 0.88f;
        public const float ListHeaderMb = 14f;
        public const float HintH = 80f, HintFont = 32f, HintMb = 16f;
        public const float ListTop = ListHeaderH + ListHeaderMb;                   // 90
        public const float ListH = ContentH - ListTop - HintH - HintMb;            // 498
        public const float HintTop = ContentH - HintH;                             // 604
        /// <summary>Lề trong bảng danh sách (mock.py card đặt ở CX+16, card đầu cách trên 8).</summary>
        public const float ListPadX = 16f, ListPadTop = 8f, ListPadBot = 6f;
        public const float CardW = RecipeListW - ListPadX * 2f;                    // 388
        public const float ScrollH = ListH - ListPadTop - ListPadBot;              // 484
        public const float CardH = 150f, CardGap = 10f;
        //  SOÁT: 3 card = 150*3 + 10*2 = 470 ≤ 484 ✔ ; card thứ 4 cần 630 > 484 ⇒ ĐÚNG 3 card
        //  hiện cùng lúc như bản duyệt, card thứ 4 phải cuộn mới thấy.

        // ── Card công thức — mock.py CDW/CDH ───────────────────────────────────────
        public const float CardIconPlate = 116f, CardIconLeft = 16f;
        /// <summary>Đĩa icon canh giữa theo chiều dọc card: (150 − 116)/2 = 17.</summary>
        public static float CardIconTop => (CardH - CardIconPlate) * 0.5f;
        public const float CardIconImg = 98f;
        public const float CardTextLeft = 148f;
        public const float CardTextPadR = 8f;
        public static float CardTextW => CardW - CardTextLeft - CardTextPadR;      // 232
        //  ⚠ CardNameH = 32 (KHÔNG phải 34): hộp tên tâm y 50 ⇒ 34..66, vừa khít ĐỈNH thẻ
        //    con vật ở y 66. Để 34 thì hộp là 33..67, đè 1px và `SoatHinhHoc` báo lỗi đỏ.
        public const float CardNameFont = 32f, CardNameCy = 50f, CardNameH = 32f;
        public const float CardTimeFont = 23f, CardTimeCy = 84f, CardTimeH = 26f;
        public const float CardTagLeft = CardTextLeft + 84f;                       // 232
        public const float CardTagTop = 66f, CardTagH = 36f;
        public const float CardTagFont = 22f, CardTagPadX = 17f;
        public const float CardChipW = 94f, CardChipH = 40f, CardChipGap = 9f;
        public const float CardChipTop = 102f;
        public const float CardChipIcon = 34f, CardChipFont = 25f;
        public const float CardLockGlyph = 64f, CardLockTop = 30f, CardLockLeft = 24f;
        public const float CardLockTextFont = 24f;
        //  SOÁT TRÀN CARD (388 × 150):
        //    dọc  — đĩa 17..133 (còn 17 mỗi mép) | tên tâm 50 (hộp 33..67) | thời gian tâm 84
        //           (71..97) | thẻ con vật 66..102 | chip nguyên liệu 102..142 ⇒ đáy còn 8 ✔
        //    ngang— tên/thời gian bắt đầu 148, ô rộng 232 ⇒ hết 380, mép phải 388 còn 8 ✔
        //           thẻ con vật ở 232, dài nhất "Bò sữa" = 34+6*13 = 112 ⇒ hết 344 ✔
        //           2 chip = 94+9+94 = 197 từ x148 ⇒ hết 345 ✔
        //    Tên dài nhất "Cỏ trộn cho bò" cỡ 32 đo trên ảnh duyệt = 204px < 232 ✔ và nó KẾT
        //    THÚC ở hàng 1 (tâm 50) nên KHÔNG chạm thẻ con vật ở hàng 2 (66..102).

        // ── Khung animation — mock.py AH / FR / panel_outer ────────────────────────
        //  ⚠ ĐÂY LÀ KHUNG GỖ THẬT (`panel_outer.png` — ván gỗ + đinh tán), KHÔNG còn là một
        //    vạch viền 3px vẽ tay như bản trước ("dựng nền lên"). Trời/đất nằm BÊN TRONG.
        public const float AnimW = RightW;                        // 974
        public const float AnimH = 418f;
        public const float AnimVienGo = 20f;                      // mock.py FR
        public const float AnimInnerW = AnimW - AnimVienGo * 2f;   // 934
        public const float AnimInnerH = AnimH - AnimVienGo * 2f;   // 378
        /// <summary>Trời 60% lòng khung (mock.py int(IBH2*0.60) = 226).</summary>
        public const float SkyH = 226f;
        public const float GroundH = AnimInnerH - SkyH;           // 152
        /// <summary>Bo góc lòng khung (mock.py rounded_rectangle radius 16).</summary>
        public const float AnimRadius = 16f;

        // ── Badge trạng thái — mock.py BW2/BH2 ─────────────────────────────────────
        public const float BadgeW = 268f, BadgeH = 62f;
        public const float BadgeLeft = 22f, BadgeTop = 22f;
        public const float BadgePadX = 26f, BadgeGap = 12f;
        public const float BadgeFont = 28f;
        public const float DotSize = 18f;

        // ── Bong bóng nguyên liệu — mock.py IBW/IBH ────────────────────────────────
        public const float BubbleW = 148f, BubbleH = 84f;
        public const float BubbleLeft = 34f, BubbleTop = 132f;
        public const float BubblePadX = 17f, BubbleGap = 11f;
        public const float BubbleIcon = 54f, BubbleFont = 34f;

        // ── Băng tải — mock.py BELT_* ──────────────────────────────────────────────
        //  Vị trí: chạy từ x 86 vào ĐẾN TRONG chân máy (mép phải = MX+52) ⇒ rộng 346.
        //  ⚠ KHÁC BẢN TRƯỚC: bản trước cố giữ ĐÚNG tỉ lệ ảnh 10:1 (cao 34.6 ở bề rộng 346).
        //    Bản duyệt vẽ 346×72, tức KÉO CAO 2.08×, và 4 con lăn bake trong ảnh biến thành
        //    ô-van dọc. Đã cân nhắc và CHẤP NHẬN: ở 34.6px băng tải mảnh như một cái gạch,
        //    không còn nối được vào chân máy 280px, và chủ dự án đang phàn nàn CHÍNH VỀ VIỆC
        //    mọi thứ quá mảnh/quá nhỏ. Méo con lăn ở mức này không nhìn ra ở 100% zoom.
        public const float BeltW = 346f, BeltH = 72f;
        public const float BeltLeft = 86f, BeltTop = 258f;
        /// <summary>Lề của lớp sọc cuộn so với mép mâm băng tải.</summary>
        public const float BeltBorder = 4f;
        // conveyor_base.png thật 1200×120 — chỉ cần bề CAO để tính tỉ lệ kéo dọc.
        public const float BeltArtVbH = 120f;       // bề cao thật
        public const float BeltArtRollerTop = 72f;  // đỉnh 4 con lăn trong ảnh gốc
        /// <summary>Tỉ lệ kéo dọc ảnh băng tải (72/120 = 0.6).</summary>
        public static float BeltTiLeDoc => BeltH / BeltArtVbH;
        /// <summary>
        /// Thụt đáy lớp sọc cuộn: dừng ngay TRÊN 4 con lăn vẽ sẵn (48px ảnh × 0.6 = 28.8px
        /// node) nên mặt băng chạy mà con lăn đứng, không bị sọc quét lên thành vệt bẩn.
        /// </summary>
        public static float BeltStripeChanDuoi => (BeltArtVbH - BeltArtRollerTop) * BeltTiLeDoc;
        /// <summary>Chu kỳ hoa văn sọc chéo theo trục X = 42px (texture vẽ đúng 42×42).</summary>
        public const int BeltTileX = 42;

        // ── Bó cỏ chạy trên băng tải — mock.py KHÔNG vẽ (ảnh duyệt bắt đúng lúc băng trống)
        //  Toạ độ suy từ hình học băng tải: mặt băng ở y 258 (đáy lòng khung 378 ⇒ bottom 120),
        //  chạy từ x 96 tới trước chân máy (380+30) ⇒ hành trình 300px.
        public const float ItemSize = 52f;
        public const float ItemLeft = 96f, ItemBottom = 120f;
        public const int   ItemCount = 2;
        public const float ItemTravel = 300f, ItemOvershoot = 26f, ItemDrop = 14f;

        // ── Máy xay — mock.py MSZ/MX/MY ────────────────────────────────────────────
        public const float MachineSize = 280f;
        public const float MachineRight = 274f;     // 934 − 380 − 280
        /// <summary>
        /// ⚠ 24 LÀ SỐ CÓ CHỦ ĐÍCH, ĐỪNG NÂNG. Khung animation CỐ Ý KHÔNG CÓ MASK: nhánh
        /// `Conveyor` đã mang một `Mask` (stencil) và chồng hai cơ chế cắt khác loại lên một
        /// layout đã chốt là rủi ro lớn hơn lợi ích. Vì vậy khói được giữ trong khung bằng
        /// HÌNH HỌC, không bằng cắt: máy cao 280 với lề đáy 24 để còn ĐÚNG
        /// <see cref="TroiTrenPheu"/> = 74px trời TRÊN miệng phễu — trùng khít
        /// `MillSmokeFX.caoBay` mặc định (74f). Nâng lề đáy hoặc hạ MachineSize là khói bay
        /// vượt mép khung gỗ.
        /// </summary>
        public const float MachineBottom = 24f;
        /// <summary>Khoảng trời còn lại trên miệng phễu — phải ≥ MillSmokeFX.caoBay (74).</summary>
        public static float TroiTrenPheu => AnimInnerH - MachineBottom - MachineSize;   // 74

        // Bánh răng: toạ độ đo TRỰC TIẾP trên ảnh duyệt, không còn quy đổi viewBox 200 nữa
        // (mock.py putfit gear_large ở MX+36/MY+120 cỡ 140, gear_small ở MX+174/MY+146 cỡ 96).
        // ĐO TỪ SPRITE, KHÔNG ƯỚC LƯỢNG.
        // `machine_body.png` 600×600: PHỄU chiếm y 30..170 (hẹp, x 156..442 thóp dần),
        // THÂN máy chiếm y 171..578 và x 22..578. Ở MachineSize 280 (tỉ lệ 0.4667) thân máy
        // nằm trong x 10..270, y 80..270 của hệ toạ độ máy — mọi bánh răng phải nằm gọn
        // trong ô đó, nếu không nó chờm lên phễu hoặc ra ngoài thân.
        //
        // Bánh lớn tâm (92, 176) · bánh nhỏ tâm (196, 186) ⇒ hai bánh cách nhau 7px,
        // nhìn như đang ăn khớp. left/top = tâm − size/2.
        public const float GearLargeSize = 116f, GearLargeLeft = 34f,  GearLargeTop = 118f;
        public const float GearSmallSize =  78f, GearSmallLeft = 157f, GearSmallTop = 147f;

        // ── Đĩa thành phẩm — mock.py OP/OX/OY ─────────────────────────────────────
        //  TO HƠN BẢN TRƯỚC 40% (112 → 240 trong hệ mới; 158 → 192 với icon).
        public const float OutPlate = 240f;
        public const float OutRight = 22f, OutTop = 70f;
        public const float OutIcon = 192f;
        /// <summary>Icon nhấc LÊN 8px so với tâm đĩa (mock.py OY + OP/2 − 8).</summary>
        public const float OutIconLechY = 8f;
        public const float OutTagW = 252f, OutTagH = 54f;
        /// <summary>Nhãn sản phẩm nhô 40px XUỐNG dưới đáy đĩa (mock.py OY + OP − 14).</summary>
        public const float OutTagBottom = -40f;
        public const float OutTagFont = 27f;
        //  SOÁT TRÀN ĐĨA (lòng khung 934 × 378, đĩa neo PHẢI-TRÊN right 22 / top 70):
        //    ngang — đĩa x 672..912, mép lòng khung 934 ⇒ còn 22 ✔
        //            máy x 380..660 ⇒ khe đĩa↔máy = 12px, KHÔNG chồng ✔
        //    dọc   — đĩa y 70..310, lòng khung 378 ⇒ còn 68 ✔
        //            nhãn y 226..280 (nhô 40 dưới đáy đĩa) ⇒ còn 98 ✔
        //  Trần thật của đĩa: 912 phải ≤ 934−? và 672 phải ≥ 660 ⇒ OutPlate ≤ 252. Quá 252 là
        //  đĩa CHỒNG LÊN rect máy xay; muốn to hơn phải dịch máy sang trái TRƯỚC.

        // ── Quầng sáng bao thành phẩm ─────────────────────────────────────────────
        /// <summary>
        /// Cạnh quầng sáng sau đĩa thành phẩm. 160 (hệ cũ, đĩa 112) → 300 (hệ mới, đĩa 240).
        ///
        /// 300 KHÔNG phải "nhân cho đẹp" — nó là mức giữ NGUYÊN độ mờ ở mép khung như bản 160
        /// đã được chấp nhận. Phép tính đầy đủ (alpha tắt theo (1 − d/R)^BagGlowMem, bake sẵn
        /// trong sprite; MillOutputBagFX: alphaGlowMax 0.8, scaleGlowMax 1.18):
        ///   • MỨC CHUẨN của bản 160 cũ: tâm đĩa cách mép ô 20 + 112/2 = 76px;
        ///     R lúc thở = 160×1.18/2 = 94.4 ⇒ 1 − 76/94.4 = 0.194915
        ///     ⇒ alpha mép = 0.8 × 0.19492^1.8 = 0.04215. ĐÂY là ngưỡng phải bám.
        ///   • BẢN MỚI: tâm đĩa ở (934 − 22 − 120, 70 + 120) = (792, 190) trong lòng khung
        ///     934×378 ⇒ mép GẦN NHẤT là mép PHẢI, d = 934 − 792 = 142px
        ///     (các mép khác: trên 190, dưới 188, trái 792 — xa hơn nhiều).
        ///   • Muốn alpha mép ≤ 0.04215 thì (1 − 142/R)^1.8 ≤ 0.05268
        ///     ⇒ 1 − 142/R ≤ 0.05268^(1/1.8) = 0.194915 ⇒ 142/R ≥ 0.805085 ⇒ R ≤ 176.38
        ///     ⇒ cạnh ≤ 176.36 × 2 / 1.18 = 298.95 ⇒ LÀM TRÒN XUỐNG 296 (số chẵn, có biên).
        ///     ⚠ ĐỪNG làm tròn LÊN 300: ở 300 thì R = 177 và alpha mép = 0.0433, tức ĐẬM HƠN
        ///       mức 0.04215 đã được chấp nhận. 300 vẫn dưới mốc 0.046 của bản 128 xa xưa,
        ///       nhưng mốc phải bám là bản 160, không phải bản 128.
        ///   • Kiểm lại ở 296: R = 296×1.18/2 = 174.64
        ///     ⇒ 0.8 × (1 − 142/174.64)^1.8 = 0.8 × 0.18689^1.8 = 0.0391 ≤ 0.04215 ✔
        ///   • Lúc KHÔNG thở (scale 1, R = 148): 0.8 × (1 − 142/148)^1.8 = 0.0032 ⇒ vô hình.
        ///   • Hệ quả đã biết và CHẤP NHẬN (y như bản 160): quầng sáng chạm mép PHẢI rect máy
        ///     xay (x = 660, d = 132) với alpha 0.8 × (1 − 132/174.64)^1.8 = 0.063 — một vầng
        ///     vàng rất nhạt loang lên hông máy. Bản cũ ở chỗ tương ứng là 0.10, tức bản này
        ///     NHẠT HƠN. Đây là ánh sáng loang, không phải hình bị cắt.
        /// ĐỪNG nâng quá 298.95.
        /// </summary>
        public const float BagGlowSize = 296f;
        /// <summary>Độ dốc tắt alpha của quầng sáng: &gt;1 ⇒ lõi sáng gọn, mép tan mềm.</summary>
        public const float BagGlowMem = 1.8f;
        /// <summary>
        /// Lệch để TÂM quầng sáng trùng TÂM đĩa thành phẩm. Hai node cùng neo góc PHẢI-TRÊN
        /// nên giữ nguyên right/top mà đổi kích cỡ là quầng sáng lệch; phải bù
        /// (300 − 240)/2 = 30 vào cả hai mép.
        /// </summary>
        public static float BagGlowLech => (BagGlowSize - OutPlate) * 0.5f;

        // ── Khu slot xay ──────────────────────────────────────────────────────────
        //  Mốc dọc đo từ ĐÁY KHUNG GỖ (AnimH 418), không từ đáy lòng khung — xem khối ghi chú
        //  "HAI CHỖ CỐ Ý LỆCH KHỎI mock.py" ở đầu class.
        public const float SlotsHeaderTop = AnimH + 22f;             // 440
        /// <summary>Tâm chữ header cách mốc 24px (mock.py SHY + 24).</summary>
        public const float SlotsHeaderCy = 24f;
        public const float SlotsHeaderH = 32f;
        public const float SlotsHeaderFont = 30f;
        public const float SlotsSummaryFont = 24f;
        public const float SlotsSummaryLeft = 168f;
        public const float SlotsTop = SlotsHeaderTop + 52f;          // 492
        public const float SlotsH = ContentH - SlotsTop;             // 192
        public const float SlotGap = 18f;
        public const int   SlotCount = 5;
        /// <summary>180.4 — 5 slot + 4 khe 18 phủ ĐÚNG 974 của cột phải.</summary>
        public static float SlotW => (RightW - SlotGap * (SlotCount - 1)) / SlotCount;

        // ── Card slot: BA LỚP ─────────────────────────────────────────────────────
        //  ⚠⚠ ĐỌC KỸ — ĐÂY LÀ LỖI ĐÃ SHIP MỘT LẦN:
        //    `slot_empty.png` KHÔNG phải một cái thẻ, nó là một VIỀN NÉT ĐỨT TRONG SUỐT
        //    (lòng alpha ≈ 6%). Đặt trực tiếp lên khung gỗ `shop_card_outer` thì gỗ lộ qua và
        //    chữ "Trống" chìm mất. BẮT BUỘC có lớp KEM ĐẶC ở giữa:
        //      lớp 1  shop_card_outer  9-slice 30  (khung gỗ, phủ kín card)
        //      lớp 2  shop_card_inner  9-slice 28  (nền kem ĐẶC, thụt 9)
        //      lớp 3  slot_empty       9-slice 26  (viền nét đứt, thụt 9, CHỈ trạng thái Trống)
        //    Slot khoá: nhân màu lớp 2 với SlotMauKhoa để lùi về sau.
        public const float SlotSliceKhung = 30f;
        public const float SlotSliceLot   = 28f;
        public const float SlotSliceTrong = 26f;
        public const float SlotInset = 9f;
        public static float SlotNoiW => SlotW - SlotInset * 2f;      // 162.4
        public static float SlotNoiH => SlotsH - SlotInset * 2f;     // 174
        /// <summary>Nhân màu nền slot KHOÁ (mock.py 0.90/0.87/0.80) — lùi về sau slot đang mở.</summary>
        public static Color SlotMauKhoa => new Color(0.90f, 0.87f, 0.80f, 1f);

        public const float SlotNumLeft = 14f, SlotNumCy = 24f;
        public const float SlotNumFont = 25f, SlotNumH = 26f;

        // Trạng thái TRỐNG (mock.py: đĩa 108 ở +42, chữ "Trống" tâm 172)
        public const float SlotEmptyPlate = 108f, SlotEmptyPlateTop = 42f;
        public const float SlotEmptyCy = 172f, SlotEmptyH = 28f, SlotEmptyFont = 27f;

        // Trạng thái ĐANG XAY / CHỜ THU — mock.py KHÔNG vẽ hai trạng thái này.
        //  Phải nhét 4 hàng vào 174px lòng card (đĩa + tên + thanh tiến độ + nút) nên đĩa ở đây
        //  NHỎ HƠN đĩa của trạng thái Trống (74 so với 108). Đó là có chủ ý: trạng thái Trống
        //  chỉ có 2 hàng nên được đĩa to nhất; đổi đĩa Trống xuống 74 cho "đồng bộ" là làm
        //  xấu đúng cái ảnh đã duyệt.
        public const float SlotIconPlate = 74f, SlotIconPlateTop = 10f;
        public const float SlotIconImg = 64f;
        public const float SlotNameTop = 88f, SlotNameH = 24f, SlotNameFont = 21f;
        public const float SlotProgTiLe = 0.90f, SlotProgH = 20f, SlotProgBottom = 58f;
        public const float SlotTimerFont = 19f;
        public const float SlotBtnTiLe = 0.90f, SlotBtnH = 42f, SlotBtnBottom = 10f;
        public const float SlotBtnFont = 26f;

        // Hai trạng thái KHOÁ (mock.py: đĩa khoá 84 ở +44, nút kim cương 144.4×46 ở đáy −58)
        public const float SlotLockBadge = 84f, SlotLockTop = 44f;
        /// <summary>Ổ khoá TRẮNG vẽ đè lên đĩa nâu — `shop_lock_badge` KHÔNG có hình khoá bên trong.</summary>
        public const float SlotLockGlyph = 46f;
        public const float SlotGemBtnInset = 18f, SlotGemBtnH = 46f, SlotGemBtnBottom = 12f;
        public static float SlotGemBtnW => SlotW - SlotGemBtnInset * 2f;   // 144.4
        public const float SlotGemIcon = 34f, SlotGemFont = 27f, SlotGemGap = 14f;
        public const float SlotPillW = 152f, SlotPillH = 52f, SlotPillBottom = 12f;
        public const float SlotLockTextFont = 20f, SlotLockLevelFont = 23f;

        //  SOÁT TRÀN CARD SLOT (180.4 × 192, lòng lớp kem = 9..183):
        //    "#N"          y 20..46,  x 23..~50 (chữ "#1" cỡ 25 ≈ 27px)
        //    TRỐNG         đĩa 42..150 → chữ 158..186 ⇒ đáy còn 6 ✔ ; khe đĩa↔chữ 8 ✔
        //                  đĩa x 36.2..144.2; ở y 46 nửa rộng đĩa chỉ 38.4 ⇒ x ≥ 51.8
        //                  ⇒ KHÔNG chạm "#N" ✔
        //    ĐANG XAY      đĩa 10..84 (x 53.2..127.2, phải "#N" ⇒ tách hẳn ✔)
        //                  tên 88..112 | thanh 114..134 | nút 140..182
        //                  khe 4 / 2 / 6, đáy còn 10 ✔ — KHÔNG chỗ nào chồng
        //                  (bản trước tên 87..117 và thanh 111..125 CHỒNG 6px, nay hết)
        //    CHỜ THU       đĩa 10..84 | tên 88..112 | nút THU 140..182 ✔
        //    KHOÁ (gem)    ổ khoá 44..128 | nút 134..180 ⇒ khe 6, đáy còn 12 ✔
        //                  KHÔNG có chữ "Mở slot": ở 192px nó đè lên nút, mà ổ khoá + giá kim
        //                  cương đã đủ nghĩa (chốt bản duyệt).
        //    KHOÁ (cấp)    ổ khoá 44..128 | viên thuốc 128..180, trong đó
        //                  "Chưa đủ cấp" 130..152 và "Cấp 18" 152..178 ⇒ đáy còn 12 ✔

        public const float RedDotSize = 18f, RedDotRight = 16f, RedDotTop = 16f;
        /// <summary>Vòng sáng "thả được vào đây": dày 6, bo 18, phình 3px ra ngoài mép card.</summary>
        public const float DropRingBorder = 6f, DropRingRadius = 18f, DropRingInset = -3f;

        // ── Toast ────────────────────────────────────────────────────────────────
        //  Dùng CHUNG art `shop_toast` với bảng gợi ý ⇒ CHUNG spriteBorder (34,22,34,22).
        //  Cao 88 ≥ 22+22 = 44 ⇒ còn dải giữa để kéo ✔
        public const float ToastW = 720f, ToastH = 88f;
        public const float ToastBottom = 160f, ToastFont = 34f;

        // ── Bảng màu ─────────────────────────────────────────────────────────────
        //  Màu CHỮ và VIỀN CHỮ lấy đúng từ mock.py (bản duyệt), KHÔNG lấy từ CSS 1000×680 nữa.
        /// <summary>mock.py BROWN (94,62,38) — chữ thân bài.</summary>
        public const string CTextBrown   = "#5E3E26";
        /// <summary>mock.py BROWN_L (140,100,66) — chữ phụ.</summary>
        public const string CTextLight   = "#8C6442";
        /// <summary>Viền chữ tiêu đề (mock.py shadow 168,96,22).</summary>
        public const string CVienTieuDe  = "#A86016";
        /// <summary>Viền chữ header cột (mock.py shadow 150,92,20).</summary>
        public const string CVienHeader  = "#965C14";
        /// <summary>Viền chữ trên nền xanh lá (mock.py shadow 70,110,40).</summary>
        public const string CVienXanhLa  = "#466E28";
        /// <summary>Viền chữ trên nền nâu (mock.py shadow 90,58,34).</summary>
        public const string CVienNau     = "#5A3A22";
        /// <summary>Viền chữ trên nền xanh dương (mock.py shadow 30,90,130).</summary>
        public const string CVienXanhDuong = "#1E5A82";
        /// <summary>Số thứ tự slot (mock.py 150,126,100).</summary>
        public const string CSlotNum     = "#967E64";
        /// <summary>Chữ "Trống" (mock.py 152,130,106).</summary>
        public const string CSlotTrong   = "#98826A";
        /// <summary>Chấm trạng thái lúc máy rảnh (mock.py 150,145,138).</summary>
        public const string CDotRanh     = "#96918A";
        /// <summary>Chấm trạng thái lúc đang xay.</summary>
        public const string CDotDangXay  = "#62E15D";

        // Màu dùng cho sprite VẼ THAY THẾ khi thiếu art (chỉ chạy ở nhánh dự phòng)
        public const string CWoodBorder  = "#8B5933";
        public const string CInnerBg     = "#FFF6E5";
        public const string CPanelBorder = "#D6B798";
        public const string CBadgeBg     = "#F4E2C7";
        public const string CBadgeBorder = "#C4A882";
        public const string CConveyor    = "#3F2C21";
        public const string CBeltBorder  = "#231812";
        public const string CBeltStripe  = "#2A1D15";
        public const string COutBg       = "#F8E6CA";
        public const string COutBorder   = "#DFB980";
        public const string CCardBorder  = "#E4D5C2";
        public const string CSlotIconBg  = "#F6E7D1";
        public const string CProgTrack   = "#D9D9D9";
        public const string CCloseBg     = "#D45B4B";
        public const string CLockCircle  = "#645747";
        public const string CLockedPill  = "#AFA28F";
        public const string CRedDot      = "#FF4A4A";
        public const string CBtnGreen    = "#82C94F";
        public const string CBtnBlue     = "#40A4E5";
        public const string CItemGrain   = "#D9A85B";
        public const string CItemGrainBd = "#A8763A";
        /// <summary>Quầng sáng bao thành phẩm — vàng ấm, bake sẵn trong sprite.</summary>
        public const string CBagGlow     = "#FFD24A";

        // ── Tô màu nút lớn `Btn_Main` ────────────────────────────────────────────
        //  ⚠ QUYẾT ĐỊNH BẮT BUỘC ĐỌC — xem khối ghi chú dài ở `MillWiring.WirePopup`.
        //  `MillPopupUI.CapNhatNutLon` (dòng 700) GHI `imgMainButtonBg.color = sanSang ?
        //  mauNutBamDuoc : mauNutKhoa`. Bảng gợi ý nay là art `shop_toast` (XANH LÁ), nên
        //  mauNutBamDuoc PHẢI là TRẮNG TINH (phép nhân đơn vị = art hiện đúng màu gốc), còn
        //  trạng thái chặn dùng một mức XÁM TRUNG TÍNH (làm tối đều, giữ vệt sáng và viền)
        //  chứ KHÔNG dùng màu kem #D9CDB9 như bản trước — kem × xanh lá = xanh ô-liu bẩn.
        /// <summary>Tint nút lớn lúc bấm được = TRẮNG ⇒ art `shop_toast` giữ nguyên màu.</summary>
        public const string CNutBamDuoc  = "#FFFFFF";
        /// <summary>Tint nút lớn lúc bị chặn = xám trung tính ⇒ art tối đều, không lệch tông.</summary>
        public const string CNutKhoa     = "#B3ADA0";

        // ═══ TRỜI & ĐẤT VẼ LẠI (chốt 21/08) ═══════════════════════════════════════════
        //  ⚠ ĐỌC KỸ TRƯỚC KHI SỬA — đây là chỗ tool CỐ Ý LỆCH khỏi file thiết kế gốc:
        //  CSS mockup (full_mill_ui.html dòng 20-23, 315-323) cho TRỜI MÀU BẠC HÀ NHẠT
        //  (#E6F3E6 → #CBE6CF) và ĐẤT NÂU SÁNG PHẲNG (#B48D64 + sọc #A68058). Chủ dự án xem
        //  thì thấy "thô/bợt", yêu cầu TRỜI XANH → TRẮNG và ĐẤT NÂU ĐẬM có vạch chân trời.
        //  ⇒ Màu dưới đây KHÔNG có trong CSS. Mỗi hex đều SUY RA từ một biến :root của chính
        //    mockup theo một phép nhân/pha ghi rõ bên cạnh, để không lạc bảng màu popup.

        /// <summary>Đỉnh trời — --btn-blue #40A4E5 pha 30% trắng.</summary>
        public const string CSkyBlue     = "#79BFED";
        /// <summary>Chân trời — --btn-blue #40A4E5 pha 94% trắng (gần trắng, không trắng tinh).</summary>
        public const string CSkyPale     = "#F4FAFD";
        /// <summary>Mặt đất — --ground-main #B48D64 × 0.86.</summary>
        public const string CSoilTop     = "#9B7956";
        /// <summary>Đất sâu — #B48D64 × 0.70 (càng xuống càng tối ⇒ có khối, không phẳng).</summary>
        public const string CSoilBottom  = "#7E6346";
        /// <summary>Luống đất — --ground-stripe #A68058 × 0.78.</summary>
        public const string CSoilFurrow  = "#826445";
        /// <summary>Vạch chân trời (mép đất cắt vào trời) — #B48D64 × 0.52.</summary>
        public const string CHorizonDark = "#5E4934";
        /// <summary>Mép đất hứng sáng ngay dưới vạch chân trời — #B48D64 × 1.11.</summary>
        public const string CHorizonRim  = "#C89D6F";
        /// <summary>Dày vạch chân trời (px).</summary>
        public const float  HorizonLineH = 4f;
        /// <summary>Dày dải sáng dưới vạch chân trời (px).</summary>
        public const float  HorizonRimH  = 6f;
        /// <summary>
        /// Chu kỳ luống đất. CSS gốc 30px ở khung rộng 629; khung nay rộng 934 (×1.49) nên
        /// giãn lên 46 để mật độ luống nhìn KHÔNG đổi. Vạch vẽ theo chu kỳ 2× số này.
        /// </summary>
        public const float GroundStripe = 46f;
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
        /// Nạp Sprite từ MỘT ĐƯỜNG DẪN ASSET CỤ THỂ (không đi qua 4 thư mục ưu tiên) — dùng
        /// cho texture hiệu ứng: Assets/Lana Studio/Hyper Casual FX/Textures/*.png và
        /// Assets/_Game/Farm/Art/UI_OrderBoard/ob_smoke.png.
        ///
        /// Hai cạm bẫy đều được xử lý và GHI VÀO BÁO CÁO:
        ///  1. <c>textureType != Sprite</c> ⇒ <c>LoadAssetAtPath&lt;Sprite&gt;</c> LUÔN null.
        ///     Tool tự đặt <c>TextureImporterType.Sprite</c> + <c>SaveAndReimport()</c> rồi
        ///     mới nạp. (Kiểm 21/08: cả 7 texture đã sẵn textureType = Sprite ⇒ không sửa gì.)
        ///  2. <c>spriteImportMode = Multiple</c> ⇒ <c>LoadAssetAtPath&lt;Sprite&gt;</c> CŨNG
        ///     null vì sprite nằm ở sub-asset. Phải quét
        ///     <c>LoadAllAssetRepresentationsAtPath</c> — đúng mẫu đã dùng ở
        ///     Assets/_Game/Farm/Editor/UnlockIconFillTool.cs dòng 458-465.
        ///     confetti_large.png ĐANG là Multiple (sheet 4 ô: confetti_large_0..3) ⇒ nhánh
        ///     này BẮT BUỘC phải có, nếu không mảnh giấy pháo bông sẽ null im lặng.
        ///     ⚠ KHÔNG đổi Multiple → Single: hệ khác của dự án có thể đang dùng từng ô.
        /// </summary>
        public static Sprite TimTheoDuongDan(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null)
            {
                Ghi("KHÔNG CÓ texture tại  " + path);
                return null;
            }

            if (imp.textureType != TextureImporterType.Sprite)
            {
                TextureImporterType cu = imp.textureType;
                imp.textureType = TextureImporterType.Sprite;
                imp.SaveAndReimport();
                Ghi("SỬA IMPORT  " + Path.GetFileName(path) + "  textureType " + cu +
                    " → Sprite (nếu không thì LoadAssetAtPath<Sprite> trả null)");
            }

            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null)
            {
                // Sprite Mode = Multiple ⇒ sprite là sub-asset, lấy ô ĐẦU TIÊN.
                s = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                                 .OfType<Sprite>()
                                 .FirstOrDefault();
                if (s != null)
                    Ghi("TÌM ĐƯỢC  " + s.name + "  ← " + path +
                        " (Sprite Mode = Multiple, lấy ô đầu)");
            }
            else Ghi("TÌM ĐƯỢC  " + s.name + "  ← " + path);

            if (s == null) Ghi("LỖI: không nạp được Sprite nào từ  " + path);
            return s;
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

        /// <summary>
        /// XOÁ spriteBorder + chuẩn hoá import cho một sprite cần vẽ Type = SIMPLE.
        ///
        /// ⚠ BẮT BUỘC GỌI, KHÔNG ĐƯỢC "bỏ qua cho gọn": spriteBorder nằm trong FILE .meta
        /// của asset, nó SỐNG SÓT qua các lần chạy tool. Bản trước của tool này từng
        /// <c>ApSlice(btn_close, 45, 15,15,15,15)</c>; nếu bản mới chỉ "không gọi ApSlice
        /// nữa" thì border 21 texel VẪN CÒN trong .meta ⇒ <see cref="MillUI.Img"/> thấy
        /// border ≠ 0 và chọn Type = Sliced ⇒ nút tròn vẫn bị 9-slice y như cũ. Phải ghi
        /// border = 0 một cách tường minh mới sửa được.
        /// </summary>
        public static Sprite ApSimple(Sprite sp)
        {
            return ApSlice(sp, 1f, 0f, 0f, 0f, 0f);
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

        // ── VẼ: TRỜI & ĐẤT (hai sprite lớn của ô animation) ─────────────────────────

        /// <summary>
        /// TRỜI: gradient dọc XANH → TRẮNG + một vầng nắng mờ ở góc phải-trên, bo 2 GÓC TRÊN.
        ///
        /// Vì sao không dùng <see cref="VeKhoi"/> nữa: Khoi chỉ nội suy THẲNG fillTop→fillBottom,
        /// ra một dải màu phẳng đúng kiểu chủ dự án gọi là "thô". Ở đây dùng smoothstep (giữ
        /// màu xanh lâu ở trên, tan nhanh về trắng ở sát chân trời) + vầng nắng, nên nền có
        /// chiều sâu mà vẫn KHÔNG cần thêm asset nào.
        ///
        /// Màu lấy từ <see cref="MillDesign.CSkyBlue"/> / <see cref="MillDesign.CSkyPale"/> —
        /// xem khối ghi chú "TRỜI &amp; ĐẤT VẼ LẠI" trong MillDesign để biết vì sao lệch CSS.
        /// Không 9-slice: sprite vẽ đúng bằng kích thước node (629×150).
        /// </summary>
        public static Sprite VeTroi(string id, int w, int h, float r)
        {
            if (_cache.TryGetValue(id, out Sprite san) && san != null) return san;

            Color dinh = Hex(MillDesign.CSkyBlue);
            Color chan = Hex(MillDesign.CSkyPale);
            float nangX = w * 0.80f, nangY = h * 0.82f;      // tâm vầng nắng (góc phải-trên)
            float nangR = h * 0.95f;                          // bán kính tắt dần của vầng nắng

            int tw = Mathf.Max(4, w * SS), th = Mathf.Max(4, h * SS);
            Sprite sp = Ve(id, tw, th, Vector4.zero, TextureImporterType.Sprite,
                TextureWrapMode.Clamp, (x, y) =>
                {
                    float px = (x + 0.5f) / SS, py = (y + 0.5f) / SS;   // py = 0 ở ĐÁY ảnh
                    // Bo 2 góc TRÊN — khớp radius 15 của `.animation-box` trừ viền 3 (HTML 307/310).
                    var p = new Vector2(px - w * 0.5f, py - h * 0.5f);
                    float sd = SdBoGocRieng(p, new Vector2(w * 0.5f, h * 0.5f), r, r, 0f, 0f);
                    float aOut = Mathf.Clamp01(0.5f - sd);
                    if (aOut <= 0.002f) return Color.clear;

                    float t = Mathf.Clamp01(1f - py / Mathf.Max(1f, h));   // 0 đỉnh → 1 chân trời
                    float ts = t * t * (3f - 2f * t);                       // smoothstep
                    Color c = Color.Lerp(dinh, chan, ts);

                    float dx = px - nangX, dy = py - nangY;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / nangR;
                    if (d < 1f)
                    {
                        float k = (1f - d) * (1f - d);
                        c = Color.Lerp(c, Color.white, 0.30f * k);
                    }

                    c.a = aOut;
                    return c;
                });
            if (sp != null) _cache[id] = sp;
            return sp;
        }

        /// <summary>
        /// ĐẤT: nâu ĐẬM có khối (sáng ở mặt, tối dần xuống sâu) + VẠCH CHÂN TRỜI ở mép trên
        /// + luống dọc giữ đúng chu kỳ 30/60px của CSS (HTML 323) + hạt đất lấm tấm, bo 2
        /// GÓC DƯỚI.
        ///
        /// Vạch chân trời = 3px tối (<see cref="MillDesign.CHorizonDark"/>) rồi 4px sáng
        /// (<see cref="MillDesign.CHorizonRim"/>) tan vào đất: mắt đọc ra "mặt đất cắt vào
        /// trời", thứ mà một ô màu phẳng không bao giờ có.
        /// Hạt đất là nhiễu TIỀN ĐỊNH (hash sin) ±3% độ sáng — vẽ lại bao nhiêu lần cũng ra
        /// đúng một file, nên <c>VeRaFile</c> vẫn so byte và không làm git rác.
        /// </summary>
        public static Sprite VeDat(string id, int w, int h, float r)
        {
            if (_cache.TryGetValue(id, out Sprite san) && san != null) return san;

            Color mat   = Hex(MillDesign.CSoilTop);
            Color sau   = Hex(MillDesign.CSoilBottom);
            Color luong = Hex(MillDesign.CSoilFurrow);
            Color vach  = Hex(MillDesign.CHorizonDark);
            Color riem  = Hex(MillDesign.CHorizonRim);
            float chuKy = MillDesign.GroundStripe;             // 30 (HTML 323)
            float dayVach = MillDesign.HorizonLineH;           // 3
            float dayRiem = MillDesign.HorizonRimH;            // 4

            int tw = Mathf.Max(4, w * SS), th = Mathf.Max(4, h * SS);
            Sprite sp = Ve(id, tw, th, Vector4.zero, TextureImporterType.Sprite,
                TextureWrapMode.Clamp, (x, y) =>
                {
                    float px = (x + 0.5f) / SS, py = (y + 0.5f) / SS;   // py = 0 ở ĐÁY ảnh
                    // Bo 2 góc DƯỚI — khớp radius 15 trừ viền 3 của `.animation-box`.
                    var p = new Vector2(px - w * 0.5f, py - h * 0.5f);
                    float sd = SdBoGocRieng(p, new Vector2(w * 0.5f, h * 0.5f), 0f, 0f, r, r);
                    float aOut = Mathf.Clamp01(0.5f - sd);
                    if (aOut <= 0.002f) return Color.clear;

                    float sauBaoNhieu = h - py;                 // 0 ở MẶT ĐẤT → h ở đáy ô
                    float t = Mathf.Clamp01(sauBaoNhieu / Mathf.Max(1f, h));
                    Color c = Color.Lerp(mat, sau, t);

                    // Luống dọc: CSS repeating-linear-gradient(90deg, transparent 0→30px,
                    // stripe 30→60px). Pha 55% để thành luống đất, không thành sọc dán lên.
                    int o = Mathf.FloorToInt(px / Mathf.Max(1f, chuKy));
                    if (((o % 2) + 2) % 2 == 1) c = Color.Lerp(c, luong, 0.55f);

                    // Hạt đất lấm tấm ±3% độ sáng (nhiễu tiền định, không dùng Random).
                    float nz = Mathf.Sin(px * 12.9898f + py * 78.233f) * 43758.5453f;
                    nz -= Mathf.Floor(nz);
                    float hat = 0.97f + 0.06f * nz;
                    c.r *= hat; c.g *= hat; c.b *= hat;

                    // Vạch chân trời + riềm sáng ngay dưới nó.
                    if (sauBaoNhieu < dayVach)
                        c = vach;
                    else if (sauBaoNhieu < dayVach + dayRiem)
                        c = Color.Lerp(riem, c, (sauBaoNhieu - dayVach) / dayRiem);

                    c.a = aOut;
                    return c;
                });
            if (sp != null) _cache[id] = sp;
            return sp;
        }

        // ── VẼ: QUẦNG SÁNG TOẢ TRÒN ─────────────────────────────────────────────────

        /// <summary>
        /// Quầng sáng toả tròn: đặc ở tâm, alpha về 0 ĐÚNG ở rìa. Không 9-slice.
        ///
        /// Alpha giảm theo hàm bậc <paramref name="doDoc"/>: a = (1 − d/R)^doDoc. Bậc &gt; 1
        /// cho lõi sáng gọn và mép tan mềm; bậc = 1 (tuyến tính) trông như một vòng gradient
        /// có mép cứng.
        ///
        /// ⚠ MÀU BAKE SẴN VÀO PNG, KHÔNG vẽ trắng để tint: <see cref="MillOutputBagFX"/> chỉ
        /// đổi ALPHA của <c>Image.color</c> (giữ nguyên RGB) nên sprite phải mang đúng màu.
        /// </summary>
        public static Sprite VeQuangSang(string id, int size, Color mau, float doDoc)
        {
            if (_cache.TryGetValue(id, out Sprite san) && san != null) return san;

            int n = Mathf.Max(8, size * SS);
            float R = n * 0.5f;
            float k = doDoc > 0.05f ? doDoc : 1f;

            Sprite sp = Ve(id, n, n, Vector4.zero, TextureImporterType.Sprite,
                TextureWrapMode.Clamp, (x, y) =>
                {
                    float dx = x + 0.5f - R, dy = y + 0.5f - R;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / R;      // 0 ở tâm → 1 ở rìa
                    if (d >= 1f) return Color.clear;
                    Color c = mau;
                    c.a = mau.a * Mathf.Pow(1f - d, k);
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

        // ── VIỀN CHỮ ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Viền chữ (outline) — thay cho `text-shadow` của mockup: mock.py vẽ chữ hai lần lệch
        /// (2,2) để có bóng, TMP không có drop-shadow rời nên dùng outline cùng màu bóng.
        /// Nhờ nó chữ TRẮNG trên ruy băng vàng / nền xanh lá vẫn tách khỏi nền.
        ///
        /// ⚠ CHỈ GỌI Ở CHỖ THẬT CẦN. Ghi `outlineWidth` làm TMP tạo một material INSTANCE
        /// riêng cho text đó (mất batch, thêm một draw call). Vì vậy tool chỉ bật viền cho
        /// chữ TRẮNG trên nền có hoạ tiết; chữ nâu trên nền kem không cần và không được bật.
        /// </summary>
        /// ⚠ THAM SỐ LÀ `TMP_Text`, KHÔNG PHẢI `TextMeshProUGUI`: hai field contract
        /// `MillPopupUI.txtMainButton` / `toastText` và `out` của `MillCardBuilder.Chip` đều
        /// khai báo kiểu `TMP_Text` (kiểu cơ sở). Nhận `TextMeshProUGUI` là bốn chỗ gọi không
        /// biên dịch được. `outlineWidth` / `outlineColor` đều nằm trên TMP_Text nên không
        /// mất gì; `Color → Color32` có toán tử ngầm của UnityEngine.
        public static TMP_Text Vien(TMP_Text t, float doDay, Color mau)
        {
            if (t == null) return null;
            t.outlineWidth = doDay;
            t.outlineColor = mau;
            return t;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  MillSkin — GOM TOÀN BỘ SPRITE POPUP CẦN (tìm được thì dùng, không thì vẽ)
    //
    //  Bảng đối chiếu node → sprite → số 9-slice ghi ngay tại từng dòng gán dưới đây, số lấy
    //  từ /tmp/mill/mock.py (bản duyệt) chứ KHÔNG từ full_mill_ui.html nữa.
    //  CỐ Ý KHÔNG DÙNG: `ribbon_header` (khối vàng phẳng — đã thay bằng shop_banner_ribbon),
    //  `slot_normal` (card slot nay là 3 lớp), `tab_active` / `tab_inactive` (bỏ hệ tab).
    // ═══════════════════════════════════════════════════════════════════════════════
    internal sealed class MillSkin
    {
        // ── Art của chủ dự án (4 thư mục generated_sprites) ──────────────────────
        public Sprite popupBoard, panelInner;     // ui_mill_assets: popup_board, panel_inner
        public Sprite bannerRibbon;               // ui_shop_svg:    shop_banner_ribbon
        public Sprite panelWood;                  // ui_svg_perfect: panel_outer  (KHUNG GỖ)
        public Sprite machineBody, gearLarge, gearSmall, beltBase;
        public Sprite cardActive, cardInactive, cardLocked;
        public Sprite btnGreen, btnBlue;          // chỉ còn là lớp DỰ PHÒNG cho nút bộ shop
        public Sprite btnClose, progTrack, progFill;
        public Sprite circlePlate;                // shop_circle_plate — đĩa icon card + slot
        public Sprite circlePreview;              // circle_preview    — đĩa THÀNH PHẨM 240px
        public Sprite chipBg;                     // shop_currency_chip — chip gem + badge + nhãn
        public Sprite toastBg;                    // shop_toast         — bảng gợi ý + toast
        public Sprite listPanel;                  // inner_panel        — bảng danh sách + bong bóng
        public Sprite pillBg;                     // badge_count        — thẻ con vật + viên "Cấp"
        public Sprite btnBuyGold, btnBuyGem, btnBuyLocked;
        public Sprite slotFrame, slotFill, slotDashed, slotReady;
        public Sprite lockBadge, gemIcon;

        // ── Sprite tool VẼ (không art nào thay được) ─────────────────────────────
        public Sprite sky, ground, dotGreen, lockGlyph, closeGlyph, redDot;
        public Sprite itemGrain, dropRing, bagGlow;
        public Texture2D beltTex;

        // ── Hiệu ứng pháo bông + khói — nạp theo đường dẫn asset cụ thể ──────────
        public Sprite[] fxGiay;                       // MillCelebrationFX.anhGiay
        public Sprite fxSao, fxLoe, fxKhoi, fxBongBong;

        /// <summary>
        /// true khi `btnClose` là art của chủ dự án (btn_close.png ĐÃ VẼ SẴN DẤU ✖ bên trong).
        /// Node `Glyph_X` phải BỎ trong trường hợp này, nếu không popup có HAI dấu X chồng nhau.
        /// </summary>
        public bool closeCoDauX;

        /// <summary>
        /// true khi `beltBase` là conveyor_base.png (art, ĐÃ BAKE 4 con lăn) ⇒ lớp sọc cuộn
        /// phải dừng TRÊN con lăn. false ⇒ mâm vẽ tay phẳng, sọc phủ gần hết chiều cao.
        /// </summary>
        public bool beltLaArt;

        // ── Đường dẫn art hiệu ứng. ĐÃ `test -f` từng file trên máy chủ dự án.
        //    Thư mục có DẤU CÁCH trong tên ("Lana Studio", "Hyper Casual FX") — đúng như trên
        //    đĩa, AssetDatabase chấp nhận; đừng "sửa" thành gạch dưới.
        internal const string FxThuMuc   = "Assets/Lana Studio/Hyper Casual FX/Textures/";
        internal const string FxConfetti = FxThuMuc + "confetti_large.png";  // 256, Sprite Mode = MULTIPLE
        internal const string FxSquare   = FxThuMuc + "Square01.png";        // 128
        internal const string FxPlus     = FxThuMuc + "Plus01.png";          // 128
        internal const string FxStar     = FxThuMuc + "Star01.png";          // 256
        internal const string FxFlare    = FxThuMuc + "Flare01.png";         // 256
        internal const string FxCircle   = FxThuMuc + "Circle01.png";        // 128
        internal const string FxSmoke    = "Assets/_Game/Farm/Art/UI_OrderBoard/ob_smoke.png"; // 128

        // Bộ sprite chỉ giải MỘT LẦN cho mỗi lần chạy lệnh: prefab card và popup đều cần
        // nó, giải hai lần thì báo cáo bị nhân đôi và tốn thêm một lượt import.
        private static MillSkin _phien;

        /// <summary>Xoá cache phiên. Gọi ở đầu mỗi lệnh.</summary>
        public static void XoaCache() { _phien = null; }

        /// <summary>
        /// Giải toàn bộ sprite. Ghi mọi bước vào báo cáo.
        ///
        /// ⚠⚠ MỘT ASSET → MỘT `spriteBorder`. `ApSlice` ghi border vào FILE .meta, nên gọi hai
        /// lần trên cùng một PNG với hai bộ số là lần sau ĐÈ lần trước và một trong hai node
        /// bị méo góc mà Unity không báo gì. Mọi asset dùng ở nhiều nơi dưới đây đều được giải
        /// MỘT LẦN vào một biến rồi chia sẻ tham chiếu:
        ///   shop_currency_chip → chip kim cương + badge trạng thái + nhãn sản phẩm  (26)
        ///   shop_toast         → bảng gợi ý `Btn_Main` + toast                 (34,22,34,22)
        ///   inner_panel        → bảng danh sách công thức + bong bóng nguyên liệu    (26)
        ///   badge_count        → thẻ con vật trên card + viên thuốc "Cấp 18"         (13)
        ///   shop_circle_plate  → đĩa icon card + đĩa icon slot                   (Simple)
        ///   shop_btn_buy_gold  → chip nguyên liệu trên card + nút THU            (Simple)
        ///
        /// ⚠ SỐ 9-SLICE Ở ĐÂY LÀ PIXEL CỦA ẢNH GỐC, trùng khít tham số `b` của
        /// `slice9()` trong mock.py — vì thế `ApSlice` luôn được gọi với vbW = BỀ RỘNG THẬT
        /// của ảnh (tỉ lệ quy đổi = 1). Import settings đặt PPU 100 = referencePixelsPerUnit
        /// của Canvas nên 1 texel border = 1 px UI, tức 4 góc giữ nguyên kích thước như
        /// mock.py dán góc ở cỡ gốc. Đừng đổi vbW thành cỡ node.
        /// </summary>
        public static MillSkin Tao(MillReport rep)
        {
            if (_phien != null) return _phien;
            MillSpriteFactory.GanBaoCao(rep);
            var s = new MillSkin();
            Color Hex(string h) => MillSpriteFactory.Hex(h);
            Color Hex2(string h, float a) => MillSpriteFactory.Hex(h, a);

            // ── 1. KHUNG POPUP ────────────────────────────────────────────────────
            // popup_board.png 2048×1365, bo góc + đinh tán ở 4 góc ⇒ 9-slice 150 (mock.py 150).
            s.popupBoard = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("popup_board"), 2048f, 150f, 150f, 150f, 150f);

            // panel_inner.png 2048×1251 ⇒ 9-slice 120 (mock.py 120).
            s.panelInner = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("panel_inner"), 2048f, 120f, 120f, 120f, 120f);

            // ── RUY BĂNG: shop_banner_ribbon, KHÔNG PHẢI ribbon_header ────────────
            //  Lý do đầy đủ ở `MillDesign.RibbonW`. Tóm lại: ribbon_header là một khối chữ
            //  nhật vàng PHẲNG tỉ lệ 6.08, còn shop_banner_ribbon là ruy băng thật (gradient,
            //  vệt sáng, hai đầu đuôi cá) tỉ lệ 4.47.
            //  ⚠ KHÔNG 9-SLICE: hai đầu là ĐUÔI CỜ, kéo giãn phần giữa sẽ làm đuôi lệch khỏi
            //    thân. Vẽ Type = Simple ở đúng tỉ lệ gốc (720/161 = 4.47).
            s.bannerRibbon = MillSpriteFactory.ApSimple(
                MillSpriteFactory.Tim("shop_banner_ribbon"));

            // ── KHUNG GỖ khu animation: panel_outer.png 420×280 ⇒ 9-slice 34 (mock.py 34).
            //  Bản trước chỉ có một vạch viền 3px vẽ tay quanh trời/đất ⇒ nhìn như "dựng nền
            //  lên". Nay là khung tranh gỗ thật, trời/đất lọt BÊN TRONG.
            s.panelWood = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("panel_outer"), 420f, 34f, 34f, 34f, 34f);

            // ── 2. CARD CÔNG THỨC — recipe_card_*.png 720×240 ⇒ 9-slice 44 (mock.py 44) ──
            s.cardActive = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("recipe_card_active"),   720f, 44f, 44f, 44f, 44f);
            s.cardInactive = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("recipe_card_inactive"), 720f, 44f, 44f, 44f, 44f);
            s.cardLocked = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("recipe_card_locked"),   720f, 44f, 44f, 44f, 44f);

            // ── 3. MÁY XAY + BĂNG TẢI ────────────────────────────────────────────
            s.machineBody = MillSpriteFactory.ApSimple(MillSpriteFactory.Tim("machine_body"));
            s.gearLarge   = MillSpriteFactory.ApSimple(MillSpriteFactory.Tim("gear_large"));
            s.gearSmall   = MillSpriteFactory.ApSimple(MillSpriteFactory.Tim("gear_small"));

            // conveyor_base.png 1200×120, 4 con lăn BAKE SẴN ⇒ Type = Simple (9-slice ngang sẽ
            // kéo giãn đúng dải chứa con lăn và làm lệch khoảng cách giữa chúng).
            s.beltBase = MillSpriteFactory.ApSimple(MillSpriteFactory.Tim("conveyor_base"));
            s.beltLaArt = s.beltBase != null;
            if (s.beltBase == null)
                s.beltBase = MillSpriteFactory.VeKhoi("mill_belt_base",
                    MillSpriteFactory.K(38, 38, 15f, 2f,
                        Hex(MillDesign.CConveyor), Hex(MillDesign.CBeltBorder)));

            // Hoa văn sọc chéo cuộn — texture 42×42 Wrap = Repeat. VẪN CẦN dù băng tải đã
            // dùng art: `UIScrollingTexture` chỉ cuộn được RawImage có texture Wrap = Repeat.
            s.beltTex = MillSpriteFactory.VeTextureBangTai("mill_belt_stripes");

            // ── 4. NÚT ───────────────────────────────────────────────────────────
            // btn_close.png 64×64: ĐĨA đỏ ĐÃ CÓ DẤU ✖ trắng ở giữa.
            //  ⚠ KHÔNG 9-slice: hình tròn. 9-slice giữ 4 góc theo texel gốc rồi kéo giữa nên
            //    vẽ ở 104px là méo thành bầu dục có cạnh phẳng. Simple (64 → 104) mới đúng.
            s.btnClose = MillSpriteFactory.ApSimple(MillSpriteFactory.Tim("btn_close"));
            s.closeCoDauX = s.btnClose != null;
            if (s.btnClose == null)
                s.btnClose = MillSpriteFactory.VeKhoi("mill_btn_close",
                    MillSpriteFactory.K(40, 40, 12f, 3f,
                        Hex(MillDesign.CCloseBg), Color.white));

            // btn_green / btn_blue 360×120 của ui_mill_assets — CHỈ còn là lớp DỰ PHÒNG cho
            // bộ nút shop. 9-slice (8,12,8,8) theo đơn vị thiết kế 120 ⇒ ×3 = (24,36,24,24).
            s.btnGreen = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("btn_green"), 120f, 8f, 12f, 8f, 8f);
            s.btnBlue = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("btn_blue"),  120f, 8f, 12f, 8f, 8f);

            // Bộ nút shop 160×56 (gradient + dải đáy đậm).
            //  ⚠ Type = Simple: node lớn nhất dùng nó chỉ 144.4×46, tức thu 0.90 ngang và 0.82
            //    dọc — gần đồng dạng. 9-slice thì border dọc cần ≥ r13 + lip 4 = 17 mỗi mép,
            //    34 < 46 nhưng góc sẽ to hơn thiết kế; và mock.py cũng resize thẳng (slice9
            //    tự rơi về `fit()` vì 26+26 = 52 > 46). Simple là khớp bản duyệt.
            s.btnBuyGold = MillSpriteFactory.ApSimple(MillSpriteFactory.Tim("shop_btn_buy_gold"));
            if (s.btnBuyGold == null) s.btnBuyGold = s.btnGreen;
            s.btnBuyGem = MillSpriteFactory.ApSimple(MillSpriteFactory.Tim("shop_btn_buy_gem"));
            if (s.btnBuyGem == null) s.btnBuyGem = s.btnBlue;
            s.btnBuyLocked = MillSpriteFactory.ApSimple(
                MillSpriteFactory.Tim("shop_btn_buy_locked"));

            // ── 5. ASSET DÙNG Ở NHIỀU NƠI — giải MỘT LẦN, MỘT bộ border ──────────

            // shop_currency_chip.png 140×56 ⇒ 9-slice 26 (mock.py 26).
            //  Ba node: chip kim cương 224×68, badge trạng thái 268×62, nhãn sản phẩm 252×54.
            //  Dọc 26+26 = 52 < 54 (node THẤP NHẤT) ⇒ mọi node còn dải giữa để kéo ✔
            s.chipBg = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("shop_currency_chip"), 140f, 26f, 26f, 26f, 26f);
            if (s.chipBg == null)
                s.chipBg = MillSpriteFactory.VeKhoi("mill_chip_bg",
                    MillSpriteFactory.K(48, 48, 20f, 2f,
                        Hex(MillDesign.CBadgeBg), Hex(MillDesign.CBadgeBorder)));

            // shop_toast.png 220×56 ⇒ 9-slice (34,22,34,22) (mock.py truyền đúng tuple này).
            //  ⚠ DÙNG shop_toast, KHÔNG dùng btn_green cho bảng gợi ý: btn_green là một khối
            //    xanh PHẲNG, còn shop_toast có vệt sáng trắng chạy ngang trên + viền đậm ⇒
            //    nhìn ra hình khối nhô lên, không phẳng như nền.
            //  Hai node: bảng gợi ý 420×80 và toast 720×88. Dọc 22+22 = 44 < 80 ✔
            s.toastBg = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("shop_toast"), 220f, 34f, 22f, 34f, 22f);
            if (s.toastBg == null)
                s.toastBg = MillSpriteFactory.VeKhoi("mill_toast",
                    MillSpriteFactory.K(44, 44, 14f, 2f,
                        Hex2(MillDesign.CConveyor, 0.94f), Hex(MillDesign.CBeltBorder)));

            // inner_panel.png 140×140 (khay kem viền nâu) ⇒ 9-slice 26 (mock.py 26).
            //  Hai node: bảng danh sách công thức 420×498 và bong bóng nguyên liệu 148×84.
            //  ⚠ ĐI KÈM MỘT THAY ĐỔI BẮT BUỘC: khay này màu KEM ⇒ `Txt_InputBubble` phải là
            //    chữ NÂU. Để trắng (đúng với nền nâu của CSS cũ) là chữ TÀNG HÌNH.
            s.listPanel = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("inner_panel"), 140f, 26f, 26f, 26f, 26f);
            if (s.listPanel == null)
                s.listPanel = MillSpriteFactory.VeKhoi("mill_list_panel",
                    MillSpriteFactory.K(44, 44, 15f, 3f,
                        Hex(MillDesign.CInnerBg), Hex(MillDesign.CPanelBorder)));

            // badge_count.png 54×30 (viên thuốc nâu đặc, chữ trắng đọc rõ) ⇒ 9-slice 13.
            //  Hai node: thẻ con vật trên card (cao 36) và viên "Chưa đủ cấp / Cấp 18"
            //  (152×52). Dọc 13+13 = 26 < 36 ✔
            s.pillBg = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("badge_count"), 54f, 13f, 13f, 13f, 13f);
            if (s.pillBg == null)
                s.pillBg = MillSpriteFactory.VeKhoi("mill_pill",
                    MillSpriteFactory.K(36, 36, 15f, 0f,
                        Hex(MillDesign.CLockedPill), Color.clear));

            // shop_circle_plate.png 120×120 (đĩa kem có vành mờ). Hình tròn ⇒ Simple.
            //  Hai node: đĩa icon card 116 và đĩa icon slot (108 khi Trống, 74 khi đang xay).
            s.circlePlate = MillSpriteFactory.ApSimple(
                MillSpriteFactory.Tim("shop_circle_plate"));
            if (s.circlePlate == null)
                s.circlePlate = MillSpriteFactory.VeDia("mill_circle_plate", 120,
                    Hex(MillDesign.CSlotIconBg), 0f, Color.clear);

            // circle_preview.png 140×140 — CHỈ dùng cho đĩa THÀNH PHẨM 240px. Hình tròn ⇒ Simple.
            s.circlePreview = MillSpriteFactory.ApSimple(
                MillSpriteFactory.Tim("circle_preview"));
            if (s.circlePreview == null)
                s.circlePreview = MillSpriteFactory.VeDia("mill_out_plate", 240,
                    Hex(MillDesign.COutBg), 4f, Hex(MillDesign.COutBorder));

            // ── 6. BA LỚP CARD SLOT ──────────────────────────────────────────────
            //  ⚠⚠ `slot_empty` LÀ VIỀN NÉT ĐỨT TRONG SUỐT, KHÔNG PHẢI MỘT CÁI THẺ. Đặt trực
            //     tiếp lên khung gỗ là gỗ lộ qua và chữ "Trống" chìm mất — LỖI ĐÃ SHIP MỘT LẦN.
            //     Phải có `shop_card_inner` (kem ĐẶC) chen giữa. Xem `MillSlotBuilder`.
            s.slotFrame = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("shop_card_outer"), 160f,
                MillDesign.SlotSliceKhung, MillDesign.SlotSliceKhung,
                MillDesign.SlotSliceKhung, MillDesign.SlotSliceKhung);
            if (s.slotFrame == null)
                s.slotFrame = MillSpriteFactory.VeKhoi("mill_slot_frame",
                    MillSpriteFactory.K(48, 48, 18f, 4f,
                        Hex(MillDesign.CInnerBg), Hex(MillDesign.CWoodBorder)));

            s.slotFill = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("shop_card_inner"), 140f,
                MillDesign.SlotSliceLot, MillDesign.SlotSliceLot,
                MillDesign.SlotSliceLot, MillDesign.SlotSliceLot);
            if (s.slotFill == null)
                s.slotFill = MillSpriteFactory.VeKhoi("mill_slot_fill",
                    MillSpriteFactory.K(44, 44, 14f, 0f,
                        Hex(MillDesign.CInnerBg), Color.clear));

            s.slotDashed = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("slot_empty"), 130f,
                MillDesign.SlotSliceTrong, MillDesign.SlotSliceTrong,
                MillDesign.SlotSliceTrong, MillDesign.SlotSliceTrong);
            if (s.slotDashed == null)
                s.slotDashed = MillSpriteFactory.VeKhoi("mill_slot_dashed",
                    MillSpriteFactory.K(44, 44, 14f, 2f,
                        Color.clear, Hex(MillDesign.CCardBorder)));

            // "Chờ thu" — slot_selected.png 130×130: viền VÀNG KIM + lòng vàng nhạt.
            s.slotReady = MillSpriteFactory.ApSlice(
                MillSpriteFactory.Tim("slot_selected"), 130f, 26f, 26f, 26f, 26f);
            if (s.slotReady == null)
                s.slotReady = MillSpriteFactory.VeKhoi("mill_slot_ready",
                    MillSpriteFactory.K(44, 44, 14f, 4f,
                        Color.clear, Hex(MillDesign.CBagGlow)));

            // ── 7. THANH TIẾN ĐỘ ────────────────────────────────────────────────
            // progress_track.png 100×24 ⇒ 9-slice CHỈ NGANG (8,0,8,0): node cao 20 < 24 nên
            // border dọc phải là 0, nếu không hai nắp chồng nhau.
            s.progTrack = MillSpriteFactory.Tim("progress_track");
            if (s.progTrack == null)
                s.progTrack = MillSpriteFactory.VeKhoi("mill_prog_track",
                    MillSpriteFactory.K(22, 22, 10f, 0f,
                        Hex(MillDesign.CProgTrack), Color.clear));
            else s.progTrack = MillSpriteFactory.ApSlice(s.progTrack, 100f, 8f, 0f, 8f, 0f);

            // progress_fill.png 60×24.
            //  ⚠ BORDER = 0 CÓ CHỦ Ý: Image.Type = Filled (bắt buộc, xem MillSlotUI) KHÔNG
            //    dùng spriteBorder. Vẫn PHẢI gọi ApSlice(...,0,0,0,0): nó đặt FullRect + PPU
            //    100 + alphaIsTransparency, thiếu FullRect thì mesh Tight cắt lẹm đầu thanh.
            s.progFill = MillSpriteFactory.Tim("progress_fill");
            if (s.progFill == null)
                s.progFill = MillSpriteFactory.VeKhoi("mill_prog_fill",
                    MillSpriteFactory.K(22, 22, 10f, 0f,
                        Hex(MillDesign.CBtnGreen), Color.clear));
            else s.progFill = MillSpriteFactory.ApSlice(s.progFill, 60f, 0f, 0f, 0f, 0f);

            // ── 8. Ổ KHOÁ + KIM CƯƠNG ───────────────────────────────────────────
            // shop_lock_badge.png 64×64: đĩa nâu đậm có vành sáng, BÊN TRONG KHÔNG có hình ổ
            // khoá (đã soi ảnh thật) ⇒ vẫn giữ node `Glyph_Lock` vẽ ổ khoá TRẮNG đè lên,
            // đúng như mock.py vẽ padlock trắng lên đĩa. Hình tròn ⇒ Simple.
            s.lockBadge = MillSpriteFactory.ApSimple(MillSpriteFactory.Tim("shop_lock_badge"));
            if (s.lockBadge == null)
                s.lockBadge = MillSpriteFactory.VeDia("mill_lock_badge", 84,
                    Hex(MillDesign.CLockCircle), 0f, Color.clear);

            s.gemIcon = MillSpriteFactory.Tim("kimcuong", "kimcuong-removebg-preview",
                                              "icon_gem", "gem", "diamond");
            if (s.gemIcon == null)
                s.gemIcon = MillSpriteFactory.VeKimCuong("mill_gem", 46, Hex(MillDesign.CBtnBlue));

            // ── 9. SPRITE PHẢI VẼ (không folder nào có) ──────────────────────────

            // TRỜI & ĐẤT — kích thước vào TÊN FILE để tự vô hiệu cache khi đổi layout.
            //  Lòng khung nay 934×378 (cũ 629×250) ⇒ hai PNG mới, hai PNG cũ thành rác vô hại.
            //  Bo góc 16 = AnimRadius (mock.py bo lòng khung radius 16).
            int gw = Mathf.RoundToInt(MillDesign.AnimInnerW);
            s.sky = MillSpriteFactory.VeTroi("mill_sky_" + gw + "x" + (int)MillDesign.SkyH,
                gw, (int)MillDesign.SkyH, MillDesign.AnimRadius);
            s.ground = MillSpriteFactory.VeDat("mill_ground_" + gw + "x" + (int)MillDesign.GroundH,
                gw, (int)MillDesign.GroundH, MillDesign.AnimRadius);

            // Chấm trạng thái — vẽ TRẮNG vì MillPopupUI tự tô mauDotDangXay / mauDotRanh.
            s.dotGreen = MillSpriteFactory.VeDia("mill_dot", (int)MillDesign.DotSize,
                Color.white, 2f, new Color(0f, 0f, 0f, 0.28f));

            // Ổ khoá trắng + dấu X trắng (không lệ thuộc glyph của font).
            s.lockGlyph  = MillSpriteFactory.VeOKhoa("mill_glyph_lock",
                (int)MillDesign.SlotLockGlyph);
            s.closeGlyph = MillSpriteFactory.VeDauX("mill_glyph_x", 44);

            s.redDot = MillSpriteFactory.VeDia("mill_reddot", (int)MillDesign.RedDotSize,
                Hex(MillDesign.CRedDot), 3f, Color.white);

            // VÒNG SÁNG THẢ — CHỈ có viền, lòng TRONG SUỐT (fill = clear ⇒ PixelKhoi đi nhánh
            // "khối chỉ có viền") nên nội dung slot vẫn thấy nguyên qua giữa vòng.
            //  56×56 để VeKhoi tự tính 9-slice (18+6)×2 = 48 texel mỗi mép trên texture 112
            //  (48 < 112/2 − 1 = 55 ⇒ KHÔNG bị kẹp) ⇒ giãn tới 186×198 mà 4 góc không méo.
            s.dropRing = MillSpriteFactory.VeKhoi("mill_drop_ring",
                MillSpriteFactory.K(56, 56, MillDesign.DropRingRadius,
                    MillDesign.DropRingBorder, Color.clear, Hex(MillDesign.CBtnGreen)));

            // QUẦNG SÁNG BAO THÀNH PHẨM — màu BAKE sẵn vì MillOutputBagFX chỉ đổi alpha.
            s.bagGlow = MillSpriteFactory.VeQuangSang("mill_bag_glow",
                Mathf.RoundToInt(MillDesign.BagGlowSize), Hex(MillDesign.CBagGlow),
                MillDesign.BagGlowMem);

            // Bó cỏ trên băng tải: PLACEHOLDER, lead thay bằng icon lúa mì thật.
            s.itemGrain = MillSpriteFactory.VeKhoi("mill_item_grain",
                MillSpriteFactory.K(26, 26, 10f, 2f, Hex(MillDesign.CItemGrain),
                    Hex(MillDesign.CItemGrainBd)));

            // ── 10. ART HIỆU ỨNG PHÁO BÔNG + KHÓI ───────────────────────────────
            //  Nạp theo ĐƯỜNG DẪN ASSET (không qua 4 thư mục popup) vì đây là art dùng chung.
            //  Thiếu file thì để null: hai FX này TUỲ CHỌN, null chỉ mất hiệu ứng.
            var giay = new List<Sprite>();
            foreach (string p in new[] { FxConfetti, FxSquare, FxPlus })
            {
                Sprite g = MillSpriteFactory.TimTheoDuongDan(p);
                if (g != null) giay.Add(g);
            }
            s.fxGiay = giay.ToArray();
            s.fxSao      = MillSpriteFactory.TimTheoDuongDan(FxStar);
            s.fxLoe      = MillSpriteFactory.TimTheoDuongDan(FxFlare);
            s.fxKhoi     = MillSpriteFactory.TimTheoDuongDan(FxSmoke);
            s.fxBongBong = MillSpriteFactory.TimTheoDuongDan(FxCircle);

            // ── Ghi rõ chỗ tool CỐ Ý không theo mockup HTML, để không ai phải soi code ──
            rep.Can("TRỜI/ĐẤT đã VẼ LẠI theo yêu cầu 21/08: trời xanh #79BFED → gần trắng " +
                    "#F4FAFD, đất nâu đậm #9B7956 → #7E6346 + vạch chân trời #5E4934. CSS " +
                    "mockup (full_mill_ui.html dòng 20-23, 317, 320-323) vốn là trời BẠC HÀ " +
                    "#E6F3E6 → #CBE6CF và đất #B48D64 PHẲNG — không có art SVG nào cho hai " +
                    "mảng này. Muốn về đúng mockup: dùng lại CSkyTop/CSkyBottom/CGroundMain/" +
                    "CGroundStripe (đã bỏ khỏi MillDesign, xem git).");
            rep.Can("BẢNG GỢI Ý `Btn_Main` nay là art shop_toast (XANH LÁ). MillPopupUI dòng " +
                    "700 vẫn TÔ MÀU node này, nên tool đã đặt mauNutBamDuoc = TRẮNG TINH " +
                    "(phép nhân đơn vị ⇒ art giữ đúng màu) và mauNutKhoa = xám trung tính " +
                    "#B3ADA0 (làm tối đều). KHÔNG đổi mauNutKhoa về kem #D9CDB9: kem × xanh " +
                    "lá = xanh ô-liu bẩn. Không cần sửa MillPopupUI.");
            rep.Can("ĐÃ BỎ: 4 đinh tán vẽ tay ở góc Window (popup_board đã bake sẵn đinh tán), " +
                    "node viền `AnimBox_Frame` (nay là khung gỗ panel_outer thật), 4 con lăn vẽ " +
                    "tay (conveyor_base đã bake), và chữ 'Mở slot' ở slot khoá (ở 192px nó đè " +
                    "lên nút kim cương; ổ khoá + giá đã đủ nghĩa). Sprite slot_normal / " +
                    "ribbon_header / tab_active / tab_inactive CỐ Ý không dùng.");

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

            // BẮT BUỘC: không có nhánh vẽ thay thế nào cho những mảnh này.
            C(popupBoard, "popup_board", true);
            C(panelInner, "panel_inner", true);
            C(bannerRibbon, "shop_banner_ribbon", true);
            C(panelWood, "panel_outer", true);
            C(machineBody, "machine_body", true);
            C(gearLarge, "gear_large", true);
            C(gearSmall, "gear_small", true);
            C(cardActive, "recipe_card_active", true);
            C(cardInactive, "recipe_card_inactive", true);
            C(cardLocked, "recipe_card_locked", true);
            if (beltTex == null)
                rep.Loi("THIẾU texture sọc băng tải — UIScrollingTexture sẽ không chạy " +
                        "(nó cần RawImage CÓ texture, Wrap = Repeat).");

            // TUỲ CHỌN: đều có nhánh vẽ tay thay thế ngay tại chỗ gán (xem `Tao`).
            C(btnClose, "btn_close", false);
            C(chipBg, "shop_currency_chip", false);
            C(toastBg, "shop_toast", false);
            C(listPanel, "inner_panel", false);
            C(pillBg, "badge_count", false);
            C(circlePlate, "shop_circle_plate", false);
            C(circlePreview, "circle_preview", false);
            C(slotFrame, "shop_card_outer", false);
            C(slotFill, "shop_card_inner", false);
            C(slotDashed, "slot_empty", false);
            C(slotReady, "slot_selected", false);
            C(lockBadge, "shop_lock_badge", false);
            C(btnBuyGold, "shop_btn_buy_gold", false);
            C(btnBuyGem, "shop_btn_buy_gem", false);
            C(btnBuyLocked, "shop_btn_buy_locked", false);
            C(btnGreen, "btn_green", false);
            C(btnBlue, "btn_blue", false);
            if (!beltLaArt)
                rep.Canh("Thiếu conveyor_base.png — băng tải quay lại mâm vẽ tay (phẳng, " +
                         "KHÔNG có con lăn). Không chặn.");

            // Hiệu ứng pháo bông / khói: TUỲ CHỌN — null là mất hiệu ứng, không mất chức năng.
            if (fxGiay == null || fxGiay.Length == 0)
                rep.Canh("Không nạp được mảnh giấy pháo bông nào (confetti_large / Square01 / " +
                         "Plus01) — MillCelebrationFX sẽ không có giấy để bắn.");
            C(fxSao, MillSkin.FxStar, false);
            C(fxLoe, MillSkin.FxFlare, false);
            C(fxKhoi, MillSkin.FxSmoke, false);
            C(fxBongBong, MillSkin.FxCircle, false);
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

        // Hiệu ứng (chốt 21/08 — luồng kéo-thả). Cả ba là TUỲ CHỌN với MillPopupUI:
        // để null thì logic xay vẫn đúng, chỉ mất phần phản hồi thị giác.
        public MillIntakeFX     fxNguyenLieu;
        public MillOutputBagFX  fxBaoRa;
        public MillCollectFlyFX fxBayVeKho;

        // Pháo bông (xay xong) + khói máy (đang xay) — chốt 21/08, cùng nằm trên
        // `AnimationBox`. Cũng là TUỲ CHỌN với MillPopupUI.
        public MillCelebrationFX fxPhaoHoa;
        public MillSmokeFX       fxKhoi;
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

            // ══════════════════════════════════════════════════════════════════════════
            //  ⚠⚠ BẮT BUỘC — THIẾU DÒNG NÀY LÀ POPUP THÀNH MỘT TẤM ẢNH  (bug 21/08)
            // ══════════════════════════════════════════════════════════════════════════
            // Canvas LỒNG đăng ký toàn bộ Graphic con của nó vào CHÍNH NÓ trong
            // `GraphicRegistry`, không vào canvas cha. `GraphicRaycaster` chỉ raycast
            // những Graphic đăng ký vào canvas mà NÓ đang nằm trên. Vì vậy
            // GraphicRaycaster của `Canvas_Popup` KHÔNG "thấy" bất cứ thứ gì bên trong
            // `MillPopup_Root`.
            //
            // Hệ quả khi thiếu: popup vẽ ra HOÀN HẢO — khung gỗ, ruy băng, card, slot,
            // bánh răng quay, băng tải chạy — nhưng KHÔNG có một pixel nào hit-test được.
            // Nút X không bấm được, card không chọn được, nút THU không bấm được, kéo-thả
            // không nổ OnBeginDrag. Không có lỗi đỏ, không có warning, Console sạch trơn.
            // Đây là lý do bản trước bị đánh giá "như 1 image dựng lên, chả thấy logic nào".
            //
            // Kiểm nhanh khi nghi ngờ lại: Play Mode → EventSystem trong Inspector, mục
            // "Pointer 1 ... Selected/PointerEnter" phải đổi khi rê chuột trên popup.
            //
            // ĐỪNG XOÁ. ĐỪNG chuyển sang canvas cha. Nếu bỏ Canvas lồng thì mới bỏ được
            // dòng này — nhưng lúc đó popup mất sortingOrder riêng.
            MillUI.Comp<GraphicRaycaster>(root);

            MillUI.Comp<MillPopupUI>(root);           // ⚠ Comp dùng `== null`, không dùng `??`

            // ── HIỆU ỨNG BAY VỀ KHO — gắn TRÊN ROOT vì nó cần đúng Canvas này (order 400).
            //    Gắn ở Canvas_HUD (order 100) là icon bay DƯỚI popup rồi biến mất.
            //    `diemDen` CỐ Ý để TRỐNG: đích là nút KHO của HUD do tool khác dựng, wire
            //    ở đây sẽ tạo phụ thuộc chéo hai tool. Script tự tìm qua
            //    TownshipHUDController.Instance.btnTabWarehouse, không thấy thì bay về góc
            //    dưới-trái màn hình.
            var fxFly = MillUI.Comp<MillCollectFlyFX>(root);
            var soFly = new SerializedObject(fxFly);
            MillWiring.W(soFly, "canvasBay", cv, rep, "MillCollectFlyFX");
            soFly.ApplyModifiedPropertiesWithoutUndo();
            b.fxBayVeKho = fxFly;

            // ── PopupRoot: node BỊ TẮT/BẬT (root phải luôn active để Awake set Instance) ──
            RectTransform popupRoot = MillUI.Stretch(MillUI.Node(root.transform, "PopupRoot"),
                                                     0f, 0f, 0f, 0f);
            b.popupRoot = popupRoot.gameObject;

            // Lớp tối phía sau — CHẶN click xuống world khi popup mở.
            Image dim = MillUI.Img(MillUI.Stretch(MillUI.Node(popupRoot, "Dim"), 0f, 0f, 0f, 0f),
                                   null, new Color(0f, 0f, 0f, 0.55f), true);
            dim.sprite = null;

            // ── WINDOW — 1560×900 (81% × 83% của 1920×1080) ─────────────────────────
            //  ⚠ `PopupLechY` = −10 (dịch XUỐNG): ruy băng nhô 92px lên trên mép Window, đặt
            //    Window đúng tâm màn hình là đỉnh ruy băng ra y = −2, BỊ CẮT. Xem MillDesign.
            //  ⚠ ĐÃ BỎ 4 node `Rivet_*`: popup_board.png đã BAKE SẴN đinh tán ở 4 góc, thêm
            //    4 đĩa 16px vẽ tay nữa là hai lớp đinh lệch nhau (mock.py duyệt không có).
            RectTransform win = MillUI.CC(MillUI.Node(popupRoot, "Window"),
                                          MillDesign.PopupW, MillDesign.PopupH,
                                          0f, MillDesign.PopupLechY);
            win.localScale = Vector3.one * MillDesign.TiLeHienThi;
            MillUI.Img(win, sk.popupBoard, Color.white, true);

            // ── INNER PANEL — padding 44 của Window ⇒ 1472×812 ──────────────────────
            RectTransform panel = MillUI.Stretch(MillUI.Node(win, "InnerPanel"),
                MillDesign.PopupPad, MillDesign.PopupPad, MillDesign.PopupPad, MillDesign.PopupPad);
            MillUI.Img(panel, sk.panelInner, Color.white);

            DungChipGem(panel, sk, b);

            // Ô nội dung 1420×684 tại (26, 102) của InnerPanel — mock.py CX/CY/CW/CH.
            RectTransform content = MillUI.TL(MillUI.Node(panel, "Content"),
                MillDesign.ContentW, MillDesign.ContentH,
                MillDesign.ContentLeft, MillDesign.ContentTop);

            DungCotCongThuc(content, sk, b);
            DungCotPhai(content, sk, b, rep);

            // ── RUY BĂNG + NÚT X + TOAST: tạo SAU panel để vẽ ĐÈ LÊN ────────────────
            DungRuyBang(win, sk, b);
            DungNutDong(win, sk, b);
            DungToast(win, sk, b);

            if (cardPrefab == null)
                rep.Canh("Prefab card công thức chưa dựng được ⇒ danh sách công thức sẽ TRỐNG " +
                         "lúc runtime (MillPopupUI.DungDanhSachCard cần recipeCardPrefab).");

            SoatHinhHoc(rep);

            rep.Ok("Dựng xong hierarchy: " + DemNode(root.transform) + " node.");
            return b;
        }

        // ═════════════════════ SOÁT HÌNH HỌC (chạy mỗi lần dựng) ═════════════════════

        /// <summary>
        /// Kiểm mọi phép soát tràn BẰNG CODE, không chỉ bằng chú thích.
        ///
        /// Lý do tồn tại: bản trước có đủ chú thích "đã soát tràn" nhưng số thật thì chồng
        /// nhau (tên slot 87..117 và thanh tiến độ 111..125). Chú thích không chạy được, hàm
        /// này chạy được — ai sửa một hằng số trong `MillDesign` mà làm vỡ hình học thì thấy
        /// LỖI ĐỎ ở lệnh 1, không phải thấy popup xấu ba tuần sau.
        /// </summary>
        private static void SoatHinhHoc(MillReport rep)
        {
            void Can(bool ok, string mo)
            {
                if (ok) rep.Ok("SOÁT ✔ " + mo);
                else rep.Loi("SOÁT ✘ " + mo);
            }

            // ── 1. Popup nằm trong màn hình tham chiếu ──────────────────────────
            float winTop = (MillDesign.ManHinhH - MillDesign.PopupH) * 0.5f
                         - MillDesign.PopupLechY;                            // 100
            float winLeft = (MillDesign.ManHinhW - MillDesign.PopupW) * 0.5f;  // 180
            Can(MillDesign.PopupW <= MillDesign.ManHinhW &&
                MillDesign.PopupH <= MillDesign.ManHinhH,
                "Window " + MillDesign.PopupW + "×" + MillDesign.PopupH + " nằm trong " +
                MillDesign.ManHinhW + "×" + MillDesign.ManHinhH);
            Can(winTop + MillDesign.RibbonTop >= 0f,
                "đỉnh ruy băng ở y = " + (winTop + MillDesign.RibbonTop) + " ≥ 0 " +
                "(PopupLechY = " + MillDesign.PopupLechY + " tồn tại CHỈ vì phép soát này)");
            float xNutPhai = winLeft + MillDesign.PopupW - MillDesign.CloseOffRight;
            Can(xNutPhai <= MillDesign.ManHinhW,
                "mép phải nút X ở x = " + xNutPhai + " ≤ " + MillDesign.ManHinhW);

            // ── 2. Ô nội dung khớp hai cột ─────────────────────────────────────
            Can(Mathf.Approximately(MillDesign.RecipeListW + MillDesign.ColGap
                                    + MillDesign.RightW, MillDesign.ContentW),
                "hai cột " + MillDesign.RecipeListW + " + " + MillDesign.ColGap + " + " +
                MillDesign.RightW + " = " + MillDesign.ContentW);

            // ── 3. Bảng danh sách chứa đúng 3 card ─────────────────────────────
            float ba = MillDesign.CardH * 3f + MillDesign.CardGap * 2f;      // 470
            float bon = MillDesign.CardH * 4f + MillDesign.CardGap * 3f;     // 630
            Can(ba <= MillDesign.ScrollH && bon > MillDesign.ScrollH,
                "3 card = " + ba + " ≤ viewport " + MillDesign.ScrollH + " < 4 card = " + bon);

            // ── 4. Nội dung card 150px ─────────────────────────────────────────
            Can(MillDesign.CardChipTop + MillDesign.CardChipH <= MillDesign.CardH,
                "chip nguyên liệu card hết ở y " +
                (MillDesign.CardChipTop + MillDesign.CardChipH) + " ≤ " + MillDesign.CardH);
            Can(MillDesign.CardNameCy + MillDesign.CardNameH * 0.5f <= MillDesign.CardTagTop,
                "đáy hộp tên card " + (MillDesign.CardNameCy + MillDesign.CardNameH * 0.5f) +
                " ≤ đỉnh thẻ con vật " + MillDesign.CardTagTop + " (hai hàng khác nhau)");
            Can(MillDesign.CardTextLeft + MillDesign.CardChipW * 2f + MillDesign.CardChipGap
                <= MillDesign.CardW,
                "2 chip nguyên liệu hết ở x " + (MillDesign.CardTextLeft +
                MillDesign.CardChipW * 2f + MillDesign.CardChipGap) + " ≤ " + MillDesign.CardW);

            // ── 5. Khu animation + đĩa thành phẩm ──────────────────────────────
            float macTrai = MillDesign.AnimInnerW - MillDesign.MachineRight
                          - MillDesign.MachineSize;                          // 380
            float macPhai = macTrai + MillDesign.MachineSize;                // 660
            float diaTrai = MillDesign.AnimInnerW - MillDesign.OutRight - MillDesign.OutPlate;
            Can(diaTrai > macPhai,
                "đĩa thành phẩm bắt đầu x " + diaTrai + " > mép phải máy xay x " + macPhai +
                " (khe " + (diaTrai - macPhai) + "px, KHÔNG chồng)");
            Can(MillDesign.OutTop + MillDesign.OutPlate <= MillDesign.AnimInnerH,
                "đáy đĩa y " + (MillDesign.OutTop + MillDesign.OutPlate) + " ≤ lòng khung " +
                MillDesign.AnimInnerH);
            Can(MillDesign.OutTop + MillDesign.OutPlate - MillDesign.OutTagBottom
                <= MillDesign.AnimInnerH,
                "đáy nhãn sản phẩm y " + (MillDesign.OutTop + MillDesign.OutPlate
                - MillDesign.OutTagBottom) + " ≤ lòng khung " + MillDesign.AnimInnerH);
            Can(MillDesign.BeltTop + MillDesign.BeltH <= MillDesign.AnimInnerH,
                "đáy băng tải y " + (MillDesign.BeltTop + MillDesign.BeltH) + " ≤ lòng khung " +
                MillDesign.AnimInnerH);

            // Alpha quầng sáng ở mép GẦN NHẤT của lòng khung — mức chuẩn 0.046 của bản 160 cũ.
            float tamX = MillDesign.AnimInnerW - MillDesign.OutRight - MillDesign.OutPlate * 0.5f;
            float d = MillDesign.AnimInnerW - tamX;                          // 142
            float R = MillDesign.BagGlowSize * 1.18f * 0.5f;                 // 174.64
            float alpha = 0.8f * Mathf.Pow(Mathf.Max(0f, 1f - d / R), MillDesign.BagGlowMem);
            // Ngưỡng 0.04215 = alpha mép của bản 160 cũ (đĩa 112, d 76, R 94.4) — mức đã
            // được chấp nhận bằng mắt. KHÔNG dùng 0.046 (mốc của bản 128, lỏng hơn).
            Can(alpha <= 0.04215f,
                "alpha quầng sáng ở mép lòng khung = " + alpha.ToString("0.0000") +
                " ≤ 0.04215 (mức đã được chấp nhận của bản 160 cũ). d = " + d + ", R = " + R);

            // ── 6. Nội dung card slot 192px ────────────────────────────────────
            float lotDay = MillDesign.SlotsH - MillDesign.SlotInset;         // 183
            float dayNut = MillDesign.SlotsH - MillDesign.SlotBtnBottom;     // 182
            float dinhNut = dayNut - MillDesign.SlotBtnH;                    // 140
            float dayThanh = MillDesign.SlotsH - MillDesign.SlotProgBottom;  // 134
            float dinhThanh = dayThanh - MillDesign.SlotProgH;               // 114
            float dayTen = MillDesign.SlotNameTop + MillDesign.SlotNameH;    // 112
            float dayDia = MillDesign.SlotIconPlateTop + MillDesign.SlotIconPlate;  // 84
            Can(Mathf.Approximately(MillDesign.SlotNoiH,
                                    MillDesign.SlotsH - MillDesign.SlotInset * 2f),
                "lòng card slot cao " + MillDesign.SlotNoiH);
            Can(dayNut <= lotDay,
                "đáy nút slot y " + dayNut + " ≤ mép lớp kem " + lotDay);
            Can(dayThanh <= dinhNut,
                "đáy thanh tiến độ y " + dayThanh + " ≤ đỉnh nút y " + dinhNut);
            Can(dayTen <= dinhThanh,
                "đáy hộp tên y " + dayTen + " ≤ đỉnh thanh tiến độ y " + dinhThanh);
            Can(dayDia <= MillDesign.SlotNameTop,
                "đáy đĩa icon y " + dayDia + " ≤ đỉnh hộp tên y " + MillDesign.SlotNameTop);
            // Đĩa của trạng thái Trống + chữ "Trống"
            float dayDiaTrong = MillDesign.SlotEmptyPlateTop + MillDesign.SlotEmptyPlate;
            float dinhChuTrong = MillDesign.SlotEmptyCy - MillDesign.SlotEmptyH * 0.5f;
            Can(dayDiaTrong <= dinhChuTrong,
                "đáy đĩa 'Trống' y " + dayDiaTrong + " ≤ đỉnh chữ 'Trống' y " + dinhChuTrong);
            Can(MillDesign.SlotEmptyCy + MillDesign.SlotEmptyH * 0.5f <= MillDesign.SlotsH,
                "đáy chữ 'Trống' y " + (MillDesign.SlotEmptyCy + MillDesign.SlotEmptyH * 0.5f) +
                " ≤ " + MillDesign.SlotsH);
            // Slot khoá: ổ khoá → nút kim cương / viên thuốc
            float dayKhoa = MillDesign.SlotLockTop + MillDesign.SlotLockBadge;      // 128
            float dinhNutGem = MillDesign.SlotsH - MillDesign.SlotGemBtnBottom
                             - MillDesign.SlotGemBtnH;                              // 134
            float dinhPill = MillDesign.SlotsH - MillDesign.SlotPillBottom
                           - MillDesign.SlotPillH;                                  // 128
            Can(dayKhoa <= dinhNutGem,
                "đáy ổ khoá y " + dayKhoa + " ≤ đỉnh nút kim cương y " + dinhNutGem);
            Can(dayKhoa <= dinhPill,
                "đáy ổ khoá y " + dayKhoa + " ≤ đỉnh viên thuốc 'Cấp' y " + dinhPill);
            Can(MillDesign.SlotsH - MillDesign.SlotGemBtnBottom <= lotDay,
                "đáy nút kim cương y " + (MillDesign.SlotsH - MillDesign.SlotGemBtnBottom) +
                " ≤ mép lớp kem " + lotDay);
            // "#N" không bị đĩa icon đè: mép TRÁI đĩa phải ở phải mép PHẢI của "#N"
            float diaTraiSlot = (MillDesign.SlotW - MillDesign.SlotIconPlate) * 0.5f;
            float nEnd = MillDesign.SlotInset + MillDesign.SlotNumLeft
                       + MillDesign.SlotNumFont * 1.1f;
            Can(diaTraiSlot >= nEnd,
                "mép trái đĩa icon x " + diaTraiSlot.ToString("0.0") + " ≥ mép phải '#N' x " +
                nEnd.ToString("0.0"));

            // ── 7. Hàng slot phủ đúng bề rộng cột phải ─────────────────────────
            float tong = MillDesign.SlotW * MillDesign.SlotCount
                       + MillDesign.SlotGap * (MillDesign.SlotCount - 1);
            Can(Mathf.Abs(tong - MillDesign.RightW) < 0.01f,
                MillDesign.SlotCount + " slot " + MillDesign.SlotW.ToString("0.0") + " + " +
                (MillDesign.SlotCount - 1) + " khe " + MillDesign.SlotGap + " = " +
                tong.ToString("0.0") + " = cột phải " + MillDesign.RightW +
                " (mock.py hụt 40px ở đây — xem ghi chú đầu MillDesign)");
            Can(Mathf.Approximately(MillDesign.SlotsTop + MillDesign.SlotsH,
                                    MillDesign.ContentH),
                "hàng slot hết ở y " + (MillDesign.SlotsTop + MillDesign.SlotsH) +
                " = đáy ô nội dung " + MillDesign.ContentH);
        }


        // ═════════════════════ CHIP KIM CƯƠNG (góc phải-trên panel) ═════════════════════

        /// <summary>
        /// mock.py GW/GH — chip 224×68 neo góc PHẢI-TRÊN của InnerPanel, cách 24/22.
        /// Dự án đã BỎ hệ tab nên hàng trên chỉ còn chip này (không dựng node tab nào).
        ///
        /// Bề rộng CỐ ĐỊNH 224, KHÔNG ContentSizeFitter: mock.py vẽ chip cỡ cố định, và số dư
        /// kim cương đổi từng giây — để chip tự co là mép phải nhảy theo từng chữ số.
        /// </summary>
        private static void DungChipGem(RectTransform panel, MillSkin sk, MillBuilt b)
        {
            RectTransform chip = MillUI.TR(MillUI.Node(panel, "Chip_Gem"),
                MillDesign.ChipW, MillDesign.ChipH,
                MillDesign.ChipRight, MillDesign.ChipTop);
            MillUI.Img(chip, sk.chipBg, Color.white);
            var hg = MillUI.HangNgang(chip, MillDesign.ChipGap,
                                      MillDesign.ChipPadX, MillDesign.ChipPadX, 0f, 0f, false);
            hg.childAlignment = TextAnchor.MiddleLeft;   // icon rồi số, dồn về trái

            RectTransform ic = MillUI.Node(chip, "Icon_Gem");
            MillUI.Img(ic, sk.gemIcon, Color.white);
            MillUI.CoDinh(ic, MillDesign.ChipGemIcon, MillDesign.ChipGemIcon);

            RectTransform tx = MillUI.Node(chip, "Txt_GemBalance");
            b.txtGemBalance = MillUI.Txt(tx, "0", MillDesign.ChipFont,
                                         MillSpriteFactory.Hex(MillDesign.CTextBrown),
                                         TextAlignmentOptions.Left);
            MillUI.CoDinhCao(tx, MillDesign.ChipFont + 8f);
        }

        // ═════════════════════ CỘT CÔNG THỨC (trái) ═════════════════════

        private static void DungCotCongThuc(RectTransform content, MillSkin sk, MillBuilt b)
        {
            RectTransform col = MillUI.TL(MillUI.Node(content, "RecipeColumn"),
                MillDesign.RecipeListW, MillDesign.ContentH, 0f, 0f);

            // ── Header "CÔNG THỨC" — CÙNG art ruy băng với tiêu đề popup ────────────
            //  mock.py: shop_banner_ribbon 340×76, canh giữa cột. Ruy băng có đuôi cờ nên
            //  Type = Simple (xem MillSkin.bannerRibbon).
            RectTransform hdr = MillUI.TL(MillUI.Node(col, "Header_Ribbon"),
                MillDesign.ListHeaderW, MillDesign.ListHeaderH,
                (MillDesign.RecipeListW - MillDesign.ListHeaderW) * 0.5f, 0f);
            MillUI.Img(hdr, sk.bannerRibbon, Color.white);
            var th = MillUI.Txt(MillUI.TL(MillUI.Node(hdr, "Txt_ListHeader"),
                    MillDesign.ListHeaderW, MillDesign.ListHeaderH * MillDesign.ListHeaderTiLeChu,
                    0f, 0f),
                "CÔNG THỨC", MillDesign.ListHeaderFont, Color.white, TextAlignmentOptions.Center);
            MillUI.Vien(th, 0.14f, MillSpriteFactory.Hex(MillDesign.CVienHeader));

            // ── Bảng danh sách — art inner_panel (khay kem viền nâu), 9-slice 26 ───
            RectTransform box = MillUI.TL(MillUI.Node(col, "RecipeList"),
                MillDesign.RecipeListW, MillDesign.ListH, 0f, MillDesign.ListTop);
            MillUI.Img(box, sk.listPanel, Color.white);

            // Viewport thụt đúng lề mock.py: 16 hai bên, 8 trên, 6 dưới ⇒ 388×484.
            //  SOÁT: 3 card = 150×3 + 10×2 = 470 ≤ 484 ✔ (card thứ 4 cần 630 ⇒ phải cuộn).
            RectTransform sv = MillUI.Stretch(MillUI.Node(box, "ScrollView"),
                MillDesign.ListPadX, MillDesign.ListPadTop,
                MillDesign.ListPadX, MillDesign.ListPadBot);
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
            vlg.spacing = MillDesign.CardGap;
            vlg.padding = new RectOffset(0, 0, 0, 0);
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
            scroll.scrollSensitivity = 40f;
            scroll.inertia = true;
            b.recipeContainer = ct;

            // ── BẢNG GỢI Ý (node vẫn tên `Btn_Main`) ───────────────────────────────
            //
            // Chốt 21/08: bắt đầu một mẻ xay = KÉO card công thức và THẢ vào slot, không còn
            // nút "XAY NGAY". `MillPopupUI.GanSuKienNut` TẮT component Button (không dùng
            // interactable = false — nó bật Disabled tint alpha 0.55 làm bảng mờ như bị lỗi),
            // còn `MillPopupUI.CapNhatNutLon` đổi chữ + màu nền theo 4 trạng thái.
            //
            // ⚠ GIỮ NGUYÊN tên node `Btn_Main`, component Button và cả ba field wire
            // (`btnMain` / `txtMainButton` / `imgMainButtonBg`) — MillWiring tra field theo
            // TÊN và MillAudit vẫn soát đủ ba field; đổi là đứt wiring âm thầm.
            //
            // ⚠ ART = `shop_toast`, KHÔNG PHẢI `btn_green` và KHÔNG PHẢI sprite trắng vẽ tay.
            //   btn_green là một khối xanh PHẲNG; shop_toast có vệt sáng trắng ngang trên +
            //   viền đậm nên đọc ra hình khối nhô lên. Việc MillPopupUI TÔ MÀU node này được
            //   xử lý bằng cách đặt mauNutBamDuoc = TRẮNG TINH và mauNutKhoa = xám trung tính
            //   trong `MillWiring.WirePopup` — KHÔNG cần sửa MillPopupUI. Đọc khối ghi chú ở
            //   đó trước khi đổi bất cứ thứ gì ở đây.
            //
            // CỠ CHỮ 32 (MillDesign.HintFont). Ô chữ rộng 396px (420 − 24). Đo trên ẢNH DUYỆT
            // với font Baloo2: "KÉO VÀO SLOT ĐỂ XAY" cỡ 32 ≈ 307px ⇒ 16.2px/ký tự. Ba nhãn
            // còn lại đều NGẮN HƠN: "CHỌN MỘT CÔNG THỨC" 18 ký tự ≈ 291px,
            // "THIẾU NGUYÊN LIỆU" ≈ 275px, "HẾT SLOT TRỐNG" ≈ 227px ⇒ tất cả < 396 ✔
            // ⚠ Đây là số đo trên Baloo2; font thật của dự án là FontVo. Nếu FontVo rộng hơn
            //   ~29% thì nhãn TRÀN (Txt dựng NoWrap + Overflow, chữ chạy ra ngoài mép bảng
            //   chứ không bị cắt). Thấy tràn thì hạ cỡ ở ĐÚNG node này cho CẢ 4 nhãn —
            //   runtime chỉ đổi `.text`, không bao giờ đổi cỡ chữ.
            RectTransform bm = MillUI.TL(MillUI.Node(col, "Btn_Main"),
                MillDesign.RecipeListW, MillDesign.HintH, 0f, MillDesign.HintTop);
            b.imgMainButtonBg = MillUI.Img(bm, sk.toastBg, Color.white, true);
            b.btnMain = MillUI.Btn(b.imgMainButtonBg);

            b.txtMainButton = MillUI.Txt(MillUI.TL(MillUI.Node(bm, "Txt_MainButton"),
                                                   MillDesign.RecipeListW - 24f, MillDesign.HintH,
                                                   12f, 0f),
                "KÉO VÀO SLOT ĐỂ XAY", MillDesign.HintFont, Color.white,
                TextAlignmentOptions.Center);
            MillUI.Vien(b.txtMainButton, 0.12f, MillSpriteFactory.Hex(MillDesign.CVienXanhLa));
        }

        private static int DemNode(Transform t)
        {
            int n = 1;
            for (int i = 0; i < t.childCount; i++) n += DemNode(t.GetChild(i));
            return n;
        }

        // ═════════════════════ CỘT PHẢI: Ô ANIMATION + KHU SLOT ═════════════════════

        private static void DungCotPhai(RectTransform content, MillSkin sk, MillBuilt b,
                                        MillReport rep)
        {
            RectTransform col = MillUI.TL(MillUI.Node(content, "RightColumn"),
                MillDesign.RightW, MillDesign.ContentH,
                MillDesign.RecipeListW + MillDesign.ColGap, 0f);

            DungOAnimation(col, sk, b, rep);
            DungKhuSlot(col, sk, b, rep);
        }

        /// <summary>
        /// Khu animation — KHUNG GỖ 974×418 (`panel_outer`) + lòng khung 934×378.
        ///
        /// ⚠ KHÔNG CÓ MASK, VÀ ĐÓ LÀ QUYẾT ĐỊNH CÓ CHỦ ĐÍCH:
        ///   nhánh `Conveyor` đã mang một `Mask` (stencil) để cắt lớp sọc theo alpha mâm băng
        ///   tải. Chồng thêm một RectMask2D lên `AnimationBox` là hai cơ chế cắt khác loại
        ///   trên cùng một layout đã chốt — rủi ro lớn hơn nhiều so với lợi ích.
        ///   ⇒ Khói và quầng sáng được giữ trong khung bằng HÌNH HỌC:
        ///      • máy cao 280 với lề đáy 24 ⇒ còn ĐÚNG 74px trời trên miệng phễu, trùng khít
        ///        `MillSmokeFX.caoBay` mặc định (74f). ĐỪNG bóp khoảng này.
        ///      • `BagGlowSize` 300 được tính để alpha ở mép lòng khung chỉ còn 0.043
        ///        (xem phép tính đầy đủ ở MillDesign.BagGlowSize).
        ///
        /// Thứ tự con = thứ tự vẽ. Trời/đất trước, rồi băng tải, rồi máy (băng tải chạy VÀO
        /// chân máy nên máy phải vẽ sau), rồi quầng sáng, rồi đĩa thành phẩm.
        /// Khung gỗ KHÔNG cần node riêng: nó là Image của node CHA, mọi thứ khác thụt vào 20px
        /// nên không bao giờ che lên ván gỗ.
        /// </summary>
        private static void DungOAnimation(RectTransform col, MillSkin sk, MillBuilt b,
                                           MillReport rep)
        {
            RectTransform frame = MillUI.TL(MillUI.Node(col, "AnimFrame"),
                MillDesign.AnimW, MillDesign.AnimH, 0f, 0f);
            MillUI.Img(frame, sk.panelWood, Color.white);

            // ⚠ TÊN NODE PHẢI LÀ "AnimationBox": MillAudit.SoatHieuUng kiểm 4 component hiệu
            //   ứng có nằm trên node tên này hay không.
            RectTransform box = MillUI.Stretch(MillUI.Node(frame, "AnimationBox"),
                MillDesign.AnimVienGo, MillDesign.AnimVienGo,
                MillDesign.AnimVienGo, MillDesign.AnimVienGo);

            float W = MillDesign.AnimInnerW;

            // Trời 60% / đất 40% lòng khung; hai sprite đã bo sẵn 2 góc tương ứng (r16).
            MillUI.Img(MillUI.TL(MillUI.Node(box, "Sky"), W, MillDesign.SkyH, 0f, 0f),
                       sk.sky, Color.white);
            MillUI.Img(MillUI.BL(MillUI.Node(box, "Ground"), W, MillDesign.GroundH, 0f, 0f),
                       sk.ground, Color.white);

            // ── Badge trạng thái — art shop_currency_chip 268×62 ───────────────────
            RectTransform badge = MillUI.TL(MillUI.Node(box, "Badge_Status"),
                MillDesign.BadgeW, MillDesign.BadgeH,
                MillDesign.BadgeLeft, MillDesign.BadgeTop);
            MillUI.Img(badge, sk.chipBg, Color.white);
            var hb = MillUI.HangNgang(badge, MillDesign.BadgeGap,
                                      MillDesign.BadgePadX, MillDesign.BadgePadX, 0f, 0f, false);
            hb.childAlignment = TextAnchor.MiddleLeft;

            RectTransform dot = MillUI.Node(badge, "Img_StatusDot");
            // Sprite chấm vẽ TRẮNG: MillPopupUI tự tô mauDotDangXay / mauDotRanh khi đổi
            // trạng thái ⇒ để trắng thì tint ra đúng màu.
            b.imgStatusDot = MillUI.Img(dot, sk.dotGreen,
                                        MillSpriteFactory.Hex(MillDesign.CDotRanh));
            MillUI.CoDinh(dot, MillDesign.DotSize, MillDesign.DotSize);

            RectTransform bt = MillUI.Node(badge, "Txt_StatusBadge");
            b.txtStatusBadge = MillUI.Txt(bt, "Máy đang rảnh", MillDesign.BadgeFont,
                MillSpriteFactory.Hex(MillDesign.CTextBrown), TextAlignmentOptions.Left);
            MillUI.CoDinhCao(bt, MillDesign.BadgeFont + 6f);

            // ── Bong bóng nguyên liệu — art inner_panel 148×84 ─────────────────────
            RectTransform bub = MillUI.TL(MillUI.Node(box, "Bubble_Input"),
                MillDesign.BubbleW, MillDesign.BubbleH,
                MillDesign.BubbleLeft, MillDesign.BubbleTop);
            MillUI.Img(bub, sk.listPanel, Color.white);
            var hu = MillUI.HangNgang(bub, MillDesign.BubbleGap,
                                      MillDesign.BubblePadX, MillDesign.BubblePadX, 0f, 0f, false);
            hu.childAlignment = TextAnchor.MiddleLeft;

            RectTransform bi = MillUI.Node(bub, "Img_InputIcon");
            b.imgInputIcon = MillUI.Img(bi, null, Color.white);
            b.imgInputIcon.enabled = false;          // MillPopupUI bật khi công thức có icon
            MillUI.CoDinh(bi, MillDesign.BubbleIcon, MillDesign.BubbleIcon);

            RectTransform bx = MillUI.Node(bub, "Txt_InputBubble");
            // ⚠ CHỮ NÂU, KHÔNG TRẮNG: nền bong bóng là art `inner_panel` màu KEM. Giữ chữ
            // trắng trên nền kem là chữ TÀNG HÌNH — lỗi kiểu "không báo gì mà mất nội dung".
            b.txtInputBubble = MillUI.Txt(bx, "x0", MillDesign.BubbleFont,
                                          MillSpriteFactory.Hex(MillDesign.CTextBrown),
                                          TextAlignmentOptions.Left);
            MillUI.CoDinhCao(bx, MillDesign.BubbleFont + 6f);

            // ── Băng tải — art conveyor_base.png 346×72 ────────────────────────────
            //  QUYẾT ĐỊNH 1 — KHÔNG 9-SLICE: ảnh bake sẵn 4 con lăn ở dải dưới, 9-slice ngang
            //    kéo giãn ĐÚNG cái dải đó ⇒ con lăn lệch khoảng cách.
            //  QUYẾT ĐỊNH 2 — CHẤP NHẬN KÉO CAO 2.08× (34.6 → 72) như bản duyệt: ở đúng tỉ lệ
            //    ảnh, băng tải chỉ dày 34.6px, mảnh như một cái gạch cạnh cái máy 280px và
            //    không còn nối được vào chân máy. Con lăn thành ô-van dọc nhưng không nhìn ra
            //    ở 100% zoom. Đây là chỗ tool CỐ Ý bỏ nguyên tắc "giữ tỉ lệ ảnh" của bản trước.
            //  QUYẾT ĐỊNH 3 — VẪN GIỮ LỚP SỌC CUỘN (bắt buộc: mất nó là băng tải "đứng"):
            //    `UIScrollingTexture` cần RawImage + texture Wrap = Repeat, thứ mà một tấm PNG
            //    tĩnh không thay thế được. Lớp sọc thụt đáy `BeltStripeChanDuoi` = 28.8px để
            //    dừng ngay TRÊN 4 con lăn vẽ sẵn.
            RectTransform belt = MillUI.TL(MillUI.Node(box, "Conveyor"),
                MillDesign.BeltW, MillDesign.BeltH, MillDesign.BeltLeft, MillDesign.BeltTop);
            Image beltImg = MillUI.Img(belt, sk.beltBase, Color.white);

            // Mask THEO ALPHA SPRITE (không phải RectMask2D): hai đầu băng tải bo góc,
            // RectMask2D chỉ cắt hình chữ nhật nên sọc sẽ tràn ra góc cong.
            var mask = MillUI.Comp<Mask>(belt.gameObject);
            mask.showMaskGraphic = true;
            beltImg.raycastTarget = false;

            float stripeBot = sk.beltLaArt ? MillDesign.BeltStripeChanDuoi : MillDesign.BeltBorder;
            RectTransform st = MillUI.Stretch(MillUI.Node(belt, "Belt_Stripes"),
                MillDesign.BeltBorder, MillDesign.BeltBorder,
                MillDesign.BeltBorder, stripeBot);
            var raw = MillUI.Comp<RawImage>(st.gameObject);
            raw.texture = sk.beltTex;
            raw.color = Color.white;
            raw.raycastTarget = false;
            // uvRect map 1 texel = 1 px màn hình ⇒ hoa văn đúng cỡ và tốc độ đúng 42px/s.
            float uw = (MillDesign.BeltW - MillDesign.BeltBorder * 2f) / MillDesign.BeltTileX;
            float uh = (MillDesign.BeltH - MillDesign.BeltBorder - stripeBot) / MillDesign.BeltTileX;
            raw.uvRect = new Rect(0f, 0f, uw, uh);

            b.belt = MillUI.Comp<UIScrollingTexture>(st.gameObject);
            b.belt.pixelsPerSecond = 42f;
            b.belt.stripePeriodPx = 30f;             // chỉ dùng nếu bật cờ dưới
            b.belt.dungChuKyHoaVan = false;          // ⚠ texture rộng ĐÚNG 42 ⇒ phải để FALSE
            b.belt.cuonTheoTrucDoc = false;
            b.belt.autoStart = false;                // MillPopupUI điều khiển qua SetRunning

            // ── Bó cỏ chạy trên băng tải — ĐÚNG 2 cái, lệch pha 1.5s trên chu kỳ 3s ──
            b.beltItems = new ConveyorItem[MillDesign.ItemCount];
            for (int i = 0; i < MillDesign.ItemCount; i++)
            {
                RectTransform it = MillUI.BL(MillUI.Node(box, "BeltItem_" + (i + 1)),
                    MillDesign.ItemSize, MillDesign.ItemSize,
                    MillDesign.ItemLeft, MillDesign.ItemBottom);
                MillUI.Img(it, sk.itemGrain, Color.white);

                var ci = MillUI.Comp<ConveyorItem>(it.gameObject);
                ci.cycleSeconds = 3f;
                ci.delaySeconds = i * 1.5f;              // MillPopupUI ghi đè
                ci.travelPx = MillDesign.ItemTravel;
                ci.overshootPx = MillDesign.ItemOvershoot;
                ci.dropPx = MillDesign.ItemDrop;
                ci.mocChay = 0.80f;
                ci.mocRoi = 0.85f;
                ci.autoStart = false;
                b.beltItems[i] = ci;
            }

            // ── Máy xay 280×280 — neo góc PHẢI-DƯỚI lòng khung ─────────────────────
            //  ⚠ `MachineBottom` = 24 là số CÓ CHỦ ĐÍCH: nó để lại ĐÚNG 74px trời trên miệng
            //    phễu cho khói bay hết trong khung (khung KHÔNG có mask). Xem MillDesign.
            RectTransform mac = MillUI.BR(MillUI.Node(box, "Machine"),
                MillDesign.MachineSize, MillDesign.MachineSize,
                MillDesign.MachineRight, MillDesign.MachineBottom);

            // Phễu + thân + highlight nằm chung machine_body.png
            MillUI.Img(MillUI.Stretch(MillUI.Node(mac, "Body"), 0f, 0f, 0f, 0f),
                       sk.machineBody, Color.white);

            b.gearLarge = DungBanhRang(mac, "Gear_Large", sk.gearLarge,
                MillDesign.GearLargeSize, MillDesign.GearLargeLeft, MillDesign.GearLargeTop, rep);
            b.gearSmall = DungBanhRang(mac, "Gear_Small", sk.gearSmall,
                MillDesign.GearSmallSize, MillDesign.GearSmallLeft, MillDesign.GearSmallTop, rep);

            // ── QUẦNG SÁNG BAO THÀNH PHẨM ──────────────────────────────────────────
            //  ⚠ TẠO NGAY TRƯỚC `Output_Bubble` — cùng cha nên THỨ TỰ SIBLING quyết định thứ
            //    tự vẽ, tạo trước ⇒ vẽ PHÍA SAU cái đĩa, đúng ý "hào quang sau đĩa".
            //  ⚠ KHÔNG làm CON của `Output_Bubble`: con LUÔN vẽ trên Image của cha, quầng sáng
            //    sẽ phủ kín mất cái đĩa.
            //  Neo cùng góc PHẢI-TRÊN với đĩa, bù `BagGlowLech` = 30 để hai TÂM trùng nhau.
            //  ⚠ 300 LÀ TRẦN — phép tính đầy đủ ở MillDesign.BagGlowSize. Nâng nữa là halo
            //    hiện thành vệt vàng nhạt tràn ra ngoài khung gỗ (khung không có mask).
            RectTransform bgl = MillUI.TR(MillUI.Node(box, "Bag_Glow"),
                MillDesign.BagGlowSize, MillDesign.BagGlowSize,
                MillDesign.OutRight - MillDesign.BagGlowLech,
                MillDesign.OutTop - MillDesign.BagGlowLech);
            // Màu TRẮNG alpha 0: màu vàng đã bake trong sprite, MillOutputBagFX chỉ nhấc
            // alpha lên (giữ nguyên RGB) ⇒ để trắng thì không tint lệch màu.
            Image imgBagGlow = MillUI.Img(bgl, sk.bagGlow, new Color(1f, 1f, 1f, 0f));
            imgBagGlow.raycastTarget = false;
            bgl.gameObject.SetActive(false);       // MillOutputBagFX bật khi có hàng chờ thu

            // ── ĐĨA THÀNH PHẨM 240px (art circle_preview) ──────────────────────────
            //  ⚠ TÊN NODE PHẢI LÀ "Output_Bubble": MillAudit.SoatHieuUng kiểm MillOutputBagFX
            //    có nằm trên node tên này hay không.
            //  Soát tràn (đĩa x 672..912 / y 70..310 trong lòng khung 934×378): xem MillDesign.
            RectTransform ob = MillUI.TR(MillUI.Node(box, "Output_Bubble"),
                MillDesign.OutPlate, MillDesign.OutPlate,
                MillDesign.OutRight, MillDesign.OutTop);
            MillUI.Img(ob, sk.circlePreview, Color.white);

            RectTransform oi = MillUI.CC(MillUI.Node(ob, "Img_OutputIcon"),
                MillDesign.OutIcon, MillDesign.OutIcon, 0f, MillDesign.OutIconLechY);
            b.imgOutputIcon = MillUI.Img(oi, null, Color.white);
            b.imgOutputIcon.enabled = false;

            // Nhãn sản phẩm — art shop_currency_chip 252×54, nhô 40px XUỐNG dưới đáy đĩa.
            RectTransform ot = MillUI.BC(MillUI.Node(ob, "Output_Tag"),
                MillDesign.OutTagW, MillDesign.OutTagH, MillDesign.OutTagBottom);
            MillUI.Img(ot, sk.chipBg, Color.white);

            b.txtOutputTag = MillUI.TxtStretch(ot, "Txt_OutputTag", "", MillDesign.OutTagFont,
                MillSpriteFactory.Hex(MillDesign.CTextBrown), TextAlignmentOptions.Center);
            b.txtOutputTag.rectTransform.offsetMin = new Vector2(16f, 0f);
            b.txtOutputTag.rectTransform.offsetMax = new Vector2(-16f, 0f);

            // ── HIỆU ỨNG. Chỉ GẮN COMPONENT + wire, KHÔNG tạo node mới. ────────────

            // Hạt nguyên liệu bay từ bong bóng vào phễu máy + máy nhún một nhịp.
            var fxIn = MillUI.Comp<MillIntakeFX>(box.gameObject);
            var soIn = new SerializedObject(fxIn);
            const string ownIn = "MillIntakeFX";
            MillWiring.W(soIn, "diemXuatPhat", bub, rep, ownIn);
            MillWiring.W(soIn, "diemDich",     mac, rep, ownIn);
            MillWiring.W(soIn, "noiChuaHat",   box, rep, ownIn);
            MillWiring.W(soIn, "thanMay",      mac, rep, ownIn);
            soIn.ApplyModifiedPropertiesWithoutUndo();
            b.fxNguyenLieu = fxIn;

            // Đĩa thành phẩm nảy ra + quầng sáng thở lúc chờ thu. Component nằm TRÊN
            // `Output_Bubble` và `bao` trỏ về chính nó.
            var fxBag = MillUI.Comp<MillOutputBagFX>(ob.gameObject);
            var soBag = new SerializedObject(fxBag);
            const string ownBag = "MillOutputBagFX";
            MillWiring.W(soBag, "bao",     ob,         rep, ownBag);
            MillWiring.W(soBag, "imgGlow", imgBagGlow, rep, ownBag);
            soBag.ApplyModifiedPropertiesWithoutUndo();
            b.fxBaoRa = fxBag;

            // ── PHÁO BÔNG khi xay xong ─────────────────────────────────────────────
            //  Gắn TRÊN `AnimationBox`: script tự sinh mảnh giấy làm CON của `noiChua`, nên
            //  giấy bay trong LÒNG khung gỗ, không tràn ra cả popup.
            var fxCel = MillUI.Comp<MillCelebrationFX>(box.gameObject);
            var soCel = new SerializedObject(fxCel);
            const string ownCel = "MillCelebrationFX";
            MillWiring.W(soCel, "noiChua", box, rep, ownCel);
            // anhGiay là MẢNG Sprite[]: confetti_large (ô đầu của sheet) + Square01 + Plus01.
            MillWiring.WArr(soCel, "anhGiay", sk.fxGiay, rep, ownCel);
            MillWiring.W(soCel, "anhSao", sk.fxSao, rep, ownCel);
            MillWiring.W(soCel, "anhLoe", sk.fxLoe, rep, ownCel);
            soCel.ApplyModifiedPropertiesWithoutUndo();
            b.fxPhaoHoa = fxCel;

            // ── KHÓI phun ra khi máy đang xay ──────────────────────────────────────
            //  `mieng` = node `Machine` (phễu): khói phải phun ra từ máy, không từ giữa khung.
            //  ⚠ Máy nay ở lòng khung (không còn ở `AnimationBox` cũ 629×250) nhưng TÊN NODE
            //    và quan hệ cha-con KHÔNG đổi ⇒ `mieng` vẫn trỏ đúng `Machine`, và
            //    `MillSmokeFX.caoBay` 74 vẫn khớp khoảng trời còn lại (MillDesign.TroiTrenPheu).
            var fxSmoke = MillUI.Comp<MillSmokeFX>(box.gameObject);
            var soSmoke = new SerializedObject(fxSmoke);
            const string ownSmoke = "MillSmokeFX";
            MillWiring.W(soSmoke, "noiChua", box, rep, ownSmoke);
            MillWiring.W(soSmoke, "mieng", mac, rep, ownSmoke);
            MillWiring.W(soSmoke, "anhKhoi", sk.fxKhoi, rep, ownSmoke);
            MillWiring.W(soSmoke, "anhBongBong", sk.fxBongBong, rep, ownSmoke);
            soSmoke.ApplyModifiedPropertiesWithoutUndo();
            b.fxKhoi = fxSmoke;

            if (MillDesign.TroiTrenPheu < 74f)
                rep.Loi("Trời còn lại trên miệng phễu chỉ " + MillDesign.TroiTrenPheu +
                        "px < 74px của MillSmokeFX.caoBay ⇒ khói sẽ bay vượt mép khung gỗ " +
                        "(khung KHÔNG có mask). Hạ MachineSize hoặc MachineBottom.");
        }

        /// <summary>
        /// Một bánh răng. Toạ độ + kích thước đo TRỰC TIẾP trên ảnh duyệt (mock.py putfit),
        /// không còn quy đổi qua viewBox 200 của SVG như bản trước — art gear_*.png được vẽ
        /// nguyên khối ở đúng cỡ node nên Type = Simple là khớp.
        /// `RotatingGear` là file CÓ SẴN của dự án; popup tự gọi Configure() lúc Open nên chỉ
        /// cần tắt playOnStart.
        /// </summary>
        private static RotatingGear DungBanhRang(RectTransform mac, string ten, Sprite sp,
            float size, float left, float top, MillReport rep)
        {
            RectTransform rt = MillUI.TL(MillUI.Node(mac, ten), size, size, left, top);

            // ⚠ BẮT BUỘC — nếu không, BÁNH RĂNG XOAY RA NGOÀI RÌA MÁY.
            // `TL` đặt pivot vào GÓC TRÊN-TRÁI. `RotatingGear.Update` gọi
            // `transform.Rotate`, mà phép quay lấy PIVOT làm tâm ⇒ bánh răng không xoay tại
            // chỗ mà ĐI VÒNG quanh cái góc đó, quét đường tròn bán kính tới size×√2
            // (bánh 116px ⇒ ~164px) — lao hẳn ra khỏi thân máy. Không có lỗi đỏ nào cả,
            // mọi thứ đều hợp lệ, chỉ là quay quanh sai điểm.
            //
            // DoiPivotVeGiua đưa pivot về giữa và bù anchoredPosition nên hình KHÔNG xê dịch.
            // `RotatingGear.Awake` cũng tự làm bước này (lưới an toàn cho scene cũ), nhưng
            // dựng sẵn ở đây thì Editor xem prefab là đã thấy đúng.
            MillRectUtil.DoiPivotVeGiua(rt);

            MillUI.Img(rt, sp, Color.white);
            var g = MillUI.Comp<RotatingGear>(rt.gameObject);
            MillWiring.DatBool(g, "playOnStart", false, rep);
            return g;
        }

        /// <summary>
        /// Khu slot xay — tiêu đề + 5 card slot phủ ĐÚNG 974 của cột phải.
        ///
        /// ⚠ Mốc dọc lấy từ ĐÁY KHUNG GỖ (AnimH 418 + 22 = 440), và bề rộng lấy `RightW` 974.
        ///   mock.py ở chỗ này vô tình dùng lại hai biến đã bị gán lại thành kích thước LÒNG
        ///   khung (378 / 934) trong khi vẫn đặt slot đầu ở mép TRÁI khung ngoài ⇒ hàng slot
        ///   trong ảnh duyệt hụt 40px so với khung animation ngay phía trên. Bản này sửa lại
        ///   cho hai khối thẳng lề — xem khối ghi chú đầu `MillDesign`.
        /// </summary>
        private static void DungKhuSlot(RectTransform col, MillSkin sk, MillBuilt b,
                                        MillReport rep)
        {
            float hdrTop = MillDesign.SlotsHeaderTop + MillDesign.SlotsHeaderCy
                         - MillDesign.SlotsHeaderH * 0.5f;                      // 448

            MillUI.Txt(MillUI.TL(MillUI.Node(col, "Txt_SlotsHeader"), 240f,
                                 MillDesign.SlotsHeaderH, 0f, hdrTop),
                       "SLOT XAY", MillDesign.SlotsHeaderFont,
                       MillSpriteFactory.Hex(MillDesign.CTextBrown), TextAlignmentOptions.Left);

            b.txtSlotSummary = MillUI.Txt(MillUI.TL(MillUI.Node(col, "Txt_SlotSummary"),
                    MillDesign.RightW - MillDesign.SlotsSummaryLeft, MillDesign.SlotsHeaderH,
                    MillDesign.SlotsSummaryLeft, hdrTop),
                "", MillDesign.SlotsSummaryFont,
                MillSpriteFactory.Hex(MillDesign.CTextLight), TextAlignmentOptions.Left);

            RectTransform cont = MillUI.TL(MillUI.Node(col, "SlotsContainer"),
                MillDesign.RightW, MillDesign.SlotsH, 0f, MillDesign.SlotsTop);

            float w = MillDesign.SlotW;
            b.slots = new MillSlotUI[MillDesign.SlotCount];
            for (int i = 0; i < MillDesign.SlotCount; i++)
                b.slots[i] = MillSlotBuilder.Dung(cont, i, w, sk, rep);
        }

        // ═════════════════════ RUY BĂNG / NÚT X / TOAST ═════════════════════

        /// <summary>
        /// Ruy băng tiêu đề — art `shop_banner_ribbon` 720×161, nhô 92px LÊN TRÊN mép Window.
        /// ⚠ KHÔNG dùng `ribbon_header`: nó là khối chữ nhật vàng PHẲNG tỉ lệ 6.08, wire đúng
        ///   vẫn đọc ra "tấm nền" — đúng thứ chủ dự án phàn nàn. Lý do đầy đủ ở MillDesign.
        /// Tạo SAU InnerPanel để vẽ đè lên.
        /// </summary>
        private static void DungRuyBang(RectTransform win, MillSkin sk, MillBuilt b)
        {
            RectTransform rb = MillUI.TC(MillUI.Node(win, "Ribbon"),
                MillDesign.RibbonW, MillDesign.RibbonH, MillDesign.RibbonTop);
            MillUI.Img(rb, sk.bannerRibbon, Color.white);

            // Ô chữ chỉ cao 88% ruy băng ⇒ tâm chữ ở 44% chiều cao (art có bóng/đuôi ở đáy).
            var t = MillUI.Txt(MillUI.TL(MillUI.Node(rb, "Txt_Title"),
                    MillDesign.RibbonW, MillDesign.RibbonH * MillDesign.RibbonTiLeChu, 0f, 0f),
                "MÁY XAY THỨC ĂN", MillDesign.RibbonFont, Color.white,
                TextAlignmentOptions.Center);
            MillUI.Vien(t, 0.16f, MillSpriteFactory.Hex(MillDesign.CVienTieuDe));
            b.txtTitle = t;
        }

        /// <summary>
        /// Nút X — art `btn_close.png` 104×104, TÂM trùng GÓC PHẢI-TRÊN của Window
        /// (tràn 46.8px ra ngoài mép phải, 43.68px lên trên mép trên).
        /// </summary>
        private static void DungNutDong(RectTransform win, MillSkin sk, MillBuilt b)
        {
            RectTransform rt = MillUI.TR(MillUI.Node(win, "Btn_Close"),
                MillDesign.CloseSize, MillDesign.CloseSize,
                MillDesign.CloseOffRight, MillDesign.CloseOffTop);
            Image img = MillUI.Img(rt, sk.btnClose, Color.white, true);
            b.btnClose = MillUI.Btn(img);

            //  ⚠ CHỈ vẽ dấu ✖ khi nút đóng là bản VẼ TAY. Art btn_close.png của chủ dự án ĐÃ
            //    CÓ dấu ✖ trắng ngay trong ảnh ⇒ thêm node này nữa là HAI dấu X chồng lệch
            //    nhau, đúng lỗi "nhìn thô" mà chủ dự án phàn nàn.
            if (!sk.closeCoDauX)
            {
                Image g = MillUI.Img(MillUI.CC(MillUI.Node(rt, "Glyph_X"), 44f, 44f, 0f, 0f),
                                     sk.closeGlyph, Color.white);
                g.raycastTarget = false;
            }
        }

        /// <summary>
        /// Toast — art `shop_toast` 720×88 căn giữa đáy Window (DÙNG CHUNG spriteBorder với
        /// bảng gợi ý, xem MillSkin). Tắt sẵn; MillPopupUI tự fade qua CanvasGroup.
        /// </summary>
        private static void DungToast(RectTransform win, MillSkin sk, MillBuilt b)
        {
            RectTransform rt = MillUI.BC(MillUI.Node(win, "Toast"),
                MillDesign.ToastW, MillDesign.ToastH, MillDesign.ToastBottom);
            MillUI.Img(rt, sk.toastBg, Color.white);
            MillUI.Comp<CanvasGroup>(rt.gameObject);

            b.toastText = MillUI.TxtStretch(rt, "Txt_Toast", "", MillDesign.ToastFont,
                Color.white, TextAlignmentOptions.Center, true);
            b.toastText.rectTransform.offsetMin = new Vector2(28f, 10f);
            b.toastText.rectTransform.offsetMax = new Vector2(-28f, -10f);
            MillUI.Vien(b.toastText, 0.12f, MillSpriteFactory.Hex(MillDesign.CVienXanhLa));

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
            float H    = MillDesign.SlotsH;        // 192
            float IN   = MillDesign.SlotInset;     // 9
            float noiW = MillDesign.SlotNoiW;      // 162.4

            RectTransform card = MillUI.TL(MillUI.Node(parent, "Slot_" + (idx + 1)), w, H,
                idx * (w + MillDesign.SlotGap), 0f);

            // ═══ LỚP 1 — KHUNG GỖ (shop_card_outer, 9-slice 30) ═══════════════════
            // raycastTarget = TRUE (khác mặc định của MillUI.Img): EventSystem chỉ gửi
            // OnDrop tới node có Graphic ĂN RAYCAST nằm dưới con trỏ. Để false thì thả bao
            // vào vùng trống của slot rơi vào hư không mà KHÔNG có lỗi nào để lần ra.
            // MillSlotUI.Awake cũng tự bật lại (đai + dây), nhưng prefab trên đĩa phải đúng.
            Image bg = MillUI.Img(card, sk.slotFrame, Color.white, true);

            // ═══ LỚP 2 — NỀN KEM ĐẶC (shop_card_inner, 9-slice 28), thụt 9 ════════
            //  ⚠⚠ ĐỪNG BỎ LỚP NÀY. `slot_empty` (lớp 3) là một VIỀN NÉT ĐỨT TRONG SUỐT, không
            //     phải một cái thẻ. Đặt nó trực tiếp lên khung gỗ thì gỗ lộ qua giữa ô và chữ
            //     "Trống" chìm mất — LỖI ĐÃ SHIP MỘT LẦN. Lớp kem ĐẶC phải nằm giữa.
            //  Là con ĐẦU TIÊN của card ⇒ vẽ dưới cả 5 root.
            MillUI.Img(MillUI.Stretch(MillUI.Node(card, "Card_Fill"), IN, IN, IN, IN),
                       sk.slotFill, Color.white);

            var slot = MillUI.Comp<MillSlotUI>(card.gameObject);
            var so = new SerializedObject(slot);
            string own = "MillSlotUI[" + (idx + 1) + "]";

            // ── Toạ độ dùng chung (soát tràn đầy đủ ở MillDesign, khối "SOÁT TRÀN CARD SLOT")
            float progW = noiW * MillDesign.SlotProgTiLe;      // 146.16
            float btnW  = noiW * MillDesign.SlotBtnTiLe;       // 146.16

            // ═══ ROOT 1: ĐANG XAY ═══════════════════════════════════════════════
            //  4 hàng trong 174px lòng card: đĩa 10..84 | tên 88..112 | thanh 114..134 |
            //  nút 140..182. Bản trước tên 87..117 và thanh 111..125 CHỒNG NHAU 6px — nay hết.
            RectTransform rRun = MillUI.Stretch(MillUI.Node(card, "Root_Running"), 0, 0, 0, 0);
            DiaKem(rRun, sk, MillDesign.SlotIconPlate, MillDesign.SlotIconPlateTop);

            RectTransform tr = MillUI.BC(MillUI.Node(rRun, "Progress_Track"),
                                          progW, MillDesign.SlotProgH, MillDesign.SlotProgBottom);
            MillUI.Img(tr, sk.progTrack, Color.white);
            MillUI.Comp<RectMask2D>(tr.gameObject);   // giữ đầu thanh fill trong rãnh bo góc

            RectTransform fi = MillUI.Stretch(MillUI.Node(tr, "Progress_Fill"), 0, 0, 0, 0);
            Image fill = MillUI.Img(fi, sk.progFill, Color.white);
            // ⚠ BẮT BUỘC: Filled / Horizontal / Left. Để Simple thì `fillAmount` không có
            //   tác dụng và thanh ĐỨNG YÊN mà Unity KHÔNG báo lỗi gì — bug rất khó thấy.
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;

            var txtTimer = MillUI.TxtStretch(tr, "Txt_Timer", "", MillDesign.SlotTimerFont,
                Color.white, TextAlignmentOptions.Center);

            // Nút tăng tốc — art shop_btn_buy_gem (Simple).
            RectTransform bs = MillUI.BC(MillUI.Node(rRun, "Btn_SpeedUp"),
                                          btnW, MillDesign.SlotBtnH, MillDesign.SlotBtnBottom);
            Image bsImg = MillUI.Img(bs, sk.btnBuyGem, Color.white, true);
            Button btnSpeed = MillUI.Btn(bsImg);
            MillUI.HangNgang(bs, MillDesign.SlotGemGap, 12f, 12f, 0f, 4f, false);
            RectTransform bsIc = MillUI.Node(bs, "Icon_Gem");
            MillUI.Img(bsIc, sk.gemIcon, Color.white);
            MillUI.CoDinh(bsIc, MillDesign.SlotGemIcon, MillDesign.SlotGemIcon);
            RectTransform bsTx = MillUI.Node(bs, "Txt_SpeedUpCost");
            var txtSpeed = MillUI.Txt(bsTx, "x0", MillDesign.SlotBtnFont, Color.white,
                                      TextAlignmentOptions.Center);
            MillUI.CoDinhCao(bsTx, MillDesign.SlotBtnFont + 6f);
            MillUI.Vien(txtSpeed, 0.12f, MillSpriteFactory.Hex(MillDesign.CVienXanhDuong));

            // ═══ ROOT 2: CHỜ THU ════════════════════════════════════════════════
            RectTransform rRdy = MillUI.Stretch(MillUI.Node(card, "Root_Ready"), 0, 0, 0, 0);
            // NỀN NHẤN "có hàng chờ thu" — art slot_selected.png (viền vàng kim).
            //  ⚠ PHẢI là con ĐẦU TIÊN của root: cùng cha nên thứ tự sibling = thứ tự vẽ, tạo
            //    trước ⇒ nằm DƯỚI đĩa kem và nút THU. `MillSlotUI.SetMode` bật/tắt root nên
            //    nền này tự hiện/ẩn theo trạng thái, KHÔNG cần thêm field vào contract.
            MillUI.Img(MillUI.Stretch(MillUI.Node(rRdy, "Bg_Ready"), IN, IN, IN, IN),
                       sk.slotReady, Color.white);
            DiaKem(rRdy, sk, MillDesign.SlotIconPlate, MillDesign.SlotIconPlateTop);

            // Nút THU — art shop_btn_buy_gold (Simple).
            RectTransform bc = MillUI.BC(MillUI.Node(rRdy, "Btn_Collect"),
                                          btnW, MillDesign.SlotBtnH, MillDesign.SlotBtnBottom);
            Image bcImg = MillUI.Img(bc, sk.btnBuyGold, Color.white, true);
            Button btnCollect = MillUI.Btn(bcImg);
            var txtThu = MillUI.Txt(MillUI.CC(MillUI.Node(bc, "Txt_Collect"),
                                               btnW, MillDesign.SlotBtnH, 0f, 2f),
                       "THU", MillDesign.SlotBtnFont, Color.white, TextAlignmentOptions.Center);
            MillUI.Vien(txtThu, 0.12f, MillSpriteFactory.Hex(MillDesign.CVienXanhLa));

            // ═══ ROOT 3: TRỐNG ══════════════════════════════════════════════════
            //  Hai hàng thôi nên đĩa được TO NHẤT (108, so với 74 của hai trạng thái có nút).
            RectTransform rEmp = MillUI.Stretch(MillUI.Node(card, "Root_Empty"), 0, 0, 0, 0);
            // ═══ LỚP 3 — VIỀN NÉT ĐỨT (slot_empty), CHỈ ở trạng thái Trống ═══════
            //  Nằm TRÊN lớp kem đặc `Card_Fill` nên nét đứt hiện rõ mà lòng ô vẫn kem, không
            //  lộ gỗ. Con ĐẦU TIÊN của root, lý do như `Bg_Ready`.
            MillUI.Img(MillUI.Stretch(MillUI.Node(rEmp, "Bg_Empty"), IN, IN, IN, IN),
                       sk.slotDashed, Color.white);
            DiaKem(rEmp, sk, MillDesign.SlotEmptyPlate, MillDesign.SlotEmptyPlateTop);
            MillUI.Txt(MillUI.TL(MillUI.Node(rEmp, "Txt_Empty"), noiW - 16f, MillDesign.SlotEmptyH,
                                 IN + 8f, MillDesign.SlotEmptyCy - MillDesign.SlotEmptyH * 0.5f),
                       "Trống", MillDesign.SlotEmptyFont,
                       MillSpriteFactory.Hex(MillDesign.CSlotTrong), TextAlignmentOptions.Center);

            // ═══ ROOT 4: CHƯA MỞ — MUA BẰNG KIM CƯƠNG ═══════════════════════════
            //  ⚠ ĐÃ BỎ HẲN nhãn "Mở slot" (node `Txt_UnlockHint` của bản trước): ở chiều cao
            //    192px nó đè lên nút kim cương, mà ổ khoá + giá đã đủ nghĩa. Đây là quyết định
            //    của bản duyệt, không phải quên. Node đó KHÔNG có field contract nào trỏ tới
            //    nên bỏ là an toàn.
            RectTransform rGem = MillUI.Stretch(MillUI.Node(card, "Root_UnlockGem"), 0, 0, 0, 0);
            // Nền kem ĐẶC nhân màu SlotMauKhoa ⇒ slot khoá LÙI VỀ SAU slot đang mở.
            MillUI.Img(MillUI.Stretch(MillUI.Node(rGem, "Bg_Locked"), IN, IN, IN, IN),
                       sk.slotFill, MillDesign.SlotMauKhoa);

            RectTransform bu = MillUI.BC(MillUI.Node(rGem, "Btn_UnlockGem"),
                MillDesign.SlotGemBtnW, MillDesign.SlotGemBtnH, MillDesign.SlotGemBtnBottom);
            Image buImg = MillUI.Img(bu, sk.btnBuyGem, Color.white, true);
            Button btnUnlock = MillUI.Btn(buImg);
            MillUI.HangNgang(bu, MillDesign.SlotGemGap, 12f, 12f, 0f, 4f, false);
            RectTransform buIc = MillUI.Node(bu, "Icon_Gem");
            MillUI.Img(buIc, sk.gemIcon, Color.white);
            MillUI.CoDinh(buIc, MillDesign.SlotGemIcon, MillDesign.SlotGemIcon);
            RectTransform buTx = MillUI.Node(bu, "Txt_GemCost");
            var txtGemCost = MillUI.Txt(buTx, "0", MillDesign.SlotGemFont, Color.white,
                                        TextAlignmentOptions.Center);
            MillUI.CoDinhCao(buTx, MillDesign.SlotGemFont + 6f);
            MillUI.Vien(txtGemCost, 0.12f, MillSpriteFactory.Hex(MillDesign.CVienXanhDuong));

            // ═══ ROOT 5: CHƯA ĐỦ CẤP ════════════════════════════════════════════
            //  Ổ khoá dùng chung nằm ở 44..128, nên hai dòng chữ phải gói trong MỘT viên thuốc
            //  ở đáy (128..180) — không còn chỗ cho một dòng rời phía trên. `txtLockLabel` và
            //  `txtLockLevelValue` VẪN WIRE ĐỦ (MillSlotUI.BindLockedLevel dùng cả hai; để
            //  trống ô thứ hai thì nó dồn 2 dòng vào ô đầu, ta không cần nhánh đó).
            RectTransform rLvl = MillUI.Stretch(MillUI.Node(card, "Root_LockedLevel"), 0, 0, 0, 0);
            MillUI.Img(MillUI.Stretch(MillUI.Node(rLvl, "Bg_Locked"), IN, IN, IN, IN),
                       sk.slotFill, MillDesign.SlotMauKhoa);

            RectTransform pill = MillUI.BC(MillUI.Node(rLvl, "Locked_Pill"),
                MillDesign.SlotPillW, MillDesign.SlotPillH, MillDesign.SlotPillBottom);
            MillUI.Img(pill, sk.pillBg, Color.white);

            var txtLockLabel = MillUI.Txt(MillUI.TL(MillUI.Node(pill, "Txt_LockLabel"),
                    MillDesign.SlotPillW, 22f, 0f, 2f),
                "Chưa đủ cấp", MillDesign.SlotLockTextFont, Color.white,
                TextAlignmentOptions.Center);
            var txtLvl = MillUI.Txt(MillUI.TL(MillUI.Node(pill, "Txt_LockLevelValue"),
                    MillDesign.SlotPillW, 26f, 0f, 24f),
                "Cấp 18", MillDesign.SlotLockLevelFont, Color.white,
                TextAlignmentOptions.Center);

            // ═══ NODE DÙNG CHUNG (vẽ SAU 5 root) ════════════════════════════════
            //
            //  ⚠ VÌ SAO icon/tên/ổ khoá/chấm đỏ NẰM NGOÀI 5 root trạng thái:
            //  `MillSlotUI.SetMode` bật ĐÚNG MỘT root và tắt 4 root còn lại, nhưng nó điều
            //  khiển `imgLockIcon` bằng `.enabled` và `redDot` bằng SetActive RIÊNG. Nhét ổ
            //  khoá vào `rootLockedLevel` thì ở trạng thái `UnlockGem` root đó tắt ⇒ ổ khoá vô
            //  hình dù enabled = true. Còn `imgIcon`/`txtName` do `DatCongThuc` xử lý (null ⇒
            //  enabled = false / chuỗi rỗng) nên cũng phải sống ngoài root.
            //  Nhờ `DatCongThuc(null)` ở BindEmpty/BindUnlockGem/BindLockedLevel, `Txt_Name`
            //  (88..112) và `Img_Icon` đều RỖNG ở ba trạng thái đó ⇒ không đè lên "Trống" hay
            //  lên ổ khoá.
            //
            //  ⚠ ĐĨA KEM thì NGƯỢC LẠI: nó phải TẮT ở hai trạng thái khoá, mà contract KHÔNG
            //  có field nào cho nó ⇒ đặt một bản copy trong mỗi root Running/Ready/Empty (và
            //  đĩa của Empty CỐ Ý to hơn — xem MillDesign).

            RectTransform ii = MillUI.TC(MillUI.Node(card, "Img_Icon"),
                MillDesign.SlotIconImg, MillDesign.SlotIconImg,
                MillDesign.SlotIconPlateTop
                + (MillDesign.SlotIconPlate - MillDesign.SlotIconImg) * 0.5f);
            Image imgIcon = MillUI.Img(ii, null, Color.white);
            imgIcon.enabled = false;

            // Đĩa ổ khoá — art shop_lock_badge (KHÔNG có hình khoá bên trong) + ổ khoá TRẮNG
            // vẽ đè lên, đúng như bản duyệt.
            RectTransform li = MillUI.TC(MillUI.Node(card, "Img_LockIcon"),
                MillDesign.SlotLockBadge, MillDesign.SlotLockBadge, MillDesign.SlotLockTop);
            Image imgLock = MillUI.Img(li, sk.lockBadge, Color.white);
            imgLock.enabled = false;              // SetMode bật khi ở hai trạng thái khoá
            Image lockGlyph = MillUI.Img(MillUI.CC(MillUI.Node(li, "Glyph_Lock"),
                    MillDesign.SlotLockGlyph, MillDesign.SlotLockGlyph, 0f, 0f),
                sk.lockGlyph, Color.white);
            lockGlyph.raycastTarget = false;

            var txtIndex = MillUI.Txt(MillUI.TL(MillUI.Node(card, "Txt_Index"), 44f,
                                                 MillDesign.SlotNumH,
                                                 IN + MillDesign.SlotNumLeft,
                                                 IN + MillDesign.SlotNumCy
                                                 - MillDesign.SlotNumH * 0.5f),
                "#" + (idx + 1), MillDesign.SlotNumFont,
                MillSpriteFactory.Hex(MillDesign.CSlotNum), TextAlignmentOptions.Left);

            var txtName = MillUI.Txt(MillUI.TL(MillUI.Node(card, "Txt_Name"), noiW - 16f,
                                                MillDesign.SlotNameH, IN + 8f,
                                                MillDesign.SlotNameTop),
                "", MillDesign.SlotNameFont, MillSpriteFactory.Hex(MillDesign.CTextBrown),
                TextAlignmentOptions.Center);

            RectTransform rd = MillUI.TR(MillUI.Node(card, "RedDot"),
                MillDesign.RedDotSize, MillDesign.RedDotSize,
                MillDesign.RedDotRight, MillDesign.RedDotTop);
            MillUI.Img(rd, sk.redDot, Color.white);
            rd.gameObject.SetActive(false);

            // ── VIỀN SÁNG "THẢ ĐƯỢC VÀO ĐÂY" ────────────────────────────────────
            //  ⚠ TẠO CUỐI CÙNG, sau cả 5 root và mọi node dùng chung: cùng cha nên thứ tự
            //    sibling quyết định thứ tự vẽ ⇒ vòng sáng phải là con CUỐI để không bị nền
            //    khoá (phủ kín card) hay icon che mất.
            //  Stretch kín card với inset ÂM ⇒ vòng phình ra ngoài mép 3px, ôm sát khung gỗ.
            //  MillSlotUI.SetDropHighlight bật/tắt node này và chỉ đổi ALPHA của Image
            //  (0.45 lúc chỉ sẵn sàng nhận, 1.0 lúc con trỏ hover) — kênh vẽ RIÊNG.
            RectTransform dh = MillUI.Stretch(MillUI.Node(card, "Drop_Highlight"),
                MillDesign.DropRingInset, MillDesign.DropRingInset,
                MillDesign.DropRingInset, MillDesign.DropRingInset);
            Image imgDrop = MillUI.Img(dh, sk.dropRing, Color.white);
            // BẮT BUỘC false: vòng sáng phủ kín slot, ăn raycast thì nó chiếm luôn cú thả
            // và OnDrop của MillSlotUI không bao giờ nổ.
            imgDrop.raycastTarget = false;
            dh.gameObject.SetActive(false);

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
            MillWiring.W(so, "dropHighlight", dh.gameObject, rep, own);
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

        /// <summary>
        /// Đĩa kem sau icon — art `shop_circle_plate.png`, một bản copy trong từng root.
        ///
        /// Cỡ và mốc trên TRUYỀN VÀO vì trạng thái TRỐNG được đĩa TO HƠN (108 ở +42) so với
        /// hai trạng thái có nút (74 ở +10): Trống chỉ có 2 hàng nội dung, Đang xay có 4.
        /// Xem khối "SOÁT TRÀN CARD SLOT" trong MillDesign.
        /// </summary>
        private static void DiaKem(RectTransform root, MillSkin sk, float co, float top)
        {
            MillUI.Img(MillUI.TC(MillUI.Node(root, "Icon_Circle"), co, co, top),
                       sk.circlePlate, Color.white);
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

                float noiW = MillDesign.CardTextW;                        // 232

                Image bg = MillUI.Img(rt, sk.cardInactive, Color.white, true);
                var card = MillUI.Comp<MillRecipeCardUI>(temp);

                // NGUỒN KÉO: kéo card thả vào slot để bắt đầu mẻ xay.
                // ⚠ PHẢI thêm SAU MillRecipeCardUI — nó có
                //   [RequireComponent(typeof(MillRecipeCardUI))].
                // `canvasBong` CỐ Ý để TRỐNG: prefab card không chứa Canvas nào nên không có
                // gì để wire; script tự GetComponentInParent<Canvas>() lúc kéo và ra đúng
                // canvas popup (order 400).
                MillUI.Comp<MillRecipeDragSource>(temp);

                MillUI.Comp<CanvasGroup>(temp);
                // GIỮ NGUYÊN `btnSelect`: bấm (không kéo) vẫn chọn công thức để xem trước
                // sản phẩm ở đĩa đầu ra. Unity phân biệt click/drag bằng
                // EventSystem.pixelDragThreshold nên hai cử chỉ không tranh nhau.
                Button btn = MillUI.Btn(bg);

                // Chiều cao cố định cho VerticalLayoutGroup của danh sách.
                MillUI.CoDinh(rt, MillDesign.CardW, MillDesign.CardH);

                // ── Đĩa icon 116px (art shop_circle_plate), canh giữa dọc card ────
                RectTransform ic = MillUI.TL(MillUI.Node(rt, "Icon_Circle"),
                    MillDesign.CardIconPlate, MillDesign.CardIconPlate,
                    MillDesign.CardIconLeft, MillDesign.CardIconTop);
                MillUI.Img(ic, sk.circlePlate, Color.white);
                RectTransform ii = MillUI.CC(MillUI.Node(ic, "Img_Icon"),
                    MillDesign.CardIconImg, MillDesign.CardIconImg, 0f, 0f);
                Image imgIcon = MillUI.Img(ii, null, Color.white);
                imgIcon.enabled = false;

                // ── Hàng 1: tên (tâm y 50) — hàng 2: thời gian ủ (tâm y 84) ───────
                var txtName = MillUI.Txt(MillUI.TL(MillUI.Node(rt, "Txt_Name"),
                        noiW, MillDesign.CardNameH, MillDesign.CardTextLeft,
                        MillDesign.CardNameCy - MillDesign.CardNameH * 0.5f),
                    "Cám cho gà", MillDesign.CardNameFont,
                    MillSpriteFactory.Hex(MillDesign.CTextBrown), TextAlignmentOptions.Left);

                var txtTime = MillUI.Txt(MillUI.TL(MillUI.Node(rt, "Txt_BrewTime"),
                        noiW, MillDesign.CardTimeH, MillDesign.CardTextLeft,
                        MillDesign.CardTimeCy - MillDesign.CardTimeH * 0.5f),
                    "Ủ 2p00", MillDesign.CardTimeFont,
                    MillSpriteFactory.Hex(MillDesign.CTextLight), TextAlignmentOptions.Left);

                // ── Hàng 3: chip nguyên liệu — art shop_btn_buy_gold 94×40, khe 9 ─
                RectTransform row = MillUI.TL(MillUI.Node(rt, "Cost_Row"), noiW,
                    MillDesign.CardChipH, MillDesign.CardTextLeft, MillDesign.CardChipTop);
                var h = MillUI.HangNgang(row, MillDesign.CardChipGap, 0f, 0f, 0f, 0f, false);
                h.childAlignment = TextAnchor.MiddleLeft;

                Image ing1, ing2; TMP_Text tIng1, tIng2;
                Chip(row, "Chip_1", sk, out ing1, out tIng1);
                Chip(row, "Chip_2", sk, out ing2, out tIng2);

                // ── Thẻ con vật — art badge_count, MỌC SANG PHẢI từ x 232 ────────
                //  mock.py: bề rộng = 34 + 13 × số ký tự, tức 17 lề mỗi bên + chữ, KHÔNG có
                //  icon. Vì thế `spacing = 0` và `Img_Badge` CỐ Ý KHÔNG có LayoutElement:
                //  MillRecipeCardUI để `imgBadge.enabled = false` khi công thức không có
                //  `animalBadgeIcon` (mọi công thức tool tạo đều vậy), và LayoutUtility BỎ QUA
                //  component Behaviour đang tắt ⇒ ô icon co về 0, viên thuốc rộng đúng
                //  34 + chữ như bản duyệt.
                //  ⚠ Nếu sau này có người gán `animalBadgeIcon` thật thì THÊM
                //    MillUI.CoDinh(tgi, 26f, 26f) và spacing 6 ở đây — không thêm sẵn vì nó
                //    chiếm 32px chỗ trống vĩnh viễn, làm chữ lệch khỏi tâm viên thuốc.
                RectTransform tag = MillUI.TL(MillUI.Node(rt, "Badge_Animal"), 60f,
                    MillDesign.CardTagH, MillDesign.CardTagLeft, MillDesign.CardTagTop);
                MillUI.Img(tag, sk.pillBg, Color.white);
                MillUI.HangNgang(tag, 0f, MillDesign.CardTagPadX, MillDesign.CardTagPadX,
                                 0f, 0f, true);
                RectTransform tgi = MillUI.Node(tag, "Img_Badge");
                Image imgBadge = MillUI.Img(tgi, null, Color.white);
                imgBadge.enabled = false;
                RectTransform tgt = MillUI.Node(tag, "Txt_Badge");
                var txtBadge = MillUI.Txt(tgt, "Gà", MillDesign.CardTagFont, Color.white,
                    TextAlignmentOptions.Center);
                MillUI.CoDinhCao(tgt, MillDesign.CardTagFont + 6f);
                MillUI.Vien(txtBadge, 0.12f, MillSpriteFactory.Hex(MillDesign.CVienNau));

                // ── Lớp phủ KHOÁ ────────────────────────────────────────────────
                RectTransform ov = MillUI.Stretch(MillUI.Node(rt, "Lock_Overlay"), 0, 0, 0, 0);
                Image glyph = MillUI.Img(MillUI.TL(MillUI.Node(ov, "Big_Lock"),
                        MillDesign.CardLockGlyph, MillDesign.CardLockGlyph,
                        MillDesign.CardLockLeft, MillDesign.CardLockTop),
                    sk.lockGlyph, new Color(0.16f, 0.15f, 0.13f, 0.95f));
                glyph.raycastTarget = false;
                var txtLock = MillUI.Txt(MillUI.TL(MillUI.Node(ov, "Txt_LockText"),
                        MillDesign.CardW - 16f, 28f, 8f,
                        MillDesign.CardLockTop + MillDesign.CardLockGlyph + 8f),
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

        /// <summary>
        /// Một chip nguyên liệu — art `shop_btn_buy_gold.png` vẽ ở 94×40 (mock.py cwid 94).
        /// Bề rộng CỐ ĐỊNH: mock.py vẽ mọi chip cùng cỡ, và chữ chỉ là "x3"/"x12" nên để chip
        /// tự co là hai chip so le nhau.
        /// Icon 34 ở lề trái 7 (tâm x 24), chữ bắt đầu x 46 — khe 5.
        /// </summary>
        private static void Chip(RectTransform row, string ten, MillSkin sk,
                                 out Image img, out TMP_Text txt)
        {
            RectTransform chip = MillUI.Node(row, ten);
            chip.sizeDelta = new Vector2(MillDesign.CardChipW, MillDesign.CardChipH);
            MillUI.Img(chip, sk.btnBuyGold, Color.white);
            var hg = MillUI.HangNgang(chip, 5f, 7f, 7f, 0f, 0f, false);
            hg.childAlignment = TextAnchor.MiddleLeft;
            // CoDinh (không ContentSizeFitter): chip nằm TRONG một LayoutGroup (Cost_Row) đang
            // childControlWidth ⇒ hai thứ cùng đặt sizeDelta sẽ giành nhau.
            MillUI.CoDinh(chip, MillDesign.CardChipW, MillDesign.CardChipH);

            RectTransform ic = MillUI.Node(chip, "Img_Ing");
            img = MillUI.Img(ic, null, Color.white);
            img.enabled = false;
            MillUI.CoDinh(ic, MillDesign.CardChipIcon, MillDesign.CardChipIcon);

            RectTransform tx = MillUI.Node(chip, "Txt_Ing");
            txt = MillUI.Txt(tx, "", MillDesign.CardChipFont, Color.white,
                             TextAlignmentOptions.Left);
            MillUI.CoDinhCao(tx, MillDesign.CardChipFont + 6f);
            MillUI.Vien(txt, 0.12f, MillSpriteFactory.Hex(MillDesign.CVienXanhLa));
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

            // Hiệu ứng (chốt 21/08). MillPopupUI coi cả ba là TUỲ CHỌN — null thì mẻ xay
            // vẫn chạy đúng, chỉ mất hạt bay / bao nảy / icon bay về kho.
            W(so, "fxNguyenLieu", b.fxNguyenLieu, rep, own);
            W(so, "fxBaoRa",      b.fxBaoRa,      rep, own);
            W(so, "fxBayVeKho",   b.fxBayVeKho,   rep, own);
            // Hai FX mới 21/08 — cũng TUỲ CHỌN. Nếu MillPopupUI chưa có field thì `W` báo
            // "SCRIPT KHÔNG CÓ FIELD NÀY" ở mục CHƯA WIRE, không nổ exception.
            W(so, "fxPhaoHoa",    b.fxPhaoHoa,    rep, own);
            W(so, "fxKhoi",       b.fxKhoi,       rep, own);

            // ═══════════════════════════════════════════════════════════════════════
            //  MÀU TÔ NÚT LỚN — QUYẾT ĐỊNH VỀ ĐIỂM 6 (đọc hết trước khi sửa)
            //
            //  VẤN ĐỀ: `MillPopupUI.CapNhatNutLon` (dòng 700) ghi
            //      imgMainButtonBg.color = sanSang ? mauNutBamDuoc : mauNutKhoa;
            //  Image.color là PHÉP NHÂN với pixel của sprite. Bảng gợi ý `Btn_Main` nay dùng
            //  art `shop_toast` (XANH LÁ có vệt sáng trắng) thay cho sprite TRẮNG vẽ tay của
            //  bản trước. Nếu giữ mauNutKhoa = kem #D9CDB9 như cũ thì
            //  kem × xanh lá = XANH Ô-LIU BẨN, đúng thứ cần tránh.
            //
            //  BA PHƯƠNG ÁN VÀ LỰA CHỌN:
            //   (a) Bỏ hẳn việc tô màu, đổi SPRITE theo trạng thái → phải sửa MillPopupUI
            //       (thêm field spriteNutKhoa + đổi dòng 700). Dev A đang giữ file đó.
            //   (b) Giữ sprite TRẮNG vẽ tay cho `Btn_Main` → mất art `shop_toast`, tức mất
            //       đúng cái mảnh mà bản duyệt dùng ở đây. Trái bản duyệt.
            //   (c) ĐÃ CHỌN — giữ art `shop_toast` VÀ vô hiệu hoá tác hại của phép nhân, bằng
            //       cách đặt hai Color field mà TOOL NÀY vốn đã sở hữu:
            //         mauNutBamDuoc = TRẮNG TINH  ⇒ phép nhân đơn vị, art hiện đúng màu gốc.
            //         mauNutKhoa    = xám TRUNG TÍNH #B3ADA0 ⇒ làm TỐI ĐỀU cả ba kênh, giữ
            //                         nguyên vệt sáng và viền đậm; xanh lá tối đi = "đang bị
            //                         chặn", không lệch sang tông khác.
            //  ⇒ KHÔNG CẦN SỬA MillPopupUI.cs. Được cả art của bản duyệt và cả 4 trạng thái.
            //
            //  ⚠ ĐỪNG đổi mauNutKhoa về một màu CÓ TÔNG (kem/nâu/xanh) — nhân tông với tông
            //    là ra màu thứ ba không ai chọn. Chỉ dùng xám trung tính ở đây.
            //  ⚠ Nếu sau này Dev A muốn làm theo (a) cho sạch: bỏ dòng 700 của MillPopupUI,
            //    thêm `[SerializeField] private Sprite spriteNutKhoa;` rồi
            //    `imgMainButtonBg.sprite = sanSang ? spriteNutBinhThuong : spriteNutKhoa;`
            //    — tool đã có sẵn `MillSkin.btnBuyLocked` (shop_btn_buy_locked.png) để wire
            //    vào field đó. Nói trước để tool sửa cùng lượt.
            // ═══════════════════════════════════════════════════════════════════════
            DatMau(so, "mauNutBamDuoc", MillSpriteFactory.Hex(MillDesign.CNutBamDuoc));
            DatMau(so, "mauNutKhoa",    MillSpriteFactory.Hex(MillDesign.CNutKhoa));
            DatMau(so, "mauDotDangXay", MillSpriteFactory.Hex(MillDesign.CDotDangXay));
            DatMau(so, "mauDotRanh",    MillSpriteFactory.Hex(MillDesign.CDotRanh));

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
            // ── Khung popup + khung khu animation ────────────────────────────────
            { "popup_board",         "BẮT BUỘC (ui_mill_assets) 2048×1365, 9-slice 150" },
            { "panel_inner",         "BẮT BUỘC (ui_mill_assets) 2048×1251, 9-slice 120" },
            { "shop_banner_ribbon",  "BẮT BUỘC (ui_shop_svg) 480×120 — RUY BĂNG tiêu đề " +
                                     "720×161 VÀ header cột 340×76. Type = Simple (hai đầu là " +
                                     "đuôi cờ). THAY CHO ribbon_header, xem MillDesign.RibbonW" },
            { "panel_outer",         "BẮT BUỘC (ui_svg_perfect) 420×280, 9-slice 34 — KHUNG GỖ " +
                                     "bao khu animation, dày 20px. Bản trước chỉ có một vạch " +
                                     "viền 3px vẽ tay ở đây" },

            // ── Card công thức ──────────────────────────────────────────────────
            { "recipe_card_active",  "BẮT BUỘC (ui_mill_assets) 720×240, 9-slice 44" },
            { "recipe_card_inactive","BẮT BUỘC (ui_mill_assets) 720×240, 9-slice 44" },
            { "recipe_card_locked",  "BẮT BUỘC (ui_mill_assets) 720×240, 9-slice 44" },

            // ── Máy xay ─────────────────────────────────────────────────────────
            { "machine_body",        "BẮT BUỘC (ui_mill_assets) — thân máy 280px" },
            { "gear_large",          "BẮT BUỘC (ui_mill_assets) — bánh răng lớn 140px" },
            { "gear_small",          "BẮT BUỘC (ui_mill_assets) — bánh răng nhỏ 96px" },
            { "conveyor_base",       "băng tải 346×72 (4 con lăn bake sẵn, Type = Simple ⇒ " +
                                     "KHÔNG 9-slice; lớp sọc cuộn dừng trên con lăn). " +
                                     "Thiếu ⇒ VẼ mâm phẳng KHÔNG con lăn" },

            // ── Asset DÙNG CHUNG (một asset → MỘT spriteBorder) ─────────────────
            { "shop_currency_chip",  "chip kim cương 224×68 + badge trạng thái 268×62 + nhãn " +
                                     "sản phẩm 252×54 — DÙNG CHUNG 9-slice 26. " +
                                     "Thiếu ⇒ VẼ #F4E2C7 viền 2 #C4A882 r20" },
            { "shop_toast",          "bảng gợi ý `Btn_Main` 420×80 + toast 720×88 — DÙNG CHUNG " +
                                     "9-slice (34,22,34,22). XANH LÁ ⇒ chữ trắng. THAY CHO " +
                                     "btn_green (khối phẳng). Thiếu ⇒ VẼ #3F2C21 a.94 r14" },
            { "inner_panel",         "bảng danh sách công thức 420×498 + bong bóng nguyên liệu " +
                                     "148×84 — DÙNG CHUNG 9-slice 26. Nền KEM ⇒ chữ bong bóng " +
                                     "phải NÂU. Thiếu ⇒ VẼ #FFF6E5 viền 3 #D6B798 r15" },
            { "badge_count",         "thẻ con vật trên card (cao 36) + viên 'Chưa đủ cấp / Cấp " +
                                     "18' 152×52 — DÙNG CHUNG 9-slice 13. " +
                                     "Thiếu ⇒ VẼ #AFA28F r15" },
            { "shop_circle_plate",   "đĩa icon card 116 + đĩa icon slot (108 khi Trống, 74 khi " +
                                     "đang xay) — Type = Simple. Thiếu ⇒ VẼ đĩa #F6E7D1" },
            { "shop_btn_buy_gold",   "chip nguyên liệu trên card 94×40 + nút THU 146×42 — " +
                                     "Type = Simple. Thiếu ⇒ dùng btn_green, thiếu nữa ⇒ VẼ" },

            // ── Chỉ một chỗ dùng ────────────────────────────────────────────────
            { "circle_preview",      "ĐĨA THÀNH PHẨM 240px (to hơn bản trước 40%) — Simple. " +
                                     "Thiếu ⇒ VẼ đĩa #F8E6CA viền 4 #DFB980" },
            { "btn_close",           "nút X 104×104 (ĐÃ CÓ dấu ✖ trong ảnh ⇒ tool bỏ node " +
                                     "Glyph_X). Simple. Thiếu ⇒ VẼ #D45B4B viền 3 trắng + X" },
            { "shop_card_outer",     "LỚP 1 card slot — khung gỗ 180.4×192, 9-slice 30. " +
                                     "Thiếu ⇒ VẼ kem viền 4 #8B5933 r18" },
            { "shop_card_inner",     "LỚP 2 card slot — nền kem ĐẶC 162.4×174, 9-slice 28. " +
                                     "⚠ BẮT BUỘC PHẢI CÓ MỘT LỚP ĐẶC Ở ĐÂY: slot_empty là viền " +
                                     "nét đứt TRONG SUỐT, thiếu lớp này là gỗ lộ qua và chữ " +
                                     "'Trống' chìm mất (lỗi đã ship một lần). " +
                                     "Thiếu ⇒ VẼ khối kem #FFF6E5 r14" },
            { "slot_empty",          "LỚP 3 card slot — viền NÉT ĐỨT trong suốt, 9-slice 26, " +
                                     "CHỈ ở trạng thái Trống, nằm TRÊN lớp 2. " +
                                     "Thiếu ⇒ VẼ vòng viền #E4D5C2 r14" },
            { "slot_selected",       "nền nhấn trạng thái CHỜ THU (viền vàng #FFCF3D), " +
                                     "9-slice 26. Thiếu ⇒ VẼ vòng viền #FFD24A r14" },
            { "shop_lock_badge",     "đĩa ổ khoá slot 84px (KHÔNG có hình khoá bên trong ⇒ vẫn " +
                                     "giữ node Glyph_Lock vẽ ổ khoá TRẮNG đè lên). Simple. " +
                                     "Thiếu ⇒ VẼ đĩa #645747" },
            { "shop_btn_buy_gem",    "nút tăng tốc 146×42 + nút mở slot 144.4×46 — Simple. " +
                                     "Thiếu ⇒ dùng btn_blue, thiếu nữa ⇒ VẼ" },
            { "shop_btn_buy_locked", "sprite nút chết — hiện CHƯA node nào dùng. Giữ sẵn cho " +
                                     "phương án (a) của điểm 6 (đổi sprite thay vì tô màu), " +
                                     "xem ghi chú ở MillWiring.WirePopup" },
            { "progress_track",      "rãnh tiến độ 146×20, 9-slice CHỈ NGANG (8,0,8,0) vì node " +
                                     "cao 20 < ảnh 24. Thiếu ⇒ VẼ #D9D9D9 r10" },
            { "progress_fill",       "thanh tiến độ, border 0 vì Image.Type = Filled. " +
                                     "Thiếu ⇒ VẼ #82C94F r10" },
            { "btn_green",           "DỰ PHÒNG cho shop_btn_buy_gold (9-slice 24,36,24,24)" },
            { "btn_blue",            "DỰ PHÒNG cho shop_btn_buy_gem  (9-slice 24,36,24,24)" },
            { "kimcuong-removebg-preview",
                                     "icon kim cương 46px (chip) / 34px (nút slot) — art thật ở " +
                                     "Assets/Assetsgame/, `Tim` bắt được bằng nhánh tìm rộng cả " +
                                     "project. Thiếu ⇒ VẼ hình thoi #40A4E5" },

            // ── CỐ Ý KHÔNG DÙNG ────────────────────────────────────────────────
            { "ribbon_header",       "CỐ Ý KHÔNG DÙNG — 1440×270, thân thật tỉ lệ 6.08, bản " +
                                     "chất là KHỐI CHỮ NHẬT VÀNG PHẲNG với hai đuôi tí xíu; " +
                                     "wire đúng vẫn đọc ra 'tấm nền'. Đã thay bằng " +
                                     "shop_banner_ribbon (tỉ lệ 4.47, có đuôi cá thật)" },
            { "slot_normal",         "CỐ Ý KHÔNG DÙNG — card slot nay là BA LỚP " +
                                     "shop_card_outer + shop_card_inner + slot_empty" },
            { "tab_active",          "CỐ Ý KHÔNG DÙNG — dự án đã bỏ hệ tab" },
            { "tab_inactive",        "CỐ Ý KHÔNG DÙNG — dự án đã bỏ hệ tab" },
        };

        /// <summary>
        /// Art hiệu ứng nạp theo ĐƯỜNG DẪN (pháo bông + khói). Cột 2 = node/field dùng nó.
        /// </summary>
        private static readonly string[,] BangFx =
        {
            { MillSkin.FxConfetti, "MillCelebrationFX.anhGiay[0] — Sprite Mode = MULTIPLE, " +
                                   "phải lấy sub-asset (LoadAssetAtPath<Sprite> trả null)" },
            { MillSkin.FxSquare,   "MillCelebrationFX.anhGiay[1]" },
            { MillSkin.FxPlus,     "MillCelebrationFX.anhGiay[2]" },
            { MillSkin.FxStar,     "MillCelebrationFX.anhSao" },
            { MillSkin.FxFlare,    "MillCelebrationFX.anhLoe" },
            { MillSkin.FxSmoke,    "MillSmokeFX.anhKhoi (puff thật 128×128 của UI_OrderBoard)" },
            { MillSkin.FxCircle,   "MillSmokeFX.anhBongBong" },
        };

        /// <summary>Sprite tool LUÔN vẽ (không art nào thay được).</summary>
        private static readonly string[] LuonVe =
        {
            "mill_sky_934x226   gradient XANH #79BFED → gần TRẮNG #F4FAFD (smoothstep) + vầng " +
                              "nắng góc phải-trên, bo 2 góc trên r16. KÍCH THƯỚC MỚI: lòng " +
                              "khung gỗ nay 934×378 (cũ 629×250) ⇒ hai PNG cũ " +
                              "mill_sky_629x150 / mill_ground_629x100 thành rác vô hại, xoá tay được",
            "mill_ground_934x152 đất nâu ĐẬM #9B7956 → #7E6346 + VẠCH CHÂN TRỜI 4px #5E4934 + " +
                              "riềm sáng 6px #C89D6F + luống dọc #826445 chu kỳ 46/92px " +
                              "(CSS gốc 30/60 ở khung 629, giãn ×1.49 cho khung 934 để mật độ " +
                              "luống không đổi) + hạt đất ±3%, bo 2 góc dưới r16",
            "mill_dot          đĩa TRẮNG 18px — MillPopupUI tự tô #62E15D (đang xay) / #96918A (rảnh)",
            "mill_belt_stripes TEXTURE 42×42 sọc chéo #2A1D15, Wrap = REPEAT — VẪN CẦN dù băng " +
                              "tải đã dùng art: UIScrollingTexture chỉ cuộn được RawImage có " +
                              "texture Wrap = Repeat",
            "mill_glyph_lock   ổ khoá TRẮNG 46px vẽ đè lên đĩa shop_lock_badge (đĩa không có " +
                              "hình khoá bên trong) + ổ khoá 64px cho lớp phủ khoá của card",
            "mill_glyph_x      dấu X trắng 44px — CHỈ vẽ khi THIẾU art btn_close",
            "mill_reddot       đĩa #FF4A4A viền 3 trắng 18px (góc phải-trên card slot)",
            "mill_item_grain   PLACEHOLDER bó cỏ #D9A85B 52px trên băng tải",
            "mill_drop_ring    VÒNG viền 6px #82C94F r18, lòng TRONG SUỐT, 9-slice từ texture " +
                              "112 (border 48 < 55 ⇒ không bị kẹp) — viền sáng slot nhận thả",
            "mill_bag_glow     quầng sáng toả tròn #FFD24A 296px (cũ 160, nâng theo đĩa thành " +
                              "phẩm 112 → 240), alpha tắt theo (1−d/R)^1.8 về 0 ở rìa. " +
                              "298.95 là TRẦN — phép tính ở MillDesign.BagGlowSize",
            "(CHỈ KHI THIẾU ART) mill_belt_base / mill_chip_bg / mill_toast / mill_list_panel / " +
                              "mill_pill / mill_circle_plate / mill_out_plate / mill_slot_frame / " +
                              "mill_slot_fill / mill_slot_dashed / mill_slot_ready / " +
                              "mill_lock_badge / mill_prog_track / mill_prog_fill / " +
                              "mill_btn_close / mill_gem — nhánh dự phòng, không vẽ nếu art đủ",
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

            // ── 1b. ART HIỆU ỨNG (pháo bông + khói) ──────────────────────────────
            //  Soát cả IMPORT SETTINGS, không chỉ sự tồn tại: textureType != Sprite hoặc
            //  Sprite Mode = Multiple đều làm LoadAssetAtPath<Sprite> trả NULL — hai lỗi im
            //  lặng khiến hiệu ứng "không chạy mà không báo gì".
            int fxCo = 0, fxThieu = 0;
            for (int i = 0; i < BangFx.GetLength(0); i++)
            {
                string pFx = BangFx[i, 0], dungLam = BangFx[i, 1];
                var impFx = AssetImporter.GetAtPath(pFx) as TextureImporter;
                if (impFx == null)
                {
                    rep.Sprite("THIẾU FX  " + pFx + "  ⇒ " + dungLam);
                    rep.Canh("Không có texture hiệu ứng " + pFx + " — " + dungLam +
                             " sẽ để NULL (hiệu ứng tắt, không chặn).");
                    fxThieu++;
                    continue;
                }
                fxCo++;
                rep.Sprite("CÓ FX     " + Path.GetFileName(pFx) + "  textureType=" +
                           impFx.textureType + "  spriteMode=" + impFx.spriteImportMode +
                           "  → " + dungLam);
                if (impFx.textureType != TextureImporterType.Sprite)
                    rep.Canh("Texture " + Path.GetFileName(pFx) + " đang textureType = " +
                             impFx.textureType + " ⇒ lệnh 1 sẽ TỰ ĐẶT về Sprite rồi reimport.");
                if (impFx.spriteImportMode == SpriteImportMode.Multiple)
                    rep.Ok("Texture " + Path.GetFileName(pFx) + " ở Sprite Mode = Multiple — " +
                           "tool lấy ô ĐẦU TIÊN qua LoadAllAssetRepresentationsAtPath " +
                           "(LoadAssetAtPath<Sprite> trả null ở chế độ này).");
            }
            rep.Ok("Art hiệu ứng: " + fxCo + "/" + BangFx.GetLength(0) + " có sẵn, " +
                   fxThieu + " thiếu.");

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
                    "imgMainButtonBg","fxNguyenLieu","fxBaoRa","fxBayVeKho",
                    // Hai FX thêm 21/08 — nếu MillPopupUI chưa có field thì dòng này báo
                    // "SCRIPT KHÔNG CÓ FIELD NÀY", đúng thứ cần biết.
                    "fxPhaoHoa","fxKhoi"
                }, rep);

                SoatSlot(ui, rep);
                SoatAnimation(ui, rep);
                SoatHieuUng(ui, rep);
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
            rep.Can("Kiểm tra bằng mắt: mở popup trong Play mode ở CanvasScaler 1920×1080 và " +
                    "so với ẢNH DUYỆT /tmp/mill/prev/mock_new.png (render bởi /tmp/mill/mock.py). " +
                    "KHÔNG so với full_mill_ui.html nữa — file đó vẽ ở viewport 1000×680, chính " +
                    "nó là nguồn của lỗi 'popup bé, chữ tí' của bản trước.");
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
                "btnUnlockGem","redDot","dropHighlight"
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

        /// <summary>
        /// Soát 3 hiệu ứng của luồng kéo-thả: đúng component, ĐÚNG NODE, đủ mốc để chạy.
        ///
        /// MillPopupUI coi cả ba là TUỲ CHỌN (null thì mẻ xay vẫn đúng) ⇒ đặt sai node hay
        /// để null KHÔNG sinh lỗi nào, hiệu ứng chỉ đơn giản là không thấy. Đúng loại lỗi
        /// im lặng mà lệnh 3 tồn tại để bắt.
        /// </summary>
        private static void SoatHieuUng(MillPopupUI ui, MillReport rep)
        {
            var so = new SerializedObject(ui);

            // ── Hạt nguyên liệu bay vào máy — phải nằm trên `AnimationBox` ────────
            var fxIn = LayFX<MillIntakeFX>(so, "fxNguyenLieu");
            if (fxIn == null)
                rep.Canh("MillPopupUI.fxNguyenLieu đang NULL ⇒ thả bao vào slot sẽ không có " +
                         "hạt nào bay vào máy và máy không nhún. Chạy lệnh 1 để dựng lại.");
            else
            {
                DungNode(fxIn, "AnimationBox", rep);
                SoatFields(new SerializedObject(fxIn), "MillIntakeFX",
                    new[] { "diemXuatPhat", "diemDich", "noiChuaHat", "thanMay" }, rep);
            }

            // ── Bao thành phẩm nảy ra — phải nằm trên `Output_Bubble` ─────────────
            var fxBag = LayFX<MillOutputBagFX>(so, "fxBaoRa");
            if (fxBag == null)
                rep.Canh("MillPopupUI.fxBaoRa đang NULL ⇒ xay xong bao KHÔNG nảy ra và không " +
                         "có quầng sáng nhắc 'còn hàng chờ thu'. Chạy lệnh 1 để dựng lại.");
            else
            {
                DungNode(fxBag, "Output_Bubble", rep);
                SoatFields(new SerializedObject(fxBag), "MillOutputBagFX",
                    new[] { "bao", "imgGlow" }, rep);
            }

            // ── Icon bay về kho — phải nằm CHUNG node với MillPopupUI (canvas order 400) ──
            var fxFly = LayFX<MillCollectFlyFX>(so, "fxBayVeKho");
            if (fxFly == null)
                rep.Canh("MillPopupUI.fxBayVeKho đang NULL ⇒ bấm THU thì hàng vẫn vào kho " +
                         "nhưng không có icon nào bay. Chạy lệnh 1 để dựng lại.");
            else
            {
                if (fxFly.gameObject != ui.gameObject)
                    rep.Canh("MillCollectFlyFX đang nằm trên '" + fxFly.gameObject.name +
                             "' chứ không phải node gốc popup ('" + ui.gameObject.name +
                             "'). Icon phải gắn vào Canvas order " + MillDesign.SortOrder +
                             " của popup, gắn vào Canvas_HUD (order 100) là icon bay DƯỚI " +
                             "popup rồi biến mất.");
                else if (fxFly.GetComponent<Canvas>() == null)
                    rep.Canh("Node gốc popup KHÔNG có Canvas ⇒ MillCollectFlyFX không có " +
                             "canvas để gắn icon bay.");
                else
                    rep.Ok("MillCollectFlyFX nằm đúng node gốc popup (có Canvas).");

                SoatFields(new SerializedObject(fxFly), "MillCollectFlyFX",
                    new[] { "canvasBay" }, rep);
                // `diemDen` CỐ Ý để trống — xem chú thích ở MillPopupBuilder.Dung.
                rep.Ok("MillCollectFlyFX.diemDen CỐ Ý để trống: đích là nút KHO ở HUD do tool " +
                       "khác dựng, script tự tìm qua TownshipHUDController lúc chạy.");
            }

            // ── Pháo bông xay xong — phải nằm trên `AnimationBox` ─────────────────
            var fxCel = LayFX<MillCelebrationFX>(so, "fxPhaoHoa");
            if (fxCel == null)
                rep.Canh("MillPopupUI.fxPhaoHoa đang NULL ⇒ xay xong KHÔNG có pháo bông. " +
                         "Chạy lệnh 1 để dựng lại.");
            else
            {
                DungNode(fxCel, "AnimationBox", rep);
                SoatFields(new SerializedObject(fxCel), "MillCelebrationFX",
                    new[] { "noiChua", "anhGiay", "anhSao", "anhLoe" }, rep);
            }

            // ── Khói máy đang xay — phải nằm trên `AnimationBox` ──────────────────
            var fxSmoke = LayFX<MillSmokeFX>(so, "fxKhoi");
            if (fxSmoke == null)
                rep.Canh("MillPopupUI.fxKhoi đang NULL ⇒ máy chạy mà không có khói. " +
                         "Chạy lệnh 1 để dựng lại.");
            else
            {
                DungNode(fxSmoke, "AnimationBox", rep);
                SoatFields(new SerializedObject(fxSmoke), "MillSmokeFX",
                    new[] { "noiChua", "mieng", "anhKhoi", "anhBongBong" }, rep);

                // `mieng` phải là node máy, không phải chính AnimationBox — khói phun ra từ
                // phễu. Đặt sai thì khói bốc lên từ giữa khung, trông như lỗi render.
                var pMieng = new SerializedObject(fxSmoke).FindProperty("mieng");
                var mieng = pMieng != null ? pMieng.objectReferenceValue as RectTransform : null;
                if (mieng != null && !string.Equals(mieng.name, "Machine", StringComparison.Ordinal))
                    rep.Canh("MillSmokeFX.mieng đang trỏ vào '" + mieng.name +
                             "' — tool wire vào node 'Machine' (phễu máy).");
            }
        }

        /// <summary>Đọc một field tham chiếu private ra component cụ thể. null nếu không có.</summary>
        private static T LayFX<T>(SerializedObject so, string field) where T : Component
        {
            SerializedProperty p = so != null ? so.FindProperty(field) : null;
            return p != null ? p.objectReferenceValue as T : null;
        }

        /// <summary>Cảnh báo nếu một component hiệu ứng không nằm trên node tool đã dựng.</summary>
        private static void DungNode(Component c, string tenMongDoi, MillReport rep)
        {
            if (c == null) return;
            if (string.Equals(c.gameObject.name, tenMongDoi, StringComparison.Ordinal))
                rep.Ok(c.GetType().Name + " nằm đúng node '" + tenMongDoi + "'.");
            else
                rep.Canh(c.GetType().Name + " đang nằm trên node '" + c.gameObject.name +
                         "' — tool dựng nó trên '" + tenMongDoi + "'. Mọi toạ độ mốc của hiệu " +
                         "ứng quy về node này nên đặt sai chỗ là hạt/bao chạy lệch chỗ.");
        }
    }
}
#endif
