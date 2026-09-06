// ═══════════════════════════════════════════════════════════════════════════
//  STUDIO OVERLAY IN-GAME (F10) — DỰNG TIMELINE LEVEL 1..30 & TUTORIAL SCRUBBER
// ═══════════════════════════════════════════════════════════════════════════
#if UNITY_EDITOR || DEVELOPMENT_BUILD

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameProgressionStudioOverlay : MonoBehaviour
{
    private static GameProgressionStudioOverlay _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (_instance == null)
        {
            var go = new GameObject("[QA_Progression_Studio_F10]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<GameProgressionStudioOverlay>();
        }
    }

    private bool _showOverlay = false;
    private int _targetLevel = 1;
    private Vector2 _scrollPos;
    private int _tab = 0; // 0 = Timeline Lv1..30, 1 = Tutorial Steps, 2 = QA Cheats

    private static readonly Rect RECT_PANEL = new Rect(16f, 16f, 420f, 520f);
    private static GUIStyle _headerStyle;

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && (kb.f7Key.wasPressedThisFrame || kb.f10Key.wasPressedThisFrame || kb.f11Key.wasPressedThisFrame))
        {
            _showOverlay = !_showOverlay;
        }
#else
        if (Input.GetKeyDown(KeyCode.F7) || Input.GetKeyDown(KeyCode.F10) || Input.GetKeyDown(KeyCode.F11))
        {
            _showOverlay = !_showOverlay;
        }
#endif
    }

    private void OnGUI()
    {
        // Nút bấm nổi ở góc trái trên màn hình để mở Studio nhanh bằng chuột
        if (!_showOverlay)
        {
            if (GUI.Button(new Rect(12f, 12f, 110f, 32f), "🎬 STUDIO (F7)"))
            {
                _showOverlay = true;
            }
            return;
        }

        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                richText = true
            };
        }

        GUILayout.BeginArea(RECT_PANEL, GUI.skin.window);
        GUILayout.Label("🎬 <b>PROGRESSION STUDIO (F10)</b>", _headerStyle);

        int curLv = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 1;
        int curExp = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.CurrentExp : 0;
        int reqExp = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.RequiredExpCurrentLevel : 40;
        GUILayout.Label($"Cấp: <b>{curLv}</b> | EXP: {curExp}/{reqExp} | Tốc độ: {Time.timeScale:0.0}x");

        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(_tab == 0, "🎞️ Level 1..30", GUI.skin.button)) _tab = 0;
        if (GUILayout.Toggle(_tab == 1, "📋 Tutorial", GUI.skin.button)) _tab = 1;
        if (GUILayout.Toggle(_tab == 2, "⚡ QA / Cheats", GUI.skin.button)) _tab = 2;
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        _scrollPos = GUILayout.BeginScrollView(_scrollPos);

        if (_tab == 0) DrawLevelTab();
        else if (_tab == 1) DrawTutorialTab();
        else DrawQATab();

        GUILayout.EndScrollView();

        GUILayout.Space(4);
        if (GUILayout.Button("❌ Đóng bảng Studio (F10)"))
        {
            _showOverlay = false;
        }

        GUILayout.EndArea();
    }

    private void DrawLevelTab()
    {
        GUILayout.Label("<b>1. Nhảy tới Cấp độ (1..30):</b>", _headerStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Cấp {_targetLevel}", GUILayout.Width(60));
        _targetLevel = (int)GUILayout.HorizontalSlider(_targetLevel, 1, 30);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Lv 1")) _targetLevel = 1;
        if (GUILayout.Button("Lv 2")) _targetLevel = 2;
        if (GUILayout.Button("Lv 3")) _targetLevel = 3;
        if (GUILayout.Button("Lv 6")) _targetLevel = 6;
        if (GUILayout.Button("Lv 11")) _targetLevel = 11;
        if (GUILayout.Button("Lv 20")) _targetLevel = 20;
        if (GUILayout.Button("Lv 30")) _targetLevel = 30;
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        if (GUILayout.Button($"⏭️ Nhảy sang Cấp {_targetLevel}", GUILayout.Height(30)))
        {
            if (PlayerProgressManager.Instance != null)
            {
                PlayerProgressManager.Instance.ForceSetLevelExp(_targetLevel, 0);
            }
        }

        if (GUILayout.Button($"🎉 Preview Popup Lên Cấp {_targetLevel}", GUILayout.Height(30)))
        {
            var popup = FindFirstObjectByType<LevelUpPopupUI>(FindObjectsInactive.Include);
            if (popup != null) popup.DebugShowLevel(_targetLevel);
        }
    }

    private void DrawTutorialTab()
    {
        var tm = TutorialManager.Instance ?? FindFirstObjectByType<TutorialManager>(FindObjectsInactive.Include);
        if (tm == null)
        {
            GUILayout.Label("Không tìm thấy TutorialManager.");
            return;
        }

        int count = tm.TongSoBuoc;
        int cur = tm.ChiSoBuocHienTai;
        GUILayout.Label($"Bước hiện tại: [{cur}/{count}] <b>{tm.TenBuocHienTai}</b>", _headerStyle);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<< Bước trước") && cur > 0) tm.DebugNhayToiBuoc(cur - 1);
        if (GUILayout.Button("Bước sau >>") && cur < count - 1) tm.DebugNhayToiBuoc(cur + 1);
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.Label("<b>Danh sách các bước:</b>", _headerStyle);
        for (int i = 0; i < count; i++)
        {
            string name = tm.LayTenBuoc(i);
            bool isCur = (i == cur);
            if (GUILayout.Button((isCur ? "▶ " : "  ") + $"[{i:00}] {name}"))
            {
                tm.DebugNhayToiBuoc(i);
            }
        }
    }

    private void DrawQATab()
    {
        GUILayout.Label("<b>Tốc độ game:</b>", _headerStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("0x")) Time.timeScale = 0f;
        if (GUILayout.Button("1x")) Time.timeScale = 1f;
        if (GUILayout.Button("2x")) Time.timeScale = 2f;
        if (GUILayout.Button("5x")) Time.timeScale = 5f;
        if (GUILayout.Button("10x")) Time.timeScale = 10f;
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("<b>Tiền tệ & Kho:</b>", _headerStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+10k Vàng")) FarmEconomyManager.Instance?.AddGold(10000);
        if (GUILayout.Button("+100k Vàng")) FarmEconomyManager.Instance?.AddGold(100000);
        if (GUILayout.Button("+500 Kim Cương")) FarmEconomyManager.Instance?.AddGems(500);
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        if (GUILayout.Button("🔄 RESET VỀ LEVEL 1 TÂN THỦ", GUILayout.Height(30)))
        {
            var settings = FindFirstObjectByType<SettingsPopupUI>();
            if (settings != null) settings. OnResetProgressClicked();
        }
    }
}
#endif
