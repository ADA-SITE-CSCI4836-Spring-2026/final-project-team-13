using System;
using UnityEngine;

public class PatientTreatmentAnimator : MonoBehaviour
{
    [Serializable]
    private class TreatmentAnimation
    {
        public TreatmentType treatment;
        public string triggerName;
        public string stateName;
    }

    [SerializeField] private Animator animator;
    [SerializeField] private float crossFadeSeconds = 0.15f;
    [SerializeField] private TreatmentAnimation[] treatmentAnimations =
    {
        new TreatmentAnimation { treatment = TreatmentType.Idle, stateName = "Idle" },
        new TreatmentAnimation { treatment = TreatmentType.Inspect, triggerName = "Inspect" },
        new TreatmentAnimation { treatment = TreatmentType.Medication, triggerName = "Medication" },
        new TreatmentAnimation { treatment = TreatmentType.Surgery, triggerName = "Surgery" },
        new TreatmentAnimation { treatment = TreatmentType.Shock, triggerName = "Shock" },
        new TreatmentAnimation { treatment = TreatmentType.Rest, triggerName = "Rest" }
    };

    public Animator Animator => animator;

    private void Awake()
    {
        animator = animator != null ? animator : GetComponentInChildren<Animator>();
    }

    public void ApplyTreatment(TreatmentType treatment)
    {
        var mapping = FindMapping(treatment);
        if (mapping == null)
        {
            Debug.LogWarning($"No animation mapping configured for treatment '{treatment}' on '{name}'.");
            return;
        }

        PlayMapping(mapping);
    }

    public void ApplyTreatmentByName(string treatmentName)
    {
        if (Enum.TryParse(treatmentName, true, out TreatmentType treatment))
        {
            ApplyTreatment(treatment);
            return;
        }

        PlayTriggerOrState(treatmentName);
    }

    public void PlayTriggerOrState(string triggerOrStateName)
    {
        if (string.IsNullOrWhiteSpace(triggerOrStateName))
        {
            return;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            Debug.LogWarning($"No Animator found on patient '{name}'.");
            return;
        }

        if (HasParameter(triggerOrStateName, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(triggerOrStateName);
        }
        else
        {
            animator.CrossFadeInFixedTime(triggerOrStateName, crossFadeSeconds);
        }
    }

    private TreatmentAnimation FindMapping(TreatmentType treatment)
    {
        foreach (var mapping in treatmentAnimations)
        {
            if (mapping != null && mapping.treatment == treatment)
            {
                return mapping;
            }
        }

        return null;
    }

    private void PlayMapping(TreatmentAnimation mapping)
    {
        if (!string.IsNullOrWhiteSpace(mapping.triggerName))
        {
            PlayTriggerOrState(mapping.triggerName);
            return;
        }

        if (!string.IsNullOrWhiteSpace(mapping.stateName))
        {
            PlayTriggerOrState(mapping.stateName);
        }
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        foreach (var parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }
}
