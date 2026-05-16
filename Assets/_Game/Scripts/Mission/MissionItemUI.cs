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

    [Header("Trạng thái nút")]
    [SerializeField] private GameObject obj_BtnNormal;
    [SerializeField] private GameObject obj_BtnClaimed;

    private MissionData _data;
    private bool _claimed;

    public void Setup(MissionData data)
    {
        _data   = data;
        _claimed = false;

        if (img_Icon    != null && data.missionIcon  != null) img_Icon.sprite    = data.missionIcon;
        if (img_Reward  != null && data.rewardIcon   != null) img_Reward.sprite  = data.rewardIcon;

        if (txt_MissionName  != null) txt_MissionName.text  = data.missionName;
        if (txt_RewardAmount != null) txt_RewardAmount.text  = data.rewardAmount.ToString();

        btn_Claim.onClick.RemoveAllListeners();
        btn_Claim.onClick.AddListener(OnClaimClicked);

        SetClaimedState(false);
        UpdateProgress(0);
    }

    public void UpdateProgress(int currentAmount)
    {
        if (_claimed) return;

        if (txt_Progress != null)
            txt_Progress.text = $"{Mathf.Min(currentAmount, _data.targetAmount)}/{_data.targetAmount}";

        // Cập nhật trạng thái nút dựa trên tiến độ nhiệm vụ
        bool canClaim = currentAmount >= _data.targetAmount;
        btn_Claim.interactable = canClaim;

        // Đổi text nút: "Tiến hành" khi chưa đủ, "Nhận" khi đã hoàn thành
        if (txt_BtnClaim != null)
            txt_BtnClaim.text = canClaim ? "Nhận" : "Tiến hành";
    }

    private void OnClaimClicked()
    {
        if (_claimed || _data == null) return;

        if (_data.rewardType == RewardType.Coin)
            PlayerWallet.Instance?.AddCoin(_data.rewardAmount);
        else
            PlayerWallet.Instance?.AddDiamond(_data.rewardAmount);

        _claimed = true;
        SetClaimedState(true);

        Debug.Log($"[Mission] Đã nhận thưởng: {_data.missionName} — +{_data.rewardAmount} {_data.rewardType}");
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
