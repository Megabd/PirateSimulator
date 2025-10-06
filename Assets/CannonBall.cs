using UnityEngine;

public class CannonBall : MonoBehaviour
{
    [SerializeField] Vector3 direction = Vector3.right;
    [SerializeField] float speed = 10f;
    [SerializeField] float lifeTime = 10f;

    private float timer = 0f;
    void Awake()
    {
        // keep it normalized so "speed" is in units/second
        if (direction.sqrMagnitude > 0.0001f)
            direction = direction.normalized;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > lifeTime) Destroy(gameObject);
        transform.position += direction * speed * Time.deltaTime;
    }

    public void Init(Vector3 dir)
    {
        direction = dir.normalized;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Ship>(out var ship))
        {
            ship.TakeDamage(10);
        }
        Destroy(gameObject);
    }
}
