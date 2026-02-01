using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class SheepCounter : MonoBehaviour
{
    public static SheepCounter instance;
    [SerializeField,ReadOnly] private int TotalAnimals;
    
    [SerializeField] [ReadOnly] private int currentAnimalsEaten = 0;
    [FormerlySerializedAs("OnLevelCompleted")] public UnityEvent OnAllAnimalsEaten;

    private void Awake()
    {
        instance = this;
    }

    public void EatAnimal()
    {
        currentAnimalsEaten++;
        if (currentAnimalsEaten >= TotalAnimals)
        {
            OnAllAnimalsEaten?.Invoke();
        }
    }

    public void RegisterAnimal()
    {
        TotalAnimals++;
    }
}
