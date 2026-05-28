using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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


    private char[] sequence;
    private int[] resultStates;
    private int currentIndex;
    private int correctCount;
    private int wrongCount;
    private bool isPlaying;

    private Action<bool> onFinished;

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
    }

    private void Update()
    {
        if (!isPlaying) return;

        Debug.Log("Letter mini game is playing");

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
            Debug.Log("Pressed Letter: " + pressedChar);
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

        onFinished?.Invoke(isSuccess);
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