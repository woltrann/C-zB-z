using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class CraftStation : NatureObject
{
    [Header("Station Settings")]
    [SerializeField] private string stationName = "Craft Station";
    [SerializeField, Min(0.1f)] private float interactionDistance = 2.5f;
    [SerializeField] private Transform outputPoint;
    [SerializeField, HideInInspector] private float outputSpacing = 0.8f;
    [SerializeField] private ParticleSystem completionEffect;
    [SerializeField] private Outline outline;
    [Header("Recipes")]
    [SerializeField] private List<CraftRecipe> recipes = new List<CraftRecipe>();

    private static readonly HashSet<CraftStation> stations = new HashSet<CraftStation>();
    private readonly Dictionary<ItemData, int> reserved = new Dictionary<ItemData, int>();
    private Inventory reservationOwner;
    private int preparedRecipeIndex = -1;
    private bool changing;
    private Coroutine production;
    private Collider[] interactionColliders;

    public static float MaximumInteractionDistance { get; private set; }
    public string StationName => stationName;
    public float InteractionDistance => interactionDistance;
    public IReadOnlyList<CraftRecipe> Recipes => recipes;
    public int PreparedRecipeIndex => preparedRecipeIndex;
    public CraftRecipe PreparedRecipe => preparedRecipeIndex >= 0 && preparedRecipeIndex < recipes.Count ? recipes[preparedRecipeIndex] : null;
    public bool IsCrafting { get; private set; }
    public bool HasReservedMaterials => reserved.Count > 0;
    public float Progress { get; private set; }
    public int CompletedBatches { get; private set; }
    public int TotalBatches { get; private set; }
    public int ReadyBatches
    {
        get
        {
            if (PreparedRecipe == null || !ValidateRecipe(preparedRecipeIndex, out _)) return 0;
            int count = int.MaxValue;
            foreach (CraftIngredient ingredient in PreparedRecipe.RequiredMaterials)
                count = Mathf.Min(count, GetReserved(ingredient.Item) / ingredient.RequiredAmount);
            return count;
        }
    }
    public string LastMessage { get; private set; }
    public event Action Changed;
    public event Action<float> ProgressChanged;

    private void OnEnable()
    {
        stations.Add(this);
        UpdateMaximumDistance();
        SetHighlighted(false);
        SubscribeVisuals();
    }

    private void OnDisable()
    {
        DisableVisuals();
        stations.Remove(this);
        UpdateMaximumDistance();
        SetHighlighted(false);
        if (production != null)
            StopCoroutine(production);
        production = null;
        if (IsCrafting)
        {
            IsCrafting = false;
            Progress = 0;
            LastMessage = "Üretim durduruldu; malzemeler tezgahta korunuyor.";
            Changed?.Invoke();
        }
    }

    private void OnValidate() { if (Application.isPlaying) UpdateMaximumDistance(); }

    private static void UpdateMaximumDistance()
    {
        MaximumInteractionDistance = 0;
        foreach (CraftStation station in stations)
            if (station != null && station.isActiveAndEnabled && station.interactionDistance > 0 &&
                !float.IsNaN(station.interactionDistance) && !float.IsInfinity(station.interactionDistance))
                MaximumInteractionDistance = Mathf.Max(MaximumInteractionDistance, station.interactionDistance);
    }

    public override void Interact(PlayerToolController player)
    {
        if (player != null && CraftingUI.Instance != null)
            CraftingUI.Instance.Open(this, player.GetComponent<Inventory>());
    }

    public void SetHighlighted(bool highlighted)
    {
        if (outline != null)
            outline.enabled = highlighted;
    }

    public bool IsInRange(Transform player)
    {
        return player != null && (player.position - GetInteractionPoint(player.position)).sqrMagnitude <= interactionDistance * interactionDistance;
    }

    // Model children can be offset or scaled independently from the station root.
    public Vector3 GetInteractionPoint(Vector3 observer)
    {
        if (interactionColliders == null)
            interactionColliders = GetComponentsInChildren<Collider>(true);
        Vector3 nearest = transform.position;
        float nearestDistance = float.PositiveInfinity;
        foreach (Collider collider in interactionColliders)
        {
            if (collider == null || !collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy) continue;
            MeshCollider meshCollider = collider as MeshCollider;
            Vector3 point = meshCollider != null && !meshCollider.convex
                ? collider.bounds.ClosestPoint(observer)
                : collider.ClosestPoint(observer);
            float distance = (point - observer).sqrMagnitude;
            if (distance < nearestDistance) { nearest = point; nearestDistance = distance; }
        }
        return nearest;
    }

    public int GetReserved(ItemData item) => item != null && reserved.TryGetValue(item, out int amount) ? amount : 0;

    public bool ValidateRecipe(int index, out string error)
    {
        if (outputPoint == null) { error = "OutputPoint atanmamış."; return false; }
        if (interactionDistance <= 0 || float.IsNaN(interactionDistance) || float.IsInfinity(interactionDistance))
        { error = "Etkileşim uzaklığı pozitif ve sonlu olmalı."; return false; }
        if (index < 0 || index >= recipes.Count || recipes[index] == null)
        { error = "Geçerli bir tarif seç."; return false; }
        if (!recipes[index].Validate(out error)) return false;
        return ValidateInputs(recipes[index], out error);
    }

    public bool IsReady => ReadyBatches > 0;

    private bool CanChange(Inventory inventory, bool allowCrafting = false)
    {
        return !changing && isActiveAndEnabled && inventory != null && IsInRange(inventory.transform) && (!IsCrafting || allowCrafting) &&
               (reserved.Count == 0 || reservationOwner == inventory);
    }

    private bool Reject(string message) { LastMessage = message; return false; }

    public bool TryPrepare(int recipeIndex, Inventory inventory)
    {
        if (!CanChange(inventory)) return Reject("Tezgah şu anda bu işleme uygun değil.");
        if (!ValidateRecipe(recipeIndex, out string error)) return Reject(error);
        if (preparedRecipeIndex == recipeIndex) return true;
        if (reserved.Count > 0) return Reject("Önce eklenen malzemeleri geri al");
        preparedRecipeIndex = recipeIndex;
        reservationOwner = inventory;
        LastMessage = "Malzemeleri tezgaha ekle.";
        Changed?.Invoke();
        return true;
    }

    public bool TryLoad(ItemData item, int amount, Inventory inventory)
    {
        if (!CanChange(inventory) || amount <= 0 || PreparedRecipe == null) return Reject("Malzeme eklenemedi.");
        if (!ValidateRecipe(preparedRecipeIndex, out string error)) return Reject(error);
        int required = 0;
        foreach (CraftIngredient ingredient in PreparedRecipe.RequiredMaterials)
            if (ingredient.Item == item) { required = ingredient.RequiredAmount; break; }
        int previous = GetReserved(item);
        if (item == null || required == 0 || amount > required - previous || !inventory.HasItem(item, amount))
            return Reject("Yeterli malzeme yok veya miktar sınırı aşılmış.");
        changing = true;
        try
        {
            reserved[item] = previous + amount;
            if (!inventory.RemoveItem(item, amount))
            {
                SetReserved(item, previous);
                return Reject("Envanter değişti; malzeme aktarılmadı.");
            }
            reservationOwner = inventory;
        }
        finally { changing = false; }
        LastMessage = "Malzeme eklendi.";
        Changed?.Invoke();
        return true;
    }

    private void SetReserved(ItemData item, int amount)
    {
        if (amount > 0) reserved[item] = amount;
        else reserved.Remove(item);
    }

    public bool TryUnload(ItemData item, int amount, Inventory inventory)
    {
        if (!CanChange(inventory) || item == null || amount <= 0 || GetReserved(item) < amount)
            return Reject("Bu malzeme geri alınamıyor.");
        int previous = GetReserved(item);
        changing = true;
        try
        {
            SetReserved(item, previous - amount);
            if (!inventory.AddItem(item, amount))
            {
                SetReserved(item, previous);
                return Reject("Envanterde yer yok; malzeme tezgahta kaldı.");
            }
        }
        finally { changing = false; }
        LastMessage = "Malzeme geri alındı.";
        Changed?.Invoke();
        return true;
    }

    public bool TryCancel(Inventory inventory)
    {
        if (!CanChange(inventory, true)) return Reject("İptal işlemi şu anda yapılamıyor.");
        if (production != null) StopCoroutine(production);
        production = null;
        IsCrafting = false;
        Progress = 0;
        changing = true;
        try
        {
            var refund = new Dictionary<ItemData, int>(reserved);
            reserved.Clear();
            if (!inventory.TryAddItems(refund))
            {
                foreach (var pair in refund) reserved.Add(pair.Key, pair.Value);
                LastMessage = "Üretim durduruldu. İade için envanterde yer yok; malzemeler tezgahta korundu.";
                Changed?.Invoke();
                return false;
            }
            preparedRecipeIndex = -1;
            reservationOwner = null;
        }
        finally { changing = false; }
        LastMessage = "Tarif iptal edildi; malzemeler iade edildi.";
        Changed?.Invoke();
        return true;
    }

    public bool TryStart(Inventory inventory)
    {
        if (!CanChange(inventory) || !IsReady) return Reject(IsCrafting ? "Üretim zaten sürüyor." : "Önce gereken malzemeleri ekle.");
        CraftRecipe recipe = PreparedRecipe;
        var ingredients = new Dictionary<ItemData, int>();
        foreach (CraftIngredient ingredient in recipe.RequiredMaterials)
            ingredients.Add(ingredient.Item, ingredient.RequiredAmount);
        TotalBatches = 1;
        CompletedBatches = 0;
        IsCrafting = true;
        Progress = 0;
        LastMessage = "Üretiliyor...";
        production = StartCoroutine(Produce(recipe.OutputWorldPrefab, recipe.OutputAmount, recipe.CraftDuration, ingredients));
        Changed?.Invoke();
        return true;
    }

    private IEnumerator Produce(WorldItem prefab, int amount, float duration, Dictionary<ItemData, int> ingredients)
    {
        while (CompletedBatches < TotalBatches)
        {
            Progress = 0;
            Changed?.Invoke();
            // Yield even for instant recipes: large queues never block the main thread.
            yield return null;
            float elapsed = 0;
            while (elapsed < duration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                Progress = Mathf.Clamp01(elapsed / duration);
                ProgressChanged?.Invoke(Progress);
            }
            if (!Complete(prefab, amount, ingredients)) break;
            CompletedBatches++;
            Progress = 1;
            Changed?.Invoke();
        }
        production = null;
        IsCrafting = false;
        Changed?.Invoke();
    }

    private bool Complete(WorldItem prefab, int amount, Dictionary<ItemData, int> ingredients)
    {
        changing = true;
        bool spawned;
        string error;
        try
        {
            foreach (var ingredient in ingredients)
                if (GetReserved(ingredient.Key) < ingredient.Value)
                { LastMessage = "Malzemeler değişti; üretim durduruldu."; return false; }
            Physics.SyncTransforms();
            spawned = CraftOutputSpawner.TrySpawn(prefab, amount, outputPoint, outputSpacing, out error);
            if (spawned)
                foreach (var ingredient in ingredients)
                    SetReserved(ingredient.Key, GetReserved(ingredient.Key) - ingredient.Value);
        }
        finally { changing = false; }
        LastMessage = spawned ? "Üretim tamamlandı! Ürünleri F ile toplayabilirsin." : error;
        if (spawned && completionEffect != null) completionEffect.Play();
        return spawned;
    }

    private sealed class InputPreview
    {
        public ItemData item;
        public GameObject prefab;
        public readonly List<GameObject> instances = new List<GameObject>();
    }

    [Header("Input Placement")]
    [SerializeField] private Transform inputPoint;
    [SerializeField, Min(0)] private float inputSpacing = 0.5f;
    [Header("Output Stack")]
    [SerializeField, Min(0.03f)] private float stackGap = 0.05f;
    [SerializeField] private bool keepStackStable = true;

    private bool subscribed;
    private readonly Dictionary<ItemData, InputPreview> inputPreviews = new Dictionary<ItemData, InputPreview>();
    private readonly List<InputPreview> rowOrder = new List<InputPreview>();
    private readonly List<WorldItem> outputs = new List<WorldItem>();
    public float StackGap => Mathf.Max(0.03f, stackGap);
    public bool KeepStackStable => keepStackStable;

    private void SubscribeVisuals()
    {
        if (subscribed) return;
        Changed += RefreshVisuals;
        subscribed = true;
        RefreshVisuals();
    }

    private void DisableVisuals()
    {
        if (subscribed) Changed -= RefreshVisuals;
        subscribed = false;
        HideAll();
    }

    private void OnDestroy()
    {
        foreach (var entry in inputPreviews.Values)
            if (entry != null) foreach (var instance in entry.instances)
                if (instance != null) Destroy(instance);
    }

    private InputPreview Find(ItemData item)
    {
        if (!inputPreviews.TryGetValue(item, out var entry))
        {
            entry = new InputPreview { item = item, prefab = item.WorldPrefab };
            inputPreviews.Add(item, entry);
        }
        return entry;
    }

    public bool ValidateInputs(CraftRecipe recipe, out string error)
    {
        error = null;
        if (inputPoint == null) return true;
        if (inputSpacing < 0 || float.IsNaN(inputSpacing) || float.IsInfinity(inputSpacing))
        { error = "Giriş aralığı geçersiz."; return false; }
        foreach (var ingredient in recipe.RequiredMaterials)
            if (ingredient.Item.WorldPrefab == null || ingredient.Item.WorldPrefab.GetComponentInChildren<MeshFilter>(true) == null)
            { error = ingredient.Item.ItemName + " için ItemData World Prefab alanını ata."; return false; }
        return true;
    }

    private void HideAll()
    {
        foreach (var entry in inputPreviews.Values)
            if (entry != null) foreach (var instance in entry.instances)
                if (instance != null) instance.SetActive(false);
    }

    private void RefreshVisuals()
    {
        HideAll();
        if (inputPoint == null || PreparedRecipe == null) { rowOrder.Clear(); return; }
        foreach (var ingredient in PreparedRecipe.RequiredMaterials) Find(ingredient.Item);
        foreach (var entry in inputPreviews.Values)
        {
            if (entry == null) continue;
            int wanted = GetReserved(entry.item);
            int existing = rowOrder.FindAll(value => value == entry).Count;
            while (existing > wanted) { rowOrder.RemoveAt(rowOrder.LastIndexOf(entry)); existing--; }
            while (existing < wanted) { rowOrder.Add(entry); existing++; }
        }
        float cursor = 0;
        var used = new Dictionary<InputPreview, int>();
        foreach (var entry in rowOrder)
        {
            int index = used.TryGetValue(entry, out int count) ? count : 0;
            used[entry] = index + 1;
            if (entry.prefab == null) continue;
            if (index == entry.instances.Count) entry.instances.Add(CreateRenderOnly(entry.prefab));
            GameObject visual = entry.instances[index];
            visual.transform.position = inputPoint.position + inputPoint.right * cursor;
            visual.transform.rotation = inputPoint.rotation * entry.prefab.transform.rotation;
            Vector3 size = entry.prefab.transform.lossyScale;
            visual.transform.localScale = new Vector3(size.x / transform.lossyScale.x,
                size.y / transform.lossyScale.y, size.z / transform.lossyScale.z);
            visual.SetActive(true);
            cursor += inputSpacing;
        }
    }

    private GameObject CreateRenderOnly(GameObject prefab)
    {
        var root = new GameObject("Reserved Material Visual");
        root.transform.SetParent(transform, false);
        foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            if (filter.sharedMesh == null || renderer == null) continue;
            var part = new GameObject(filter.name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = prefab.transform.InverseTransformPoint(filter.transform.position);
            part.transform.localRotation = Quaternion.Inverse(prefab.transform.rotation) * filter.transform.rotation;
            part.transform.localScale = new Vector3(filter.transform.lossyScale.x / prefab.transform.lossyScale.x,
                filter.transform.lossyScale.y / prefab.transform.lossyScale.y, filter.transform.lossyScale.z / prefab.transform.lossyScale.z);
            part.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            part.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
        }
        return root;
    }

    public float StackTop(float baseHeight)
    {
        outputs.RemoveAll(item => item == null || !item.gameObject.activeInHierarchy);
        foreach (var item in outputs) baseHeight = Mathf.Max(baseHeight, CraftOutputSpawner.GetBounds(item.gameObject).max.y + StackGap);
        return baseHeight;
    }

    public void RegisterOutput(WorldItem item) => outputs.Add(item);
}
