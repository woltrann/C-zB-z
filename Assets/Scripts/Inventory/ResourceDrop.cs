using System;
using UnityEngine;

[Serializable]
public class ResourceDrop
{
    [Header("Resource")]
    public ItemData item;

    [Header("Amount")]
    [Min(1)]
    public int amount = 1;

    [Header("World Object")]
    public GameObject dropPrefab;

    [Header("Spawn")]
    [Min(1)]
    public int spawnCount = 1;
}