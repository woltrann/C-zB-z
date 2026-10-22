using UnityEngine;

public class WorldItem : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private ItemData item;

    [Header("Amount")]
    [SerializeField] private int amount = 1;

    public ItemData Item => item;
    public int Amount => amount;


    public void Initialize(ItemData itemData, int itemAmount)
    {
        item = itemData;
        amount = itemAmount;
    }


    public bool TryCollect(Inventory inventory)
    {
        if (inventory == null)
            return false;

        if (item == null)
            return false;

        if (amount <= 0)
            return false;


        bool added = inventory.AddItem(item, amount);

        if (!added)
        {
            Debug.Log("Envanterde yeterli boþ alan yok.");
            return false;
        }


        Destroy(gameObject);

        return true;
    }
}