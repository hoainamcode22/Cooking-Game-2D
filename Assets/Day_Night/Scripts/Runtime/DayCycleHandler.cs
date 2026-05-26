// Disabled: duplicate of HappyHarvest/Scripts/DayCycleHandler.cs — use Day_Night.DayNightCycleController instead
#if false
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace HappyHarvest
{
    [DefaultExecutionOrder(10)]
    public class DayCycleHandler : MonoBehaviour
    {
        public Transform LightsRoot;

        [Header("Day Light")]
        public Light2D DayLight;
        public Gradient DayLightGradient;

        [Header("Night Light")]
        public Light2D NightLight;
        public Gradient NightLightGradient;

        [Header("Ambient Light")]
        public Light2D AmbientLight;
        public Gradient AmbientLightGradient;

        [Header("RimLights")]
        public Light2D SunRimLight;
        public Gradient SunRimLightGradient;
        public Light2D MoonRimLight;
        public Gradient MoonRimLightGradient;

        [Tooltip("The angle 0 = upward, going clockwise to 1 along the day")]
        public AnimationCurve ShadowAngle;
        [Tooltip("The scale of the normal shadow length (0 to 1) along the day")]
        public AnimationCurve ShadowLength;

        private List<ShadowInstance> m_Shadows = new();
        private List<LightInterpolator> m_LightBlenders = new();

        private void Awake()
        {
            GameManager.Instance.DayCycleHandler = this;
        }

        public void Tick()
        {
            UpdateLight(GameManager.Instance.CurrentDayRatio);
        }

        public void UpdateLight(float ratio)
        {
            DayLight.color = DayLightGradient.Evaluate(ratio);
            NightLight.color = NightLightGradient.Evaluate(ratio);

#if UNITY_EDITOR
            if(AmbientLight != null)
#endif
                AmbientLight.color = AmbientLightGradient.Evaluate(ratio);

#if UNITY_EDITOR
            if(SunRimLight != null)
#endif
                SunRimLight.color = SunRimLightGradient.Evaluate(ratio);

#if UNITY_EDITOR
            if(MoonRimLight != null)
#endif
                MoonRimLight.color = MoonRimLightGradient.Evaluate(ratio);

            LightsRoot.rotation = Quaternion.Euler(0,0, 360.0f * ratio);
            UpdateShadow(ratio);
        }

        void UpdateShadow(float ratio)
        {
            var currentShadowAngle = ShadowAngle.Evaluate(ratio);
            var currentShadowLength = ShadowLength.Evaluate(ratio);

            var opposedAngle = currentShadowAngle + 0.5f;
            while (currentShadowAngle > 1.0f)
                currentShadowAngle -= 1.0f;

            foreach (var shadow in m_Shadows)
            {
                var t = shadow.transform;
                t.eulerAngles = new Vector3(0,0, currentShadowAngle * 360.0f);
                t.localScale = new Vector3(1, 1f * shadow.BaseLength * currentShadowLength, 1);
            }

            foreach (var handler in m_LightBlenders)
            {
                handler.SetRatio(ratio);
            }
        }

        public void Save(ref DayCycleHandlerSaveData data) { }

        public void Load(DayCycleHandlerSaveData data) { }

        public static void RegisterShadow(ShadowInstance shadow)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var instance = GameObject.FindFirstObjectByType<DayCycleHandler>();
                if (instance != null) instance.m_Shadows.Add(shadow);
            }
            else
            {
#endif
                GameManager.Instance.DayCycleHandler.m_Shadows.Add(shadow);
#if UNITY_EDITOR
            }
#endif
        }

        public static void UnregisterShadow(ShadowInstance shadow)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var instance = GameObject.FindFirstObjectByType<DayCycleHandler>();
                if (instance != null) instance.m_Shadows.Remove(shadow);
            }
            else
            {
#endif
                if(GameManager.Instance?.DayCycleHandler != null)
                    GameManager.Instance.DayCycleHandler.m_Shadows.Remove(shadow);
#if UNITY_EDITOR
            }
#endif
        }

        public static void RegisterLightBlender(LightInterpolator interpolator)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var instance = FindFirstObjectByType<DayCycleHandler>();
                if (instance != null) instance.m_LightBlenders.Add(interpolator);
            }
            else
            {
#endif
            GameManager.Instance.DayCycleHandler.m_LightBlenders.Add(interpolator);
#if UNITY_EDITOR
            }
#endif
        }

        public static void UnregisterLightBlender(LightInterpolator interpolator)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var instance = FindFirstObjectByType<DayCycleHandler>();
                if (instance != null) instance.m_LightBlenders.Remove(interpolator);
            }
            else
            {
#endif
            if(GameManager.Instance?.DayCycleHandler != null)
                GameManager.Instance.DayCycleHandler.m_LightBlenders.Remove(interpolator);
#if UNITY_EDITOR
            }
#endif
        }
    }

    [System.Serializable]
    public struct DayCycleHandlerSaveData
    {
        public float TimeOfTheDay;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(DayCycleHandler))]
    class DayCycleEditor : Editor
    {
        private DayCycleHandler m_Target;

        public override VisualElement CreateInspectorGUI()
        {
            m_Target = target as DayCycleHandler;

            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var slider = new Slider(0.0f, 1.0f);
            slider.label = "Test time 0:00";
            slider.RegisterValueChangedCallback(evt =>
            {
                m_Target.UpdateLight(evt.newValue);
                slider.label = $"Test Time {GameManager.GetTimeAsString(evt.newValue)} ({evt.newValue:F2})";
                SceneView.RepaintAll();
            });

            root.RegisterCallback<ClickEvent>(evt =>
            {
                m_Target.UpdateLight(slider.value);
                SceneView.RepaintAll();
            });

            root.Add(slider);
            return root;
        }
    }
#endif

}
#endif // false
