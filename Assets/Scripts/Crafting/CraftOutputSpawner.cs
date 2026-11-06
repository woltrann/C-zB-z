using System;
using System.Collections.Generic;
using UnityEngine;

internal static class CraftOutputSpawner
{
    internal static bool TrySpawn(WorldItem prefab, int amount, Transform outputPoint, float spacing, out string error)
    {
        error = "Ürün oluşturulamadı; malzemeler tezgahta korundu.";
        if (prefab == null || prefab.Item == null || amount <= 0 || outputPoint == null)
            return false;

        var batch = new GameObject("CraftOutputBatch");
        batch.SetActive(false);
        var products = new List<WorldItem>(amount);
        var occupied = new List<Bounds>(amount);
        var hits = new Collider[32];
        try
        {
            for (int i = 0; i < amount; i++)
            {
                WorldItem product = UnityEngine.Object.Instantiate(prefab, batch.transform);
                products.Add(product);
                product.transform.rotation = prefab.transform.rotation;
                Bounds bounds = GetBounds(product.gameObject);
                Vector3 centerOffset = bounds.center - product.transform.position;
                float step = Mathf.Max(spacing, Mathf.Max(bounds.size.x, bounds.size.z) + 0.12f);
                bool placed = false;
                for (int ring = 0; ring <= 8 && !placed; ring++)
                {
                    for (int z = -ring; z <= ring && !placed; z++)
                    for (int x = -ring; x <= ring && !placed; x++)
                    {
                        if (ring > 0 && Mathf.Abs(x) != ring && Mathf.Abs(z) != ring) continue;
                        Vector3 candidate = outputPoint.position + Vector3.right * (x * step) + Vector3.forward * (z * step);
                        Vector3 rayOrigin = candidate + Vector3.up * 3f;
                        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit ground, 8f,
                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                        if (ground.collider.GetComponentInParent<WorldItem>() != null ||
                            ground.collider.GetComponentInParent<Inventory>() != null) continue;
                        candidate.y = ground.point.y + bounds.extents.y - centerOffset.y + 0.06f;
                        var proposed = new Bounds(candidate + centerOffset, bounds.size + Vector3.one * 0.04f);
                        bool blocked = false;
                        foreach (Bounds previous in occupied)
                            if (previous.Intersects(proposed)) { blocked = true; break; }
                        if (blocked) continue;
                        int count = Physics.OverlapBoxNonAlloc(proposed.center, proposed.extents, hits,
                            Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Collide);
                        if (count == hits.Length) continue;
                        for (int h = 0; h < count; h++)
                        {
                            Collider hit = hits[h];
                            if (hit != null && (!hit.isTrigger || hit.GetComponentInParent<WorldItem>() != null))
                            { blocked = true; break; }
                        }
                        if (blocked) continue;
                        product.transform.position = candidate;
                        product.Initialize(prefab.Item, 1);
                        foreach (Outline outline in product.GetComponentsInChildren<Outline>(true)) outline.enabled = false;
                        occupied.Add(proposed);
                        placed = true;
                    }
                }
                if (!placed)
                {
                    error = "Çıkış çevresinde güvenli boş yer bulunamadı; malzemeler korundu.";
                    Destroy(batch);
                    return false;
                }
            }
            batch.SetActive(true);
            foreach (WorldItem product in products)
            {
                if (product == null || !product.isActiveAndEnabled)
                    throw new InvalidOperationException("Ürün prefabı etkinleştirilemedi.");
            }
            foreach (WorldItem product in products) product.transform.SetParent(null, true);
            Destroy(batch);
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            foreach (WorldItem product in products)
                if (product != null) { product.gameObject.SetActive(false); Destroy(product.gameObject); }
            if (batch != null) { batch.SetActive(false); Destroy(batch); }
            error = "Ürün oluşturulamadı: " + exception.Message + " Malzemeler korundu.";
            return false;
        }
    }

    private static Bounds GetBounds(GameObject item)
    {
        Bounds result = new Bounds(item.transform.position, Vector3.one * 0.15f);
        bool found = false;
        foreach (MeshFilter filter in item.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            Bounds local = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 world = filter.transform.TransformPoint(corner);
                if (!found) { result = new Bounds(world, Vector3.zero); found = true; }
                else result.Encapsulate(world);
            }
        }
        return result;
    }

    private static void Destroy(UnityEngine.Object value)
    {
        if (Application.isPlaying) UnityEngine.Object.Destroy(value);
        else UnityEngine.Object.DestroyImmediate(value);
    }
}
