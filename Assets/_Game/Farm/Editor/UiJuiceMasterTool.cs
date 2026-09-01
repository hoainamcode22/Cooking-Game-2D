#if UNITY_EDITOR
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [V1] BỘ ĐIỀU HÀNH GÓI UI JUICE — theo lệnh Sếp "tăng hiệu suất 100%, 1 nút":
///
///   ★★★ Tools/Farm Game/UI JUICE — LÀM TẤT CẢ (1 nút)
///       Chạy TOÀN BỘ chuỗi vận hành theo đúng thứ tự, khỏi nhớ menu lẻ:
///       1. Setup Reward Fly FX (hệ tiền bay + thư viện icon + gán icon_gold của đội vẽ)
///       2. Hoàn thiện popup Level-Up (dim xám + chữ trắng + BO GÓC + 4 nhân vật 230px + art + thở)
///       3. Đổ quà V3 (APPLY — mọi level ≥6 món)
///       4. Idle sống động cho 11 khách du lịch (frame đổi chân/ngoái nhìn)
///       5. Đồng bộ icon vàng toàn scene (hỏi trước khi APPLY — thay cả HUD)
///       6. Hỏi SAVE SCENE (Ctrl+S hộ Sếp — có hỏi, không tự tiện)
///       7. Tự chạy KHÁM BỆNH in bảng ✓/✗ cuối cùng.
///
///   ★★★ Tools/Farm Game/UI JUICE — KHÁM BỆNH (kiểm tra ✓✗)
///       Không đổi gì — chỉ khám và in bảng trạng thái từng hạng mục + cách fix.
///       Sếp thấy "không có gì thay đổi" → bấm nút này, chụp Console gửi Lead.
/// </summary>
public static class UiJuiceMasterTool
{
    private const string MENU_ALL    = "Tools/Farm Game/★★★ UI JUICE — LÀM TẤT CẢ (1 nút)";
    private const string MENU_DOCTOR = "Tools/Farm Game/★★★ UI JUICE — KHÁM BỆNH (kiểm tra ✓✗)";

    // ════════════════════════════════════════════════════════════════════
    //  LÀM TẤT CẢ
    // ════════════════════════════════════════════════════════════════════

    [MenuItem(MENU_ALL)]
    public static void RunAll()
    {
        if (!EditorUtility.DisplayDialog("UI JUICE — LÀM TẤT CẢ",
            "Chạy trọn chuỗi: Reward FX → Popup Level-Up (dim/chữ/bo góc/nhân vật) →\n" +
            "Đổ quà (APPLY) → Idle khách du lịch → Đồng bộ icon vàng → Save scene.\n\n" +
            "Mỗi bước in report riêng vào Console. Tiếp tục?", "Chạy", "Thôi"))
            return;

        Buoc("1/5 Setup Reward Fly FX",        RewardFxSetupTool.SetupRewardFlyFX);
        Buoc("2/5 Hoàn thiện popup Level-Up",  LevelUpSlotLayoutFixTool.FixLayoutAndWireArt);
        Buoc("3/5 Đổ quà V3 (APPLY)",          LevelRewardV2FillTool.Apply);
        Buoc("4/5 Idle sống động khách",       TouristBreathingSetupTool.AddBreathing);
        Buoc("5/5 Đồng bộ icon vàng (APPLY)",  RewardFxSetupTool.DongBoIconVangApply);

        if (EditorUtility.DisplayDialog("Save scene?",
            "Đã chạy xong 5 bước. LƯU SCENE ngay bây giờ (thay cho Ctrl+S)?", "Lưu", "Để tôi tự Ctrl+S"))
        {
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[UiJuiceMaster] Đã save scene.");
        }

        Doctor();   // khám lại, in bảng ✓/✗ chốt hạ
    }

    private static void Buoc(string ten, System.Action act)
    {
        Debug.Log($"━━━━━━━━ [UiJuiceMaster] BƯỚC {ten} ━━━━━━━━");
        try { act(); }
        catch (System.Exception e) { Debug.LogError($"[UiJuiceMaster] Bước '{ten}' LỖI: {e.Message}\n{e.StackTrace}"); }
    }

    // ════════════════════════════════════════════════════════════════════
    //  KHÁM BỆNH — chỉ đọc, không sửa
    // ════════════════════════════════════════════════════════════════════

