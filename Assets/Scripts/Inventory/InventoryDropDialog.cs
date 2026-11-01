using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryDropDialog : MonoBehaviour
{
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private TMP_Text heading;
    [SerializeField] private TMP_Text feedback;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button transferAllButton;
    private bool initialized;
    private Func<int, bool> confirm;
    private Action cancelled;
    private int maximum;
    private TMP_FontAsset font;

    public bool IsOpen => gameObject.activeSelf;

    public static InventoryDropDialog Create(Canvas canvas, TMP_FontAsset fontAsset)
    {
        GameObject root = new GameObject("InventoryDropDialog", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
        InventoryDropDialog dialog = root.AddComponent<InventoryDropDialog>();
        dialog.font = fontAsset != null ? fontAsset : TMP_Settings.defaultFontAsset;
        dialog.Build();
        root.SetActive(false);
        return dialog;
    }

    private void Build()
    {
        RectTransform panel = Rect("Panel", transform, new Vector2(370f, 320f), Vector2.zero);
        Image background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.12f, 0.15f, 0.18f, 1f);
        heading = Label("Heading", panel, new Vector2(330f, 80f), new Vector2(0f, 72f), 23f);
        RectTransform inputRect = Rect("Amount", panel, new Vector2(170f, 44f), new Vector2(0f, 8f));
        Image inputBackground = inputRect.gameObject.AddComponent<Image>();
        inputBackground.color = new Color(0.23f, 0.27f, 0.3f, 1f);
        RectTransform viewport = Rect("Viewport", inputRect, Vector2.zero, Vector2.zero);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(10f, 4f);
        viewport.offsetMax = new Vector2(-10f, -4f);
        viewport.gameObject.AddComponent<RectMask2D>();
        TMP_Text inputText = Label("Text", viewport, Vector2.zero, Vector2.zero, 24f);
        inputText.rectTransform.anchorMin = Vector2.zero;
        inputText.rectTransform.anchorMax = Vector2.one;
        inputText.rectTransform.offsetMin = inputText.rectTransform.offsetMax = Vector2.zero;
        amountInput = inputRect.gameObject.AddComponent<TMP_InputField>();
        amountInput.targetGraphic = inputBackground;
        amountInput.textViewport = viewport;
        amountInput.textComponent = inputText;
        amountInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        amountInput.characterLimit = 10;
        amountInput.lineType = TMP_InputField.LineType.SingleLine;

        feedback = Label("Feedback", panel, new Vector2(330f, 35f), new Vector2(0f, -35f), 16f);
        confirmButton = MakeButton("Drop", "Yere bırak", panel, new Vector2(85f, -86f));
        cancelButton = MakeButton("Cancel", "İptal", panel, new Vector2(-85f, -86f));
        transferAllButton = MakeButton("TransferAll", "Hepsini taşı", panel, new Vector2(0f, -138f));
        ((RectTransform)transferAllButton.transform).sizeDelta = new Vector2(315f, 42f);
        transferAllButton.gameObject.SetActive(false);
    }

    private void Initialize()
    {
        if (initialized)
            return;
        amountInput.onValueChanged.AddListener(ValidateAmount);
        confirmButton.onClick.AddListener(Submit);
        cancelButton.onClick.AddListener(Cancel);
        if (transferAllButton != null)
            transferAllButton.onClick.AddListener(SubmitAll);
        initialized = true;
    }

    public void Show(ItemData item, int available, Func<int, bool> onConfirm, Action onCancel)
    {
        Initialize();
        maximum = available;
        confirm = onConfirm;
        cancelled = onCancel;
        heading.richText = false;
        heading.text = item.ItemName + "\nKaç tanesini atmak istersin?";
        confirmButton.GetComponentInChildren<TMP_Text>().text = "Yere bırak";
        if (transferAllButton != null)
        {
            transferAllButton.GetComponentInChildren<TMP_Text>(true).text = "Hepsini yere at";
            transferAllButton.gameObject.SetActive(true);
        }
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        amountInput.text = "1";
        ValidateAmount(amountInput.text);
        amountInput.Select();
        amountInput.ActivateInputField();
    }

    public void ShowTransfer(ItemData item, int available, Func<int, bool> onConfirm, Action onCancel)
    {
        Show(item, available, onConfirm, onCancel);
        heading.text = item.ItemName + "\nKaç tanesini aktarmak istersin?";
        confirmButton.GetComponentInChildren<TMP_Text>().text = "Aktar";
        if (transferAllButton != null)
        {
            transferAllButton.GetComponentInChildren<TMP_Text>(true).text = "Hepsini taşı";
            transferAllButton.gameObject.SetActive(true);
        }
    }

    private void SubmitAll()
    {
        amountInput.text = maximum.ToString();
        Submit();
    }

    private void ValidateAmount(string value)
    {
        bool valid = int.TryParse(value, out int amount) && amount >= 1 && amount <= maximum;
        confirmButton.interactable = valid;
        feedback.text = valid ? "Mevcut: " + maximum : "1 ile " + maximum + " arasında bir miktar gir.";
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Cancel();
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            Submit();
    }

    private void Submit()
    {
        if (!int.TryParse(amountInput.text, out int amount) || amount < 1 || amount > maximum)
            return;
        if (confirm != null && confirm(amount))
            Close();
        else
            feedback.text = "İşlem yapılamadı. Miktarı kontrol et veya iptal et.";
    }

    public void Cancel()
    {
        Action callback = cancelled;
        Close();
        callback?.Invoke();
    }

    private void Close()
    {
        confirm = null;
        cancelled = null;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        gameObject.SetActive(false);
    }

    private RectTransform Rect(string objectName, Transform parent, Vector2 size, Vector2 position)
    {
        RectTransform rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private TMP_Text Label(string objectName, Transform parent, Vector2 size, Vector2 position, float fontSize)
    {
        TextMeshProUGUI label = Rect(objectName, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private Button MakeButton(string objectName, string caption, Transform parent, Vector2 position)
    {
        RectTransform rect = Rect(objectName, parent, new Vector2(145f, 42f), position);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.25f, 0.36f, 0.4f, 1f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        Label("Label", rect, new Vector2(140f, 40f), Vector2.zero, 20f).text = caption;
        return button;
    }
}
