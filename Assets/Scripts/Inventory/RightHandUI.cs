
using UnityEngine;
using UnityEngine.UI;

public class RightHandUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerToolController toolController;

    [Header("Right Hand")]
    [SerializeField] private Image equippedItemIcon;

    [Header("Belt")]
    [SerializeField] private Image beltSlot1Icon;

    private void OnEnable()
    {
        if (toolController != null)
            toolController.OnRightHandChanged += RefreshUI;

        RefreshUI();
    }

    private void OnDisable()
    {
        if (toolController != null)
            toolController.OnRightHandChanged -= RefreshUI;
    }

    private void RefreshUI()
    {
        if (toolController == null)
            return;

        // Sað el ikonunu güncelle.
        UpdateIcon(equippedItemIcon, toolController.CurrentItem);

        // Kemer ikonunu güncelle.
        UpdateIcon(beltSlot1Icon, toolController.BeltItem);
    }

    private void UpdateIcon(Image image, ItemData item)
    {
        if (image == null)
            return;

        bool hasItem = item != null && item.Icon != null;

        image.enabled = hasItem;

        if (hasItem)
            image.sprite = item.Icon;
        else
            image.sprite = null;
    }
}
