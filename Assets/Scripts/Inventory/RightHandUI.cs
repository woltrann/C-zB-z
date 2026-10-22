using UnityEngine;
using UnityEngine.UI;

public class RightHandUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerToolController toolController;

    [Header("Right Hand")]
    [SerializeField] private Image equippedItemIcon;

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
        if (equippedItemIcon == null)
            return;

        ItemData item = toolController != null
            ? toolController.CurrentItem
            : null;

        bool hasItem = item != null && item.Icon != null;

        equippedItemIcon.enabled = hasItem;
        equippedItemIcon.sprite = hasItem ? item.Icon : null;
    }
}