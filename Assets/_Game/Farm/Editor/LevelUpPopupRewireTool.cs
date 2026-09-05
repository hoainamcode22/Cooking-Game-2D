using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;   // [B2] tạo/nối Text_MoKhoa, Text_Hint

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
    public static void DryRun() { Chay(false); }

    [MenuItem(MENU_APPLY, false, 61)]
    public static void Apply() { Chay(true); }

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
            if (pBlink != null)
            {
                if (blink != null) pBlink.objectReferenceValue = blink;
                else if (!dungBlink && pBlink.objectReferenceValue != null)
                {
                    // [B2 — 2026-09-05] Bảng nói KHÔNG dùng blink nhưng slot còn giữ blink CŨ
                    // (từ lần rewire trước / tool khác) → xoá, kẻo nhân vật chớp mắt sai ảnh.
                    bc.AppendLine($"║    + [{i}] xoá blinkSprite cũ '{pBlink.objectReferenceValue.name}' (bảng: dungBlink=false)");
                    pBlink.objectReferenceValue = null;
                    soSua++;
                }
            }

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
        // ══ ④ [B2 — 2026-09-05] NỀN MỜ · NÚT TIẾP TỤC · DÂY CHỮ MỞ KHOÁ / HINT ═══
        // Phục hồi những gì MasterTutorialBeautifier từng ép sai (dim alpha 0, nút builtin
        // phẳng) và nối 2 field chữ mà scene cũ chưa có. Mọi ghi đều qua Undo.
        bc.AppendLine("║");
        bc.AppendLine("║ ── ④ Nền mờ · nút Tiếp tục · dây chữ hint (hintText) · [V6] bố trí vùng đáy");

        Transform rootHienThi = popup.transform.Find("Root_HienThi");
        Transform contentTf   = popup.transform.Find("Root_HienThi/Content");

        // ④a · Bg_NenToi alpha 0.65
        var imgDim = rootHienThi != null ? rootHienThi.Find("Bg_NenToi")?.GetComponent<Image>() : null;
        if (imgDim == null)
        {
            soLoi++;
            bc.AppendLine("║    ✖ không thấy Root_HienThi/Bg_NenToi (Image) — bỏ qua nền mờ");
        }
        else if (Mathf.Abs(imgDim.color.a - 0.65f) > 0.01f)
        {
            soSua++;
            bc.AppendLine($"║    ✔ Bg_NenToi alpha {imgDim.color.a:0.00} → 0.65");
            if (ghiThat)
            {
                Undo.RecordObject(imgDim, "Bg_NenToi alpha 0.65");
                var c = imgDim.color; c.a = 0.65f; imgDim.color = c;
                EditorUtility.SetDirty(imgDim);
            }
        }
        else bc.AppendLine("║    · Bg_NenToi alpha đã 0.65");

        // ④b · Btn_TiepTuc = UIStandardSprites.BtnGreen, Sliced, trắng
        Transform btnTf = contentTf != null ? contentTf.Find("Btn_TiepTuc") : null;
        if (btnTf == null)
            btnTf = popup.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Btn_TiepTuc");
        var imgBtn   = btnTf != null ? btnTf.GetComponent<Image>() : null;
        var sprGreen = UIStandardSprites.BtnGreen;
        if (imgBtn == null)      { soLoi++; bc.AppendLine("║    ✖ không thấy Btn_TiepTuc (Image) — bỏ qua nút"); }
        else if (sprGreen == null) { soLoi++; bc.AppendLine("║    ✖ UIStandardSprites.BtnGreen không load được — kiểm tra registry"); }
        else if (imgBtn.sprite != sprGreen || imgBtn.type != Image.Type.Sliced || imgBtn.color != Color.white)
        {
            soSua++;
            bc.AppendLine($"║    ✔ Btn_TiepTuc: sprite '{(imgBtn.sprite ? imgBtn.sprite.name : "NULL")}' → '{sprGreen.name}', Sliced, màu trắng");
            if (ghiThat)
            {
                Undo.RecordObject(imgBtn, "Btn_TiepTuc = BtnGreen");
                imgBtn.sprite = sprGreen;
                imgBtn.type   = Image.Type.Sliced;
                imgBtn.color  = Color.white;
                EditorUtility.SetDirty(imgBtn);
            }
        }
        else bc.AppendLine("║    · Btn_TiepTuc đã đúng (BtnGreen · Sliced · trắng)");

        // ④c · Dây chữ hint — tạo TMP dưới Content nếu chưa có (runtime BoTriVungDuoi tự bật + đặt chữ).
        // [V6 2026-09-05] Toạ độ cũ (neo đáy Content, y=165) tính sai: nút Tiếp tục neo TÂM Content
        // (y=-462 → theo đáy là 46..164) nên Text_Hint nằm NGAY TRÊN nút. Nay neo TÂM Content,
        // y=-385, 1100×66 (khe 12px dưới khung trắng -340..-90, khe 23px trên nút -559..-441).
        // Text_MoKhoa (unlockDescText) KHÔNG tạo nữa: dải icon + tag MỚI đã thay nó; field vẫn
        // giữ trong script, object cũ (nếu còn) để nguyên, runtime luôn SetActive(false).
        NoiDayChu(soPopup, popup, contentTf, "hintText", "Text_Hint",
                  new Vector2(0.5f, 0.5f), new Vector2(0f, -385f), new Vector2(1100f, 66f), 28,
                  bc, ref soSua, ref soLoi, ghiThat);

        // ④d · [V6 2026-09-05] BỐ TRÍ VÙNG ĐÁY — khớp LevelUpPopupUI.BoTriVungDuoi() để scene
        // lưu ra đúng số runtime sẽ ép (mở scene không thấy lệch, Inspector không "nhảy" khi Play).
        bc.AppendLine("║");
        bc.AppendLine("║ ── ④d Bố trí vùng đáy (Dai_MoKhoa -215 · Nen_Dai kem · Btn -500 · tắt V3_DimBackground)");
        BoTriVungDuoiScene(popup, contentTf, bc, ref soSua, ref soLoi, ghiThat);

        bc.AppendLine("║");
        bc.AppendLine($"║ TỔNG: {soSua} chỗ cần sửa · {soLoi} lỗi");

        if (!ghiThat)
        {
            bc.AppendLine("║ ⓘ DRY-RUN — CHƯA ghi gì. Sạch rồi thì chạy bản (APPLY).");
            bc.AppendLine("╚════════════════════════════════════════════════════════════");
            Debug.Log(bc.ToString());
            return;
        }

        soPopup.ApplyModifiedProperties();
        EditorUtility.SetDirty(popup);
        EditorSceneManager.MarkSceneDirty(popup.gameObject.scene);

        bc.AppendLine("║ ✔ ĐÃ APPLY THÀNH CÔNG — nhớ Ctrl+S lưu scene.");
        bc.AppendLine("╚════════════════════════════════════════════════════════════");
        Debug.Log(bc.ToString());
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// [V6 2026-09-05] Tạo/nối 1 TMP dưới Content. Nhận đủ anchor + vị trí + size (bản cũ
    /// cố định neo đáy 900×40 và CHỈ sửa khi anchoredPosition lệch → anchor/size trôi không
    /// bao giờ được kéo về). Nay so cả anchor / pivot / sizeDelta / cha, lệch chỗ nào sửa chỗ đó.
    /// </summary>
    private static void NoiDayChu(
        SerializedObject soPopup, LevelUpPopupUI popup, Transform content,
        string fieldName, string tenObj, Vector2 anchor, Vector2 viTri, Vector2 kichThuoc, int fontSize,
        StringBuilder bc, ref int soSua, ref int soLoi, bool ghiThat)
    {
        var prop = soPopup.FindProperty(fieldName);
        if (prop == null) { soLoi++; bc.AppendLine($"║    ✖ không có field '{fieldName}' — code đã đổi tên?"); return; }

        Transform t = content != null ? content.Find(tenObj) : null;
        if (t == null) t = popup.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == tenObj);
        var tmp = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        var pivot = new Vector2(0.5f, 0.5f);

        if (tmp != null)
        {
            RectTransform existingRt = tmp.rectTransform;
            bool saiCha  = content != null && existingRt.parent != content;
            bool saiRect = existingRt.anchorMin != anchor || existingRt.anchorMax != anchor
                        || existingRt.pivot != pivot
                        || existingRt.anchoredPosition != viTri
                        || existingRt.sizeDelta != kichThuoc;
            if (saiCha || saiRect)
            {
                soSua++;
                bc.AppendLine($"║    ✔ căn lại '{tenObj}': anchor {existingRt.anchorMin}→{anchor}, pos {existingRt.anchoredPosition}→{viTri}, " +
                              $"size {existingRt.sizeDelta}→{kichThuoc}{(saiCha ? " · đưa về Content" : "")}");
                if (ghiThat)
                {
                    if (saiCha) Undo.SetTransformParent(existingRt, content, "Đưa " + tenObj + " về Content");
                    Undo.RecordObject(existingRt, "Cập nhật toạ độ " + tenObj);
                    existingRt.anchorMin = existingRt.anchorMax = anchor;
                    existingRt.pivot = pivot;
                    existingRt.anchoredPosition = viTri;
                    existingRt.sizeDelta = kichThuoc;
                    existingRt.localScale = Vector3.one;
                    EditorUtility.SetDirty(existingRt);
                }
            }
            else bc.AppendLine($"║    · '{tenObj}' đã đúng chỗ (anchor {anchor}, y={viTri.y}, {kichThuoc.x}×{kichThuoc.y})");

            if (prop.objectReferenceValue != tmp)
            {
                soSua++;
                bc.AppendLine($"║    ✔ nối {fieldName} → '{tenObj}'");
                prop.objectReferenceValue = tmp;
            }
            return;
        }

        soSua++;
        bc.AppendLine($"║    ✔ tạo TMP '{tenObj}' dưới Content (anchor {anchor}, y={viTri.y}, {kichThuoc.x}×{kichThuoc.y}) rồi nối {fieldName}" +
                      (content == null ? " — ✖ KHÔNG có Root_HienThi/Content để tạo!" : ""));
        if (!ghiThat) return;
        if (content == null) { soLoi++; return; }

        var go = new GameObject(tenObj, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Tạo " + tenObj);
        var rt = (RectTransform)go.transform;
        rt.SetParent(content, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot     = pivot;
        rt.anchoredPosition = viTri;
        rt.sizeDelta = kichThuoc;

        // Đứng TRƯỚC Btn_TiepTuc trong sibling order → không bao giờ vẽ đè lên nút.
        var btn = content.Find("Btn_TiepTuc");
        if (btn != null) rt.SetSiblingIndex(btn.GetSiblingIndex());

        tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text              = "";
        tmp.fontSize          = fontSize;
        tmp.enableAutoSizing  = true;
        tmp.fontSizeMin       = 20;
        tmp.fontSizeMax       = fontSize;
        tmp.maxVisibleLines   = 2;
        tmp.alignment         = TextAlignmentOptions.Center;
        tmp.color             = new Color32(255, 245, 220, 255);
        tmp.textWrappingMode  = TextWrappingModes.Normal;
        tmp.overflowMode      = TextOverflowModes.Ellipsis;
        tmp.raycastTarget     = false;
        go.SetActive(false);   // LevelUpPopupUI.BoTriVungDuoi tự bật + đặt chữ khi mở popup

        prop.objectReferenceValue = tmp;
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// [V6 2026-09-05] Ghi bố cục vùng đáy vào scene (Undo được) — cùng số với
    /// LevelUpPopupUI.BoTriVungDuoi(): Dai_MoKhoa y=-215, Btn_TiepTuc y=-500,
    /// Nen_Dai (255,243,214,235), tắt V3_DimBackground (lớp mờ trùng với Bg_NenToi).
    /// Thiếu object nào thì báo và bỏ qua mục đó, không dừng tool.
    /// </summary>
    private static void BoTriVungDuoiScene(LevelUpPopupUI popup, Transform content,
                                           StringBuilder bc, ref int soSua, ref int soLoi, bool ghiThat)
    {
        // Local function KHÔNG được bắt tham số ref (CS1628) → đếm vào biến cục bộ rồi cộng dồn.
        int suaCucBo = 0, loiCucBo = 0;

        Transform Tim(string ten)
        {
            Transform t = content != null ? content.Find(ten) : null;
            return t ?? popup.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == ten);
        }

        void EpY(string ten, float y)
        {
            var rt = Tim(ten) as RectTransform;
            if (rt == null) { loiCucBo++; bc.AppendLine($"║    ✖ không thấy '{ten}' — bỏ qua"); return; }
            if (Mathf.Approximately(rt.anchoredPosition.y, y)) { bc.AppendLine($"║    · {ten} đã ở y={y}"); return; }
            suaCucBo++;
            bc.AppendLine($"║    ✔ {ten} y {rt.anchoredPosition.y} → {y}");
            if (!ghiThat) return;
            Undo.RecordObject(rt, $"{ten} y={y}");
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            EditorUtility.SetDirty(rt);
        }

        EpY("Dai_MoKhoa",  -215f);
        EpY("Btn_TiepTuc", -500f);
        soSua += suaCucBo;
        soLoi += loiCucBo;

        // Nen_Dai — kem ấm
        Color kem = new Color32(255, 243, 214, 235);
        var nenDai = Tim("Nen_Dai")?.GetComponent<Image>();
        if (nenDai == null) { soLoi++; bc.AppendLine("║    ✖ không thấy Nen_Dai (Image) — bỏ qua"); }
        else if (nenDai.color != kem)
        {
            soSua++;
            bc.AppendLine($"║    ✔ Nen_Dai màu {nenDai.color} → (255,243,214,235)");
            if (ghiThat)
            {
                Undo.RecordObject(nenDai, "Nen_Dai màu kem");
                nenDai.color = kem;
                EditorUtility.SetDirty(nenDai);
            }
        }
        else bc.AppendLine("║    · Nen_Dai đã màu kem");

        // V3_DimBackground — lớp mờ thứ 2 (0.62) chồng lên Bg_NenToi (0.65) → nền tối ~87%. Tắt.
        var dimV3 = Tim("V3_DimBackground");
        if (dimV3 == null) bc.AppendLine("║    · không có V3_DimBackground (tốt)");
        else if (dimV3.gameObject.activeSelf)
        {
            soSua++;
            bc.AppendLine("║    ✔ tắt V3_DimBackground (lớp mờ trùng, chỉ giữ Bg_NenToi 0.65)");
            if (ghiThat)
            {
                Undo.RecordObject(dimV3.gameObject, "Tắt V3_DimBackground");
                dimV3.gameObject.SetActive(false);
                EditorUtility.SetDirty(dimV3.gameObject);
            }
        }
        else bc.AppendLine("║    · V3_DimBackground đã tắt");
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
