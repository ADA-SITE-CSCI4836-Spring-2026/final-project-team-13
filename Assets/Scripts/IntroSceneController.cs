using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class IntroSceneController : MonoBehaviour
{
    private const string TickingClipPath = "Audio/tictac";

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
    private bool skipRequested;

    private void Awake()
    {
        BuildOverlay();
        ConfigureTickingAudio();
    }

    private void Start()
    {
        StartCoroutine(PlayIntro());
    }

    private void Update()
    {
        if (Input.GetKeyDown(skipKey) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            skipRequested = true;
        }
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

        yield return FadeOverlay(1f, 0f, fadeSeconds);
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
        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        var backgroundObject = new GameObject("Black Background");
        backgroundObject.transform.SetParent(canvasObject.transform, false);
        var background = backgroundObject.AddComponent<Image>();
        background.color = backgroundColor;

        var backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        var textObject = new GameObject("Intro Text");
        textObject.transform.SetParent(canvasObject.transform, false);
        introText = textObject.AddComponent<Text>();
        introText.text = string.Empty;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        introText.font = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        introText.fontSize = fontSize;
        introText.fontStyle = FontStyle.Bold;
        introText.alignment = TextAnchor.MiddleCenter;
        introText.horizontalOverflow = HorizontalWrapMode.Wrap;
        introText.verticalOverflow = VerticalWrapMode.Overflow;
        introText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);

        var textRect = introText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.12f, 0.25f);
        textRect.anchorMax = new Vector2(0.88f, 0.75f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
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
}
