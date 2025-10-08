using Unity.VisualScripting;
using UnityEngine;

public class Ship : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] int health = 100;
    [SerializeField] float turnSpeed = 100f; // deg/sec
    [SerializeField] public float Speed { get; private set; } = 2f;
    [SerializeField] float senseRadius = 10f;          // who counts as nearby
    [SerializeField] float sampleOffset = 5f;         // how far F/L/R samples are

    [Header("Wandering")]
    [SerializeField] Vector3 target;

    [SerializeField] Vector2 targetChangeInterval = new Vector2(3f, 6f);
    float targetChangeTimer;
    float timer;
    float fixedY; // lock Y here
    Sea sea;

    public bool teamRed;

    public void Init(bool teamR)
    {
        teamRed = teamR;
    }
    void Start()
    {
        sea = FindFirstObjectByType<Sea>();
        targetChangeTimer = Random.Range(targetChangeInterval.x, targetChangeInterval.y);
        fixedY = transform.position.y; // remember our water level
        target = sea.GetRandomPointInSea();
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            if (teamRed)
                renderer.material.color = Color.red;
            else
                renderer.material.color = Color.blue;
        }
    }

    void Update()
    {
        // periodically pick a new target
        timer += Time.deltaTime;
        if (timer >= targetChangeTimer)
        {
            target = ChooseTargetPosition();
            targetChangeTimer = Random.Range(targetChangeInterval.x, targetChangeInterval.y);
            timer = 0f;
        }

        // --- YAW-ONLY STEERING (XZ plane) ---
        Vector3 toTarget = target - transform.position;
        Vector3 flatDir = new Vector3(toTarget.x, 0f, toTarget.z);
        if (flatDir.sqrMagnitude > 1e-6f)
        {
            float currentYaw = transform.eulerAngles.y;
            float targetYaw = Mathf.Atan2(flatDir.x, flatDir.z) * Mathf.Rad2Deg; // XZ → yaw
            float nextYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(90f, nextYaw, 0f); // zero X/Z rotation
        }

        // move forward on XZ only
        Vector3 forwardXZ = new Vector3(transform.up.x, 0f, transform.up.z).normalized;
        Vector3 windXZ = new Vector3(sea.WindDirection.x, 0f, sea.WindDirection.y);
        transform.position += (forwardXZ * Speed + windXZ) * Time.deltaTime;    

        // hard-lock Y
        var p = transform.position;
        p.y = fixedY;
        transform.position = p;
    }
    public void TakeDamage(int dmg)
    {
        health -= dmg;
        if (health <= 0) Destroy(gameObject);
    }

    // Choose a new target position by sampling forward, left, and right, and choosing the one with most allies and at least 1 enemy
    private Vector3 ChooseTargetPosition()
    {
        Vector3 pos = transform.position;

        // Build an XZ basis: forward from transform.up, right via cross
        Vector3 fwd = new Vector3(transform.up.x, 0f, transform.up.z).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

        // Sample points: forward, left, right
        Vector3[] samples = new Vector3[4] {
            pos + fwd   * sampleOffset,
            pos - right * sampleOffset,
            pos + right * sampleOffset,
            pos - fwd * sampleOffset
        };

        foreach (var s in samples)
        {
            if (!sea.IsWithinSeaBounds(s)) continue;
            var hits = Physics.OverlapSphere(s, senseRadius);
            bool hasEnemies = false;
            int allyToEnemyCount = 0;

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<Ship>(out var ship))
                {
                    if (ship == this) continue; // skip self
                    if (ship.teamRed == this.teamRed)
                        allyToEnemyCount++;
                    else { 
                        hasEnemies = true;
                        allyToEnemyCount--; // enemies count negatively
                    }
                }
            }
            if (hasEnemies && allyToEnemyCount > 0)
            {
                return s;
            }
        }
        return sea.GetRandomPointInSea(); // fallback to random

    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target);
        Gizmos.DrawSphere(target, 0.3f);
    }
}
