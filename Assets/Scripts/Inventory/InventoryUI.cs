
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private Image bagBackground;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Transform slotGrid;
    [SerializeField] private InventorySlotUI slotPrefab;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text capacityText;

    private readonly List<InventorySlotUI> slotUIs =
        new List<InventorySlotUI>();

    private BagData displayedBag;
    [SerializeField] private InventoryDropDialog dropDialog;
    private RectTransform dragRect;
    private Canvas rootCanvas;
    private static InventoryUI interactionOwner;
    private bool dragging;
    [SerializeField] private StorageUI storageUI;
    private StorageContainer activeStorage;
    private static InventoryUI storageOwner;
    private InventorySlot draggedSlot;
    private int draggedAmount;
    private readonly List<RaycastResult> dropHits = new List<RaycastResult>();

    public bool IsStorageOpen => activeStorage != null;
    public bool HasQuantityDialog => dropDialog != null && dropDialog.IsOpen;
    public bool IsDragging => dragging;

    public void OpenStorage(StorageContainer storage)
    {
        if (storage == null || storageUI == null || IsInputBlocked)
            return;
        activeStorage = storage;
        storageOwner = this;
        storage.Open(() =>
        {
            if (this != null && isActiveAndEnabled && activeStorage == storage && storage.isActiveAndEnabled)
                storageUI.Show(storage, this);
        });
    }

    public void CloseStorage()
    {
        if (dropDialog != null && dropDialog.IsOpen)
            dropDialog.Cancel();
        CancelDrag();
        if (storageUI != null)
            storageUI.Hide();
        if (activeStorage != null)
            activeStorage.Close();
        activeStorage = null;
        if (storageOwner == this)
            storageOwner = null;
    }

    private void Update()
    {
        if (storageOwner == this && (activeStorage == null || !activeStorage.isActiveAndEnabled))
            CloseStorage();
    }

    private InventorySlot GetSlot(int index, StorageContainer storage)
    {
        if (storage != null)
            return storage == activeStorage && index >= 0 && index < storage.SlotCount ? storage.Slots[index] : null;
        return inventory != null && index >= 0 && index < inventory.SlotCount ? inventory.Slots[index] : null;
    }

    public static bool IsInputBlocked => interactionOwner != null || storageOwner != null;

    public bool TryBeginDrag(int slotIndex, ItemData item, PointerEventData eventData)
    {
        return TryBeginDrag(slotIndex, item, eventData, null);
    }

    public bool TryBeginDrag(int slotIndex, ItemData item, PointerEventData eventData, StorageContainer storage)
    {
        InventorySlot source = GetSlot(slotIndex, storage);
        if (interactionOwner != null || (storageOwner != null && storageOwner != this) ||
            item == null || source == null || source.IsEmpty || source.item != item)
            return false;
        rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (rootCanvas == null)
            return false;

        interactionOwner = this;
        dragging = true;
        draggedSlot = source;
        draggedAmount = source.amount;
        GameObject icon = new GameObject("InventoryDragIcon", typeof(RectTransform), typeof(Image));
        dragRect = icon.GetComponent<RectTransform>();
        dragRect.SetParent(rootCanvas.transform, false);
        dragRect.sizeDelta = inventory.EquippedBag != null ? inventory.EquippedBag.SlotSize : new Vector2(64f, 64f);
        Image image = icon.GetComponent<Image>();
        image.sprite = item.Icon;
        image.preserveAspect = true;
        image.raycastTarget = false;
        MoveDrag(eventData);
        return true;
    }

    public void MoveDrag(PointerEventData eventData)
    {
        if (!dragging || dragRect == null || rootCanvas == null)
            return;
        Camera camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)rootCanvas.transform, eventData.position, camera, out Vector2 position))
            dragRect.anchoredPosition = position;
    }

    public void EndDrag(int slotIndex, ItemData item, PointerEventData eventData)
    {
        EndDrag(slotIndex, item, eventData, null);
    }

    public void EndDrag(int slotIndex, ItemData item, PointerEventData eventData, StorageContainer sourceStorage)
    {
        if (!dragging)
            return;
        InventorySlot source = GetSlot(slotIndex, sourceStorage);
        int expectedAmount = draggedAmount;
        bool validSource = source != null && ReferenceEquals(source, draggedSlot) &&
            source.item == item && source.amount == expectedAmount;
        CancelDrag();
        if (!validSource)
            return;

        dropHits.Clear();
        if (EventSystem.current != null)
            EventSystem.current.RaycastAll(eventData, dropHits);
        InventorySlotUI targetView = dropHits.Count > 0 ? dropHits[0].gameObject.GetComponentInParent<InventorySlotUI>() : null;
        if (targetView != null && targetView.Owner == this)
        {
            InventorySlot target = GetSlot(targetView.SlotIndex, targetView.Storage);
            if (target != null && !ReferenceEquals(source, target))
                RequestTransfer(source, target, item, expectedAmount, sourceStorage, targetView.Storage);
            return;
        }
        // A storage session never turns a missed drop into a world drop.
        if (IsStorageOpen || sourceStorage != null)
            return;
        if (panelRect == null || RectTransformUtility.RectangleContainsScreenPoint(
            panelRect, eventData.position, eventData.pressEventCamera))
            return;
        if (inventory == null || slotIndex < 0 || slotIndex >= inventory.SlotCount)
            return;
        InventorySlot slot = inventory.Slots[slotIndex];
        if (slot.IsEmpty || slot.item != item)
            return;
        if (slot.amount == 1)
        {
            inventory.TryDropSlot(slotIndex, item, 1);
            return;
        }

        if (dropDialog == null)
        {
            Debug.LogError("InventoryUI: Drop Dialog reference missing.", this);
            return;
        }
        interactionOwner = this;
        dropDialog.Show(item, slot.amount, amount =>
        {
            bool success = inventory != null && inventory.TryDropSlot(slotIndex, item, amount);
            if (success && interactionOwner == this)
                interactionOwner = null;
            return success;
        }, () =>
        {
            if (interactionOwner == this)
                interactionOwner = null;
        });
    }

    private void RequestTransfer(InventorySlot source, InventorySlot target, ItemData item, int count,
        StorageContainer sourceStorage, StorageContainer targetStorage)
    {
        ItemData expectedTargetItem = target.item;
        int expectedTargetAmount = target.amount;
        bool fromStorage = sourceStorage != null;
        bool toStorage = targetStorage != null;
        System.Func<int, bool> transfer = amount =>
        {
            if (!IsCurrentSlot(source, sourceStorage, fromStorage) ||
                !IsCurrentSlot(target, targetStorage, toStorage) ||
                !SlotTransfer.TryTransfer(source, target, item, count, expectedTargetItem, expectedTargetAmount, amount))
                return false;
            inventory.NotifyChanged();
            if (sourceStorage != null)
                sourceStorage.NotifyChanged();
            if (targetStorage != null && targetStorage != sourceStorage)
                targetStorage.NotifyChanged();
            if (interactionOwner == this)
                interactionOwner = null;
            return true;
        };
        if (!target.IsEmpty || count == 1)
        {
            transfer(count);
            return;
        }
        if (dropDialog == null)
            return;
        interactionOwner = this;
        dropDialog.ShowTransfer(item, count, transfer, () =>
        {
            if (interactionOwner == this)
                interactionOwner = null;
        });
    }

    private bool IsCurrentSlot(InventorySlot slot, StorageContainer storage, bool isStorage)
    {
        if (isStorage)
        {
            if (storage == null || storage != activeStorage || !storage.isActiveAndEnabled)
                return false;
            for (int i = 0; i < storage.SlotCount; i++)
                if (ReferenceEquals(storage.Slots[i], slot))
                    return true;
            return false;
        }
        if (inventory == null)
            return false;
        for (int i = 0; i < inventory.SlotCount; i++)
            if (ReferenceEquals(inventory.Slots[i], slot))
                return true;
        return false;
    }

    public void CancelDrag()
    {
        dragging = false;
        if (dragRect != null)
            Destroy(dragRect.gameObject);
        dragRect = null;
        draggedSlot = null;
        if (interactionOwner == this && (dropDialog == null || !dropDialog.IsOpen))
            interactionOwner = null;
    }

    private void OnDisable()
    {
        CloseStorage();
        CancelDrag();
        if (dropDialog != null && dropDialog.IsOpen)
            dropDialog.Cancel();
        if (interactionOwner == this)
            interactionOwner = null;
    }

    private void Start()
    {
        if (storageUI != null)
            storageUI.Hide();
        if (dropDialog != null)
            dropDialog.gameObject.SetActive(false);

        if (inventory != null)
            inventory.OnInventoryChanged += RefreshUI;

        ApplyBagLayout();
        RefreshUI();
    }

    private void OnDestroy()
    {
        OnDisable();
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshUI;
    }

    private void ApplyBagLayout()
    {
        if (inventory == null)
        {
            Debug.LogError("InventoryUI: Inventory atanmamýþ.", this);
            return;
        }

        BagData bag = inventory.EquippedBag;

        if (bag == null)
        {
            Debug.LogError("InventoryUI: Takýlý çanta bulunamadý.", this);
            return;
        }

        displayedBag = bag;

        if (bagBackground != null)
            bagBackground.sprite = bag.PanelSprite;

        if (panelRect != null)
            panelRect.sizeDelta = bag.PanelSize;

        if (titleText != null)
            titleText.text = bag.BagName;

        if (capacityText != null)
            capacityText.text = bag.SlotCount + " Slot";

        RebuildSlots();
    }

    private void RebuildSlots()
    {
        if (inventory == null ||
            slotGrid == null ||
            slotPrefab == null)
        {
            Debug.LogError("InventoryUI: Slot referanslarý eksik.", this);
            return;
        }

        foreach (InventorySlotUI slotUI in slotUIs)
        {
            if (slotUI != null)
                Destroy(slotUI.gameObject);
        }

        slotUIs.Clear();

        BagData bag = inventory.EquippedBag;

        if (bag == null)
            return;

        Vector2[] positions = bag.SlotPositions;

        if (positions == null || positions.Length != bag.SlotCount)
        {
            Debug.LogError(
                bag.BagName +
                ": Slot Positions sayýsý, Slot Count ile ayný olmalý.",
                this
            );
            return;
        }

        for (int i = 0; i < bag.SlotCount; i++)
        {
            InventorySlotUI slotUI =
                Instantiate(slotPrefab, slotGrid);

            RectTransform slotRect =
                slotUI.GetComponent<RectTransform>();

            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.sizeDelta = bag.SlotSize;
            slotRect.anchoredPosition = positions[i];

            slotUI.Bind(this, i);
            slotUIs.Add(slotUI);
        }
    }

    private void RefreshUI()
    {
        if (inventory == null)
            return;

        if (inventory.EquippedBag != displayedBag)
            ApplyBagLayout();

        if (slotUIs.Count != inventory.SlotCount)
            ApplyBagLayout();

        int count = Mathf.Min(
            slotUIs.Count,
            inventory.Slots.Count
        );

        for (int i = 0; i < count; i++)
            slotUIs[i].Refresh(inventory.Slots[i]);
    }
}
