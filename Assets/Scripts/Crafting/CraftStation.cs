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
    [SerializeField, Min(0.05f)] private float outputSpacing = 0.8f;
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
    public string LastMessage { get; private set; }
    public event Action Changed;
    public event Action<float> ProgressChanged;

    private void OnEnable()
    {
        stations.Add(this);
        UpdateMaximumDistance();
        SetHighlighted(false);
    }

    private void OnDisable()
    {
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
        if (interactionDistance <= 0 || float.IsNaN(interactionDistance) || float.IsInfinity(interactionDistance) ||
            outputSpacing <= 0 || float.IsNaN(outputSpacing) || float.IsInfinity(outputSpacing))
        { error = "Etkileşim uzaklığı ve ürün aralığı pozitif ve sonlu olmalı."; return false; }
        if (index < 0 || index >= recipes.Count || recipes[index] == null)
        { error = "Geçerli bir tarif seç."; return false; }
        return recipes[index].Validate(out error);
    }

    public bool IsReady
    {
        get
        {
            CraftRecipe recipe = PreparedRecipe;
            if (recipe == null || !ValidateRecipe(preparedRecipeIndex, out _)) return false;
            foreach (CraftIngredient ingredient in recipe.RequiredMaterials)
                if (GetReserved(ingredient.Item) != ingredient.RequiredAmount) return false;
            return true;
        }
    }

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
            return Reject("Yeterli malzeme yok veya gereken miktar aşılmış.");
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
        changing = true;
        try
        {
            var refund = new Dictionary<ItemData, int>(reserved);
            reserved.Clear();
            if (!inventory.TryAddItems(refund))
            {
                foreach (var pair in refund) reserved.Add(pair.Key, pair.Value);
                return Reject("İptal için envanterde yeterli yer yok. Malzemeler tezgahta korundu.");
            }
            if (production != null) StopCoroutine(production);
            production = null;
            IsCrafting = false;
            Progress = 0;
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
        IsCrafting = true;
        Progress = 0;
        LastMessage = "Üretiliyor...";
        Changed?.Invoke();
        // Snapshot output settings so an Inspector edit cannot change an in-flight job.
        WorldItem prefab = recipe.OutputWorldPrefab;
        int amount = recipe.OutputAmount;
        if (recipe.CraftDuration == 0) Complete(prefab, amount);
        else production = StartCoroutine(Produce(prefab, amount, recipe.CraftDuration));
        return true;
    }

    private IEnumerator Produce(WorldItem prefab, int amount, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            Progress = Mathf.Clamp01(elapsed / duration);
            ProgressChanged?.Invoke(Progress);
        }
        production = null;
        Complete(prefab, amount);
    }

    private void Complete(WorldItem prefab, int amount)
    {
        changing = true;
        bool spawned;
        string error;
        try
        {
            spawned = CraftOutputSpawner.TrySpawn(prefab, amount, outputPoint, outputSpacing, out error);
            if (spawned) reserved.Clear();
        }
        finally { changing = false; IsCrafting = false; production = null; }
        Progress = spawned ? 1 : 0;
        LastMessage = spawned ? "Üretim tamamlandı! Ürünleri F ile toplayabilirsin." : error;
        if (spawned && completionEffect != null) completionEffect.Play();
        Changed?.Invoke();
    }
}
