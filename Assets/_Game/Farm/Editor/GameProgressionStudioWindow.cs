using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// BÀN DỰNG TIMELINE & KỊCH BẢN GAME (LEVEL 1 -> LEVEL 30 & TUTORIAL STORYBOARD)
/// Giúp dev/designer duyệt và test từng phân đoạn (Chapter / Segment) như dựng video Premiere/CapCut.
/// </summary>
public class GameProgressionStudioWindow : EditorWindow
{
    private enum StudioTab
    {
        TimelineLevel1To30,
        TutorialStoryboard,
        EconomyAndQA
    }

    private StudioTab _currentTab = StudioTab.TimelineLevel1To30;
    private Vector2 _scrollPos;
    private int _scrubLevel = 1;
    private string _tutorialSearchText = "";
    private int _selectedChapter = 0;

    private readonly string[] _chapterNames = new string[]
    {
        "🌾 [Chương 1] Cấp 1 - 2: Tân Thủ & Khởi Nghiệp",
        "🐔 [Chương 2] Cấp 3 - 5: Chăn Nuôi & Nông Trại",
        "🍳 [Chương 3] Cấp 6 - 10: Bếp Nấu Ăn & Chế Biến",
        "🚢 [Chương 4] Cấp 11 - 20: Tàu Du Lịch & Khách Mua",
        "👑 [Chương 5] Cấp 21 - 30: Đại Gia Trang Đỉnh Cao"
    };

