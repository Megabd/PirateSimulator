using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Cannon : MonoBehaviour
{
    [SerializeField] GameObject CannonBallPrefab;
    [SerializeField] Vector2 cooldownInterval = new Vector2(5f, 15f);
    [SerializeField] float shootDistance = 10f;
    [SerializeField] Vector2 RandomDelayInterval = new Vector2(0.1f, 0.5f); // random delay variation

    private float timer = 0f;


    void Update()
    {
        timer -= Time.deltaTime;

        if (timer < 0f)
        {
            // 3D raycast in shootDirection
            if (Physics.Raycast(transform.position, transform.up, out RaycastHit hit, shootDistance))
            {
                if (hit.collider.TryGetComponent<Ship>(out var ship))
                {
                    timer = Random.Range(cooldownInterval.x, cooldownInterval.y); 

                    // clamp delay to non-negative (WaitForSeconds doesn't accept negative)
                    float delay = Random.Range(RandomDelayInterval.x, RandomDelayInterval.y);

                    StartCoroutine(DelayedShoot(delay));
                }
            }
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
}
