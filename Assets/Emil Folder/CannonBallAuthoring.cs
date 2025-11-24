using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class CannonBallAuthoring : MonoBehaviour
{
    public float Radius = 0.5f;
    class Baker : Baker<CannonBallAuthoring>
    {
        public override void Bake(CannonBallAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent<CannonBalls>(entity);
            AddComponent<URPMaterialPropertyBaseColor>(entity);
        }
    }
}

public struct CannonBalls : IComponentData
{
    public float3 Velocity;
    public float Lifetime;
    public float Radius;
}