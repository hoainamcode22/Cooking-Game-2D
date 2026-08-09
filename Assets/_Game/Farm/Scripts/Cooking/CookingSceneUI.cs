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
        if (currentScene.IsValid() && currentScene.isLoaded)
            SceneTransitionManager.Instance.UnloadScene(currentScene.name, SceneTransitionManager.TransitionType.BoardDrop);
    }
}