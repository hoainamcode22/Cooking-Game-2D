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

    [Header("── Dynamic Atmosphere & FX ──")]
    [SerializeField] private RectTransform shimmerRect;
    [SerializeField] private RectTransform[] floatingClouds;
    [SerializeField] private RectTransform[] floatingSparkles;

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
        "Bắt đầu ngày mới bằng việc gieo trồng các luống lúa và rau tươi xanh!",
        "Thử nghiệm nhiều công thức nấu ăn độc đáo tại Nhà Bếp Nông Trại!",
        "Hoàn thành các đơn hàng tại Bảng Đơn để thu thập EXP và Vàng nâng cấp!",
        "Nâng cấp Nhà Kho thường xuyên để chứa được nhiều nông sản và nguyên liệu quý hơn!",
        "Chăm sóc vật nuôi và thu hoạch nông sản tươi mỗi ngày nhé!",
        "Đón chào những vị khách du lịch thân thiện cập bến để nhận thưởng lớn!"
    };

    private float currentProgress = 0f;
    private int currentTipIndex = 0;
    private Coroutine tipCoroutine;
    private Coroutine charAnimCoroutine;

    private Vector2 initialCharacterPos;
    private Vector2 initialFoodPos;
    private Vector2[] initialCloudPos;
    private Vector2[] initialSparklePos;

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
        if (txtTipTitle != null) txtTipTitle.text = "Mẹo Hay:";

        if (characterRect != null) initialCharacterPos = characterRect.anchoredPosition;
        if (foodDecorRect != null) initialFoodPos = foodDecorRect.anchoredPosition;

        if (floatingClouds != null && floatingClouds.Length > 0)
        {
            initialCloudPos = new Vector2[floatingClouds.Length];
            for (int i = 0; i < floatingClouds.Length; i++)
            {
                if (floatingClouds[i] != null) initialCloudPos[i] = floatingClouds[i].anchoredPosition;
            }
        }

        if (floatingSparkles != null && floatingSparkles.Length > 0)
        {
            initialSparklePos = new Vector2[floatingSparkles.Length];
            for (int i = 0; i < floatingSparkles.Length; i++)
            {
                if (floatingSparkles[i] != null) initialSparklePos[i] = floatingSparkles[i].anchoredPosition;
            }
        }

        ShuffleTips();
        tipCoroutine = StartCoroutine(RotateTipsRoutine());
        charAnimCoroutine = StartCoroutine(CharacterAnimationRoutine());
        StartCoroutine(LoadingRoutine());
    }

    private void Update()
    {
        float time = Time.unscaledTime;

        // 1. Cô bé Tutorial nhún nhảy có hồn + squish thở + nghiêng nhẹ
        if (characterRect != null)
        {
            float bounceY = Mathf.Sin(time * 3.5f) * 6.5f;
            float tiltZ = Mathf.Sin(time * 1.8f) * 2f;
            float squishX = 1f + Mathf.Sin(time * 3.5f) * 0.035f;
            float squishY = 1f - Mathf.Sin(time * 3.5f) * 0.035f;
            characterRect.anchoredPosition = initialCharacterPos + new Vector2(0f, bounceY);
            characterRect.localScale = new Vector3(squishX, squishY, 1f);
            characterRect.localEulerAngles = new Vector3(0f, 0f, tiltZ);
        }

        // 2. Dĩa món ăn bồng bềnh lơ lửng + lượn sóng
        if (foodDecorRect != null)
        {
            float floatY = Mathf.Sin(time * 2.8f + 1f) * 6f;
            float rotZ = Mathf.Sin(time * 2.2f) * 3f;
            foodDecorRect.anchoredPosition = initialFoodPos + new Vector2(0f, floatY);
            foodDecorRect.localEulerAngles = new Vector3(0f, 0f, rotZ);
        }

        // 3. Vệt sáng Shimmer Gleam trượt dọc thanh loading
        if (shimmerRect != null && imgProgressFill != null)
        {
            float totalWidth = 474f;
            float curWidth = totalWidth * imgProgressFill.fillAmount;
            if (curWidth > 15f)
            {
                if (!shimmerRect.gameObject.activeSelf) shimmerRect.gameObject.SetActive(true);
                float speed = 280f;
                float shimmerX = Mathf.Repeat(time * speed, curWidth);
                shimmerRect.anchoredPosition = new Vector2(shimmerX - (totalWidth * 0.5f) + 15f, 0f);
            }
            else
            {
                if (shimmerRect.gameObject.activeSelf) shimmerRect.gameObject.SetActive(false);
            }
        }

        // 4. Mây trôi lững lờ trên bầu trời nông trại
        if (floatingClouds != null && initialCloudPos != null)
        {
            for (int i = 0; i < floatingClouds.Length; i++)
            {
                if (floatingClouds[i] == null || i >= initialCloudPos.Length) continue;
                float speed = 16f + i * 10f;
                float driftX = (time * speed) % 2400f;
                floatingClouds[i].anchoredPosition = new Vector2(-1200f + driftX, initialCloudPos[i].y + Mathf.Sin(time * 1.2f + i) * 6f);
            }
        }

        // 5. Hạt sáng lấp lánh lung linh
        if (floatingSparkles != null && initialSparklePos != null)
        {
            for (int i = 0; i < floatingSparkles.Length; i++)
            {
                if (floatingSparkles[i] == null || i >= initialSparklePos.Length) continue;
                float pulse = 0.8f + Mathf.Sin(time * 4f + i * 1.5f) * 0.35f;
                floatingSparkles[i].localScale = new Vector3(pulse, pulse, 1f);
                floatingSparkles[i].anchoredPosition = initialSparklePos[i] + new Vector2(
                    Mathf.Sin(time * 1.5f + i) * 8f,
                    Mathf.Cos(time * 2.0f + i) * 8f
                );
            }
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

        // Hiệu ứng nảy punch 100% trước khi chuyển cảnh
        if (txtProgressPercent != null)
        {
            Vector3 origScale = txtProgressPercent.transform.localScale;
            float pTimer = 0f;
            while (pTimer < 0.35f)
            {
                pTimer += Time.unscaledDeltaTime;
                float s = 1f + Mathf.Sin((pTimer / 0.35f) * Mathf.PI) * 0.25f;
                txtProgressPercent.transform.localScale = origScale * s;
                yield return null;
            }
            txtProgressPercent.transform.localScale = origScale;
        }

        yield return new WaitForSecondsRealtime(0.15f);

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
