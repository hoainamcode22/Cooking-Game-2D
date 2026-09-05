using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NỐI LẠI DÂY POPUP LÊN CẤP — 1 nút, sửa đúng 3 chỗ đứt tìm được ngày 2026-09-04.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// 3 LỖI ĐÃ ĐO TRONG SCN_Farm.unity (không suy đoán, đọc thẳng từ file scene):
///   ① LevelUpPopupUI.celebrationSlots = [{fileID:0} ×4]  ⇒ popup KHÔNG cầm được slot nào,
///     nên mọi tool art chạy đều thoát sớm ở nhánh "chưa có celebrationSlots".
///     Trong khi 4 GameObject V2_CharSlot_01..04 VẪN sống và VẪN có component.
///   ② V2_CharSlot_03 / 04 . puppetMaster trỏ vào guid LẠ, không phải
///     char_03_master.png / char_04_master.png ⇒ 2 nhân vật bên phải sai hình.
///   ③ LevelUpPopupUI.fireworkSprites = [] ⇒ pháo hoa rơi vào fallback
///     Resources.GetBuiltinResource("UI/Skin/UISprite.psd") = KHỐI MÀU PHẲNG, không phải art.
///
/// TOOL NÀY KHÔNG ĐỘNG VÀO: layout/anchor (tool ★ HOÀN THIỆN NHÂN VẬT lo), logic popup,
/// hay bất kỳ field nào ngoài 4 field kể trên. Không tự lưu scene (luật studio).
///
/// CHẾ ĐỘ PUPPET (Sếp chốt 2026-09-04): giữ 1 sprite master + code tự diễn
/// (thở / nghiêng / nảy / chớp mắt) — nhẹ máy. Nên tool XOÁ mảng `frames` để ép về
/// đúng nhánh puppet, KHÔNG import sprite-sheet 12 frame.
///
/// [LevelUp]
/// </summary>
public static class LevelUpPopupRewireTool
{
    private const string MENU_DRY   = "Tools/Farm Game/Level Up Popup/★ Nối lại dây popup (DRY-RUN)";
    private const string MENU_APPLY = "Tools/Farm Game/Level Up Popup/★ Nối lại dây popup (APPLY)";

    private const string CHAR_ROOT      = "Assets/Art/UI/LevelUpV2/characters";
    private const string FIREWORK_ROOT  = "Assets/Art/UI/LevelUpV2/fireworks";
    private const int    SLOT_COUNT     = 4;

