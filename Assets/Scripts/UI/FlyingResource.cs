using System.Collections;
using UnityEngine;
using FarmGame.Core;

namespace FarmGame.UI
{
    public class FlyingResource : MonoBehaviour
    {
        public enum ResourceType { Gold, Diamond, EXP }
        public ResourceType type;
        public int amount;
        
        public void FlyToHUD(Vector3 startScreenPos)
        {
            transform.position = startScreenPos;
            RectTransform target = null;

            if (type == ResourceType.Gold) target = HUDController.Instance.goldContainer;
            else if (type == ResourceType.Diamond) target = HUDController.Instance.diamondContainer;
            else if (type == ResourceType.EXP) target = HUDController.Instance.expContainer;

            StartCoroutine(FlyCoroutine(target));
        }

        private IEnumerator FlyCoroutine(RectTransform target)
        {
            Vector3 startPos = transform.position;
            float time = 0;
            float duration = 0.8f; // Fly time

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;
                // Ease in out
                t = t * t * (3f - 2f * t);
                
                if (target != null)
                {
                    transform.position = Vector3.Lerp(startPos, target.position, t);
                }
                yield return null;
            }

            if (target != null)
            {
                HUDController.Instance.PunchScale(target);
            }

            if (type == ResourceType.Gold) PlayerProfile.Instance.AddGold(amount);
            else if (type == ResourceType.Diamond) PlayerProfile.Instance.AddDiamond(amount);
            else if (type == ResourceType.EXP) 
            {
                PlayerProfile.Instance.AddEXP(amount);
                HUDController.Instance.AddEXPVisuals(amount);
            }

            Destroy(gameObject);
        }
    }
}
