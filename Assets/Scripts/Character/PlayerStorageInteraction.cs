using UnityEngine;

[DefaultExecutionOrder(-50)]
public class PlayerStorageInteraction : MonoBehaviour
{
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private Camera viewCamera;
    [SerializeField, Min(0.1f)] private float interactionDistance = 7f;
    [SerializeField, Range(0f, 180f)] private float interactionAngle = 90f;
    private PlayerInteraction characterInteraction;
    private Collider[] nearbyObjects = new Collider[32];
    private StorageContainer target;
    private int consumedFrame = -1;
    public bool HasTarget => target != null;
    public bool ConsumedInteraction => consumedFrame == Time.frameCount;

    private void Awake()
    {
        characterInteraction = GetComponent<PlayerInteraction>();
    }

    private void Update()
    {
        if (inventoryUI == null)
            return;
        if (inventoryUI.IsStorageOpen)
        {
            SetTarget(null);
            if (!inventoryUI.HasQuantityDialog && !inventoryUI.IsDragging &&
                (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)))
            {
                consumedFrame = Time.frameCount;
                inventoryUI.CloseStorage();
            }
            return;
        }
        if (InventoryUI.IsInputBlocked)
        {
            SetTarget(null);
            return;
        }
        SetTarget(DetectStorage());
        if (target != null && Input.GetKeyDown(KeyCode.E))
        {
            consumedFrame = Time.frameCount;
            inventoryUI.OpenStorage(target);
        }
    }

    private StorageContainer DetectStorage()
    {
        // Share the tree interaction settings, including its direction/distance weighting.
        float radius = Mathf.Max(0.01f, characterInteraction != null
            ? characterInteraction.InteractionDistance : interactionDistance);
        float angleLimit = Mathf.Max(0.01f, characterInteraction != null
            ? characterInteraction.InteractionAngle : interactionAngle);
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, nearbyObjects,
            Physics.AllLayers, QueryTriggerInteraction.Ignore);
        while (count == nearbyObjects.Length)
        {
            System.Array.Resize(ref nearbyObjects, nearbyObjects.Length * 2);
            count = Physics.OverlapSphereNonAlloc(transform.position, radius, nearbyObjects,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
        }

        StorageContainer best = null;
        float bestScore = -Mathf.Infinity;
        for (int i = 0; i < count; i++)
        {
            StorageContainer storage = nearbyObjects[i].GetComponentInParent<StorageContainer>();
            if (storage == null || !storage.isActiveAndEnabled)
                continue;
            Vector3 direction = storage.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                continue;
            float distance = direction.magnitude;
            float angle = Vector3.Angle(transform.forward, direction.normalized);
            if (angle > angleLimit)
                continue;
            float score = (1f - angle / angleLimit) * 0.7f + (1f - distance / radius) * 0.3f;
            if (score > bestScore)
            {
                bestScore = score;
                best = storage;
            }
        }
        return best;
    }

    private void SetTarget(StorageContainer value)
    {
        if (target == value)
            return;
        if (target != null)
            target.SetHighlighted(false);
        target = value;
        if (target != null)
            target.SetHighlighted(true);
    }

    private void OnDisable()
    {
        SetTarget(null);
        if (inventoryUI != null)
            inventoryUI.CloseStorage();
    }
}
