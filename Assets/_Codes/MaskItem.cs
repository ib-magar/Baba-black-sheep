using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

public class MaskItem : MonoBehaviour
{
    [Header("Mask Properties")]
    [SerializeField, ReadOnly]
    private MaskType maskType;
    
    [SerializeField, ReadOnly]
    private int orderIndex;
    
    [SerializeField, ReadOnly]
    private Vector3 visualLocalPosition;
    
    [SerializeField, ReadOnly]
    private Vector3 visualLocalScale;

    [Header("Item Settings")]
    [SerializeField, Tooltip("Can this mask item be picked up and re-equipped?")]
    private bool canBeReequipped = true;
    
    [SerializeField, Tooltip("Can this item be equipped right now?")]
    private bool canEquipNow = false;
    
    [SerializeField, Tooltip("Reference to the player mask system")]
    private PlayerMask playerMask;

    [Header("Trigger Settings")]
    [SerializeField, Tooltip("Collider to check for player overlap")]
    private Collider itemCollider;
    
    [SerializeField, Tooltip("Layer mask for checking player collision")]
    private LayerMask playerLayer;
    
    [SerializeField, Tooltip("Check radius for player detection")]
    private float checkRadius = 1f;

    [Header("Visual")]
    [SerializeField, Tooltip("The visual child transform (for applying visual data)")]
    private Transform visualTransform;

    [Header("Animation")]
    public Vector3 initialOffset = new Vector3(0, 1, 0);
    [SerializeField] private float dropAnimationDuration = 0.15f;
    [SerializeField] private float equipAnimationDuration = 0.15f;

    private bool isInitialized = false;
    private bool isAnimating = false;

    public void Initialize(MaskType type, MaskVisualData visualData)
    {
        maskType = type;
        orderIndex = visualData.orderIndex;
        visualLocalPosition = visualData.localPosition;
        visualLocalScale = visualData.localScale;
        
        // Find visual transform if not assigned
        if (visualTransform == null && transform.childCount > 0)
        {
            visualTransform = transform.GetChild(0);
        }
        
        // Apply visual data ONLY to the child transform
        if (visualTransform != null)
        {
            visualTransform.localPosition = visualLocalPosition;
            visualTransform.localScale = visualLocalScale;
            
            // Ensure root transform stays at defaults
            transform.localScale = Vector3.one;
        }
        
        isInitialized = true;
        
        // Try to find player mask if not assigned
        if (playerMask == null)
        {
            playerMask = FindObjectOfType<PlayerMask>();
        }
        
        // Set up collider if not assigned
        if (itemCollider == null)
        {
            itemCollider = GetComponent<Collider>();
        }
        
        Debug.Log($"MaskItem initialized: {maskType} (Order: {orderIndex})");
    }

    IEnumerator Start()
    {
        // Wait a frame for physics to settle
        yield return null;
        
        // Play drop animation
        if (visualTransform != null)
        {
            isAnimating = true;
            Vector3 targetPos = visualTransform.position + initialOffset;
            visualTransform.DOMove(targetPos, dropAnimationDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => {
                    isAnimating = false;
                    Debug.Log($"Drop animation completed for {maskType}");
                });
        }
        
        // Wait for animation to complete
        yield return new WaitForSeconds(dropAnimationDuration);
        
        // Check if player is inside the collider at spawn
        CheckForPlayerOverlap();
    }

    private void CheckForPlayerOverlap()
    {
        if (itemCollider == null) return;
        
        // Use physics overlap to check if player is inside the collider
        Collider[] overlappingColliders = Physics.OverlapBox(
            itemCollider.bounds.center, 
            itemCollider.bounds.extents, 
            itemCollider.transform.rotation, 
            playerLayer
        );
        
        bool playerIsOverlapping = false;
        foreach (var collider in overlappingColliders)
        {
            if (collider.CompareTag("Player"))
            {
                playerIsOverlapping = true;
                break;
            }
        }
        
        // Set canEquipNow based on whether player is overlapping
        canEquipNow = !playerIsOverlapping;
        
        if (playerIsOverlapping)
        {
            Debug.Log($"Player is overlapping {maskType} mask. Cannot equip yet.");
        }
        else
        {
            Debug.Log($"Player is not overlapping {maskType} mask. Can equip now.");
        }
    }

    [Button("Re-equip Mask")]
    [EnableIf("@canBeReequipped && playerMask != null && canEquipNow && !isAnimating")]
    public bool ReequipMask()
    {
        if (!canBeReequipped)
        {
            Debug.Log($"Cannot re-equip {maskType}: Item is not reequippable");
            return false;
        }
        
        if (playerMask == null)
        {
            Debug.LogWarning($"Cannot re-equip {maskType}: PlayerMask reference not found");
            return false;
        }

        if (!canEquipNow)
        {
            Debug.Log($"Cannot re-equip {maskType}: Cannot equip at this moment");
            return false;
        }

        if (isAnimating)
        {
            Debug.Log($"Cannot re-equip {maskType}: Animation in progress");
            return false;
        }

        StartCoroutine(EquipAnimation());
        return true;
    }
    
