using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Minigame bấm chữ theo thứ tự.
///
/// ── SỬA CHO MOBILE (game phát hành Android/iOS) ─────────────────────────────
/// TRƯỚC: chỉ đọc bàn phím (Input.GetKeyDown(KeyCode.A + i) / Keyboard.current) và
/// trong cả file KHÔNG có Button nào ⇒ trên điện thoại KHÔNG CHƠI ĐƯỢC. Lỗi CHẶN
/// GAMEPLAY.
///
/// NAY: <b>tự sinh HÀNG NÚT CHỮ trên UI</b> mỗi lượt chơi — bấm nút = bấm phím
/// tương ứng, đi vào đúng <see cref="CheckInput"/> cũ (logic đúng/sai/kết thúc
/// KHÔNG đổi một dòng).
///   • Nút = các chữ CÓ TRONG chuỗi lượt này (bỏ trùng, xáo trộn) — hiện 26 chữ
///     trên điện thoại thì nút bé như hạt gạo, không bấm nổi. Muốn khó hơn thì tăng
///     <c>soChuGiaThem</c> để chèn chữ nhiễu (mặc định 0 = giữ đúng độ khó cũ).
///   • Cỡ nút tính theo PIXEL THẬT của máy (mặc định 90px, khuyến nghị tối thiểu của
///     Apple/Google), quy đổi qua <c>Canvas.scaleFactor</c> nên đúng trên mọi mật độ
///     điểm ảnh; nhiều chữ thì hàng tự co lại cho vừa bề rộng màn hình.
///   • HorizontalLayoutGroup + ContentSizeFitter: hàng tự giãn theo số chữ, tự căn giữa.
///   • Bàn phím GIỮ NGUYÊN song song để Sếp test bằng máy tính.
/// </summary>
public class LetterMiniGame : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text sequenceText;
    [SerializeField] private TMP_Text guideText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Image timerFill;

    [Header("Setting")]
    [SerializeField] private int sequenceLength = 5;
    [SerializeField] private int requiredCorrect = 3;
    [SerializeField] private int maxWrong = 2;
    private float timeLimit;
    private float currentTime;

    [Header("Easy Settings")]
    [SerializeField] private float TimeEasy = 10f;


    [Header("Normal Settings")]
    [SerializeField] private float TimeNormal = 8f;


    [Header("Hard Settings")]
    [SerializeField] private float TimeHard = 6f;
    [Header("Interaction Blocker")]
    [SerializeField] private GameObject interactionBlocker;

    [Header("Mobile — hàng nút chữ")]
    [Tooltip("Sinh hàng nút chữ để chơi bằng ngón tay. TẮT = chỉ còn bàn phím (đừng tắt ở bản mobile).")]
    [SerializeField] private bool hienHangNutChu = true;

    [Tooltip("Chỗ chứa hàng nút (tuỳ chọn). Để TRỐNG thì script tự dựng dưới panel.")]
    [SerializeField] private RectTransform hangNutChuCoSan;

    [Tooltip("Cỡ nút theo PIXEL THẬT của máy. 90 là mức tối thiểu Apple/Google khuyến nghị cho vùng chạm.")]
    [SerializeField] private float coNutPixel = 90f;

    [Tooltip("Số chữ NHIỄU chèn thêm ngoài các chữ có trong chuỗi. 0 = giữ đúng độ khó bản cũ.")]
    [SerializeField] private int soChuGiaThem = 0;

    [Tooltip("Khoảng cách giữa 2 nút (canvas unit).")]
    [SerializeField] private float khoangCachNut = 14f;


    private char[] sequence;
    private int[] resultStates;
    private int currentIndex;
    private int correctCount;
    private int wrongCount;
    private bool isPlaying;

    private Action<bool> onFinished;

    // ── Mobile runtime ──────────────────────────────────────────────────────
    private RectTransform      _hangNut;
    private readonly List<Button> _nutChu = new List<Button>();

    public void StartMiniGame(DishDifficulty difficulty,Action<bool> callback)
    {
        onFinished = callback;
        ApplyDifficulty(difficulty);
        if (interactionBlocker != null)
        {
            interactionBlocker.SetActive(true);
            interactionBlocker.transform.SetAsLastSibling();
        }

        panel.SetActive(true);
        sequenceText.fontSize = 60;
        sequenceText.fontStyle = FontStyles.Bold;

        sequence = new char[sequenceLength];
        resultStates = new int[sequenceLength];

        for (int i = 0; i < sequenceLength; i++)
        {
            sequence[i] = (char)('A' + UnityEngine.Random.Range(0, 26));
            resultStates[i] = 0;
        }

        currentIndex = 0;
        correctCount = 0;
        wrongCount = 0;
        currentTime = timeLimit;
        if (timerFill != null)
        {
            timerFill.fillAmount = 1f ;
        }
        isPlaying = true;

        guideText.text = "Nhấn đúng các chữ cái theo thứ tự";
        UpdateUI();

        // Mobile: dựng lại hàng nút theo chuỗi vừa random
        DungHangNutChu();
    }

    private void Update()
    {
        if (!isPlaying) return;


        currentTime -= Time.deltaTime;

        if (timerFill != null)
        {
            timerFill.fillAmount = currentTime / timeLimit;
        }

        if (currentTime <= 0)
        {
            FinishMiniGame(correctCount >= requiredCorrect);
            return;
        }

        ReadKeyboardInput();
    }

    private void ReadKeyboardInput()
    {
        if (TryGetPressedLetter(out char pressedChar))
        {
            CheckInput(pressedChar);
        }
    }
    private void CheckInput(char pressedChar)
    {
        if (!isPlaying) return;

        char expectedChar = sequence[currentIndex];

        if (pressedChar == expectedChar)
        {
            correctCount++;
            resultStates[currentIndex] = 1;
        }
        else
        {
            wrongCount++;
            resultStates[currentIndex] = -1;
        }

        currentIndex++;

        UpdateUI();

        if (wrongCount > maxWrong)
        {
            FinishMiniGame(false);
            return;
        }

        if (currentIndex >= sequenceLength)
        {
            bool isSuccess = correctCount >= requiredCorrect;
            FinishMiniGame(isSuccess);
        }
    }

    private void UpdateUI()
    {
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < sequence.Length; i++)
        {
            if (resultStates[i] == 1)
            {
                builder.Append("<color=green>");
                builder.Append(sequence[i]);
                builder.Append("</color>");
            }
            else if (resultStates[i] == -1)
            {
                builder.Append("<color=red>");
                builder.Append(sequence[i]);
                builder.Append("</color>");
            }
            else if (i == currentIndex)
            {
                builder.Append("<color=#FFC300>");
                builder.Append(sequence[i]);
                builder.Append("</color>");
            }
            else
            {
                builder.Append(sequence[i]);
            }

            builder.Append(" ");
        }

        sequenceText.text = builder.ToString();

        resultText.text = $"Đúng: {correctCount}/{sequenceLength} | Sai: {wrongCount}/{maxWrong + 1}";
    }

    private void FinishMiniGame(bool isSuccess)
    {
        if (!isPlaying) return;
        if (interactionBlocker != null)
        {
            interactionBlocker.SetActive(false);
        }

        isPlaying = false;
        panel.SetActive(false);

        AnHangNutChu(); // không để hàng nút nằm lại nhận chạm sau khi xong

        onFinished?.Invoke(isSuccess);
    }

    // =========================================================================
    //  MOBILE — hàng nút chữ
    // =========================================================================

    /// <summary>
    /// Dựng lại hàng nút cho lượt chơi hiện tại: chữ trong chuỗi (bỏ trùng) +
    /// soChuGiaThem chữ nhiễu, xáo trộn. Gọi mỗi lượt vì chuỗi random lại.
    /// </summary>
    private void DungHangNutChu()
    {
        if (!hienHangNutChu) { AnHangNutChu(); return; }
        if (panel == null) return;

        // 1. Tập chữ cần hiện
        var chuHien = new List<char>();
        for (int i = 0; i < sequence.Length; i++)
            if (!chuHien.Contains(sequence[i])) chuHien.Add(sequence[i]);

        for (int i = 0; i < Mathf.Max(0, soChuGiaThem) && chuHien.Count < 26; i++)
        {
            char c;
            int caiThoat = 0; // chống vòng lặp vô hạn khi gần hết 26 chữ
            do { c = (char)('A' + UnityEngine.Random.Range(0, 26)); caiThoat++; }
            while (chuHien.Contains(c) && caiThoat < 200);
            if (!chuHien.Contains(c)) chuHien.Add(c);
        }

        // Xáo trộn (Fisher-Yates) — không thì thứ tự nút chính là đáp án
        for (int i = chuHien.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            char tam = chuHien[i];
            chuHien[i] = chuHien[j];
            chuHien[j] = tam;
        }

        // 2. Chỗ chứa
        if (_hangNut == null)
        {
            _hangNut = hangNutChuCoSan != null ? hangNutChuCoSan : TaoHangNut();
            if (_hangNut == null) return;
        }
        _hangNut.gameObject.SetActive(true);

        // 3. Cỡ nút: pixel thật → canvas unit, và co lại nếu hàng vượt bề rộng
        float co = CoNutCanvasUnit(chuHien.Count);

        // 4. Tạo/tái dùng nút (tái dùng để không cấp phát lại mỗi lượt nấu)
        for (int i = 0; i < chuHien.Count; i++)
        {
            Button nut = i < _nutChu.Count ? _nutChu[i] : TaoMotNut();
            if (nut == null) continue;
            if (i >= _nutChu.Count) _nutChu.Add(nut);

            char chu = chuHien[i];

            var le = nut.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.minWidth = le.preferredWidth = co;
                le.minHeight = le.preferredHeight = co;
            }

            var label = nut.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text     = chu.ToString();
                label.fontSize = co * 0.5f;
            }

            nut.onClick.RemoveAllListeners();
            nut.onClick.AddListener(() => OnBamNutChu(chu)); // bắt biến chu theo từng vòng
            nut.interactable = true;
            nut.gameObject.SetActive(true);
        }

        // Nút thừa của lượt trước → tắt, không destroy
        for (int i = chuHien.Count; i < _nutChu.Count; i++)
            if (_nutChu[i] != null) _nutChu[i].gameObject.SetActive(false);

        var layout = _hangNut.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) layout.spacing = khoangCachNut;
    }

    /// <summary>Bấm nút chữ = bấm phím chữ đó — đi vào đúng CheckInput cũ.</summary>
    private void OnBamNutChu(char chu)
    {
        if (!isPlaying) return;
        CheckInput(chu);
    }

    private RectTransform TaoHangNut()
    {
        var go = new GameObject("HangNutChu", typeof(RectTransform));
        go.transform.SetParent(panel.transform, false);

        var rt = (RectTransform)go.transform;
        // Neo đáy panel, tự giãn theo nội dung (ContentSizeFitter), căn giữa ngang
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -20f); // ngay dưới panel, không đè chuỗi chữ

        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment         = TextAnchor.MiddleCenter;
        layout.spacing                = khoangCachNut;
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth      = true;  // để LayoutElement quyết cỡ
        layout.childControlHeight     = true;

        var fitter = go.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        return rt;
    }

    private Button TaoMotNut()
    {
        if (_hangNut == null) return null;

        var go = new GameObject("Nut_Chu", typeof(RectTransform));
        go.transform.SetParent(_hangNut, false);

        var img = go.AddComponent<Image>();
        img.color         = new Color(0.98f, 0.86f, 0.55f); // vàng kem, nổi trên panel tối
        img.raycastTarget = true;

        var nut = go.AddComponent<Button>();
        nut.targetGraphic = img;

        go.AddComponent<LayoutElement>();

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var lrt = (RectTransform)labelGo.transform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.alignment     = TextAlignmentOptions.Center;
        label.fontStyle     = FontStyles.Bold;
        label.color         = new Color(0.30f, 0.20f, 0.08f);
        label.raycastTarget = false; // chữ không được nuốt click của nút

        return nut;
    }

    /// <summary>
    /// Quy cỡ nút từ PIXEL THẬT sang canvas unit qua Canvas.scaleFactor, rồi co lại
    /// nếu cả hàng vượt bề rộng canvas (nhiều chữ) — không thì hàng tràn ra ngoài
    /// màn hình và mấy chữ cuối không bấm được.
    /// </summary>
    private float CoNutCanvasUnit(int soNut)
    {
        Canvas canvas = _hangNut != null ? _hangNut.GetComponentInParent<Canvas>() : null;
        if (canvas != null && canvas.rootCanvas != null) canvas = canvas.rootCanvas;

        float scale = canvas != null ? canvas.scaleFactor : 1f;
        if (scale <= 0.0001f) scale = 1f;

        float co = Mathf.Max(1f, coNutPixel) / scale; // 90px thật → canvas unit

        // Giới hạn theo bề rộng canvas (chừa 8% lề 2 bên)
        var canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        if (canvasRect != null && soNut > 0)
        {
            float beRong = canvasRect.rect.width * 0.84f;
            float toiDa  = (beRong - khoangCachNut * (soNut - 1)) / soNut;
            if (toiDa > 8f && toiDa < co) co = toiDa;
        }

        return co;
    }

    private void AnHangNutChu()
    {
        if (_hangNut != null) _hangNut.gameObject.SetActive(false);
    }

    /// <summary>Object bị tắt giữa lượt → không để hàng nút nằm lại nhận chạm.</summary>
    private void OnDisable()
    {
        AnHangNutChu();
    }

    private bool TryGetPressedLetter(out char pressedChar)
    {
        pressedChar = '\0';

    #if ENABLE_LEGACY_INPUT_MANAGER
        for (int i = 0; i < 26; i++)
        {
            KeyCode keyCode = (KeyCode)((int)KeyCode.A + i);

            if (Input.GetKeyDown(keyCode))
            {
                pressedChar = (char)('A' + i);
                return true;
            }
        }
    #endif

    #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            for (int i = 0; i < 26; i++)
            {
                Key key = (Key)((int)Key.A + i);

                if (Keyboard.current[key].wasPressedThisFrame)
                {
                    pressedChar = (char)('A' + i);
                    return true;
                }
            }
        }
    #endif

        return false;
    }
    private void ApplyDifficulty(DishDifficulty difficulty)
    {
        switch (difficulty)
        {
            case DishDifficulty.Easy:
                timeLimit = TimeEasy;
                break;

            case DishDifficulty.Normal:
                timeLimit = TimeNormal;
                break;

            case DishDifficulty.Hard:
                timeLimit = TimeHard;
                break;

            default:
                timeLimit = TimeNormal;
                break;
        }
    }
}
