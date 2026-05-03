using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionCursor : MonoBehaviour
{
    [SerializeField] private Camera controlledCamera;
    [SerializeField] private LayerMask interactionMask = ~0;
    [SerializeField] private float maxRayDistance = 1000f;

    private Interactable hoveredInteractable;

    private void Awake()
    {
        controlledCamera = controlledCamera != null ? controlledCamera : GetComponent<Camera>();
    }

    private void Update()
    {
        var interactable = FindInteractableUnderCursor();
        SetHoveredInteractable(interactable);

        if (interactable != null && Input.GetMouseButtonDown(0) && !PointerIsOverUi())
        {
            interactable.Interact();
        }
    }

    private Interactable FindInteractableUnderCursor()
    {
        if (controlledCamera == null)
        {
            return null;
        }

        var ray = controlledCamera.ScreenPointToRay(Input.mousePosition);
        var hits = Physics.RaycastAll(ray, maxRayDistance, interactionMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            var hitInteractable = hit.collider.GetComponentInParent<Interactable>();
            if (hitInteractable != null)
            {
                return hitInteractable;
            }
        }

        var nearestDistance = float.PositiveInfinity;
        Interactable nearestInteractable = null;
        var interactables = FindObjectsOfType<Interactable>();

        foreach (var interactable in interactables)
        {
            if (interactable == null || !interactable.TryGetBounds(out var bounds))
            {
                continue;
            }

            if (!bounds.IntersectRay(ray, out var distance) || distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            nearestInteractable = interactable;
        }

        return nearestInteractable;
    }

    private void SetHoveredInteractable(Interactable interactable)
    {
        if (hoveredInteractable == interactable)
        {
            return;
        }

        if (hoveredInteractable != null)
        {
            hoveredInteractable.SetHovered(false);
        }

        hoveredInteractable = interactable;

        if (hoveredInteractable != null)
        {
            hoveredInteractable.SetHovered(true);
        }
    }

    private static bool PointerIsOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
