using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor Tool: Tools/Farm Game/Level Up Popup/★ Nâng cấp V2 (1 nút)
///
/// NÂNG CẤP popup Lên Cấp hiện có lên chuẩn "juice V2" (tham chiếu Family Farm):
///   1. Tạo 4 slot NHÂN VẬT ĂN MỪNG (2 trái 2 phải quanh badge) + CelebrationCharacterSlot,
///      TỰ GÁN frames TẠM từ NPC có sẵn (NV01/NV03/NV05/NV07 — NVxx_down_1..3, loop 3 frame)
///      để chạy được NGAY trong lúc chờ đội vẽ giao art 12 frame.
///   2. Add LevelUpSparkleFX (tia sáng quay + sparkle 4 cánh + glow pulse — thuần code).
///   3. Add LevelUpTapToClose (overlay trong suốt, chạm bất kỳ đâu để đóng sau ~0.8s).
///   4. Nối dây mọi SerializeField mới của LevelUpPopupUI qua SerializedObject.
///
/// Menu phụ: ".../Gắn art nhân vật V2 từ Assets/Art/UI/LevelUpV2" — khi art thật về
/// (char_01..04 × f01..f12) chạy 1 nút để thay frames, KHÔNG cần sửa code/scene tay.
///
/// AN TOÀN: idempotent (chạy lại không nhân đôi), có Undo, KHÔNG auto-save scene
/// (chỉ MarkSceneDirty — bạn tự Ctrl+S sau khi ưng mắt). In report chi tiết ra Console.
/// </summary>
public static class LevelUpPopupV2SetupTool
{
    private const string MENU_ROOT    = "Tools/Farm Game/Level Up Popup/";
    private const string MENU_UPGRADE = MENU_ROOT + "★ Nâng cấp V2 (1 nút)";
    private const string MENU_ART     = MENU_ROOT + "Gắn art nhân vật V2 từ Assets/Art/UI/LevelUpV2";

    // ── Tên object do tool này sở hữu (idempotent: tìm theo tên trước khi tạo) ──
    private const string GO_CELEBRATION_ROOT = "V2_Celebration";
    private const string GO_CHAR_SLOT_PREFIX = "V2_CharSlot_";      // + 01..04
    private const string GO_TAP_CATCHER      = "V2_TapCatcher";
    private const string GO_BADGE_ANCHOR     = "V2_BadgeAnchor";

    // ── Nguồn frames TẠM: NPC có sẵn của game (3 frame đi xuống) ──
    private static readonly string[] TempNpcIds = { "NV01", "NV03", "NV05", "NV07" };
    private const string NPC_ROOT   = "Assets/NV_NPC/NVGAME/Processed";
    private const float  TEMP_FPS   = 6f;    // 3 frame → 6fps = 2 loop/giây, nhìn tự nhiên

    // ── Nguồn art THẬT (menu phụ) ──
    private const string ART_ROOT   = "Assets/Art/UI/LevelUpV2/characters";
    private const int    ART_FRAMES = 12;
    private const float  ART_FPS    = 12f;

    // ── Bố cục 4 slot quanh badge: anchor chuẩn hoá theo ContentPanel ──
    // 2 TRÁI (ngoài-trên, trong-dưới) + 2 PHẢI đối xứng. Dùng anchor tỉ lệ thay vì
    // px cứng để popup Township kích thước nào cũng đặt đúng chỗ.
    private static readonly Vector2[] SlotAnchors =
    {
        new Vector2(0.075f, 0.80f),   // trái - ngoài, cao
        new Vector2(0.185f, 0.62f),   // trái - trong, thấp
        new Vector2(0.925f, 0.80f),   // phải - ngoài, cao
        new Vector2(0.815f, 0.62f),   // phải - trong, thấp
    };
    private static readonly Vector2 SlotSize = new Vector2(110f, 110f);

