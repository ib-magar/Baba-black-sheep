using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

// Struct to hold visual data for masks
public struct MaskVisualData
{
    public Vector3 localPosition;
    public Vector3 localScale;
    public int orderIndex;
    public MaskType maskType;
}

public class MaskVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Required]
    private PlayerMask playerMask;
    
    [SerializeField, Required]
    private MaskDataSO maskData;
    
    [Header("Visual Settings")]
    [SerializeField]
    private Transform maskParent;
    
    [SerializeField, Tooltip("Position offset between each mask layer (Y axis for vertical stacking)")]
    private Vector3 positionOffset = new Vector3(0, 0.15f, 0);
    
    [SerializeField, Range(0.01f, 0.3f), Tooltip("Scale increase per additional mask layer")]
    private float scaleIncrement = 0.05f;
    
    [SerializeField, Tooltip("Maximum scale for masks (to prevent them from becoming too large)")]
    private float maxScale = 2.0f;

    [Header("Drop Settings")]
    [SerializeField, Tooltip("Should dropped items maintain their visual scale?")]
    private bool maintainVisualScaleOnDrop = true;
    
    [SerializeField, Tooltip("Offset from player position where mask items spawn")]
    private Vector3 dropSpawnOffset = new Vector3(0, 0.5f, 1f);
    
    [SerializeField, Tooltip("Local position offset for the visual child in dropped items")]
    private Vector3 dropVisualLocalOffset = Vector3.zero;

    [Header("Debug")]
    [SerializeField, ReadOnly]
    private Dictionary<MaskType, GameObject> activeMaskVisuals = new Dictionary<MaskType, GameObject>();
    
    [SerializeField, ReadOnly]
    private Dictionary<MaskType, MaskVisualData> maskVisualDataCache = new Dictionary<MaskType, MaskVisualData>();
    
    [SerializeField, ReadOnly]
    private List<MaskType> currentVisualStack = new List<MaskType>();

    private void OnEnable()
    {
        if (playerMask != null)
        {
            playerMask.OnMaskAdded += OnMaskAdded;
            playerMask.OnMaskRemoved += OnMaskRemoved;
        }
    }

    private void OnDisable()
    {
        if (playerMask != null)
        {
            playerMask.OnMaskAdded -= OnMaskAdded;
            playerMask.OnMaskRemoved -= OnMaskRemoved;
        }
    }

    private void Start()
    {
        if (playerMask == null)
        {
            playerMask = GetComponent<PlayerMask>();
        }
        
        if (playerMask != null)
        {
            UpdateVisuals();
        }
        else
        {
            Debug.LogError("PlayerMask reference not found!");
        }
    }

  
    private void OnMaskAdded(MaskType maskType)
    {
        UpdateVisuals();
    }

    private void OnMaskRemoved(MaskType maskType)
    {
        // Visual destruction is handled in UpdateVisuals
        UpdateVisuals();
    }

    [Button("Update Visuals")]
    public void UpdateVisuals()
    {
        if (playerMask == null || maskData == null || maskParent == null)
        {
            Debug.LogWarning("Missing references in MaskVisualController");
            return;
        }

        List<MaskType> newStack = playerMask.GetMaskStack();
        
        // Skip if stack is empty or only has default mask
        if (newStack.Count == 0 || (newStack.Count == 1 && newStack[0] == MaskType.Default))
        {
            ClearAllVisuals();
            currentVisualStack = new List<MaskType>(newStack);
            maskVisualDataCache.Clear();
            return;
        }
        
        // Destroy visuals for masks that are no longer in the stack
        // But skip Default mask as it should never have a visual
        List<MaskType> masksToRemove = new List<MaskType>();
        foreach (var kvp in activeMaskVisuals)
        {
            if (!newStack.Contains(kvp.Key) || kvp.Key == MaskType.Default)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
                masksToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var maskType in masksToRemove)
        {
            activeMaskVisuals.Remove(maskType);
            maskVisualDataCache.Remove(maskType);
        }

        // Count how many non-default masks we have (for scale calculation)
        int nonDefaultMaskCount = 0;
        foreach (var maskType in newStack)
        {
            if (maskType != MaskType.Default)
            {
                nonDefaultMaskCount++;
            }
        }
        
        // Clear cache and recalculate
        maskVisualDataCache.Clear();
        
        // Update positions and scales for all masks (skip Default mask)
        int visualLayerIndex = 0;
        for (int i = 0; i < newStack.Count; i++)
        {
            MaskType maskType = newStack[i];
            
            // Skip Default mask - it doesn't get a visual
            if (maskType == MaskType.Default)
                continue;
                
            GameObject maskVisual = GetOrCreateMaskVisual(maskType);
            
            if (maskVisual != null)
            {
                // Calculate position (first non-default mask at base, subsequent masks offset)
                Vector3 localPosition = positionOffset * visualLayerIndex;
                maskVisual.transform.localPosition = localPosition;
                
                // Calculate scale: Start at 1.0, increase with each additional mask
                // visualLayerIndex starts at 0 for the first non-default mask
                float scaleFactor = 1.0f + (scaleIncrement * visualLayerIndex);
                scaleFactor = Mathf.Min(scaleFactor, maxScale); // Clamp to max scale
                Vector3 localScale = Vector3.one * scaleFactor;
                maskVisual.transform.localScale = localScale;
                
                // Set order in hierarchy (bottom mask first, top mask last for proper rendering)
                maskVisual.transform.SetSiblingIndex(visualLayerIndex);
                
                // Cache the visual data
                MaskVisualData visualData = new MaskVisualData
                {
                    localPosition = localPosition,
                    localScale = localScale,
                    orderIndex = visualLayerIndex,
                    maskType = maskType
                };
                maskVisualDataCache[maskType] = visualData;
                
                visualLayerIndex++;
            }
        }

        currentVisualStack = new List<MaskType>(newStack);
    }

    public MaskVisualData GetMaskVisualData(MaskType maskType)
    {
        if (maskVisualDataCache.TryGetValue(maskType, out MaskVisualData data))
        {
            return data;
        }
        
        // Return default data if not found in cache
        return new MaskVisualData
        {
            localPosition = Vector3.zero,
            localScale = Vector3.one,
            orderIndex = 0,
            maskType = maskType
        };
    }

    public void DropMaskItem(MaskType maskType, MaskVisualData visualData, Vector3 spawnPosition)
    {
        GameObject maskItemPrefab = maskData.GetMaskItem(maskType);
        if (maskItemPrefab == null)
        {
            Debug.LogWarning($"No mask item prefab found for {maskType}");
            return;
        }

        // Calculate spawn position with offset
        //Vector3 finalSpawnPosition = spawnPosition + dropSpawnOffset;
        Vector3 finalSpawnPosition = transform.position;
        
        // Instantiate the mask item at world position with no rotation (identity)
        GameObject maskItemObj = Instantiate(maskItemPrefab, finalSpawnPosition, Quaternion.identity);
        maskItemObj.name = $"{maskData.GetName(maskType)}_Item";
        
        // Get or add the MaskItem component
        MaskItem maskItem = maskItemObj.GetComponent<MaskItem>();
        if (maskItem == null)
        {
            maskItem = maskItemObj.AddComponent<MaskItem>();
        }
        
        // Initialize the mask item with visual data
        maskItem.Initialize(maskType, visualData);
        
        // Apply visual data ONLY to the first child, NOT to the root object
        if (maskItemObj.transform.childCount > 0)
        {
            Transform visualChild = maskItemObj.transform.GetChild(0);
            
            if (maintainVisualScaleOnDrop)
            {
                // Apply the cached local position and scale to the child
                // Add any additional offset for dropped items
                visualChild.localPosition = visualData.localPosition + dropVisualLocalOffset;
                visualChild.localScale = visualData.localScale;
                
                // Keep the root object at default transform (position set above, scale = 1)
                maskItemObj.transform.localScale = Vector3.one;
            }
        }
        else
        {
            Debug.LogWarning($"Mask item prefab for {maskType} has no child objects. Visual data cannot be applied.");
        }
        
        Debug.Log($"Dropped {maskType} mask item at {finalSpawnPosition}");
    }

    private GameObject GetOrCreateMaskVisual(MaskType maskType)
    {
        // Don't create visual for Default mask
        if (maskType == MaskType.Default)
            return null;

        // Return existing visual if we have it
        if (activeMaskVisuals.TryGetValue(maskType, out GameObject existingVisual))
        {
            return existingVisual;
        }

        // Create new visual
        GameObject prefab = maskData.GetPrefab(maskType);
        if (prefab == null)
        {
            Debug.LogWarning($"No prefab found for mask type: {maskType}");
            return null;
        }

        GameObject newVisual = Instantiate(prefab, maskParent);
        newVisual.name = $"{maskData.GetName(maskType)}_Visual";
        
        // Reset transform (position and scale will be set by UpdateVisuals)
        newVisual.transform.localPosition = Vector3.zero;
        newVisual.transform.localRotation = Quaternion.identity;
        newVisual.transform.localScale = Vector3.one;
        
        activeMaskVisuals[maskType] = newVisual;
        return newVisual;
    }

    [Button("Clear All Visuals")]
    private void ClearAllVisuals()
    {
        foreach (var kvp in activeMaskVisuals)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        activeMaskVisuals.Clear();
        maskVisualDataCache.Clear();
        currentVisualStack.Clear();
    }

    [Button("Print Active Visuals")]
    private void PrintActiveVisuals()
    {
        string debugString = "Active Mask Visuals:\n";
        if (activeMaskVisuals.Count == 0)
        {
            debugString += "No active visuals (Default mask never gets a visual)\n";
        }
        
        foreach (var kvp in activeMaskVisuals)
        {
            debugString += $"{kvp.Key}: {(kvp.Value != null ? kvp.Value.name : "NULL")}\n";
        }
        Debug.Log(debugString);
    }

    [Button("Print Visual Data Cache")]
    private void PrintVisualDataCache()
    {
        string debugString = "Visual Data Cache:\n";
        if (maskVisualDataCache.Count == 0)
        {
            debugString += "Cache is empty\n";
        }
        
        foreach (var kvp in maskVisualDataCache)
        {
            debugString += $"{kvp.Key}: LocalPos={kvp.Value.localPosition}, LocalScale={kvp.Value.localScale}, Order={kvp.Value.orderIndex}\n";
        }
        Debug.Log(debugString);
    }

    [Button("Test Drop at Current Position")]
    private void TestDropCurrentMask()
    {
        if (playerMask == null || playerMask.GetMaskCount() <= 1)
        {
            Debug.Log("Cannot test drop: Not enough masks or player mask not found");
            return;
        }

        MaskType currentTopMask = playerMask.GetCurrentMask();
        if (currentTopMask == MaskType.Default)
        {
            Debug.Log("Cannot drop default mask");
            return;
        }

        MaskVisualData visualData = GetMaskVisualData(currentTopMask);
        DropMaskItem(currentTopMask, visualData, transform.position);
    }

    private void OnValidate()
    {
        if (maskParent == null)
        {
            // Try to find or create a mask parent
            Transform existingParent = transform.Find("MaskVisuals");
            if (existingParent != null)
            {
                maskParent = existingParent;
            }
            else
            {
                GameObject parentObj = new GameObject("MaskVisuals");
                parentObj.transform.SetParent(transform);
                parentObj.transform.localPosition = Vector3.zero;
                maskParent = parentObj.transform;
            }
        }
    }
}