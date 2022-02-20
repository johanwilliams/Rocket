using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody2D))]
public class HomingMissile : NetworkBehaviour
{
    private enum State { Launched, Searching, Locked };

    private Rigidbody2D rigidBody;
    private uint shooter;
    [SerializeField] private float destroyAfter = 10f;
    [SerializeField] private float speed = 110;
    [SerializeField] private float rotateSpeed = 200f;


    [Header("Search properties")]
    [SerializeField] [Range(0f, 5f)] private float searchStartTime = 1f;
    [SerializeField] [Range(1f, 5f)] private float searchesPerSecond = 2f;    
    [SerializeField] [Range(1f, 100f)] private float searchRadius = 20f;
    [SerializeField] [Range(1f, 360f)] private float searchAngle = 180f;


    private State state;        // State of the missile
    private Transform target;   // The target to lock on to

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        state = State.Launched;
        StartCoroutine(Searching());
    }

    // IEnumerator which searches for targets to lock onto
    private IEnumerator Searching()
    {
        yield return new WaitForSeconds(searchStartTime);
        state = State.Searching;
        while (isActiveAndEnabled)
        {
            SearchForTarget();
            yield return new WaitForSeconds(1f / searchesPerSecond);
        }
        yield return true;
    }

    // Search for nearest rockets to lock onto
    private void SearchForTarget()
    {
        float currentTargetDistance = searchRadius;

        // Search all game objects in a circle around us
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, searchRadius);

        // For each collider we found
        foreach (Collider2D hitCollider in hitColliders)
        {
            // Is the collider a Player we can lock onto?
            if (hitCollider.gameObject.GetComponent<RocketMovement>() != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, hitCollider.transform.position);
                float angleToTarget = AngleToTarget(hitCollider.transform) * 2f;
                // Is the player closer than current target and within our seachangle?
                if (distanceToTarget < currentTargetDistance && angleToTarget <= searchAngle)
                {
                    // Lock onto the new target
                    state = State.Locked;
                    target = hitCollider.transform;
                }
            }
        }
    }

    // Returns the angle from the missile direction (up) to the target
    private float AngleToTarget(Transform _target)
    {
        Vector3 targetDir = _target.position - transform.position;
        return Vector3.Angle(targetDir, transform.up);
    }

    public override void OnStartServer()
    {
        if (destroyAfter > 0)
            Invoke(nameof(DestroySelf), destroyAfter);
    }

    private void Update()
    {
        //Rotate
        if (target != null)
        {
            Vector2 dirToTarget = (Vector2)target.position - rigidBody.position;
            dirToTarget.Normalize();
            float rotateAmount = Vector3.Cross(dirToTarget, transform.up).z;
            rigidBody.angularVelocity = -rotateAmount * rotateSpeed;
            Debug.DrawLine(transform.position, target.transform.position);
        }

        transform.position = transform.position + rigidBody.transform.up * speed * Time.deltaTime;
    }


    void OnDrawGizmos()
    {
        if (state == State.Searching)
        {
            Color c = Color.yellow;
            c.a = 0.5f;
            Gizmos.color = c;
            
            Gizmos.DrawSphere(transform.position, searchRadius);
        }
        else if (state == State.Locked)
        {
            Debug.DrawLine(transform.position, target.transform.position, Color.red);
        }
    }

    // destroy for everyone on the server
    [Server]
    void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }

}
