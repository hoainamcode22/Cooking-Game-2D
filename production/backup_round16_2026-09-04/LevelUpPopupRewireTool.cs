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

        for (int i = 0; i < Mathf.Min(SLOT_COUNT, slotList.Length); i++)
        {
            string charId = $"char_{(i + 1):00}";
            Sprite master = TaiSprite($"{CHAR_ROOT}/{charId}/{charId}_master.png")
                         ?? TaiSprite($"{CHAR_ROOT}/{charId}/{charId}_f01.png");
            Sprite blink  = TaiSprite($"{CHAR_ROOT}/{charId}/{charId}_blink.png");

            if (master == null)
            {
                soLoi++;
                bc.AppendLine($"║    ✖ [{i}] {charId}: KHÔNG thấy {charId}_master.png lẫn {charId}_f01.png trong {CHAR_ROOT}/{charId}/");
                continue;
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
}
