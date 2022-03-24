using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody2D))]
public class HomingMissile : NetworkBehaviour
{    
    [SerializeField] private float destroyAfter = 10f;    

    [Header("Missile lock on")]
    [SerializeField] [Range(0f, 5f)] private float searchStartTime = 1f;
    [SerializeField] [Range(1f, 5f)] private float searchesPerSecond = 2f;    
    [SerializeField] [Range(1f, 100f)] private float searchRadius = 20f;
    [SerializeField] [Range(1f, 360f)] private float searchAngle = 180f;

    [Header("Movement")]
    [SerializeField] private float speed = 110;
    [SerializeField] private float rotateSpeed = 200f;

    [Header("Prediction")]
    [SerializeField] private float maxDistancePredict = 100;
    [SerializeField] private float minDistancePredict = 5;
    [SerializeField] private float maxTimePrediction = 5;
    private Vector3 standardPrediction;

    [Header("Death")]
    [SerializeField] private float damage = 80;
    [SerializeField] private ParticleSystem deathEffect;
    [SerializeField] private GameObject[] disableGameObjectsOnDeath;
    [SerializeField] private float deathTime = 2f;

    private enum State { Launched, Searching, Locked };
    private Rigidbody2D rigidBody;
    private uint shooter;
    private State state;                    // State of the missile (searching, locked on)
    private Rigidbody2D targetRb;           // Target rigid body (to predict its movement etc)
    private Health targetHealth;            // Used to damage the target if we hit

    private bool dead = false;

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();

        if (!isServer)
            rigidBody.isKinematic = true;
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
            PlayerManager p = hitCollider.gameObject.GetComponent<PlayerManager>();
            if (p != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, hitCollider.transform.position);
                float angleToTarget = AngleToTarget(hitCollider.transform) * 2f;
                // Is the player closer than current target and within our seachangle?
                if (distanceToTarget < currentTargetDistance && angleToTarget <= searchAngle)
                {
                    // Lock onto the new target
                    state = State.Locked;
                    targetHealth = hitCollider.gameObject.GetComponent<Health>();
                    targetRb = hitCollider.gameObject.GetComponent<Rigidbody2D>();
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

        if (dead)
            return;

        // If target is dead go back to searching
        if (state == State.Locked && targetHealth != null && targetHealth.IsDead())
        {
            targetRb = null;
            targetHealth = null;
            state = State.Searching;

        }

        // Thrust
        //ThrustRocket();        

        // Turn
        if (state == State.Locked)
        {
            var leadTimePercentage = Mathf.InverseLerp(minDistancePredict, maxDistancePredict, Vector3.Distance(transform.position, targetRb.position));

            PredictMovement(leadTimePercentage);
            //RotateRocket();            
        }        
    }

    private void FixedUpdate()
    {
        if (!isServer)
            return;

        // Thrust
        ThrustRocket();

        // Turn
        if (state == State.Locked)
        {            
            RotateRocket();
        }
    }

    private void ThrustRocket()
    {
        //transform.position = transform.position + rigidBody.transform.up * speed * Time.deltaTime;
        transform.position = transform.position + rigidBody.transform.up * speed;
    }

    private void PredictMovement(float leadTimePercentage)
    {
        var predictionTime = Mathf.Lerp(0, maxTimePrediction, leadTimePercentage);

        standardPrediction = targetRb.position + targetRb.velocity * predictionTime;
    }    

    private void RotateRocket()
    {
        Vector2 dirToTarget = (Vector2)standardPrediction - rigidBody.position;
        float angle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg - 90;
        Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
        //transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * rotateSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, q, rotateSpeed);
    }

    [ServerCallback]
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Only check for a hit and deal damage on the server
        if (!isServer)
            return;

        dead = true;

        // Deal damage
        if (state == State.Locked)
            targetHealth.TakeDamage(damage);        

        RpcDie();

        StartCoroutine(WaitBeforeDying());

    }

    [ClientRpc]
    private void RpcDie()
    {
        // Disable game objects
        foreach (GameObject gameObject in disableGameObjectsOnDeath)
            gameObject.SetActive(false);

        // Disable collider
        Collider2D _col = GetComponent<Collider2D>();
        if (_col != null)
            _col.enabled = false;

        // Play deatch effect
        deathEffect.Play();
        //AudioManager.instance.PlayClipAtPoint("Explosion", transform.position);
        AudioManager.instance.Play("Explosion");
    }

    IEnumerator WaitBeforeDying()
    {
        yield return new WaitForSeconds(deathTime);
        if (isServer)
            DestroySelf();
    }

    void OnDrawGizmos()
    {
        if (targetRb == null)
        {
            Color c = Color.yellow;
            c.a = 0.5f;
            Gizmos.color = c;
            
            Gizmos.DrawSphere(transform.position, searchRadius);
        }
        else if (state == State.Locked)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetRb.position);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, standardPrediction);            
        }
    }

    // destroy for everyone on the server
    [Server]
    void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }

}
