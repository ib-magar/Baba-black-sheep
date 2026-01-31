using UnityEngine;

public abstract class InteractableBlock : MonoBehaviour
{
    // Abstract method that must be implemented by derived classes
    public abstract bool CanPlayerMoveHere(InteractionData interactionData);
}