using UnityEngine;

public class BedPatientSlot : MonoBehaviour
{
    [SerializeField] private Transform patientMount;
    [SerializeField] private Object defaultPatientPrefab;
    [SerializeField] private bool spawnDefaultPatientOnStart = true;
    [SerializeField] private Vector3 patientLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 patientLocalEulerAngles = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 patientLocalScale = Vector3.one;

    private GameObject currentPatient;
    private PatientTreatmentAnimator treatmentAnimator;

    public Transform PatientMount => patientMount != null ? patientMount : transform;
    public GameObject CurrentPatient => currentPatient;
    public PatientTreatmentAnimator TreatmentAnimator => treatmentAnimator;

    private void Start()
    {
        if (spawnDefaultPatientOnStart && currentPatient == null && defaultPatientPrefab != null)
        {
            SpawnPatient(defaultPatientPrefab);
        }
    }

    public GameObject SpawnPatient(Object patientPrefab)
    {
        var prefabGameObject = ResolvePatientPrefab(patientPrefab);
        if (prefabGameObject == null)
        {
            Debug.LogWarning($"Cannot spawn patient on '{name}'. Assign a patient prefab GameObject or component.");
            return null;
        }

        ClearPatient();

        currentPatient = (GameObject)Instantiate(prefabGameObject, PatientMount, false);
        currentPatient.transform.localPosition = patientLocalPosition;
        currentPatient.transform.localRotation = Quaternion.Euler(patientLocalEulerAngles);
        currentPatient.transform.localScale = patientLocalScale;

        treatmentAnimator = currentPatient.GetComponentInChildren<PatientTreatmentAnimator>();
        if (treatmentAnimator == null)
        {
            treatmentAnimator = currentPatient.AddComponent<PatientTreatmentAnimator>();
        }

        return currentPatient;
    }

    private static GameObject ResolvePatientPrefab(Object patientPrefab)
    {
        if (patientPrefab == null)
        {
            return null;
        }

        if (patientPrefab is GameObject prefabGameObject)
        {
            return prefabGameObject;
        }

        if (patientPrefab is Component component)
        {
            return component.gameObject;
        }

        return null;
    }

    public void ClearPatient()
    {
        if (currentPatient == null)
        {
            treatmentAnimator = null;
            return;
        }

        Destroy(currentPatient);
        currentPatient = null;
        treatmentAnimator = null;
    }

    public void ApplyTreatment(TreatmentType treatment)
    {
        if (treatmentAnimator == null)
        {
            treatmentAnimator = GetComponentInChildren<PatientTreatmentAnimator>();
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
            treatmentAnimator = GetComponentInChildren<PatientTreatmentAnimator>();
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
