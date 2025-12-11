using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject ShipPrefab;
    public GameObject CannonBallPrefab;
    public GameObject SeaPrefab;
    public int ShipCount;
    public bool Schedule;
    public bool ScheduleParallel;


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

public static class CannonConfig
{
    public const float SenseDistance = 20f;
    public const float CannonballSpeed = 10f;
    public const float CannonballLifeTime = 5f;
    public const float ShootWarmupTime = 0.5f;
}

public static class ShipConfig
{
    public const float ShipSpeed = 3f;
    public const float SenseRadius = 40f;
    public const float SenseOffset = 20f;
}

public struct SeaConfig
{
    public readonly static float halfWidth = 2000f;
    public readonly static float halfHeight = 2000f;
}