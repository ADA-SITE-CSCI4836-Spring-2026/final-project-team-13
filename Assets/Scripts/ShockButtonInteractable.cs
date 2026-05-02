using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class ShockButtonInteractable : MonoBehaviour
{
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
        PatientStorySystem.EnsureExists().RegisterGlobalShock();
    }
}
