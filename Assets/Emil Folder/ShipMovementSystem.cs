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
        float dt = SystemAPI.Time.DeltaTime;
        float t = (float)SystemAPI.Time.ElapsedTime;

        // fixed -90 X tilt
        var tilt = quaternion.Euler(math.radians(90f), 0f, 0f);

        foreach (var transform in
                 SystemAPI.Query<RefRW<LocalTransform>>()
                          .WithAll<ShipAuthoring.Ship>())
        {
            float3 pos = transform.ValueRO.Position;
            float angle = (0.5f + noise.cnoise(new float3(pos.x, pos.z, t * 0.15f) / 10f)) * (4f * math.PI);

            float sx, cz;
            math.sincos(angle, out sx, out cz);
            float3 dir = new float3(sx, 0f, cz);

            transform.ValueRW.Position += dir * (5f * dt);

            var yaw = quaternion.RotateY(angle);
            transform.ValueRW.Rotation = math.mul(yaw, tilt);
        }
    }
}
