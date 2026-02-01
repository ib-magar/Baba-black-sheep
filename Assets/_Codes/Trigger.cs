using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class Trigger : MonoBehaviour
{
    [Header("Button Visual")]
    [SerializeField] private Transform buttonTransform;
    [SerializeField] private float buttonPressDistance = 0.05f;
    [SerializeField] private float buttonPressSpeed = 5f;

    [Header("Events")]
    public UnityEvent OnTriggerActivated;
    public UnityEvent OnTriggerDeactivated;

    [Header("Settings")]
    [SerializeField] private bool isOneTimeTrigger = false;
    [SerializeField] private LayerMask triggerLayers = ~0; // Everything by default

    private bool isPressed = false;
    private bool hasBeenTriggered = false;
    private Vector3 buttonUpPosition;
    private Vector3 buttonDownPosition;

    private List<Collider> currentColliders = new List<Collider>();

    private void Start()
    {
        if (buttonTransform != null)
        {
            buttonUpPosition = buttonTransform.localPosition;
            buttonDownPosition = buttonUpPosition - new Vector3(0, buttonPressDistance, 0);
        }
    }

    private void Update()
    {
        if (buttonTransform != null)
        {
            // Smoothly move button based on pressed state
            Vector3 targetPosition = isPressed ? buttonDownPosition : buttonUpPosition;
            buttonTransform.localPosition = Vector3.Lerp(
                buttonTransform.localPosition,
                targetPosition,
                Time.deltaTime * buttonPressSpeed
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if layer is in triggerLayers
        if (!IsLayerInMask(other.gameObject.layer)) return;

        // Check if this is a one-time trigger that has already been used
        if (isOneTimeTrigger && hasBeenTriggered) return;

        // Add to current colliders if not already present
        if (!currentColliders.Contains(other))
        {
            currentColliders.Add(other);

            // Only trigger if this is the first collider entering
            if (currentColliders.Count == 1)
            {
                ActivateTrigger();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (currentColliders.Contains(other))
        {
            currentColliders.Remove(other);

            // Only deactivate when all colliders have exited
            if (currentColliders.Count == 0 && !isOneTimeTrigger)
            {
            if (checkColliders())
            return;
                DeactivateTrigger();
            }
        }
    }
    public bool checkColliders()
    {
        if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit hit, .5f, triggerLayers,
                QueryTriggerInteraction.Collide))
            return true;

        return false;
    }
    private void ActivateTrigger()
    {
        if (isPressed) return;

        isPressed = true;
        if (isOneTimeTrigger) hasBeenTriggered = true;

        // Invoke the activation event
        OnTriggerActivated?.Invoke();
    }
    private void DeactivateTrigger()
    {
        if (!isPressed) return;

        isPressed = false;

        // Invoke the deactivation event
        OnTriggerDeactivated?.Invoke();
    }

    private bool IsLayerInMask(int layer)
    {
        return triggerLayers == (triggerLayers | (1 << layer));
    }

    public void ResetTrigger()
    {
        // Useful for resetting one-time triggers
        if (isOneTimeTrigger)
        {
            hasBeenTriggered = false;
            isPressed = false;
            currentColliders.Clear();
        }
    }

    // For external activation (e.g., from other scripts)
    public void ActivateFromExternal()
    {
        if (isOneTimeTrigger && hasBeenTriggered) return;
        ActivateTrigger();
    }

    // For external deactivation
    public void DeactivateFromExternal()
    {
        if (isOneTimeTrigger) return;
        DeactivateTrigger();
    }

    // Getters
    public bool IsPressed => isPressed;
    public bool HasBeenTriggered => hasBeenTriggered;
}