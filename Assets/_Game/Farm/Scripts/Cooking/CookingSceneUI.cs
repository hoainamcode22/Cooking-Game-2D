using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CookingSceneUI : MonoBehaviour
{
    [SerializeField] private Button btnBackFarm;
    [SerializeField] private string farmSceneName = "SCN_Farm";

    private void Awake()
    {
        if (btnBackFarm != null)
            btnBackFarm.onClick.AddListener(BackToFarm);
    }

    public void BackToFarm()
    {
        SceneManager.LoadScene(farmSceneName);
    }
}