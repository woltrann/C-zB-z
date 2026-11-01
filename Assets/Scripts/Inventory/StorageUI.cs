using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StorageUI : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private RectTransform slotGrid;
    [SerializeField] private InventorySlotUI slotPrefab;
    [SerializeField] private Button closeButton;
    private readonly List<InventorySlotUI> slotViews = new List<InventorySlotUI>();
    private StorageContainer container;
    private InventoryUI owner;
    private bool initialized;

    public void Show(StorageContainer storage, InventoryUI inventoryUI)
    {
        Hide();
        container = storage;
        owner = inventoryUI;
        if (!initialized)
        {
            closeButton.onClick.AddListener(() => { if (owner != null) owner.CloseStorage(); });
            initialized = true;
        }
        title.text = storage.name;
        title.richText = false;
        while (slotViews.Count < storage.SlotCount)
        {
            InventorySlotUI view = Instantiate(slotPrefab, slotGrid);
            slotViews.Add(view);
        }
        for (int i = 0; i < slotViews.Count; i++)
        {
            bool visible = i < storage.SlotCount;
            slotViews[i].gameObject.SetActive(visible);
            if (!visible)
                continue;
            RectTransform rect = (RectTransform)slotViews[i].transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(72f, 72f);
            rect.anchoredPosition = new Vector2((i % 3 - 1) * 86f, 42f - (i / 3) * 86f);
            slotViews[i].Bind(owner, i, storage);
        }
        container.OnChanged += Refresh;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Refresh();
    }

    private void Refresh()
    {
        if (container == null)
            return;
        for (int i = 0; i < container.SlotCount; i++)
            slotViews[i].Refresh(container.Slots[i]);
    }

    public void Hide()
    {
        if (container != null)
            container.OnChanged -= Refresh;
        container = null;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (container != null)
            container.OnChanged -= Refresh;
    }
}
