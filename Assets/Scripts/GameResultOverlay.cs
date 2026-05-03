using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameResultOverlay
{
    public static void ShowWin()
    {
        Show("You Won!", "You saved 2 people.");
    }

    public static void ShowLoss()
    {
        Show("You Lost!", "The final shock killed them all.");
    }

    private static void Show(string title, string subtitle)
    {
        StopGameActivity();

        var existing = GameObject.Find("Game Result Canvas");
        if (existing != null)
        {
            Object.Destroy(existing);
        }

        var canvasObject = new GameObject("Game Result Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        var backgroundObject = new GameObject("Result Background");
        backgroundObject.transform.SetParent(canvasObject.transform, false);
        var background = backgroundObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.96f);

        var backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        var textObject = new GameObject("Result Text");
        textObject.transform.SetParent(canvasObject.transform, false);
        var text = textObject.AddComponent<Text>();
        text.text = $"{title}\n\n{subtitle}\n\nTime is not your enemy.\nIt is the only thing you never controlled.";
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.font = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 58;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;

        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.12f, 0.34f);
        textRect.anchorMax = new Vector2(0.88f, 0.82f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var mainMenuButton = CreateButton(canvasObject.transform, "Main Menu", ReturnToMainMenu);
        var mainMenuRect = mainMenuButton.GetComponent<RectTransform>();
        mainMenuRect.anchorMin = new Vector2(0.5f, 0.24f);
        mainMenuRect.anchorMax = new Vector2(0.5f, 0.24f);
        mainMenuRect.pivot = new Vector2(0.5f, 0.5f);
        mainMenuRect.anchoredPosition = Vector2.zero;
        mainMenuRect.sizeDelta = new Vector2(220f, 58f);

        var restartButton = CreateButton(canvasObject.transform, "Restart", RestartScene);
        var restartRect = restartButton.GetComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(0.5f, 0.16f);
        restartRect.anchorMax = new Vector2(0.5f, 0.16f);
        restartRect.pivot = new Vector2(0.5f, 0.5f);
        restartRect.anchoredPosition = Vector2.zero;
        restartRect.sizeDelta = new Vector2(220f, 58f);
    }

    private static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject(label);
        buttonObject.transform.SetParent(parent, false);

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.22f, 0.24f, 0.26f, 0.95f);

        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(action);

        var textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);
        var text = textObject.AddComponent<Text>();
        text.text = label;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.font = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 28;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static void RestartScene()
    {
        Time.timeScale = 1f;
        IntroSceneController.RestartAfterMainMenuSkip();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private static void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private static void StopGameActivity()
    {
        BackgroundMusic.StopCurrent();
        InteractableGlow.ClearAll();
        BedLightController.SetFocusedBed(null);

        foreach (var audioSource in Object.FindObjectsOfType<AudioSource>())
        {
            audioSource.Stop();
        }

        foreach (var interactable in Object.FindObjectsOfType<Interactable>())
        {
            interactable.SetHovered(false);
            interactable.enabled = false;
        }

        foreach (var pauseMenu in Object.FindObjectsOfType<PauseMenuController>())
        {
            pauseMenu.enabled = false;
        }

        Time.timeScale = 0f;
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
