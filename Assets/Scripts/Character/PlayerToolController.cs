
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

    private WorldItem currentWorldItem;

    private readonly WorldItem[] beltItems = new WorldItem[3];
    private Transform[] beltSockets;

    // TAB ile sýradaki kemer yuvasýný takip eder.
    private int nextBeltSlotIndex;

    public ToolType CurrentTool => currentTool;

    public BeltData EquippedBelt => equippedBelt;

    public int BeltSlotCount =>
        equippedBelt != null
            ? Mathf.Clamp(equippedBelt.SlotCount, 0, beltItems.Length)
            : 0;

    public ItemData CurrentItem =>
        currentWorldItem != null
            ? currentWorldItem.Item
            : null;

    // Eski RightHandUI ile uyumluluk.
    public ItemData BeltItem => GetBeltItem(0);

    // Yeni çoklu slot UI sistemi için.
    public ItemData GetBeltItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= BeltSlotCount)
            return null;

        WorldItem worldItem = beltItems[slotIndex];

        return worldItem != null
            ? worldItem.Item
            : null;
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

    public bool HasTool(ToolType requiredTool)
    {
        return currentTool == requiredTool;
    }

    public bool EquipWorldItem(WorldItem worldItem)
    {
        if (worldItem == null || rightHandSocket == null)
            return false;

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

        // El doluysa eski aleti kemere taþýmayý dene.
        if (currentWorldItem != null)
        {
            int emptySlot = FindEmptyBeltSlot();

            if (emptySlot >= 0)
            {
                MoveItemToBelt(currentWorldItem, emptySlot);

                currentWorldItem = null;
                currentTool = ToolType.None;
            }
            else
            {
                // Kemer doluysa eldeki aleti býrak.
                DropCurrentTool();
            }
        }

        MoveItemToHand(worldItem);

        OnRightHandChanged?.Invoke();

        Debug.Log("Sað ele takýldý: " + itemData.ItemName);

        return true;
    }

    private int FindEmptyBeltSlot()
    {
        for (int i = 0; i < BeltSlotCount; i++)
        {
            if (beltItems[i] == null && beltSockets[i] != null)
                return i;
        }

        return -1;
    }

    private void MoveItemToHand(WorldItem worldItem)
    {
        if (worldItem == null)
            return;

        currentWorldItem = worldItem;
        currentTool = worldItem.Item.ToolType;

        AttachItem(worldItem, rightHandSocket);
    }

    private void MoveItemToBelt(WorldItem worldItem, int slotIndex)
    {
        if (worldItem == null)
            return;

        if (slotIndex < 0 || slotIndex >= BeltSlotCount)
            return;

        if (beltSockets[slotIndex] == null)
            return;

        beltItems[slotIndex] = worldItem;

        AttachItem(worldItem, beltSockets[slotIndex]);
    }

    private void AttachItem(WorldItem worldItem, Transform socket)
    {
        if (worldItem == null || socket == null)
            return;

        worldItem.transform.SetParent(socket);
        worldItem.transform.localPosition = Vector3.zero;
        worldItem.transform.localRotation = Quaternion.identity;

        foreach (Collider col in
                 worldItem.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        foreach (Outline outline in
                 worldItem.GetComponentsInChildren<Outline>())
        {
            outline.enabled = false;
        }
    }

    private void SwapHandAndBelt()
    {
        int slotCount = BeltSlotCount;

        if (slotCount == 0 || rightHandSocket == null)
            return;

        // Dolu bir kemer yuvasý bul.
        int selectedSlot = -1;

        for (int i = 0; i < slotCount; i++)
        {
            int index = (nextBeltSlotIndex + i) % slotCount;

            if (beltItems[index] != null && beltSockets[index] != null)
            {
                selectedSlot = index;
                break;
            }
        }

        if (selectedSlot < 0)
            return;

        WorldItem oldHandItem = currentWorldItem;
        WorldItem selectedBeltItem = beltItems[selectedSlot];

        // Kemerdeki aleti ele taþý.
        beltItems[selectedSlot] = null;
        MoveItemToHand(selectedBeltItem);

        // Eski el aletini boþalan kemer yuvasýna taþý.
        if (oldHandItem != null)
        {
            MoveItemToBelt(oldHandItem, selectedSlot);
        }

        // Bir sonraki TAB baþka yuvadan devam etsin.
        nextBeltSlotIndex = (selectedSlot + 1) % slotCount;

        OnRightHandChanged?.Invoke();

        Debug.Log("El ve kemer aletleri deðiþtirildi.");
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

        itemTransform.SetParent(null);

        Vector3 origin =
            transform.position +
            transform.forward * 1.2f +
            Vector3.up * 2f;

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

        foreach (Collider col in
                 droppedItem.GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }

        foreach (Outline outline in
                 droppedItem.GetComponentsInChildren<Outline>())
        {
            outline.enabled = false;
        }

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




    public bool UpgradeBelt(BeltData newBelt)
    {
        if (newBelt == null)
        {
            Debug.LogWarning("Yeni kemer verisi bulunamadý.");
            return false;
        }

        int currentCapacity = BeltSlotCount;
        int newCapacity = newBelt.SlotCount;

        // Sadece kapasite artýran yükseltmelere izin ver.
        if (newCapacity <= currentCapacity)
        {
            Debug.LogWarning(
                "Yeni kemerin kapasitesi mevcut kemerden büyük olmalý."
            );

            return false;
        }

        // Yeni açýlacak fiziksel yuvalarý kontrol et.
        if (beltSockets == null || newCapacity > beltSockets.Length)
        {
            Debug.LogWarning("Kemer yuvalarý eksik.");
            return false;
        }

        for (int i = 0; i < newCapacity; i++)
        {
            if (beltSockets[i] == null)
            {
                Debug.LogWarning(
                    "BeltSocket" + (i + 1) + " atanmamýþ."
                );

                return false;
            }
        }

        // Yeni kemeri kuþan.
        equippedBelt = newBelt;

        // Mevcut aletlere dokunmuyoruz.
        // UI güncellemesini tetikle.
        OnRightHandChanged?.Invoke();

        Debug.Log(
            "Kemer yükseltildi: " +
            newBelt.BeltName +
            " | Kapasite: " + BeltSlotCount
        );

        return true;
    }

}