    IEnumerator EquipAnimation()
    {
        isAnimating = true;
        
        if (visualTransform != null)
        {
            Vector3 targetPos = visualTransform.position - initialOffset;
            visualTransform.DOMove(targetPos, equipAnimationDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() => {
                    // After animation completes, actually equip the mask
                    bool success = playerMask.EquipMaskFromItem(maskType);
                    if (success)
                    {
                        Debug.Log($"Re-equipped {maskType}");
                        Destroy(gameObject);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to re-equip {maskType}");
                        isAnimating = false;
                    }
                });
        }
        else
        {
            // If no visual transform, just equip immediately
            bool success = playerMask.EquipMaskFromItem(maskType);
            if (success)
            {
                Debug.Log($"Re-equipped {maskType}");
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning($"Failed to re-equip {maskType}");
                isAnimating = false;
            }
        }
        
        yield return new WaitForSeconds(equipAnimationDuration);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canBeReequipped || isAnimating) return;
        
        // Check if the player entered the trigger
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player entered {maskType} mask trigger");
            // Don't equip immediately, just note the presence
            ReequipMask();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!canBeReequipped || isAnimating) return;
        
        // Check if the player exited the trigger
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player exited {maskType} mask trigger");
            
            // Wait a frame then check if player is still overlapping
            // This handles edge cases where player might exit and re-enter quickly
            StartCoroutine(CheckAfterExit());
        }
    }
    
    IEnumerator CheckAfterExit()
    {
        yield return null; // Wait one frame
        
        // Perform overlap check to see if player is truly gone
        CheckForPlayerOverlap();
        
        // If player has left and we can equip now, optionally auto-equip
        if (canEquipNow)
        {
            Debug.Log($"Player has left {maskType} mask area. Ready for pickup.");
            // Optional: Auto-equip when player leaves
            // ReequipMask();
        }
    }

    [Button("Manual Check Overlap")]
    private void ManualCheckOverlap()
    {
        CheckForPlayerOverlap();
        Debug.Log($"Manual check: canEquipNow = {canEquipNow}");
    }

    [Button("Force Enable Equip")]
    private void ForceEnableEquip()
    {
        canEquipNow = true;
        Debug.Log($"Force enabled equip for {maskType}");
    }

    [Button("Force Disable Equip")]
    private void ForceDisableEquip()
    {
        canEquipNow = false;
        Debug.Log($"Force disabled equip for {maskType}");
    }

    [Button("Print Item Info")]
    public void PrintItemInfo()
    {
        if (!isInitialized)
        {
            Debug.Log("MaskItem not initialized");
            return;
        }
        
        string info = $"Mask Item Info:\n" +
                     $"Type: {maskType}\n" +
                     $"Order Index: {orderIndex}\n" +
                     $"Visual Local Position: {visualLocalPosition}\n" +
                     $"Visual Local Scale: {visualLocalScale}\n" +
                     $"Root Position: {transform.position}\n" +
                     $"Root Scale: {transform.localScale}\n" +
                     $"Can Be Re-equipped: {canBeReequipped}\n" +
                     $"Can Equip Now: {canEquipNow}\n" +
                     $"Is Animating: {isAnimating}\n" +
                     $"PlayerMask Reference: {(playerMask != null ? "Set" : "Not Set")}";
        
        Debug.Log(info);
    }

    [Button("Reset Visual Transform")]
    private void ResetVisualTransform()
    {
        if (visualTransform != null && isInitialized)
        {
            visualTransform.localPosition = visualLocalPosition;
            visualTransform.localScale = visualLocalScale;
            transform.localScale = Vector3.one;
            Debug.Log($"Reset visual transform for {maskType}");
        }
    }

    // Getters for external scripts
    public MaskType GetMaskType() => maskType;
    public int GetOrderIndex() => orderIndex;
    public Vector3 GetVisualLocalPosition() => visualLocalPosition;
    public Vector3 GetVisualLocalScale() => visualLocalScale;
    public bool CanBeReequipped() => canBeReequipped;
    public bool CanEquipNow() => canEquipNow;
    public bool IsAnimating() => isAnimating;

    private void OnValidate()
    {
        // Auto-find visual transform if not set
        if (visualTransform == null && transform.childCount > 0)
        {
            visualTransform = transform.GetChild(0);
        }
        
        // Auto-find collider if not set
        if (itemCollider == null)
        {
            itemCollider = GetComponent<Collider>();
        }
        
        // Set player layer if not configured
        if (playerLayer.value == 0)
        {
            playerLayer = LayerMask.GetMask("Player");
            if (playerLayer.value == 0)
            {
                playerLayer = LayerMask.GetMask("Default");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (itemCollider != null)
        {
            Gizmos.color = canEquipNow ? Color.green : Color.red;
            Gizmos.matrix = Matrix4x4.TRS(
                itemCollider.bounds.center, 
                itemCollider.transform.rotation, 
                Vector3.one
            );
            Gizmos.DrawWireCube(Vector3.zero, itemCollider.bounds.size);
        }
    }
}