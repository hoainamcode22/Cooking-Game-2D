using UnityEngine;

public class HomeSceneUI : MonoBehaviour
{
    public void GoToCooking()
    {
        SceneLoader.Instance.LoadScene("SampleScene");
    }

    public void GoToFarm()
    {
        SceneLoader.Instance.LoadScene("SCN_Farm");
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