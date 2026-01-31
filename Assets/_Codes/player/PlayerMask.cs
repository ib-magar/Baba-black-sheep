using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

public enum MaskType
{
    Default,
    Mask1,
    Mask2,
    Mask3,
    Mask4
}

public class PlayerMask : MonoBehaviour
{
    // Event for mask stack changes
    public event Action OnMaskStackUpdated;
    
    [Header("Mask Stack Configuration")]
    [SerializeField, ReadOnly]
    private List<MaskType> maskStack = new List<MaskType>();

    [Header("Initial Masks")]
    [SerializeField, Tooltip("Masks that are added at the start")]
    private List<MaskType> initialMasks = new List<MaskType> { MaskType.Default };

    [Header("Debug Information")]
    [SerializeField, ReadOnly, LabelText("Current Mask (Outermost)")]
    private MaskType currentTopMask = MaskType.Default;
    
    [SerializeField, ReadOnly, LabelText("Mask Count")]
    private int maskCount = 0;

    private void Awake()
    {
        InitializeMaskStack();
    }

    private void InitializeMaskStack()
    {
        maskStack.Clear();
        
        // Add initial masks (ensuring no duplicates)
        foreach (var mask in initialMasks)
        {
            if (!maskStack.Contains(mask))
            {
                maskStack.Add(mask);
            }
        }
        
        // Ensure at least one mask exists
        if (maskStack.Count == 0)
        {
            maskStack.Add(MaskType.Default);
        }
        
        UpdateDebugInfo();
        OnMaskStackUpdated?.Invoke();
    }

    [Button("Add Mask", ButtonSizes.Medium)]
    [HorizontalGroup("MaskOperations")]
    public bool AddMask(MaskType maskToAdd)
    {
        // Check if mask already exists in stack
        if (maskStack.Contains(maskToAdd))
        {
            Debug.LogWarning($"Cannot add {maskToAdd}: Mask already equipped.");
            return false;
        }

        maskStack.Add(maskToAdd);
        UpdateDebugInfo();
        OnMaskStackUpdated?.Invoke();
        Debug.Log($"Added mask: {maskToAdd}. Total masks: {maskStack.Count}");
        PrintCurrentMask();
        return true;
    }

    [Button("Remove Outer Mask", ButtonSizes.Medium)]
    [HorizontalGroup("MaskOperations")]
    [EnableIf("@CanRemoveMask()")]
    public bool RemoveOuterMask()
    {
        if (!CanRemoveMask())
        {
            Debug.LogWarning("Cannot remove mask. At least one mask must remain.");
            return false;
        }

        MaskType removedMask = maskStack[maskStack.Count - 1];
        maskStack.RemoveAt(maskStack.Count - 1);
        UpdateDebugInfo();
        OnMaskStackUpdated?.Invoke();
        Debug.Log($"Removed outer mask: {removedMask}. Remaining masks: {maskStack.Count}");
        PrintCurrentMask();
        return true;
    }

    private bool CanRemoveMask()
    {
        return maskStack.Count > 1;
    }

    [Button("Print Current Mask", ButtonSizes.Medium)]
    [HorizontalGroup("DebugButtons")]
    public void PrintCurrentMask()
    {
        Debug.Log($"Current outermost mask: {currentTopMask}");
    }

    [Button("Print Current Stack", ButtonSizes.Medium)]
    [HorizontalGroup("DebugButtons")]
    public void PrintCurrentStack()
    {
        string stackString = $"Current Mask Stack (Top to Bottom) - Total: {maskStack.Count}\n";
        
        for (int i = maskStack.Count - 1; i >= 0; i--)
        {
            string position = $"Layer {maskStack.Count - i}";
            string maskName = maskStack[i].ToString();
            string indicator = (i == maskStack.Count - 1) ? "[OUTERMOST] " : "";
            string lastIndicator = (i == 0) ? " [LAST - CANNOT REMOVE]" : "";
            
            stackString += $"{position}: {indicator}{maskName}{lastIndicator}\n";
        }
        
        Debug.Log(stackString);
    }

    [ShowInInspector]
    [BoxGroup("Current Stack Display", centerLabel: true)]
    [PropertyOrder(1)]
    [MultiLineProperty(8)]
    [HideLabel]
    public string StackDisplay
    {
        get
        {
            if (maskStack.Count == 0) return "No masks in stack";
            
            string display = $"╔══════════════════════════════╗\n";
            display += $"║      MASK STACK DISPLAY      ║\n";
            display += $"╠══════════════════════════════╣\n";
            display += $"║ Outer Mask: {currentTopMask.ToString().PadRight(12)} ║\n";
            display += $"║ Total Masks: {maskStack.Count.ToString().PadRight(11)} ║\n";
            display += $"╠══════════════════════════════╣\n";
            
            for (int i = maskStack.Count - 1; i >= 0; i--)
            {
                string maskName = maskStack[i].ToString();
                string position = (i == maskStack.Count - 1) ? "[OUTER]" : $"Layer {maskStack.Count - i}";
                string lastIndicator = (i == 0) ? " (LAST)" : "";
                
                display += $"║ {position.PadRight(8)}: {maskName.PadRight(10)}{lastIndicator.PadRight(13)} ║\n";
            }
            
            display += $"╚══════════════════════════════╝";
            
            return display;
        }
    }

    public MaskType GetCurrentMask()
    {
        return currentTopMask;
    }

    public bool HasMask(MaskType mask)
    {
        return maskStack.Contains(mask);
    }

    public int GetMaskCount()
    {
        return maskStack.Count;
    }

    public List<MaskType> GetMaskStack()
    {
        return new List<MaskType>(maskStack);
    }

    private void UpdateDebugInfo()
    {
        if (maskStack.Count > 0)
        {
            currentTopMask = maskStack[maskStack.Count - 1];
        }
        else
        {
            currentTopMask = MaskType.Default;
        }
        
        maskCount = maskStack.Count;
    }

    [SerializeField, BoxGroup("Debug/Quick Actions"), HideLabel, EnumToggleButtons]
    private MaskType debugAddMask = MaskType.Mask1;
    
    [SerializeField, BoxGroup("Debug/Quick Actions"), HideLabel, EnumToggleButtons]
    private MaskType debugRemoveMask = MaskType.Mask1;

    [Button("Debug Add Selected", ButtonSizes.Medium)]
    [BoxGroup("Debug/Quick Actions")]
    private void DebugAddSelectedMask()
    {
        AddMask(debugAddMask);
    }

    [Button("Debug Remove Selected", ButtonSizes.Medium)]
    [BoxGroup("Debug/Quick Actions")]
    private void DebugRemoveSelectedMask()
    {
        if (maskStack.Count > 1 && maskStack[maskStack.Count - 1] == debugRemoveMask)
        {
            RemoveOuterMask();
        }
        else if (maskStack.Count <= 1)
        {
            Debug.Log($"Cannot remove {debugRemoveMask}. Only one mask remains (cannot remove last mask).");
        }
        else
        {
            Debug.Log($"Cannot remove {debugRemoveMask}. It's not the outermost mask. Current outermost: {currentTopMask}");
        }
    }
}