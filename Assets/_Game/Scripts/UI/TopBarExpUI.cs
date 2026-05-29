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
    [SerializeField] private float fillSmoothTime = 0.35f;

    private Coroutine fillRoutine;
    private bool started;

    public RectTransform IconExp => iconExp;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void Start()
    {
        started = true;

        // Tự tìm txtLevel từ child của iconExp (ngôi sao level)
        if (txtLevel == null && iconExp != null)
            txtLevel = iconExp.GetComponentInChildren<TMP_Text>(true);

        // Tự tạo text "0 / 40" trên thanh EXP nếu chưa có
        if (txtExpCentered == null && expBarFill != null)
        {
            Transform barParent = expBarFill.transform.parent != null
                ? expBarFill.transform.parent
                : expBarFill.transform;
            txtExpCentered = CreateExpBarText(barParent);
        }

        Subscribe();
        RefreshImmediate();
    }

    private void OnEnable()
    {
        Subscribe();
        if (started) RefreshImmediate();
    }

    private void OnDisable()
    {
        if (PlayerProgressManager.Instance != null)
        {
            PlayerProgressManager.Instance.OnLevelChanged -= HandleLevelChanged;
            PlayerProgressManager.Instance.OnExpChanged   -= HandleExpChanged;
        }

        if (fillRoutine != null) { StopCoroutine(fillRoutine); fillRoutine = null; }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void Subscribe()
    {
        if (PlayerProgressManager.Instance == null) return;
        PlayerProgressManager.Instance.OnLevelChanged -= HandleLevelChanged;
        PlayerProgressManager.Instance.OnExpChanged   -= HandleExpChanged;
        PlayerProgressManager.Instance.OnLevelChanged += HandleLevelChanged;
        PlayerProgressManager.Instance.OnExpChanged   += HandleExpChanged;
    }

    // Tạo GameObject TMP text căn giữa thanh EXP
    private TMP_Text CreateExpBarText(Transform parent)
    {
        var go = new GameObject("Txt_ExpBar");
        go.layer = parent.gameObject.layer;

        // Thêm TextMeshProUGUI trước — nó tự add RectTransform
        var ugui = go.AddComponent<TextMeshProUGUI>();
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin    = Vector2.zero;
        rt.anchorMax    = Vector2.one;
        rt.offsetMin    = new Vector2(4, 0);
        rt.offsetMax    = new Vector2(-4, 0);

        ugui.fontSize      = 17;
        ugui.fontStyle     = FontStyles.Bold;
        ugui.alignment     = TextAlignmentOptions.Center;
        ugui.color         = Color.white;
        ugui.raycastTarget = false;
        ugui.text          = "0 / 40";

        // Viền đen nhỏ để dễ đọc trên cả phần xanh lẫn phần trống
        ugui.outlineWidth = 0.2f;
        ugui.outlineColor = new Color32(0, 0, 0, 200);

        return ugui;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void RefreshImmediate()
    {
        if (PlayerProgressManager.Instance == null) return;

        int level = PlayerProgressManager.Instance.Level;
        int cur   = PlayerProgressManager.Instance.CurrentExp;
        int req   = PlayerProgressManager.Instance.RequiredExpCurrentLevel;

        if (txtLevel != null)
            txtLevel.text = level.ToString();

        if (txtExpCentered != null)
            txtExpCentered.text = req <= 0 ? "MAX" : $"{cur} / {req}";

        float fill = req <= 0 ? 1f : Mathf.Clamp01((float)cur / req);
        if (expBarFill != null)
            expBarFill.fillAmount = fill;
    }

    // ── Event handlers ─────────────────────────────────────────────────────────

    private void HandleLevelChanged(int level)
    {
        if (txtLevel != null)
            txtLevel.text = level.ToString();

        // Reset text — RequiredExp đã đổi sang level mới
        RefreshImmediate();
    }

    private void HandleExpChanged(int currentExp, int requiredExp)
    {
        if (txtExpCentered != null)
            txtExpCentered.text = requiredExp <= 0 ? "MAX" : $"{currentExp} / {requiredExp}";

        float target = requiredExp <= 0 ? 1f : Mathf.Clamp01((float)currentExp / requiredExp);
        AnimateFillTo(target);
    }

    // ── Fill animation ─────────────────────────────────────────────────────────

    private void AnimateFillTo(float targetFill)
    {
        if (expBarFill == null) return;
        if (fillRoutine != null) StopCoroutine(fillRoutine);
        fillRoutine = StartCoroutine(CoFill(targetFill));
    }

    private IEnumerator CoFill(float targetFill)
    {
        float start    = expBarFill.fillAmount;
        float elapsed  = 0f;
        float duration = Mathf.Max(0.05f, fillSmoothTime);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            expBarFill.fillAmount = Mathf.Lerp(start, targetFill, Mathf.SmoothStep(0f, 1f, p));
            yield return null;
        }

        expBarFill.fillAmount = targetFill;
        fillRoutine = null;
    }
}
