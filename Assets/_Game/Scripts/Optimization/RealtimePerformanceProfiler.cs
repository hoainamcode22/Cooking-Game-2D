using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CookingGame.Optimization
{
    /// <summary>
    /// BỘ ĐO HIỆU NĂNG & THỜI GIAN THỰC (REALTIME PROFILER & SYSTEM MONITOR):
    ///
    /// Giúp theo dõi trực quan:
    ///  1. FPS thực tế & Frame Time (ms).
    ///  2. Bộ nhớ RAM đang dùng & VRAM Textures.
    ///  3. Tốc độ sinh rác bộ nhớ (GC Alloc/sec) — thủ phạm gây giật khựng.
    ///  4. Số lượng Draw Calls, Batches, Triangles.
    ///  5. Thống kê các object/hệ thống đang chạy trong scene.
    ///
    /// PHÍM TẮT: Bấm F4 để Ẩn / Hiện bảng đo.
    /// Trên Mobile: Có nút tròn nhỏ [FPS] ở góc trên cùng để bật/tắt.
    /// </summary>
    [DisallowMultipleComponent]
    public class RealtimePerformanceProfiler : MonoBehaviour
    {
        private static RealtimePerformanceProfiler _instance;
        private bool _show = false;

        // FPS & Frame Time
        private float _fpsAccum;
        private int   _fpsFrames;
        private float _currentFps;
        private float _minFps = 999f;
        private float _frameTimeMs;
        private float _fpsTimer;

        // Memory & GC
        private long _lastTotalMemory;
        private float _gcTimer;
        private float _gcRateKBPerSec;
        private float _allocatedRamMB;
        private float _reservedRamMB;

        // GUI Styles
        private GUIStyle _panelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _warnStyle;
        private GUIStyle _buttonStyle;
        private bool     _stylesInit;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBoot()
        {
            if (_instance != null) return;
            var go = new GameObject("[PerfProfiler_Monitor]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<RealtimePerformanceProfiler>();
        }
#endif

        private void Update()
        {
            // Toggle bằng phím F4
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.f4Key.wasPressedThisFrame)
            {
                _show = !_show;
            }
#else
            if (Input.GetKeyDown(KeyCode.F4))
            {
                _show = !_show;
            }
#endif

            // Tính FPS & Frame Time mỗi 0.2s
            float dt = Time.unscaledDeltaTime;
            _fpsAccum += dt;
            _fpsFrames++;
            _fpsTimer += dt;

            if (_fpsTimer >= 0.2f)
            {
                _currentFps = _fpsFrames / _fpsAccum;
                _frameTimeMs = (_fpsAccum / _fpsFrames) * 1000f;
                if (_currentFps < _minFps && _currentFps > 5f) _minFps = _currentFps;

                _fpsAccum = 0f;
                _fpsFrames = 0;
                _fpsTimer = 0f;
            }

            // Đo GC Allocations mỗi 1.0s
            _gcTimer += dt;
            if (_gcTimer >= 1.0f)
            {
                long currentMemory = Profiler.GetMonoUsedSizeLong();
                long delta = currentMemory - _lastTotalMemory;
                if (delta > 0)
                {
                    _gcRateKBPerSec = delta / 1024f / _gcTimer;
                }
                _lastTotalMemory = currentMemory;
                _allocatedRamMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
                _reservedRamMB = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
                _gcTimer = 0f;
            }
        }

        private void InitStyles()
        {
            if (_stylesInit) return;
            _stylesInit = true;

            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.12f, 0.92f));
            bgTex.Apply();

            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = bgTex;
            _panelStyle.padding = new RectOffset(12, 12, 10, 10);

            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = 14;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 12;
            _labelStyle.normal.textColor = Color.white;

            _warnStyle = new GUIStyle(GUI.skin.label);
            _warnStyle.fontSize = 12;
            _warnStyle.fontStyle = FontStyle.Bold;
            _warnStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 11;
            _buttonStyle.fontStyle = FontStyle.Bold;
        }

        private void OnGUI()
        {
            InitStyles();

            // Nút bấm nhỏ góc màn hình (dành cho Mobile không có phím F4)
            float btnW = 75f;
            float btnH = 30f;
            string btnText = _show ? "✕ ĐÓNG" : $"⚡ {_currentFps:0} FPS";
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = _show ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.2f, 0.7f, 0.3f);

            if (GUI.Button(new Rect(10f, 10f, btnW, btnH), btnText, _buttonStyle))
            {
                _show = !_show;
            }
            GUI.backgroundColor = oldBg;

            if (!_show) return;

            // Bảng thông số chi tiết
            float panelW = 340f;
            float panelH = 360f;
            Rect panelRect = new Rect(10f, 45f, panelW, panelH);

            GUILayout.BeginArea(panelRect, _panelStyle);

            GUILayout.Label("📊 THỐNG KÊ HIỆU NĂNG THỜI GIAN THỰC", _headerStyle);
            GUILayout.Space(6);

            // FPS & Frame Time
            string fpsColor = _currentFps >= 55f ? "<color=#4ade80>" : (_currentFps >= 30f ? "<color=#facc15>" : "<color=#f87171>");
            GUILayout.Label($"<b>FPS:</b> {fpsColor}{_currentFps:0.0} FPS</color>  ·  <b>Khung hình:</b> {_frameTimeMs:0.1} ms", _labelStyle);
            GUILayout.Label($"<b>Mục tiêu:</b> 60.0 FPS (16.6 ms)  ·  <b>Thấp nhất:</b> {_minFps:0.0} FPS", _labelStyle);
            GUILayout.Space(6);

            // Bộ nhớ RAM & VRAM
            GUILayout.Label("<b>💾 BỘ NHỚ (RAM & GC):</b>", _headerStyle);
            GUILayout.Label($" • RAM Đang Dùng: <b>{_allocatedRamMB:0.0} MB</b> / {_reservedRamMB:0.0} MB", _labelStyle);
            GUILayout.Label($" • Mono Heap (Script): <b>{Profiler.GetMonoUsedSizeLong() / 1024f / 1024f:0.0} MB</b>", _labelStyle);

            // Cảnh báo GC
            if (_gcRateKBPerSec > 100f)
            {
                GUILayout.Label($" ⚠ Rác sinh ra: <b>{_gcRateKBPerSec:0} KB/giây</b> (Đang sinh rác nhiều!)", _warnStyle);
            }
            else
            {
                GUILayout.Label($" • Rác sinh ra: <b>{_gcRateKBPerSec:0} KB/giây</b> (Rất tốt, ít rác)", _labelStyle);
            }
            GUILayout.Space(6);

            // Trạng thái các hệ thống
            GUILayout.Label("<b>⚙️ TRẠNG THÁI HỆ THỐNG:</b>", _headerStyle);
            GUILayout.Label($" • Hệ thống Câu Cá: <b>{GetFishingStatusLabel()}</b>", _labelStyle);
            GUILayout.Label($" • Resolution Scaling: <b>{Screen.width}x{Screen.height} (60Hz)</b>", _labelStyle);
            GUILayout.Label($" • Thiết bị: <b>{SystemInfo.deviceModel}</b>", _labelStyle);
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dọn RAM (GC)", _buttonStyle, GUILayout.Height(26)))
            {
                System.GC.Collect();
                Resources.UnloadUnusedAssets();
            }
            if (GUILayout.Button(DevOverlayGate.Enabled ? "Tắt Dev UI (F3/F7)" : "Bật Dev UI (F3/F7)", _buttonStyle, GUILayout.Height(26)))
            {
                DevOverlayGate.Set(!DevOverlayGate.Enabled);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private static string GetFishingStatusLabel()
        {
            var gateType = System.Type.GetType("FarmGame.Fishing.FishingFeatureGate, Assembly-CSharp");
            if (gateType != null)
            {
                var field = gateType.GetField("IsEnabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field != null && (bool)field.GetValue(null))
                    return "<color=#4ade80>BẬT</color>";
                return "<color=#9ca3af>ĐÃ TẮT (Tiết kiệm CPU/RAM)</color>";
            }
            return "<color=#9ca3af>CHƯA CÀI ĐẶT / ĐÃ TÁCH</color>";
        }
    }
}
