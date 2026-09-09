using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Đèn lồng theo người chơi: con "PlayerLantern" có Light2D Point (blend style 0) bám theo owner.
    /// Ban ngày vẫn sáng nhẹ; ban đêm (ratio [0.75..1] ∪ [0..0.2] theo DayNightCycleController) tăng ×1.6, nội suy mượt.
    /// Bù scale của owner (prefab nhân vật scale theo playerWorldHeight) để bán kính/độ cao đèn luôn tính theo world unit.
    /// </summary>
    public class PlayerLanternLight : MonoBehaviour
    {
        private const string ChildName = "PlayerLantern";
        private const float LocalOffsetY = 0.35f;
        private const float InnerRadiusRatio = 0.3f;
        private const float FalloffIntensity = 0.6f;
        private const float NightMultiplier = 1.6f;
        private const float BlendSpeed = 1.5f;   // đơn vị hệ số/giây khi nội suy ngày↔đêm

        [SerializeField] private float baseIntensity = 0.8f;
        [SerializeField] private float baseRadius = 2.4f;

        private Light2D _light;
        private Day_Night.DayNightCycleController _dayNight;
        private bool _dayNightSearched;
        private float _nightBlend;   // 0 = ngày, 1 = đêm (đã nội suy)

        /// <summary>Find-or-create con PlayerLantern dưới owner và cấu hình Light2D theo cfg. Trả về component (không bao giờ null nếu owner != null).</summary>
        public static PlayerLanternLight Attach(Transform owner, FishingConfig cfg)
        {
            if (owner == null) { return null; }
            Transform t = owner.Find(ChildName);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(ChildName);
                go.transform.SetParent(owner, false);
            }
            else { go = t.gameObject; }

            var lantern = go.GetComponent<PlayerLanternLight>();
            if (lantern == null) { lantern = go.AddComponent<PlayerLanternLight>(); }
            lantern.Configure(cfg);
            return lantern;
        }

        private void Configure(FishingConfig cfg)
        {
            baseRadius = cfg != null ? Mathf.Max(0.2f, cfg.playerLanternRadius) : 2.4f;
            baseIntensity = cfg != null ? Mathf.Max(0f, cfg.playerLanternIntensity) : 0.8f;
            Color color = cfg != null ? cfg.playerLanternColor : new Color(1f, 0.93f, 0.78f, 1f);

            _light = GetComponent<Light2D>();
            if (_light == null) { _light = gameObject.AddComponent<Light2D>(); }

            // Các thuộc tính public của Light2D (URP 14-17): lightType, blendStyleIndex, pointLightOuter/InnerRadius, intensity, color, falloffIntensity.
            _light.lightType = Light2D.LightType.Point;
            _light.blendStyleIndex = 0;
            _light.pointLightOuterRadius = baseRadius;
            _light.pointLightInnerRadius = baseRadius * InnerRadiusRatio;
            _light.intensity = baseIntensity;
            _light.color = color;
            _light.falloffIntensity = FalloffIntensity;
            ApplyToAllSortingLayers(_light);

            ApplyOwnerScaleCompensation();
            _nightBlend = 0f;
        }

        /// <summary>AddComponent<Light2D> mặc định đã áp mọi sorting layer; trong Editor ghi tường minh cho chắc (m_ApplyToSortingLayers không có setter public).</summary>
        private static void ApplyToAllSortingLayers(Light2D light)
        {
#if UNITY_EDITOR
            try
            {
                var so = new UnityEditor.SerializedObject(light);
                var pLayers = so.FindProperty("m_ApplyToSortingLayers");
                if (pLayers != null && pLayers.isArray)
                {
                    var layers = SortingLayer.layers;
                    pLayers.arraySize = layers.Length;
                    for (int i = 0; i < layers.Length; i++) { pLayers.GetArrayElementAtIndex(i).intValue = layers[i].id; }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            catch (System.Exception e) { Debug.LogWarning(FishingIds.LogTag + " PlayerLantern: không ghi được m_ApplyToSortingLayers — " + e.Message); }
#endif
        }

        /// <summary>Owner có scale khác 1 (nhân vật scale theo playerWorldHeight) → con đặt scale nghịch đảo để đèn giữ world unit.</summary>
        private void ApplyOwnerScaleCompensation()
        {
            Transform owner = transform.parent;
            if (owner == null) { return; }
            Vector3 ls = owner.lossyScale;
            float sx = Mathf.Abs(ls.x) > 0.0001f ? 1f / ls.x : 1f;
            float sy = Mathf.Abs(ls.y) > 0.0001f ? 1f / ls.y : 1f;
            transform.localScale = new Vector3(sx, sy, 1f);
            transform.localPosition = new Vector3(0f, LocalOffsetY * sy, 0f);
            transform.localRotation = Quaternion.identity;
        }

        private void LateUpdate()
        {
            if (_light == null) { return; }
            if (!_dayNightSearched)
            {
                _dayNightSearched = true;
                _dayNight = FindFirstObjectByType<Day_Night.DayNightCycleController>();
            }

            float target = _dayNight != null ? NightWeight(_dayNight.CurrentDayRatio) : 0f;
            _nightBlend = Mathf.MoveTowards(_nightBlend, target, BlendSpeed * Time.deltaTime);
            _light.intensity = baseIntensity * Mathf.Lerp(1f, NightMultiplier, _nightBlend);
        }

        /// <summary>Trọng số đêm 0..1 theo ratio ngày: đêm đủ ở [0.85..1] ∪ [0..0.1], dốc mượt trong [0.75..0.85] và [0.1..0.2].</summary>
        private static float NightWeight(float ratio)
        {
            float r = Mathf.Repeat(ratio, 1f);
            if (r >= 0.85f || r <= 0.1f) { return 1f; }
            if (r >= 0.75f) { return Mathf.SmoothStep(0f, 1f, (r - 0.75f) / 0.1f); }
            if (r <= 0.2f) { return Mathf.SmoothStep(1f, 0f, (r - 0.1f) / 0.1f); }
            return 0f;
        }
    }
}
