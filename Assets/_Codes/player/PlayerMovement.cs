using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float jumpHeight = 0.5f;
    [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("Input Settings")]
    [SerializeField] private float inputDeadzone = 0.1f;
    
    [Header("References")]
    [SerializeField] private Collider playerCollider;
    [SerializeField] private Transform visualTransform; // Visual object for jump effect
    
    // Movement state
    private bool isMoving = false;
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private Vector3 visualStartPosition;
    private float moveTimer = 0f;
    
    // Input handling
    private Vector2 currentInput;
    private Vector2 lastValidInput;
    private bool inputActive = false;
    
    // Global direction vectors
    private Vector3 globalForward = Vector3.forward;    // Positive Z axis
    private Vector3 globalBackward = Vector3.back;      // Negative Z axis
    private Vector3 globalRight = Vector3.right;        // Positive X axis
    private Vector3 globalLeft = Vector3.left;          // Negative X axis
    
    // Input Actions
    private InputAction moveAction;
    private PlayerInput playerInput;
    
    private void Awake()
    {
        // Get PlayerInput component
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("PlayerMovement: No PlayerInput component found!");
            return;
        }
        
        // Get the collider if not assigned
        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider>();
            if (playerCollider == null)
            {
                Debug.LogError("PlayerMovement: No collider found on player!");
            }
        }
        
        // Get visual transform if not assigned
        if (visualTransform == null)
        {
            // Try to find a child named "Visual" or use the first child
            visualTransform = transform.Find("Visual");
            if (visualTransform == null && transform.childCount > 0)
            {
                visualTransform = transform.GetChild(0);
                Debug.LogWarning($"PlayerMovement: Using first child '{visualTransform.name}' as visual transform");
            }
            else if (visualTransform == null)
            {
                Debug.LogWarning("PlayerMovement: No visual transform assigned and no child found. Jump effect will not work.");
            }
        }
        
        SetupInputActions();
    }
    
    private void SetupInputActions()
    {
        // Find the Movement action in the Player action map
        InputActionMap playerMap = playerInput.actions.FindActionMap("Player");
        if (playerMap == null)
        {
            Debug.LogError("PlayerMovement: 'Player' action map not found!");
            return;
        }
        
        moveAction = playerMap.FindAction("Movement");
        if (moveAction == null)
        {
            Debug.LogError("PlayerMovement: 'Movement' action not found in Player action map!");
            return;
        }
        
        // Enable the action map
        playerMap.Enable();
        
        // Subscribe to movement events
        moveAction.performed += OnMovementPerformed;
        moveAction.canceled += OnMovementCanceled;
    }
    
    private void OnEnable()
    {
        // Re-enable input when component is enabled
        if (moveAction != null)
        {
            moveAction.Enable();
        }
    }
    
    private void OnDisable()
    {
        // Disable input when component is disabled
        if (moveAction != null)
        {
            moveAction.Disable();
        }
    }
    
    private void Start()
    {
        // Store initial visual position
        if (visualTransform != null)
        {
            visualStartPosition = visualTransform.localPosition;
        }
    }
    
    private void Update()
    {
        // Handle continuous input while moving
        if (inputActive && !isMoving)
        {
            ProcessMovementInput(currentInput);
        }
        
        // Update movement animation if moving
        if (isMoving)
        {
            UpdateMovement();
        }
    }
    
    // Public event handler for Movement action performed
    public void OnMovementPerformed(InputAction.CallbackContext context)
    {
        currentInput = context.ReadValue<Vector2>();
        
        // Check if input exceeds deadzone
        if (currentInput.magnitude > inputDeadzone)
        {
            inputActive = true;
            lastValidInput = currentInput.normalized;
            
            // If not currently moving, process immediately
            if (!isMoving)
            {
                ProcessMovementInput(lastValidInput);
            }
        }
        else
        {
            // Input below deadzone, treat as canceled
            OnMovementCanceled(context);
        }
    }
    
    // Event handler for Movement action canceled
    public void OnMovementCanceled(InputAction.CallbackContext context)
    {
        inputActive = false;
        currentInput = Vector2.zero;
    }
    
    private void ProcessMovementInput(Vector2 inputDirection)
    {
        // If already moving or no valid input, exit
        if (isMoving || inputDirection.magnitude == 0)
            return;
        
        // Determine movement direction based on input for global axis movement
        Vector3 movementDirection = GetGlobalDirection(inputDirection);
        
        // Check if movement is possible
        if (movementDirection != Vector3.zero && CanMoveToPosition(movementDirection))
        {
            StartMovement(movementDirection);
        }
    }
    
    private Vector3 GetGlobalDirection(Vector2 input)
    {
        Vector3 movementDirection = Vector3.zero;
        
        // Get the dominant axis
        float absX = Mathf.Abs(input.x);
        float absY = Mathf.Abs(input.y);
        
        // Determine primary direction based on which axis has greater magnitude
        if (absX > absY)
        {
            // Horizontal movement dominates
            if (input.x > 0)
            {
                movementDirection = globalRight; // Positive X axis
            }
            else
            {
                movementDirection = globalLeft;  // Negative X axis
            }
        }
        else
        {
            // Vertical movement dominates
            if (input.y > 0)
            {
                movementDirection = globalForward;  // Positive Z axis (Forward)
            }
            else
            {
                movementDirection = globalBackward; // Negative Z axis (Backward)
            }
        }
        
        return movementDirection.normalized * gridSize;
    }
    
    private bool CanMoveToPosition(Vector3 direction)
    {
        Vector3 targetPos = transform.position + direction;
        
        // If no collider assigned, allow movement (for debugging)
        if (playerCollider == null)
        {
            Debug.LogWarning("PlayerMovement: No collider assigned, movement allowed by default.");
            return true;
        }
        
        // Calculate collider center in world space
        Vector3 colliderCenter = playerCollider.bounds.center;
        
        // Check for obstacles at target position using BoxCast
        RaycastHit hit;
        Vector3 raycastStart = colliderCenter + Vector3.up * 0.1f;
        
        // Cast a box that matches the player's collider size
        Vector3 halfExtents = playerCollider.bounds.extents * 0.9f; // Slightly smaller to avoid edge cases
        halfExtents.y = 0.05f; // Keep vertical check small
        
        // Debug visualization
        Debug.DrawRay(raycastStart, direction, Color.yellow, 1f);
        
        // Check for obstacles
        if (Physics.BoxCast(raycastStart, halfExtents, direction.normalized, 
            out hit, Quaternion.identity, gridSize, obstacleLayer))
        {
            Debug.Log($"Cannot move: Obstacle detected - {hit.collider.name}");
            return false;
        }
        
        // Ground checking removed as requested
        
        return true;
    }
    
    private void StartMovement(Vector3 direction)
    {
        isMoving = true;
        moveTimer = 0f;
        
        startPosition = transform.position;
        targetPosition = startPosition + direction;
        
        // Store visual start position
        if (visualTransform != null)
        {
            visualStartPosition = visualTransform.localPosition;
        }
        
        // Ensure target position aligns with grid
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
        
        // Apply easing for smoother movement
        float easedT = EaseInOutCubic(t);
        
        // Move the main object linearly
        Vector3 horizontalPos = Vector3.Lerp(startPosition, targetPosition, easedT);
        transform.position = horizontalPos;
        
        // Apply jump animation to visual object only
        if (visualTransform != null)
        {
            float jumpProgress = Mathf.Sin(easedT * Mathf.PI);
            Vector3 verticalOffset = Vector3.up * (jumpHeight * jumpProgress);
            visualTransform.localPosition = visualStartPosition + verticalOffset;
        }
        
        // Check if movement is complete
        if (t >= 1f)
        {
            isMoving = false;
            transform.position = targetPosition; // Snap to exact grid position
            
            // Reset visual position
            if (visualTransform != null)
            {
                visualTransform.localPosition = visualStartPosition;
            }
            
            // After movement completes, check if there's still active input
            if (inputActive && !isMoving)
            {
                // Use the last valid input for the next movement
                ProcessMovementInput(lastValidInput);
            }
        }
    }
    
    private float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
    
    // Public method to manually trigger movement
    public void MoveInDirection(Vector2 direction)
    {
        if (!isMoving && direction.magnitude > inputDeadzone)
        {
            ProcessMovementInput(direction.normalized);
        }
    }
    
    // Clean up event subscriptions
    private void OnDestroy()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMovementPerformed;
            moveAction.canceled -= OnMovementCanceled;
        }
    }
    
    // Helper methods for debugging and external control
    public bool IsMoving() => isMoving;
    public Vector3 GetTargetPosition() => targetPosition;
    
    // For debugging
    private void OnDrawGizmosSelected()
    {
        if (playerCollider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(playerCollider.bounds.center, playerCollider.bounds.size);
            
            if (Application.isPlaying && isMoving)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(startPosition, targetPosition);
                Gizmos.DrawWireSphere(targetPosition, 0.2f);
                
                // Draw global direction vectors
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, globalForward * 2f);
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, globalRight * 2f);
            }
        }
    }
}