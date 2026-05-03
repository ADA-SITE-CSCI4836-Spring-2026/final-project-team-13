using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class InteractableGlow : MonoBehaviour
{
    private static readonly List<InteractableGlow> ActiveGlows = new List<InteractableGlow>();

    [SerializeField] private Color glowColor = new Color(1f, 0.35f, 0f, 1f);
    [SerializeField, Range(0f, 10f)] private float outlineWidth = 1f;
    [SerializeField] private bool hideWhileBedFocused;

    private readonly List<GameObject> outlines = new List<GameObject>();
    private Material maskMaterial;
    private Material outlineMaterial;
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
        DestroyMaterial(maskMaterial);
        DestroyMaterial(outlineMaterial);
    }

    private void LateUpdate()
    {
        RefreshMaterialProperties();
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
        var shouldShow = interactable != null && interactable.IsHovered && IsGameplayOverviewActive();
        SetVisible(shouldShow);
    }

    private static bool IsGameplayOverviewActive()
    {
        if (GameObject.Find("Game Result Canvas") != null ||
            GameObject.Find("Main Menu Canvas") != null ||
            GameObject.Find("Intro Canvas") != null ||
            GameObject.Find("Pause Menu Canvas") != null)
        {
            return false;
        }

        var camera = Camera.main;
        if (camera == null)
        {
            return true;
        }

        var controller = camera.GetComponent<BedFocusCameraController>();
        return controller == null || controller.CanFocusFromWorldClick;
    }

    private void BuildOutlines()
    {
        var maskShader = LoadOutlineShader("Shaders/InteractableOutlineMask", "Custom/InteractableOutlineMask");
        var outlineShader = LoadOutlineShader("Shaders/InteractableOutline", "Custom/InteractableOutline");
        if (maskShader == null || outlineShader == null)
        {
            return;
        }

        maskMaterial = new Material(maskShader);
        maskMaterial.renderQueue = 2999;

        outlineMaterial = CreateGlowMaterial(outlineShader);

        var renderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (var sourceRenderer in renderers)
        {
            if (sourceRenderer.GetComponentInParent<InteractableGlow>() != this)
            {
                continue;
            }

            var sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                continue;
            }

            CreateOutlineObject(sourceRenderer, sourceFilter.sharedMesh, " Outline Mask", maskMaterial);
            CreateOutlineObject(sourceRenderer, sourceFilter.sharedMesh, " Outline", outlineMaterial);
        }
    }

    private static Shader LoadOutlineShader(string resourcesPath, string shaderName)
    {
        var shader = Resources.Load<Shader>(resourcesPath);
        return shader != null ? shader : Shader.Find(shaderName);
    }

    private void CreateOutlineObject(MeshRenderer sourceRenderer, Mesh mesh, string suffix, Material material)
    {
        var outlineObject = new GameObject(sourceRenderer.gameObject.name + suffix);
        outlineObject.transform.SetParent(sourceRenderer.transform, false);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;

        var filter = outlineObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        var renderer = outlineObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = CreateMaterialArray(material, mesh.subMeshCount);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        outlines.Add(outlineObject);
    }

    private Material CreateGlowMaterial(Shader shader)
    {
        var material = new Material(shader);
        ApplyGlowMaterialProperties(material);
        material.renderQueue = 3000;
        return material;
    }

    private void RefreshMaterialProperties()
    {
        if (outlineMaterial != null)
        {
            ApplyGlowMaterialProperties(outlineMaterial);
        }
    }

    private void ApplyGlowMaterialProperties(Material material)
    {
        if (material == null)
        {
            return;
        }

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
            material.SetFloat("_OutlineWidth", outlineWidth);
        }
    }

    private static Material[] CreateMaterialArray(Material material, int count)
    {
        var materialCount = Mathf.Max(1, count);
        var materials = new Material[materialCount];
        for (var i = 0; i < materials.Length; i++)
        {
            materials[i] = material;
        }

        return materials;
    }

    private static void DestroyMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(material);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(material);
        }
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
