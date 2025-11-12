using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

partial struct RotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float3 up = math.up();
        foreach (var (transform, RotationComponent) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationComponent>>()) 
        {
            float3 toTarget = RotationComponent.ValueRO.desiredPosition - transform.ValueRO.Position;
            //Normalize
            float lenSq = math.lengthsq(toTarget);
            toTarget *= math.rsqrt(lenSq);
            quaternion goalRot = quaternion.LookRotationSafe(toTarget, up);

            // write it 
            var lt = transform.ValueRO;
            lt.Rotation = goalRot;
            transform.ValueRW = lt;
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
