using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core;

namespace UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Data Profile")]
        [Tooltip("Optional reference to fetch max exp, though logic is contained in visual loop too.")]
        public PlayerProfile playerProfile;

        [Header("UI References")]
        public Image expFillBar;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI expText;
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI diamondText;

        [Header("Animation Settings")]
        [Tooltip("How fast the EXP fills up visually (EXP per second)")]
        public float expFillSpeed = 100f; 

        // Internal visual tracking state
        private float visualCurrentEXP;
        private int visualMaxEXP;
        private int visualLevel;

        private Coroutine expCoroutine;

        private void Start()
        {
            if (playerProfile != null)
            {
                SyncWithProfile(playerProfile);
            }
        }

        /// <summary>
        /// Sync the initial visual state of the HUD with actual profile data.
        /// </summary>
        public void SyncWithProfile(PlayerProfile profile)
        {
            visualCurrentEXP = profile.CurrentEXP;
            visualMaxEXP = profile.MaxEXP;
            visualLevel = profile.Level;
            
            UpdateEXPUI();
            UpdateGoldText(profile.Gold);
            UpdateDiamondText(profile.Diamond);
        }

        public void UpdateGoldText(int amount)
        {
            if (goldText != null)
                goldText.text = amount.ToString();
        }

        public void UpdateDiamondText(int amount)
        {
            if (diamondText != null)
                diamondText.text = amount.ToString();
        }

        /// <summary>
        /// Call this to animate EXP visually on the HUD.
        /// It smoothly interpolates the EXP fill bar. If it hits MaxEXP, it increments the level,
        /// resets to 0, and continues animating the remainder.
        /// </summary>
        public void AnimateEXP(int amountToAdd)
        {
            if (expCoroutine != null)
                StopCoroutine(expCoroutine);
            
            expCoroutine = StartCoroutine(AnimateEXPCoroutine(amountToAdd));
        }

        private IEnumerator AnimateEXPCoroutine(int amountToAdd)
        {
            float remainingExpToAdd = amountToAdd;
            
            while (remainingExpToAdd > 0)
            {
                float step = expFillSpeed * Time.deltaTime;
                
                if (step > remainingExpToAdd)
                {
                    step = remainingExpToAdd;
                }
                
                visualCurrentEXP += step;
                remainingExpToAdd -= step;

                if (visualCurrentEXP >= visualMaxEXP)
                {
                    visualCurrentEXP -= visualMaxEXP;
                    visualLevel++;
                    visualMaxEXP = GetMaxEXPForLevel(visualLevel);
                }

                UpdateEXPUI();
                yield return null;
            }
        }

        private void UpdateEXPUI()
        {
            if (expFillBar != null)
                expFillBar.fillAmount = visualCurrentEXP / (float)visualMaxEXP;
                
            if (levelText != null)
                levelText.text = $"Lv {visualLevel}";
                
            if (expText != null)
                expText.text = $"{(int)visualCurrentEXP} / {visualMaxEXP}";
        }

        private int GetMaxEXPForLevel(int level)
        {
            if (playerProfile != null)
                return playerProfile.CalculateMaxEXPForLevel(level);
            
            // Fallback simple formula if profile is unassigned
            return level * 100;
        }

        /// <summary>
        /// Triggers a PunchScale effect to make the element bounce slightly.
        /// </summary>
        public void PunchScale(Transform target, float scaleMultiplier = 1.3f, float duration = 0.2f)
        {
            if (target != null)
                StartCoroutine(PunchScaleCoroutine(target, scaleMultiplier, duration));
        }
        
        /// <summary>
        /// Triggers a Shake effect on the target UI element.
        /// </summary>
        public void Shake(Transform target, float intensity = 10f, float duration = 0.2f)
        {
            if (target != null)
                StartCoroutine(ShakeCoroutine(target, intensity, duration));
        }

        private IEnumerator PunchScaleCoroutine(Transform target, float scaleMultiplier, float duration)
        {
            Vector3 originalScale = Vector3.one; // Assume base scale is 1
            Vector3 targetScale = originalScale * scaleMultiplier;
            
            float halfDuration = duration / 2f;
            float elapsed = 0f;

            // Scale Up
            while (elapsed < halfDuration)
            {
                if (target == null) yield break;
                target.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / halfDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            
            // Scale Down
            while (elapsed < halfDuration)
            {
                if (target == null) yield break;
                target.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / halfDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (target != null)
                target.localScale = originalScale;
        }

        private IEnumerator ShakeCoroutine(Transform target, float intensity, float duration)
        {
            Vector3 originalPos = target.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (target == null) yield break;
                
                float x = originalPos.x + Random.Range(-intensity, intensity);
                float y = originalPos.y + Random.Range(-intensity, intensity);
                
                target.localPosition = new Vector3(x, y, originalPos.z);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (target != null)
                target.localPosition = originalPos;
        }
    }
}
