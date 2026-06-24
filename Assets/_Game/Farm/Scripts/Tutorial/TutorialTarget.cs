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
        TutorialManager.UnregisterTarget(targetID, this);
    }

    void OnEnable()  => TutorialManager.RegisterTarget(targetID, this);
    void OnDisable() => TutorialManager.UnregisterTarget(targetID, this);

    /// <summary>Set ID at runtime and re-register. Used by TutorialRuntimeTargetResolver.</summary>
    public void SetTargetId(string id)
    {
        if (!string.IsNullOrEmpty(targetID) && targetID != id)
            TutorialManager.UnregisterTarget(targetID, this);

        targetID = id;
        if (!string.IsNullOrEmpty(id))
            TutorialManager.RegisterTarget(id, this);
    }
}
