using UnityEngine;
using Sirenix.OdinInspector;

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
    
    [SerializeField, Tooltip("Reference to the player mask system")]
    private PlayerMask playerMask;

    [Header("Visual")]
    [SerializeField, Tooltip("The visual child transform (for applying visual data)")]
    private Transform visualTransform;

    private bool isInitialized = false;

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
        
        Debug.Log($"MaskItem initialized: {maskType} (Order: {orderIndex})");
    }

    [Button("Re-equip Mask")]
    [EnableIf("@canBeReequipped && playerMask != null")]
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
        
        bool success = playerMask.EquipMaskFromItem(maskType);
        
        if (success)
        {
            Debug.Log($"Re-equipped {maskType}");
            
            // Destroy the item after successful re-equip
            Destroy(gameObject);
            return true;
        }
        
        Debug.LogWarning($"Failed to re-equip {maskType}");
        return false;
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

    // Optional: Add interaction trigger for player
    private void OnTriggerEnter(Collider other)
    {
        if (!canBeReequipped) return;
        
        // Check if the player interacted with the item
        // You can implement your own interaction logic here
        if (other.CompareTag("Player"))
        {
            // Auto-re-equip when player touches the item
            ReequipMask();
        }
    }

    // Getters for external scripts
    public MaskType GetMaskType() => maskType;
    public int GetOrderIndex() => orderIndex;
    public Vector3 GetVisualLocalPosition() => visualLocalPosition;
    public Vector3 GetVisualLocalScale() => visualLocalScale;
    public bool CanBeReequipped() => canBeReequipped;

    private void OnValidate()
    {
        // Auto-find visual transform if not set
        if (visualTransform == null && transform.childCount > 0)
        {
            visualTransform = transform.GetChild(0);
        }
    }
}