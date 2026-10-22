
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
