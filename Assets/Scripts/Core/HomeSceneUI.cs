using UnityEngine;

public class HomeSceneUI : MonoBehaviour
{
    public void GoToCooking()
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene("SampleScene", SceneTransitionManager.TransitionType.CloudWipe);
        else
            SceneLoader.Instance.LoadScene("SampleScene");
    }

    public void GoToFarm()
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene("SCN_Farm", SceneTransitionManager.TransitionType.CloudWipe);
        else
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