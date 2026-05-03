using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class InteractableGlow : MonoBehaviour
{
    private static readonly List<InteractableGlow> ActiveGlows = new List<InteractableGlow>();

    [SerializeField] private Color glowColor = Color.white;
    [SerializeField] private float outlineScale = 1.04f;
    [SerializeField] private bool hideWhileBedFocused;

    private readonly List<GameObject> outlines = new List<GameObject>();
    private Interactable interactable;

    public bool HideWhileBedFocused
    {
        get => hideWhileBedFocused;
        set
        {
            hideWhileBedFocused = value;
            RefreshVisibility();
        }
    }

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        BuildOutlines();
        SetVisible(false);
    }

    private void OnEnable()
    {
        if (!ActiveGlows.Contains(this))
        {
            ActiveGlows.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveGlows.Remove(this);
        SetVisible(false);
    }

    private void OnDestroy()
    {
        ActiveGlows.Remove(this);
    }

    private void LateUpdate()
    {
        RefreshVisibility();
    }

    public static void ClearAll()
    {
        foreach (var glow in ActiveGlows.ToArray())
        {
            if (glow != null)
            {
                glow.ClearGlow();
            }
        }
    }

    public void ClearGlow()
    {
        if (interactable != null)
        {
            interactable.SetHovered(false);
        }

        SetVisible(false);
    }

    private void RefreshVisibility()
    {
        var shouldShow = interactable != null && interactable.IsHovered;
        if (shouldShow && hideWhileBedFocused && IsAnyBedFocused())
        {
            shouldShow = false;
        }

        SetVisible(shouldShow);
    }

    private static bool IsAnyBedFocused()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            return false;
        }

        var controller = camera.GetComponent<BedFocusCameraController>();
        return controller != null && !controller.CanFocusFromWorldClick;
    }

    private void BuildOutlines()
    {
        var shader = Shader.Find("Custom/InteractableOutline");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        var renderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (var sourceRenderer in renderers)
        {
            var sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                continue;
            }

            var outlineObject = new GameObject(sourceRenderer.gameObject.name + " White Glow");
            outlineObject.transform.SetParent(sourceRenderer.transform, false);
            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one * outlineScale;

            var filter = outlineObject.AddComponent<MeshFilter>();
            filter.sharedMesh = sourceFilter.sharedMesh;

            var renderer = outlineObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateGlowMaterial(shader);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            outlines.Add(outlineObject);
        }
    }

    private Material CreateGlowMaterial(Shader shader)
    {
        var material = new Material(shader);
        if (material.HasProperty("_OutlineColor"))
        {
            material.SetColor("_OutlineColor", glowColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", glowColor);
        }

        if (material.HasProperty("_OutlineWidth"))
        {
            material.SetFloat("_OutlineWidth", 0.035f);
        }

        material.renderQueue = 3000;
        return material;
    }

    private void SetVisible(bool visible)
    {
        foreach (var outline in outlines)
        {
            if (outline != null && outline.activeSelf != visible)
            {
                outline.SetActive(visible);
            }
        }
    }
}