    [MenuItem("Tools/Farm Game/★ Progression & Tutorial Timeline Studio (Cap 1 - 30)", false, 0)]
    [MenuItem("Tools/Progression Studio", false, 0)]
    [MenuItem("Window/Progression & Tutorial Studio", false, 100)]
    public static void OpenWindow()
    {
        var window = GetWindow<GameProgressionStudioWindow>("Progression Studio");
        window.minSize = new Vector2(650, 680);
        window.Show();
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawTabBar();

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        switch (_currentTab)
        {
            case StudioTab.TimelineLevel1To30:
                DrawTimelineLevelScrubber();
                break;
            case StudioTab.TutorialStoryboard:
                DrawTutorialStoryboard();
                break;
            case StudioTab.EconomyAndQA:
                DrawEconomyAndQAControls();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎬 <b>GAME PROGRESSION & TUTORIAL TIMELINE STUDIO</b>", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, richText = true });
        EditorGUILayout.LabelField("Công cụ kiểm thử & căn chỉnh phân đoạn game từ Cấp 1 đến Cấp 30 như dựng Timeline video.", EditorStyles.miniLabel);

        if (Application.isPlaying)
        {
            int currentLevel = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 1;
            int currentExp = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.CurrentExp : 0;
            int reqExp = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.RequiredExpCurrentLevel : 40;
            EditorGUILayout.HelpBox($"🎮 ĐANG PLAY GAME — Cấp hiện tại: {currentLevel} | EXP: {currentExp}/{reqExp} | Tốc độ TimeScale: {Time.timeScale:0.0}x", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("💡 Bật nút PLAY trong Unity để kích hoạt tính năng nhảy Level, preview Popup Lên Cấp và tua thời gian trực tiếp.", MessageType.None);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawTabBar()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(_currentTab == StudioTab.TimelineLevel1To30, "🎞️ TIMELINE CẤP 1 - 30", EditorStyles.toolbarButton)) _currentTab = StudioTab.TimelineLevel1To30;
        if (GUILayout.Toggle(_currentTab == StudioTab.TutorialStoryboard, "📋 TUTORIAL STORYBOARD", EditorStyles.toolbarButton)) _currentTab = StudioTab.TutorialStoryboard;
        if (GUILayout.Toggle(_currentTab == StudioTab.EconomyAndQA, "⚡ TỐC ĐỘ & QA CHEAT", EditorStyles.toolbarButton)) _currentTab = StudioTab.EconomyAndQA;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(6);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // TAB 1: TIMELINE CẤP 1 - 30
    // ═════════════════════════════════════════════════════════════════════════
    private void DrawTimelineLevelScrubber()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("<b>1. CHỌN PHÂN ĐOẠN PHIM / CHƯƠNG (SEGMENT)</b>", new GUIStyle(EditorStyles.label) { richText = true });
        
        int oldChapter = _selectedChapter;
        _selectedChapter = EditorGUILayout.Popup(_selectedChapter, _chapterNames);
        if (oldChapter != _selectedChapter)
        {
            switch (_selectedChapter)
            {
                case 0: _scrubLevel = 1; break;
                case 1: _scrubLevel = 3; break;
                case 2: _scrubLevel = 6; break;
                case 3: _scrubLevel = 11; break;
                case 4: _scrubLevel = 21; break;
            }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("<b>2. THANH TRƯỢT TIMELINE SCRUBBER (CẤP 1 ➔ 30)</b>", new GUIStyle(EditorStyles.label) { richText = true });

        EditorGUILayout.BeginHorizontal();
        _scrubLevel = EditorGUILayout.IntSlider(_scrubLevel, 1, 30);
        EditorGUILayout.EndHorizontal();

        // Nút nhảy nhanh các mốc quan trọng
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Lv 1 (Tân thủ)")) _scrubLevel = 1;
        if (GUILayout.Button("Lv 2 (Gặt lúa)")) _scrubLevel = 2;
        if (GUILayout.Button("Lv 3 (Nuôi gà)")) _scrubLevel = 3;
        if (GUILayout.Button("Lv 6 (Mở Bếp)")) _scrubLevel = 6;
        if (GUILayout.Button("Lv 11 (Tàu thủy)")) _scrubLevel = 11;
        if (GUILayout.Button("Lv 20 (Chế biến)")) _scrubLevel = 20;
        if (GUILayout.Button("Lv 30 (Max)")) _scrubLevel = 30;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // Nút thực thi
        GUI.enabled = Application.isPlaying;
        EditorGUILayout.BeginHorizontal();
        
        GUI.backgroundColor = new Color(0.3f, 0.8f, 1.0f);
        if (GUILayout.Button($"⏭️ NHẢY TỚI CẤP {_scrubLevel} NGAY", GUILayout.Height(36)))
        {
            JumpToLevel(_scrubLevel);
        }

        GUI.backgroundColor = new Color(1.0f, 0.75f, 0.2f);
        if (GUILayout.Button($"🎉 PREVIEW POPUP LÊN CẤP {_scrubLevel}", GUILayout.Height(36)))
        {
            PreviewLevelUpPopup(_scrubLevel);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
        GUI.enabled = true;

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);
        DrawLevelDetailsCard(_scrubLevel);
    }

    private void DrawLevelDetailsCard(int level)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"🔍 <b>THÔNG TIN NỘI DUNG MỞ KHOÁ Ở CẤP {level}:</b>", new GUIStyle(EditorStyles.boldLabel) { richText = true });

        string rewardAssetPath = $"Assets/_Game/Farm/data/Lever Game/LevelReward_L{level}.asset";
        var rewardConfig = AssetDatabase.LoadAssetAtPath<LevelRewardConfig>(rewardAssetPath);

        if (rewardConfig != null)
        {
            EditorGUILayout.LabelField($"• Vàng thưởng: <b>+{rewardConfig.giftGold}</b> | Kim Cương: <b>+{rewardConfig.giftGems}</b>", new GUIStyle(EditorStyles.label) { richText = true });
            
            if (rewardConfig.giftItems != null && rewardConfig.giftItems.Count > 0)
            {
                EditorGUILayout.LabelField("• Vật phẩm tặng kèm:", EditorStyles.boldLabel);
                foreach (var gift in rewardConfig.giftItems)
                {
                    EditorGUILayout.LabelField($"   - {gift.displayName} (x{gift.amount})");
                }
            }

            var unlocks = rewardConfig.GetUnlockEntries();
            if (unlocks != null && unlocks.Count > 0)
            {
                EditorGUILayout.LabelField("• Tính năng / Nông sản vừa mở khoá:", EditorStyles.boldLabel);
                foreach (var unlock in unlocks)
                {
                    EditorGUILayout.LabelField($"   🔓 {unlock.label}");
                }
            }
            else
            {
                EditorGUILayout.LabelField("• Mở rộng giới hạn sản xuất và các đơn hàng giá trị cao.");
            }

            if (GUILayout.Button("📂 Mở file config LevelReward này trong Project", GUILayout.Width(300)))
            {
                EditorGUIUtility.PingObject(rewardConfig);
                Selection.activeObject = rewardConfig;
            }
        }
        else
        {
            EditorGUILayout.LabelField($"Chưa tìm thấy file config '{rewardAssetPath}'.", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // TAB 2: TUTORIAL STORYBOARD
    // ═════════════════════════════════════════════════════════════════════════
    private void DrawTutorialStoryboard()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("<b>KỊCH BẢN TỪNG BƯỚC HƯỚNG DẪN (TUTORIAL FLOW)</b>", new GUIStyle(EditorStyles.label) { richText = true });
        
        EditorGUILayout.BeginHorizontal();
        _tutorialSearchText = EditorGUILayout.TextField("Tìm kiếm bước:", _tutorialSearchText);
        if (GUILayout.Button("Xoá", GUILayout.Width(50))) _tutorialSearchText = "";
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);

        var tutorialManager = TutorialManager.Instance ?? Object.FindFirstObjectByType<TutorialManager>(FindObjectsInactive.Include);
        IReadOnlyList<TutorialStepData> steps = null;

        if (tutorialManager != null)
        {
            steps = tutorialManager.DanhSachBuoc;
        }

        if (steps == null || steps.Count == 0)
        {
            EditorGUILayout.HelpBox("Chưa tìm thấy danh sách bước trong TutorialManager. Vui lòng mở scene SCN_Farm.", MessageType.Warning);
            return;
        }

        int currentIndex = (tutorialManager != null && Application.isPlaying) ? tutorialManager.ChiSoBuocHienTai : -1;

        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (step == null) continue;

            if (!string.IsNullOrEmpty(_tutorialSearchText))
            {
                if (!step.name.ToLower().Contains(_tutorialSearchText.ToLower()) &&
                    !step.npcText.ToLower().Contains(_tutorialSearchText.ToLower()) &&
                    !step.targetID.ToLower().Contains(_tutorialSearchText.ToLower()))
                {
                    continue;
                }
            }

            bool isCurrent = (i == currentIndex);
            GUI.backgroundColor = isCurrent ? new Color(0.6f, 1f, 0.6f) : Color.white;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"<b>[Bước {i:00}] {step.name}</b>" + (isCurrent ? " 📍 (ĐANG CHẠY)" : ""), new GUIStyle(EditorStyles.label) { richText = true });

            if (GUILayout.Button("🔍 Ping Asset", GUILayout.Width(90)))
            {
                EditorGUIUtility.PingObject(step);
                Selection.activeObject = step;
            }

            if (Application.isPlaying)
            {
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
                if (GUILayout.Button("▶ Nhảy tới bước này", GUILayout.Width(130)))
                {
                    tutorialManager.DebugNhayToiBuoc(i);
                }
                GUI.backgroundColor = isCurrent ? new Color(0.6f, 1f, 0.6f) : Color.white;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"💬 <b>Thoại:</b> \"{step.npcText}\"", new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true });
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"🎯 <b>Target:</b> {(!string.IsNullOrEmpty(step.targetID) ? step.targetID : "(Không)")}", new GUIStyle(EditorStyles.miniLabel) { richText = true }, GUILayout.Width(200));
            EditorGUILayout.LabelField($"⏳ <b>Chờ:</b> {step.waitAction}", EditorStyles.miniLabel, GUILayout.Width(160));
            if (!string.IsNullOrEmpty(step.dragToTargetId))
            {
                EditorGUILayout.LabelField($"👉 <b>Kéo tới:</b> {step.dragToTargetId}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(2);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // TAB 3: TỐC ĐỘ & QA CHEAT
    // ═════════════════════════════════════════════════════════════════════════
    private void DrawEconomyAndQAControls()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("<b>1. ĐIỀU CHỈNH TỐC ĐỘ THỜI GIAN (GAME SPEED)</b>", new GUIStyle(EditorStyles.label) { richText = true });

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("⏸️ Tạm dừng (0x)")) Time.timeScale = 0f;
        if (GUILayout.Button("▶️ Chuẩn (1x)")) Time.timeScale = 1.0f;
        if (GUILayout.Button("⏩ Nhanh (2x)")) Time.timeScale = 2.0f;
        if (GUILayout.Button("🚀 Siêu tốc (5x)")) Time.timeScale = 5.0f;
        if (GUILayout.Button("⚡ MAX (10x)")) Time.timeScale = 10.0f;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("<b>2. TIỀN TỆ & TÀI NGUYÊN (QA CHEAT)</b>", new GUIStyle(EditorStyles.label) { richText = true });

        GUI.enabled = Application.isPlaying;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("💰 +10,000 Vàng"))
        {
            FarmEconomyManager.Instance?.AddGold(10000);
        }
        if (GUILayout.Button("💰 +100,000 Vàng"))
        {
            FarmEconomyManager.Instance?.AddGold(100000);
        }
        if (GUILayout.Button("💎 +500 Kim Cương"))
        {
            FarmEconomyManager.Instance?.AddGems(500);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📦 Cấp full hạt giống khởi đầu"))
        {
            var starter = Object.FindFirstObjectByType<StarterInventorySetup>();
            if (starter != null)
            {
                PlayerPrefs.DeleteKey("STARTER_ITEMS_GIVEN");
                starter.SendMessage("GiveStarterItems", UnityEngine.SendMessageOptions.DontRequireReceiver);
            }
        }
        if (GUILayout.Button("🧹 Xoá sạch Kho (Làm trống)"))
        {
            WarehouseManager.Instance?.XoaSaveVaLamTrongKho();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("🔄 RESET TIẾN TRÌNH VỀ LEVEL 1 (NHƯ MỚI CÀI GAME)", GUILayout.Height(32)))
        {
            var settings = Object.FindFirstObjectByType<SettingsPopupUI>();
            if (settings != null)
            {
                settings.OnResetProgressClicked();
            }
        }
        GUI.backgroundColor = Color.white;

        GUI.enabled = true;
        EditorGUILayout.EndVertical();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // ACTIONS
    // ═════════════════════════════════════════════════════════════════════════
    private static void JumpToLevel(int targetLevel)
    {
        if (PlayerProgressManager.Instance != null)
        {
            PlayerProgressManager.Instance.ForceSetLevelExp(targetLevel, 0);
            Debug.Log($"[Studio] Đã chuyển game sang Cấp {targetLevel}.");
        }
        else if (FarmLevelManager.Instance != null)
        {
            FarmLevelManager.Instance.SetLevel(targetLevel);
            Debug.Log($"[Studio] Đã chuyển FarmLevelManager sang Cấp {targetLevel}.");
        }
    }

    private static void PreviewLevelUpPopup(int targetLevel)
    {
        var popup = Object.FindFirstObjectByType<LevelUpPopupUI>(FindObjectsInactive.Include);
        if (popup != null)
        {
            popup.DebugShowLevel(targetLevel);
            Debug.Log($"[Studio] Đang mở preview Popup Lên Cấp {targetLevel}.");
        }
        else
        {
            Debug.LogWarning("[Studio] Không tìm thấy LevelUpPopupUI trong scene.");
        }
    }
}
