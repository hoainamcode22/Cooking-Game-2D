using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    private void Start()
    {
        SceneManager.LoadScene("SCN_Home");
    }
}