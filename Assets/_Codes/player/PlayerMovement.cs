using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float jumpHeight = 0.5f;
    [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private float rotationSpeed = 10f; // Added rotation speed
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask blockLayer;

    [Header("Input Settings")]
    [SerializeField] private float inputDeadzone = 0.1f;

    [Header("References")]
    [SerializeField] private Collider playerCollider;
    [SerializeField] private Transform visualTransform; // This should be the visual object that rotates

    // Movement state
    private bool isMoving = false;
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private Vector3 visualStartPosition;
    private float moveTimer = 0f;

    // Rotation state
    private Quaternion targetRotation;
    private Quaternion startRotation;
    private bool isRotating = false;

    // Input handling
    private Vector2 currentInput;
    private Vector2 lastValidInput;
    private bool inputActive = false;

    // Global direction vectors
    private Vector3 globalForward = Vector3.forward;
    private Vector3 globalBackward = Vector3.back;
    private Vector3 globalRight = Vector3.right;
    private Vector3 globalLeft = Vector3.left;

    // Current facing direction
    private Vector3 currentFacingDirection = Vector3.forward;

    // Input Actions
    private InputAction moveAction;
    private PlayerInput playerInput;

    private void Awake()
    {
        Physics.queriesHitTriggers = true;
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("PlayerMovement: No PlayerInput component found!");
            return;
        }

        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider>();
            if (playerCollider == null)
            {
                Debug.LogError("PlayerMovement: No collider found on player!");
            }
        }

        if (visualTransform == null)
        {
            visualTransform = transform.Find("Visual");
            if (visualTransform == null && transform.childCount > 0)
            {
                visualTransform = transform.GetChild(0);
            }
        }

        SetupInputActions();
    }

    private void SetupInputActions()
    {
        InputActionMap playerMap = playerInput.actions.FindActionMap("Player");
        if (playerMap == null)
        {
            Debug.LogError("PlayerMovement: 'Player' action map not found!");
            return;
        }

        moveAction = playerMap.FindAction("Movement");
        if (moveAction != null)
        {
            playerMap.Enable();
            moveAction.performed += OnMovementPerformed;
            moveAction.canceled += OnMovementCanceled;
        }
        else
        {
            Debug.LogError("PlayerMovement: 'Movement' action not found in Player action map!");
        }
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.Disable();
    }

    private void Start()
    {
        if (visualTransform != null)
        {
            visualStartPosition = visualTransform.localPosition;
            // Initialize rotation to face forward
            targetRotation = Quaternion.LookRotation(globalForward);
            visualTransform.rotation = targetRotation;
            currentFacingDirection = globalForward;
        }
    }

    private void Update()
    {
        if (inputActive && !isMoving)
        {
            ProcessMovementInput(currentInput);
        }

        if (isMoving)
        {
            UpdateMovement();
        }

        // Smoothly rotate towards target rotation
        if (isRotating && visualTransform != null)
        {
            UpdateRotation();
        }
    }

    public void OnMovementPerformed(InputAction.CallbackContext context)
    {
        currentInput = context.ReadValue<Vector2>();
        
        if (currentInput.magnitude > inputDeadzone)
        {
            inputActive = true;
            lastValidInput = currentInput.normalized;

            if (!isMoving)
            {
                ProcessMovementInput(lastValidInput);
            }
        }
        else
        {
            OnMovementCanceled(context);
        }
    }

    public void OnMovementCanceled(InputAction.CallbackContext context)
    {
        inputActive = false;
        currentInput = Vector2.zero;
    }

    private void ProcessMovementInput(Vector2 inputDirection)
    {
        if (isMoving || inputDirection.magnitude == 0)
            return;

        Vector3 movementDirection = GetGlobalDirection(inputDirection);

        if (movementDirection != Vector3.zero && CanMoveToPosition(movementDirection))
        {
            // Set rotation before starting movement
            SetFacingDirection(movementDirection);
            StartMovement(movementDirection);
        }
    }

    private Vector3 GetGlobalDirection(Vector2 input)
    {
        Vector3 movementDirection = Vector3.zero;

        float absX = Mathf.Abs(input.x);
        float absY = Mathf.Abs(input.y);

        if (absX > absY)
        {
            if (input.x > 0) movementDirection = globalRight;
            else movementDirection = globalLeft;
        }
        else
        {
            if (input.y > 0) movementDirection = globalForward;
            else movementDirection = globalBackward;
        }

        return movementDirection.normalized * gridSize;
    }

    private void SetFacingDirection(Vector3 movementDirection)
    {
        if (visualTransform == null) return;

        // Normalize the movement direction
        Vector3 lookDirection = movementDirection.normalized;
        
        // Don't rotate if we're already facing that direction
        if (Vector3.Dot(currentFacingDirection, lookDirection) > 0.99f)
            return;

        currentFacingDirection = lookDirection;
        
        // Calculate target rotation
        targetRotation = Quaternion.LookRotation(lookDirection);
        startRotation = visualTransform.rotation;
        isRotating = true;
    }

    private void UpdateRotation()
    {
        if (visualTransform == null) return;

        // Smoothly interpolate rotation
        visualTransform.rotation = Quaternion.Slerp(
            visualTransform.rotation, 
            targetRotation, 
            rotationSpeed * Time.deltaTime
        );

        // Check if rotation is complete
        if (Quaternion.Angle(visualTransform.rotation, targetRotation) < 0.1f)
        {
            visualTransform.rotation = targetRotation;
            isRotating = false;
        }
    }

    private bool CanMoveToPosition(Vector3 direction)
    {
        Vector3 targetPos = transform.position + direction;

        if (playerCollider == null)
        {
            return true;
        }

        Vector3 colliderCenter = playerCollider.bounds.center;
        RaycastHit hit;
        Vector3 raycastStart = colliderCenter + Vector3.up * 0.1f;
        Vector3 halfExtents = playerCollider.bounds.extents * 0.8f;
        halfExtents.y = 0.05f;

        // Check for obstacles first
        if (Physics.BoxCast(raycastStart, halfExtents, direction.normalized,
            out hit, Quaternion.identity, gridSize, obstacleLayer))
        {
            return false;
        }

        RaycastHit rayHit;

        if (Physics.Raycast(raycastStart, direction.normalized, out rayHit, gridSize, allMask, QueryTriggerInteraction.Collide))
        {
            if (rayHit.collider.gameObject.TryGetComponent<InteractableBlock>(out InteractableBlock block))
                return TryInteractWithBlock(direction, targetPos, block);
        }

        return true;
    }

    public LayerMask allMask;
    public PlayerMask playerMask;
    
    private bool TryInteractWithBlock(Vector3 direction, Vector3 playerTargetPos, InteractableBlock blockObject)
    {
        // Create interaction data
        InteractionData interactionData = new InteractionData(
            direction,
            transform.position,
            playerTargetPos,
            gameObject,
            playerMask.GetCurrentMask()
        );

        // Ask the block if the player can move
        bool canMove = blockObject.CanPlayerMoveHere(interactionData);

        return canMove;
    }

    private void StartMovement(Vector3 direction)
    {
        isMoving = true;
        moveTimer = 0f;

        startPosition = transform.position;
        targetPosition = startPosition + direction;

        if (visualTransform != null)
        {
            visualStartPosition = visualTransform.localPosition;
        }

        targetPosition = new Vector3(
            Mathf.Round(targetPosition.x / gridSize) * gridSize,
            startPosition.y,
            Mathf.Round(targetPosition.z / gridSize) * gridSize
        );
    }

    private void UpdateMovement()
    {
        moveTimer += Time.deltaTime;
        float t = Mathf.Clamp01(moveTimer / moveDuration);
        float easedT = EaseInOutCubic(t);

        // Move main object linearly
        Vector3 horizontalPos = Vector3.Lerp(startPosition, targetPosition, easedT);
        transform.position = horizontalPos;

        // Apply jump to visual
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

            if (inputActive && !isMoving)
            {
                ProcessMovementInput(lastValidInput);
            }
        }
    }

    private float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    public void MoveInDirection(Vector2 direction)
    {
        if (!isMoving && direction.magnitude > inputDeadzone)
        {
            ProcessMovementInput(direction.normalized);
        }
    }

    // Public method to manually set facing direction (useful for cutscenes, etc.)
    public void SetFacing(Vector3 direction)
    {
        if (visualTransform == null || direction.magnitude == 0) return;
        
        direction = direction.normalized;
        currentFacingDirection = direction;
        targetRotation = Quaternion.LookRotation(direction);
        visualTransform.rotation = targetRotation;
        isRotating = false; // Instantly face the direction
    }

    // Get current facing direction
    public Vector3 GetFacingDirection()
    {
        return currentFacingDirection;
    }

    private void OnDestroy()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMovementPerformed;
            moveAction.canceled -= OnMovementCanceled;
        }
    }

    // Helper methods
    public bool IsMoving() => isMoving;
    public Vector3 GetTargetPosition() => targetPosition;

    private void OnDrawGizmosSelected()
    {
        if (playerCollider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(playerCollider.bounds.center, playerCollider.bounds.size);

            if (Application.isPlaying)
            {
                // Draw facing direction
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, currentFacingDirection * 1.5f);

                if (isMoving)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(startPosition, targetPosition);
                    Gizmos.DrawWireSphere(targetPosition, 0.2f);
                }
            }
        }
    }
}