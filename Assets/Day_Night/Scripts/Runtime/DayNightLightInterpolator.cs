using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Day_Night
{
    [ExecuteAlways]
    [DefaultExecutionOrder(999)]
    public class DayNightLightInterpolator : MonoBehaviour
    {
        [Serializable]
        public class LightFrame
        {
            public Light2D ReferenceLight;
            public float NormalizedTime;
        }

        public Light2D TargetLight;
        public LightFrame[] LightFrames;

        private DayNightCycleController controller;

        private void Update()
        {
            if (controller == null)
            {
                controller = FindFirstObjectByType<DayNightCycleController>();
            }

            SetRatio(controller != null ? controller.CurrentDayRatio : 0.5f);
        }

        public void SetRatio(float time)
        {
            if (TargetLight == null || LightFrames == null || LightFrames.Length == 0)
            {
                return;
            }

            int startFrame = 0;
            while (startFrame < LightFrames.Length - 1 && LightFrames[startFrame + 1].NormalizedTime < time)
            {
                startFrame++;
            }

            if (startFrame == LightFrames.Length - 1)
            {
                Interpolate(LightFrames[startFrame].ReferenceLight, LightFrames[startFrame].ReferenceLight, 0f);
                return;
            }

            float frameLength = LightFrames[startFrame + 1].NormalizedTime - LightFrames[startFrame].NormalizedTime;
            float frameValue = time - LightFrames[startFrame].NormalizedTime;
            float normalizedFrame = frameLength <= 0f ? 0f : frameValue / frameLength;
            Interpolate(LightFrames[startFrame].ReferenceLight, LightFrames[startFrame + 1].ReferenceLight, normalizedFrame);
        }

        private void Interpolate(Light2D start, Light2D end, float time)
        {
            if (start == null || end == null || TargetLight == null)
            {
                return;
            }

            TargetLight.color = Color.Lerp(start.color, end.color, time);
            TargetLight.intensity = Mathf.Lerp(start.intensity, end.intensity, time);

            Vector3[] startPath = start.shapePath;
            Vector3[] endPath = end.shapePath;
            if (startPath == null || endPath == null || startPath.Length == 0 || startPath.Length != endPath.Length)
            {
                return;
            }

            Vector3[] newPath = new Vector3[startPath.Length];
            for (int i = 0; i < startPath.Length; i++)
            {
                newPath[i] = Vector3.Lerp(startPath[i], endPath[i], time);
            }

            TargetLight.SetShapePath(newPath);
        }
    }
}
