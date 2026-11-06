using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterPanelToggle : MonoBehaviour
{
    [SerializeField] private RectTransform drawerViewport;
    [SerializeField] private CanvasGroup drawerContent;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text arrowText;
    [SerializeField] private CanvasGroup panelVisibility;
    [SerializeField] private RectTransform statsViewport;
    [SerializeField] private CanvasGroup compactStats;
    [SerializeField] private CanvasGroup detailedStats;
    [SerializeField] private float closedWidth = 64f;
    [SerializeField] private float openWidth = 196f;
    [SerializeField, Min(0f)] private float animationDuration = 0.22f;

    private bool isOpen;
    private bool statsExpanded;
    private float progress;

    public bool IsOpen => isOpen;
    public bool AreStatsExpanded => statsExpanded;

    private void Awake()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleStats);

        if (drawerContent != null)
            drawerContent.alpha = 1f;
        ApplyPanelVisibility();
        ApplyVisualState();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && !InventoryUI.IsInputBlocked && !IsEditingText())
            Toggle();

        float target = statsExpanded ? 1f : 0f;
        if (Mathf.Approximately(progress, target))
            return;

        progress = animationDuration > 0f
            ? Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime / animationDuration)
            : target;
        ApplyVisualState();
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        ApplyPanelVisibility();
    }

    public void ToggleStats()
    {
        if (!isOpen)
            return;

        statsExpanded = !statsExpanded;
        if (arrowText != null)
            arrowText.text = statsExpanded ? ">" : "<";
    }

    private void ApplyPanelVisibility()
    {
        if (panelVisibility == null)
            return;

        panelVisibility.alpha = isOpen ? 1f : 0f;
        panelVisibility.interactable = isOpen;
        panelVisibility.blocksRaycasts = isOpen;
    }

    private void ApplyVisualState()
    {
        float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
        if (drawerViewport != null && statsViewport != null)
            statsViewport.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                Mathf.Lerp(closedWidth, openWidth, easedProgress));

        if (compactStats != null)
            compactStats.alpha = 1f - easedProgress;
        if (detailedStats != null)
            detailedStats.alpha = easedProgress;

        if (toggleButton != null && statsViewport != null)
        {
            RectTransform arrowRect = (RectTransform)toggleButton.transform;
            Vector2 position = arrowRect.anchoredPosition;
            position.x = statsViewport.anchoredPosition.x - statsViewport.rect.width;
            arrowRect.anchoredPosition = position;
        }
    }

    private static bool IsEditingText()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
            return false;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        TMP_InputField tmpInput = selected.GetComponentInParent<TMP_InputField>();
        if (tmpInput != null && tmpInput.isFocused)
            return true;

        InputField input = selected.GetComponentInParent<InputField>();
        return input != null && input.isFocused;
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(ToggleStats);
    }
}

