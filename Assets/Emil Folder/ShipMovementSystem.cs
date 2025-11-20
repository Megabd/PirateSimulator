// ShipMovementSystem.cs
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct ShipMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ShipAuthoring.Ship>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {   
        
        /*float dt = SystemAPI.Time.DeltaTime;
        float t = (float)SystemAPI.Time.ElapsedTime;

        // fixed -90 X tilt
        var tilt = quaternion.Euler(math.radians(90f), 0f, 0f);

        foreach (var (transform, SpeedComponent, WindComponent) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<SpeedComponent>, RefRO<WindComponent>>().WithAll<ShipAuthoring.Ship>())
        {
            //float3 pos = transform.ValueRO.Position;
            //float angle = (0.5f + noise.cnoise(new float3(pos.x, pos.z, t * 0.15f) / 10f)) * (4f * math.PI);
            float3 upVector = transform.ValueRO.Up();

            transform.ValueRW.Position.x += (upVector.x * SpeedComponent.ValueRO.speed + WindComponent.ValueRO.windDirection.x) * SystemAPI.Time.DeltaTime; 
            transform.ValueRW.Position.z += (upVector.z * SpeedComponent.ValueRO.speed + WindComponent.ValueRO.windDirection.y) * SystemAPI.Time.DeltaTime;

            var yaw = quaternion.RotateY(0);
            //transform.ValueRW.Rotation = math.mul(yaw, tilt);
            
        }*/
    }
}
