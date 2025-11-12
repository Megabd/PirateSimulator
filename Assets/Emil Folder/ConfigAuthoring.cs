using Unity.Entities;
using UnityEngine;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject ShipPrefab;
    public GameObject CannonBallPrefab;
    public int ShipCount;

    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new Config
            {
                ShipPrefab = GetEntity(authoring.ShipPrefab, TransformUsageFlags.Dynamic),
                CannonBallPrefab = GetEntity(authoring.CannonBallPrefab, TransformUsageFlags.Dynamic),
                ShipCount = authoring.ShipCount,
            });
        }
    }
}
public struct Config : IComponentData
{
    public Entity ShipPrefab;
    public Entity CannonBallPrefab;
    public int ShipCount;
}