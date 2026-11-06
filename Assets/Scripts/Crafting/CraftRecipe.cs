using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CraftRecipe
{
    [SerializeField] private string recipeName;
    [SerializeField] private WorldItem outputWorldPrefab;
    [SerializeField, Min(1)] private int outputAmount = 1;
    [SerializeField, Min(0)] private float craftDuration;
    [SerializeField] private List<CraftIngredient> requiredMaterials = new List<CraftIngredient>();

    public string RecipeName => recipeName;
    public WorldItem OutputWorldPrefab => outputWorldPrefab;
    public int OutputAmount => outputAmount;
    public float CraftDuration => craftDuration;
    public IReadOnlyList<CraftIngredient> RequiredMaterials => requiredMaterials;

    public bool Validate(out string error)
    {
        if (string.IsNullOrWhiteSpace(recipeName))
            error = "Tarif adı boş.";
        else if (outputWorldPrefab == null || outputWorldPrefab.Item == null)
            error = "Ürün prefabı veya prefabın ItemData referansı eksik.";
        else if (!outputWorldPrefab.enabled || !outputWorldPrefab.gameObject.activeSelf ||
                 outputWorldPrefab.GetComponentInChildren<Collider>() == null)
            error = "Ürün prefabında etkin WorldItem ve Collider gerekli.";
        else if (outputAmount <= 0 || craftDuration < 0 || float.IsNaN(craftDuration) || float.IsInfinity(craftDuration))
            error = "Ürün miktarı pozitif, süre sonlu ve sıfır veya daha büyük olmalı.";
        else if (requiredMaterials == null || requiredMaterials.Count == 0)
            error = "En az bir malzeme gerekli.";
        else
        {
            for (int i = 0; i < requiredMaterials.Count; i++)
            {
                CraftIngredient ingredient = requiredMaterials[i];
                if (ingredient == null || ingredient.Item == null || ingredient.RequiredAmount <= 0)
                {
                    error = "Malzeme referansı ve pozitif miktarı gerekli.";
                    return false;
                }
                for (int j = 0; j < i; j++)
                    if (requiredMaterials[j].Item == ingredient.Item)
                    {
                        error = "Aynı malzeme tarifte iki defa bulunamaz.";
                        return false;
                    }
            }
            error = null;
            return true;
        }
        return false;
    }
}
