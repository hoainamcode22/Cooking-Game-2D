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
        if (isSuccess)
        {
            StartCoroutine(CelebrationRoutine());
        }
        else
        {
            StartCoroutine(BlackSmokeRoutine());
        }

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

    private IEnumerator CelebrationRoutine()
    {
        if (scoreResultBoxUI != null)
        {
            Transform tr = scoreResultBoxUI.transform;
            tr.localScale = Vector3.zero;
            float t = 0;
            while(t < 0.5f)
            {
                t += Time.deltaTime;
                tr.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.2f, t / 0.5f);
                tr.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(-180, 0, t / 0.5f));
                yield return null;
            }
            tr.localScale = Vector3.one;
            tr.localEulerAngles = Vector3.zero;
            CreateConfetti();
        }
    }
    
    private void CreateConfetti()
    {
        for (int i = 0; i < 20; i++)
        {
            GameObject confetti = new GameObject("Confetti");
            confetti.transform.SetParent(this.transform, false);
            UnityEngine.UI.Image img = confetti.AddComponent<UnityEngine.UI.Image>();
            img.color = Random.ColorHSV();
            confetti.transform.position = this.transform.position;
            StartCoroutine(ConfettiFall(confetti));
        }
    }
    
    private IEnumerator ConfettiFall(GameObject obj)
    {
        Vector3 pos = obj.transform.position;
        Vector3 vel = new Vector3(Random.Range(-300f, 300f), Random.Range(300f, 600f), 0);
        float t = 0;
        while(t < 2f)
        {
            t += Time.deltaTime;
            vel.y -= 1000f * Time.deltaTime;
            pos += vel * Time.deltaTime;
            if(obj != null)
            {
                obj.transform.position = pos;
                obj.transform.Rotate(0, 0, 500f * Time.deltaTime);
            }
            yield return null;
        }
        if(obj != null) Destroy(obj);
    }

    private IEnumerator BlackSmokeRoutine()
    {
        for (int i = 0; i < 10; i++)
        {
            GameObject smoke = new GameObject("BlackSmoke");
            smoke.transform.SetParent(this.transform, false);
            UnityEngine.UI.Image img = smoke.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            smoke.transform.position = this.transform.position;
            StartCoroutine(SmokeRise(smoke));
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator SmokeRise(GameObject obj)
    {
        Vector3 pos = obj.transform.position;
        Vector3 vel = new Vector3(Random.Range(-100f, 100f), Random.Range(100f, 300f), 0);
        float scale = Random.Range(0.5f, 1.5f);
        float t = 0;
        while(t < 1.5f)
        {
            t += Time.deltaTime;
            pos += vel * Time.deltaTime;
            if(obj != null)
            {
                obj.transform.position = pos;
                obj.transform.localScale = Vector3.one * (scale + t);
                UnityEngine.UI.Image img = obj.GetComponent<UnityEngine.UI.Image>();
                Color c = img.color;
                c.a = 1f - (t / 1.5f);
                img.color = c;
            }
            yield return null;
        }
        if(obj != null) Destroy(obj);
    }
}