    // =========================================================================
    // MENU 1 — ★ Nâng cấp V2 (1 nút)
    // =========================================================================

    [MenuItem(MENU_UPGRADE)]
    public static void UpgradeToV2()
    {
        var report = new StringBuilder();
        report.AppendLine("═══════ NÂNG CẤP POPUP LÊN CẤP → V2 — BÁO CÁO ═══════");

        // ── 1. Tìm popup hiện có (cùng cách LevelUpPopupSetupTool tìm) ──────
        LevelUpPopupUI popup = FindPopup(report);
        if (popup == null)
        {
            EditorUtility.DisplayDialog("Nâng cấp V2",
                "Khong tim thay LevelUpPopupUI nao trong scene.\n\n" +
                "Chay truoc: Tools ▸ Farm ▸ Popup Len Cap (Township) de dung popup, roi chay lai tool nay.",
                "OK");
            return;
        }

        var so = new SerializedObject(popup);
        // Chốt an toàn: script LevelUpPopupUI phải là bản V2 (đã có field mới).
        if (so.FindProperty("celebrationSlots") == null ||
            so.FindProperty("sparkleFx")        == null ||
            so.FindProperty("tapCatcher")       == null)
        {
            EditorUtility.DisplayDialog("Nâng cấp V2",
                "LevelUpPopupUI.cs trong project CHUA phai ban V2 " +
                "(thieu field celebrationSlots / sparkleFx / tapCatcher).\n\n" +
                "Cap nhat file LevelUpPopupUI.cs ban V2 truoc, doi Unity compile xong roi chay lai.",
                "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Nang cap Level Up Popup V2");

        // ── 2. Xác định các mốc hierarchy ───────────────────────────────────
        RectTransform popupRoot = ResolvePopupRoot(popup, so);
        RectTransform content   = so.FindProperty("contentPanel").objectReferenceValue as RectTransform;
        if (content == null)
        {
            content = popupRoot;   // fallback: đặt thẳng vào root
            report.AppendLine("[WARN] contentPanel chua noi day → dat slot nhan vat theo popup root.");
        }
        report.AppendLine($"• Popup        : {FullPath(popup.transform)}");
        report.AppendLine($"• Root hien thi: {popupRoot.name}   • ContentPanel: {content.name}");

        RectTransform badge = FindBadgeAnchor(popupRoot, content, report);

        // ── 3. Dựng 4 slot nhân vật ăn mừng ─────────────────────────────────
        RectTransform celebrationRoot = EnsureChildRect(content, GO_CELEBRATION_ROOT,
            Vector2.zero, Vector2.one, Vector2.zero, report);
        // Không chặn raycast của cả vùng
        celebrationRoot.SetAsLastSibling();   // nhân vật nổi trên nền panel

        var slots = new CelebrationCharacterSlot[SlotAnchors.Length];
        for (int i = 0; i < SlotAnchors.Length; i++)
        {
            string slotName = GO_CHAR_SLOT_PREFIX + (i + 1).ToString("00");
            RectTransform slotRt = EnsureChildRect(celebrationRoot, slotName,
                SlotAnchors[i], SlotAnchors[i], SlotSize, report);

            var img = slotRt.GetComponent<Image>();
            if (img == null)
            {
                img = Undo.AddComponent<Image>(slotRt.gameObject);
                report.AppendLine($"  + Image cho {slotName}");
            }
            img.raycastTarget  = false;
            img.preserveAspect = true;

            var slot = slotRt.GetComponent<CelebrationCharacterSlot>();
            if (slot == null)
            {
                slot = Undo.AddComponent<CelebrationCharacterSlot>(slotRt.gameObject);
                report.AppendLine($"  + CelebrationCharacterSlot cho {slotName}");
            }
            slots[i] = slot;

            // Gán frames TẠM từ NPC + lệch pha 0 / 0.25 / 0.5 / 0.75
            Sprite[] tempFrames = LoadTempNpcFrames(TempNpcIds[i], report);
            WireCharacterSlot(slot, img, tempFrames,
                tempFrames.Length > 0 ? TEMP_FPS : ART_FPS,
                i * 0.25f, report, slotName);
        }

        // ── 4. LevelUpSparkleFX ─────────────────────────────────────────────
        var sparkle = popup.GetComponent<LevelUpSparkleFX>();
        if (sparkle == null)
        {
            sparkle = Undo.AddComponent<LevelUpSparkleFX>(popup.gameObject);
            report.AppendLine("+ LevelUpSparkleFX (tren object LevelUpPopupUI)");
        }
        else report.AppendLine("= LevelUpSparkleFX da co — giu nguyen");

        var soSparkle = new SerializedObject(sparkle);
        SetRef(soSparkle, "badgeAnchor", badge, report, "SparkleFX.badgeAnchor");
        SetRef(soSparkle, "sparkleArea", content, report, "SparkleFX.sparkleArea");
        soSparkle.ApplyModifiedProperties();

        // ── 5. LevelUpTapToClose (overlay full-screen, sibling index 1) ─────
        RectTransform tapRt = EnsureChildRect(popupRoot, GO_TAP_CATCHER,
            Vector2.zero, Vector2.one, Vector2.zero, report);
        // Sibling 1: NGAY TRÊN nền dim (index 0 thường là bg/VFX_Background),
        // và DƯỚI ContentPanel → nút Nhận Quà vẫn ăn raycast trước.
        tapRt.SetSiblingIndex(Mathf.Min(1, popupRoot.childCount - 1));

        var tapImg = tapRt.GetComponent<Image>();
        if (tapImg == null)
        {
            tapImg = Undo.AddComponent<Image>(tapRt.gameObject);
            report.AppendLine($"  + Image (trong suot) cho {GO_TAP_CATCHER}");
        }
        tapImg.color = Color.clear;
        tapImg.raycastTarget = true;

        var tap = tapRt.GetComponent<LevelUpTapToClose>();
        if (tap == null)
        {
            tap = Undo.AddComponent<LevelUpTapToClose>(tapRt.gameObject);
            report.AppendLine($"  + LevelUpTapToClose cho {GO_TAP_CATCHER} (minOpenDelay mac dinh 0.8s)");
        }

        // ── 6. Nối dây các SerializeField mới của LevelUpPopupUI ────────────
        var slotsProp = so.FindProperty("celebrationSlots");
        slotsProp.arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++)
            slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        report.AppendLine($"• LevelUpPopupUI.celebrationSlots ← {slots.Length} slot");

        SetRef(so, "sparkleFx",  sparkle, report, "LevelUpPopupUI.sparkleFx");
        SetRef(so, "tapCatcher", tap,     report, "LevelUpPopupUI.tapCatcher");
        var tapFlag = so.FindProperty("tapAnywhereToClose");
        if (tapFlag != null && !tapFlag.boolValue)
        {
            tapFlag.boolValue = true;
            report.AppendLine("• LevelUpPopupUI.tapAnywhereToClose = true");
        }
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(popup);
        if (popup.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(popup.gameObject.scene);   // KHÔNG auto-save

        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = popup.gameObject;

        report.AppendLine("─────────────────────────────────────────────");
        report.AppendLine("XONG. Scene DA DANH DAU dirty nhung CHUA save — kiem tra roi Ctrl+S.");
        report.AppendLine("Khi art that ve: chay menu '" + MENU_ART + "'.");
        Debug.Log(report.ToString());

        EditorUtility.DisplayDialog("Nâng cấp V2",
            "Da nang cap popup len V2!\n\n" +
            "• 4 slot nhan vat (frames tam tu NPC)\n" +
            "• SparkleFX (tia quay + sparkle + glow)\n" +
            "• Tap-anywhere-to-close (delay 0.8s)\n\n" +
            "Chi tiet xem Console. Scene CHUA duoc save — hay Ctrl+S sau khi kiem tra.",
            "OK");
    }

    [MenuItem(MENU_UPGRADE, true)]
    private static bool ValidateUpgrade() => !EditorApplication.isPlaying;

    // =========================================================================
    // MENU 2 — Gắn art nhân vật thật khi đội vẽ giao hàng
    // =========================================================================

    [MenuItem(MENU_ART)]
    public static void WireRealCharacterArt()
    {
        var report = new StringBuilder();
        report.AppendLine("═══════ GẮN ART NHÂN VẬT V2 (char_01..04 × f01..f12) ═══════");

        LevelUpPopupUI popup = FindPopup(report);
        if (popup == null)
        {
            EditorUtility.DisplayDialog("Gắn art V2", "Khong tim thay LevelUpPopupUI trong scene.", "OK");
            return;
        }

        var so = new SerializedObject(popup);
        var slotsProp = so.FindProperty("celebrationSlots");
        if (slotsProp == null || slotsProp.arraySize == 0)
        {
            EditorUtility.DisplayDialog("Gắn art V2",
                "LevelUpPopupUI chua co celebrationSlots.\n\nChay '★ Nang cap V2 (1 nut)' truoc.",
                "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Gan art nhan vat Level Up V2");

        int wired = 0;
        int slotCount = slotsProp.arraySize;
        for (int i = 0; i < slotCount; i++)
        {
            var slot = slotsProp.GetArrayElementAtIndex(i).objectReferenceValue as CelebrationCharacterSlot;
            if (slot == null)
            {
                report.AppendLine($"[WARN] celebrationSlots[{i}] = null → bo qua (chay lai '★ Nang cap V2').");
                continue;
            }

            string charId = $"char_{(i + 1):00}";
            var frames = new System.Collections.Generic.List<Sprite>(ART_FRAMES);
            var missing = new System.Collections.Generic.List<string>();

            for (int f = 1; f <= ART_FRAMES; f++)
            {
                string path = $"{ART_ROOT}/{charId}/{charId}_f{f:00}.png";
                Sprite s = LoadSpriteAt(path);
                if (s != null) frames.Add(s);
                else           missing.Add(path);
            }

            if (frames.Count == 0)
            {
                report.AppendLine($"[WARN] {charId}: KHONG tim thay frame nao trong {ART_ROOT}/{charId}/ " +
                                  "→ GIU NGUYEN frames hien tai (NPC tam).");
                continue;
            }
            if (missing.Count > 0)
                report.AppendLine($"[WARN] {charId}: thieu {missing.Count}/{ART_FRAMES} frame " +
                                  $"(vd: {missing[0]}) → dung {frames.Count} frame tim duoc.");

            var soSlot = new SerializedObject(slot);
            var framesProp = soSlot.FindProperty("frames");
            var fpsProp    = soSlot.FindProperty("framesPerSecond");
            if (framesProp == null)
            {
                report.AppendLine($"[ERR] {charId}: CelebrationCharacterSlot khong co field 'frames'?!");
                continue;
            }
            framesProp.arraySize = frames.Count;
            for (int f = 0; f < frames.Count; f++)
                framesProp.GetArrayElementAtIndex(f).objectReferenceValue = frames[f];
            if (fpsProp != null) fpsProp.floatValue = ART_FPS;
            soSlot.ApplyModifiedProperties();
            EditorUtility.SetDirty(slot);

            // Slot có thể đã tự tắt lúc chạy thử vì thiếu frames → bật lại cho chắc.
            if (!slot.gameObject.activeSelf)
            {
                Undo.RecordObject(slot.gameObject, "Enable char slot");
                slot.gameObject.SetActive(true);
            }

            report.AppendLine($"• {charId} → slot {i + 1}: gan {frames.Count} frame, fps = {ART_FPS}");
            wired++;
        }

        if (popup.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(popup.gameObject.scene);   // KHÔNG auto-save
        Undo.CollapseUndoOperations(undoGroup);

        report.AppendLine($"─── XONG: {wired}/{slotCount} slot da gan art that. Scene chua save — Ctrl+S. ───");
        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Gắn art V2",
            $"Da gan art that cho {wired}/{slotCount} slot.\nChi tiet xem Console.", "OK");
    }

    [MenuItem(MENU_ART, true)]
    private static bool ValidateWireArt() => !EditorApplication.isPlaying;

    // =========================================================================
    // Helpers — tìm kiếm
    // =========================================================================

    /// <summary>Tìm popup hiện có — cùng API với LevelUpPopupSetupTool (không deprecated).</summary>
    private static LevelUpPopupUI FindPopup(StringBuilder report)
    {
        var all = Object.FindObjectsByType<LevelUpPopupUI>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (all.Length == 0) return null;
        if (all.Length > 1)
            report.AppendLine($"[WARN] Scene co {all.Length} LevelUpPopupUI — dung cai dau: {all[0].name}");
        return all[0];
    }

    /// <summary>Lấy popupRoot đã nối dây; fallback về chính RectTransform của component.</summary>
    private static RectTransform ResolvePopupRoot(LevelUpPopupUI popup, SerializedObject so)
    {
        var rootProp = so.FindProperty("popupRoot");
        var rootGo = rootProp != null ? rootProp.objectReferenceValue as GameObject : null;
        var rt = rootGo != null ? rootGo.transform as RectTransform : null;
        return rt != null ? rt : popup.transform as RectTransform;
    }

    /// <summary>
    /// Tìm badge/sao vàng để neo tia sáng: dò đệ quy theo tên quen thuộc
    /// (sao / star / badge / huyhieu / level_badge). Không thấy → tạo anchor
    /// <see cref="GO_BADGE_ANCHOR"/> ở đỉnh giữa ContentPanel (nơi sao thường đứng).
    /// </summary>
    private static RectTransform FindBadgeAnchor(RectTransform popupRoot, RectTransform content, StringBuilder report)
    {
        string[] keys = { "sao", "star", "badge", "huyhieu", "huy_hieu" };
        RectTransform found = FindDeepByNameContains(content, keys)
                           ?? FindDeepByNameContains(popupRoot, keys);
        if (found != null)
        {
            report.AppendLine($"• Badge (neo tia sang): tim thay '{found.name}'");
            return found;
        }

        RectTransform anchor = EnsureChildRect(content, GO_BADGE_ANCHOR,
            new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(10f, 10f), report);
        report.AppendLine($"• Badge: KHONG tim thay theo ten → tao anchor '{GO_BADGE_ANCHOR}' o dinh giua ContentPanel " +
                          "(keo lai vi tri trong Scene view neu sao nam cho khac).");
        return anchor;
    }

    /// <summary>Dò đệ quy con cháu có tên chứa 1 trong các từ khoá (không phân biệt hoa thường).</summary>
    private static RectTransform FindDeepByNameContains(Transform root, string[] keys)
    {
        if (root == null) return null;
        foreach (Transform child in root)
        {
            string n = child.name.ToLowerInvariant().Replace(" ", "").Replace("-", "_");
            for (int k = 0; k < keys.Length; k++)
                if (n.Contains(keys[k]) && child is RectTransform rt)
                    return rt;
            var deeper = FindDeepByNameContains(child, keys);
            if (deeper != null) return deeper;
        }
        return null;
    }

    // =========================================================================
    // Helpers — dựng object (idempotent + Undo)
    // =========================================================================

    /// <summary>
    /// Tìm con TRỰC TIẾP theo tên; chưa có thì tạo mới (RectTransform, đã set anchor/size).
    /// anchorMin == anchorMax → object điểm (sizeDelta = size);
    /// khác nhau → object stretch (offset 0).
    /// </summary>
    private static RectTransform EnsureChildRect(
        RectTransform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 size, StringBuilder report)
    {
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name && parent.GetChild(i) is RectTransform existing)
                return existing;   // idempotent — giữ nguyên object + chỉnh tay của designer

        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        if (anchorMin == anchorMax)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = size;
        }
        else
        {
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        report.AppendLine($"+ Tao '{name}' duoi '{parent.name}'");
        return rt;
    }

    /// <summary>Gán frames + fps + phase + targetImage cho 1 CelebrationCharacterSlot qua SerializedObject.</summary>
    private static void WireCharacterSlot(
        CelebrationCharacterSlot slot, Image img, Sprite[] frames,
        float fps, float phase, StringBuilder report, string slotName)
    {
        var soSlot = new SerializedObject(slot);

        var imgProp = soSlot.FindProperty("targetImage");
        if (imgProp != null) imgProp.objectReferenceValue = img;

        var framesProp = soSlot.FindProperty("frames");
        if (framesProp != null)
        {
            framesProp.arraySize = frames.Length;
            for (int f = 0; f < frames.Length; f++)
                framesProp.GetArrayElementAtIndex(f).objectReferenceValue = frames[f];
        }

        var fpsProp = soSlot.FindProperty("framesPerSecond");
        if (fpsProp != null) fpsProp.floatValue = fps;

        var phaseProp = soSlot.FindProperty("phaseOffset");
        if (phaseProp != null) phaseProp.floatValue = phase;

        soSlot.ApplyModifiedProperties();
        EditorUtility.SetDirty(slot);

        if (frames.Length > 0)
        {
            report.AppendLine($"  • {slotName}: {frames.Length} frame tam, fps={fps}, phase={phase:0.00}");
            // Hiện frame đầu ngay trong Editor cho dễ thấy vị trí
            img.sprite = frames[0];
        }
        else
        {
            report.AppendLine($"  • {slotName}: CHUA co frame (NPC nguon thieu) — slot se TU AN khi chay. " +
                              "Gan art that bang menu 'Gan art nhan vat V2'.");
        }
    }

    // =========================================================================
    // Helpers — load sprite
    // =========================================================================

    /// <summary>Nạp 3 frame tạm NVxx_down_1..3 của một NPC. Thiếu file nào bỏ file đó + log.</summary>
    private static Sprite[] LoadTempNpcFrames(string npcId, StringBuilder report)
    {
        var frames = new System.Collections.Generic.List<Sprite>(3);
        for (int f = 1; f <= 3; f++)
        {
            string path = $"{NPC_ROOT}/{npcId}/{npcId}_down_{f}.png";
            Sprite s = LoadSpriteAt(path);
            if (s != null) frames.Add(s);
            else report.AppendLine($"  [WARN] Khong tim thay sprite tam: {path}");
        }
        return frames.ToArray();
    }

    /// <summary>
    /// Load Sprite tại đường dẫn — chịu được cả texture import kiểu Single lẫn Multiple
    /// (Multiple thì LoadAssetAtPath&lt;Sprite&gt; trả null, phải duyệt LoadAllAssetsAtPath).
    /// </summary>
    private static Sprite LoadSpriteAt(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;

        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite sp) return sp;
        return null;
    }

    // =========================================================================
    // Helpers — SerializedObject
    // =========================================================================

    private static void SetRef(SerializedObject so, string field, Object value,
        StringBuilder report, string label)
    {
        var p = so.FindProperty(field);
        if (p == null)
        {
            report.AppendLine($"[WARN] Khong tim thay field '{field}' — script chua phai ban V2?");
            return;
        }
        if (p.objectReferenceValue != value)
        {
            p.objectReferenceValue = value;
            report.AppendLine($"• {label} ← {(value != null ? value.name : "null")}");
        }
    }

    private static string FullPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + " / " + p; }
        return p;
    }
}
