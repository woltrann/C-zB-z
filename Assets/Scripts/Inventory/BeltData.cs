
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Belt",
    menuName = "Game/Equipment/Belt"
)]
public class BeltData : ScriptableObject
{
    [Header("Belt Information")]
    [SerializeField] private string beltName;

    [Header("Appearance")]
    [SerializeField] private Sprite beltSprite;

    [Header("Capacity")]
    [Range(1, 3)]
    [SerializeField] private int slotCount = 1;

    public string BeltName => beltName;
    public Sprite BeltSprite => beltSprite;
    public int SlotCount => slotCount;
}
