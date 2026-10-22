using UnityEngine;

public class PlayerToolController : MonoBehaviour
{
    [Header("Current Tool")]
    [SerializeField] private ToolType currentTool = ToolType.None;

    [Header("Right Hand")]
    [SerializeField] private Transform rightHandSocket;

    [Header("Equipped Belt")]
    [SerializeField] private BeltData equippedBelt;

    [Header("Belt Sockets")]
    [SerializeField] private Transform beltSocket1;
    [SerializeField] private Transform beltSocket2;
    [SerializeField] private Transform beltSocket3;


    public System.Action OnRightHandChanged;

    public ItemData CurrentItem =>
        currentWorldItem != null
            ? currentWorldItem.Item
            : null;

    private GameObject currentHandObject;
    private WorldItem currentWorldItem;
    private WorldItem beltWorldItem;
    private WorldItem[] beltItems = new WorldItem[3];
    private Transform[] beltSockets;


    public ToolType CurrentTool => currentTool;

    public BeltData EquippedBelt => equippedBelt;

    public int BeltSlotCount =>
        equippedBelt != null
            ? equippedBelt.SlotCount
            : 0;

    public ItemData BeltItem =>
    beltWorldItem != null
        ? beltWorldItem.Item
        : null;


    public bool HasTool(ToolType requiredTool)
    {
        return currentTool == requiredTool;
    }


    public bool EquipWorldItem(WorldItem worldItem)
    {
        if (worldItem == null || rightHandSocket == null)
        {
            Debug.LogWarning(
                "Alet takýlamadý: WorldItem veya RightHandSocket eksik.",
                this
            );

            return false;
        }

        ItemData itemData = worldItem.Item;

        if (itemData == null || itemData.ItemType != ItemType.Tool)
            return false;

        if (itemData.ToolType == ToolType.None)
        {
            Debug.LogWarning(
                itemData.ItemName + " için ToolType atanmamýþ.",
                worldItem
            );

            return false;
        }

        // El doluysa mevcut alet için yer aç.
        if (currentWorldItem != null)
        {
            if (beltWorldItem == null && beltSocket1 != null)
            {
                // Kemer boþ: eldeki aleti kemere taþý.
                beltWorldItem = currentWorldItem;

                beltWorldItem.transform.SetParent(beltSocket1);
                beltWorldItem.transform.localPosition = Vector3.zero;
                beltWorldItem.transform.localRotation = Quaternion.identity;

                Collider[] beltColliders =
                    beltWorldItem.GetComponentsInChildren<Collider>();

                foreach (Collider col in beltColliders)
                    col.enabled = false;

                Outline[] beltOutlines =
                    beltWorldItem.GetComponentsInChildren<Outline>();

                foreach (Outline outline in beltOutlines)
                    outline.enabled = false;

                currentWorldItem = null;
                currentTool = ToolType.None;
            }
            else
            {
                // Kemer dolu: eldeki aleti yere býrak.
                DropCurrentTool();
            }
        }

        // Yeni aleti ele tak.
        currentWorldItem = worldItem;
        currentTool = itemData.ToolType;

        currentWorldItem.transform.SetParent(rightHandSocket);
        currentWorldItem.transform.localPosition = Vector3.zero;
        currentWorldItem.transform.localRotation = Quaternion.identity;

        Collider[] handColliders =
            currentWorldItem.GetComponentsInChildren<Collider>();

        foreach (Collider col in handColliders)
            col.enabled = false;

        Outline[] handOutlines =
            currentWorldItem.GetComponentsInChildren<Outline>();

        foreach (Outline outline in handOutlines)
            outline.enabled = false;

        Debug.Log("Sað ele takýldý: " + itemData.ItemName);

        OnRightHandChanged?.Invoke();

        return true;
    }

    private void Awake()
    {
        beltSockets = new Transform[]
        {
        beltSocket1,
        beltSocket2,
        beltSocket3
        };
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            DropCurrentTool();
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwapHandAndBelt();
        }
    }

    public void DropCurrentTool()
    {
        if (currentWorldItem == null)
        {
            currentTool = ToolType.None;
            return;
        }

        WorldItem droppedItem = currentWorldItem;
        Transform itemTransform = droppedItem.transform;

        // Karakterin elinden ayýr.
        itemTransform.SetParent(null);

        // Aleti karakterin önüne yerleþtir.
        Vector3 origin =
            transform.position +
            transform.forward * 1.2f +
            Vector3.up * 2f;

        // Zemini yukarýdan aþaðýya bul.
        RaycastHit hit;

        if (Physics.Raycast(
            origin,
            Vector3.down,
            out hit,
            5f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            itemTransform.position =
                hit.point + Vector3.up * 0.15f;
        }
        else
        {
            itemTransform.position =
                transform.position +
                transform.forward * 1.2f +
                Vector3.up * 0.5f;
        }

        // Yerde tekrar seçilebilir olsun.
        Collider[] colliders =
            droppedItem.GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
            col.enabled = true;

        Outline[] outlines =
            droppedItem.GetComponentsInChildren<Outline>();

        foreach (Outline outline in outlines)
            outline.enabled = false;

        currentWorldItem = null;
        currentTool = ToolType.None;
        OnRightHandChanged?.Invoke();
    }
    public void EquipTool(ToolType tool)
    {
        currentTool = tool;
        Debug.Log("Equipped Tool: " + currentTool);
    }

    public void RemoveTool()
    {
        DropCurrentTool();
    }


    private void SwapHandAndBelt()
    {
        // Kemer boþsa deðiþtirme yapma.
        if (beltWorldItem == null || beltSocket1 == null)
            return;

        WorldItem oldHandItem = currentWorldItem;

        // Kemer aletini ele al.
        currentWorldItem = beltWorldItem;
        currentTool = currentWorldItem.Item.ToolType;

        currentWorldItem.transform.SetParent(rightHandSocket);
        currentWorldItem.transform.localPosition = Vector3.zero;
        currentWorldItem.transform.localRotation = Quaternion.identity;

        // Önceki el aletini kemere gönder.
        beltWorldItem = oldHandItem;

        if (beltWorldItem != null)
        {
            beltWorldItem.transform.SetParent(beltSocket1);
            beltWorldItem.transform.localPosition = Vector3.zero;
            beltWorldItem.transform.localRotation = Quaternion.identity;
        }
        else
        {
            currentWorldItem = currentWorldItem;
        }

        // Elde ve kemerdeki aletlerin collider'larý kapalý kalmalý.
        if (currentWorldItem != null)
        {
            foreach (Collider col in currentWorldItem.GetComponentsInChildren<Collider>())
                col.enabled = false;

            foreach (Outline outline in currentWorldItem.GetComponentsInChildren<Outline>())
                outline.enabled = false;
        }

        if (beltWorldItem != null)
        {
            foreach (Collider col in beltWorldItem.GetComponentsInChildren<Collider>())
                col.enabled = false;

            foreach (Outline outline in beltWorldItem.GetComponentsInChildren<Outline>())
                outline.enabled = false;
        }

        OnRightHandChanged?.Invoke();

        Debug.Log("El ve kemer aletleri deðiþtirildi.");
    }

}
