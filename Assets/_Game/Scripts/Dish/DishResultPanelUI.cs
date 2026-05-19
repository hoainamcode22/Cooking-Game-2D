using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel hiển thị tất cả món ăn đã nấu (có trong FarmInventoryManager).
/// Tự động sinh DishResultSlotUI từ prefab.
///
/// Cách dùng:
///   1. Kéo Panel_Dish GameObject vào scene, gắn component này.
///   2. Gán dishDatabase (All_Data.asset), slotPrefab, và slotContainer.
///   3. Gọi OpenPanel() từ button trong UI nông trại / kho.
/// </summary>
public class DishResultPanelUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ListDishData dishDatabase;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform  slotContainer;
    [SerializeField] private Button     btnClose;

    private readonly List<DishResultSlotUI> slots = new List<DishResultSlotUI>();

    private void Awake()
    {
        if (btnClose != null)
            btnClose.onClick.AddListener(ClosePanel);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Start()
    {
        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged += Refresh;
    }

    private void OnDestroy()
    {
        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged -= Refresh;
    }

    public void OpenPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        Refresh();
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    // ─── Refresh ──────────────────────────────────────────────────────────────

    public void Refresh()
    {
        if (dishDatabase == null || dishDatabase.allDishes == null) return;
        if (FarmInventoryManager.Instance == null) return;

        // Thu thập các món ăn có trong kho
        var dishesInInventory = new List<(DishData dish, int amount)>();
        foreach (var dish in dishDatabase.allDishes)
        {
            if (dish == null || string.IsNullOrEmpty(dish.dishId)) continue;

            int amount = FarmInventoryManager.Instance.GetAmount(dish.dishId);
            if (amount > 0)
                dishesInInventory.Add((dish, amount));
        }

        EnsureSlots(dishesInInventory.Count);

        for (int i = 0; i < slots.Count; i++)
        {
            if (i < dishesInInventory.Count)
                slots[i].SetData(dishesInInventory[i].dish, dishesInInventory[i].amount);
            else
                slots[i].SetEmpty();
        }
    }

    // ─── Pool ─────────────────────────────────────────────────────────────────

    private void EnsureSlots(int needed)
    {
        if (slotPrefab == null || slotContainer == null) return;

        while (slots.Count < needed)
        {
            var go   = Instantiate(slotPrefab, slotContainer, false);
            var slot = go.GetComponent<DishResultSlotUI>();
            if (slot == null) slot = go.AddComponent<DishResultSlotUI>();
            slots.Add(slot);
        }
    }
}
