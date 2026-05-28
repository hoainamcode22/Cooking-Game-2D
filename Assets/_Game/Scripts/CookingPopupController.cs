using System.Collections;
using UnityEngine;

public class CookingPopupController : MonoBehaviour
{
    [Header("Fail Message")]
    [SerializeField] private GameObject failMessageText;
    [SerializeField] private float failMessageDuration = 2f;

    [Header("Check Selection Popup")]
    [SerializeField] private GameObject checkSelectionPopup;
    [SerializeField] private float checkSelectionPopupDuration = 2f;

    [Header("Score Result")]
    [SerializeField] private ScoreResultBoxUI scoreResultBoxUI;

    [Header("Cooking Effect")]
    [SerializeField] private CookingEffectController cookingEffectController;

    private bool isShowingFailMessage = false;
    private bool isShowingCheckSelectionPopup = false;

    public bool IsShowingFailMessage => isShowingFailMessage;
    public bool IsShowingCheckSelectionPopup => isShowingCheckSelectionPopup;

    public void ShowFailMessage()
    {
        StartCoroutine(ShowFailMessageRoutine());
    }

    public void ShowCheckSelectionPopup()
    {
        StartCoroutine(ShowCheckSelectionPopupRoutine());
    }

    public void ShowScoreResultPopup(CookingScoreResult result, bool isSuccess, DishData dishData)
    {
        Debug.Log("CALL SHOW SCORE RESULT POPUP | isSuccess = " + isSuccess);
        StartCoroutine(ShowScoreResultPopupRoutine(result, isSuccess, dishData));
    }

    private IEnumerator ShowFailMessageRoutine()
    {
        isShowingFailMessage = true;

        if (failMessageText != null)
        {
            failMessageText.SetActive(true);
        }

        yield return new WaitForSeconds(failMessageDuration);

        if (failMessageText != null)
        {
            failMessageText.SetActive(false);
        }

        isShowingFailMessage = false;
    }

    private IEnumerator ShowCheckSelectionPopupRoutine()
    {
        isShowingCheckSelectionPopup = true;

        if (checkSelectionPopup != null)
        {
            checkSelectionPopup.SetActive(true);
        }

        yield return new WaitForSeconds(checkSelectionPopupDuration);

        if (checkSelectionPopup != null)
        {
            checkSelectionPopup.SetActive(false);
        }

        isShowingCheckSelectionPopup = false;
    }

    public IEnumerator ShowScoreResultPopupRoutine(CookingScoreResult result, bool isSuccess, DishData dishData)
    {
        Debug.Log("CALL SHOW SCORE RESULT POPUP | isSuccess = " + isSuccess);

        if (scoreResultBoxUI != null)
        {
            scoreResultBoxUI.ShowResult(result, isSuccess);
        }
        else
        {
            Debug.LogWarning("ScoreResultBoxUI is missing.");
        }

        yield return new WaitForSeconds(3f);

        if (scoreResultBoxUI != null)
        {
            scoreResultBoxUI.Hide();
        }

        if (isSuccess && cookingEffectController != null)
        {
            cookingEffectController.ShowCookedDishOnPlate(dishData);
        }
    }
}