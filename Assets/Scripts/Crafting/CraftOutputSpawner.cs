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

        var visuals = outputPoint.GetComponentInParent<CraftStation>();
        if (visuals == null) { error = "CraftStation bağlantısı eksik."; return false; }
        var batch = new GameObject("CraftOutputBatch");
        batch.SetActive(false);
        var products = new List<WorldItem>(amount);
        var hits = new Collider[32];
        try
        {
            Physics.SyncTransforms();
            float bottom = visuals.StackTop(outputPoint.position.y + visuals.StackGap);
            for (int i = 0; i < amount; i++)
            {
                WorldItem product = UnityEngine.Object.Instantiate(prefab, batch.transform);
                products.Add(product);
                product.transform.rotation = outputPoint.rotation * prefab.transform.rotation;
                Bounds bounds = GetBounds(product.gameObject);
                Vector3 offset = bounds.center - product.transform.position;
                Vector3 center = new Vector3(outputPoint.position.x, bottom + bounds.extents.y, outputPoint.position.z);
                Vector3 extents = bounds.extents + Vector3.one * 0.01f;
                Vector3 queryCenter = center;
                Quaternion queryRotation = Quaternion.identity;
                var box = product.GetComponent<BoxCollider>();
                if (box != null)
                {
                    queryRotation = product.transform.rotation;
                    extents = Vector3.Scale(box.size, product.transform.lossyScale) * 0.5f + Vector3.one * 0.01f;
                    queryCenter = center - offset + queryRotation * Vector3.Scale(box.center, product.transform.lossyScale);
                }
                int count = Physics.OverlapBoxNonAlloc(queryCenter, extents, hits, queryRotation,
                    Physics.AllLayers, QueryTriggerInteraction.Collide);
                if (count == hits.Length) throw new InvalidOperationException("Çıktı alanı çok kalabalık.");
                for (int h = 0; h < count; h++)
                    if (hits[h] != null && hits[h].GetComponentInParent<CraftStation>() != visuals &&
                        (!hits[h].isTrigger || hits[h].GetComponentInParent<WorldItem>() != null))
                        throw new InvalidOperationException("İstif noktasında engel var; çıktı noktası yüzeyin üstünde olmalı.");
                product.transform.position = center - offset;
                product.Initialize(prefab.Item, 1);
                foreach (Outline outline in product.GetComponentsInChildren<Outline>(true)) outline.enabled = false;
                var body = product.GetComponent<Rigidbody>();
                if (body != null && visuals.KeepStackStable)
                {
                    if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                    body.isKinematic = true;
                }
                bottom = center.y + bounds.extents.y + visuals.StackGap;
            }
            batch.SetActive(true);
            foreach (WorldItem product in products)
                if (product == null || !product.isActiveAndEnabled)
                    throw new InvalidOperationException("Ürün prefabı etkinleştirilemedi.");
            foreach (WorldItem product in products)
            {
                product.transform.SetParent(null, true);
                visuals.RegisterOutput(product);
            }
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

    internal static Bounds GetBounds(GameObject item)
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
        foreach (BoxCollider box in item.GetComponentsInChildren<BoxCollider>(true))
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = box.center + Vector3.Scale(box.size * 0.5f,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Vector3 world = box.transform.TransformPoint(corner);
            if (!found) { result = new Bounds(world, Vector3.zero); found = true; }
            else result.Encapsulate(world);
        }
        return result;
    }

    private static void Destroy(UnityEngine.Object value)
    {
        if (Application.isPlaying) UnityEngine.Object.Destroy(value);
        else UnityEngine.Object.DestroyImmediate(value);
    }
}
