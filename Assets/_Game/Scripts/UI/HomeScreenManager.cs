using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý Màn Hình Khởi Động / Loading Home (HomeScreenManager).
/// Thiết kế chuẩn theo concept:
/// - Background thung lũng nông trại nghệ thuật toàn màn hình.
/// - Khung Tip & Loading giấy kem ấm áp bo góc mềm mại.
/// - Nhân vật hoạt hình nhún nhảy vui vẻ bên trái.
/// - Món ăn thơm ngon bồng bềnh bên phải.
/// - Thanh Fill Bar xanh tươi kèm % tiến độ.
/// - Tự động nạp và chuyển cảnh thẳng vào SCN_Farm.
/// </summary>
public class HomeScreenManager : MonoBehaviour
{
    public static HomeScreenManager Instance { get; private set; }

    [Header("── UI Progress Bar ──")]
    [SerializeField] private Image imgProgressFill;
    [SerializeField] private TMP_Text txtProgressPercent;

    [Header("── Animated Decor Elements ──")]
    [SerializeField] private RectTransform characterRect;
    [SerializeField] private Image characterImage;
    [SerializeField] private Sprite[] characterFrames;
    [SerializeField] private float frameRate = 8f;
    [SerializeField] private RectTransform foodDecorRect;

    [Header("── UI Fun Tips ──")]
    [SerializeField] private TMP_Text txtTipTitle;
    [SerializeField] private TMP_Text txtFunTip;
    [SerializeField] private CanvasGroup tipCanvasGroup;
    [SerializeField] private float tipInterval = 2.4f;

    [Header("── Scene Transition ──")]
    [SerializeField] private string targetSceneName = "SCN_Farm";
    [SerializeField] private float minLoadingSeconds = 2.8f;

    [Header("── Danh sách Tip vui nhộn ──")]
    [SerializeField] private List<string> funTips = new List<string>
    {
        "Grow crops, cook delicious dishes, and build your dream farm!",
        "Trồng trọt, nấu những món ăn thơm ngon và xây dựng nông trại trong mơ!",
        "Tưới nước mỗi ngày để hoa màu nhanh lớn và bội thu nhé! 🥕",
        "Nấu ăn tại Bếp Nông Trại để phục vụ du khách trên những chuyến tàu! 🍲",
        "Nâng cấp nhà kho để chứa được nhiều nông sản và nguyên liệu quý hơn! 🌾",
        "Bến tàu du lịch mang tới rất nhiều vị khách phương xa thân thiện! 🚢",
        "Thu hoạch lúa và ngô để làm bột bánh mì nóng hổi giòn rụm! 🍞"
    };

    private float currentProgress = 0f;
    private int currentTipIndex = 0;
    private Coroutine tipCoroutine;
    private Coroutine charAnimCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (imgProgressFill != null) imgProgressFill.fillAmount = 0f;
        if (txtProgressPercent != null) txtProgressPercent.text = "0%";
        if (txtTipTitle != null) txtTipTitle.text = "🌱 Tip:";

        ShuffleTips();
        tipCoroutine = StartCoroutine(RotateTipsRoutine());
        charAnimCoroutine = StartCoroutine(CharacterAnimationRoutine());
        StartCoroutine(LoadingRoutine());
    }

    private void Update()
    {
        // Hiệu ứng thở nhẹ và nhún nhảy của nhân vật & dĩa món ăn
        float time = Time.unscaledTime;
        if (characterRect != null)
        {
            float bounceY = Mathf.Sin(time * 3.5f) * 6f;
            float squishX = 1f + Mathf.Sin(time * 3.5f) * 0.035f;
            float squishY = 1f - Mathf.Sin(time * 3.5f) * 0.035f;
            characterRect.localScale = new Vector3(squishX, squishY, 1f);
        }

        if (foodDecorRect != null)
        {
            float floatY = Mathf.Sin(time * 2.8f + 1f) * 4.5f;
            float rotZ = Mathf.Sin(time * 2.2f) * 2.5f;
            foodDecorRect.localEulerAngles = new Vector3(0f, 0f, rotZ);
        }
    }

    private IEnumerator CharacterAnimationRoutine()
    {
        if (characterFrames == null || characterFrames.Length == 0 || characterImage == null)
            yield break;

        int frame = 0;
        float delay = 1f / Mathf.Max(1f, frameRate);

        while (true)
        {
            characterImage.sprite = characterFrames[frame];
            frame = (frame + 1) % characterFrames.Length;
            yield return new WaitForSecondsRealtime(delay);
        }
    }

    private void ShuffleTips()
    {
        if (funTips == null || funTips.Count == 0) return;
        for (int i = 0; i < funTips.Count; i++)
        {
            int r = Random.Range(i, funTips.Count);
            string tmp = funTips[i];
            funTips[i] = funTips[r];
            funTips[r] = tmp;
        }
    }

    private IEnumerator RotateTipsRoutine()
    {
        while (true)
        {
            if (funTips.Count > 0 && txtFunTip != null)
            {
                // Fade out
                if (tipCanvasGroup != null)
                {
                    float t = 0f;
                    while (t < 0.2f)
                    {
                        t += Time.unscaledDeltaTime;
                        tipCanvasGroup.alpha = 1f - (t / 0.2f);
                        yield return null;
                    }
                }

                currentTipIndex = (currentTipIndex + 1) % funTips.Count;
                txtFunTip.text = funTips[currentTipIndex];

                // Fade in
                if (tipCanvasGroup != null)
                {
                    float t = 0f;
                    while (t < 0.25f)
                    {
                        t += Time.unscaledDeltaTime;
                        tipCanvasGroup.alpha = t / 0.25f;
                        yield return null;
                    }
                }
            }

            yield return new WaitForSecondsRealtime(tipInterval);
        }
    }

    private IEnumerator LoadingRoutine()
    {
        float timer = 0f;
        float totalTime = Mathf.Max(2.0f, minLoadingSeconds);

        // Bắt đầu tải ngầm SCN_Farm
        AsyncOperation asyncLoad = null;
        try
        {
            asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
            if (asyncLoad != null) asyncLoad.allowSceneActivation = false;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[HomeScreenManager] LoadSceneAsync warning: " + e.Message);
        }

        while (timer < totalTime)
        {
            timer += Time.unscaledDeltaTime;
            float targetP = Mathf.Clamp01(timer / totalTime);

            if (asyncLoad != null)
            {
                float asyncP = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                targetP = Mathf.Min(targetP, Mathf.Max(asyncP, targetP * 0.9f));
            }

            currentProgress = Mathf.MoveTowards(currentProgress, targetP, Time.unscaledDeltaTime * 1.8f);
            UpdateProgressVisual(currentProgress);

            yield return null;
        }

        currentProgress = 1f;
        UpdateProgressVisual(1f);

        yield return new WaitForSecondsRealtime(0.25f);

        if (tipCoroutine != null) StopCoroutine(tipCoroutine);
        if (charAnimCoroutine != null) StopCoroutine(charAnimCoroutine);

        if (asyncLoad != null)
        {
            asyncLoad.allowSceneActivation = true;
        }
        else
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.LoadScene(targetSceneName, SceneTransitionManager.TransitionType.CloudWipe);
            else
                SceneManager.LoadScene(targetSceneName);
        }
    }

    private void UpdateProgressVisual(float progress)
    {
        if (imgProgressFill != null)
            imgProgressFill.fillAmount = progress;

        int percent = Mathf.RoundToInt(progress * 100f);
        if (txtProgressPercent != null)
            txtProgressPercent.text = $"{percent}%";
    }
}
