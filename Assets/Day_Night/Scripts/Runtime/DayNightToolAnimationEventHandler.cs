using UnityEngine;
using UnityEngine.VFX;

namespace Day_Night
{
    public class DayNightToolAnimationEventHandler : MonoBehaviour
    {
        public VisualEffect FrontEffect;
        public string FrontEffectId;
        public VisualEffect UpEffect;
        public string UpEffectId;
        public VisualEffect SideEffect;
        public string SideEffectId;

        public void TriggerFrontVFX()
        {
            Trigger(FrontEffect, FrontEffectId, UpEffect, SideEffect);
        }

        public void TriggerSideVFX()
        {
            Trigger(SideEffect, SideEffectId, FrontEffect, UpEffect);
        }

        public void TriggerUpVFX()
        {
            Trigger(UpEffect, UpEffectId, FrontEffect, SideEffect);
        }

        private static void Trigger(VisualEffect active, string eventName, VisualEffect firstInactive, VisualEffect secondInactive)
        {
            if (firstInactive != null) firstInactive.gameObject.SetActive(false);
            if (secondInactive != null) secondInactive.gameObject.SetActive(false);
            if (active == null) return;

            active.gameObject.SetActive(true);
            if (!string.IsNullOrEmpty(eventName))
            {
                active.SendEvent(eventName);
            }
        }
    }
}
