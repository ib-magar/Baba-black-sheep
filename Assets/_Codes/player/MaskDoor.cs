using UnityEngine;
using UnityEngine.Events;

public class MaskDoor : InteractableBlock
{
    public MaskDataSO MaskDataSO;
    public MaskType currentMask;

    public UnityEvent onDoorUnlock;
    public override bool CanPlayerMoveHere(InteractionData interactionData)
    {
        bool canBlockMove = MatchMask(interactionData.currentMask);

        if (canBlockMove)
        {
            onDoorUnlock?.Invoke(); 
            return true;
        }
        return false;
    }

    bool MatchMask(MaskType tomatch)
    {
       return MaskDataSO.MaskMatch(currentMask, tomatch);
    }
}
