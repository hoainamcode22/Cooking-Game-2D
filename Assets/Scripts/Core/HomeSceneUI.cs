using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeSceneUI : MonoBehaviour
{
    public void GoToCooking()
    {
        SceneManager.LoadScene("SCN_Cooking");
    }

    public void GoToFarm()
    {
        SceneManager.LoadScene("SCN_Farm");
    }

    public void OpenDailyReward()
    {
        Debug.Log("Open Daily Reward");
    }

    public void OpenQuests()
    {
        Debug.Log("Open Quests");
    }

    public void OpenEvent()
    {
        Debug.Log("Open Event");
    }

    public void OpenShop()
    {
        Debug.Log("Open Shop");
    }
}