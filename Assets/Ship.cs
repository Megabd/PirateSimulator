using Unity.VisualScripting;
using UnityEngine;

public class Ship : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] int health = 100;
    [SerializeField] float turnSpeed = 100f; // deg/sec
    [SerializeField] float speed = 3f;       // units/sec

    [Header("Wandering")]
    [SerializeField] Vector3 target;
    [SerializeField] Vector2 targetChangeIntervalRange = new Vector2(5f, 15f);

    float targetChangeInterval;
    float timer;
    float fixedY; // lock Y here
    Sea sea;

    void Start()
    {
        sea = FindFirstObjectByType<Sea>();
        fixedY = transform.position.y; // remember our water level
        target = sea.GetRandomPointInSea();
        targetChangeInterval = Random.Range(targetChangeIntervalRange.x, targetChangeIntervalRange.y);
    }

    void Update()
    {
        // periodically pick a new target
        timer += Time.deltaTime;
        if (timer >= targetChangeInterval)
        {
            target = sea.GetRandomPointInSea();
            targetChangeInterval = Random.Range(targetChangeIntervalRange.x, targetChangeIntervalRange.y);
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
        transform.position += forwardXZ * speed * Time.deltaTime;

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target);
        Gizmos.DrawSphere(target, 0.3f);
    }
}