    [MenuItem(MENU_DOCTOR)]
    public static void Doctor()
    {
        var r = new StringBuilder();
        r.AppendLine("╔══════════ UI JUICE — KHÁM BỆNH ══════════╗");

        // 1. Thư viện icon + icon art
        var lib = RewardIconLibrary.Instance;
        bool libOk = lib != null && lib.goldSprite != null;
        Dong(r, "Icon vàng art đã vào thư viện", libOk,
             libOk ? $"goldSprite = {lib.goldSprite.name}" : "Chạy bước 1 (Setup Reward Fly FX)");

        // 2. HUD đã thay icon mới?
        var hudGo  = GameObject.Find("Icon_Gold");
        var hudImg = hudGo != null ? hudGo.GetComponent<Image>() : null;
        string hudName = hudImg != null && hudImg.sprite != null ? hudImg.sprite.name : "(không thấy)";
        Dong(r, "HUD dùng icon vàng MỚI", hudName == "icon_gold",
             $"HUD đang dùng sprite '{hudName}'" + (hudName == "icon_gold" ? "" : " → chạy Đồng bộ icon vàng (APPLY)"));

        // 3. Popup: dim / chữ trắng / slot 230 / art puppet
        var popup = Object.FindFirstObjectByType<LevelUpPopupUI>(FindObjectsInactive.Include);
        if (popup == null)
        {
            Dong(r, "LevelUpPopupUI trong scene", false, "Mở đúng scene SCN_Farm rồi khám lại");
        }
        else
        {
            var so = new SerializedObject(popup);
            var rootP = so.FindProperty("popupRoot");
            var root  = rootP != null ? rootP.objectReferenceValue as GameObject : null;
            bool dimOk = root != null && root.transform.Find("V3_DimBackground") != null;
            Dong(r, "Nền dim xám", dimOk, dimOk ? "" : "Chạy bước 2 (Hoàn thiện popup)");

            var goldRowP = so.FindProperty("goldRewardRow");
            var goldRow  = goldRowP != null ? goldRowP.objectReferenceValue as GameObject : null;
            var tmp = goldRow != null ? goldRow.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            bool chuOk = tmp != null && tmp.color.r > 0.95f && tmp.color.g > 0.95f && tmp.color.b > 0.95f;
            Dong(r, "Chữ thưởng TRẮNG-to", chuOk, chuOk ? "" : "Chạy bước 2 (Hoàn thiện popup)");

            var slotsP = so.FindProperty("celebrationSlots");
            bool size230 = false, artOk = false;
            if (slotsP != null && slotsP.arraySize > 0)
            {
                var s0 = slotsP.GetArrayElementAtIndex(0).objectReferenceValue as CelebrationCharacterSlot;
                if (s0 != null)
                {
                    var rt = s0.transform as RectTransform;
                    size230 = rt != null && Mathf.Abs(rt.sizeDelta.x - 230f) < 1f;
                    var soS = new SerializedObject(s0);
                    var mP  = soS.FindProperty("puppetMaster");
                    artOk = mP != null && mP.objectReferenceValue != null;
                }
            }
            Dong(r, "Nhân vật popup 230px", size230, size230 ? "" : "Chạy bước 2 (Hoàn thiện popup)");
            Dong(r, "Nhân vật dùng art thật + thở", artOk, artOk ? "" : "Chạy bước 2 (Hoàn thiện popup)");
        }

        // 4. Quà ≥6 món (kiểm L4 làm mẫu)
        var l4 = AssetDatabase.LoadAssetAtPath<LevelRewardConfig>(
            "Assets/_Game/Farm/data/Lever Game/LevelReward_L4.asset");
        int soQua = l4 != null && l4.giftItems != null ? l4.giftItems.Count : -1;
        Dong(r, "Quà level ≥6 món (mẫu L4)", soQua >= 6,
             $"L4 đang có {soQua} món" + (soQua >= 6 ? "" : " → chạy Đổ quà V2 (APPLY)"));

        // 5. Khách du lịch có frame idle?
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Game/Farm/Prefabs/Tourists/Tourist_NV01.prefab");
        bool khachOk = false;
        if (pf != null)
        {
            var nb = pf.GetComponent<NpcBreathingIdle>();
            if (nb != null)
            {
                var soN = new SerializedObject(nb);
                var dfP = soN.FindProperty("downFrames");
                khachOk = dfP != null && dfP.arraySize > 0 &&
                          dfP.GetArrayElementAtIndex(0).objectReferenceValue != null;
            }
        }
        Dong(r, "Khách du lịch idle sống động (frame)", khachOk,
             khachOk ? "" : "Chạy menu Tourist Boat → Thêm hiệu ứng THỞ cho khách");

        // 6. Pháo hoa V2 bật?
        var cm = Object.FindFirstObjectByType<ConstructionManager>(FindObjectsInactive.Include);
        bool fxOk = false; string fxNote = "Không thấy ConstructionManager trong scene";
        if (cm != null)
        {
            var soC = new SerializedObject(cm);
            var p = soC.FindProperty("useCelebrationV2");
            fxOk = p != null && p.boolValue;
            fxNote = p == null ? "Field useCelebrationV2 không tồn tại — code chưa compile?"
                   : fxOk ? "Bật — xây xong 1 công trình (buildTime>0) sẽ thấy 4 đợt pháo sáng TRƯỚC nhà + bóng bay"
                          : "Đang TẮT — tick useCelebrationV2 trên ConstructionManager";
        }
        Dong(r, "Pháo hoa xây xong V2", fxOk, fxNote);

        // 7. Scene bẩn chưa save?
        bool dirty = EditorSceneManager.GetActiveScene().isDirty;
        Dong(r, "Scene đã save", !dirty, dirty ? "ĐANG CÓ THAY ĐỔI CHƯA LƯU → Ctrl+S!" : "");

        r.AppendLine("╚══════════════════════════════════════════╝");
        r.AppendLine("Mục nào ✗ → làm theo cột hướng dẫn, hoặc bấm '★★★ UI JUICE — LÀM TẤT CẢ (1 nút)'.");
        Debug.Log(r.ToString());
        EditorUtility.DisplayDialog("UI JUICE — Khám bệnh", "Đã in bảng ✓/✗ vào Console.\nChụp Console gửi Lead nếu còn mục ✗.", "OK");
    }

    private static void Dong(StringBuilder r, string ten, bool ok, string note)
    {
        r.AppendLine($"  {(ok ? "✓" : "✗")}  {ten,-38} {note}");
    }
}
#endif
