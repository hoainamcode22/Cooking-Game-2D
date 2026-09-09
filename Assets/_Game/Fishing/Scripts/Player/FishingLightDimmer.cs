using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FarmGame.Fishing
{
    /// <summary>
    /// BỘ GIẢM SÁNG cho scene câu (CHỦ FILE: Lead, vòng 16). Sếp báo 09/09: "map sáng quá, bờ đá chói, Game view trắng xoá".
    ///
    /// Nguyên nhân đo được: prefab DayNightWeatherSetup chép nguyên từ farm. DayNightCycleController GHI cường độ
    /// mỗi frame từ curve (Day fallback 1.57, Ambient 0.8 ⇒ ~2.4x), cộng 3 đèn Point bán kính 43.59 unit — ở farm
    /// (world ×150) là vệt nhỏ, ở scene câu (world 1 unit) phủ nguyên map. Nền đất ×2.4 ⇒ cháy trắng.
    ///
    /// Vì controller ghi lại intensity MỌI FRAME trong Update, sửa tay trên Light2D vô ích và sửa prefab farm thì
    /// phạm luật "không đụng farm". Cách làm: LateUpdate (luôn chạy SAU mọi Update) nhân cfg.sceneLightMultiplier
    /// vào từng Light2D dưới gốc prefab. Chống nhân dồn: nhớ giá trị mình vừa ghi; frame nào controller không ghi
    /// lại (intensity vẫn bằng số mình ghi) thì bỏ qua, không nhân tiếp.
    ///
    /// [ExecuteAlways] để Scene view (PreviewInEditMode của controller) cũng đúng độ sáng lúc Sếp vẽ map.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class FishingLightDimmer : MonoBehaviour
    {
        public const string ObjectName = "FishingLightDimmer";

        [Tooltip("Gốc prefab ngày-đêm (DayNightWeatherSetup). Trống → tự tìm DayNightCycleController trong scene.")]
        [SerializeField] private Transform lightsRoot;

        private readonly List<Light2D> _lights = new List<Light2D>(12);
        private readonly Dictionary<Light2D, float> _written = new Dictionary<Light2D, float>(12);
        private Day_Night.DayNightCycleController _ctrl;
        private int _nextRescanFrame = -1;
        private float _appliedMult = 1f;
        private const int RescanEveryFrames = 90;

        public Transform LightsRoot { get { return lightsRoot; } set { lightsRoot = value; _nextRescanFrame = -1; } }

        private void OnEnable() { _nextRescanFrame = -1; }

        private void OnDisable()
        {
            // Trả lại cường độ gốc để tắt component là thấy y farm (không để lại số đã nhân).
            for (int i = 0; i < _lights.Count; i++)
            {
                Light2D l = _lights[i];
                float w;
                // Trả theo hệ số ĐÃ ÁP (không đọc config lúc này — Sếp có thể vừa đổi số).
                if (l != null && _written.TryGetValue(l, out w) && Mathf.Approximately(l.intensity, w) && w > 0f && _appliedMult > 0.0001f)
                {
                    l.intensity = w / _appliedMult;
                }
            }
            _written.Clear();
            _lights.Clear();
        }

        private void LateUpdate()
        {
            FishingConfig cfg = FishingDatabase.ConfigOrDefault;
            if (!Application.isPlaying && (cfg == null || !cfg.sceneLightApplyInEditMode)) { return; }

            if (Time.frameCount >= _nextRescanFrame) { Rescan(); }
            if (_lights.Count == 0) { return; }

            float mult = Mathf.Clamp(cfg != null ? cfg.sceneLightMultiplier : 0.5f, 0.1f, 1.5f);
            _appliedMult = mult;
            float floor = cfg != null ? Mathf.Clamp01(cfg.sceneAmbientFloor) : 0.25f;
            Light2D ambient = _ctrl != null ? _ctrl.AmbientLight : null;
            bool playing = Application.isPlaying;

            for (int i = 0; i < _lights.Count; i++)
            {
                Light2D l = _lights[i];
                if (l == null) { continue; }
                // [Reviewer N1] Edit Mode CHỈ nhân 5 đèn controller ghi lại mỗi tick (Day/Night/Ambient/SunRim/MoonRim).
                // Đèn Point/Flicker không ai ghi lại trong Edit Mode → nhân 1 lần là dính vào override prefab instance,
                // Ctrl+S lưu số đã nhân, mở lại nhân tiếp ⇒ mỗi lần lưu mất nửa. Trong Play mới nhân tất cả (Play không lưu scene).
                if (!playing && !IsControllerLight(l)) { continue; }
                float cur = l.intensity;
                float prev;
                // Controller chưa ghi lại từ lần trước (giá trị vẫn là của mình) → KHÔNG nhân dồn.
                if (_written.TryGetValue(l, out prev) && Mathf.Approximately(cur, prev)) { continue; }
                float target = cur * mult;
                if (ambient != null && l == ambient && target < floor && cur > 0f) { target = floor; }
                l.intensity = target;
                _written[l] = target;
            }
        }

        private bool IsControllerLight(Light2D l)
        {
            if (_ctrl == null || l == null) { return false; }
            return l == _ctrl.DayLight || l == _ctrl.NightLight || l == _ctrl.AmbientLight || l == _ctrl.SunRimLight || l == _ctrl.MoonRimLight;
        }

        private void Rescan()
        {
            _nextRescanFrame = Time.frameCount + RescanEveryFrames;
            if (lightsRoot == null || _ctrl == null)
            {
                _ctrl = lightsRoot != null ? lightsRoot.GetComponentInChildren<Day_Night.DayNightCycleController>(true) : null;
                if (_ctrl == null)
                {
                    _ctrl = FindFirstObjectByType<Day_Night.DayNightCycleController>(FindObjectsInactive.Exclude);
                    if (_ctrl != null && lightsRoot == null) { lightsRoot = _ctrl.transform.root; }
                }
            }
            if (lightsRoot == null) { _lights.Clear(); return; }

            Light2D[] found = lightsRoot.GetComponentsInChildren<Light2D>(true);
            _lights.Clear();
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null) { _lights.Add(found[i]); }
            }
            // Bỏ entry của đèn đã bị huỷ.
            var dead = new List<Light2D>();
            foreach (var kv in _written) { if (kv.Key == null || !_lights.Contains(kv.Key)) { dead.Add(kv.Key); } }
            for (int i = 0; i < dead.Count; i++) { _written.Remove(dead[i]); }
        }
    }
}
