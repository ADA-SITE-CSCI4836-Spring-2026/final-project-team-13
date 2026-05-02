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
    [SerializeField] private Vector2 backButtonAnchoredPosition = new Vector2(24f, -24f);
    [SerializeField] private Vector2 timerTextAnchoredPosition = new Vector2(0f, -24f);
    [SerializeField] private Vector2 endDecisionButtonAnchoredPosition = new Vector2(-24f, -24f);
    [SerializeField] private Vector2 stopButtonAnchoredPosition = new Vector2(-24f, -68f);
    [SerializeField] private Vector2 continueButtonAnchoredPosition = new Vector2(-140f, -68f);
    [SerializeField] private bool buildOptionsUi = true;
    [SerializeField] private bool moveFocusDetailObject = true;
    [SerializeField] private string focusDetailObjectName = "tabletpen";
    [SerializeField] private Vector3 focusDetailCameraLocalPosition = new Vector3(5.5f, -2f, 13f);
    [SerializeField] private float focusDetailScaleMultiplier = 1.35f;
    [SerializeField] private Vector3 focusDetailFaceCameraEulerOffset = new Vector3(0f, 0f, -113.5f);
    [SerializeField] private bool showStoryTextOnTablet = true;
    [SerializeField] private Vector3 tabletTextLocalPosition = new Vector3(-0.08f, 0.07f, 0.05f);
    [SerializeField] private Vector3 tabletTextLocalEulerAngles = new Vector3(0f, 180f, -90f);
    [SerializeField] private Vector3 tabletTextLocalScale = new Vector3(0.02f, 0.02f, 0.02f);
    [SerializeField] private float tabletTextCharacterSize = 0.12f;
    [SerializeField] private int tabletTextFontSize = 36;
    [SerializeField] private Color tabletTextColor = Color.black;

    private readonly List<FocusTarget> focusTargets = new List<FocusTarget>();
    private Coroutine moveRoutine;
    private Vector3 overviewPosition;
    private Quaternion overviewRotation;
    private float overviewFieldOfView;
    private Transform focusedBed;
    private Transform focusedDetailObject;
    private BedPatientSlot focusedPatientSlot;
    private GameObject optionsPanel;
    private RectTransform backButtonRect;
    private GameObject endDecisionButtonObject;
    private GameObject stopButtonObject;
    private GameObject continueButtonObject;
    private Text timerText;
    private Coroutine detailMoveRoutine;
    private PatientStorySystem storySystem;
    private TextMesh focusedStoryText;
    private readonly Dictionary<Transform, DetailTransformSnapshot> detailSnapshots = new Dictionary<Transform, DetailTransformSnapshot>();

    private struct FocusTarget
    {
        public Transform Root;
        public string Label;
    }

    private struct DetailTransformSnapshot
    {
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
    }

    private void Awake()
    {
        controlledCamera = controlledCamera != null ? controlledCamera : GetComponent<Camera>();
    }

    private void Start()
    {
        storySystem = PatientStorySystem.EnsureExists();
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
        if (focusedBed != null && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
        {
            ReturnToOverview();
        }

        if (focusedBed != null && Input.GetMouseButtonDown(0) && PointerIsOverBackButton())
        {
            ReturnToOverview();
        }

        RefreshFocusedStoryText();
        RefreshStoryHud();
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

    public void Focus(BedFocusTarget bedTarget)
    {
        if (bedTarget == null)
        {
            return;
        }

        focusedPatientSlot = bedTarget.GetComponentInParent<BedPatientSlot>();
        Focus(bedTarget.FocusRoot, bedTarget.DisplayName);
    }

    public void Focus(Transform focusRoot, string label)
    {
        if (focusRoot == null || !TryGetRendererBounds(focusRoot, out var bounds))
        {
            return;
        }

        FocusBed(new FocusTarget
        {
            Root = focusRoot,
            Label = string.IsNullOrWhiteSpace(label) ? focusRoot.name : label
        }, bounds);
    }

    private void FocusBed(FocusTarget target, Bounds bounds)
    {
        focusedBed = target.Root;
        focusedPatientSlot = target.Root.GetComponentInParent<BedPatientSlot>();

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
        MoveDetailObjectToFocusedPose(target.Root, targetPosition, targetRotation);
        ShowOptions(true);
    }

    public void ReturnToOverview()
    {
        SetFocusedStoryTextVisible(false);
        ReturnFocusedDetailObject();
        focusedBed = null;
        focusedPatientSlot = null;
        MoveCamera(overviewPosition, overviewRotation, overviewFieldOfView);
        ShowOptions(false);
    }

    public void ApplyFocusedTreatment(TreatmentType treatment)
    {
        if (focusedPatientSlot == null && focusedBed != null)
        {
            focusedPatientSlot = focusedBed.GetComponentInParent<BedPatientSlot>();
        }

        if (focusedPatientSlot == null)
        {
            Debug.LogWarning("No patient slot found for the focused bed.");
            return;
        }

        focusedPatientSlot.ApplyTreatment(treatment);
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

    private bool PointerIsOverBackButton()
    {
        return backButtonRect != null &&
            optionsPanel != null &&
            optionsPanel.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(backButtonRect, Input.mousePosition);
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

        var backButton = CreateButton(canvasObject.transform, "Back", backButtonAnchoredPosition, ReturnToOverview);
        optionsPanel = backButton.gameObject;
        backButtonRect = optionsPanel.GetComponent<RectTransform>();

        timerText = CreateText(canvasObject.transform, "Time: 30s", 22, FontStyle.Bold);
        timerText.alignment = TextAnchor.MiddleCenter;
        var timerRect = timerText.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 1f);
        timerRect.anchorMax = new Vector2(0.5f, 1f);
        timerRect.pivot = new Vector2(0.5f, 1f);
        timerRect.anchoredPosition = timerTextAnchoredPosition;
        timerRect.sizeDelta = new Vector2(760f, 42f);

        var endDecisionButton = CreateButton(canvasObject.transform, "End", endDecisionButtonAnchoredPosition, EndCurrentDecision);
        endDecisionButtonObject = endDecisionButton.gameObject;
        var endButtonRect = endDecisionButtonObject.GetComponent<RectTransform>();
        endButtonRect.anchorMin = new Vector2(1f, 1f);
        endButtonRect.anchorMax = new Vector2(1f, 1f);
        endButtonRect.pivot = new Vector2(1f, 1f);

        var stopButton = CreateButton(canvasObject.transform, "Stop", stopButtonAnchoredPosition, StopTreatment);
        stopButtonObject = stopButton.gameObject;
        var stopButtonRect = stopButtonObject.GetComponent<RectTransform>();
        stopButtonRect.anchorMin = new Vector2(1f, 1f);
        stopButtonRect.anchorMax = new Vector2(1f, 1f);
        stopButtonRect.pivot = new Vector2(1f, 1f);

        var continueButton = CreateButton(canvasObject.transform, "Continue", continueButtonAnchoredPosition, ContinueTreatment);
        continueButtonObject = continueButton.gameObject;
        var continueButtonRect = continueButtonObject.GetComponent<RectTransform>();
        continueButtonRect.anchorMin = new Vector2(1f, 1f);
        continueButtonRect.anchorMax = new Vector2(1f, 1f);
        continueButtonRect.pivot = new Vector2(1f, 1f);
        continueButtonRect.sizeDelta = new Vector2(120f, 36f);
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
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(104f, 36f);

        var text = CreateText(buttonObject.transform, label, 14, FontStyle.Normal);
        text.alignment = TextAnchor.MiddleCenter;
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private void MoveDetailObjectToFocusedPose(Transform focusRoot, Vector3 cameraTargetPosition, Quaternion cameraTargetRotation)
    {
        if (!moveFocusDetailObject || string.IsNullOrWhiteSpace(focusDetailObjectName))
        {
            return;
        }

        var previousDetailObject = focusedDetailObject;
        var detailObject = FindFocusDetailObject(focusRoot);
        if (previousDetailObject != null && previousDetailObject != detailObject)
        {
            RestoreDetailObject(previousDetailObject, transitionSeconds);
        }

        if (detailObject == null)
        {
            focusedDetailObject = null;
            return;
        }

        focusedDetailObject = detailObject;
        focusedStoryText = EnsureTabletStoryText(detailObject);
        SetFocusedStoryTextVisible(true);
        RefreshFocusedStoryText();

        if (!detailSnapshots.ContainsKey(detailObject))
        {
            detailSnapshots.Add(detailObject, new DetailTransformSnapshot
            {
                LocalPosition = detailObject.localPosition,
                LocalRotation = detailObject.localRotation,
                LocalScale = detailObject.localScale
            });
        }

        var snapshot = detailSnapshots[detailObject];
        var targetPosition = cameraTargetPosition + cameraTargetRotation * focusDetailCameraLocalPosition;
        var targetRotation = Quaternion.LookRotation(cameraTargetPosition - targetPosition, Vector3.up) *
            Quaternion.Euler(focusDetailFaceCameraEulerOffset);
        var targetLocalScale = snapshot.LocalScale * focusDetailScaleMultiplier;

        MoveDetailObject(detailObject, targetPosition, targetRotation, targetLocalScale, false, snapshot, transitionSeconds);
    }

    private void ReturnFocusedDetailObject()
    {
        if (focusedDetailObject == null)
        {
            return;
        }

        RestoreDetailObject(focusedDetailObject, 0f);
        focusedDetailObject = null;
        focusedStoryText = null;
    }

    private void RestoreDetailObject(Transform detailObject, float seconds)
    {
        if (detailObject == null || !detailSnapshots.TryGetValue(detailObject, out var snapshot))
        {
            return;
        }

        var targetPosition = detailObject.parent != null ? detailObject.parent.TransformPoint(snapshot.LocalPosition) : snapshot.LocalPosition;
        var targetRotation = detailObject.parent != null ? detailObject.parent.rotation * snapshot.LocalRotation : snapshot.LocalRotation;

        MoveDetailObject(detailObject, targetPosition, targetRotation, snapshot.LocalScale, true, snapshot, seconds);
    }

    private void MoveDetailObject(
        Transform detailObject,
        Vector3 targetPosition,
        Quaternion targetRotation,
        Vector3 targetLocalScale,
        bool restoreLocalAtEnd,
        DetailTransformSnapshot restoreSnapshot,
        float seconds)
    {
        if (seconds <= 0f)
        {
            if (detailMoveRoutine != null)
            {
                StopCoroutine(detailMoveRoutine);
                detailMoveRoutine = null;
            }

            if (restoreLocalAtEnd)
            {
                detailObject.localPosition = restoreSnapshot.LocalPosition;
                detailObject.localRotation = restoreSnapshot.LocalRotation;
                detailObject.localScale = restoreSnapshot.LocalScale;
            }
            else
            {
                detailObject.position = targetPosition;
                detailObject.rotation = targetRotation;
                detailObject.localScale = targetLocalScale;
            }

            return;
        }

        if (detailMoveRoutine != null)
        {
            StopCoroutine(detailMoveRoutine);
        }

        detailMoveRoutine = StartCoroutine(MoveDetailObjectRoutine(
            detailObject,
            targetPosition,
            targetRotation,
            targetLocalScale,
            restoreLocalAtEnd,
            restoreSnapshot,
            seconds));
    }

    private IEnumerator MoveDetailObjectRoutine(
        Transform detailObject,
        Vector3 targetPosition,
        Quaternion targetRotation,
        Vector3 targetLocalScale,
        bool restoreLocalAtEnd,
        DetailTransformSnapshot restoreSnapshot,
        float seconds)
    {
        var startPosition = detailObject.position;
        var startRotation = detailObject.rotation;
        var startLocalScale = detailObject.localScale;
        var elapsed = 0f;

        while (elapsed < seconds)
        {
            if (detailObject == null)
            {
                detailMoveRoutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            var t = seconds <= 0f ? 1f : Mathf.Clamp01(elapsed / seconds);
            t = t * t * (3f - 2f * t);

            detailObject.position = Vector3.Lerp(startPosition, targetPosition, t);
            detailObject.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            detailObject.localScale = Vector3.Lerp(startLocalScale, targetLocalScale, t);

            yield return null;
        }

        if (restoreLocalAtEnd)
        {
            detailObject.localPosition = restoreSnapshot.LocalPosition;
            detailObject.localRotation = restoreSnapshot.LocalRotation;
            detailObject.localScale = restoreSnapshot.LocalScale;
        }
        else
        {
            detailObject.position = targetPosition;
            detailObject.rotation = targetRotation;
            detailObject.localScale = targetLocalScale;
        }

        detailMoveRoutine = null;
    }

    private Transform FindFocusDetailObject(Transform focusRoot)
    {
        var searchRoot = focusRoot;
        var patientSlot = focusRoot.GetComponentInParent<BedPatientSlot>();
        if (patientSlot != null)
        {
            searchRoot = patientSlot.transform;
        }

        foreach (var child in searchRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == focusDetailObjectName)
            {
                return child;
            }
        }

        return null;
    }

    private TextMesh EnsureTabletStoryText(Transform detailObject)
    {
        if (!showStoryTextOnTablet || detailObject == null)
        {
            return null;
        }

        foreach (var textMesh in detailObject.GetComponentsInChildren<TextMesh>(true))
        {
            if (textMesh.name == "Tablet Story Text")
            {
                return textMesh;
            }
        }

        var textObject = new GameObject("Tablet Story Text");
        textObject.transform.SetParent(detailObject, false);
        textObject.transform.localPosition = tabletTextLocalPosition;
        textObject.transform.localRotation = Quaternion.Euler(tabletTextLocalEulerAngles);
        textObject.transform.localScale = tabletTextLocalScale;

        var text = textObject.AddComponent<TextMesh>();
        text.anchor = TextAnchor.UpperLeft;
        text.alignment = TextAlignment.Left;
        text.characterSize = tabletTextCharacterSize;
        text.fontSize = tabletTextFontSize;
        text.color = tabletTextColor;

        return text;
    }

    private void RefreshFocusedStoryText()
    {
        if (focusedStoryText == null || focusedPatientSlot == null)
        {
            return;
        }

        storySystem = storySystem != null ? storySystem : PatientStorySystem.EnsureExists();
        focusedStoryText.text = storySystem.GetPatientStatus(focusedPatientSlot);
    }

    private void SetFocusedStoryTextVisible(bool visible)
    {
        if (focusedStoryText != null)
        {
            focusedStoryText.gameObject.SetActive(visible);
        }
    }

    private void EndCurrentDecision()
    {
        storySystem = storySystem != null ? storySystem : PatientStorySystem.EnsureExists();
        storySystem.EndCurrentDecision();
        RefreshFocusedStoryText();
        RefreshStoryHud();
    }

    private void StopTreatment()
    {
        storySystem = storySystem != null ? storySystem : PatientStorySystem.EnsureExists();
        storySystem.StopTreatment();
        RefreshFocusedStoryText();
        RefreshStoryHud();
    }

    private void ContinueTreatment()
    {
        storySystem = storySystem != null ? storySystem : PatientStorySystem.EnsureExists();
        storySystem.ContinueTreatment();
        RefreshFocusedStoryText();
        RefreshStoryHud();
    }

    private void RefreshStoryHud()
    {
        if (timerText == null)
        {
            return;
        }

        storySystem = storySystem != null ? storySystem : PatientStorySystem.EnsureExists();
        timerText.text = storySystem.PromptText;

        if (endDecisionButtonObject != null)
        {
            endDecisionButtonObject.SetActive(storySystem.CanEndDecision);
        }

        if (stopButtonObject != null)
        {
            stopButtonObject.SetActive(storySystem.CanStopTreatment);
        }

        if (continueButtonObject != null)
        {
            continueButtonObject.SetActive(storySystem.CanContinueTreatment);
        }
    }
}
