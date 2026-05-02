using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class BedFocusTarget : MonoBehaviour
{
    [SerializeField] private Transform focusRoot;
    [SerializeField] private string displayName;
    [SerializeField] private bool installCameraController = true;

    private Interactable interactable;

    public Transform FocusRoot => focusRoot != null ? focusRoot : transform;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;

    private void Awake()
    {
        ConfigureInteractable();
        EnsureCameraControllers();
    }

    private void Start()
    {
        EnsureCameraControllers();
    }

    public void Focus()
    {
        var focusController = EnsureCameraControllers();
        if (focusController != null)
        {
            focusController.Focus(this);
        }
    }

    private void ConfigureInteractable()
    {
        interactable = GetComponent<Interactable>();
        if (interactable == null)
        {
            interactable = gameObject.AddComponent<Interactable>();
        }

        interactable.Clicked.RemoveListener(Focus);
        interactable.Clicked.AddListener(Focus);
    }

    private BedFocusCameraController EnsureCameraControllers()
    {
        if (!installCameraController)
        {
            return null;
        }

        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return null;
        }

        var focusController = mainCamera.GetComponent<BedFocusCameraController>();
        if (focusController == null)
        {
            focusController = mainCamera.gameObject.AddComponent<BedFocusCameraController>();
        }

        if (mainCamera.GetComponent<InteractionCursor>() == null)
        {
            mainCamera.gameObject.AddComponent<InteractionCursor>();
        }

        return focusController;
    }
}
