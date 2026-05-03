using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class IntroSceneController : MonoBehaviour
{
    private const string TickingClipPath = "Audio/tictac";
    private const string SkipMainMenuKey = "OneLastSleep.SkipMainMenu";

    [SerializeField] private string[] introLines =
    {
        "Three patients.",
        "Not enough time.",
        "Time is your enemy.",
        "And theirs too.",
        "Your time starts now.",
        "tic... tac... tic... tac..."
    };

    [SerializeField] private float initialDelaySeconds = 0.5f;
    [SerializeField] private float fadeSeconds = 0.85f;
    [SerializeField] private float holdSeconds = 1.25f;
    [SerializeField] private float finalHoldSeconds = 2f;
    [SerializeField] private int fontSize = 46;
    [SerializeField] private Color textColor = new Color(0.9f, 0.9f, 0.86f, 1f);
    [SerializeField] private Color backgroundColor = Color.black;
    [SerializeField] private KeyCode skipKey = KeyCode.Space;

    private CanvasGroup canvasGroup;
    private Text introText;
    private AudioSource tickingSource;
    private GameObject menuCanvasObject;
    private GameObject instructionsPanel;
    private bool introPlaying;
    private bool skipRequested;

    private void Awake()
    {
        ConfigureTickingAudio();
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (PlayerPrefs.GetInt(SkipMainMenuKey, 0) == 1)
        {
            PlayerPrefs.DeleteKey(SkipMainMenuKey);
            StartIntro();
            return;
        }

        BuildMainMenu();
    }

    private void Update()
    {
        if (!introPlaying)
        {
            return;
        }

        if (Input.GetKeyDown(skipKey) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            skipRequested = true;
        }
    }

    public static void RestartAfterMainMenuSkip()
    {
        PlayerPrefs.SetInt(SkipMainMenuKey, 1);
        PlayerPrefs.Save();
    }

    private void StartIntro()
    {
        if (introPlaying)
        {
            return;
        }

        if (menuCanvasObject != null)
        {
            Destroy(menuCanvasObject);
        }

        BackgroundMusic.PlayCurrent();
        BuildOverlay();
        introPlaying = true;
        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        yield return WaitOrSkip(initialDelaySeconds);

        for (var i = 0; i < introLines.Length; i++)
        {
            if (skipRequested)
            {
                break;
            }

            introText.text = introLines[i];

            if (introLines[i].StartsWith("tic") && tickingSource != null && !tickingSource.isPlaying)
            {
                tickingSource.Play();
            }

            yield return FadeText(0f, 1f, fadeSeconds);
            yield return WaitOrSkip(i == introLines.Length - 1 ? finalHoldSeconds : holdSeconds);
            yield return FadeText(1f, 0f, fadeSeconds);
        }

        if (tickingSource != null)
        {
            tickingSource.Stop();
        }

        yield return FadeOverlay(1f, 0f, fadeSeconds);
        PatientStorySystem.EnsureExists().BeginStoryTimer();
        Destroy(gameObject);
    }

    private IEnumerator FadeText(float from, float to, float seconds)
    {
        var color = introText.color;
        var elapsed = 0f;

        while (elapsed < seconds && !skipRequested)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / seconds));
            introText.color = color;
            yield return null;
        }

        color.a = skipRequested ? 0f : to;
        introText.color = color;
    }

    private IEnumerator FadeOverlay(float from, float to, float seconds)
    {
        var elapsed = 0f;

        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / seconds));
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private IEnumerator WaitOrSkip(float seconds)
    {
        var elapsed = 0f;
        while (elapsed < seconds && !skipRequested)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void BuildMainMenu()
    {
        EnsureEventSystem();

        menuCanvasObject = new GameObject("Main Menu Canvas");
        var canvas = menuCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = menuCanvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        menuCanvasObject.AddComponent<GraphicRaycaster>();

        var background = CreatePanel(menuCanvasObject.transform, "Black Background", Color.black);
        Stretch(background.GetComponent<RectTransform>());

        var title = CreateText(menuCanvasObject.transform, "One Last Sleep", 76, FontStyle.Bold);
        title.alignment = TextAnchor.MiddleCenter;
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.18f, 0.62f);
        titleRect.anchorMax = new Vector2(0.82f, 0.82f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        var subtitle = CreateText(menuCanvasObject.transform, "Time is your enemy. Or is it?...", 32, FontStyle.Italic);
        subtitle.alignment = TextAnchor.MiddleCenter;
        var subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.18f, 0.58f);
        subtitleRect.anchorMax = new Vector2(0.82f, 0.66f);
        subtitleRect.offsetMin = Vector2.zero;
        subtitleRect.offsetMax = Vector2.zero;

        PlaceMenuButton(CreateButton(menuCanvasObject.transform, "Start Game", StartIntro), 0f, 0.48f);
        PlaceMenuButton(CreateButton(menuCanvasObject.transform, "Instructions", ShowInstructions), 0f, 0.38f);
        PlaceMenuButton(CreateButton(menuCanvasObject.transform, "Quit", QuitGame), 0f, 0.28f);
        BuildInstructionsPanel();
    }

    private void BuildInstructionsPanel()
    {
        instructionsPanel = CreatePanel(menuCanvasObject.transform, "Instructions Panel", new Color(0f, 0f, 0f, 0.96f));
        Stretch(instructionsPanel.GetComponent<RectTransform>());
        instructionsPanel.SetActive(false);

        var text = CreateText(
            instructionsPanel.transform,
            "Instructions\n\nRead the patient notes and analyze who is getting better or worse.\nPress the red button before time runs out.\nIn the next stages, decide whether to press again or stop based on the patient notes.\nChoose carefully: not every patient can be saved.",
            36,
            FontStyle.Bold);
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.18f, 0.34f);
        textRect.anchorMax = new Vector2(0.82f, 0.78f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        PlaceMenuButton(CreateButton(instructionsPanel.transform, "Back", HideInstructions), 0f, 0.22f);
    }

    private void ShowInstructions()
    {
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(true);
        }
    }

    private void HideInstructions()
    {
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(false);
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BuildOverlay()
    {
        var canvasObject = new GameObject("Intro Canvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        var backgroundObject = CreatePanel(canvasObject.transform, "Black Background", backgroundColor);
        Stretch(backgroundObject.GetComponent<RectTransform>());

        introText = CreateText(canvasObject.transform, string.Empty, fontSize, FontStyle.Bold);
        introText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
        introText.alignment = TextAnchor.MiddleCenter;
        introText.horizontalOverflow = HorizontalWrapMode.Wrap;
        introText.verticalOverflow = VerticalWrapMode.Overflow;

        var textRect = introText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.12f, 0.25f);
        textRect.anchorMax = new Vector2(0.88f, 0.75f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var skipButton = CreateButton(canvasObject.transform, "Skip", RequestSkip);
        var skipRect = skipButton.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(1f, 1f);
        skipRect.anchorMax = new Vector2(1f, 1f);
        skipRect.pivot = new Vector2(1f, 1f);
        skipRect.anchoredPosition = new Vector2(-24f, -24f);
        skipRect.sizeDelta = new Vector2(120f, 48f);
    }

    private void ConfigureTickingAudio()
    {
        tickingSource = gameObject.AddComponent<AudioSource>();
        tickingSource.clip = Resources.Load<AudioClip>(TickingClipPath);
        tickingSource.playOnAwake = false;
        tickingSource.loop = true;
        tickingSource.volume = 0.65f;
        tickingSource.spatialBlend = 0f;
    }

    private void RequestSkip()
    {
        skipRequested = true;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);
        var image = panelObject.AddComponent<Image>();
        image.color = color;
        return panelObject;
    }

    private static Text CreateText(Transform parent, string label, int size, FontStyle style)
    {
        var textObject = new GameObject(label);
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<Text>();
        text.text = label;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.font = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        return text;
    }

    private static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject(label);
        buttonObject.transform.SetParent(parent, false);

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.19f, 0.2f, 0.9f);

        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(action);

        var text = CreateText(buttonObject.transform, label, 28, FontStyle.Bold);
        var textRect = text.GetComponent<RectTransform>();
        Stretch(textRect);

        return button;
    }

    private static void PlaceMenuButton(Button button, float x, float y)
    {
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, y);
        rect.anchorMax = new Vector2(0.5f, y);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(320f, 64f);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
