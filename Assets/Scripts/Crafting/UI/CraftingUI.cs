using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public sealed class CraftingUI : MonoBehaviour
{
    [Header("Theme")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite insetSprite;
    [SerializeField] private Sprite buttonSprite;
    [Header("Shared View")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text stationTitle;
    [SerializeField] private RectTransform recipeContent;
    [SerializeField] private RectTransform ingredientContent;
    [SerializeField] private TMP_Text detailTitle;
    [SerializeField] private TMP_Text detailSummary;
    [SerializeField] private Image outputIcon;
    [SerializeField] private TMP_Text status;
    [SerializeField] private Image progressFill;
    [SerializeField] private Button prepareButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button closeButton;

    public static CraftingUI Instance { get; private set; }
    public static bool BlocksWorldInput => Instance != null && (Instance.IsVisible || Instance.consumedFrame == Time.frameCount);
    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;
    public CraftStation Station => station;

    private CraftStation station;
    private Inventory inventory;
    private int selectedIndex;
    private int openedFrame = -1;
    private int consumedFrame = -1;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private bool ownsCursor;
    private bool localError;
    private string localMessage;
    private readonly List<RecipeView> recipeViews = new List<RecipeView>();
    private readonly List<IngredientView> ingredientViews = new List<IngredientView>();
    private readonly StringBuilder text = new StringBuilder(256);
    private static readonly Color Ink = new Color(0.35f, 0.23f, 0.14f, 1);

    private sealed class RecipeView
    {
        public GameObject Root;
        public Image Icon;
        public Image Background;
        public Button Button;
        public TMP_Text Label;
        public int Index;
    }

    private sealed class IngredientView
    {
        public GameObject Root;
        public Image Icon;
        public TMP_Text Label;
        public Button Add;
        public Button Return;
        public ItemData Item;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
        if (panelRoot == null) BuildView(GetComponentInParent<Canvas>());
        closeButton.onClick.AddListener(Close);
        prepareButton.onClick.AddListener(Prepare);
        cancelButton.onClick.AddListener(CancelPreparation);
        ConfigureView();
        panelRoot.SetActive(false);
    }

    public bool Open(CraftStation target, Inventory playerInventory)
    {
        if (target == null || playerInventory == null || !target.IsInRange(playerInventory.transform)) return false;
        if (!IsVisible && InventoryUI.IsInputBlocked) return false;
        if (IsVisible) Close();
        station = target;
        inventory = playerInventory;
        selectedIndex = station.PreparedRecipeIndex >= 0 ? station.PreparedRecipeIndex : 0;
        localError = false;
        localMessage = null;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        ownsCursor = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        station.Changed += OnStationChanged;
        station.ProgressChanged += UpdateProgress;
        inventory.OnInventoryChanged += Refresh;
        openedFrame = Time.frameCount;
        consumedFrame = Time.frameCount;
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
        FitWindow();
        Refresh();
        return true;
    }

    public void Close()
    {
        bool wasVisible = IsVisible;
        if (station != null) { station.Changed -= OnStationChanged; station.ProgressChanged -= UpdateProgress; }
        if (inventory != null) inventory.OnInventoryChanged -= Refresh;
        station = null;
        inventory = null;
        if (panelRoot != null) panelRoot.SetActive(false);
        if (ownsCursor) { Cursor.lockState = previousCursorLock; Cursor.visible = previousCursorVisible; ownsCursor = false; }
        if (wasVisible) consumedFrame = Time.frameCount;
    }

    private void OnDisable() => Close();

    private void OnDestroy()
    {
        Close();
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (prepareButton != null) prepareButton.onClick.RemoveListener(Prepare);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(CancelPreparation);
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsVisible) return;
        if (station == null || !station.isActiveAndEnabled || inventory == null || !station.IsInRange(inventory.transform))
        { Close(); return; }
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }

    }

    // Kept for callers of the old API; keyboard input no longer starts crafting.
    public bool HandleCraftInput() => false;

    public void ConfigureView()
    {
        if (panelRoot == null) return;
        Transform controls = panelRoot.transform.Find("Window/Controls");
        if (controls != null) { controls.GetComponent<TMP_Text>().text = "ESC: Kapat"; controls.GetComponent<TMP_Text>().fontSize = 18; }
        prepareButton.GetComponentInChildren<TMP_Text>(true).text = "Tarifi Seç";
        cancelButton.GetComponentInChildren<TMP_Text>(true).text = "Seçimi İptal Et";
        prepareButton.GetComponentInChildren<TMP_Text>(true).fontSize = 22;
        cancelButton.GetComponentInChildren<TMP_Text>(true).fontSize = 18;
        detailSummary.fontSize = 24;
        var window = panelRoot.transform.Find("Window") as RectTransform;
        window.sizeDelta = new Vector2(780, 520);
        Layout("StationName", new Vector2(-15, 224));
        Layout("Close", new Vector2(350, 224));
        Layout("RecipesHeading", new Vector2(-252, 174));
        Layout("RecipeName", new Vector2(125, 174));
        Layout("OutputIcon", new Vector2(-70, 120));
        Layout("OutputInfo", new Vector2(140, 120));
        Layout("Recipes", new Vector2(-252, -8), new Vector2(240, 320));
        Layout("Materials", new Vector2(125, -35), new Vector2(456, 230));
        Layout("Progress", new Vector2(125, -162));
        Layout("Prepare", new Vector2(130, -196));
        Layout("Cancel", new Vector2(308, -196));
        Layout("Controls", new Vector2(-252, -206));
        Layout("Status", new Vector2(0, -238));
        panelRoot.GetComponent<Image>().color = new Color(0, 0, 0, 0.03f);
        FitWindow();
    }

    private void Layout(string name, Vector2 position, Vector2? size = null)
    {
        var rect = panelRoot.transform.Find("Window/" + name) as RectTransform;
        if (rect == null) return;
        rect.anchoredPosition = position;
        if (size.HasValue) rect.sizeDelta = size.Value;
    }

    private void OnRectTransformDimensionsChange() => FitWindow();

    private void FitWindow()
    {
        if (panelRoot == null) return;
        RectTransform area = panelRoot.transform as RectTransform;
        RectTransform window = panelRoot.transform.Find("Window") as RectTransform;
        if (area == null || window == null || area.rect.width <= 0 || area.rect.height <= 0) return;
        window.anchorMin = window.anchorMax = new Vector2(0, 0.5f);
        window.pivot = new Vector2(0, 0.5f);
        window.anchoredPosition = new Vector2(16, 0);
        float scale = Mathf.Min(0.85f, Mathf.Min((area.rect.width * 0.35f - 20) / 780f, (area.rect.height - 24) / 520f));
        window.localScale = Vector3.one * Mathf.Max(0.05f, scale);
    }

    private void OnStationChanged() { localError = false; Refresh(); }

    private void SelectRecipe(int index)
    {
        if (station == null || station.IsCrafting) return;
        if (station.PreparedRecipeIndex >= 0 && station.PreparedRecipeIndex != index)
        {
            if (station.HasReservedMaterials)
            {
                localError = true;
                localMessage = "Önce eklenen malzemeleri geri al";
                Refresh();
                return;
            }
            if (!station.TryCancel(inventory)) { Feedback(false); return; }
        }
        selectedIndex = index;
        localError = false;
        localMessage = null;
        Refresh();
    }

    private void Prepare()
    {
        if (station == null || station.IsCrafting) return;
        Feedback(station.PreparedRecipeIndex == selectedIndex
            ? station.TryStart(inventory)
            : station.TryPrepare(selectedIndex, inventory));
    }
    private void CancelPreparation() { if (station != null) Feedback(station.TryCancel(inventory)); }

    private void Feedback(bool success)
    {
        localError = !success;
        localMessage = station != null ? station.LastMessage : null;
        Refresh();
    }

    private void Load(IngredientView row)
    {
        if (station == null || inventory == null || station.PreparedRecipe == null) return;
        Feedback(station.TryLoad(row.Item, 1, inventory));
    }

    private void Return(IngredientView row)
    {
        if (station != null) Feedback(station.TryUnload(row.Item, station.GetReserved(row.Item), inventory));
    }

    private void Refresh()
    {
        if (!IsVisible || station == null || inventory == null) return;
        stationTitle.text = station.StationName;
        for (int i = 0; i < station.Recipes.Count; i++)
        {
            if (i == recipeViews.Count) recipeViews.Add(CreateRecipeView());
            RecipeView row = recipeViews[i];
            row.Index = i;
            row.Root.SetActive(true);
            CraftRecipe recipe = station.Recipes[i];
            ItemData item = recipe != null && recipe.OutputWorldPrefab != null ? recipe.OutputWorldPrefab.Item : null;
            row.Icon.sprite = item != null ? item.Icon : null;
            row.Icon.enabled = row.Icon.sprite != null;
            text.Clear();
            text.Append(recipe != null ? recipe.RecipeName : "Geçersiz tarif");
            if (recipe != null)
            {
                text.Append("  x").Append(recipe.OutputAmount);

            }
            row.Label.text = text.ToString();
            row.Background.color = selectedIndex == i ? new Color(1, 0.85f, 0.61f, 1) : Color.white;
            row.Button.interactable = !station.IsCrafting;
        }
        for (int i = station.Recipes.Count; i < recipeViews.Count; i++) recipeViews[i].Root.SetActive(false);
        CraftRecipe selected = selectedIndex >= 0 && selectedIndex < station.Recipes.Count ? station.Recipes[selectedIndex] : null;
        bool preparing = selected != null && station.PreparedRecipeIndex == selectedIndex;
        detailTitle.text = "ÜRÜN";
        ItemData output = selected != null && selected.OutputWorldPrefab != null ? selected.OutputWorldPrefab.Item : null;
        outputIcon.sprite = output != null ? output.Icon : null;
        outputIcon.enabled = outputIcon.sprite != null;
        detailSummary.text = selected == null ? "Bu tezgahta tarif bulunmuyor." :
            (output != null ? output.ItemName : "Eksik ürün") + " x" + selected.OutputAmount;
        int ingredientCount = selected != null && selected.RequiredMaterials != null ? selected.RequiredMaterials.Count : 0;
        for (int i = 0; i < ingredientCount; i++)
        {
            if (i == ingredientViews.Count) ingredientViews.Add(CreateIngredientView());
            IngredientView row = ingredientViews[i];
            CraftIngredient ingredient = selected.RequiredMaterials[i];
            row.Root.SetActive(true);
            row.Item = ingredient != null ? ingredient.Item : null;
            int required = ingredient != null ? ingredient.RequiredAmount : 0;
            int loaded = preparing ? station.GetReserved(row.Item) : 0;
            int available = inventory.GetItemCount(row.Item);
            row.Icon.sprite = row.Item != null ? row.Item.Icon : null;
            row.Icon.enabled = row.Icon.sprite != null;
            row.Label.text = (row.Item != null ? row.Item.ItemName : "Eksik ItemData") + " — " + loaded + "/" + required +
                "\nEnvanter: " + available;
            row.Add.gameObject.SetActive(preparing && loaded < required);
            row.Return.gameObject.SetActive(preparing && loaded > 0);
            row.Add.interactable = !station.IsCrafting && row.Item != null && available > 0 && loaded < required;
            row.Return.interactable = !station.IsCrafting;

        }
        for (int i = ingredientCount; i < ingredientViews.Count; i++) ingredientViews[i].Root.SetActive(false);
        prepareButton.gameObject.SetActive(selected != null && (!preparing || station.IsReady || station.IsCrafting));
        prepareButton.GetComponentInChildren<TMP_Text>(true).text = preparing ? "Üret" : "Tarifi Seç";
        prepareButton.interactable = !station.IsCrafting;
        cancelButton.gameObject.SetActive(station.PreparedRecipeIndex >= 0);
        cancelButton.interactable = true;
        cancelButton.GetComponentInChildren<TMP_Text>(true).text = station.IsCrafting ? "İptal Et" : "Seçimi İptal Et";
        progressFill.transform.parent.gameObject.SetActive(station.IsCrafting);
        string message = localError ? localMessage : station.IsCrafting ? "Üretiliyor..." :
            station.IsReady ? "Üretime hazır" : station.LastMessage;
        status.text = !preparing ? "" : string.IsNullOrEmpty(message) ? "Üretmek için malzemeleri ekle." : message;
        if (localError) status.text = localMessage;
        status.color = localError ? new Color(0.72f, 0.21f, 0.13f, 1) : Ink;
        UpdateProgress(station.Progress);
    }

    private void UpdateProgress(float value) { if (progressFill != null) progressFill.fillAmount = Mathf.Clamp01(value); }

    private RecipeView CreateRecipeView()
    {
        RectTransform root = Rect(recipeContent, "Recipe", new Vector2(224, 104), Vector2.zero);
        Image background = Image(root, insetSprite, Color.white, true);
        Button button = root.gameObject.AddComponent<Button>(); button.targetGraphic = background;
        root.gameObject.AddComponent<LayoutElement>().preferredHeight = 104;
        var row = new RecipeView { Root = root.gameObject, Background = background, Button = button };
        row.Icon = Image(Rect(root, "Icon", new Vector2(38, 38), new Vector2(-88, 0)), null, Color.white, false);
        row.Icon.preserveAspect = true;
        row.Label = Label(root, "Info", new Vector2(154, 96), new Vector2(25, 0), "", 22);
        row.Label.alignment = TextAlignmentOptions.MidlineLeft;
        button.onClick.AddListener(() => SelectRecipe(row.Index));
        return row;
    }

    private IngredientView CreateIngredientView()
    {
        RectTransform root = Rect(ingredientContent, "Material", new Vector2(432, 100), Vector2.zero);
        Image(root, insetSprite, new Color(1, 0.96f, 0.89f, 1), false);
        root.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
        var row = new IngredientView { Root = root.gameObject };
        row.Icon = Image(Rect(root, "Icon", new Vector2(34, 34), new Vector2(-186, 14)), null, Color.white, false);
        row.Icon.preserveAspect = true;
        row.Label = Label(root, "Info", new Vector2(344, 61), new Vector2(25, 15), "", 22);
        row.Label.alignment = TextAlignmentOptions.MidlineLeft;
        row.Add = MakeButton(root, "Add", "Ekle", new Vector2(112, 25), new Vector2(66, -33), 12);
        row.Return = MakeButton(root, "Return", "Geri Al", new Vector2(88, 25), new Vector2(173, -33), 12);
        row.Add.onClick.AddListener(() => Load(row));
        row.Return.onClick.AddListener(() => Return(row));
        return row;
    }

    public void BuildView(Canvas canvas)
    {
        if (panelRoot != null || canvas == null) return;
        if (font == null) font = TMP_Settings.defaultFontAsset;
        RectTransform overlay = Rect(transform, "CraftingPanel", Vector2.zero, Vector2.zero);
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        Image(overlay, null, new Color(0.08f, 0.06f, 0.04f, 0.65f), true);
        panelRoot = overlay.gameObject;
        RectTransform window = Rect(overlay, "Window", new Vector2(780, 620), Vector2.zero);
        Image(window, panelSprite, Color.white, true);
        stationTitle = Label(window, "StationName", new Vector2(640, 46), new Vector2(-15, 274), "ÜRETİM", 28);
        stationTitle.fontStyle = FontStyles.Bold;
        closeButton = MakeButton(window, "Close", "X", new Vector2(38, 34), new Vector2(350, 274), 20);
        Label(window, "RecipesHeading", new Vector2(230, 26), new Vector2(-252, 224), "TARİFLER", 20).fontStyle = FontStyles.Bold;
        recipeContent = Scroll(window, "Recipes", new Vector2(240, 412), new Vector2(-252, -2));
        detailTitle = Label(window, "RecipeName", new Vector2(450, 32), new Vector2(125, 224), "Tarif seç", 23);
        detailTitle.fontStyle = FontStyles.Bold;
        outputIcon = Image(Rect(window, "OutputIcon", new Vector2(48, 48), new Vector2(-70, 167)), null, Color.white, false);
        outputIcon.preserveAspect = true;
        detailSummary = Label(window, "OutputInfo", new Vector2(350, 55), new Vector2(140, 167), "", 17);
        ingredientContent = Scroll(window, "Materials", new Vector2(456, 318), new Vector2(125, -32));
        RectTransform bar = Rect(window, "Progress", new Vector2(450, 10), new Vector2(125, -202));
        Image(bar, insetSprite, new Color(0.69f, 0.53f, 0.36f, 1), false);
        RectTransform fill = Rect(bar, "Fill", Vector2.zero, Vector2.zero);
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = fill.offsetMax = Vector2.zero;
        progressFill = Image(fill, panelSprite, new Color(0.38f, 0.62f, 0.24f, 1), false);
        progressFill.type = UnityEngine.UI.Image.Type.Filled; progressFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; progressFill.fillAmount = 0;
        prepareButton = MakeButton(window, "Prepare", "Tarifi Seç", new Vector2(170, 40), new Vector2(130, -235), 18);
        cancelButton = MakeButton(window, "Cancel", "Tarifi İptal Et", new Vector2(145, 40), new Vector2(308, -235), 16);
        Label(window, "Controls", new Vector2(240, 62), new Vector2(-252, -250), "ESC: Kapat", 14);
        status = Label(window, "Status", new Vector2(732, 40), new Vector2(0, -284), "", 16);
        panelRoot.SetActive(false);
    }

    private RectTransform Scroll(Transform parent, string name, Vector2 size, Vector2 position)
    {
        RectTransform root = Rect(parent, name, size, position);
        Image(root, insetSprite, new Color(1, 0.96f, 0.89f, 1), true);
        ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        RectTransform viewport = Rect(root, "Viewport", Vector2.zero, Vector2.zero);
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one; viewport.offsetMin = new Vector2(4, 4); viewport.offsetMax = new Vector2(-4, -4);
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = Rect(viewport, "Content", Vector2.zero, Vector2.zero);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1); content.sizeDelta = Vector2.zero; content.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 8; layout.padding = new RectOffset(4, 4, 4, 4); layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport; scroll.content = content;
        return content;
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = parent.gameObject.layer;
        RectTransform rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); rect.sizeDelta = size; rect.anchoredPosition = position;
        return rect;
    }

    private Image Image(RectTransform rect, Sprite sprite, Color color, bool raycast)
    {
        Image image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite; image.color = color; image.raycastTarget = raycast;
        if (sprite != null) { image.type = UnityEngine.UI.Image.Type.Sliced; image.pixelsPerUnitMultiplier = 2; }
        return image;
    }

    private TMP_Text Label(Transform parent, string name, Vector2 size, Vector2 position, string caption, float sizeInPoints)
    {
        var label = Rect(parent, name, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.text = caption; label.fontSize = sizeInPoints; label.alignment = TextAlignmentOptions.Center;
        label.color = Ink; label.raycastTarget = false; label.richText = false;
        return label;
    }

    private Button MakeButton(Transform parent, string name, string caption, Vector2 size, Vector2 position, float textSize)
    {
        RectTransform rect = Rect(parent, name, size, position);
        Image background = Image(rect, buttonSprite, new Color(0.65f, 0.82f, 0.43f, 1), true);
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = background;
        Label(rect, "Label", size - new Vector2(8, 4), Vector2.zero, caption, textSize).fontStyle = FontStyles.Bold;
        return button;
    }
}

