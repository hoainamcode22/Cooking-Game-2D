using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopBarExpUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image expBarFill;
    [SerializeField] private TMP_Text txtLevel;
    [SerializeField] private TMP_Text txtExpCentered;
    [SerializeField] private RectTransform iconExp;

    [Header("Animation")]
    [SerializeField] private float fillSmoothTime = 0.25f;

    private Coroutine fillRoutine;

    public RectTransform IconExp => iconExp;

    private void OnEnable()
    {
        if (PlayerProgressManager.Instance != null)
        {
            PlayerProgressManager.Instance.OnLevelChanged += HandleLevelChanged;
            PlayerProgressManager.Instance.OnExpChanged += HandleExpChanged;
        }

        RefreshImmediate();
    }

    private void OnDisable()
    {
        if (PlayerProgressManager.Instance != null)
        {
            PlayerProgressManager.Instance.OnLevelChanged -= HandleLevelChanged;
            PlayerProgressManager.Instance.OnExpChanged -= HandleExpChanged;
        }

        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }
    }

    public void RefreshImmediate()
    {
        if (PlayerProgressManager.Instance == null)
            return;

        int level = PlayerProgressManager.Instance.Level;
        int cur = PlayerProgressManager.Instance.CurrentExp;
        int req = PlayerProgressManager.Instance.RequiredExpCurrentLevel;

        if (txtLevel != null)
            txtLevel.text = level.ToString();

        if (txtExpCentered != null)
        {
            if (req <= 0)
                txtExpCentered.text = "MAX";
            else
                txtExpCentered.text = $"{cur} / {req}";
        }

        float fill = req <= 0 ? 1f : Mathf.Clamp01((float)cur / req);
        if (expBarFill != null)
            expBarFill.fillAmount = fill;
    }

    private void HandleLevelChanged(int level)
    {
        if (txtLevel != null)
            txtLevel.text = level.ToString();

        RefreshImmediate();
    }

    private void HandleExpChanged(int currentExp, int requiredExp)
    {
        if (txtExpCentered != null)
        {
            if (requiredExp <= 0)
                txtExpCentered.text = "MAX";
            else
                txtExpCentered.text = $"{currentExp} / {requiredExp}";
        }

        float targetFill = requiredExp <= 0 ? 1f : Mathf.Clamp01((float)currentExp / requiredExp);
        AnimateFillTo(targetFill);
    }

    private void AnimateFillTo(float targetFill)
    {
        if (expBarFill == null)
            return;

        if (fillRoutine != null)
            StopCoroutine(fillRoutine);

        fillRoutine = StartCoroutine(CoFill(targetFill));
    }

    private IEnumerator CoFill(float targetFill)
    {
        float start = expBarFill.fillAmount;
        float t = 0f;

        float duration = Mathf.Max(0.01f, fillSmoothTime);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float ease = Mathf.SmoothStep(0f, 1f, p);
            expBarFill.fillAmount = Mathf.Lerp(start, targetFill, ease);
            yield return null;
        }

        expBarFill.fillAmount = targetFill;
        fillRoutine = null;
    }
}
