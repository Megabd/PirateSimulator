using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class ShipAuthoring : MonoBehaviour
{
    class ShipBaker : Baker<ShipAuthoring>
    {
        public override void Bake(ShipAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic|TransformUsageFlags.WorldSpace);
            AddComponent(entity, new SpeedComponent { speed = 1.0f });
            AddComponent(entity, new RotationComponent { turnSpeed = 60.0f, desiredPosition = new float3(0.0f, 0.0f, 0.0f) });
            AddComponent(entity, new HealthComponent { health = 5 });
            AddComponent(entity, new WindComponent { windDirection = new float2(0.0f, 0.0f), power = 1.0f });
            
        }
    }
}
