
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

    private void Start()
    {
        RefreshBeltUI();
    }

    public void RefreshBeltUI()
    {
        if (toolController == null)
            return;

        BeltData belt = toolController.EquippedBelt;

        if (beltBackground != null)
        {
            beltBackground.sprite = belt != null
                ? belt.BeltSprite
                : null;

            beltBackground.enabled =
                belt != null && belt.BeltSprite != null;
        }

        int slotCount = toolController.BeltSlotCount;

        if (beltSlot1 != null)
            beltSlot1.SetActive(slotCount >= 1);

        if (beltSlot2 != null)
            beltSlot2.SetActive(slotCount >= 2);

        if (beltSlot3 != null)
            beltSlot3.SetActive(slotCount >= 3);
    }
}
