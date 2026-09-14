using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// MOT cong duy nhat de ca game ban telemetry. Goi tu bat ky dau:
///
///   GameAnalytics.Log("tutorial_done");
///   GameAnalytics.Log("level_up", "level", 7);
///   GameAnalytics.Log("order_delivered", ("order_id", id), ("gold", 250), ("late", false));
///   GameAnalytics.SetUserProperty("player_level", "7");
///
/// DUONG DI THAT SU:
///   WebGL player -> Plugins/WebGL/GameAnalytics.jslib -> window.__gameAnalytics (index.html)
///   -> gtag.js (Google Analytics 4) va/hoac Firebase JS SDK.
///   Firebase Unity SDK KHONG chay tren WebGL nen day la duong duy nhat.
///
/// TREN NEN TANG KHAC:
///   - Trong Editor: in ra Console (de tu kiem tra event co ban dung khong).
///   - Trong player Android/iOS/Standalone: no-op hoan toan, khong ton gi.
///   Nghia la file nay BIEN DICH DUOC ke ca khi may chua cai WebGL Build Support.
/// </summary>
public static class GameAnalytics
{
    // =========================================================================
    //  Cau hinh
    // =========================================================================

    private const string PrefKeyEnabled = "ANALYTICS_ENABLED";
    private const string PrefKeyFirstOpen = "ANALYTICS_FIRST_OPEN_DONE";

    /// <summary>Gioi han cua GA4 — vuot thi bi cat/tu choi, nen ta tu kep truoc.</summary>
    private const int MaxEventNameLen = 40;
    private const int MaxParamNameLen = 40;
    private const int MaxParamValueLen = 100;

    /// <summary>GA4 chi cho toi da 25 tham so moi event.</summary>
    private const int MaxParamsPerEvent = 25;

    // =========================================================================
    //  Trang thai phien
    // =========================================================================

    private static string _sessionId;
    private static int _eventCounter;
    private static bool _initialised;
    private static int _enabledCache = -1; // -1 = chua doc PlayerPrefs

    /// <summary>GUID tao luc boot. Di kem MOI event => chung minh duoc "1 phien choi" tren dashboard.</summary>
    public static string SessionId
    {
        get
        {
            EnsureInit();
            return _sessionId;
        }
    }

    /// <summary>So event da ban trong phien nay (tang dan, khong bao gio lui).</summary>
    public static int EventCount => _eventCounter;

    /// <summary>TRUE neu day la lan dau may nay mo game (dua tren PlayerPrefs, khong chinh xac tuyet doi).</summary>
    public static bool IsFirstOpen { get; private set; }

    /// <summary>
    /// Cong tac tong. Tat = khong event nao roi khoi may. Luu vao PlayerPrefs nen nho qua cac phien.
    /// Dung cho nut "Tat thu thap du lieu" trong Settings neu can.
    /// </summary>
    public static bool Enabled
    {
        get
        {
            if (_enabledCache < 0)
                _enabledCache = PlayerPrefs.GetInt(PrefKeyEnabled, 1);
            return _enabledCache != 0;
        }
        set
        {
            _enabledCache = value ? 1 : 0;
            PlayerPrefs.SetInt(PrefKeyEnabled, _enabledCache);
            PlayerPrefs.Save();
        }
    }

    /// <summary>TRUE neu trang web dang host thuc su co gtag/firebase. Luon FALSE ngoai WebGL.</summary>
    public static bool JsReady
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { return AnalyticsJsReady() != 0; }
            catch (Exception) { return false; }
#else
            return false;
#endif
        }
    }

    // =========================================================================
    //  Extern sang JavaScript — CHI ton tai khi build WebGL
    // =========================================================================

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern int  AnalyticsJsReady();
    [DllImport("__Internal")] private static extern void AnalyticsJsLogEvent(string name, string jsonParams);
    [DllImport("__Internal")] private static extern void AnalyticsJsSetUserProp(string key, string val);
