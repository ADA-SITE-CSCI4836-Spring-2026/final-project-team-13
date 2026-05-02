using UnityEngine;

public class BedPatientSlot : MonoBehaviour
{
    [SerializeField] private Transform patientMount;

    private PatientTreatmentAnimator treatmentAnimator;

    public Transform PatientMount => patientMount != null ? patientMount : transform;
    public GameObject CurrentPatient => FindCurrentPatient();
    public PatientTreatmentAnimator TreatmentAnimator => treatmentAnimator;

    private void Awake()
    {
        EnsureBedLightController();
        RefreshTreatmentAnimator();
    }

    private void EnsureBedLightController()
    {
        if (GetComponent<BedLightController>() == null)
        {
            gameObject.AddComponent<BedLightController>();
        }
    }

    private GameObject FindCurrentPatient()
    {
        if (treatmentAnimator != null)
        {
            return treatmentAnimator.gameObject;
        }

        var patientAnimator = PatientMount.GetComponentInChildren<Animator>();
        if (patientAnimator != null)
        {
            return patientAnimator.gameObject;
        }

        if (PatientMount.childCount > 0)
        {
            return PatientMount.GetChild(0).gameObject;
        }

        return null;
    }

    private void RefreshTreatmentAnimator()
    {
        treatmentAnimator = PatientMount.GetComponentInChildren<PatientTreatmentAnimator>();
        if (treatmentAnimator != null)
        {
            return;
        }

        var animator = PatientMount.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            treatmentAnimator = animator.GetComponent<PatientTreatmentAnimator>();
            if (treatmentAnimator == null)
            {
                treatmentAnimator = animator.gameObject.AddComponent<PatientTreatmentAnimator>();
            }
        }
    }

    public void ApplyTreatment(TreatmentType treatment)
    {
        if (treatmentAnimator == null)
        {
            RefreshTreatmentAnimator();
        }

        if (treatmentAnimator == null)
        {
            Debug.LogWarning($"No patient treatment animator found on bed '{name}'.");
            return;
        }

        treatmentAnimator.ApplyTreatment(treatment);
    }

    public void ApplyTreatmentByName(string treatmentName)
    {
        if (treatmentAnimator == null)
        {
            RefreshTreatmentAnimator();
        }

        if (treatmentAnimator == null)
        {
            Debug.LogWarning($"No patient treatment animator found on bed '{name}'.");
            return;
        }

        treatmentAnimator.ApplyTreatmentByName(treatmentName);
    }

    public void Inspect()
    {
        ApplyTreatment(TreatmentType.Inspect);
    }

    public void Medication()
    {
        ApplyTreatment(TreatmentType.Medication);
    }

    public void Surgery()
    {
        ApplyTreatment(TreatmentType.Surgery);
    }

    public void Shock()
    {
        ApplyTreatment(TreatmentType.Shock);
    }

    public void Rest()
    {
        ApplyTreatment(TreatmentType.Rest);
    }
}
