using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
/*
public partial struct CanonBallSpawnSystem : ISystem
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
        int count = config.ShipCount * 12;
        var entities = new NativeArray<Entity>(count, Allocator.Temp);
        em.Instantiate(config.CannonBallPrefab, entities);
        var rng = Unity.Mathematics.Random.CreateFromIndex(1337u);
        for (int i = 0; i < count; i++)
        {
            float2 xz = rng.NextFloat2(
                new float2(-SeaConfig.halfWidth, -SeaConfig.halfHeight),
                new float2(SeaConfig.halfWidth, SeaConfig.halfHeight));

            var pos = new float3(xz.x, -2f, xz.y);
            em.SetComponentData(entities[i],
                LocalTransform.FromPositionRotationScale(pos, quaternion.identity, 1f));
            em.SetComponentData(entities[i], new CannonBalls
            {
                Velocity = 0f,
                Lifetime = CannonConfig.CannonballLifeTime,
                Radius = 0.5f //canonball hitbox
            });
            }

        entities.Dispose();
    }
}
*/