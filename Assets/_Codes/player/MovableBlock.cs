using Sirenix.OdinInspector;
using UnityEngine;

public class MovableBlock : InteractableBlock
{
    [Header("Movement Settings")]
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float moveDuration = 0.5f;

    [Header("Visual Settings")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private float jumpHeight = 0.3f;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask collisionLayers; // Set to "Everything" or specific layers

    // Movement state
    private bool isMoving = false;
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private Vector3 visualStartPosition;
    private float moveTimer = 0f;

    // Collider reference
    private Collider blockCollider;

    private void Awake()
    {
        Physics.queriesHitTriggers = false;

        // Get collider
        blockCollider = GetComponent<Collider>();
        if (blockCollider == null)
        {
            Debug.LogError($"MovableBlock {name}: No collider found!");
        }

        // Setup visual transform
        if (visualTransform == null)
        {
            visualTransform = transform;
        }

        visualStartPosition = visualTransform.localPosition;
    }

    public override bool CanPlayerMoveHere(InteractionData interactionData)
    {
        Debug.Log($"MovableBlock {name} checking if player can move to {interactionData.playerTargetPosition}");

        // Check if this block can move in the given direction
        bool canBlockMove = CanBlockMoveTo(interactionData.direction);

        if (canBlockMove)
        {
            // If block can move, start moving it
            StartBlockMovement(interactionData.direction);
            return true;
        }


        return false;
    }

    private bool CanBlockMoveTo(Vector3 direction)
    {
        Vector3 targetPos = transform.position + direction;

        // Check for any collider at target position
        bool isTargetPositionOccupied = CheckCollisionAtPosition(targetPos);

        if (isTargetPositionOccupied)
        {
            Debug.Log($"MovableBlock {name}: Target position occupied, cannot move");
            return false;
        }

        Debug.Log($"MovableBlock {name}: Target position clear, can move");
        return true;
    }

    private bool CheckCollisionAtPosition(Vector3 position)
    {
        if (blockCollider == null) return false;

        // Calculate collider bounds at target position
        Vector3 colliderCenter = position + (blockCollider.bounds.center - transform.position);
        Vector3 halfExtents = blockCollider.bounds.extents * 0.9f; // Slightly smaller to avoid self-collision
        Debug.DrawLine(transform.position, colliderCenter, Color.red, 2f);
        RaycastHit hit;
        if (Physics.Raycast(transform.position,(colliderCenter-transform.position).normalized,out hit,gridSize))
        {
            return true;
        }

        return false;
    }

    [Button("move left")]
    void moveleft()=>StartBlockMovement(Vector3.left * gridSize);

    private void StartBlockMovement(Vector3 direction)
    {
        if (isMoving) return;

        startPosition = transform.position;
        targetPosition = startPosition + direction;

        // Align to grid
        targetPosition = new Vector3(
            Mathf.Round(targetPosition.x / gridSize) * gridSize,
            startPosition.y,
            Mathf.Round(targetPosition.z / gridSize) * gridSize
        );

        isMoving = true;
        moveTimer = 0f;

        Debug.Log($"Moving block from {startPosition} to {targetPosition}");
    }

    private void Update()
    {
        if (isMoving)
        {
            UpdateBlockMovement();
        }
    }

    private void UpdateBlockMovement()
    {
        moveTimer += Time.deltaTime;
        float t = Mathf.Clamp01(moveTimer / moveDuration);
        float easedT = EaseInOutCubic(t);

        // Move main object linearly
        transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);

        // Apply jump animation to visual
        if (visualTransform != null)
        {
            float jumpProgress = Mathf.Sin(easedT * Mathf.PI);
            Vector3 verticalOffset = Vector3.up * (jumpHeight * jumpProgress);
            visualTransform.localPosition = visualStartPosition + verticalOffset;
        }

        if (t >= 1f)
        {
            isMoving = false;
            transform.position = targetPosition;

            if (visualTransform != null)
            {
                visualTransform.localPosition = visualStartPosition;
            }

            OnMovementComplete();
        }
    }

    private void OnMovementComplete()
    {
        Debug.Log($"MovableBlock {name} movement complete");
    }

    private float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    // For visualization
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && isMoving)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(startPosition, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.2f);
        }

        // Draw collision check area in editor
        if (blockCollider != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(blockCollider.bounds.center, blockCollider.bounds.size);
        }
    }
}