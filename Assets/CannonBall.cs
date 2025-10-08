using UnityEngine;

public class CannonBall : MonoBehaviour
{
    [SerializeField] Vector3 direction = Vector3.right;
    [SerializeField] public float Speed { get; private set; } = 10f;
    [SerializeField] float lifeTime = 10f;

    private Sea sea;
    private float timer = 0f;
    void Awake()
    {
        // keep it normalized so "speed" is in units/second
        if (direction.sqrMagnitude > 0.0001f)
            direction = direction.normalized;
        sea = FindFirstObjectByType<Sea>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > lifeTime) Destroy(gameObject);

        // Apply wind from Sea
        Vector3 windXZ = new Vector3(sea.WindDirection.x, 0f, sea.WindDirection.y);
        transform.position += (direction * Speed + windXZ) * Time.deltaTime;
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
