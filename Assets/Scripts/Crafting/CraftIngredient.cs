using System;
using UnityEngine;

[Serializable]
public sealed class CraftIngredient
{
    [SerializeField] private ItemData item;
    [SerializeField, Min(1)] private int requiredAmount = 1;

    public ItemData Item => item;
    public int RequiredAmount => requiredAmount;
}
