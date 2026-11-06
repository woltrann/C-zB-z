using UnityEngine;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 2.5f;
    [SerializeField] private float interactionAngle = 90f;
    [SerializeField] private LayerMask interactionLayer;

    [Header("Item Collection")]
    [SerializeField] private float collectRadius = 2f;
    [SerializeField] private float collectAngle = 120f;
    [SerializeField] private LayerMask worldItemLayer;

    [SerializeField] private Transform playerTransform;

    [Header("References")]
    [SerializeField] private PlayerToolController toolController;

    public float InteractionDistance => interactionDistance;
    public float InteractionAngle => interactionAngle;

    private NatureObject currentObject;
    private CraftStation highlightedStation;
    private PlayerStorageInteraction storageInteraction;
    private WorldItem currentSelectedItem;

    private Collider[] nearbyObjects = new Collider[20];
    private Collider[] nearbyItems = new Collider[32];

    private void Awake()
    {
        storageInteraction = GetComponent<PlayerStorageInteraction>();
        if (playerTransform == null)
            playerTransform = transform;
    }

    private void Update()
    {
        if (InventoryUI.IsInputBlocked)
        {
            SetCraftTarget(null);
            return;
        }

        DetectObject();
        UpdateSelectedItem();

        if (Input.GetKeyDown(KeyCode.E) && (storageInteraction == null || (!storageInteraction.HasTarget && !storageInteraction.ConsumedInteraction)))
        {
            Interact();
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryCollectSelectedItem();
        }
    }

    private void DetectObject()
    {
        currentObject = null;

        float radius = Mathf.Max(interactionDistance, CraftStation.MaximumInteractionDistance);
        int objectCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            radius,
            nearbyObjects,
            interactionLayer,
            QueryTriggerInteraction.Ignore
        );

        while (objectCount == nearbyObjects.Length)
        {
            System.Array.Resize(ref nearbyObjects, nearbyObjects.Length * 2);
            objectCount = Physics.OverlapSphereNonAlloc(transform.position, radius, nearbyObjects,
                interactionLayer, QueryTriggerInteraction.Ignore);
        }

        float bestScore = -Mathf.Infinity;

        for (int i = 0; i < objectCount; i++)
        {
            Collider collider = nearbyObjects[i];

            NatureObject natureObject =
                collider.GetComponentInParent<NatureObject>();

            if (natureObject == null)
                continue;
            CraftStation station = natureObject as CraftStation;
            if (station != null)
            {
                if (!station.isActiveAndEnabled || !station.IsInRange(transform)) continue;
            }
            else
            {
                MeshCollider meshCollider = collider as MeshCollider;
                Vector3 closest = meshCollider != null && !meshCollider.convex
                    ? collider.bounds.ClosestPoint(transform.position)
                    : collider.ClosestPoint(transform.position);
                if ((closest - transform.position).sqrMagnitude > interactionDistance * interactionDistance) continue;
            }

            Vector3 targetPoint = station != null
                ? station.GetInteractionPoint(transform.position)
                : natureObject.transform.position;
            Vector3 direction = targetPoint - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.01f)
            {
                if (station == null) continue;
                direction = transform.forward;
            }

            float distance = direction.magnitude;

            direction.Normalize();

            float angle =
                Vector3.Angle(
                    transform.forward,
                    direction
                );

            // Karakterin baktýðý açý dýþýnda
            // kalan objeleri yok say.
            if (angle > interactionAngle)
                continue;

            // Öncelik hesabý
            float angleScore =
                1f - (angle / interactionAngle);

            float distanceScore =
                1f - (distance / (station != null ? station.InteractionDistance : interactionDistance));

            // Yöne daha fazla önem veriyoruz.
            float score =
                angleScore * 0.7f +
                distanceScore * 0.3f;

            if (score > bestScore)
            {
                bestScore = score;
                currentObject = natureObject;
            }
        }
        SetCraftTarget(currentObject as CraftStation);
    }

    private void SetCraftTarget(CraftStation station)
    {
        if (highlightedStation == station) return;
        if (highlightedStation != null) highlightedStation.SetHighlighted(false);
        highlightedStation = station;
        if (highlightedStation != null) highlightedStation.SetHighlighted(true);
    }

    private void OnDisable() => SetCraftTarget(null);

    private void Interact()
    {
        if (currentObject == null)
        {
            Debug.Log("Yakýnda etkileþime girilecek obje yok.");
            return;
        }

        if (toolController == null)
        {
            Debug.LogError(
                "PlayerInteraction: Tool Controller atanmadý!"
            );

            return;
        }

        ToolType requiredTool =
            currentObject.RequiredTool;

        if (!(currentObject is CraftStation) && !toolController.HasTool(requiredTool))
        {
            Debug.Log(
                requiredTool + " gerekli!"
            );

            return;
        }

        currentObject.Interact(toolController);
    }


    private WorldItem FindBestWorldItem()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            playerTransform.position,
            collectRadius,
            nearbyItems,
            worldItemLayer
        );

        // Grow only when saturated so crowded drop areas consider every item.
        while (hitCount == nearbyItems.Length)
        {
            System.Array.Resize(ref nearbyItems, nearbyItems.Length * 2);
            hitCount = Physics.OverlapSphereNonAlloc(
                playerTransform.position,
                collectRadius,
                nearbyItems,
                worldItemLayer
            );
        }

        if (hitCount == 0)
            return null;


        WorldItem bestItem = null;

        float bestScore = float.MinValue;


        Vector3 forward = playerTransform.forward;
        forward.y = 0f;
        forward.Normalize();


        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = nearbyItems[i];
            WorldItem worldItem =
                hit.GetComponentInParent<WorldItem>();

            if (worldItem == null)
                continue;


            Vector3 direction =
                worldItem.transform.position -
                playerTransform.position;

            direction.y = 0f;


            float distance =
                direction.magnitude;


            if (distance <= 0.01f)
                continue;


            direction.Normalize();


            float directionScore =
                (Vector3.Dot(forward, direction) + 1f) * 0.5f;


            float distanceScore =
                1f -
                (distance / collectRadius);


            float score =
                directionScore * 0.6f +
                distanceScore * 0.4f;


            if (score > bestScore)
            {
                bestScore = score;
                bestItem = worldItem;
            }
        }


        return bestItem;
    }
    private void SetItemOutline(
    WorldItem worldItem,
    bool enabled)
    {
        if (worldItem == null)
            return;


        Outline outline =
            worldItem.GetComponentInChildren<Outline>();


        if (outline == null)
            return;


        outline.enabled = enabled;
    }
    private void UpdateSelectedItem()
    {
        WorldItem newSelectedItem =
            FindBestWorldItem();


        if (newSelectedItem == currentSelectedItem)
            return;


        // Eski seçimin outline'ýný kapat
        if (currentSelectedItem != null)
        {
            SetItemOutline(
                currentSelectedItem,
                false
            );
        }


        currentSelectedItem =
            newSelectedItem;


        // Yeni seçimin outline'ýný aç
        if (currentSelectedItem != null)
        {
            SetItemOutline(
                currentSelectedItem,
                true
            );
        }
    }

