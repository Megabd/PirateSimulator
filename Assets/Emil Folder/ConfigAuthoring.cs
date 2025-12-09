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
    public float2 MapSize;

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
                MapSize = authoring.MapSize,
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