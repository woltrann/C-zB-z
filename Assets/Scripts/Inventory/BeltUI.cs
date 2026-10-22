using UnityEngine;
using UnityEngine.UI;

public class BeltUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerToolController toolController;

    [Header("Belt Appearance")]
    [SerializeField] private Image beltBackground;

    [Header("Belt Slots")]
    [SerializeField] private GameObject beltSlot1;
    [SerializeField] private GameObject beltSlot2;
    [SerializeField] private GameObject beltSlot3;

    [Header("Belt Item Icons")]
    [SerializeField] private Image beltSlot1Icon;
    [SerializeField] private Image beltSlot2Icon;
    [SerializeField] private Image beltSlot3Icon;

    private void OnEnable()
    {
        if (toolController != null)
            toolController.OnRightHandChanged += RefreshBeltUI;

        RefreshBeltUI();
    }

    private void OnDisable()
    {
        if (toolController != null)
            toolController.OnRightHandChanged -= RefreshBeltUI;
    }

    public void RefreshBeltUI()
    {
        if (toolController == null)
            return;

        BeltData belt = toolController.EquippedBelt;

        // Takýlý kemerin görselini güncelle.
        if (beltBackground != null)
        {
            bool hasBeltSprite =
                belt != null && belt.BeltSprite != null;

            beltBackground.enabled = hasBeltSprite;

            beltBackground.sprite = hasBeltSprite
                ? belt.BeltSprite
                : null;
        }

        // Kemer kapasitesini öðren.
        int slotCount = toolController.BeltSlotCount;

        // Slot arka planlarýný göster veya gizle.
        if (beltSlot1 != null)
            beltSlot1.SetActive(slotCount >= 1);

        if (beltSlot2 != null)
            beltSlot2.SetActive(slotCount >= 2);

        if (beltSlot3 != null)
            beltSlot3.SetActive(slotCount >= 3);

        // Her kemer yuvasýnýn ikonunu güncelle.
        UpdateSlotIcon(
            beltSlot1Icon,
            toolController.GetBeltItem(0)
        );

        UpdateSlotIcon(
            beltSlot2Icon,
            toolController.GetBeltItem(1)
        );

        UpdateSlotIcon(
            beltSlot3Icon,
            toolController.GetBeltItem(2)
        );
    }

    private void UpdateSlotIcon(Image icon, ItemData item)
    {
        if (icon == null)
            return;

        bool hasItem = item != null && item.Icon != null;

        icon.enabled = hasItem;

        icon.sprite = hasItem ? item.Icon : null;
    }
}