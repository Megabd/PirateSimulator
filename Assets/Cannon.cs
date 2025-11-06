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
    Ship targetShip;

    private void Start()
    {
        startLocalRotation = transform.localRotation;
        parentShip = GetComponentInParent<Ship>();
        var windTemp = FindFirstObjectByType<Sea>().WindDirection;
        wind = new Vector3(windTemp.x, 0f, windTemp.y);
        CannonBallSpeed = CannonBallPrefab.GetComponent<CannonBall>().Speed;
        targetShip = null;
    }
    void Update()
    {
        shootCooldownTimer -= Time.deltaTime;

           

        // Look for a target every frame to keep tracking while waiting to fire
        if (Physics.Raycast(transform.position, transform.up, out RaycastHit hit, shootDistance))
        {
            if (hit.collider.TryGetComponent<Ship>(out var ship) && ship.teamRed != parentShip.teamRed)
            {

                // Fire only if cooldown is ready
                if (shootCooldownTimer <= 0f)
                {
                    targetShip = ship;
                    shootCooldownTimer = Random.Range(cooldownInterval.x, cooldownInterval.y);
                    float delay = Mathf.Max(0f, Random.Range(RandomDelayInterval.x, RandomDelayInterval.y));
                    StartCoroutine(DelayedShoot(delay));
                }
            }
        }

        // Only return to start if no target this frame
        if (targetShip == null)
        {
            ReturnToStart();
        }
        else {
            // Aim continuously while target 
            AimSimplePrediction(targetShip.transform, targetShip.transform.up * targetShip.Speed + wind);
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
        targetShip = null; // reset target after shooting
    }

    // (Optional) visualize ray in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * shootDistance);
    }

    void AimSimplePrediction(Transform target, Vector3 targetVelocity)
    {
        // where target will be
        Vector3 toTarget = target.position - transform.position;
        float dist = toTarget.magnitude;

        float timeToIntercept = dist / CannonBallSpeed;
        Vector3 predicted = target.position + targetVelocity * timeToIntercept;

        // direction we want to point the BARREL at
        Vector3 desiredDir = predicted - transform.position;

        // stay on XZ so it doesn't pitch up/down
        desiredDir = Vector3.ProjectOnPlane(desiredDir, Vector3.up).normalized;
        if (desiredDir.sqrMagnitude < 0.0001f)
            return;

        // CURRENT world direction of the barrel (because your barrel = transform.up)
        Vector3 currentBarrelDir = transform.up;

        // make a rotation that turns "what I'm currently pointing" into "what I want to point"
        Quaternion alignRot = Quaternion.FromToRotation(currentBarrelDir, desiredDir);

        // apply it on top of the current world rotation to get the target world rotation
        Quaternion targetWorldRot = alignRot * transform.rotation;

        // turn that into local space under the ship
        Quaternion targetLocalRot = Quaternion.Inverse(parentShip.transform.rotation) * targetWorldRot;

        // keep your clamping
        float angleFromStart = Quaternion.Angle(startLocalRotation, targetLocalRot);
        Quaternion goalLocalRot =
            angleFromStart <= 60f
            ? targetLocalRot
            : Quaternion.RotateTowards(startLocalRotation, targetLocalRot, 60f);

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
