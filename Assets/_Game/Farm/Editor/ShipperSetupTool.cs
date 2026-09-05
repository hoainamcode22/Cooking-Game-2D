using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// TOOL 4 — SETUP CÔ GÁI GIỎ HOA (shipper).
///
/// Menu:
///   Tools/Farm Game/Shipper/★ SETUP cô gái giỏ hoa (1 nút)
///   Tools/Farm Game/Shipper/Tạo Shipper_HomeAnchor trong scene (cần Sếp bấm riêng)
///   Tools/Farm Game/Shipper/Kiểm tra sẵn sàng
///   Tools/Farm Game/Shipper/Test giao hàng ngay (Play Mode)
///
/// LÀM GÌ (nút SETUP)
///   1. Đọc 12 sprite từ flowergirl_walk_spritesheet.png theo ĐÚNG thứ tự §5.1:
///        fg_down_1,2,3 · fg_left_1,2,3 · fg_right_1,2,3 · fg_up_1,2,3
///      Thứ tự này BẮT BUỘC: FourDirWalkAnimator.SetupFromFlat() cắt mảng theo INDEX
///      (0-2 = down, 3-5 = left, 6-8 = right, 9-11 = up). Đảo thứ tự ⇒ cô gái đi
///      ngang mà hiện mặt trước.
///      CHƯA SLICE ⇒ ABORT, chỉ Sếp chạy CharacterSheetSliceTool trước.
///   2. Tạo/cập nhật Assets/_Game/Resources/ShipperConfig.asset.
///   3. Dựng prefab Assets/_Game/Farm/Prefabs/Shipper/FlowerGirl_Shipper.prefab:
///      SpriteRenderer + SortingGroup + BoxCollider2D(isTrigger) +
///      SpriteSequencePlayer + FourDirWalkAnimator + FlowerGirlShipper.
///      KHÔNG Animator · KHÔNG TouristAgent · KHÔNG TouristRequestBubble.
///   4. Gán prefab vào ShipperConfig.shipperPrefab.
///
/// CỐ Ý KHÔNG LÀM
///   • KHÔNG set enabled = true (§9 CONTRACT — Sếp tự tick).
///   • KHÔNG tự tạo Shipper_HomeAnchor: đó là SỬA SCENE .unity, thuộc DANH SÁCH DỪNG.
///     Nút SETUP chỉ IN hướng dẫn + toạ độ. Muốn tạo thì bấm menu RIÊNG bên trên,
///     nó hỏi xác nhận và KHÔNG tự save scene (Sếp tự Ctrl+S).
///
/// IDEMPOTENT: prefab/config đã có ⇒ cập nhật tại chỗ, không nhân bản. Menu tạo
/// anchor thấy object cùng tên ⇒ chỉ chọn nó, không tạo cái thứ hai.
/// </summary>
public static class ShipperSetupTool
{
    // ─── Menu ────────────────────────────────────────────────────────────
    private const string MenuRoot   = "Tools/Farm Game/Shipper/";
    private const string MenuSetup  = MenuRoot + "★ SETUP cô gái giỏ hoa (1 nút)";
    private const string MenuAnchor = MenuRoot + "Tạo Shipper_HomeAnchor trong scene (cần Sếp bấm riêng)";
    private const string MenuCheck  = MenuRoot + "Kiểm tra sẵn sàng";
    private const string MenuTest   = MenuRoot + "Test giao hàng ngay (Play Mode)";

    private const string Tag = "[Tool]";

    // ─── Hằng số CHỈNH ĐƯỢC ──────────────────────────────────────────────

    /// <summary>Chiều cao cô gái trong world (unit) — khớp ShipperConfig.worldHeight mặc định.</summary>
    private const float ShipperWorldHeight = 170f;

    /// <summary>Order gốc §2 CONTRACT (FlowerGirlShipper tự y-sort động quanh mốc này).</summary>
    private const int ShipperSortingOrder = 5000;

