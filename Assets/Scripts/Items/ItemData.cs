using UnityEngine;

[CreateAssetMenu(
    fileName = "New Item",
    menuName = "Game/Items/Item"
)]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string itemID;
    [SerializeField] private string itemName;

    [Header("Visual")]
    [SerializeField] private Sprite icon;

    [Header("Inventory")]
    [SerializeField] private ItemType itemType;
    [SerializeField] private int maxStack = 99;

    [Header("Equipment")]
    [SerializeField] private ToolType toolType = ToolType.None;
    [SerializeField] private GameObject handPrefab;

    [Header("World Drop")]
    [SerializeField] private GameObject worldPrefab;

    public GameObject WorldPrefab => worldPrefab;
    public ToolType ToolType => toolType;
    public GameObject HandPrefab => handPrefab;

    public string ItemID => itemID;
    public string ItemName => itemName;
    public Sprite Icon => icon;
    public ItemType ItemType => itemType;
    public int MaxStack => maxStack;
}