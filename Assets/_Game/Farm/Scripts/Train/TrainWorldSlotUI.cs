// ============================================================
// DEPRECATED — replaced by TrainWagonSlot.cs
//
// This class is kept ONLY so that any existing scene references
// do not cause missing-script errors. It does nothing.
// Do NOT add this component to new GameObjects.
// Use TrainWagonSlot on WorldSlot_01..04 instead.
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// OBSOLETE. Replaced by TrainWagonSlot.
/// Kept as a compile-safe stub so existing scenes don't break.
/// </summary>
[System.Obsolete("Use TrainWagonSlot instead.")]
public class TrainWorldSlotUI : MonoBehaviour
{
#pragma warning disable 0649
    [Header("DEPRECATED — use TrainWagonSlot instead")]
    [SerializeField] private Image    iconImage;
    [SerializeField] private TMP_Text txtLabel;
    [SerializeField] private Button   clickButton;
    [SerializeField] private int      slotIndex = 0;
#pragma warning restore 0649

    void Awake()
    {
        Debug.LogWarning(
            $"[TrainWorldSlotUI] '{gameObject.name}' still has the OBSOLETE TrainWorldSlotUI component. " +
            "Remove it and add TrainWagonSlot instead.", this);
    }

    // Stub — no-op so nothing breaks at runtime
    public void ShowCargo(TrainCargoSlot  slot) { }
    public void ShowReward(TrainRewardSlot slot) { }
    public void Hide() { }
}

// Keep the old enum so any lingering scene serialization doesn't break.
// TrainWagonSlot uses TrainWagonSlotMode instead.
public enum TrainSlotMode { Cargo, Reward }
