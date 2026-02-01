using System;
using UnityEngine;
using System.Collections;
public class Hazard : MonoBehaviour
{
    private LevelManager yoyo_whats_that;

    private void Start()
    {
        yoyo_whats_that = SheepCounter.instance.GetComponent<LevelManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
             yoyo_whats_that.GotfuckedUp();
        }
    }
    public float deathDelay = .3f;
    IEnumerator dieDelay()
    {
        yield return new WaitForSeconds(deathDelay);
        
        Destroy(gameObject);
    }
}
