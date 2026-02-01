using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class GuardSimple : MonoBehaviour
{
    // Define enum inside the class
    public enum State { Rotating, Moving, Waiting }
    
    [Header("Movement Settings")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotateSpeed = 120f;
    [SerializeField] private float pauseTime = 1f;
    [SerializeField] private float stopDistance = 0.1f;
    [SerializeField] private float faceAngle = 2f;
    
    private Animator animator;
    private int currentWaypoint = 0;
    private State currentState = State.Waiting;
    
    private const string WALK_ANIM_BOOL = "IsWalking";
    
    private void Start()
    {
        animator = GetComponent<Animator>();
        
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("Assign waypoints to Guard!");
            enabled = false;
            return;
        }
        
        transform.position = waypoints[0].position;
        StartCoroutine(PatrolLoop());
    }
    
    private IEnumerator PatrolLoop()
    {
        while (true)
        {
            for (int i = 0; i < waypoints.Length; i++)
            {
                currentWaypoint = i;
                Transform target = waypoints[i];
                
                if (target == null) continue;
                
                // Phase 1: Rotate to face target
                yield return StartCoroutine(RotateToFace(target));
                
                // Phase 2: Move to target
                yield return StartCoroutine(MoveToTarget(target));
                
                // Phase 3: Wait at target
                yield return StartCoroutine(WaitAtTarget());
            }
        }
    }
    
    private IEnumerator RotateToFace(Transform target)
    {
        currentState = State.Rotating;
        animator.SetBool(WALK_ANIM_BOOL, false);
        
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0;
        
        if (direction == Vector3.zero) yield break;
        
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        
        while (Quaternion.Angle(transform.rotation, targetRotation) > faceAngle)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );
            yield return null;
        }
        
        transform.rotation = targetRotation;
    }
    
    private IEnumerator MoveToTarget(Transform target)
    {
        currentState = State.Moving;
        animator.SetBool(WALK_ANIM_BOOL, true);
        
        while (Vector3.Distance(transform.position, target.position) > stopDistance)
        {
            // Move straight forward (we should be facing the target)
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
            
            // Check if we need to re-adjust rotation
            Vector3 currentDir = (target.position - transform.position).normalized;
            currentDir.y = 0;
            
            if (Vector3.Angle(transform.forward, currentDir) > faceAngle)
            {
                // Stop and rotate again
                animator.SetBool(WALK_ANIM_BOOL, false);
                yield return StartCoroutine(RotateToFace(target));
                animator.SetBool(WALK_ANIM_BOOL, true);
            }
            
            yield return null;
        }
        
        // Snap to exact position
        transform.position = target.position;
    }
    
    private IEnumerator WaitAtTarget()
    {
        currentState = State.Waiting;
        animator.SetBool(WALK_ANIM_BOOL, false);
        yield return new WaitForSeconds(pauseTime);
    }
    
    // Public getters
    public State GetCurrentState() => currentState;
    public bool IsMoving() => currentState == State.Moving;
    public bool IsRotating() => currentState == State.Rotating;
    public bool IsWaiting() => currentState == State.Waiting;
    public int GetCurrentWaypoint() => currentWaypoint;
}