    // ═══════════════════════════════════════════════════════════════════════
    // [VÒNG 16] BẢNG NHÂN VẬT — ĐÃ SOI TỪNG FRAME BẰNG MẮT, ĐỪNG ĐOÁN LẠI
    // ───────────────────────────────────────────────────────────────────────
    // Thư mục characters/ đang TRỘN 2 BỘ ART khác hẳn nhau:
    //
    //   · char_0X_master.png  +  char_0X_f01.png
    //       char_01 = ÔNG LÃO TOÀN THÂN giơ hai tay      ← bộ khác
    //       char_03 = avatar TRONG KHUNG TRÒN viền vàng  ← bộ khác
    //       char_04 = cao bồi TRONG KHUNG TRÒN           ← bộ khác
    //
    //   · char_0X_f02 … f12
    //       char_01 = ông thám hiểm râu, mũ be, áo khaki — BÁN THÂN ✔
    //       char_03 = cô mũ pith ô-liu nhạt, tóc xoăn    — BÁN THÂN ✔
    //       char_04 = cô mũ pith ô-liu đậm               — BÁN THÂN ✔
    //       char_02 = cô đầu bếp nhưng MŨ BỊ VỠ răng cưa — HỎNG ✖
    //
    // Sếp chốt 04/09: popup chỉ dùng bộ BÁN THÂN (đúng như đang thấy trong game).
    // ⇒ Hình đại diện phải lấy f05 (frame giữa, tư thế trung tính),
    //   TUYỆT ĐỐI KHÔNG lấy _master.png — lấy nhầm là popup hiện avatar khung tròn.
    //
    // Slot nào chưa có file art thì tự ẩn, không báo lỗi.
    //
    // [VÒNG 16 — Sếp chốt 04/09] KHÔNG đặt đội vẽ nhân vật mới nữa.
    // 4 nhân vật đều LẤY TỪ BỘ KHÁCH DU LỊCH có sẵn: Assets/NV_NPC/NVGAME/
    // (11 người, cùng bộ với 11 prefab Assets/_Game/Farm/Prefabs/Tourists/Tourist_NV01..11).
    // char_05 / char_06 do Lead tự cắt bán thân từ ảnh gốc 1664×2562, không phóng to bừa.
    private static readonly (string charId, bool dungBlink)[] BANG_NHAN_VAT =
    {
        ("char_01", false),   // = NV01 ông thám hiểm râu · blink lạc bộ (cậu bé áo caro) nên bỏ
        ("char_03", true ),   // = NV03 cô mũ pith tóc xoăn · blink cùng bộ ✔
        ("char_05", false),   // = NV09 cô cầm kính lúp   · 1 hình puppet, chưa có blink
        ("char_06", false),   // = NV06 ông lão kính tròn · 1 hình puppet, chưa có blink
    };
    // char_04 (cô mũ pith thứ hai) vẫn còn nguyên trong Assets — nếu Sếp muốn dùng lại,
    // đổi 1 dòng ở trên là xong. Hiện bỏ ra vì nhìn gần trùng char_03.
    // char_02 bị loại vì mũ đầu bếp VỠ răng cưa ở toàn bộ f02–f12.

    private static readonly string[] FIREWORK_FILES =
    {
        "confetti_01.png", "confetti_02.png", "confetti_03.png",
        "confetti_04.png", "confetti_05.png", "confetti_06.png",
        "spark_star.png",
    };

    [MenuItem(MENU_DRY, false, 60)]
    private static void DryRun() { Chay(false); }

    [MenuItem(MENU_APPLY, false, 61)]
    private static void Apply() { Chay(true); }

