using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StorageContainer : MonoBehaviour
{
    [SerializeField, Min(1)] private int slotCount = 2;
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();
    [SerializeField] private Transform lid;
    [SerializeField] private Vector3 lidOpenOffset;
    [SerializeField] private Vector3 lidOpenEuler = new Vector3(0f, 0f, -85f);
    [SerializeField, Min(0.01f)] private float openDuration = 0.4f;
    [SerializeField] private Outline outline;

    private Vector3 closedPosition;
    private Quaternion closedRotation;
    private Coroutine opening;
    private bool initialized;

    public IReadOnlyList<InventorySlot> Slots { get { EnsureSlots(); return slots; } }
    public int SlotCount => slotCount;
    public event Action OnChanged;

    private void Awake()
    {
        EnsureSlots();
        InitializeLid();
        SetHighlighted(false);
    }

    private void EnsureSlots()
    {
        while (slots.Count < slotCount)
            slots.Add(new InventorySlot());
    }

    private void InitializeLid()
    {
        if (initialized)
            return;
        if (lid != null)
        {
            closedPosition = lid.localPosition;
            closedRotation = lid.localRotation;
        }
        initialized = true;
    }

    public void SetHighlighted(bool value)
    {
        if (outline != null)
            outline.enabled = value;
    }

    public void Open(Action ready)
    {
        InitializeLid();
        SetHighlighted(false);
        if (opening != null)
            StopCoroutine(opening);
        if (lid == null)
        {
            ready?.Invoke();
            return;
        }
        opening = StartCoroutine(OpenLid(ready));
    }

    private IEnumerator OpenLid(Action ready)
    {
        Vector3 fromPosition = lid.localPosition;
        Quaternion fromRotation = lid.localRotation;
        Quaternion targetRotation = closedRotation * Quaternion.Euler(lidOpenEuler);
        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / openDuration));
            lid.localPosition = Vector3.Lerp(fromPosition, closedPosition + lidOpenOffset, t);
            lid.localRotation = Quaternion.Slerp(fromRotation, targetRotation, t);
            yield return null;
        }
        opening = null;
        ready?.Invoke();
    }

    public void Close()
    {
        if (opening != null)
            StopCoroutine(opening);
        opening = null;
        if (initialized && lid != null)
        {
            lid.localPosition = closedPosition;
            lid.localRotation = closedRotation;
        }
        SetHighlighted(false);
    }

    public void NotifyChanged() => OnChanged?.Invoke();

    private void OnDisable() => Close();
}
