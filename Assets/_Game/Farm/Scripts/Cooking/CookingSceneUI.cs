using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// View controller cho scene Bếp.
/// Khi bấm nút "Về Farm", ủy quyền cho FarmUIManager để unload scene —
/// FarmUIManager tồn tại trên Farm scene nên coroutine không bị cắt khi scene Bếp unload.
/// </summary>
public class CookingSceneUI : MonoBehaviour
{
    [SerializeField] private Button btnBackFarm;

    private void Awake()
    {
        if (btnBackFarm != null)
            btnBackFarm.onClick.AddListener(BackToFarm);
    }

    private void OnDestroy()
    {
        if (btnBackFarm != null)
            btnBackFarm.onClick.RemoveListener(BackToFarm);
    }

    public void BackToFarm()
    {
        if (FarmUIManager.Instance != null)
            FarmUIManager.Instance.ReturnFromCooking();
        else
            Debug.LogWarning("[CookingSceneUI] FarmUIManager.Instance là NULL — không thể về Farm.");
    }
}
