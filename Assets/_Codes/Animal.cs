using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Animal : InteractableBlock
{

    public MaskType AnimalType;

    public UnityEvent OnEaten;
    
    private SheepCounter _sheepCounter;

    private void Start()
    {
        _sheepCounter = SheepCounter.instance;
        _sheepCounter.RegisterAnimal();
    }
    public override bool CanPlayerMoveHere(InteractionData interactionData)
    {
       //if(interactionData.currentMask==MaskType.Wolf)
        return true;

       return false;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (isDead)
            return;
        
        if (other.tag == "Player")
        {
            isDead = true;
            OnEaten?.Invoke();
            _sheepCounter.EatAnimal();
            StartCoroutine(dieDelay());
        }
    }

    private bool isDead = false;
    public float deathDelay = .3f;
    IEnumerator dieDelay()
    {
        yield return new WaitForSeconds(deathDelay);
        Destroy(gameObject);
    }
}
