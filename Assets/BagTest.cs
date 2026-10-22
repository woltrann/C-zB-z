
using UnityEngine;

public class BagTest : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private BagData testBag;

    [ContextMenu("Equip Test Bag")]
    private void EquipTestBag()
    {
        if (inventory == null || testBag == null)
        {
            Debug.LogWarning(
                "Inventory veya Test Bag atanmamýþ.",
                this
            );
            return;
        }

        bool success = inventory.EquipBag(testBag);

        Debug.Log(
            success
                ? "Çanta deðiþtirildi. Yeni kapasite: " +
                  inventory.SlotCount
                : "Çanta deðiþtirilemedi.",
            this
        );
    }
}