    // ─────────────────────────────────────────────────────────────────────────
    private static void Chay(bool ghiThat)
    {
        var bc = new StringBuilder();
        bc.AppendLine($"╔══ [LevelUpRewire] {(ghiThat ? "APPLY" : "DRY-RUN")} — nối lại dây popup lên cấp ══");

        // ── Tìm popup trong scene đang mở (kể cả khi object đang tắt) ────────
        var popup = Object.FindObjectsByType<LevelUpPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                          .FirstOrDefault();
        if (popup == null)
        {
            Debug.LogError("[LevelUpRewire] KHÔNG thấy LevelUpPopupUI trong scene đang mở. " +
                           "Mở SCN_Farm.unity rồi chạy lại. Chưa đụng gì cả.");
            return;
        }
        bc.AppendLine($"║ Popup: {LayDuongDan(popup.transform)}");

        var soPopup = new SerializedObject(popup);
        int soSua = 0, soLoi = 0;

        // ══ ① NỐI 4 SLOT NHÂN VẬT ═══════════════════════════════════════════
        var pSlots = soPopup.FindProperty("celebrationSlots");
        if (pSlots == null)
        {
            soLoi++;
            bc.AppendLine("║ ✖ Không tìm thấy field 'celebrationSlots' — code đã đổi tên? Dừng phần ①.");
        }
        else
        {
            // Slot con thật nằm dưới popup, tìm theo component chứ không theo tên
            // (đổi tên GameObject cũng không làm hỏng tool).
            var slotsTrongScene = popup.GetComponentsInChildren<CelebrationCharacterSlot>(true)
                                       .OrderBy(s => s.name, System.StringComparer.Ordinal)
                                       .ToArray();

            bc.AppendLine($"║");
            bc.AppendLine($"║ ── ① Slot nhân vật: tìm thấy {slotsTrongScene.Length} component trong hierarchy");

            if (slotsTrongScene.Length < SLOT_COUNT)
            {
                soLoi++;
                bc.AppendLine($"║    ✖ Chỉ có {slotsTrongScene.Length}/{SLOT_COUNT} slot ⇒ chạy " +
                              "'★ Nâng cấp V2 (1 nút)' để dựng đủ 4 slot trước.");
            }
            else
            {
                if (pSlots.arraySize != SLOT_COUNT) pSlots.arraySize = SLOT_COUNT;

                for (int i = 0; i < SLOT_COUNT; i++)
                {
                    var el  = pSlots.GetArrayElementAtIndex(i);
                    var cu  = el.objectReferenceValue as CelebrationCharacterSlot;
                    var moi = slotsTrongScene[i];

                    if (cu == moi)
                    {
                        bc.AppendLine($"║    · [{i}] {moi.name} — đã đúng, bỏ qua");
                        continue;
                    }

                    bc.AppendLine($"║    ✔ [{i}] {(cu == null ? "NULL" : cu.name)}  →  {moi.name}");
                    el.objectReferenceValue = moi;
                    soSua++;
                }
            }
        }

        // ══ ② GẮN ĐÚNG SPRITE MASTER + BLINK CHO TỪNG SLOT ══════════════════
        bc.AppendLine("║");
        bc.AppendLine("║ ── ② Sprite nhân vật (chế độ PUPPET — 1 hình master)");

        var slotList = popup.GetComponentsInChildren<CelebrationCharacterSlot>(true)
                            .OrderBy(s => s.name, System.StringComparer.Ordinal)
                            .ToArray();

        for (int i = 0; i < Mathf.Min(BANG_NHAN_VAT.Length, slotList.Length); i++)
        {
            // [VÒNG 16] Lấy theo BẢNG_NHAN_VAT, KHÔNG suy ra từ chỉ số slot nữa.
            string charId    = BANG_NHAN_VAT[i].charId;
            bool   dungBlink = BANG_NHAN_VAT[i].dungBlink;

            // f05 = frame giữa của bộ BÁN THÂN. Không dùng _master.png (xem chú thích trên).
            Sprite master = TaiSprite($"{CHAR_ROOT}/{charId}/{charId}_f05.png")
                         ?? TaiSprite($"{CHAR_ROOT}/{charId}/{charId}_f06.png");
            Sprite blink  = dungBlink ? TaiSprite($"{CHAR_ROOT}/{charId}/{charId}_blink.png") : null;

            if (master == null)
            {
                // Chưa có art ⇒ báo nhẹ rồi ẩn slot, KHÔNG tính là lỗi.
                bc.AppendLine($"║    · [{i}] {charId}: chưa có art (đội vẽ chưa giao) → ẩn slot, chờ");
                if (ghiThat && slotList[i].gameObject.activeSelf)
                {
                    Undo.RecordObject(slotList[i].gameObject, "Ẩn slot nhân vật chưa có art");
                    slotList[i].gameObject.SetActive(false);
                }
                continue;
            }
            if (ghiThat && !slotList[i].gameObject.activeSelf)
            {
                Undo.RecordObject(slotList[i].gameObject, "Bật lại slot nhân vật");
                slotList[i].gameObject.SetActive(true);
            }

            var so = new SerializedObject(slotList[i]);
            var pMaster = so.FindProperty("puppetMaster");
            var pBlink  = so.FindProperty("blinkSprite");
            var pFrames = so.FindProperty("frames");
            var pImage  = so.FindProperty("targetImage");

            var cuMaster = pMaster != null ? pMaster.objectReferenceValue as Sprite : null;
            bool doi = cuMaster != master;

            if (pMaster != null) pMaster.objectReferenceValue = master;
            if (pBlink  != null && blink != null) pBlink.objectReferenceValue = blink;

            // Ép về nhánh PUPPET: có frames thì code chạy sprite-sheet, không diễn puppet.
            int framesCu = pFrames != null ? pFrames.arraySize : 0;
            if (pFrames != null && pFrames.arraySize > 0) pFrames.arraySize = 0;

            // Tự vá targetImage nếu ai đó lỡ bỏ trống.
            if (pImage != null && pImage.objectReferenceValue == null)
            {
                var img = slotList[i].GetComponent<Image>() ?? slotList[i].GetComponentInChildren<Image>(true);
                if (img != null) { pImage.objectReferenceValue = img; bc.AppendLine($"║    + [{i}] vá targetImage = {img.name}"); }
            }

            EpChuanSpriteUI(AssetDatabase.GetAssetPath(master), ghiThat);
            if (ghiThat) so.ApplyModifiedProperties();

            string ghiChu = doi ? $"SỬA  {(cuMaster == null ? "NULL" : cuMaster.name)} → {master.name}" : $"đã đúng ({master.name})";
            if (doi) soSua++;
            bc.AppendLine($"║    {(doi ? "✔" : "·")} [{i}] {slotList[i].name}: {ghiChu}" +
                          $"{(blink != null ? " · có blink" : " · KHÔNG có blink (bỏ qua, không sao)")}" +
                          $"{(framesCu > 0 ? $" · xoá {framesCu} frame để về puppet" : "")}");
        }

        // ══ ③ GẮN SPRITE PHÁO HOA THẬT ═════════════════════════════════════
        bc.AppendLine("║");
        bc.AppendLine("║ ── ③ Pháo hoa trên mặt UI");

        var pFw  = soPopup.FindProperty("fireworkSprites");
        var pUse = soPopup.FindProperty("useUIFireworks");

        var fwSprites = new List<Sprite>();
        var thieu     = new List<string>();
        foreach (var f in FIREWORK_FILES)
        {
            var s = TaiSprite($"{FIREWORK_ROOT}/{f}");
            if (s != null) fwSprites.Add(s); else thieu.Add(f);
        }

        if (thieu.Count > 0)
            bc.AppendLine($"║    ⚠ Thiếu {thieu.Count} file: {string.Join(", ", thieu)} — " +
                          $"kiểm lại {FIREWORK_ROOT}/ (Unity đã import chưa?)");

        if (pFw == null)
        {
            soLoi++;
            bc.AppendLine("║    ✖ Không tìm thấy field 'fireworkSprites'.");
        }
        else if (fwSprites.Count == 0)
        {
            soLoi++;
            bc.AppendLine($"║    ✖ 0 sprite pháo hoa đọc được ⇒ GIỮ NGUYÊN khối màu tạm, không ghi gì.");
        }
        else
        {
            bc.AppendLine($"║    ✔ {fwSprites.Count} sprite: {string.Join(", ", fwSprites.Select(s => s.name))}");
            pFw.arraySize = fwSprites.Count;
            for (int i = 0; i < fwSprites.Count; i++)
                pFw.GetArrayElementAtIndex(i).objectReferenceValue = fwSprites[i];
            soSua++;
        }

        if (pUse != null && !pUse.boolValue)
        {
            bc.AppendLine("║    ✔ useUIFireworks: FALSE → TRUE (bắt buộc, để pháo hoa nổ TRÊN mặt popup)");
            pUse.boolValue = true;
            soSua++;
        }
        else if (pUse != null)
        {
            bc.AppendLine("║    · useUIFireworks đã TRUE");
        }

        // [V3 04/09] Ép pháo hoa lên LỚP PHỦ TOÀN KHUNG (con trực tiếp popupRoot) thay vì
        // nằm trong contentPanel — đúng thứ Sếp báo "nó nằm sau lớp kia, bị che phủ".
        var pTop = soPopup.FindProperty("fireworksOnTopLayer");
        if (pTop == null)
        {
            bc.AppendLine("║    ⚠ Không thấy field 'fireworksOnTopLayer' — Unity chưa biên dịch lại LevelUpPopupUI.cs? " +
                          "Bấm Ctrl+R rồi chạy lại tool.");
        }
        else if (!pTop.boolValue)
        {
            bc.AppendLine("║    ✔ fireworksOnTopLayer: FALSE → TRUE (pháo hoa nổ phủ TOÀN khung, trên cả nền mờ lẫn card)");
            pTop.boolValue = true;
            soSua++;
        }
        else
        {
            bc.AppendLine("║    · fireworksOnTopLayer đã TRUE — pháo hoa bắn ở lớp FX_Fireworks_Layer phủ toàn khung ⇒ không bị card che");
        }

        var pSpread = soPopup.FindProperty("fireworkSpreadBoost");
        if (pSpread != null)
            bc.AppendLine($"║    · fireworkSpreadBoost = {pSpread.floatValue:0.00} (thấy hạt bay quá đà thì hạ về 1.20 trên Inspector)");

        // ── Kết ─────────────────────────────────────────────────────────────
        bc.AppendLine("║");
        bc.AppendLine($"║ TỔNG: {soSua} chỗ cần sửa · {soLoi} lỗi");

        if (!ghiThat)
        {
            bc.AppendLine("║ ⓘ DRY-RUN — CHƯA ghi gì. Sạch rồi thì chạy bản (APPLY).");
            bc.AppendLine("╚════════════════════════════════════════════════════════════");
            Debug.Log(bc.ToString());
            return;
        }

        Undo.RecordObject(popup, "Nối lại dây popup lên cấp");
        soPopup.ApplyModifiedProperties();
        EditorUtility.SetDirty(popup);
        foreach (var s in slotList.Take(SLOT_COUNT)) EditorUtility.SetDirty(s);
        EditorSceneManager.MarkSceneDirty(popup.gameObject.scene);

        bc.AppendLine("║ ✅ ĐÃ GHI vào scene (chưa lưu).");
        bc.AppendLine("║ 🔴 SẾP PHẢI BẤM Ctrl+S ĐỂ LƯU SCENE — tool cố ý KHÔNG tự lưu (luật studio).");
        bc.AppendLine("║ ⓘ Lỡ tay: Ctrl+Z rồi ĐỪNG lưu, scene về nguyên trạng.");
        bc.AppendLine("╚════════════════════════════════════════════════════════════");
        Debug.Log(bc.ToString());
    }

