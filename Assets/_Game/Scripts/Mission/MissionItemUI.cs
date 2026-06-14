using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image      img_Icon;
    [SerializeField] private TMP_Text   txt_MissionName;
    [SerializeField] private TMP_Text   txt_Progress;
    [SerializeField] private Button     btn_Claim;
    [SerializeField] private TMP_Text   txt_BtnClaim;
    [SerializeField] private Image      img_Reward;
    [SerializeField] private TMP_Text   txt_RewardAmount;

    [Header("Tráº¡ng thÃ¡i nÃºt")]
    [SerializeField] private GameObject obj_BtnNormal;
    [SerializeField] private GameObject obj_BtnClaimed;

    private MissionData _data;
    private bool _claimed;

    public void Setup(MissionData data)
    {
        _data   = data;
        _claimed = PlayerPrefs.GetInt(ClaimedPrefsKey(data), 0) == 1; // persist giữa các phiên

        if (img_Icon    != null && data.missionIcon  != null) img_Icon.sprite    = data.missionIcon;
        if (img_Reward  != null && data.rewardIcon   != null) img_Reward.sprite  = data.rewardIcon;

        if (txt_MissionName  != null) txt_MissionName.text  = data.missionName;
        if (txt_RewardAmount != null) txt_RewardAmount.text  = data.rewardAmount.ToString();

        btn_Claim.onClick.RemoveAllListeners();
        btn_Claim.onClick.AddListener(OnClaimClicked);

        SetClaimedState(_claimed);

        if (_claimed)
        {
            if (txt_Progress != null)
                txt_Progress.text = $"{data.targetAmount}/{data.targetAmount}";
        }
        else
        {
            UpdateProgress(0);
        }
    }

    public void UpdateProgress(int currentAmount)
    {
        if (_claimed) return;

        if (txt_Progress != null)
            txt_Progress.text = $"{Mathf.Min(currentAmount, _data.targetAmount)}/{_data.targetAmount}";

        // Cáº­p nháº­t tráº¡ng thÃ¡i nÃºt dá»±a trÃªn tiáº¿n Ä‘á»™ nhiá»‡m vá»¥
        bool canClaim = currentAmount >= _data.targetAmount;
        btn_Claim.interactable = canClaim;

        // Äá»•i text nÃºt: "Tiáº¿n hÃ nh" khi chÆ°a Ä‘á»§, "Nháº­n" khi Ä‘Ã£ hoÃ n thÃ nh
        if (txt_BtnClaim != null)
            txt_BtnClaim.text = canClaim ? "Nháº­n" : "Tiáº¿n hÃ nh";
    }

    private void OnClaimClicked()
    {
        if (_claimed || _data == null) return;

        if (_data.rewardType == RewardType.Coin)
            PlayerWallet.Instance?.AddCoin(_data.rewardAmount);
        else
            PlayerWallet.Instance?.AddDiamond(_data.rewardAmount);

        _claimed = true;
        PlayerPrefs.SetInt(ClaimedPrefsKey(_data), 1); // lưu trạng thái đã nhận
        PlayerPrefs.Save();

        SetClaimedState(true);
        AvatarProfilePopupUI.AddAchievementCount();

        Debug.Log($"[MissionItemUI] Claimed '{_data.MissionId}' → +{_data.rewardAmount} {_data.rewardType}");
    }

    /// <summary>Key PlayerPrefs lưu claimed. Mission daily kèm ngày → tự "reset" sang ngày mới.</summary>
    private static string ClaimedPrefsKey(MissionData data)
    {
        string id = data.MissionId;
        return data.isDaily
            ? $"MISSION_CLAIMED_DAILY_{System.DateTime.Now:yyyyMMdd}_{id}"
            : $"MISSION_CLAIMED_{id}";
    }

    private void SetClaimedState(bool claimed)
    {
        if (obj_BtnNormal  != null) obj_BtnNormal.SetActive(!claimed);
        if (obj_BtnClaimed != null) obj_BtnClaimed.SetActive(claimed);
        btn_Claim.interactable = !claimed;
    }

    public bool IsClaimed => _claimed;
    public MissionData Data => _data;
}
