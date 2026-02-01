using System;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "MaskData", menuName = "Mask System/Mask Data")]
public class MaskDataSO : ScriptableObject
{
    [SerializeField]
    private MaskDefinition[] maskDefinitions;

    public GameObject GetPrefab(MaskType maskType)
    {
        // Default mask should not have a prefab
        if (maskType == MaskType.Wolf)
            return null;
            
        foreach (var definition in maskDefinitions)
        {
            if (definition.maskType == maskType)
            {
                return definition.prefab;
            }
        }
        Debug.LogWarning($"No prefab defined for mask type: {maskType}");
        return null;
    }

    public GameObject GetMaskItem(MaskType maskType)
    {
        // Default mask should not have a mask item
        if (maskType == MaskType.Wolf)
            return null;
            
        foreach (var definition in maskDefinitions)
        {
            if (definition.maskType == maskType)
            {
                return definition.maskItem;
            }
        }
        Debug.LogWarning($"No mask item defined for mask type: {maskType}");
        return null;
    }

    public string GetName(MaskType maskType)
    {
        foreach (var definition in maskDefinitions)
        {
            if (definition.maskType == maskType)
            {
                return definition.displayName;
            }
        }
        return maskType.ToString();
    }

    public MaskDefinition[] GetAllDefinitions()
    {
        return maskDefinitions;
    }
    
    // Helper method to check if a mask type should have a visual
    public bool ShouldHaveVisual(MaskType maskType)
    {
        return maskType != MaskType.Wolf && GetPrefab(maskType) != null;
    }

    public MaskMatches[] Matches;

    public bool MaskMatch(MaskType current, MaskType tocheckmaskType)
    {
        foreach (var match in Matches)
        {
            if (current == match.mask)
            {
                if (match.matches.Contains(tocheckmaskType))
                    return true;
            }
        }
        return false;
    }
}

[Serializable]
public struct MaskDefinition
{
    public MaskType maskType;
    public string displayName;
    public GameObject prefab;
    public GameObject maskItem;
}

[System.Serializable]
public struct MaskMatches
{
    public MaskType mask;
    public MaskType[] matches;
}