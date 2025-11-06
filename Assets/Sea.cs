using UnityEditor.UI;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;


public class Sea : MonoBehaviour
{
    [SerializeField] GameObject shipPrefab;
    [SerializeField] int shipAmount = 20;
    [SerializeField] Vector2 windPowerInterval = new Vector2(0.5f, 2f);
    [SerializeField] Vector2 windChangeInterval = new Vector2(5f, 15f);
    [SerializeField] float mapSizeToShips = 0.3f;
    [SerializeField] float timeScale = 1.0f;
    public Vector2 WindDirection { get; private set; }
    private float timer = 0f;
    private float hx;
    private float hy;
    void Start()
    {
        Time.timeScale = timeScale;
        // set local scale based on shipAmount
        var s = transform.localScale;
        s.x = mapSizeToShips * shipAmount;
        s.y = mapSizeToShips * shipAmount; 
        transform.localScale = s;

        hx = transform.lossyScale.x * 0.4f;
        hy = transform.lossyScale.y * 0.4f;
        /*for (int i = 0; i < shipAmount; i++)
        {
            Vector3 pos = GetRandomPointInSea();
            var obj = Instantiate(shipPrefab, pos, shipPrefab.transform.rotation);
            if (obj.TryGetComponent<Ship>(out var ship))
            {
                ship.Init(i%2 == 0);
            }
        }*/
        spawnShipsEntities();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= windChangeInterval.y)
        {
            ChangeWindDirection();
            timer = 0f;
        }

    }


    public Vector3 GetRandomPointInSea()
    {
        float x = UnityEngine.Random.Range(-hx, hx);
        float z = UnityEngine.Random.Range(-hy, hy);
        return new Vector3(transform.position.x + x, transform.position.y + 0.1f, transform.position.z + z);
    }

    /// <summary>Is a world-space point within the sea rectangle (XZ)?</summary>
    public bool IsWithinSeaBounds(Vector3 p)
    {
        float dx = p.x - transform.position.x;
        float dz = p.z - transform.position.z;
        return Mathf.Abs(dx) <= hx && Mathf.Abs(dz) <= hy;
    }

    public void ChangeWindDirection()
    {
        float angle = UnityEngine.Random.Range(0f, 360f);
        float power = UnityEngine.Random.Range(windPowerInterval.x, windPowerInterval.y);

        // Convert angle and power to a directional vector
        float radians = angle * Mathf.Deg2Rad;
        WindDirection = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * power;
    }

    void OnDrawGizmos()
    {
        // Draw wind direction arrow at the center of the sea
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position;
        Vector3 windDir = new Vector3(WindDirection.x, 0f, WindDirection.y);

        // Arrow length reflects wind power (magnitude)
        float arrowLength = WindDirection.magnitude * 5f; // scale factor for visibility

        // Draw main line
        Gizmos.DrawLine(origin, origin + windDir.normalized * arrowLength);

        // Draw arrow head
        Vector3 right = Quaternion.Euler(0, 25, 0) * windDir.normalized;
        Vector3 left = Quaternion.Euler(0, -25, 0) * windDir.normalized;
        Gizmos.DrawLine(origin + windDir.normalized * arrowLength, origin + right * (arrowLength * 0.8f));
        Gizmos.DrawLine(origin + windDir.normalized * arrowLength, origin + left * (arrowLength * 0.8f));
    }


    void spawnShipsEntities()
    {
        EntityManager entityManager =  World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype archetype = entityManager.CreateArchetype(
            typeof(LocalTransform),
            typeof(SpeedComponent),
            typeof(RotationComponent),
            typeof(HealthComponent),
            typeof(WindComponent)
        );

        for (int i=0; i<shipAmount; i++){
            Entity entity = entityManager.CreateEntity(archetype);
            entityManager.SetComponentData(entity, new LocalTransform{Position = new float3(0.0f, 0.0f, 0.0f), Scale = 1.0f, Rotation = quaternion.identity});
            entityManager.SetComponentData(entity, new SpeedComponent{speed = 1.0f});
            entityManager.SetComponentData(entity, new RotationComponent{turnSpeed = 60.0f, desiredPosition = new float3(0.0f, 0.0f, 0.0f)});
            entityManager.SetComponentData(entity, new HealthComponent{health = 5});
            entityManager.SetComponentData(entity, new WindComponent{windDirection = new float2(0.0f, 0.0f), power = 1.0f});
        }
        
    }

}
