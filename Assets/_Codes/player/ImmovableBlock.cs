using UnityEngine;

public class ImmovableBlock : InteractableBlock
{
    public override bool CanPlayerMoveHere(InteractionData interactionData)
    {
        Debug.Log($"ImmovableBlock {name} blocks movement");
        return false; // Always blocks movement
    }
}