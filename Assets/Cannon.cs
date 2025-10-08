using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Cannon : MonoBehaviour
{
    [SerializeField] GameObject CannonBallPrefab;
    [SerializeField] Vector2 cooldownInterval = new Vector2(5f, 15f);
    [SerializeField] float shootDistance = 10f;
    [SerializeField] Vector2 RandomDelayInterval = new Vector2(0.1f, 0.5f); // random delay variation
    [SerializeField] float turnSpeed = 10f;




    float CannonBallSpeed;
    private Quaternion startLocalRotation;

    private float shootCooldownTimer = 0f;
    private Ship parentShip;
    Vector3 wind;

    private void Start()
    {
        startLocalRotation = transform.localRotation;
        parentShip = GetComponentInParent<Ship>();
        var windTemp = FindFirstObjectByType<Sea>().WindDirection;
        wind = new Vector3(windTemp.x, 0f, windTemp.y);
        CannonBallSpeed = CannonBallPrefab.GetComponent<CannonBall>().Speed;
    }
    void Update()
    {
        shootCooldownTimer -= Time.deltaTime;

        bool hasTarget = false;
        Ship targetShip = null;


        // Look for a target every frame to keep tracking while waiting to fire
        if (Physics.Raycast(transform.position, transform.up, out RaycastHit hit, shootDistance))
        {
            if (hit.collider.TryGetComponent<Ship>(out var ship) && ship.teamRed != parentShip.teamRed)
            {
                hasTarget = true;
                targetShip = ship;

                // Aim continuously while target is visible
                AimSimplePrediction(ship.transform, ship.transform.up * ship.Speed + wind);

                // Fire only if cooldown is ready
                if (shootCooldownTimer <= 0f)
                {
                    shootCooldownTimer = Random.Range(cooldownInterval.x, cooldownInterval.y);
                    float delay = Mathf.Max(0f, Random.Range(RandomDelayInterval.x, RandomDelayInterval.y));
                    StartCoroutine(DelayedShoot(delay));
                }
            }
        }

        // Only return to start if no target this frame
        if (!hasTarget)
        {
            ReturnToStart();
        }
    }

    System.Collections.IEnumerator DelayedShoot(float delay)
    {
        yield return new WaitForSeconds(delay);

        var obj = Instantiate(CannonBallPrefab, transform.position, Quaternion.LookRotation(transform.up));
        if (obj.TryGetComponent<CannonBall>(out var ball))
        {
            ball.Init(transform.up);
        }
    }

    // (Optional) visualize ray in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * shootDistance);
    }

    void AimSimplePrediction(Transform target, Vector3 targetVelocity)
    {

        Vector3 toTarget = target.position - transform.position;
        float dist = toTarget.magnitude;

        float TimeToIntercept = dist / CannonBallSpeed; // How long for the cannonball to hit the ship, does not account for any movement or wind

        Vector3 predicted = target.position + targetVelocity * TimeToIntercept;
        Vector3 desiredDir = (predicted - transform.position).normalized;

        Quaternion targetWorldRot = Quaternion.LookRotation(desiredDir, parentShip.transform.up);
        Quaternion targetLocalRot = Quaternion.Inverse(parentShip.transform.rotation) * targetWorldRot;


        float angleFromStart = Quaternion.Angle(startLocalRotation, targetLocalRot);
        Quaternion goalLocalRot =
        angleFromStart <= 60f
        ? targetLocalRot
        : Quaternion.RotateTowards(startLocalRotation, targetLocalRot, 60f);

        // 4) Rotate cannon in LOCAL space
        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            goalLocalRot,
            turnSpeed * Time.deltaTime
        );

    }

    public void ReturnToStart()
    {
        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            startLocalRotation,
            turnSpeed * Time.deltaTime
        );

        if (Quaternion.Angle(transform.localRotation, startLocalRotation) <= 0.5f)
            transform.localRotation = startLocalRotation;
    }
}
