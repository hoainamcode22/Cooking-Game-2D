using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TOOL DỰNG & NẠP ART CHO TUTORIAL V2 — 2 nút, Sếp không phải kéo tay gì.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// NÚT 1 — "Dựng card hội thoại V2": tạo nguyên cụm card + NPC dưới Tutorial_Canvas,
///          gán sẵn khung 9-slice panel_paper_cream, font Baloo2, nút Tiếp tục, và nối
///          hết reference của TutorialDialogueCard / TutorialNpcActor / TutorialVfxDirector.
///          Chạy được NGAY cả khi CHƯA có art NPC (placeholder ô vuông mờ).
///
/// NÚT 2 — "Nạp art từ art-handoff": đội vẽ thả file vào
///          production/art-handoff/2026-09-04_TutorialV2/{A_NPC_Guide,B_VFX_Tutorial}/
///          → tool copy vào Assets/Art/UI/TutorialV2/, set import chuẩn (Sprite, pivot,
///          không nén mờ), rồi GÁN THẲNG vào component. Đúng nghĩa "bỏ vào folder là xong".
///
/// AN TOÀN:
///   • KHÔNG xoá `NPC_Dialog_Popup` cũ — tutorial cũ còn nguyên để đối chiếu/lùi về.
///   • KHÔNG tự lưu scene (luật studio) — Sếp bấm Ctrl+S.
///   • Chạy lại nhiều lần vẫn ra một kết quả (idempotent), không nhân bản object.
///
/// [TutorialV2]
/// </summary>
public static class TutorialV2SetupTool
{
    private const string MENU_BUILD = "Tools/Farm Game/Tutorial V2/★ Dựng card hội thoại V2 (1 nút)";
    private const string MENU_ART   = "Tools/Farm Game/Tutorial V2/★ Nạp art NPC + VFX từ art-handoff (1 nút)";
    private const string MENU_CHECK = "Tools/Farm Game/Tutorial V2/Kiểm tra sẵn sàng (chỉ đọc)";

    private const string ROOT_NAME   = "TutorialV2_Dialogue";
    private const string CANVAS_NAME = "Tutorial_Canvas";

    private const string CARD_SPRITE = "Assets/Export_Kitchen_UI_Package/Sprites/panel_paper_cream.png";
    private const string BTN_SPRITE  = "Assets/Export_Kitchen_UI_Package/Sprites/btn_paper_small.png";
    private const string FONT_ASSET  = "Assets/_Game/Resources/Fonts/Baloo2 SDF.asset";

    private const string ART_DEST_NPC = "Assets/Art/UI/TutorialV2/npc";
    private const string ART_DEST_VFX = "Assets/Art/UI/TutorialV2/vfx";
    private const string HANDOFF_NPC  = "production/art-handoff/2026-09-04_TutorialV2/A_NPC_Guide";
    private const string HANDOFF_VFX  = "production/art-handoff/2026-09-04_TutorialV2/B_VFX_Tutorial";

