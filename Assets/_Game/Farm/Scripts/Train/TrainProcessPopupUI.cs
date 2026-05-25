using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrainProcessPopupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text txtStatus;
    [SerializeField] private TMP_Text txt_time;
    [SerializeField] private Button   Btn_close;

    public bool IsVisible => gameObject.activeSelf;
    public bool IsOpen    => gameObject.activeSelf;  // alias dùng chung với PopupManager

    void Awake()
    {
        if (Btn_close != null) Btn_close.onClick.AddListener(Hide);
        gameObject.SetActive(false);
    }

    public void Show(float totalTime)
    {
        gameObject.SetActive(true);
        if (txtStatus != null) txtStatus.text = "Đang vận chuyển...";
        UpdateTimer(totalTime);
    }

    public void ShowArrived()
    {
        gameObject.SetActive(true);
        if (txtStatus != null) txtStatus.text = "Tàu đã về!";
        if (txt_time  != null) txt_time.text  = "0:00";
    }

    public void UpdateTimer(float remaining)
    {
        if (txt_time == null) return;
        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
        int hours   = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        txt_time.text = string.Format("{0}:{1:D2}", hours, minutes);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
