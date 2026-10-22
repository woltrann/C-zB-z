
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private void Start()
    {
        if (inventory != null)
            inventory.OnInventoryChanged += RefreshUI;

        ApplyBagLayout();
        RefreshUI();
    }

    private void OnDestroy()
    {
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