    // ═══════════════════════════════════════════════════════════════════════
    // NÚT 1 — DỰNG
    // ═══════════════════════════════════════════════════════════════════════
    [MenuItem(MENU_BUILD, false, 10)]
    private static void Dung()
    {
        var bc = new StringBuilder();
        bc.AppendLine("╔══ [TutorialV2] DỰNG CARD HỘI THOẠI ══");

        Transform canvas = TimHoacTaoCanvas(bc);
        if (canvas == null) { Debug.LogError(bc.ToString()); return; }

        // ── Root ────────────────────────────────────────────────────────────
        Transform cu = canvas.Find(ROOT_NAME);
        GameObject root = cu != null ? cu.gameObject : null;
        bool taoMoi = root == null;

        if (taoMoi)
        {
            root = new GameObject(ROOT_NAME, typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(canvas, false);
            Undo.RegisterCreatedObjectUndo(root, "Dựng card hội thoại V2");
            bc.AppendLine($"║ + Tạo mới '{ROOT_NAME}'");
        }
        else bc.AppendLine($"║ · Dùng lại '{ROOT_NAME}' đã có (chạy lại không nhân bản)");

        var rootRt = (RectTransform)root.transform;
        NeoFullMan(rootRt);

        // ── NPC bên TRÁI ────────────────────────────────────────────────────
        var npcGo = LayHoacTao(root.transform, "NPC_Guide", typeof(RectTransform), typeof(Image), typeof(TutorialNpcActor));
        var npcRt = (RectTransform)npcGo.transform;
        npcRt.anchorMin = npcRt.anchorMax = new Vector2(0f, 0f);
        npcRt.pivot = new Vector2(0f, 0f);
        // ⚠️ [VÒNG 14] TRƯỚC ĐÂY (40, 40) ⇒ NPC chiếm y 40→415, trong khi hàng nút
        // CỬA HÀNG/KHO/BẢNG TIN CHỢ/NẤU ĂN (BottomLeft_Nav_Group) chiếm y −74→118.
        // Chồng nhau 78px, lại nằm ở 2 Canvas KHÁC NHAU nên hàng nút vẽ ĐÈ LÊN nửa dưới NPC
        // ⇒ nhìn như NPC bị cắt cụt chân (đúng ảnh Sếp gửi). Thêm nữa y=40 quá sát đáy,
        // máy có thanh cử chỉ sẽ nuốt mất.
        // Nay: thu về 256×320 (giữ đúng tỉ lệ 0.8 của ảnh 512×640) và nâng lên y=210
        // ⇒ NPC chiếm y 210→530, hở hàng nút 92px, chân không còn bị cắt.
        npcRt.sizeDelta = new Vector2(256f, 320f);
        npcRt.anchoredPosition = new Vector2(24f, 210f);

        var npcImg = npcGo.GetComponent<Image>();
        npcImg.raycastTarget = false;
        npcImg.preserveAspect = true;
        if (npcImg.sprite == null) npcImg.color = new Color(1f, 1f, 1f, 0.30f);   // placeholder mờ, thấy được chỗ đứng

        // ── Card ────────────────────────────────────────────────────────────
        var cardGo = LayHoacTao(root.transform, "Card", typeof(RectTransform), typeof(Image), typeof(Button));
        var cardRt = (RectTransform)cardGo.transform;
        cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0f);
        cardRt.pivot = new Vector2(0.5f, 0f);
        cardRt.sizeDelta = new Vector2(880f, 230f);
        // [VÒNG 14] Nâng từ y=60 lên 150: card cũng đang đè lên hàng nút dưới (đỉnh y=118).
        cardRt.anchoredPosition = new Vector2(90f, 150f);   // lệch phải để chừa chỗ NPC

        var cardImg = cardGo.GetComponent<Image>();
        var cardSp  = AssetDatabase.LoadAssetAtPath<Sprite>(CARD_SPRITE);
        if (cardSp != null)
        {
            cardImg.sprite = cardSp;
            cardImg.type   = Image.Type.Sliced;
            cardImg.color  = Color.white;
            bc.AppendLine($"║ ✔ Khung card: {Path.GetFileName(CARD_SPRITE)} (9-slice)");
        }
        else
        {
            cardImg.color = new Color(0.98f, 0.94f, 0.84f, 0.97f);
            bc.AppendLine($"║ ⚠ KHÔNG thấy {CARD_SPRITE} → dùng màu kem phẳng tạm. Kiểm lại đường dẫn.");
        }

        // ── Chữ thoại ───────────────────────────────────────────────────────
        var bodyGo = LayHoacTao(cardGo.transform, "Body", typeof(RectTransform), typeof(TextMeshProUGUI));
        var bodyRt = (RectTransform)bodyGo.transform;
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(46f, 76f);
        bodyRt.offsetMax = new Vector2(-46f, -34f);

        var body = bodyGo.GetComponent<TextMeshProUGUI>();
        body.fontSize = 38f;
        body.color = new Color(0.32f, 0.20f, 0.11f);   // nâu đậm, đọc rõ trên nền kem
        body.alignment = TextAlignmentOptions.TopLeft;
        body.enableWordWrapping = true;
        body.raycastTarget = false;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET);
        if (font != null) body.font = font;
        else bc.AppendLine($"║ ⚠ KHÔNG thấy font {FONT_ASSET} → TMP dùng font mặc định.");
        if (string.IsNullOrEmpty(body.text)) body.text = "Xin chào! Mình sẽ hướng dẫn bạn nhé.";