    /// <summary>Toạ độ neo nhà cô gái — Lead đã đo trên SCN_Farm.</summary>
    private static readonly Vector3 HomeAnchorPos = new Vector3(-879f, -760f, 0f);

    private const string HomeAnchorName = "Shipper_HomeAnchor";
    private const string FarmSceneName  = "SCN_Farm";

    // ─── Đường dẫn ───────────────────────────────────────────────────────
    private const string ResourcesFolder = "Assets/_Game/Resources";
    private const string ConfigPath      = ResourcesFolder + "/ShipperConfig.asset";
    private const string PrefabFolder    = "Assets/_Game/Farm/Prefabs/Shipper";
    private const string PrefabPath      = PrefabFolder + "/FlowerGirl_Shipper.prefab";

    /// <summary>Sprite dùng làm hình đứng mặc định trên prefab (frame giữa hàng "down").</summary>
    private const string SpriteDungYen = "fg_down_2";

    // ═════════════════════════════════════════════════════════════════════
    //  MENU 1 — SETUP
    // ═════════════════════════════════════════════════════════════════════
    [MenuItem(MenuSetup, false, 10)]
    public static void Setup()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Tag + " ShipperSetupTool — SETUP CÔ GÁI GIỎ HOA");
        sb.AppendLine("──────────────────────────────────────────────────────────────────────");

        // ── 1 · Nạp 12 frame theo ĐÚNG thứ tự §5.1 ───────────────────────
        string[] ten = CharacterSheetSliceTool.FlowerGirlFrameNames();
        Sprite[] walk = CharacterSheetSliceTool.LoadFramesByName(CharacterSheetSliceTool.PathFlowerGirl, ten);

        if (walk == null)
        {
            sb.AppendLine("✖ ABORT: sheet cô gái chưa slice đủ 12 sprite con.");
            sb.AppendLine("   " + CharacterSheetSliceTool.PathFlowerGirl);
            sb.AppendLine("   cần đúng các tên: " + string.Join(", ", ten));
            sb.AppendLine();
            sb.AppendLine("SẾP BẤM TRƯỚC: Tools/Farm Game/Characters/★ Slice 3 spritesheet nhân vật (APPLY)");
            sb.AppendLine("               rồi chạy lại menu này.");
            sb.AppendLine($"{Tag} TỔNG KẾT: ABORT — chưa slice spritesheet, không ghi gì lên đĩa.");
            Debug.LogError(sb.ToString());
            return;
        }
        sb.AppendLine("✔ nạp walkFrames 12/12 theo thứ tự §5.1:");
        sb.AppendLine("   [0-2] " + ten[0] + ", " + ten[1] + ", " + ten[2] + "   (down)");
        sb.AppendLine("   [3-5] " + ten[3] + ", " + ten[4] + ", " + ten[5] + "   (left)");
        sb.AppendLine("   [6-8] " + ten[6] + ", " + ten[7] + ", " + ten[8] + "   (right)");
        sb.AppendLine("   [9-11] " + ten[9] + ", " + ten[10] + ", " + ten[11] + "  (up)");

        // ── 2 · Thư mục ──────────────────────────────────────────────────
        if (!EnsureFolder(ResourcesFolder) || !EnsureFolder(PrefabFolder))
        {
            sb.AppendLine("✖ ABORT: không tạo được " + ResourcesFolder + " hoặc " + PrefabFolder + ".");
            sb.AppendLine($"{Tag} TỔNG KẾT: THẤT BẠI (thiếu thư mục đích).");
            Debug.LogError(sb.ToString());
            return;
        }

        // ── 3 · Config ───────────────────────────────────────────────────
        var cfg = AssetDatabase.LoadAssetAtPath<ShipperConfig>(ConfigPath);
        bool cfgMoi = cfg == null;
        if (cfgMoi)
        {
            cfg = ScriptableObject.CreateInstance<ShipperConfig>();
            AssetDatabase.CreateAsset(cfg, ConfigPath);
        }
        Undo.RecordObject(cfg, "Setup co gai gio hoa");

        cfg.walkFrames = walk;
        // walkFps · walkSpeed · homeAnchorOffset · roadColor… CỐ Ý KHÔNG chạm —
        // đó là các ô Sếp/DEV-C tinh chỉnh. Chỉ vá khi giá trị vô nghĩa.
        if (cfg.walkFps     <= 0.01f) cfg.walkFps     = 8f;
        if (cfg.worldHeight <= 1f)    cfg.worldHeight = ShipperWorldHeight;

        sb.AppendLine($"✔ config {(cfgMoi ? "TẠO MỚI" : "cập nhật")}: {ConfigPath}");
        sb.AppendLine($"   walkFps={cfg.walkFps:0.##} · worldHeight={cfg.worldHeight:0.##} · " +
                      $"walkSpeed={cfg.walkSpeed:0.##} · homeAnchorOffset={cfg.homeAnchorOffset}");

        // ── 4 · Sprite đứng yên + scale thật ─────────────────────────────
        Sprite spDung = walk[1];   // fg_down_2 — frame giữa hàng down (§5.1 idle)
        float caoSprite = spDung != null ? spDung.bounds.size.y : 0f;
        float scale     = caoSprite > 0.0001f ? cfg.worldHeight / caoSprite : 1f;

        sb.AppendLine($"✔ tính scale: {SpriteDungYen}.bounds.size.y = {caoSprite:0.####} world unit " +
                      $"(PPU 100) ⇒ scale = {cfg.worldHeight:0.##} / {caoSprite:0.####} = {scale:0.####}");
        if (caoSprite <= 0.0001f)
            sb.AppendLine("   ⚠ bounds.size.y = 0 ⇒ tạm dùng scale 1. Kiểm tra lại slice.");

        // ── 5 · Sorting layer ────────────────────────────────────────────
        string layer = ResolveSortingLayer("ObjectsFront", "Objects", "Default");
        sb.AppendLine($"✔ sorting layer resolve: \"{layer}\" · order {ShipperSortingOrder} " +
                      "(runtime FlowerGirlShipper resolve lại + y-sort động).");

        // ── 6 · Prefab ───────────────────────────────────────────────────
        bool prefabMoi;
        string loiPrefab;
        GameObject prefab = BuildOrUpdatePrefab(PrefabPath, "FlowerGirl_Shipper", spDung, layer,
                                               ShipperSortingOrder, cfg, scale, out prefabMoi, out loiPrefab);
        if (prefab == null)
        {
            sb.AppendLine("✖ prefab THẤT BẠI: " + loiPrefab);
            sb.AppendLine($"{Tag} TỔNG KẾT: THẤT BẠI ở bước dựng prefab.");
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            Debug.LogError(sb.ToString());
            return;
        }

        sb.AppendLine($"✔ prefab {(prefabMoi ? "TẠO MỚI" : "cập nhật")}: {PrefabPath}");
        sb.AppendLine("   SpriteRenderer + SortingGroup + BoxCollider2D(isTrigger) + " +
                      "SpriteSequencePlayer + FourDirWalkAnimator + FlowerGirlShipper");
        sb.AppendLine("   KHÔNG Animator · KHÔNG TouristAgent · KHÔNG TouristRequestBubble");

        cfg.shipperPrefab = prefab;
        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        sb.AppendLine("✔ gán ShipperConfig.shipperPrefab.");

        // ── 7 · Hướng dẫn phần thuộc DANH SÁCH DỪNG ──────────────────────
        GameObject anchor = TimObjectTheoTen(HomeAnchorName);
        sb.AppendLine();
        sb.AppendLine("──────────────────────────────────────────────────────────────────────");
        sb.AppendLine("CỐ Ý KHÔNG LÀM (DANH SÁCH DỪNG — sửa .unity):");
        sb.AppendLine("  Tool KHÔNG tự tạo " + HomeAnchorName + " trong scene.");
        sb.AppendLine("  Hiện tại trong scene: " + (anchor != null
                        ? "✅ ĐÃ CÓ tại " + anchor.transform.position
                        : "❌ CHƯA CÓ"));
        sb.AppendLine("SẾP LÀM 1 TRONG 2 CÁCH:");
        sb.AppendLine("  A) Bấm menu: " + MenuAnchor);
        sb.AppendLine("     (có hộp xác nhận · tool KHÔNG tự save · nhớ Ctrl+S sau đó)");
        sb.AppendLine("  B) Làm tay: mở Assets/_Game/Scenes/" + FarmSceneName + ".unity →");
        sb.AppendLine("     GameObject > Create Empty → đổi tên chính xác \"" + HomeAnchorName + "\" →");
        sb.AppendLine($"     Position = ({HomeAnchorPos.x:0}, {HomeAnchorPos.y:0}, {HomeAnchorPos.z:0}) → Ctrl+S");
        sb.AppendLine();
        sb.AppendLine("CỐ Ý KHÔNG LÀM (§9 CONTRACT): enabled vẫn FALSE.");
        sb.AppendLine("SẾP BẤM TAY: chọn " + ConfigPath + " → tick \"enabled\".");
        sb.AppendLine($"{Tag} TỔNG KẾT SETUP: config {(cfgMoi ? "TẠO MỚI" : "cập nhật")} · " +
                      $"prefab {(prefabMoi ? "TẠO MỚI" : "cập nhật")} · 12 walkFrames đã gán · " +
                      $"scale {scale:0.####}. Còn 2 việc tay: tạo {HomeAnchorName} + tick enabled.");
        Debug.Log(sb.ToString());

        Selection.activeObject = cfg;
        EditorGUIUtility.PingObject(cfg);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MENU 2 — TẠO Shipper_HomeAnchor (SỬA SCENE — bấm riêng)
    // ═════════════════════════════════════════════════════════════════════
    [MenuItem(MenuAnchor, false, 11)]
    public static void TaoHomeAnchor()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Tag + " ShipperSetupTool — TẠO " + HomeAnchorName);

        GameObject daCo = TimObjectTheoTen(HomeAnchorName);
        if (daCo != null)
        {
            sb.AppendLine("✔ ĐÃ CÓ sẵn " + HomeAnchorName + " tại " + daCo.transform.position +
                          " (scene \"" + daCo.scene.name + "\") — KHÔNG tạo cái thứ hai.");
            sb.AppendLine($"{Tag} TỔNG KẾT: không sửa gì (idempotent).");
            Debug.Log(sb.ToString());
            Selection.activeGameObject = daCo;
            EditorGUIUtility.PingObject(daCo);
            EditorUtility.DisplayDialog("Tạo " + HomeAnchorName,
                "Scene đã có \"" + HomeAnchorName + "\" tại " + daCo.transform.position +
                ".\nKhông tạo thêm.", "OK");
            return;
        }

        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || !active.isLoaded)
        {
            sb.AppendLine("✖ DỪNG: không có scene nào đang mở.");
            sb.AppendLine($"{Tag} TỔNG KẾT: không sửa gì.");
            Debug.LogError(sb.ToString());
            EditorUtility.DisplayDialog("Tạo " + HomeAnchorName, "Không có scene nào đang mở.", "OK");
            return;
        }

        if (active.name != FarmSceneName)
        {
            sb.AppendLine("✖ DỪNG: scene đang mở là \"" + active.name + "\", không phải \"" + FarmSceneName + "\".");
            sb.AppendLine("  CẦN LÀM: mở Assets/_Game/Scenes/" + FarmSceneName + ".unity rồi bấm lại menu này.");
            sb.AppendLine($"{Tag} TỔNG KẾT: không sửa gì.");
            Debug.LogWarning(sb.ToString());
            EditorUtility.DisplayDialog("Tạo " + HomeAnchorName,
                "Scene đang mở là \"" + active.name + "\".\n\n" +
                "Tool chỉ tạo anchor trong \"" + FarmSceneName + "\" để không rác scene khác.\n" +
                "Mở " + FarmSceneName + " rồi bấm lại.", "OK");
            return;
        }

        bool dongY = EditorUtility.DisplayDialog(
            "Tạo " + HomeAnchorName + " — SẼ SỬA SCENE",
            "⚠ Việc này SỬA SCENE \"" + active.name + "\".\n\n" +
            "Sẽ tạo 1 GameObject rỗng:\n" +
            "   tên      : " + HomeAnchorName + "\n" +
            $"   position : ({HomeAnchorPos.x:0}, {HomeAnchorPos.y:0}, {HomeAnchorPos.z:0})\n\n" +
            "Tool KHÔNG tự save scene. Sau khi tạo, Sếp NHỚ Ctrl+S.\n" +
            "Không muốn thì bấm Thôi (có thể Ctrl+Z để hoàn tác sau).",
            "TẠO (tôi sẽ Ctrl+S)", "Thôi");

        if (!dongY)
        {
            sb.AppendLine("Sếp bấm Thôi — không tạo gì.");
            sb.AppendLine($"{Tag} TỔNG KẾT: không sửa gì.");
            Debug.Log(sb.ToString());
            return;
        }

        var go = new GameObject(HomeAnchorName);
        go.transform.position   = HomeAnchorPos;
        go.transform.rotation   = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        Undo.RegisterCreatedObjectUndo(go, "Tao " + HomeAnchorName);
        EditorSceneManager.MarkSceneDirty(active);

        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);

        sb.AppendLine("✔ đã tạo " + HomeAnchorName + " tại " + HomeAnchorPos + " trong scene \"" + active.name + "\".");
        sb.AppendLine("⚠ SCENE ĐANG DIRTY — tool KHÔNG tự save. SẾP BẤM Ctrl+S NGAY.");
        sb.AppendLine($"{Tag} TỔNG KẾT: tạo 1 GameObject rỗng, chưa save scene.");
        Debug.Log(sb.ToString());
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MENU 3 — KIỂM TRA SẴN SÀNG
    // ═════════════════════════════════════════════════════════════════════
    [MenuItem(MenuCheck, false, 12)]
    public static void KiemTraSanSang()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Tag + " ShipperSetupTool — KIỂM TRA SẴN SÀNG");
        sb.AppendLine("──────────────────────────────────────────────────────────────────────");

        int ok = 0, fail = 0;

        // 1 · config
        var cfg = AssetDatabase.LoadAssetAtPath<ShipperConfig>(ConfigPath);
        bool c1 = cfg != null;
        Ghi(sb, c1, "ShipperConfig tồn tại", ConfigPath, ref ok, ref fail,
            "chạy " + MenuSetup);

        // 2 · 12 walkFrames
        int soFrame = 0;
        bool duFrame = false;
        if (cfg != null && cfg.walkFrames != null)
        {
            for (int i = 0; i < cfg.walkFrames.Length; i++) if (cfg.walkFrames[i] != null) soFrame++;
            duFrame = cfg.walkFrames.Length >= 12 && soFrame >= 12;
        }
        Ghi(sb, duFrame, "walkFrames đủ 12 sprite", soFrame + "/12 sprite khác null", ref ok, ref fail,
            "chạy Tools/Farm Game/Characters/★ Slice 3 spritesheet nhân vật (APPLY) rồi chạy lại SETUP");

        // 3 · prefab
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        bool c3 = prefab != null;
        Ghi(sb, c3, "prefab FlowerGirl_Shipper tồn tại", PrefabPath, ref ok, ref fail,
            "chạy " + MenuSetup);

        // 3b · prefab đã gán vào config chưa
        bool c3b = cfg != null && cfg.shipperPrefab != null;
        Ghi(sb, c3b, "ShipperConfig.shipperPrefab đã gán",
            c3b ? cfg.shipperPrefab.name : "(trống)", ref ok, ref fail, "chạy " + MenuSetup);

        // 4 · anchor trong scene
        GameObject anchor = TimObjectTheoTen(HomeAnchorName);
        bool c4 = anchor != null;
        Ghi(sb, c4, HomeAnchorName + " có trong scene đang mở",
            c4 ? anchor.transform.position.ToString() + " (scene \"" + anchor.scene.name + "\")"
               : "chưa có — DEV-C sẽ fallback về bảng đơn + homeAnchorOffset",
            ref ok, ref fail, "bấm menu: " + MenuAnchor);

        // 5 · số nhà trong scene
        var nha = Object.FindObjectsByType<HouseGrowthController>(FindObjectsSortMode.None);
        int soNha = nha != null ? nha.Length : 0;
        Ghi(sb, soNha > 0, "có HouseGrowthController trong scene", soNha + " nhà", ref ok, ref fail,
            "mở SCN_Farm (shipper cần ít nhất 1 nhà để giao)");

        // 5b · nhà đã Completed (cấu hình onlyDeliverToCompletedHouses)
        if (soNha > 0 && cfg != null && cfg.onlyDeliverToCompletedHouses)
        {
            int xong = 0;
            for (int i = 0; i < nha.Length; i++)
                if (nha[i] != null && nha[i].State == HouseGrowthController.GrowthState.Completed) xong++;
            sb.AppendLine((xong > 0 ? "✅" : "❌") + " nhà đã Completed (onlyDeliverToCompletedHouses = true): " +
                          xong + "/" + soNha);
            if (xong > 0) ok++; else
            {
                fail++;
                sb.AppendLine("     → CẦN LÀM: xây xong ít nhất 1 nhà, hoặc bỏ tick onlyDeliverToCompletedHouses.");
            }
        }

        // 6 · enabled
        bool c6 = cfg != null && cfg.enabled;
        Ghi(sb, c6, "ShipperConfig.enabled đã tick", c6 ? "TRUE" : "FALSE (mặc định an toàn §9)",
            ref ok, ref fail, "chọn " + ConfigPath + " → tick \"enabled\" (tool CỐ Ý không tự tick)");

        sb.AppendLine();
        sb.AppendLine("──────────────────────────────────────────────────────────────────────");
        sb.AppendLine($"{Tag} TỔNG KẾT KIỂM TRA: {ok} ✅ · {fail} ❌. " +
                      (fail == 0 ? "SẴN SÀNG — vào Play Mode rồi bấm menu Test giao hàng ngay."
                                 : "Xử các dòng ❌ ở trên theo gợi ý \"CẦN LÀM\"."));
        Debug.Log(sb.ToString());
    }

    private static void Ghi(StringBuilder sb, bool dat, string nhan, string chiTiet,
                            ref int ok, ref int fail, string canLam)
    {
        sb.AppendLine((dat ? "✅ " : "❌ ") + nhan + " — " + chiTiet);
        if (dat) ok++;
        else
        {
            fail++;
            sb.AppendLine("     → CẦN LÀM: " + canLam);
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MENU 4 — TEST GIAO HÀNG (Play Mode)
    // ═════════════════════════════════════════════════════════════════════
    [MenuItem(MenuTest, false, 13)]
    public static void TestGiaoHang()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(Tag + " ShipperSetupTool — TEST GIAO HÀNG: phải VÀO PLAY MODE trước " +
                             "(shipper là hệ runtime, Editor Mode không có instance nào). " +
                             "Bấm ▶ rồi bấm lại menu này.\n" +
                             Tag + " TỔNG KẾT: không làm gì.");
            EditorUtility.DisplayDialog("Test giao hàng",
                "Phải vào Play Mode (bấm ▶) trước rồi bấm lại menu này.", "OK");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine(Tag + " ShipperSetupTool — TEST GIAO HÀNG (Play Mode)");

        if (ShipperManager.Instance == null)
        {
            sb.AppendLine("• ShipperManager.Instance = null ⇒ gọi EnsureInstance().");
            ShipperManager.EnsureInstance();
        }

        var mgr = ShipperManager.Instance;
        if (mgr == null)
        {
            sb.AppendLine("✖ vẫn không có ShipperManager sau EnsureInstance().");
            sb.AppendLine("  CẦN LÀM: kiểm tra ShipperConfig.enabled đã tick chưa " +
                          "(flag tắt thì manager tự thoát) — chạy menu \"Kiểm tra sẵn sàng\".");
            sb.AppendLine($"{Tag} TỔNG KẾT: không giao được.");
            Debug.LogError(sb.ToString());
            return;
        }

        sb.AppendLine($"• trạng thái trước khi gọi: HasShipper={mgr.HasShipper} · " +
                      $"IsShipperBusy={mgr.IsShipperBusy} · QueuedCount={mgr.QueuedCount}");
        mgr.TriggerDelivery();
        sb.AppendLine("✔ đã gọi ShipperManager.Instance.TriggerDelivery().");
        sb.AppendLine($"• trạng thái sau khi gọi : HasShipper={mgr.HasShipper} · " +
                      $"IsShipperBusy={mgr.IsShipperBusy} · QueuedCount={mgr.QueuedCount}");
        sb.AppendLine($"{Tag} TỔNG KẾT: đã bắn 1 lệnh giao hàng. Xem log [Shipper] ở Console.");
        Debug.Log(sb.ToString());
    }

    // ═════════════════════════════════════════════════════════════════════
    //  DỰNG PREFAB
    // ═════════════════════════════════════════════════════════════════════
    private static GameObject BuildOrUpdatePrefab(string path, string tenObject, Sprite spriteDung,
                                                  string sortingLayer, int sortingOrder,
                                                  ShipperConfig cfg, float scale,
                                                  out bool moi, out string loi)
    {
        moi = !File.Exists(path);
        loi = null;

        GameObject root = null;
        try
        {
            root = moi ? new GameObject(tenObject) : PrefabUtility.LoadPrefabContents(path);
            if (root == null) { loi = "không mở/tạo được prefab."; return null; }

            var sr = root.GetComponent<SpriteRenderer>();
            if (sr == null) sr = root.AddComponent<SpriteRenderer>();
            sr.sprite           = spriteDung;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder     = sortingOrder;

            // SortingGroup: gom mọi renderer con (giỏ hoa, bóng…) vào cùng 1 lớp sắp xếp
            // để cô gái không bị "xé" khi đi qua vật khác.
            if (root.GetComponent<SortingGroup>() == null)
            {
                var sg = root.AddComponent<SortingGroup>();
                sg.sortingLayerName = sortingLayer;
                sg.sortingOrder     = sortingOrder;
            }

            var box = root.GetComponent<BoxCollider2D>();
            if (box == null) box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            if (spriteDung != null)
            {
                Vector2 size = spriteDung.bounds.size;
                box.size   = size;
                box.offset = new Vector2(0f, size.y * 0.5f);   // pivot Bottom-Center
            }

            var player = root.GetComponent<SpriteSequencePlayer>();
            if (player == null) player = root.AddComponent<SpriteSequencePlayer>();
            player.target          = sr;
            player.frames          = null;      // FourDirWalkAnimator.SetupFromFlat() nạp lúc chạy
            player.fps             = cfg != null ? cfg.walkFps : 8f;
            player.loop            = true;
            player.pingPong        = true;      // §5.1: walk ping-pong 1-2-3-2
            player.playOnEnable    = false;     // chờ FlowerGirlShipper.Setup() ra lệnh
            player.useUnscaledTime = false;

            if (root.GetComponent<FourDirWalkAnimator>() == null) root.AddComponent<FourDirWalkAnimator>();
            if (root.GetComponent<FlowerGirlShipper>()   == null) root.AddComponent<FlowerGirlShipper>();

            // KHÔNG thêm Animator / TouristAgent / TouristRequestBubble. Có sẵn thì CẢNH BÁO
            // chứ không tự gỡ (gỡ component là sửa chỉnh tay của Sếp).
            if (root.GetComponent<Animator>() != null)
                Debug.LogWarning(Tag + " " + tenObject + " ĐANG CÓ Animator — nó ghi đè sprite mỗi frame " +
                                 "và sẽ đá nhau với SpriteSequencePlayer. Gỡ tay component Animator.");
            CanhBaoComponentLa(root, "TouristAgent", tenObject);
            CanhBaoComponentLa(root, "TouristRequestBubble", tenObject);

            if (scale > 0.0001f) root.transform.localScale = new Vector3(scale, scale, 1f);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (saved == null) loi = "SaveAsPrefabAsset trả về null.";
            return saved;
        }
        catch (System.Exception e)
        {
            loi = "ngoại lệ — " + e.Message;
            return null;
        }
        finally
        {
            if (root != null)
            {
                if (moi) Object.DestroyImmediate(root);
                else     PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void CanhBaoComponentLa(GameObject root, string tenClass, string tenObject)
    {
        System.Type t = System.Type.GetType(tenClass + ", Assembly-CSharp");
        if (t == null) t = System.Type.GetType(tenClass);
        if (t == null) return;                       // class không tồn tại ⇒ khỏi lo

        if (root.GetComponent(t) != null)
            Debug.LogWarning(Tag + " " + tenObject + " ĐANG CÓ " + tenClass +
                             " — DEV-C yêu cầu prefab shipper KHÔNG dùng hệ khách du lịch. Gỡ tay.");
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TIỆN ÍCH
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tìm GameObject theo TÊN trong mọi scene đang load, KỂ CẢ object đang tắt
    /// (GameObject.Find bỏ qua object inactive — dùng một mình sẽ báo "chưa có"
    /// dù Sếp đã tạo rồi mà tắt đi).
    /// </summary>
    private static GameObject TimObjectTheoTen(string ten)
    {
        if (string.IsNullOrEmpty(ten)) return null;

        GameObject nhanh = GameObject.Find(ten);
        if (nhanh != null) return nhanh;

        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            Scene sc = SceneManager.GetSceneAt(s);
            if (!sc.IsValid() || !sc.isLoaded) continue;

            GameObject[] roots = sc.GetRootGameObjects();
            if (roots == null) continue;

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject found = TimTrongCay(roots[i], ten);
                if (found != null) return found;
            }
        }
        return null;
    }

    private static GameObject TimTrongCay(GameObject go, string ten)
    {
        if (go == null) return null;
        if (go.name == ten) return go;

        Transform tr = go.transform;
        for (int i = 0; i < tr.childCount; i++)
        {
            GameObject found = TimTrongCay(tr.GetChild(i).gameObject, ten);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Trả tên sorting layer ĐẦU TIÊN có thật. KHÔNG hardcode "CongTrinh" (layer đó
    /// không tồn tại — §7 CONTRACT: Unity im lặng đẩy renderer về Default).
    /// </summary>
    private static string ResolveSortingLayer(params string[] uuTien)
    {
        if (uuTien == null || uuTien.Length == 0) return "Default";

        SortingLayer[] ds = SortingLayer.layers;
        if (ds != null)
        {
            for (int i = 0; i < uuTien.Length; i++)
            {
                if (string.IsNullOrEmpty(uuTien[i])) continue;
                for (int k = 0; k < ds.Length; k++)
                {
                    if (ds[k].name != uuTien[i]) continue;
                    if (i > 0)
                        Debug.LogWarning(Tag + " sorting layer \"" + uuTien[0] + "\" không có trong project — dùng tạm \"" + uuTien[i] + "\".");
                    return uuTien[i];
                }
            }
        }

        Debug.LogWarning(Tag + " không có sorting layer nào trong [" + string.Join(", ", uuTien) +
                         "] — rơi về \"Default\", cô gái có thể bị nhà che.");
        return "Default";
    }

    private static bool EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder)) return false;
        if (AssetDatabase.IsValidFolder(folder)) return true;

        string parent = Path.GetDirectoryName(folder);
        if (parent != null) parent = parent.Replace('\\', '/');
        string leaf = Path.GetFileName(folder);

        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return false;
        if (!EnsureFolder(parent)) return false;

        string guid = AssetDatabase.CreateFolder(parent, leaf);
        return !string.IsNullOrEmpty(guid) && AssetDatabase.IsValidFolder(folder);
    }
}
