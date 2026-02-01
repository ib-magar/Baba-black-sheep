using UnityEngine;

public class SimpleActivateDeactivate : TriggerEffect
{
    [Header("Target Objects")]
    [SerializeField] private GameObject[] objectsToActivate;
    [SerializeField] private GameObject[] objectsToDeactivate;

    [Header("Settings")]
    [SerializeField] private bool deactivateOnExit = true;

    protected override void OnTriggerActivated()
    {
        // Activate specified objects
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        // Deactivate specified objects
        foreach (GameObject obj in objectsToDeactivate)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    protected override void OnTriggerDeactivated()
    {
        if (!deactivateOnExit) return;

        // Reverse the process if deactivateOnExit is true
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        foreach (GameObject obj in objectsToDeactivate)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }
}