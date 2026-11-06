using TMPro;
using UnityEngine;

public sealed class FpsDisplay : MonoBehaviour
{
    private static FpsDisplay instance;
    private TMP_Text label;
    private float elapsed;
    private int frames;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateDisplay()
    {
        if (instance != null) return;

        var root = new GameObject("FPS Display", typeof(Canvas));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        DontDestroyOnLoad(root);

        var textObject = new GameObject("FPS", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(root.transform, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-16, -12);
        rect.sizeDelta = new Vector2(160, 36);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 22;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.TopRight;
        text.raycastTarget = false;
        text.color = Color.white;
        text.outlineWidth = 0.18f;
        text.outlineColor = Color.black;
        text.SetText("FPS: ...");

        instance = root.AddComponent<FpsDisplay>();
        instance.label = text;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        frames++;
        if (elapsed < 0.5f) return;
        label.SetText("FPS: {0:0}", frames / elapsed);
        elapsed = 0;
        frames = 0;
    }
}
