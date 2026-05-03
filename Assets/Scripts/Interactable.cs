using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [SerializeField] private Transform interactionRoot;
    [SerializeField] private UnityEvent onClick = new UnityEvent();
    [SerializeField] private UnityEvent onHoverEnter = new UnityEvent();
    [SerializeField] private UnityEvent onHoverExit = new UnityEvent();

    private Renderer[] renderers;
    private bool isHovered;

    public Transform InteractionRoot => interactionRoot != null ? interactionRoot : transform;
    public UnityEvent Clicked => onClick;
    public UnityEvent HoverEntered => onHoverEnter;
    public UnityEvent HoverExited => onHoverExit;
    public bool IsHovered => isHovered;

    private void Awake()
    {
        CacheRenderers();
    }

    private void OnDisable()
    {
        SetHovered(false);
    }

    public bool TryGetBounds(out Bounds bounds)
    {
        CacheRenderers();
        bounds = default;

        if (renderers.Length == 0)
        {
            return false;
        }

        bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    public void SetHovered(bool hovered)
    {
        if (isHovered == hovered)
        {
            return;
        }

        isHovered = hovered;

        if (hovered)
        {
            onHoverEnter.Invoke();
        }
        else
        {
            onHoverExit.Invoke();
        }
    }

    public void Interact()
    {
        onClick.Invoke();
    }

    private void CacheRenderers()
    {
        if (renderers != null)
        {
            return;
        }

        renderers = InteractionRoot.GetComponentsInChildren<Renderer>();
    }
}
