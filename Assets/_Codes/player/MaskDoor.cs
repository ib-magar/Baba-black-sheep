using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class MaskDoor : InteractableBlock
{
    [Header("Mask Settings")]
    public MaskDataSO MaskDataSO;
    public MaskType currentMask;
    public Transform maskObjectparent;

    [Header("Door Animation")]
    [SerializeField] private Transform leftDoorAxis;
    [SerializeField] private Transform rightDoorAxis;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float closeAngle = 0f;
    [SerializeField] private float openDuration = 0.5f;
    [SerializeField] private float stayOpenDuration = 1.5f;
    [SerializeField] private float closeDuration = 0.5f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool closeAutomatically = true;

    [Header("Events")]
    public UnityEvent onDoorUnlock;
    public UnityEvent onDoorOpenStart;
    public UnityEvent onDoorCloseStart;
    public UnityEvent onDoorAnimationComplete;

    // Door state
    private bool isDoorOpen = false;
    private Coroutine doorAnimationCoroutine;
    private Quaternion leftDoorInitialRotation;
    private Quaternion rightDoorInitialRotation;

    private void Start()
    {
        // Store initial rotations
        if (leftDoorAxis != null)
            leftDoorInitialRotation = leftDoorAxis.localRotation;
        if (rightDoorAxis != null)
            rightDoorInitialRotation = rightDoorAxis.localRotation;

        // Instantiate mask object
        GameObject maskObject = MaskDataSO.getGateMaskItem(currentMask);
        if (maskObject != null)
        {
            Instantiate(maskObject, maskObjectparent.position, maskObjectparent.rotation);
        }
    }

    public override bool CanPlayerMoveHere(InteractionData interactionData)
    {
        bool canBlockMove = MatchMask(interactionData.currentMask);

        if (canBlockMove)
        {
            // Trigger door unlock event
            onDoorUnlock?.Invoke();
            
            // Open the door
            OpenDoor(interactionData);
            
            return true;
        }
        return false;
    }

    bool MatchMask(MaskType tomatch)
    {
        return MaskDataSO.MaskMatch(currentMask, tomatch);
    }

    /// <summary>
    /// Opens the door based on player direction
    /// </summary>
    public void OpenDoor(InteractionData interactionData)
    {
        // Stop any ongoing animation
        if (doorAnimationCoroutine != null)
        {
            StopCoroutine(doorAnimationCoroutine);
        }
        
        // Start new door animation
        doorAnimationCoroutine = StartCoroutine(DoorAnimationRoutine(interactionData.direction));
    }

    /// <summary>
    /// Manually opens the door without player interaction
    /// </summary>
    public void OpenDoorManual(Vector3 direction)
    {
        if (doorAnimationCoroutine != null)
        {
            StopCoroutine(doorAnimationCoroutine);
        }
        
        doorAnimationCoroutine = StartCoroutine(DoorAnimationRoutine(direction));
    }

    /// <summary>
    /// Coroutine that handles the door animation sequence
    /// </summary>
    private IEnumerator DoorAnimationRoutine(Vector3 playerDirection)
    {
        // Ensure we have door axes
        if (leftDoorAxis == null || rightDoorAxis == null)
        {
            Debug.LogWarning("Door axes not assigned!");
            yield break;
        }

        // Determine opening direction based on player approach
        float leftDoorTargetAngle;
        float rightDoorTargetAngle;

        // Calculate dot product to determine which side player is approaching from
        Vector3 toPlayer = (playerDirection).normalized;
        Vector3 doorForward = transform.forward;
        
        // Determine if player is approaching from front or back
        float dot = Vector3.Dot(toPlayer, doorForward);
        
        if (Mathf.Abs(dot) > 0.7f)
        {
            // Player is approaching from front or back
            if (dot > 0)
            {
                // Front approach - doors open inward
                leftDoorTargetAngle = -openAngle;
                rightDoorTargetAngle = openAngle;
            }
            else
            {
                // Back approach - doors open outward
                leftDoorTargetAngle = openAngle;
                rightDoorTargetAngle = -openAngle;
            }
        }
        else
        {
            // Player is approaching from side - determine which side
            Vector3 doorRight = transform.right;
            float rightDot = Vector3.Dot(toPlayer, doorRight);
            
            if (rightDot > 0)
            {
                // Approaching from right side
                leftDoorTargetAngle = openAngle;
                rightDoorTargetAngle = openAngle;
            }
            else
            {
                // Approaching from left side
                leftDoorTargetAngle = -openAngle;
                rightDoorTargetAngle = -openAngle;
            }
        }

        // Start door opening
        onDoorOpenStart?.Invoke();
        
        // Opening animation
        yield return StartCoroutine(RotateDoors(
            leftDoorInitialRotation,
            Quaternion.Euler(0, leftDoorTargetAngle, 0) * leftDoorInitialRotation,
            rightDoorInitialRotation,
            Quaternion.Euler(0, rightDoorTargetAngle, 0) * rightDoorInitialRotation,
            openDuration,
            openCurve
        ));

        isDoorOpen = true;

        // Wait while door stays open
        if (closeAutomatically)
        {
            yield return new WaitForSeconds(stayOpenDuration);

            // Start door closing
            onDoorCloseStart?.Invoke();
            
            // Closing animation
            yield return StartCoroutine(RotateDoors(
                leftDoorAxis.localRotation,
                leftDoorInitialRotation,
                rightDoorAxis.localRotation,
                rightDoorInitialRotation,
                closeDuration,
                closeCurve
            ));

            isDoorOpen = false;
        }

        // Animation complete
        onDoorAnimationComplete?.Invoke();
        doorAnimationCoroutine = null;
    }

    /// <summary>
    /// Coroutine to smoothly rotate both doors
    /// </summary>
    private IEnumerator RotateDoors(
        Quaternion leftStartRot, Quaternion leftEndRot,
        Quaternion rightStartRot, Quaternion rightEndRot,
        float duration, AnimationCurve curve)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = curve.Evaluate(t);

            if (leftDoorAxis != null)
                leftDoorAxis.localRotation = Quaternion.Slerp(leftStartRot, leftEndRot, curvedT);
            
            if (rightDoorAxis != null)
                rightDoorAxis.localRotation = Quaternion.Slerp(rightStartRot, rightEndRot, curvedT);

            yield return null;
        }

        // Ensure final rotation is exact
        if (leftDoorAxis != null)
            leftDoorAxis.localRotation = leftEndRot;
        
        if (rightDoorAxis != null)
            rightDoorAxis.localRotation = rightEndRot;
    }

    /// <summary>
    /// Immediately closes the door if it's open
    /// </summary>
    public void CloseDoorImmediately()
    {
        if (doorAnimationCoroutine != null)
        {
            StopCoroutine(doorAnimationCoroutine);
            doorAnimationCoroutine = null;
        }

        if (leftDoorAxis != null)
            leftDoorAxis.localRotation = leftDoorInitialRotation;
        
        if (rightDoorAxis != null)
            rightDoorAxis.localRotation = rightDoorInitialRotation;

        isDoorOpen = false;
        onDoorAnimationComplete?.Invoke();
    }

    /// <summary>
    /// Manually starts the close animation
    /// </summary>
    public void CloseDoorWithAnimation()
    {
        if (!isDoorOpen) return;

        if (doorAnimationCoroutine != null)
        {
            StopCoroutine(doorAnimationCoroutine);
        }

        doorAnimationCoroutine = StartCoroutine(CloseAnimationRoutine());
    }

    /// <summary>
    /// Coroutine for closing animation only
    /// </summary>
    private IEnumerator CloseAnimationRoutine()
    {
        onDoorCloseStart?.Invoke();
        
        yield return StartCoroutine(RotateDoors(
            leftDoorAxis.localRotation,
            leftDoorInitialRotation,
            rightDoorAxis.localRotation,
            rightDoorInitialRotation,
            closeDuration,
            closeCurve
        ));

        isDoorOpen = false;
        onDoorAnimationComplete?.Invoke();
        doorAnimationCoroutine = null;
    }

    /// <summary>
    /// Toggles door open/close state
    /// </summary>
    public void ToggleDoor(Vector3 direction)
    {
        if (isDoorOpen)
        {
            CloseDoorWithAnimation();
        }
        else
        {
            OpenDoorManual(direction);
        }
    }

    // Getters
    public bool IsDoorOpen => isDoorOpen;
    public Transform LeftDoorAxis => leftDoorAxis;
    public Transform RightDoorAxis => rightDoorAxis;

    /// <summary>
    /// For debugging in editor - test door opening
    /// </summary>
    [ContextMenu("Test Open Door")]
    private void TestOpenDoor()
    {
        OpenDoorManual(Vector3.forward);
    }

    [ContextMenu("Test Close Door")]
    private void TestCloseDoor()
    {
        CloseDoorWithAnimation();
    }

    [ContextMenu("Reset Door")]
    private void ResetDoor()
    {
        CloseDoorImmediately();
    }
}