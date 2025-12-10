using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.Rendering.STP;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject ShipPrefab;
    public GameObject CannonBallPrefab;
    public GameObject SeaPrefab;
    public int ShipCount;
    public bool Schedule;
    public bool ScheduleParallel;
    public static float2 MapSize;


    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new Config
            {
                ShipPrefab = GetEntity(authoring.ShipPrefab, TransformUsageFlags.Dynamic),
                CannonBallPrefab = GetEntity(authoring.CannonBallPrefab, TransformUsageFlags.Dynamic),
                SeaPrefab = GetEntity(authoring.SeaPrefab, TransformUsageFlags.Dynamic),
                ShipCount = authoring.ShipCount,
                Schedule = authoring.Schedule,
                ScheduleParallel = authoring.ScheduleParallel,
                MapSize = MapSize,
            });
        }
    }
}
public struct Config : IComponentData
{
    public Entity ShipPrefab;
    public Entity CannonBallPrefab;
    public Entity SeaPrefab;
    public int ShipCount;
    public bool Schedule;
    public bool ScheduleParallel;
    public float2 MapSize;
}

public struct CannonConfig
{
    public static float SenseDistance = 20f;
    public static float CannonballSpeed = 10f;
    public static float CannonballLifeTime = 5f;
    public static float ShootWarmupTime = 0.5f;
}

public struct ShipConfig
{
    public static float ShipSpeed = 3f;
    public static float ShipSenseOffset = 20f;
    public static float ShipSenseRadius = 100f;
}

public struct SeaConfig
{
    public static float halfWidth = ConfigAuthoring.MapSize.x * 0.5f - 10f;
    public static float halfHeight = ConfigAuthoring.MapSize.y * 0.5f - 10f;
}