private void TryCollectSelectedItem()
    {
        if (currentSelectedItem == null)
            return;

        WorldItem selectedItem = currentSelectedItem;

        ItemData itemData = selectedItem.Item;

        if (itemData == null)
            return;

        // Aletler çantaya deðil, sað ele takýlýr.
        if (itemData.ItemType == ItemType.Tool)
        {
            if (toolController == null)
            {
                Debug.LogError(
                    "PlayerInteraction: Tool Controller atanmamýþ!",
                    gameObject
                );

                return;
            }

            bool equipped = toolController.EquipWorldItem(selectedItem);

            if (equipped)
            {
                currentSelectedItem = null;
            }

            return;
        }

        // Diðer eþyalar mevcut sistemle çantaya alýnýr.
        Inventory inventory = GetComponent<Inventory>();

        if (inventory == null)
        {
            Debug.LogError(
                "Player üzerinde Inventory bulunamadý!",
                gameObject
            );

            return;
        }

        bool collected = selectedItem.TryCollect(inventory);

        if (collected)
        {
            currentSelectedItem = null;
        }
    }
   private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            interactionDistance
        );

        Gizmos.color = Color.blue;

        Vector3 left =
            Quaternion.Euler(
                0f,
                -interactionAngle,
                0f
            ) * transform.forward;

        Vector3 right =
            Quaternion.Euler(
                0f,
                interactionAngle,
                0f
            ) * transform.forward;

        Gizmos.DrawRay(
            transform.position,
            left * interactionDistance
        );

        Gizmos.DrawRay(
            transform.position,
            right * interactionDistance
        );
    }
}