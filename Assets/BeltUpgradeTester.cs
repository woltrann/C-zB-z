
using UnityEngine;

public class BeltUpgradeTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerToolController toolController;

    [Header("Belt Upgrades")]
    [SerializeField] private BeltData mediumBelt;
    [SerializeField] private BeltData largeBelt;

    private void Update()
    {
        if (InventoryUI.IsInputBlocked)
            return;

        if (toolController == null)
            return;

        // 8: Orta kemere yükselt.
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            toolController.UpgradeBelt(mediumBelt);
        }

        // 9: Büyük kemere yükselt.
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            toolController.UpgradeBelt(largeBelt);
        }
    }
}
