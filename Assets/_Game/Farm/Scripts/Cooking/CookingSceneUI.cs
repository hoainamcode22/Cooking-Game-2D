using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CookingSceneUI : MonoBehaviour
{
    [SerializeField] private Button btnBackFarm;

    [Header("Optional - Hide in Cooking")]
    [SerializeField] private GameObject[] rootsToHide;

    private void Awake()
    {
        if (btnBackFarm != null)
            btnBackFarm.onClick.AddListener(BackToFarm);

        if (FarmUIManager.Instance != null)
            FarmUIManager.Instance.EnterCookingMode();

        ApplyRootsToHide(false);
    }

    private void ApplyRootsToHide(bool visible)
    {
        if (rootsToHide == null || rootsToHide.Length == 0)
            return;

        for (int i = 0; i < rootsToHide.Length; i++)
        {
            if (rootsToHide[i] != null)
                rootsToHide[i].SetActive(visible);
        }
    }

    private void OnDestroy()
    {
        ApplyRootsToHide(true);

        if (FarmUIManager.Instance != null)
            FarmUIManager.Instance.ExitCookingMode();
    }

    public void BackToFarm()
    {
        if (FarmUIManager.Instance != null)
            FarmUIManager.Instance.ExitCookingMode();

        Scene currentScene = gameObject.scene;
        bool isFarmLoaded = SceneManager.GetSceneByName("SCN_Farm").isLoaded;

        if (isFarmLoaded)
        {
            if (SceneTransitionManager.Instance != null && currentScene.IsValid() && currentScene.isLoaded)
                SceneTransitionManager.Instance.UnloadScene(currentScene.name, SceneTransitionManager.TransitionType.BoardDrop);
            else if (currentScene.IsValid() && currentScene.isLoaded)
                SceneManager.UnloadSceneAsync(currentScene.name);
        }
        else
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.LoadScene("SCN_Farm", SceneTransitionManager.TransitionType.CloudWipe, LoadSceneMode.Single);
            else if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene("SCN_Farm");
            else
                SceneManager.LoadScene("SCN_Farm");
        }
    }
}