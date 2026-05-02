using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BedLightController : MonoBehaviour
{
    private static readonly List<BedLightController> Controllers = new List<BedLightController>();
    private static BedLightController focusedController;

    [SerializeField, Range(0f, 1f)] private float dimMultiplier = 0.35f;
    [SerializeField] private float flickerDurationSeconds = 0.85f;
    [SerializeField] private float flickerIntervalSeconds = 0.055f;
    [SerializeField] private Vector2 flickerIntensityMultiplier = new Vector2(0.05f, 1.25f);

    private readonly List<LightState> lightStates = new List<LightState>();
    private Coroutine flickerRoutine;

    private struct LightState
    {
        public Light Light;
        public float OriginalIntensity;
    }

    private void Awake()
    {
        CacheLights();
        Register();
        ApplyFocusBrightness();
    }

    private void OnEnable()
    {
        Register();
        ApplyFocusBrightness();
    }

    private void OnDisable()
    {
        Controllers.Remove(this);
        if (focusedController == this)
        {
            focusedController = null;
        }
    }

    private void OnDestroy()
    {
        Controllers.Remove(this);
        if (focusedController == this)
        {
            focusedController = null;
        }
    }

    public static void SetFocusedBed(BedPatientSlot focusedBed)
    {
        focusedController = focusedBed != null ? focusedBed.GetComponent<BedLightController>() : null;

        foreach (var controller in Controllers)
        {
            if (controller != null)
            {
                controller.ApplyFocusBrightness();
            }
        }
    }

    public static void FlickerAllBeds()
    {
        foreach (var controller in Controllers)
        {
            if (controller != null)
            {
                controller.PlayFlicker();
            }
        }
    }

    public void PlayFlicker()
    {
        if (lightStates.Count == 0)
        {
            CacheLights();
        }

        if (flickerRoutine != null)
        {
            StopCoroutine(flickerRoutine);
        }

        flickerRoutine = StartCoroutine(FlickerRoutine());
    }

    private IEnumerator FlickerRoutine()
    {
        var elapsed = 0f;

        while (elapsed < flickerDurationSeconds)
        {
            var minMultiplier = Mathf.Min(flickerIntensityMultiplier.x, flickerIntensityMultiplier.y);
            var maxMultiplier = Mathf.Max(flickerIntensityMultiplier.x, flickerIntensityMultiplier.y);

            for (var i = 0; i < lightStates.Count; i++)
            {
                var state = lightStates[i];
                if (state.Light != null)
                {
                    state.Light.intensity = state.OriginalIntensity * GetFocusMultiplier() * Random.Range(minMultiplier, maxMultiplier);
                }
            }

            elapsed += flickerIntervalSeconds;
            yield return new WaitForSeconds(flickerIntervalSeconds);
        }

        flickerRoutine = null;
        ApplyFocusBrightness();
    }

    private void ApplyFocusBrightness()
    {
        if (flickerRoutine != null)
        {
            return;
        }

        var targetMultiplier = GetFocusMultiplier();
        for (var i = 0; i < lightStates.Count; i++)
        {
            var state = lightStates[i];
            if (state.Light != null)
            {
                state.Light.intensity = state.OriginalIntensity * targetMultiplier;
            }
        }
    }

    private float GetFocusMultiplier()
    {
        return focusedController == this ? 1f : dimMultiplier;
    }

    private void CacheLights()
    {
        lightStates.Clear();

        var lights = GetComponentsInChildren<Light>(true);
        foreach (var bedLight in lights)
        {
            if (bedLight == null)
            {
                continue;
            }

            lightStates.Add(new LightState
            {
                Light = bedLight,
                OriginalIntensity = bedLight.intensity
            });
        }
    }

    private void Register()
    {
        if (!Controllers.Contains(this))
        {
            Controllers.Add(this);
        }
    }
}
