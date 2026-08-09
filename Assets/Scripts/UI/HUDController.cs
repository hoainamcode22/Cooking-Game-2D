using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmGame.Core;

namespace FarmGame.UI
{
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance;

        [Header("UI References")]
        public TextMeshProUGUI textLevel;
        public TextMeshProUGUI textEXP;
        public Image expFill;
        
        public TextMeshProUGUI textGold;
        public TextMeshProUGUI textDiamond;

        public RectTransform expContainer;
        public RectTransform goldContainer;
        public RectTransform diamondContainer;

        private float visualEXP = 0;
        private int visualLevel = 0;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (PlayerProfile.Instance != null)
            {
                PlayerProfile.Instance.ResourceChanged += UpdateStaticUI;
                visualEXP = PlayerProfile.Instance.CurrentEXP;
                visualLevel = PlayerProfile.Instance.Level;
                UpdateStaticUI();
            }
        }

        private void OnDestroy()
        {
            if (PlayerProfile.Instance != null)
                PlayerProfile.Instance.ResourceChanged -= UpdateStaticUI;
        }

        private void UpdateStaticUI()
        {
            textGold.text = PlayerProfile.Instance.Gold.ToString("N0").Replace(",", " ");
            textDiamond.text = PlayerProfile.Instance.Diamond.ToString("N0").Replace(",", " ");
        }

        public void AddEXPVisuals(int amount)
        {
            StartCoroutine(AnimateEXPCoroutine(amount));
        }

        private IEnumerator AnimateEXPCoroutine(int amount)
        {
            float targetEXP = visualEXP + amount;
            float speed = 2000f; // EXP per second

            while (visualEXP < targetEXP)
            {
                visualEXP += speed * Time.deltaTime;
                int currentMaxEXP = visualLevel * 100 + 3000;

                if (visualEXP >= currentMaxEXP)
                {
                    visualEXP -= currentMaxEXP;
                    targetEXP -= currentMaxEXP;
                    visualLevel++;
                    textLevel.text = visualLevel.ToString();
                    PunchScale(textLevel.rectTransform);
                }

                expFill.fillAmount = visualEXP / currentMaxEXP;
                textEXP.text = $"{(int)visualEXP}/{currentMaxEXP}";
                yield return null;
            }
            
            visualEXP = targetEXP; // Lock to exact
        }

        public void PunchScale(RectTransform target)
        {
            StartCoroutine(PunchScaleCoroutine(target));
        }

        private IEnumerator PunchScaleCoroutine(RectTransform target)
        {
            Vector3 originalScale = Vector3.one;
            float time = 0;
            float duration = 0.3f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float progress = time / duration;
                // Simple bounce math
                float scaleMod = 1 + Mathf.Sin(progress * Mathf.PI) * 0.2f; 
                target.localScale = originalScale * scaleMod;
                yield return null;
            }
            target.localScale = originalScale;
        }
    }
}
