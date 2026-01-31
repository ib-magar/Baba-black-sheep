using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float jumpHeight = 0.5f;
    [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("References")]
    [SerializeField] private Collider playerCollider;
    
    // Movement state
    private bool isMoving = false;
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private float moveTimer = 0f;
    
    // Input
    private Vector2 moveInput;
    private bool inputReceived = false;
    
    // Components
    private PlayerInput playerInput;
    private InputAction moveAction;
    
    // Isometric direction vectors (45-degree isometric)
    private Vector3 isoForward = new Vector3(0.5f, 0, 0.5f).normalized;
    private Vector3 isoRight = new Vector3(0.5f, 0, -0.5f).normalized;
    
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = gameObject.AddComponent<PlayerInput>();
        }
        
        // Get or create input actions
        InitializeInputActions();
        
        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider>();
        }
    }

    private void Start()
    {
        Debug.Log("Error");
    }

    private void InitializeInputActions()
    {
        // Create input actions if they don't exist
        InputActionAsset asset = playerInput.actions;
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<InputActionAsset>();
            
            // Create action map
            var map = new InputActionMap("Player");
            
            // Create move action
            moveAction = map.AddAction("Move", binding: "<Gamepad>/leftStick");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            
            asset.AddActionMap(map);
            playerInput.actions = asset;
        }
        else
        {
            moveAction = asset.FindAction("Player/Move");
        }
    }
    
    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.Enable();
        }
    }
    
    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.Disable();
        }
    }
    
    private void Update()
    {
        // Read input
        if (moveAction != null && !isMoving)
        {
            moveInput = moveAction.ReadValue<Vector2>();
            
            // Check for input (with deadzone)
            if (moveInput.magnitude > 0.1f)
            {
                inputReceived = true;
            }
        }
        
        // Handle movement
        if (!isMoving && inputReceived)
        {
            ProcessMovementInput();
            inputReceived = false;
        }
        
        // Perform movement animation
        if (isMoving)
        {
            UpdateMovement();
        }
    }
    
    private void ProcessMovementInput()
    {
        // Determine movement direction based on input
        Vector3 movementDirection = Vector3.zero;
        
        // For isometric movement, we interpret input differently
        // In isometric view, up/down moves along world X+Z, left/right moves along world X-Z
        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
        {
            // Horizontal movement
            if (moveInput.x > 0)
            {
                movementDirection = isoRight; // Right in isometric
            }
            else if (moveInput.x < 0)
            {
                movementDirection = -isoRight; // Left in isometric
            }
        }
        else
        {
            // Vertical movement
            if (moveInput.y > 0)
            {
                movementDirection = isoForward; // Up/Forward in isometric
            }
            else if (moveInput.y < 0)
            {
                movementDirection = -isoForward; // Down/Back in isometric
            }
        }
        
        // Normalize and apply grid size
        movementDirection = movementDirection.normalized * gridSize;
        
        // Check if movement is possible
        if (movementDirection != Vector3.zero && CanMoveToPosition(movementDirection))
        {
            StartMovement(movementDirection);
        }
    }
    
    private bool CanMoveToPosition(Vector3 direction)
    {
        Vector3 targetPos = transform.position + direction;
        
        // Adjust for collider size
        Vector3 colliderCenter = transform.position + playerCollider.bounds.center - transform.position;
        
        // Check for obstacles at target position
        RaycastHit hit;
        Vector3 raycastStart = colliderCenter + Vector3.up * 0.5f; // Start slightly above
        
        // Cast a box that matches the player's collider size
        Vector3 halfExtents = playerCollider.bounds.extents * 0.9f; // Slightly smaller to avoid edge cases
        halfExtents.y = 0.1f; // Keep vertical check small
        
        if (Physics.BoxCast(raycastStart, halfExtents, direction.normalized, 
            out hit, Quaternion.identity, gridSize, obstacleLayer))
        {
            return false;
        }
        
        // Also check if there's ground at the target position
        Vector3 groundCheckPos = targetPos + Vector3.up * 0.5f;
        if (!Physics.Raycast(groundCheckPos, Vector3.down, 1f, groundLayer))
        {
            return false;
        }
        
        return true;
    }
    
    private void StartMovement(Vector3 direction)
    {
        isMoving = true;
        moveTimer = 0f;
        
        startPosition = transform.position;
        targetPosition = startPosition + direction;
        
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
        
        // Calculate position with jump arc
        Vector3 horizontalPos = Vector3.Lerp(startPosition, targetPosition, easedT);
        float jumpProgress = Mathf.Sin(easedT * Mathf.PI);
        Vector3 verticalOffset = Vector3.up * (jumpHeight * jumpProgress);
        
        transform.position = horizontalPos + verticalOffset;
        
        // Check if movement is complete
        if (t >= 1f)
        {
            isMoving = false;
            transform.position = targetPosition; // Snap to exact grid position
        }
    }
    
    private float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
    
    // Public method to trigger movement from other scripts
    public void Move(Vector2 inputDirection)
    {
        if (!isMoving)
        {
            moveInput = inputDirection;
            inputReceived = true;
        }
    }
    
    // For debugging
    private void OnDrawGizmosSelected()
    {
        if (playerCollider != null && Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(playerCollider.bounds.center, playerCollider.bounds.size);
            
            if (isMoving)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(startPosition, targetPosition);
                Gizmos.DrawWireSphere(targetPosition, 0.2f);
            }
        }
    }
}