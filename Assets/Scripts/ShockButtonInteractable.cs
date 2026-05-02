using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class ShockButtonInteractable : MonoBehaviour
{
    [SerializeField] private BedPatientSlot targetBed;
    [SerializeField] private bool useFocusedBedWhenNoTarget = true;

    private Interactable interactable;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        interactable.Clicked.RemoveListener(ShockPatient);
        interactable.Clicked.AddListener(ShockPatient);
    }

    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.Clicked.RemoveListener(ShockPatient);
        }
    }

    public void ShockPatient()
    {
        var bed = targetBed != null ? targetBed : GetComponentInParent<BedPatientSlot>();
        if (bed != null)
        {
            bed.ApplyTreatment(TreatmentType.Shock);
            return;
        }

        if (!useFocusedBedWhenNoTarget)
        {
            Debug.LogWarning($"Shock button '{name}' has no target bed assigned.");
            return;
        }

        var focusController = FindObjectOfType<BedFocusCameraController>();
        if (focusController == null)
        {
            Debug.LogWarning($"Shock button '{name}' could not find a bed focus controller.");
            return;
        }

        focusController.ApplyFocusedTreatment(TreatmentType.Shock);
    }
}
