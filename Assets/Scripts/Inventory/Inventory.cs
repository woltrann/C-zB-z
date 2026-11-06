
using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("Equipped Bag")]
    [SerializeField] private BagData startingBag;

    [Header("Inventory")]
    [SerializeField]
    private List<InventorySlot> slots =
        new List<InventorySlot>();

    public IReadOnlyList<InventorySlot> Slots => slots;
    public int SlotCount => slots.Count;
    public BagData EquippedBag => startingBag;

    public event Action OnInventoryChanged;
    public void NotifyChanged() => OnInventoryChanged?.Invoke();

    private void Awake()
    {
        if (startingBag == null)
        {
            Debug.LogError(
                "Inventory: Starting Bag atanmamýþ!",
                this
            );
            return;
        }

        InitializeInventory(startingBag.SlotCount);
    }

    private void InitializeInventory(int capacity)
    {
        while (slots.Count < capacity)
        {
            slots.Add(new InventorySlot());
        }

        while (slots.Count > capacity)
        {
            slots.RemoveAt(slots.Count - 1);
        }
    }

    public bool EquipBag(BagData newBag)
    {
        if (newBag == null)
        {
            Debug.LogWarning("EquipBag: Çanta boþ.");
            return false;
        }

        int newCapacity = newBag.SlotCount;

        if (newCapacity < slots.Count)
        {
            for (int i = newCapacity; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    Debug.LogWarning(
                        "Bu çanta daha küçük. Önce taþan " +
                        "slotlardaki eþyalarý boþaltmalýsýn."
                    );
                    return false;
                }
            }
        }

        InitializeInventory(newCapacity);
        startingBag = newBag;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool AddItem(ItemData item, int amount)
    {
        if (item == null)
        {
            Debug.LogWarning("AddItem: ItemData null.");
            return false;
        }

        if (amount <= 0)
        {
            Debug.LogWarning(
                "AddItem: Amount 0 veya negatif olamaz."
            );
            return false;
        }

        // Reject the whole pickup before changing slots if it cannot fit.
        // WorldItem stays in the world when AddItem returns false.
        if (item.MaxStack <= 0)
            return false;

        int requiredSpace = amount;

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            int space = slot.IsEmpty
                ? item.MaxStack
                : slot.item == item
                    ? Mathf.Max(0, item.MaxStack - slot.amount)
                    : 0;

            requiredSpace -= Mathf.Min(space, requiredSpace);

            if (requiredSpace == 0)
                break;
        }

        if (requiredSpace > 0)
            return false;

        int remainingAmount = amount;

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot.IsEmpty || slot.item != item)
                continue;

            int space = item.MaxStack - slot.amount;

            if (space <= 0)
                continue;

            int amountToAdd = Mathf.Min(
                space,
                remainingAmount
            );

            slot.amount += amountToAdd;
            remainingAmount -= amountToAdd;

            if (remainingAmount <= 0)
            {
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (!slot.IsEmpty)
                continue;

            int amountToAdd = Mathf.Min(
                item.MaxStack,
                remainingAmount
            );

            slot.item = item;
            slot.amount = amountToAdd;
            remainingAmount -= amountToAdd;

            if (remainingAmount <= 0)
            {
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        OnInventoryChanged?.Invoke();

        Debug.Log(
            "Envanter tamamen doldurulamadý. Eksik miktar: " +
            remainingAmount
        );

        return false;
    }

    public bool TryDropSlot(int slotIndex, ItemData expectedItem, int amount)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count || expectedItem == null || amount <= 0)
            return false;

        InventorySlot slot = slots[slotIndex];
        if (slot.IsEmpty || slot.item != expectedItem || amount > slot.amount)
            return false;

        GameObject prefab = expectedItem.WorldPrefab;
        if (prefab == null || prefab.GetComponent<WorldItem>() == null)
        {
            Debug.LogWarning("Inventory: World drop prefab missing for " + expectedItem.ItemName, this);
            return false;
        }

        Vector3 position = transform.position + transform.forward * 2f + Vector3.up;
        if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out RaycastHit hit,
            5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            position = hit.point + Vector3.up * 0.5f;

        GameObject droppedObject = Instantiate(prefab, position, prefab.transform.rotation);
        droppedObject.GetComponent<WorldItem>().Initialize(expectedItem, amount);
        foreach (Outline outline in droppedObject.GetComponentsInChildren<Outline>())
            outline.enabled = false;

        slot.amount -= amount;
        if (slot.amount == 0)
            slot.Clear();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        if (!HasItem(item, amount))
            return false;

        int remainingAmount = amount;

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot.IsEmpty || slot.item != item)
                continue;

            int amountToRemove = Mathf.Min(
                slot.amount,
                remainingAmount
            );

            slot.amount -= amountToRemove;
            remainingAmount -= amountToRemove;

            if (slot.amount <= 0)
                slot.Clear();

            if (remainingAmount <= 0)
                break;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(
        ItemData item,
        int requiredAmount = 1)
    {
        if (item == null)
            return false;

        if (requiredAmount <= 0)
            return true;

        return GetItemCount(item) >= requiredAmount;
    }

    public int GetItemCount(ItemData item)
    {
        if (item == null)
            return 0;

        int totalAmount = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (!slot.IsEmpty && slot.item == item)
                totalAmount += slot.amount;
        }

        return totalAmount;
    }

    // Preflight the entire refund before committing any slot changes.
    public bool TryAddItems(IReadOnlyDictionary<ItemData, int> items)
    {
        if (items == null) return false;
        if (items.Count == 0) return true;
        var simulatedItems = new ItemData[slots.Count];
        var simulatedAmounts = new int[slots.Count];
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null || slots[i].IsEmpty) continue;
            simulatedItems[i] = slots[i].item;
            simulatedAmounts[i] = slots[i].amount;
        }
        foreach (var entry in items)
        {
            if (entry.Key == null || entry.Value <= 0 || entry.Key.MaxStack <= 0) return false;
            int remaining = entry.Value;
            for (int pass = 0; pass < 2 && remaining > 0; pass++)
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                bool empty = simulatedItems[i] == null;
                if (pass == 0 ? empty || simulatedItems[i] != entry.Key : !empty) continue;
                int available = Mathf.Max(0, entry.Key.MaxStack - simulatedAmounts[i]);
                int added = Mathf.Min(remaining, available);
                if (added == 0) continue;
                simulatedItems[i] = entry.Key;
                simulatedAmounts[i] += added;
                remaining -= added;
            }
            if (remaining != 0) return false;
        }
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) slots[i] = new InventorySlot();
            slots[i].item = simulatedItems[i];
            slots[i].amount = simulatedAmounts[i];
        }
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return;

        slots[slotIndex].Clear();
        OnInventoryChanged?.Invoke();
    }

    public void ClearInventory()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].Clear();
        }

        OnInventoryChanged?.Invoke();
    }
}
