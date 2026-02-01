using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class AnimalBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private Transform[] walkingPoints;
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Timing Settings")]
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 3f;
    [SerializeField] private float minWalkTime = 2f;
    [SerializeField] private float maxWalkTime = 5f;
    
    [Header("Gizmo Settings")]
    [SerializeField] private Color pathColor = Color.green;
    [SerializeField] private Color pointColor = Color.red;
    [SerializeField] private float pointSize = 0.3f;
    
    private Animator animator;
    private int currentPointIndex = 0;
    private bool isWalking = false;
    private bool isWaiting = false;
    
    // Animator parameter names
    private const string WALK_TRIGGER = "Walk";
    private const string IDLE_TRIGGER = "Idle";
    
    private void Start()
    {
        animator = GetComponent<Animator>();
        
        if (walkingPoints == null || walkingPoints.Length == 0)
        {
            Debug.LogError("No walking points assigned to AnimalBehavior!");
            enabled = false;
            return;
        }
        
        // Start in idle state at first point
        transform.position = walkingPoints[0].position;
        StartCoroutine(BehaviorCycle());
    }
    
    private void Update()
    {
        if (isWalking && !isWaiting)
        {
            MoveToNextPoint();
        }
    }
    
    private IEnumerator BehaviorCycle()
    {
        while (true)
        {
            // Randomly decide to walk or stop
            bool shouldWalk = Random.Range(0, 2) == 1;
            
            if (shouldWalk)
            {
                // Start walking
                isWalking = true;
                isWaiting = false;
                animator.SetTrigger(WALK_TRIGGER);
                
                // Walk for random time
                float walkDuration = Random.Range(minWalkTime, maxWalkTime);
                yield return new WaitForSeconds(walkDuration);
                
                // Stop at current point
                isWalking = false;
                animator.SetTrigger(IDLE_TRIGGER);
                
                // Wait for random time
                float waitDuration = Random.Range(minWaitTime, maxWaitTime);
                isWaiting = true;
                yield return new WaitForSeconds(waitDuration);
                isWaiting = false;
                
                // Move to next point for next cycle
                currentPointIndex = (currentPointIndex + 1) % walkingPoints.Length;
            }
            else
            {
                // Stop at current position
                isWalking = false;
                animator.SetTrigger(IDLE_TRIGGER);
                
                // Wait for random time
                float waitDuration = Random.Range(minWaitTime, maxWaitTime);
                isWaiting = true;
                yield return new WaitForSeconds(waitDuration);
                isWaiting = false;
            }
        }
    }
    
    private void MoveToNextPoint()
    {
        int targetIndex = (currentPointIndex + 1) % walkingPoints.Length;
        Transform targetPoint = walkingPoints[targetIndex];
        
        // Move towards the point
        Vector3 direction = targetPoint.position - transform.position;
        direction.y = 0; // Keep movement on ground level
        
        if (direction.magnitude > 0.1f)
        {
            // Rotate towards target
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // Move forward
            transform.position += transform.forward * walkSpeed * Time.deltaTime;
        }
        else
        {
            // Reached the point, update index
            currentPointIndex = targetIndex;
            transform.position = targetPoint.position;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (walkingPoints == null || walkingPoints.Length < 2)
            return;
        
        // Draw path lines
        Gizmos.color = pathColor;
        for (int i = 0; i < walkingPoints.Length; i++)
        {
            if (walkingPoints[i] == null) continue;
            
            int nextIndex = (i + 1) % walkingPoints.Length;
            if (walkingPoints[nextIndex] == null) continue;
            
            Gizmos.DrawLine(walkingPoints[i].position, walkingPoints[nextIndex].position);
        }
        
        // Draw points
        Gizmos.color = pointColor;
        foreach (Transform point in walkingPoints)
        {
            if (point != null)
            {
                Gizmos.DrawSphere(point.position, pointSize);
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (walkingPoints == null || walkingPoints.Length < 2)
            return;
        
        // Draw smaller, always visible path
        Gizmos.color = new Color(pathColor.r, pathColor.g, pathColor.b, 0.3f);
        for (int i = 0; i < walkingPoints.Length; i++)
        {
            if (walkingPoints[i] == null) continue;
            
            int nextIndex = (i + 1) % walkingPoints.Length;
            if (walkingPoints[nextIndex] == null) continue;
            
            Gizmos.DrawLine(walkingPoints[i].position, walkingPoints[nextIndex].position);
        }
        
        // Draw smaller points
        Gizmos.color = new Color(pointColor.r, pointColor.g, pointColor.b, 0.3f);
        foreach (Transform point in walkingPoints)
        {
            if (point != null)
            {
                Gizmos.DrawSphere(point.position, pointSize * 0.5f);
            }
        }
    }
}