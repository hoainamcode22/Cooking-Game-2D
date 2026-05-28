using UnityEngine;

[CreateAssetMenu(fileName = "PenConfig_", menuName = "Farm/Pen Mini Panel Config")]
public class PenMiniPanelConfig : ScriptableObject
{
    [Header("Identity")]
    public string penId; // "pen_01" .. "pen_04" — dùng làm key PlayerPrefs

    [Header("Thức ăn")]
    public string food1ItemId;   // "rice"
    public string food2ItemId;   // "ngo" / "bapcai" / "carot"
    public Sprite food1Icon;
    public Sprite food2Icon;

    [Header("Sản phẩm")]
    public string productItemId;          // "beef" / "pork" / "chicken_meat" / "milk"
    public string secondProductItemId;    // "egg" — chỉ gà, để trống nếu không dùng
    public Sprite productIcon;
    public Sprite secondProductIcon;      // icon trứng

    [Header("Rổ thu hoạch")]
    public Sprite basketIcon;

    [Header("Thời gian & EXP")]
    public float feedDurationSeconds = 120f;
    public int expReward = 10;
}
