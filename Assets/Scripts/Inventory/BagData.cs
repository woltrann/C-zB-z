
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Bag",
    menuName = "Game/Inventory/Bag"
)]
public class BagData : ScriptableObject
{
    [Header("Bag Identity")]
    [SerializeField] private string bagID;
    [SerializeField] private string bagName;

    [Header("Bag Visual")]
    [SerializeField] private Sprite icon;
    [SerializeField] private Sprite panelSprite;

    [Header("Panel Dimensions")]
    [SerializeField]
    private Vector2 panelSize =
        new Vector2(320f, 280f);

    [Header("Slot Settings")]
    [Min(1)]
    [SerializeField] private int slotCount = 3;

    [SerializeField]
    private Vector2 slotSize =
        new Vector2(54f, 54f);

    [Tooltip("Her slotun panel merkezine göre konumu.")]
    [SerializeField] private Vector2[] slotPositions;

    public string BagID => bagID;
    public string BagName => bagName;
    public Sprite Icon => icon;
    public Sprite PanelSprite => panelSprite;
    public Vector2 PanelSize => panelSize;
    public int SlotCount => slotCount;
    public Vector2 SlotSize => slotSize;
    public Vector2[] SlotPositions => slotPositions;
}
