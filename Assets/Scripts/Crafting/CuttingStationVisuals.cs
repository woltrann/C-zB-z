using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CuttingStationVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CraftStation station;
    [SerializeField] private Transform trunkPoint;
    [SerializeField] private Collider cuttingSurface;
    [Header("Input Visuals")]
    [SerializeField] private ItemData treeTrunkItem;
    [SerializeField] private GameObject treeTrunkVisualPrefab;
    [SerializeField] private ItemData lumberItem;
    [SerializeField] private GameObject lumberVisualPrefab;
    [Header("Visual Transform Settings")]
    [SerializeField] private Vector3 inputLocalPositionOffset;
    [SerializeField] private Vector3 inputLocalRotation;
    [SerializeField] private Vector3 inputLocalScale = Vector3.one;
    [Header("Pole Output Alignment")]
    [SerializeField] private ItemData poleOutputItem;
    [SerializeField] private Transform poleOutputReference;
    [SerializeField, Min(0)] private float poleGap = 0.08f;
    [Header("Animation")]
    [SerializeField, Min(0)] private float transitionDuration = 0.12f;
    [SerializeField] private bool useSimpleTransition = true;

    private GameObject trunkVisual;
    private GameObject lumberVisual;
    private GameObject current;
    private int completed;
    private Coroutine transition;

    private void OnEnable()
    {
        if (station == null) station = GetComponent<CraftStation>();
        if (station == null) return;
        completed = station.CompletedBatches;
        station.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (station != null) station.Changed -= Refresh;
        if (transition != null) StopCoroutine(transition);
        transition = null;
        Hide();
    }

    private void OnDestroy()
    {
        if (trunkVisual != null) Destroy(trunkVisual);
        if (lumberVisual != null) Destroy(lumberVisual);
    }

    private void Hide()
    {
        if (trunkVisual != null) trunkVisual.SetActive(false);
        if (lumberVisual != null) lumberVisual.SetActive(false);
        current = null;
    }

    private void Refresh()
    {
        bool success = station.CompletedBatches > completed;
        completed = station.CompletedBatches;
        // Completion count changes only after the existing spawner succeeds.
        if (success && current != null && useSimpleTransition && transitionDuration > 0)
        {
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(CompleteTransition(current));
            return;
        }
        if (transition == null) ShowReserved();
    }

    private void ShowReserved()
    {
        Hide();
        if (trunkPoint == null || station.PreparedRecipe == null) return;
        foreach (var ingredient in station.PreparedRecipe.RequiredMaterials)
        {
            if (station.GetReserved(ingredient.Item) <= 0) continue;
            if (ingredient.Item == treeTrunkItem)
                current = GetVisual(ref trunkVisual, treeTrunkVisualPrefab);
            else if (ingredient.Item == lumberItem)
                current = GetVisual(ref lumberVisual, lumberVisualPrefab);
            if (current == null) continue;
            current.transform.localPosition = inputLocalPositionOffset;
            current.transform.localRotation = Quaternion.Euler(inputLocalRotation);
            current.transform.localScale = inputLocalScale;
            current.SetActive(true);
            break;
        }
    }

    private GameObject GetVisual(ref GameObject cached, GameObject prefab)
    {
        if (cached != null || prefab == null) return cached;
        // Build from render data only. Even a mistaken gameplay prefab assignment
        // cannot execute Awake, create physics, or introduce collectible items.
        cached = new GameObject("Input Visual");
        cached.transform.SetParent(trunkPoint, false);
        foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            if (filter.sharedMesh == null || renderer == null) continue;
            var part = new GameObject(filter.name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(cached.transform, false);
            part.transform.localPosition = prefab.transform.InverseTransformPoint(filter.transform.position);
            part.transform.localRotation = Quaternion.Inverse(prefab.transform.rotation) * filter.transform.rotation;
            part.transform.localScale = new Vector3(filter.transform.lossyScale.x / prefab.transform.lossyScale.x,
                filter.transform.lossyScale.y / prefab.transform.lossyScale.y, filter.transform.lossyScale.z / prefab.transform.lossyScale.z);
            part.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            part.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
        }
        return cached;
    }

    private IEnumerator CompleteTransition(GameObject visual)
    {
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            visual.transform.localScale = inputLocalScale * (1 - elapsed / transitionDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transition = null;
        ShowReserved();
    }

    public bool IsAlignedPole(ItemData item) => poleOutputReference != null && item == poleOutputItem;

    public void ConfigureOutput(WorldItem product)
    {
        if (!IsAlignedPole(product.Item)) return;
        var source = poleOutputReference.GetComponent<MeshFilter>();
        var target = product.GetComponentInChildren<MeshFilter>(true);
        var collider = product.GetComponent<BoxCollider>();
        if (source == null || target == null || collider == null) return;
        Vector3 size = Vector3.Scale(source.sharedMesh.bounds.size, poleOutputReference.lossyScale);
        size.y /= 3f;
        Vector3 raw = target.sharedMesh.bounds.size;
        Vector3 scale = new Vector3(size.x / raw.x, size.y / raw.y, size.z / raw.z);
        product.transform.localScale = Vector3.one;
        product.transform.rotation = poleOutputReference.rotation;
        target.transform.localRotation = Quaternion.identity;
        target.transform.localScale = scale;
        target.transform.localPosition = -Vector3.Scale(target.sharedMesh.bounds.center, scale);
        collider.center = Vector3.zero;
        collider.size = size;
    }

    public float OutputSpacing(ItemData item, Bounds bounds, float spacing)
    {
        if (!IsAlignedPole(item)) return Mathf.Max(spacing, bounds.size.z + 0.12f);
        var mesh = poleOutputReference.GetComponent<MeshFilter>().sharedMesh;
        return mesh.bounds.size.y * poleOutputReference.lossyScale.y / 3f + poleGap;
    }

    public Vector3 OutputCandidate(Vector3 center, int index, int amount, int ring, float spacing, ItemData item = null)
    {
        if (IsAlignedPole(item))
        {
            var mesh = poleOutputReference.GetComponent<MeshFilter>().sharedMesh;
            center = poleOutputReference.TransformPoint(mesh.bounds.center);
            Vector3 side = poleOutputReference.up; side.y = 0; side.Normalize();
            Vector3 along = poleOutputReference.right; along.y = 0; along.Normalize();
            return center + side * ((index - (amount - 1) * 0.5f) * spacing)
                + along * (ring * (mesh.bounds.size.x * poleOutputReference.lossyScale.x + 0.12f));
        }
        return center + transform.forward * ((index - (amount - 1) * 0.5f) * spacing)
            + transform.right * (ring * spacing);
    }

    public bool AreSeparated(Bounds first, Bounds second, ItemData item)
    {
        if (!IsAlignedPole(item)) return false;
        Vector3 delta = first.center - second.center;
        var mesh = poleOutputReference.GetComponent<MeshFilter>().sharedMesh;
        float width = mesh.bounds.size.y * poleOutputReference.lossyScale.y / 3f;
        float length = mesh.bounds.size.x * poleOutputReference.lossyScale.x;
        return Mathf.Abs(Vector3.Dot(delta, poleOutputReference.up)) >= width + 0.04f ||
               Mathf.Abs(Vector3.Dot(delta, poleOutputReference.right)) >= length + 0.04f;
    }

    public bool FitsOutput(Bounds bounds, ItemData item = null)
    {
        if (cuttingSurface == null) return false;
        Bounds surface = IsAlignedPole(item)
            ? poleOutputReference.GetComponent<Renderer>().bounds : cuttingSurface.bounds;
        if (IsAlignedPole(item)) surface.Expand(poleGap * 4);
        return bounds.min.x >= surface.min.x && bounds.max.x <= surface.max.x &&
               bounds.min.z >= surface.min.z && bounds.max.z <= surface.max.z;
    }

    public bool IsOutputSurface(Collider collider) => collider == cuttingSurface;
}
