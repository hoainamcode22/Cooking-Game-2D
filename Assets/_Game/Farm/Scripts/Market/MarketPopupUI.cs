using UnityEngine;
using UnityEngine.UI;

public class MarketPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button btnClose;

    private void Start()
    {
        if (btnClose != null)
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(ClosePopup);
        }
    }

    // true khi popup đang thực sự hiển thị
    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    public void OpenPopup()
    {
        if (popupRoot != null)
        {
            Transform parent = popupRoot.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf)
                    parent.gameObject.SetActive(true);

                parent = parent.parent;
            }
        }

        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
            SetCanvasGroups(popupRoot, true);
        }
    }

    public void ClosePopup()
    {
        if (popupRoot != null)
        {
            SetCanvasGroups(popupRoot, false);
            popupRoot.SetActive(false);
        }
    }

    private static void SetCanvasGroups(GameObject root, bool active)
    {
        CanvasGroup[] groups = root.GetComponentsInParent<CanvasGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            groups[i].alpha = active ? 1f : 0f;
            groups[i].interactable = active;
            groups[i].blocksRaycasts = active;
        }

        CanvasGroup[] childGroups = root.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < childGroups.Length; i++)
        {
            childGroups[i].alpha = active ? 1f : 0f;
            childGroups[i].interactable = active;
            childGroups[i].blocksRaycasts = active;
        }
    }
}
