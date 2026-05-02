using UnityEngine;

public class BedFocusTarget : MonoBehaviour
{
    [SerializeField] private Transform focusRoot;
    [SerializeField] private string displayName;
    [SerializeField] private bool installCameraController = true;

    public Transform FocusRoot => focusRoot != null ? focusRoot : transform;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;

    private void Awake()
    {
        EnsureCameraController();
    }

    private void Start()
    {
        EnsureCameraController();
    }

    private void EnsureCameraController()
    {
        if (!installCameraController)
        {
            return;
        }

        var mainCamera = Camera.main;
        if (mainCamera == null || mainCamera.GetComponent<BedFocusCameraController>() != null)
        {
            return;
        }

        mainCamera.gameObject.AddComponent<BedFocusCameraController>();
    }
}
