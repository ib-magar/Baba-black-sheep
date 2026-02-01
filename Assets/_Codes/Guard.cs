using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class Guard : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float waitTimeAtPoint = 1f;
    [SerializeField] private bool randomizeWaitTime = false;
    [SerializeField] private float minWaitTime = 0.5f;
    [SerializeField] private float maxWaitTime = 2f;
    
    [Header("Gizmo Settings")]
    [SerializeField] private Color pathColor = Color.blue;
    [SerializeField] private Color pointColor = Color.yellow;
    [SerializeField] private float pointSize = 0.4f;
    [SerializeField] private bool showPathAlways = true;
    
    private Animator animator;
    private int currentPointIndex = 0;
    private bool isMoving = true;
    private bool isWaiting = false;
    
    // Animator parameter names
    private const string MOVE_SPEED_PARAM = "MoveSpeed";
    private const string IS_MOVING_PARAM = "IsMoving";
    
    private void Start()
    {
        animator = GetComponent<Animator>();
        
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError("No patrol points assigned to Guard!");
            enabled = false;
            return;
        }
        
        // Position at first point
        if (patrolPoints[0] != null)
        {
            transform.position = patrolPoints[0].position;
        }
        
        StartCoroutine(PatrolRoutine());
    }
    
    private void Update()
    {
        if (isMoving && !isWaiting && patrolPoints.Length > 0)
        {
            MoveToNextPoint();
        }
    }
    
    private IEnumerator PatrolRoutine()
    {
        while (true)
        {
            // Move to each point in sequence
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                currentPointIndex = i;
                
                // Move to current point
                isMoving = true;
                isWaiting = false;
                UpdateAnimator();
                
                // Wait until we reach the point (handled in Update)
                while (Vector3.Distance(transform.position, patrolPoints[i].position) > 0.1f)
                {
                    yield return null;
                }
                
                // Arrived at point
                transform.position = patrolPoints[i].position;
                
                // Wait at point
                isMoving = false;
                isWaiting = true;
                UpdateAnimator();
                
                float waitTime = waitTimeAtPoint;
                if (randomizeWaitTime)
                {
                    waitTime = Random.Range(minWaitTime, maxWaitTime);
                }
                
                yield return new WaitForSeconds(waitTime);
            }
        }
    }
    
    private void MoveToNextPoint()
    {
        if (currentPointIndex >= patrolPoints.Length || patrolPoints[currentPointIndex] == null)
            return;
            
        Transform targetPoint = patrolPoints[currentPointIndex];
        Vector3 direction = targetPoint.position - transform.position;
        direction.y = 0; // Keep movement on ground level
        
        if (direction.magnitude > 0.1f)
        {
            // Rotate towards target
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // Move forward
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
            
            // Calculate normalized movement speed for animation blending
            float currentSpeed = moveSpeed;
            float normalizedSpeed = Mathf.Clamp01(currentSpeed / 5f); // Normalize to 0-1 range
            animator.SetFloat(MOVE_SPEED_PARAM, normalizedSpeed);
        }
    }
    
    private void UpdateAnimator()
    {
        animator.SetBool(IS_MOVING_PARAM, isMoving && !isWaiting);
        
        if (!isMoving || isWaiting)
        {
            animator.SetFloat(MOVE_SPEED_PARAM, 0f);
        }
    }
    
    // Public method to manually set patrol points
    public void SetPatrolPoints(Transform[] newPoints)
    {
        if (newPoints != null && newPoints.Length > 0)
        {
            patrolPoints = newPoints;
            currentPointIndex = 0;
            
            if (patrolPoints[0] != null)
            {
                transform.position = patrolPoints[0].position;
            }
        }
    }
    
    // Public method to get current patrol state
    public bool IsMoving() => isMoving && !isWaiting;
    public bool IsWaiting() => isWaiting;
    public int GetCurrentPointIndex() => currentPointIndex;
    
    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showPathAlways)
        {
            DrawGizmos();
        }
    }
    
    private void OnDrawGizmos()
    {
        if (showPathAlways)
        {
            DrawGizmos();
        }
    }
    
    private void DrawGizmos()
    {
        if (patrolPoints == null || patrolPoints.Length < 2)
            return;
        
        // Draw path lines
        Gizmos.color = pathColor;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;
            
            int nextIndex = (i + 1) % patrolPoints.Length;
            if (patrolPoints[nextIndex] == null) continue;
            
            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
            
            // Draw arrow in the middle of the line to show direction
            Vector3 lineCenter = Vector3.Lerp(patrolPoints[i].position, patrolPoints[nextIndex].position, 0.5f);
            Vector3 lineDirection = (patrolPoints[nextIndex].position - patrolPoints[i].position).normalized;
            DrawArrow(lineCenter, lineDirection, 0.5f);
        }
        
        // Draw points with numbers
        Gizmos.color = pointColor;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] != null)
            {
                Gizmos.DrawSphere(patrolPoints[i].position, pointSize);
                
                // Draw point number above the point
                GUIStyle style = new GUIStyle();
                style.normal.textColor = pointColor;
                UnityEditor.Handles.Label(patrolPoints[i].position + Vector3.up * 0.5f, i.ToString(), style);
            }
        }
        
        // Draw current target point if moving
        if (Application.isPlaying && isMoving && currentPointIndex < patrolPoints.Length && patrolPoints[currentPointIndex] != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(patrolPoints[currentPointIndex].position, pointSize * 1.5f);
        }
    }
    
    private void DrawArrow(Vector3 position, Vector3 direction, float size)
    {
        Gizmos.DrawRay(position, direction * size);
        
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 30, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 30, 0) * Vector3.forward;
        
        Gizmos.DrawRay(position + direction * size, right * size * 0.5f);
        Gizmos.DrawRay(position + direction * size, left * size * 0.5f);
    }
    #endif
}