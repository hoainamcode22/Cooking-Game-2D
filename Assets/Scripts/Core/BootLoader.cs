using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    private IEnumerator Start()
    {
        // Cho Unity hoàn tất frame khởi động trước khi load
        yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync("SCN_Home");
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;
    }
}