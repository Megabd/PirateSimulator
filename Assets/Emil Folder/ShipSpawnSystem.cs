using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct ShipSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        var em = state.EntityManager;
        var config = SystemAPI.GetSingleton<Config>();
        int count = math.max(0, config.ShipCount);

        var entities = new NativeArray<Entity>(count, Allocator.Temp);
        em.Instantiate(config.ShipPrefab, entities);

        const float halfWidth = 150f;  // 300 wide
        const float halfHeight = 50f;   // 100 tall
        var rng = Unity.Mathematics.Random.CreateFromIndex(1337u);

        for (int i = 0; i < count; i++)
        {
            float2 xz = rng.NextFloat2(
                new float2(-halfWidth, -halfHeight),
                new float2(halfWidth, halfHeight));

            var pos = new float3(xz.x, 0f, xz.y);

            em.SetComponentData(entities[i],
                LocalTransform.FromPositionRotationScale(pos, quaternion.identity, 1f));
        }

        entities.Dispose();
    }
}
