using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    private IEnumerator Start()
    {
#if !UNITY_EDITOR
        // Tắt toàn bộ Debug.Log trong build để giảm lag
        Debug.unityLogger.logEnabled = false;
#endif
        yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync("SCN_Home");
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;
    }
}