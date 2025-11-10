using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

partial struct MovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, SpeedComponent, WindComponent) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<SpeedComponent>, RefRO<WindComponent>>()) 
        {
            float3 upVector = math.mul(transform.ValueRW.Rotation, new float3(0,1,0));
            //float3 forwardXZ = new float3(upVector.x, 0f, upVector.z);
            //Normalize we thinks
            float len = math.lengthsq(upVector);
            upVector /= math.sqrt(len);

            transform.ValueRW.Position.x += (upVector.x * SpeedComponent.ValueRO.speed + WindComponent.ValueRO.windDirection.x) * SystemAPI.Time.DeltaTime; 
            transform.ValueRW.Position.z += (upVector.z * SpeedComponent.ValueRO.speed + WindComponent.ValueRO.windDirection.y) * SystemAPI.Time.DeltaTime;       
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
