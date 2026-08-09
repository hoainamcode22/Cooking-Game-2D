using System.Collections;
using UnityEngine;
using Core;

namespace UI
{
    [RequireComponent(typeof(RectTransform))]
    public class FlyingResource : MonoBehaviour
    {
        public enum ResourceType { Gold, Diamond, EXP }
        
        [Header("Resource Settings")]
        public ResourceType resourceType;
        public int amount = 1;
        
        [Header("Animation Settings")]
        [Tooltip("How long it takes to reach the target UI")]
        public float flyDuration = 1.0f;
        public AnimationCurve flyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        // Effects configuration
        [Header("Effect on Arrival")]
        public bool doPunchScale = true;
        public float punchScaleMultiplier = 1.3f;
        public float punchScaleDuration = 0.2f;

        private RectTransform rectTransform;
        
        // References required for final update
        private HUDController hudController;
        private PlayerProfile playerProfile;
        private RectTransform targetUI;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }
        
        /// <summary>
        /// Starts flying the resource from a specific world position.
        /// Useful when harvesting resources in a 3D or 2D world space.
        /// </summary>
        public void InitializeFromWorld(Vector3 worldPos, Camera cam, Canvas canvas, HUDController hud, PlayerProfile profile, RectTransform target)
        {
            hudController = hud;
            playerProfile = profile;
            targetUI = target;
            
            // Convert world position to screen point, then to canvas space
            Vector2 screenPos = cam.WorldToScreenPoint(worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, 
                screenPos, 
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, 
                out Vector2 localPos);
                
            rectTransform.anchoredPosition = localPos;
            
            StartCoroutine(FlyToTarget());
        }

        /// <summary>
        /// Starts flying the resource from an anchored UI position.
        /// Useful for pure UI effects or starting from a known Canvas position.
        /// </summary>
        public void InitializeFromScreen(Vector2 startAnchoredPos, HUDController hud, PlayerProfile profile, RectTransform target)
        {
            hudController = hud;
            playerProfile = profile;
            targetUI = target;
            
            rectTransform.anchoredPosition = startAnchoredPos;
            
            StartCoroutine(FlyToTarget());
        }

        private IEnumerator FlyToTarget()
        {
            if (targetUI == null) yield break;

            Vector3 startPos = rectTransform.position;
            float elapsed = 0f;
            
            while (elapsed < flyDuration)
            {
                float t = elapsed / flyDuration;
                float curveT = flyCurve.Evaluate(t);
                
                // Lerp in world space (which is valid for UI elements in a Canvas)
                rectTransform.position = Vector3.Lerp(startPos, targetUI.position, curveT);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            rectTransform.position = targetUI.position;
            
            OnArrived();
        }
        
        private void OnArrived()
        {
            // Trigger UI Effect
            if (hudController != null && targetUI != null && doPunchScale)
            {
                hudController.PunchScale(targetUI, punchScaleMultiplier, punchScaleDuration);
            }
            
            // Apply visual and logical data updates
            if (playerProfile != null)
            {
                switch (resourceType)
                {
                    case ResourceType.Gold:
                        playerProfile.AddGold(amount);
                        if (hudController != null) hudController.UpdateGoldText(playerProfile.Gold);
                        break;
                        
                    case ResourceType.Diamond:
                        playerProfile.AddDiamond(amount);
                        if (hudController != null) hudController.UpdateDiamondText(playerProfile.Diamond);
                        break;
                        
                    case ResourceType.EXP:
                        playerProfile.AddEXP(amount);
                        // Trigger visual exp animation on the HUD
                        if (hudController != null) hudController.AnimateEXP(amount);
                        break;
                }
            }
            else if (hudController != null) 
            {
                // Fallback: Just update HUD visually without backend logic if profile is missing
                if (resourceType == ResourceType.EXP)
                {
                    hudController.AnimateEXP(amount);
                }
            }
            
            Destroy(gameObject);
        }
    }
}
