using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text amountText;


    public void Refresh(InventorySlot slot)
    {
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
        itemIcon.sprite = null;
        itemIcon.enabled = false;

        amountText.text = "";
        amountText.enabled = false;
    }
}