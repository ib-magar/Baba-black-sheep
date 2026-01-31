using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

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

    [Header("Debug")]
    [SerializeField, ReadOnly]
    private Dictionary<MaskType, GameObject> activeMaskVisuals = new Dictionary<MaskType, GameObject>();
    
    [SerializeField, ReadOnly]
    private List<MaskType> currentVisualStack = new List<MaskType>();

    private void OnEnable()
    {
        if (playerMask != null)
        {
            playerMask.OnMaskStackUpdated += UpdateVisuals;
        }
    }

    private void OnDisable()
    {
        if (playerMask != null)
        {
            playerMask.OnMaskStackUpdated -= UpdateVisuals;
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
                Vector3 position = positionOffset * visualLayerIndex;
                maskVisual.transform.localPosition = position;
                
                // Calculate scale: Start at 1.0, increase with each additional mask
                // visualLayerIndex starts at 0 for the first non-default mask
                float scaleFactor = 1.0f + (scaleIncrement * visualLayerIndex);
                scaleFactor = Mathf.Min(scaleFactor, maxScale); // Clamp to max scale
                maskVisual.transform.localScale = Vector3.one * scaleFactor;
                
                // Set order in hierarchy (bottom mask first, top mask last for proper rendering)
                maskVisual.transform.SetSiblingIndex(visualLayerIndex);
                
                visualLayerIndex++;
            }
        }

        currentVisualStack = new List<MaskType>(newStack);
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

    [Button("Test Scale Progression")]
    private void TestScaleProgression()
    {
        List<MaskType> testStack = playerMask?.GetMaskStack();
        if (testStack == null)
        {
            Debug.Log("No player mask found");
            return;
        }
        
        int nonDefaultCount = 0;
        foreach (var mask in testStack)
        {
            if (mask != MaskType.Default) nonDefaultCount++;
        }
        
        Debug.Log($"Stack Analysis:\n" +
                 $"Total Masks: {testStack.Count}\n" +
                 $"Non-Default Masks: {nonDefaultCount}\n" +
                 $"Scale Progression:");
        
        for (int i = 0; i < nonDefaultCount; i++)
        {
            float scale = 1.0f + (scaleIncrement * i);
            scale = Mathf.Min(scale, maxScale);
            Debug.Log($"  Mask {i + 1}: Scale = {scale:F2}");
        }
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