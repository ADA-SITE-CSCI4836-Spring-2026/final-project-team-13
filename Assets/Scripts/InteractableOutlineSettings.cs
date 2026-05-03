using UnityEngine;

[CreateAssetMenu(menuName = "Our Last Sleep/Interactable Outline Settings")]
public sealed class InteractableOutlineSettings : ScriptableObject
{
    private const float DefaultOutlineWidth = 1.5f;
    private const string ResourcesPath = "InteractableOutlineSettings";

    private static InteractableOutlineSettings loadedSettings;

    [SerializeField, Range(0f, 10f)] private float outlineWidth = DefaultOutlineWidth;

    public static float OutlineWidth
    {
        get
        {
            loadedSettings = loadedSettings != null
                ? loadedSettings
                : Resources.Load<InteractableOutlineSettings>(ResourcesPath);

            return loadedSettings != null ? loadedSettings.outlineWidth : DefaultOutlineWidth;
        }
    }
}
