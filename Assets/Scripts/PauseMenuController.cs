using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private GameObject canvasObject;
    private BedFocusCameraController focusController;
    private bool isPaused;

    private void Awake()
    {
        focusController = GetComponent<BedFocusCameraController>();
        BuildCanvas();
        SetVisible(false);
    }

    private void Update()
    {
        if (GameObject.Find("Game Result Canvas") != null || GameObject.Find("Main Menu Canvas") != null || GameObject.Find("Intro Canvas") != null)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.P) && !Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (isPaused)
        {
            Resume();
            return;
        }

        if (focusController != null &&
            Input.GetKeyDown(KeyCode.Escape) &&
            (!focusController.CanFocusFromWorldClick || focusController.LastOverviewReturnFrame == Time.frameCount))
        {
            return;
        }

        Pause();
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        SetVisible(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SetVisible(false);
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        IntroSceneController.RestartAfterMainMenuSkip();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void BuildCanvas()
    {
        EnsureEventSystem();

        canvasObject = new GameObject("Pause Menu Canvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1500;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObject.AddComponent<CanvasGroup>();

        var backgroundObject = new GameObject("Pause Background");
        backgroundObject.transform.SetParent(canvasObject.transform, false);
        var background = backgroundObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.88f);
        Stretch(background.GetComponent<RectTransform>());

        var titleObject = new GameObject("Paused");
        titleObject.transform.SetParent(canvasObject.transform, false);
        var title = titleObject.AddComponent<Text>();
        title.text = "Paused";
        title.font = GetFont();
        title.fontSize = 64;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.3f, 0.63f);
        titleRect.anchorMax = new Vector2(0.7f, 0.76f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        PlaceButton(CreateButton(canvasObject.transform, "Resume", Resume), 0.48f);
        PlaceButton(CreateButton(canvasObject.transform, "Restart", Restart), 0.38f);
        PlaceButton(CreateButton(canvasObject.transform, "Main Menu", MainMenu), 0.28f);
    }

    private void SetVisible(bool visible)
    {
        if (canvasObject == null)
        {
            return;
        }

        canvasObject.SetActive(visible);
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }
    }

    private static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject(label);
        buttonObject.transform.SetParent(parent, false);

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.22f, 0.24f, 0.95f);

        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(action);

        var textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);
        var text = textObject.AddComponent<Text>();
        text.text = label;
        text.font = GetFont();
        text.fontSize = 28;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        Stretch(text.GetComponent<RectTransform>());

        return button;
    }

    private static void PlaceButton(Button button, float y)
    {
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, y);
        rect.anchorMax = new Vector2(0.5f, y);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(250f, 64f);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Font GetFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}
