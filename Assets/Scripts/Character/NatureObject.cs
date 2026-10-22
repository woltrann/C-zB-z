using UnityEngine;
using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;


public enum ToolType
{
    None,
    Axe,
    Pickaxe,
    Shovel,
    Hoe,
    FishingRod
}


public abstract class NatureObject : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private ToolType requiredTool = ToolType.None;

    public ToolType RequiredTool => requiredTool;

    public abstract void Interact(PlayerToolController player);
}