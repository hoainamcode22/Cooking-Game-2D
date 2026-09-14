#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools/Map45/22. Kiem Tra Analytics
///
/// Cua so tu kiem tra duong ong telemetry cho ban WebGL:
///   1. .jslib co nam dung Assets/Plugins/WebGL/ khong
///   2. WebGL template FarmAnalytics co ton tai va da chon trong Player Settings chua
///   3. index.html con de placeholder G-XXXXXXXXXX khong
///   4. Liet ke MOI ten event ma code dang ban (quet GameAnalytics.Log( toan du an)
///   5. Nut "ban thu 1 event"
///
/// Khong sua gi cua game — chi doc file va bao cao.
/// </summary>
public class AnalyticsCheckTool : EditorWindow
{
    // ── Duong dan chuan ──────────────────────────────────────────────────────
    private const string JslibPath      = "Assets/Plugins/WebGL/GameAnalytics.jslib";
    private const string TemplateDir    = "Assets/WebGLTemplates/FarmAnalytics";
    private const string TemplateIndex  = TemplateDir + "/index.html";
    private const string TemplateStyle  = TemplateDir + "/TemplateData/style.css";
    private const string TemplateValue  = "PROJECT:FarmAnalytics";
    private const string Placeholder    = "G-XXXXXXXXXX";
    private const string ApiFilePath    = "Assets/_Game/Scripts/Analytics/GameAnalytics.cs";
    private const string HooksFilePath  = "Assets/_Game/Scripts/Analytics/AnalyticsAutoHooks.cs";

    private Vector2 _scroll;
    private List<EventUsage> _events;
    private string _lastActionMessage;
    private MessageType _lastActionType = MessageType.Info;

    private struct EventUsage
    {
        public string Name;
        public int    Count;
        public string FirstFile;
        public int    FirstLine;
    }

    [MenuItem("Tools/Map45/22. Kiem Tra Analytics", false, 22)]
    public static void Open()
    {
        var win = GetWindow<AnalyticsCheckTool>(true, "Kiểm Tra Analytics", true);
        win.minSize = new Vector2(560, 480);
        win.RefreshEvents();
        win.Show();
    }

    // =========================================================================
    //  Ve giao dien
    // =========================================================================

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField("Đường ống Analytics cho bản WebGL", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Firebase Unity SDK KHÔNG chạy trên WebGL. Bản WebGL gửi dữ liệu qua cầu JavaScript:\n" +
            "C#  →  Plugins/WebGL/GameAnalytics.jslib  →  window.__gameAnalytics (index.html)  →  gtag.js (GA4).",
            MessageType.None);

        EditorGUILayout.Space(6);
        DrawChecklist();

        EditorGUILayout.Space(10);
        DrawEventList();

        EditorGUILayout.Space(10);
        DrawActions();