        // ── Nút Tiếp tục ────────────────────────────────────────────────────
        var btnGo = LayHoacTao(cardGo.transform, "Btn_Continue", typeof(RectTransform), typeof(Image), typeof(Button));
        var btnRt = (RectTransform)btnGo.transform;
        btnRt.anchorMin = btnRt.anchorMax = new Vector2(1f, 0f);
        btnRt.pivot = new Vector2(1f, 0f);
        btnRt.sizeDelta = new Vector2(230f, 68f);
        btnRt.anchoredPosition = new Vector2(-34f, 22f);

        var btnImg = btnGo.GetComponent<Image>();
        var btnSp  = AssetDatabase.LoadAssetAtPath<Sprite>(BTN_SPRITE);
        if (btnSp != null) { btnImg.sprite = btnSp; btnImg.type = Image.Type.Sliced; btnImg.color = Color.white; }
        else btnImg.color = new Color(0.85f, 0.64f, 0.25f);

        var lblGo = LayHoacTao(btnGo.transform, "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var lblRt = (RectTransform)lblGo.transform;
        lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = new Vector2(14f, 0f); lblRt.offsetMax = new Vector2(-40f, 0f);
        var lbl = lblGo.GetComponent<TextMeshProUGUI>();
        lbl.text = "Tiếp tục";
        lbl.fontSize = 32f;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.color = new Color(0.32f, 0.20f, 0.11f);
        lbl.raycastTarget = false;
        if (font != null) lbl.font = font;

        var chevGo = LayHoacTao(btnGo.transform, "Chevron", typeof(RectTransform), typeof(TextMeshProUGUI));
        var chevRt = (RectTransform)chevGo.transform;
        chevRt.anchorMin = chevRt.anchorMax = new Vector2(1f, 0.5f);
        chevRt.pivot = new Vector2(1f, 0.5f);
        chevRt.sizeDelta = new Vector2(34f, 40f);
        chevRt.anchoredPosition = new Vector2(-14f, 0f);
        var chev = chevGo.GetComponent<TextMeshProUGUI>();
        // ⚠️ [VÒNG 14] KHÔNG dùng "▶" (U+25B6). Baloo2 là font Latin/Devanagari, KHÔNG có glyph này;
        // fallback LiberationSans cũng không ⇒ TMP vẽ ô vuông rỗng (tofu) cạnh chữ "Tiếp tục".
        // Dùng "›" (U+203A) — ký tự Latin chuẩn, chắc chắn có trong Baloo2, nhìn vẫn ra mũi tên.
        // Khi art gói mũi tên về, thay TMP này bằng Image + tut_arrow_down xoay 90°.
        chev.text = "›";
        chev.fontSize = 40f;
        chev.alignment = TextAlignmentOptions.Center;
        chev.color = new Color(0.55f, 0.33f, 0.12f);
        chev.raycastTarget = false;
        if (font != null) chev.font = font;

        // ── VFX director ────────────────────────────────────────────────────
        var vfxGo = LayHoacTao(canvas, "TutorialV2_Vfx", typeof(RectTransform), typeof(TutorialVfxDirector));
        NeoFullMan((RectTransform)vfxGo.transform);

        // ── Camera director ─────────────────────────────────────────────────
        // Đặt NGOÀI Canvas: nó điều khiển camera thế giới, không phải UI, và tránh việc
        // Unity tự thêm RectTransform cho con của Canvas.
        var camGo = LayHoacTao(canvas.parent != null ? canvas.parent : canvas,
                               "TutorialV2_Camera", typeof(TutorialCameraDirector));
        bc.AppendLine($"║ ✔ Camera director: {camGo.name}");

        // ── Nối dây bằng SerializedObject (ghi được cả field private) ────────
        var card = root.GetComponent<TutorialDialogueCard>() ?? Undo.AddComponent<TutorialDialogueCard>(root);
        var so = new SerializedObject(card);
        GanRef(so, "root",            root);
        GanRef(so, "canvasGroup",     root.GetComponent<CanvasGroup>());
        GanRef(so, "cardRect",        cardRt);
        GanRef(so, "bodyText",        body);
        GanRef(so, "continueButton",  btnGo.GetComponent<Button>());
        GanRef(so, "continueChevron", chevRt);
        GanRef(so, "npc",             npcGo.GetComponent<TutorialNpcActor>());
        so.ApplyModifiedProperties();

        var soNpc = new SerializedObject(npcGo.GetComponent<TutorialNpcActor>());
        GanRef(soNpc, "targetImage", npcImg);
        soNpc.ApplyModifiedProperties();

        // Bấm cả tấm card = skip gõ chữ / tiếp tục (giữ thói quen cũ của người chơi)
        var cardBtn = cardGo.GetComponent<Button>();
        cardBtn.transition = Selectable.Transition.None;
        NoiOnClick(cardBtn, card, "BamVaoCard");

        // ── Nối 3 ref vào TutorialManager (đây là bước làm V2 THẬT SỰ chạy) ──
        var tm = Object.FindAnyObjectByType<TutorialManager>(FindObjectsInactive.Include);
        if (tm == null)
        {
            bc.AppendLine("║ ⚠ KHÔNG thấy TutorialManager trong scene → V2 dựng xong nhưng CHƯA ai gọi. " +
                          "Mở SCN_Farm.unity rồi chạy lại tool.");
        }
        else
        {
            var soTm = new SerializedObject(tm);
            GanRef(soTm, "_v2Card",   card);
            GanRef(soTm, "_v2Vfx",    vfxGo.GetComponent<TutorialVfxDirector>());
            GanRef(soTm, "_v2Camera", camGo.GetComponent<TutorialCameraDirector>());
            var pUse = soTm.FindProperty("_useV2Dialogue");
            if (pUse != null) pUse.boolValue = true;
            soTm.ApplyModifiedProperties();
            EditorUtility.SetDirty(tm);
            bc.AppendLine($"║ ✔ Nối vào TutorialManager ('{tm.name}'): _v2Card · _v2Vfx · _v2Camera · _useV2Dialogue = true");
            bc.AppendLine("║   ⓘ Muốn về tutorial CŨ: bỏ tick 'Use V2 Dialogue' trên TutorialManager.");
        }

        bc.AppendLine("║ ✔ Đã nối: card · npc · body · nút Tiếp tục · chevron · vfx · camera");
        bc.AppendLine($"║ ⓘ '{ROOT_NAME}' đang TẮT sẵn — TutorialManager sẽ tự bật khi bước đầu chạy.");
        bc.AppendLine("║ ⓘ KHÔNG đụng tới NPC_Dialog_Popup cũ — tutorial cũ còn nguyên để đối chiếu.");
        bc.AppendLine("║ 🔴 BẤM Ctrl+S ĐỂ LƯU SCENE (tool cố ý không tự lưu).");
        bc.AppendLine("╚════════════════════════════════════════════════════════════");

        root.SetActive(false);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log(bc.ToString());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NÚT 2 — NẠP ART
    // ═══════════════════════════════════════════════════════════════════════
    [MenuItem(MENU_ART, false, 11)]
    private static void NapArt()
    {
        var bc = new StringBuilder();
        bc.AppendLine("╔══ [TutorialV2] NẠP ART TỪ ART-HANDOFF ══");

        string goc = Directory.GetParent(Application.dataPath).FullName;
        int tongCopy = 0;

        tongCopy += CopyGoi(Path.Combine(goc, HANDOFF_NPC), ART_DEST_NPC, "A (NPC)", bc);
        tongCopy += CopyGoi(Path.Combine(goc, HANDOFF_VFX), ART_DEST_VFX, "B (VFX)", bc);

        if (tongCopy > 0)
        {
            AssetDatabase.Refresh();
            ChinhImportSprite(ART_DEST_NPC, bc);
            ChinhImportSprite(ART_DEST_VFX, bc);
            AssetDatabase.Refresh();
        }

        // ── Gán vào NPC ─────────────────────────────────────────────────────
        var npc = Object.FindAnyObjectByType<TutorialNpcActor>(FindObjectsInactive.Include);
        if (npc == null) bc.AppendLine("║ ⚠ Chưa thấy TutorialNpcActor trong scene → chạy nút 'Dựng card hội thoại V2' trước.");
        else
        {
            var soN = new SerializedObject(npc);
            int a = GanDaySprite(soN, "talkFrames",  ART_DEST_NPC, "guide_talk_",  12, bc);
            int b = GanDaySprite(soN, "waveFrames",  ART_DEST_NPC, "guide_wave_",  12, bc);
            int c = GanDaySprite(soN, "pointFrames", ART_DEST_NPC, "guide_point_", 12, bc);

            // ── TẠM MƯỢN (Sếp chốt 04/09) ───────────────────────────────────
            // 12 file guide_talk_* đợt 1 bị trả lại: dính viền ô lưới với alpha ĐẶC, và sai
            // khung hình (cao 460 × rộng 451, bắt đầu y=179, KHÔNG CÓ TAY) trong khi
            // wave/point/blink là nửa người (cao ~590 × rộng ~300, y≈50).
            // Clip Talk dùng ~80% thời lượng tutorial nên không thể để trống.
            // Mượn tạm bộ wave: có chuyển động thật VÀ cùng khung hình ⇒ chuyển
            // Talk ↔ Point/Wave không giật. Đội vẽ giao lại talk đúng chuẩn thì
            // GanDaySprite ở trên trả a > 0 và tự ghi đè — KHÔNG cần sửa code, không cần nhớ.
            if (a == 0 && b > 0)
            {
                a = ChepMangSprite(soN, "waveFrames", "talkFrames");
                bc.AppendLine($"║ ⚠ TẠM MƯỢN: talkFrames chưa có art → mượn {a} frame của waveFrames.");
                bc.AppendLine("║   Cùng khung hình nên KHÔNG giật khi đổi clip. Đội vẽ giao lại 12 file");
                bc.AppendLine("║   guide_talk_01..12 rồi bấm lại nút này là tự thay, không phải làm gì thêm.");
            }

            var blink = TaiSprite($"{ART_DEST_NPC}/guide_blink.png");
            var pB = soN.FindProperty("blinkSprite");
            if (blink != null && pB != null) { pB.objectReferenceValue = blink; bc.AppendLine("║ ✔ blink: guide_blink.png"); }
            else bc.AppendLine("║ · blink: chưa có (bỏ qua chớp mắt, không sao)");

            // Frame nghỉ làm hình đứng yên khi chưa diễn — hết cảnh ô vuông mờ.
            var img = npc.GetComponent<Image>();
            // Tư thế nghỉ để NPC đứng yên khi chưa diễn clip nào. Ưu tiên talk_01, nhưng
            // clip nào cũng có frame 01 = tư thế nghỉ nên thiếu talk vẫn có hình đúng khung.
            var f01 = TaiSprite($"{ART_DEST_NPC}/guide_talk_01.png")
                   ?? TaiSprite($"{ART_DEST_NPC}/guide_wave_01.png")
                   ?? TaiSprite($"{ART_DEST_NPC}/guide_point_01.png");
            if (img != null && f01 != null) { img.sprite = f01; img.color = Color.white; EditorUtility.SetDirty(img); }

            soN.ApplyModifiedProperties();
            EditorUtility.SetDirty(npc);
            bc.AppendLine($"║ ⇒ NPC: talk {a}/12 · wave {b}/12 · point {c}/12");
        }

        // ── Gán vào VFX ─────────────────────────────────────────────────────
        var vfx = Object.FindAnyObjectByType<TutorialVfxDirector>(FindObjectsInactive.Include);
        if (vfx == null) bc.AppendLine("║ ⚠ Chưa thấy TutorialVfxDirector → chạy nút dựng trước.");
        else
        {
            var soV = new SerializedObject(vfx);
            GanMotSprite(soV, "glowRing",  $"{ART_DEST_VFX}/tut_glow_ring.png",  bc);
            GanMotSprite(soV, "arrowDown", $"{ART_DEST_VFX}/tut_arrow_down.png", bc);
            GanMotSprite(soV, "burstRay",  $"{ART_DEST_VFX}/tut_burst_ray.png",  bc);
            int s = GanDaySprite(soV, "sparkles",  ART_DEST_VFX, "tut_sparkle_",   4, bc);
            int d = GanDaySprite(soV, "dustPuffs", ART_DEST_VFX, "tut_dust_puff_", 3, bc);

            var pConf = soV.FindProperty("confettiWorldPrefab");
            if (pConf != null && pConf.objectReferenceValue == null)
            {
                var conf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/VFX/Confetti_blast_multicolor.prefab");
                if (conf != null) { pConf.objectReferenceValue = conf; bc.AppendLine("║ ✔ confetti thế giới: Confetti_blast_multicolor (Lana)"); }
            }

            soV.ApplyModifiedProperties();
            EditorUtility.SetDirty(vfx);
            bc.AppendLine($"║ ⇒ VFX: sparkle {s}/4 · dust {d}/3");
        }

        bc.AppendLine("║ 🔴 BẤM Ctrl+S ĐỂ LƯU SCENE.");
        bc.AppendLine("╚════════════════════════════════════════════════════════════");
        Debug.Log(bc.ToString());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NÚT 3 — KIỂM TRA
    // ═══════════════════════════════════════════════════════════════════════
    [MenuItem(MENU_CHECK, false, 12)]
    private static void KiemTra()
    {
        var bc = new StringBuilder();
        bc.AppendLine("╔══ [TutorialV2] KIỂM TRA SẴN SÀNG ══");

        var card = Object.FindAnyObjectByType<TutorialDialogueCard>(FindObjectsInactive.Include);
        var npc  = Object.FindAnyObjectByType<TutorialNpcActor>(FindObjectsInactive.Include);
        var vfx  = Object.FindAnyObjectByType<TutorialVfxDirector>(FindObjectsInactive.Include);
        var camd = Object.FindAnyObjectByType<TutorialCameraDirector>(FindObjectsInactive.Include);

        bc.AppendLine($"║ Card hội thoại : {(card != null ? "CÓ ✔" : "THIẾU ✖ → chạy nút dựng")}");
        bc.AppendLine($"║ NPC actor      : {(npc  != null ? "CÓ ✔" : "THIẾU ✖")}");
        bc.AppendLine($"║ VFX director   : {(vfx  != null ? "CÓ ✔" : "THIẾU ✖")}");
        bc.AppendLine($"║ Camera director: {(camd != null ? "CÓ ✔" : "THIẾU ✖")}");
        bc.AppendLine($"║ Art NPC thật   : {(npc != null && npc.CoArtThat ? "CÓ ✔ (đội vẽ đã giao)" : "CHƯA — đang chạy placeholder, vẫn OK")}");
        bc.AppendLine($"║ Khung card     : {(AssetDatabase.LoadAssetAtPath<Sprite>(CARD_SPRITE) != null ? "CÓ ✔" : "THIẾU ✖ " + CARD_SPRITE)}");

        var tm2 = Object.FindAnyObjectByType<TutorialManager>(FindObjectsInactive.Include);
        bool daNoiTm = false;
        if (tm2 != null)
        {
            var soTm2 = new SerializedObject(tm2);
            var pc = soTm2.FindProperty("_v2Card");
            var pu = soTm2.FindProperty("_useV2Dialogue");
            daNoiTm = pc != null && pc.objectReferenceValue != null && pu != null && pu.boolValue;
        }
        bc.AppendLine($"║ Nối TutorialManager: {(daNoiTm ? "CÓ ✔ (V2 đang bật)" : "CHƯA ✖ → tutorial vẫn chạy bản CŨ")}");

        bool sanSang = card != null && npc != null && vfx != null && camd != null && daNoiTm;
        bc.AppendLine(sanSang
            ? "║ ✅ Khung sườn đủ — chạy được ngay, art về chỉ cần bấm nút 'Nạp art'."
            : "║ ⚠ Còn thiếu — chạy '★ Dựng card hội thoại V2 (1 nút)'.");
        bc.AppendLine("╚════════════════════════════════════════════════════════════");

        if (sanSang) Debug.Log(bc.ToString()); else Debug.LogWarning(bc.ToString());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Hàm phụ
    // ═══════════════════════════════════════════════════════════════════════

    private static Transform TimHoacTaoCanvas(StringBuilder bc)
    {
        var all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var tut = all.FirstOrDefault(c => c.name == CANVAS_NAME);
        if (tut != null) { bc.AppendLine($"║ Canvas: {CANVAS_NAME} (có sẵn)"); return tut.transform; }

        var bat = all.FirstOrDefault(c => c.name == "Canvas_HUD") ?? all.FirstOrDefault();
        if (bat == null) { bc.AppendLine("║ ✖ Scene không có Canvas nào. Mở SCN_Farm.unity rồi chạy lại."); return null; }

        bc.AppendLine($"║ ⚠ Không thấy '{CANVAS_NAME}' → dùng tạm '{bat.name}'.");
        return bat.transform;
    }

    private static void NeoFullMan(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static GameObject LayHoacTao(Transform parent, string ten, params System.Type[] comps)
    {
        Transform t = parent.Find(ten);
        if (t != null)
        {
            foreach (var c in comps)
                if (c != typeof(RectTransform) && t.GetComponent(c) == null) Undo.AddComponent(t.gameObject, c);
            return t.gameObject;
        }

        var go = new GameObject(ten, comps);
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, "Dựng " + ten);
        return go;
    }

    private static void GanRef(SerializedObject so, string field, Object giaTri)
    {
        var p = so.FindProperty(field);
        if (p != null) p.objectReferenceValue = giaTri;
    }

    private static void NoiOnClick(Button btn, Object target, string method)
    {
        for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            if (btn.onClick.GetPersistentTarget(i) == target) return;   // đã nối rồi, đừng nối chồng

        var call = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), target, method, false, false)
                   as UnityEngine.Events.UnityAction;
        if (call != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, call);
    }

    private static int CopyGoi(string thuMucNguon, string dichAssets, string tenGoi, StringBuilder bc)
    {
        if (!Directory.Exists(thuMucNguon))
        {
            bc.AppendLine($"║ · Gói {tenGoi}: chưa có thư mục nguồn → bỏ qua");
            return 0;
        }

        var tatCa = Directory.GetFiles(thuMucNguon, "*.png", SearchOption.TopDirectoryOnly);

        // LỌC file phụ. Prompt cấm "thêm file phụ" nhưng thực tế đội vẽ hay kèm spritesheet
        // nguồn (VD guide_talk_sheet_expanded.png, 4.2MB). Copy vào Assets là phình project
        // và tạo một Sprite rác không ai dùng. Chỉ nhận đúng tên mà code sẽ đi tìm.
        var files = tatCa.Where(f => TenHopLe(Path.GetFileName(f))).ToArray();
        int boQua = tatCa.Length - files.Length;
        if (boQua > 0)
        {
            var ten = tatCa.Where(f => !TenHopLe(Path.GetFileName(f))).Select(Path.GetFileName);
            bc.AppendLine($"║ ⏭ Gói {tenGoi}: BỎ QUA {boQua} file không có trong hợp đồng đặt tên: {string.Join(", ", ten)}");
        }

        if (files.Length == 0)
        {
            bc.AppendLine($"║ · Gói {tenGoi}: không có file hợp lệ — đội vẽ chưa giao");
            return 0;
        }

        if (!AssetDatabase.IsValidFolder(dichAssets)) TaoThuMuc(dichAssets);

        string gocDuAn = Directory.GetParent(Application.dataPath).FullName;
        int n = 0;
        int loi = 0;
        foreach (var f in files)
        {
            try
            {
                string dich = Path.Combine(gocDuAn, dichAssets, Path.GetFileName(f));
                File.Copy(f, dich, true);
                n++;
            }
            catch (System.Exception e)
            {
                // Một file hỏng không được làm chết cả mẻ — các file còn lại vẫn phải qua.
                loi++;
                bc.AppendLine($"║   ✖ {Path.GetFileName(f)}: {e.Message}");
            }
        }
        if (loi > 0) bc.AppendLine($"║   ⚠ {loi} file copy hỏng (xem dòng trên), {n} file OK.");

        bc.AppendLine($"║ ✔ Gói {tenGoi}: copy {n} file → {dichAssets}");
        return n;
    }

    /// <summary>
    /// Tên file có nằm trong hợp đồng đặt tên không. Chỉ những tên này mới được vào Assets.
    /// Khớp đúng những gì TutorialNpcActor / TutorialVfxDirector sẽ đi tìm.
    /// </summary>
    private static bool TenHopLe(string tenFile)
    {
        string t = tenFile.ToLowerInvariant();

        if (t == "guide_blink.png") return true;

        foreach (var tienTo in new[] { "guide_talk_", "guide_wave_", "guide_point_" })
            for (int i = 1; i <= 12; i++)
                if (t == $"{tienTo}{i:00}.png") return true;

        if (t == "tut_glow_ring.png" || t == "tut_arrow_down.png" || t == "tut_burst_ray.png") return true;

        for (int i = 1; i <= 4; i++) if (t == $"tut_sparkle_{i:00}.png")   return true;
        for (int i = 1; i <= 3; i++) if (t == $"tut_dust_puff_{i:00}.png") return true;

        return false;
    }

    private static void TaoThuMuc(string duongDan)
    {
        var phan = duongDan.Split('/');
        string dang = phan[0];
        for (int i = 1; i < phan.Length; i++)
        {
            string tiep = dang + "/" + phan[i];
            if (!AssetDatabase.IsValidFolder(tiep)) AssetDatabase.CreateFolder(dang, phan[i]);
            dang = tiep;
        }
    }

    /// <summary>Ép mọi PNG trong thư mục về đúng chuẩn Sprite UI: Single, pivot giữa, không nén mờ.</summary>
    private static void ChinhImportSprite(string thuMuc, StringBuilder bc)
    {
        if (!AssetDatabase.IsValidFolder(thuMuc)) return;

        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { thuMuc });
        int n = 0;
        foreach (var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp == null) continue;

            bool doi = false;
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; doi = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; doi = true; }
            if (imp.mipmapEnabled) { imp.mipmapEnabled = false; doi = true; }
            if (imp.alphaIsTransparency == false) { imp.alphaIsTransparency = true; doi = true; }
            if (imp.maxTextureSize < 1024) { imp.maxTextureSize = 1024; doi = true; }

