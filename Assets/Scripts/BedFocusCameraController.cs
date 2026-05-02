using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BedFocusCameraController : MonoBehaviour
{
    [SerializeField] private Camera controlledCamera;
    [SerializeField] private string bedNamePrefix = "Bed";
    [SerializeField] private float transitionSeconds = 0.75f;
    [SerializeField] private float focusDistance = 30f;
    [SerializeField] private float focusHeight = 50f;
    [SerializeField] private float focusWorldZOffset = -10f;
    [SerializeField] private float focusFieldOfView = 38f;
    [SerializeField] private Vector2 optionsPanelAnchoredPosition = new Vector2(-24f, 24f);
    [SerializeField] private bool buildOptionsUi = true;

    private readonly List<FocusTarget> focusTargets = new List<FocusTarget>();
    private Coroutine moveRoutine;
    private Vector3 overviewPosition;
    private Quaternion overviewRotation;
    private float overviewFieldOfView;
    private Transform focusedBed;
    private GameObject optionsPanel;
    private Text optionsTitle;

    private struct FocusTarget
    {
        public Transform Root;
        public string Label;
    }

    private void Awake()
    {
        controlledCamera = controlledCamera != null ? controlledCamera : GetComponent<Camera>();
    }

    private void Start()
    {
        CacheOverviewPose();
        RefreshBeds();

        if (buildOptionsUi)
        {
            EnsureEventSystem();
            BuildOptionsUi();
            ShowOptions(false);
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !PointerIsOverUi())
        {
            TryFocusClickedBed();
        }

        if (focusedBed != null && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
        {
            ReturnToOverview();
        }
    }

    private void CacheOverviewPose()
    {
        if (controlledCamera == null)
        {
            return;
        }

        overviewPosition = controlledCamera.transform.position;
        overviewRotation = controlledCamera.transform.rotation;
        overviewFieldOfView = controlledCamera.fieldOfView;
    }

    private void RefreshBeds()
    {
        focusTargets.Clear();

        var bedTargets = FindObjectsOfType<BedFocusTarget>();
        foreach (var bedTarget in bedTargets)
        {
            var focusRoot = bedTarget.FocusRoot;
            if (focusRoot != null && TryGetRendererBounds(focusRoot, out _))
            {
                focusTargets.Add(new FocusTarget
                {
                    Root = focusRoot,
                    Label = bedTarget.DisplayName
                });
            }
        }

        if (focusTargets.Count > 0)
        {
            return;
        }

        var sceneObjects = FindObjectsOfType<Transform>();

        foreach (var sceneObject in sceneObjects)
        {
            if (!sceneObject.name.StartsWith(bedNamePrefix))
            {
                continue;
            }

            if (sceneObject.GetComponentInParent<Renderer>() != null &&
                sceneObject.parent != null &&
                sceneObject.parent.name.StartsWith(bedNamePrefix))
            {
                continue;
            }

            if (TryGetRendererBounds(sceneObject, out _))
            {
                focusTargets.Add(new FocusTarget
                {
                    Root = sceneObject,
                    Label = sceneObject.name
                });
            }
        }
    }

    private void TryFocusClickedBed()
    {
        if (controlledCamera == null)
        {
            return;
        }

        if (focusTargets.Count == 0)
        {
            RefreshBeds();
        }

        var ray = controlledCamera.ScreenPointToRay(Input.mousePosition);
        var nearestDistance = float.PositiveInfinity;
        FocusTarget nearestTarget = default;
        Bounds nearestBounds = default;

        foreach (var focusTarget in focusTargets)
        {
            var bedRoot = focusTarget.Root;
            if (bedRoot == null || !TryGetRendererBounds(bedRoot, out var bounds))
            {
                continue;
            }

            if (!bounds.IntersectRay(ray, out var distance) || distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            nearestTarget = focusTarget;
            nearestBounds = bounds;
        }

        if (nearestTarget.Root != null)
        {
            FocusBed(nearestTarget, nearestBounds);
        }
    }

    private void FocusBed(FocusTarget target, Bounds bounds)
    {
        focusedBed = target.Root;

        var horizontalForward = Vector3.ProjectOnPlane(overviewRotation * Vector3.forward, Vector3.up).normalized;
        if (horizontalForward.sqrMagnitude < 0.01f)
        {
            horizontalForward = Vector3.forward;
        }

        var lookTarget = bounds.center + Vector3.up * Mathf.Min(bounds.extents.y * 0.35f, 3f);
        var basePosition = bounds.center - horizontalForward * focusDistance + Vector3.up * focusHeight;
        var targetPosition = basePosition + Vector3.forward * focusWorldZOffset;
        var targetRotation = Quaternion.LookRotation(lookTarget - basePosition, Vector3.up);

        MoveCamera(targetPosition, targetRotation, focusFieldOfView);
        SetOptionsTitle(target.Label);
        ShowOptions(true);
    }

    public void ReturnToOverview()
    {
        focusedBed = null;
        MoveCamera(overviewPosition, overviewRotation, overviewFieldOfView);
        ShowOptions(false);
    }

    private void MoveCamera(Vector3 targetPosition, Quaternion targetRotation, float targetFieldOfView)
    {
        if (controlledCamera == null)
        {
            return;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveCameraRoutine(targetPosition, targetRotation, targetFieldOfView));
    }

    private IEnumerator MoveCameraRoutine(Vector3 targetPosition, Quaternion targetRotation, float targetFieldOfView)
    {
        var cameraTransform = controlledCamera.transform;
        var startPosition = cameraTransform.position;
        var startRotation = cameraTransform.rotation;
        var startFieldOfView = controlledCamera.fieldOfView;
        var elapsed = 0f;

        while (elapsed < transitionSeconds)
        {
            elapsed += Time.deltaTime;
            var t = transitionSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / transitionSeconds);
            t = t * t * (3f - 2f * t);

            cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            cameraTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            controlledCamera.fieldOfView = Mathf.Lerp(startFieldOfView, targetFieldOfView, t);

            yield return null;
        }

        cameraTransform.position = targetPosition;
        cameraTransform.rotation = targetRotation;
        controlledCamera.fieldOfView = targetFieldOfView;
        moveRoutine = null;
    }

    private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        bounds = default;

        if (renderers.Length == 0)
        {
            return false;
        }

        bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    private bool PointerIsOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void BuildOptionsUi()
    {
        if (optionsPanel != null)
        {
            return;
        }

        var canvasObject = new GameObject("Bed Options Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        optionsPanel = new GameObject("Bed Options Panel");
        optionsPanel.transform.SetParent(canvasObject.transform, false);

        var image = optionsPanel.AddComponent<Image>();
        image.color = new Color(0.08f, 0.09f, 0.1f, 0.88f);

        var rect = optionsPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = optionsPanelAnchoredPosition;
        rect.sizeDelta = new Vector2(260f, 190f);

        optionsTitle = CreateText(optionsPanel.transform, "Selected Bed", 18, FontStyle.Bold);
        var titleRect = optionsTitle.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -16f);
        titleRect.sizeDelta = new Vector2(-28f, 32f);

        CreateButton(optionsPanel.transform, "View Patient", new Vector2(0f, -64f), () => Debug.Log("View Patient option clicked."));
        CreateButton(optionsPanel.transform, "Assign Treatment", new Vector2(0f, -106f), () => Debug.Log("Assign Treatment option clicked."));
        CreateButton(optionsPanel.transform, "Back", new Vector2(0f, -148f), ReturnToOverview);
    }

    private void SetOptionsTitle(string bedName)
    {
        if (optionsTitle != null)
        {
            optionsTitle.text = bedName;
        }
    }

    private void ShowOptions(bool visible)
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(visible);
        }
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
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;

        return text;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject(label);
        buttonObject.transform.SetParent(parent, false);

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.22f, 0.25f, 0.28f, 0.95f);

        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(action);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(-28f, 34f);

        var text = CreateText(buttonObject.transform, label, 14, FontStyle.Normal);
        text.alignment = TextAnchor.MiddleCenter;
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }
}
