using UnityEngine;

// Gắn component này lên bất kỳ UI RectTransform nào để đăng ký nó là Tutorial Target.
// TutorialManager sẽ tìm và dùng nó để đục lỗ màn hình đúng chỗ.
[RequireComponent(typeof(RectTransform))]
public class TutorialTarget : MonoBehaviour
{
    [Tooltip("Phải khớp chính xác với targetID trong TutorialStepData")]
    public string targetID;

    public RectTransform RectTransform { get; private set; }

    void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        TutorialManager.RegisterTarget(targetID, this);
    }

    void OnDestroy()
    {
        TutorialManager.UnregisterTarget(targetID);
    }

    void OnEnable()  => TutorialManager.RegisterTarget(targetID, this);
    void OnDisable() => TutorialManager.UnregisterTarget(targetID);
}