        EditorGUILayout.EndScrollView();
    }

    private void DrawChecklist()
    {
        EditorGUILayout.LabelField("1 · Kiểm tra file & cấu hình", EditorStyles.boldLabel);

        // --- jslib ---
        bool jslibOk = File.Exists(AbsPath(JslibPath));
        Row(jslibOk,
            jslibOk ? "Plugin .jslib nằm đúng chỗ: " + JslibPath
                    : "THIẾU " + JslibPath + " — Unity chỉ nhận .jslib khi nó nằm trong thư mục Plugins/WebGL.");

        if (jslibOk)
        {
            string js = SafeRead(AbsPath(JslibPath));
            bool hasAll = js.Contains("AnalyticsJsReady")
                       && js.Contains("AnalyticsJsLogEvent")
                       && js.Contains("AnalyticsJsSetUserProp")
                       && js.Contains("mergeInto");
            Row(hasAll, hasAll
                ? "Plugin khai báo đủ 3 hàm mà C# gọi sang."
                : "Plugin thiếu một trong các hàm AnalyticsJsReady / AnalyticsJsLogEvent / AnalyticsJsSetUserProp.");
        }

        // --- API C# ---
        Row(File.Exists(AbsPath(ApiFilePath)),   "API C#: " + ApiFilePath);
        Row(File.Exists(AbsPath(HooksFilePath)), "Móc tự động: " + HooksFilePath);

        // --- template ---
        bool tplOk = File.Exists(AbsPath(TemplateIndex));
        Row(tplOk, tplOk ? "Template WebGL tồn tại: " + TemplateIndex
                         : "THIẾU " + TemplateIndex);
        Row(File.Exists(AbsPath(TemplateStyle)), "TemplateData/style.css");

        // --- template da duoc chon chua ---
        string current = PlayerSettings.WebGL.template;
        bool selected = string.Equals(current, TemplateValue, StringComparison.Ordinal);
        Row(selected, selected
            ? "Player Settings đang dùng template FarmAnalytics."
            : "Player Settings đang dùng template \"" + current + "\" — PHẢI đổi sang FarmAnalytics, nếu không trang web sẽ không có gtag.");

        if (!selected && tplOk)
        {
            if (GUILayout.Button("→ Chọn template FarmAnalytics ngay"))
            {
                PlayerSettings.WebGL.template = TemplateValue;
                AssetDatabase.SaveAssets();
                SetMessage("Đã đặt WebGL Template = FarmAnalytics.", MessageType.Info);
            }
        }

        // --- placeholder GA4 ---
        if (tplOk)
        {
            string html = SafeRead(AbsPath(TemplateIndex));
            bool stillPlaceholder = Regex.IsMatch(
                html, @"^\s*var\s+GA4_MEASUREMENT_ID\s*=\s*""" + Placeholder + @"""", RegexOptions.Multiline);

            if (stillPlaceholder)
            {
                EditorGUILayout.HelpBox(
                    "CHƯA ĐIỀN MEASUREMENT ID.\n" +
                    "Mở " + TemplateIndex + " và thay \"" + Placeholder + "\" bằng ID thật (dạng G-ABC1234567).\n" +
                    "Lấy tại: analytics.google.com → Admin → Data streams → luồng Web của bạn.\n" +
                    "Khi chưa điền, bản build sẽ chạy bình thường nhưng KHÔNG có dữ liệu nào về dashboard.",
                    MessageType.Error);

                if (GUILayout.Button("Mở index.html để sửa"))
                    InternalEditorOpen(TemplateIndex);
            }
            else
            {
                var m = Regex.Match(html, @"^\s*var\s+GA4_MEASUREMENT_ID\s*=\s*""([^""]*)""", RegexOptions.Multiline);
                Row(true, "Measurement ID đã điền: " + (m.Success ? m.Groups[1].Value : "(không đọc được)"));
            }

            // Chi tinh dong KHAI BAO that su (^var ...), khong dinh vao dong vi du trong comment.
            bool fbOn = Regex.IsMatch(html, @"^\s*var\s+FIREBASE_CONFIG\s*=\s*\{", RegexOptions.Multiline);
            EditorGUILayout.LabelField("   Firebase JS SDK: " + (fbOn ? "BẬT" : "tắt (chỉ dùng GA4 — hoàn toàn ổn)"));
        }

        // --- nen tang dang chon ---
        bool onWebGL = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
        EditorGUILayout.LabelField("   Build target hiện tại: " + EditorUserBuildSettings.activeBuildTarget +
                                   (onWebGL ? "" : "  (đổi sang WebGL khi build thật)"));
    }

    private void DrawEventList()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("2 · Các event mà code đang bắn", EditorStyles.boldLabel);
        if (GUILayout.Button("Quét lại", GUILayout.Width(80))) RefreshEvents();
        EditorGUILayout.EndHorizontal();

        if (_events == null) RefreshEvents();

        if (_events.Count == 0)
        {
            EditorGUILayout.HelpBox("Không tìm thấy lời gọi GameAnalytics.Log( nào.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField($"   Tổng {_events.Count} tên event khác nhau:");

        foreach (var e in _events)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("• " + e.Name, GUILayout.Width(240));
            EditorGUILayout.LabelField($"×{e.Count}", GUILayout.Width(40));
            if (GUILayout.Button(Path.GetFileName(e.FirstFile) + ":" + e.FirstLine, EditorStyles.miniButton))
                InternalEditorOpen(e.FirstFile, e.FirstLine);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Chép danh sách tên event vào clipboard"))
        {
            var sb = new StringBuilder();
            foreach (var e in _events) sb.AppendLine(e.Name);
            EditorGUIUtility.systemCopyBuffer = sb.ToString();
            SetMessage("Đã chép " + _events.Count + " tên event.", MessageType.Info);
        }
    }

    private void DrawActions()
    {
        EditorGUILayout.LabelField("3 · Thử nghiệm", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Trong Editor, event KHÔNG bay lên Google — nó chỉ in ra Console dạng\n" +
            "[Analytics] #1  ten_event  {\"session_id\":\"…\",\"event_seq\":1,…}\n" +
            "Đó là cách kiểm tra tên và tham số có đúng luật GA4 không.",
            MessageType.Info);

        if (GUILayout.Button("🔫  Bắn thử 1 event", GUILayout.Height(30)))
        {
            GameAnalytics.Log("editor_test_event",
                ("source", "AnalyticsCheckTool"),
                ("unity", Application.unityVersion),
                ("stamp", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")));

            SetMessage("Đã bắn 'editor_test_event' — mở Console xem dòng [Analytics].", MessageType.Info);
        }

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = GameAnalytics.Enabled;
        if (GUILayout.Button("Tắt thu thập (PlayerPrefs)")) { GameAnalytics.Enabled = false; SetMessage("Đã TẮT analytics.", MessageType.Warning); }
        GUI.enabled = !GameAnalytics.Enabled;
        if (GUILayout.Button("Bật thu thập")) { GameAnalytics.Enabled = true; SetMessage("Đã BẬT analytics.", MessageType.Info); }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("   Trạng thái: " + (GameAnalytics.Enabled ? "ĐANG BẬT" : "ĐANG TẮT") +
                                   "   ·   session: " + GameAnalytics.SessionId.Substring(0, 8) +
                                   "   ·   đã bắn: " + GameAnalytics.EventCount);

        if (!string.IsNullOrEmpty(_lastActionMessage))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(_lastActionMessage, _lastActionType);
        }
    }

    // =========================================================================
    //  Quet ma nguon tim GameAnalytics.Log("...")
    // =========================================================================

    private void RefreshEvents()
    {
        var found = new Dictionary<string, EventUsage>(StringComparer.Ordinal);

        // Quet moi script C# trong du an qua AssetDatabase (khong dung dinh dang duong dan he dieu hanh).
        string[] guids = AssetDatabase.FindAssets("t:MonoScript");
        var rx = new Regex(@"GameAnalytics\s*\.\s*Log\s*\(\s*""([^""]*)""", RegexOptions.Compiled);

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                continue;
            if (assetPath.EndsWith("/GameAnalytics.cs", StringComparison.OrdinalIgnoreCase))
                continue; // chinh file API, khong phai noi phat event

            string abs = AbsPath(assetPath);
            if (!File.Exists(abs)) continue;

            string[] lines;
            try { lines = File.ReadAllLines(abs); }
            catch (Exception) { continue; }

            for (int i = 0; i < lines.Length; i++)
            {
                foreach (Match m in rx.Matches(lines[i]))
                {
                    string name = m.Groups[1].Value;
                    if (found.TryGetValue(name, out var u))
                    {
                        u.Count++;
                        found[name] = u;
                    }
                    else
                    {
                        found[name] = new EventUsage
                        {
                            Name = name,
                            Count = 1,
                            FirstFile = assetPath,
                            FirstLine = i + 1
                        };
                    }
                }
            }
        }

        _events = new List<EventUsage>(found.Values);
        _events.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
    }

    // =========================================================================
    //  Tien ich
    // =========================================================================

    private static string AbsPath(string assetPath)
    {
        // Application.dataPath = <Project>/Assets  ->  bo "Assets" o dau assetPath.
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string SafeRead(string abs)
    {
        try { return File.ReadAllText(abs); }
        catch (Exception) { return string.Empty; }
    }

    private static void InternalEditorOpen(string assetPath, int line = 1)
    {
        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (obj != null) AssetDatabase.OpenAsset(obj, line);
        else EditorUtility.RevealInFinder(AbsPath(assetPath));
    }

    private void Row(bool ok, string label)
    {
        EditorGUILayout.LabelField((ok ? "✔  " : "✘  ") + label,
            ok ? EditorStyles.label : EditorStyles.boldLabel);
    }

    private void SetMessage(string msg, MessageType type)
    {
        _lastActionMessage = msg;
        _lastActionType = type;
        Repaint();
    }
}
#endif
