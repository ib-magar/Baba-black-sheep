using UnityEngine;

public abstract class TriggerEffect : MonoBehaviour
{
    [Header("Trigger Reference")]
    [SerializeField] protected Trigger trigger;

    protected virtual void Start()
    {
        // If no trigger is assigned, try to find one in the scene
        if (trigger == null)
        {
            trigger = GetComponent<Trigger>();
        }

        // Subscribe to trigger events if trigger exists
        if (trigger != null)
        {
            trigger.OnTriggerActivated.AddListener(OnTriggerActivated);
            trigger.OnTriggerDeactivated.AddListener(OnTriggerDeactivated);
        }
        else
        {
            Debug.LogWarning($"No trigger assigned to {gameObject.name}. TriggerEffect will not function.");
        }
    }

    protected virtual void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (trigger != null)
        {
            trigger.OnTriggerActivated.RemoveListener(OnTriggerActivated);
            trigger.OnTriggerDeactivated.RemoveListener(OnTriggerDeactivated);
        }
    }

    // Abstract methods to be implemented by derived classes
    protected abstract void OnTriggerActivated();
    protected abstract void OnTriggerDeactivated();

    // Optional method for one-time trigger effects
    protected virtual void OnTriggered() { }
}