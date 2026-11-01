using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text amountText;


    private InventoryUI owner;
    private StorageContainer storage;
    public InventoryUI Owner => owner;
    public StorageContainer Storage => storage;
    public int SlotIndex => slotIndex;
    private int slotIndex;
    private InventorySlot displayedSlot;
    private ItemData draggedItem;
    private bool dragging;

    public void Bind(InventoryUI inventoryUI, int index)
    {
        Bind(inventoryUI, index, null);
    }

    public void Bind(InventoryUI inventoryUI, int index, StorageContainer container)
    {
        owner = inventoryUI;
        slotIndex = index;
        storage = container;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left ||
            owner == null || displayedSlot == null || displayedSlot.IsEmpty)
            return;
        draggedItem = displayedSlot.item;
        dragging = owner.TryBeginDrag(slotIndex, draggedItem, eventData, storage);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragging && owner != null)
            owner.MoveDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;
        dragging = false;
        if (owner != null)
            owner.EndDrag(slotIndex, draggedItem, eventData, storage);
        draggedItem = null;
    }

    private void OnDisable()
    {
        if (dragging && owner != null)
            owner.CancelDrag();
        dragging = false;
        draggedItem = null;
    }

    public void Refresh(InventorySlot slot)
    {
        displayedSlot = slot;
        if (slot == null || slot.IsEmpty)
        {
            Clear();
            return;
        }


        // Item icon
        itemIcon.sprite = slot.item.Icon;
        itemIcon.enabled = slot.item.Icon != null;


        // Amount
        amountText.text = "x" + slot.amount;
        amountText.enabled = slot.amount > 0;
    }


    public void Clear()
    {
        displayedSlot = null;
        itemIcon.sprite = null;
        itemIcon.enabled = false;

        amountText.text = "";
        amountText.enabled = false;
    }
}