    // ─────────────────────────────────────────────────────────────────────────
    private static Sprite TaiSprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault()
            ?? AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static string LayDuongDan(Transform t)
    {
        var sb = new StringBuilder(t.name);
        while (t.parent != null) { t = t.parent; sb.Insert(0, t.name + "/"); }
        return sb.ToString();
    }

    /// <summary>[VÒNG 16] Ép PNG về đúng chuẩn Sprite UI. Ảnh mới copy tay vào Assets
    /// hay bị Unity import mặc định thành Texture (không ra Sprite) ⇒ slot hiện trống.</summary>
    private static void EpChuanSpriteUI(string duongDan, bool ghiThat)
    {
        if (string.IsNullOrEmpty(duongDan)) return;
        var ti = AssetImporter.GetAtPath(duongDan) as TextureImporter;
        if (ti == null) return;

        bool doi = false;
        if (ti.textureType        != TextureImporterType.Sprite) { ti.textureType        = TextureImporterType.Sprite; doi = true; }
        if (ti.spriteImportMode   != SpriteImportMode.Single)    { ti.spriteImportMode   = SpriteImportMode.Single;    doi = true; }
        if (!ti.alphaIsTransparency)                             { ti.alphaIsTransparency = true;                      doi = true; }
        if (ti.mipmapEnabled)                                    { ti.mipmapEnabled      = false;                      doi = true; }
        if (ti.maxTextureSize < 1024)                            { ti.maxTextureSize     = 1024;                       doi = true; }
        if (ti.textureCompression != TextureImporterCompression.Uncompressed)
        {                                                          ti.textureCompression = TextureImporterCompression.Uncompressed; doi = true; }

        if (doi && ghiThat) ti.SaveAndReimport();
    }
}
