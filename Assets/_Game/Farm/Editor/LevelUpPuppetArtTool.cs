#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// [V3] MENU: Tools/Farm Game/Level Up Popup/Gắn art PUPPET (1 hình master — khuyến nghị)
///
/// Gắn nhân vật ăn mừng theo chế độ PUPPET: mỗi slot chỉ cần 1 hình master
/// (ưu tiên char_0N_master.png, không có thì lấy char_0N_f01.png) + blink tùy chọn
/// (char_0N_blink.png — cùng pose, mắt nhắm). Code sẽ tự diễn chuyển động 60fps.
///
/// File TÁCH RIÊNG khỏi LevelUpPopupV2SetupTool theo luật "mỗi file một chủ"
/// (bài học ghép nối trong memory dự án). Idempotent, có Undo, KHÔNG auto-save scene.
/// </summary>
public static class LevelUpPuppetArtTool
{
    private const string MENU     = "Tools/Farm Game/Level Up Popup/Gắn art PUPPET (1 hình master — khuyến nghị)";
    private const string ART_ROOT = "Assets/Art/UI/LevelUpV2/characters";

    [MenuItem(MENU)]
    public static void WirePuppetArt()
    {
        var report = new StringBuilder();
        report.AppendLine("═══════ GẮN ART PUPPET V3 (1 hình master/nhân vật) ═══════");

        LevelUpPopupUI popup = Object.FindFirstObjectByType<LevelUpPopupUI>(FindObjectsInactive.Include);
        if (popup == null)
        {
            EditorUtility.DisplayDialog("Gắn art PUPPET",
                "Khong tim thay LevelUpPopupUI trong scene.\nMo dung scene farm roi chay lai.", "OK");
            return;
        }

        var so = new SerializedObject(popup);
        var slotsProp = so.FindProperty("celebrationSlots");
        if (slotsProp == null || slotsProp.arraySize == 0)
        {
            EditorUtility.DisplayDialog("Gắn art PUPPET",
                "LevelUpPopupUI chua co celebrationSlots.\n\nChay 'Tools/Farm Game/Level Up Popup/★ Nang cap V2 (1 nut)' truoc.",
                "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Gan art PUPPET Level Up V3");

        int wired = 0;
        for (int i = 0; i < slotsProp.arraySize; i++)
        {
            var slot = slotsProp.GetArrayElementAtIndex(i).objectReferenceValue as CelebrationCharacterSlot;
            if (slot == null)
            {
                report.AppendLine($"[WARN] celebrationSlots[{i}] = null → bo qua (chay lai '★ Nang cap V2').");
                continue;
            }

            string charId = $"char_{(i + 1):00}";

            // Master: ưu tiên *_master.png; fallback *_f01.png
            Sprite master = LoadSpriteAt($"{ART_ROOT}/{charId}/{charId}_master.png")
                         ?? LoadSpriteAt($"{ART_ROOT}/{charId}/{charId}_f01.png");
            Sprite blink  = LoadSpriteAt($"{ART_ROOT}/{charId}/{charId}_blink.png"); // tùy chọn

            if (master == null)
            {
                report.AppendLine($"[WARN] {charId}: khong co {charId}_master.png / {charId}_f01.png " +
                                  $"trong {ART_ROOT}/{charId}/ → GIU NGUYEN slot hien tai.");
                continue;
            }

            Undo.RecordObject(slot, "Gan puppet master");
            var soSlot = new SerializedObject(slot);

            var masterProp = soSlot.FindProperty("puppetMaster");
            var blinkProp  = soSlot.FindProperty("blinkSprite");
            var framesProp = soSlot.FindProperty("frames");
            if (masterProp == null)
            {
                report.AppendLine($"[ERR] {charId}: CelebrationCharacterSlot chua co field 'puppetMaster' " +
                                  "— file CelebrationCharacterSlot.cs chua duoc cap nhat len V3?");
                continue;
            }

            masterProp.objectReferenceValue = master;
            if (blinkProp != null) blinkProp.objectReferenceValue = blink; // null = không chớp mắt
            if (framesProp != null) framesProp.arraySize = 0;              // dẹp sheet cũ → chắc chắn vào PUPPET
            soSlot.ApplyModifiedProperties();
            EditorUtility.SetDirty(slot);

            if (!slot.gameObject.activeSelf)
            {
                Undo.RecordObject(slot.gameObject, "Bat lai slot");
                slot.gameObject.SetActive(true);
            }

            report.AppendLine($"  ✓ {charId}: master={master.name}" +
                              (blink != null ? $" + blink={blink.name}" : " (chua co blink — nhan vat khong chop mat, van OK)"));
            wired++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        report.AppendLine($"─── Ket qua: gan PUPPET cho {wired}/{slotsProp.arraySize} slot. ───");
        report.AppendLine("Scene CHUA duoc save — kiem tra bang Debug Preview L2/L5 roi Ctrl+S.");
        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Gắn art PUPPET",
            $"Da gan {wired}/{slotsProp.arraySize} slot.\nXem report day du trong Console.\nNho Ctrl+S de luu scene.", "OK");
    }

    private static Sprite LoadSpriteAt(string assetPath)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }
}
#endif