#endif

    // =========================================================================
    //  Khoi tao
    // =========================================================================

    private static void EnsureInit()
    {
        if (_initialised) return;
        _initialised = true;

        _sessionId = Guid.NewGuid().ToString("N");
        _eventCounter = 0;

        IsFirstOpen = PlayerPrefs.GetInt(PrefKeyFirstOpen, 0) == 0;
        if (IsFirstOpen)
        {
            PlayerPrefs.SetInt(PrefKeyFirstOpen, 1);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Goi som (AnalyticsAutoHooks lo viec nay) de session id co ngay tu frame dau.
    /// Goi nhieu lan cung vo hai.
    /// </summary>
    public static void Init() => EnsureInit();

    // =========================================================================
    //  API cong khai — 3 dang, chon dang nao gon nhat cho cho goi
    // =========================================================================

    /// <summary>Event khong tham so. Vd: GameAnalytics.Log("tutorial_done");</summary>
    public static void Log(string eventName)
    {
        Send(eventName, null, 0);
    }

    /// <summary>
    /// Duong nhanh 1 tham so (khong cap phat mang tuple).
    /// Vd: GameAnalytics.Log("level_up", "level", 7);
    /// </summary>
    public static void Log(string eventName, string key, object value)
    {
        var one = new KeyValuePair<string, object>[1];
        one[0] = new KeyValuePair<string, object>(key, value);
        Send(eventName, one, 1);
    }

    /// <summary>
    /// Dang tong quat. Vd:
    ///   GameAnalytics.Log("order_delivered", ("gold", 250), ("late", false), ("npc", "Lan"));
    /// </summary>
    public static void Log(string eventName, params (string key, object value)[] p)
    {
        if (p == null || p.Length == 0)
        {
            Send(eventName, null, 0);
            return;
        }

        var arr = new KeyValuePair<string, object>[p.Length];
        for (int i = 0; i < p.Length; i++)
            arr[i] = new KeyValuePair<string, object>(p[i].key, p[i].value);

        Send(eventName, arr, arr.Length);
    }

    /// <summary>Dat user property (hien o GA4 &gt; Audiences / Reports). Value bi kep 100 ky tu.</summary>
    public static void SetUserProperty(string key, object value)
    {
        EnsureInit();
        if (!Enabled) return;

        string k = SanitiseName(key, MaxParamNameLen, "user property");
        if (string.IsNullOrEmpty(k)) return;

        string v = ClampString(ValueToString(value));

#if UNITY_WEBGL && !UNITY_EDITOR
        try { AnalyticsJsSetUserProp(k, v); }
        catch (Exception) { /* nuot: analytics khong duoc lam chet game */ }
#elif UNITY_EDITOR
        Debug.Log($"[Analytics] user_property  {k} = {v}");
#endif
    }

    // =========================================================================
    //  Loi
    // =========================================================================

    private static void Send(string eventName, KeyValuePair<string, object>[] p, int count)
    {
        EnsureInit();
        if (!Enabled) return;

        string name = SanitiseEventName(eventName);
        if (string.IsNullOrEmpty(name)) return;

        _eventCounter++;

        string json = BuildJson(p, count);

#if UNITY_WEBGL && !UNITY_EDITOR
        try { AnalyticsJsLogEvent(name, json); }
        catch (Exception) { /* nuot */ }
#elif UNITY_EDITOR
        Debug.Log($"[Analytics] #{_eventCounter}  {name}  {json}");
#else
        // Player Android/iOS/Standalone: khong lam gi. Tranh canh bao bien khong dung.
        if (json == null) { }
#endif
    }

    // =========================================================================
    //  JSON tu viet tay (du an KHONG co Newtonsoft, va JsonUtility khong lam duoc
    //  dictionary kieu hon hop). Chi can dung 6 ky tu escape bat buoc cua JSON.
    // =========================================================================

    private static readonly StringBuilder _sb = new StringBuilder(512);

    private static string BuildJson(KeyValuePair<string, object>[] p, int count)
    {
        _sb.Length = 0;
        _sb.Append('{');

        // Hai truong nay di kem MOI event — day la thu chung minh duoc "phien choi"
        // tren dashboard (gom theo session_id, sap xep theo event_seq).
        AppendKey("session_id");
        AppendJsonString(_sessionId);
        _sb.Append(',');
        AppendKey("event_seq");
        _sb.Append(_eventCounter.ToString(CultureInfo.InvariantCulture));

        int written = 2;

        if (p != null)
        {
            for (int i = 0; i < count; i++)
            {
                if (written >= MaxParamsPerEvent)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[Analytics] Event co qua {MaxParamsPerEvent} tham so — cat bot phan thua.");
#endif
                    break;
                }

                string key = SanitiseName(p[i].Key, MaxParamNameLen, "param");
                if (string.IsNullOrEmpty(key)) continue;
                if (key == "session_id" || key == "event_seq") continue; // khong cho ghi de

                _sb.Append(',');
                AppendKey(key);
                AppendJsonValue(p[i].Value);
                written++;
            }
        }

        _sb.Append('}');
        return _sb.ToString();
    }

    private static void AppendKey(string key)
    {
        AppendJsonString(key);
        _sb.Append(':');
    }

    private static void AppendJsonValue(object v)
    {
        switch (v)
        {
            case null:
                _sb.Append("null");
                return;

            case bool b:
                _sb.Append(b ? "true" : "false");
                return;

            case int i:
                _sb.Append(i.ToString(CultureInfo.InvariantCulture));
                return;

            case long l:
                _sb.Append(l.ToString(CultureInfo.InvariantCulture));
                return;

            case float f:
                if (float.IsNaN(f) || float.IsInfinity(f)) { _sb.Append('0'); return; }
                _sb.Append(f.ToString("0.####", CultureInfo.InvariantCulture));
                return;

            case double d:
                if (double.IsNaN(d) || double.IsInfinity(d)) { _sb.Append('0'); return; }
                _sb.Append(d.ToString("0.####", CultureInfo.InvariantCulture));
                return;

            default:
                AppendJsonString(ClampString(ValueToString(v)));
                return;
        }
    }

    private static string ValueToString(object v)
    {
        if (v == null) return string.Empty;
        if (v is string s) return s;
        if (v is float f) return f.ToString("0.####", CultureInfo.InvariantCulture);
        if (v is double d) return d.ToString("0.####", CultureInfo.InvariantCulture);
        if (v is IFormattable fmt) return fmt.ToString(null, CultureInfo.InvariantCulture);
        return v.ToString();
    }

    private static void AppendJsonString(string s)
    {
        _sb.Append('"');
        if (!string.IsNullOrEmpty(s))
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '"':  _sb.Append("\\\""); break;
                    case '\\': _sb.Append("\\\\"); break;
                    case '\b': _sb.Append("\\b");  break;
                    case '\f': _sb.Append("\\f");  break;
                    case '\n': _sb.Append("\\n");  break;
                    case '\r': _sb.Append("\\r");  break;
                    case '\t': _sb.Append("\\t");  break;
                    default:
                        // Ky tu dieu khien phai escape \u00XX; chu co dau tieng Viet
                        // (U+00C0..) de nguyen — JSON la UTF-8, khong can escape.
                        if (c < ' ')
                            _sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            _sb.Append(c);
                        break;
                }
            }
        }
        _sb.Append('"');
    }

    // =========================================================================
    //  Lam sach theo luat GA4 — SUA CHUA chu khong tu choi.
    //  Luat: chu thuong, chi [a-z0-9_], bat dau bang chu cai, toi da 40 ky tu.
    // =========================================================================

    private static string SanitiseEventName(string raw)
    {
        string clean = SanitiseName(raw, MaxEventNameLen, "event");
        if (string.IsNullOrEmpty(clean))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[Analytics] Ten event rong/khong hop le: '{raw}' — bo qua event nay.");
#endif
            return null;
        }
        return clean;
    }

    private static readonly StringBuilder _nameSb = new StringBuilder(64);

    private static string SanitiseName(string raw, int maxLen, string kindForWarning)
    {
        if (string.IsNullOrEmpty(raw)) return null;

        _nameSb.Length = 0;
        bool changed = false;
        bool lastWasUnderscore = false;

        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];

            if (c >= 'A' && c <= 'Z')
            {
                c = (char)(c + 32); // ve chu thuong
                changed = true;
            }

            bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
            if (!ok)
            {
                c = '_';
                changed = true;
            }

            // Gop nhieu '_' lien tiep thanh mot cho de doc tren dashboard.
            if (c == '_')
            {
                if (lastWasUnderscore || _nameSb.Length == 0) { changed = true; continue; }
                lastWasUnderscore = true;
            }
            else
            {
                lastWasUnderscore = false;
            }

            _nameSb.Append(c);
        }

        // Bo '_' thua o cuoi.
        while (_nameSb.Length > 0 && _nameSb[_nameSb.Length - 1] == '_')
        {
            _nameSb.Length--;
            changed = true;
        }

        // GA4 bat buoc bat dau bang CHU CAI (khong duoc so, khong duoc '_').
        if (_nameSb.Length > 0 && !(_nameSb[0] >= 'a' && _nameSb[0] <= 'z'))
        {
            _nameSb.Insert(0, 'e');
            changed = true;
        }

        if (_nameSb.Length > maxLen)
        {
            _nameSb.Length = maxLen;
            while (_nameSb.Length > 0 && _nameSb[_nameSb.Length - 1] == '_')
                _nameSb.Length--;
            changed = true;
        }

        string result = _nameSb.ToString();

#if UNITY_EDITOR
        if (changed && !string.IsNullOrEmpty(result))
            Debug.LogWarning($"[Analytics] Ten {kindForWarning} '{raw}' khong hop luat GA4 — da sua thanh '{result}'.");
#endif

        return string.IsNullOrEmpty(result) ? null : result;
    }

    private static string ClampString(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        if (s.Length <= MaxParamValueLen) return s;
#if UNITY_EDITOR
        Debug.LogWarning($"[Analytics] Gia tri chuoi dai {s.Length} ky tu — cat con {MaxParamValueLen}.");
#endif
        return s.Substring(0, MaxParamValueLen);
    }
}