            if (doi) { imp.SaveAndReimport(); n++; }
        }
        if (n > 0) bc.AppendLine($"║ ✔ Chỉnh import chuẩn Sprite cho {n} file trong {thuMuc}");
    }

    private static Sprite TaiSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path)
            ?? AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static void GanMotSprite(SerializedObject so, string field, string path, StringBuilder bc)
    {
        var p = so.FindProperty(field);
        if (p == null) return;
        var s = TaiSprite(path);
        if (s != null) { p.objectReferenceValue = s; bc.AppendLine($"║ ✔ {field}: {Path.GetFileName(path)}"); }
        else bc.AppendLine($"║ · {field}: chưa có {Path.GetFileName(path)} (bỏ qua hiệu ứng này)");
    }

    /// <summary>
    /// Chép nguyên mảng sprite từ field này sang field khác (dùng cho việc "tạm mượn" clip).
    /// Trả về số phần tử đã chép.
    /// </summary>
    private static int ChepMangSprite(SerializedObject so, string fieldNguon, string fieldDich)
    {
        var nguon = so.FindProperty(fieldNguon);
        var dich  = so.FindProperty(fieldDich);
        if (nguon == null || dich == null || !nguon.isArray || !dich.isArray) return 0;

        dich.arraySize = nguon.arraySize;
        for (int i = 0; i < nguon.arraySize; i++)
            dich.GetArrayElementAtIndex(i).objectReferenceValue =
                nguon.GetArrayElementAtIndex(i).objectReferenceValue;

        return nguon.arraySize;
    }

    private static int GanDaySprite(SerializedObject so, string field, string thuMuc, string tienTo, int soLuong, StringBuilder bc)
    {
        var p = so.FindProperty(field);
        if (p == null) return 0;

        var ds = new List<Sprite>();
        for (int i = 1; i <= soLuong; i++)
        {
            var s = TaiSprite($"{thuMuc}/{tienTo}{i:00}.png");
            if (s != null) ds.Add(s);
        }

        if (ds.Count == 0) return 0;

        p.arraySize = ds.Count;
        for (int i = 0; i < ds.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = ds[i];
        return ds.Count;
    }
}
