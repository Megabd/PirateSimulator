using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// Optional: keep it in the same group as your other systems
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ShipRespawnSystem : ISystem
{
    private uint _baseRandomSeed;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<HealthComponent>();
        state.RequireForUpdate<Config>();

        // Initial seed
        _baseRandomSeed = 0x6E624EB7u;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();

        var job = new ShipRespawnJob
        {
            baseSeed = _baseRandomSeed
        };

        if (config.ScheduleParallel)
        {
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
        else if (config.Schedule)
        {
            state.Dependency = job.Schedule(state.Dependency);
        }
        else
        {
            job.Run();
        }

        // Change seed for next frame so the pattern isn't identical
        _baseRandomSeed++;
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}

[BurstCompile]
public partial struct ShipRespawnJob : IJobEntity
{
    public uint baseSeed;
    void Execute([EntityIndexInQuery] int entityInQueryIndex,ref HealthComponent health, ref LocalTransform transform)
    {
        if (health.health > 0) return;
        // "Respawn": reset health
        health.health = health.startingHealth;

        // Per-entity RNG, safe for parallel
        var rng = Unity.Mathematics.Random.CreateFromIndex(baseSeed + (uint)entityInQueryIndex);

        int side = rng.NextInt(0, 4);
        float x = 0f;
        float z = 0f;

        switch (side)
        {
            case 0:
                x = -SeaConfig.halfWidth;
                z = rng.NextFloat(-SeaConfig.halfHeight, SeaConfig.halfHeight);
                break;
            case 1:
                x = SeaConfig.halfWidth;
                z = rng.NextFloat(-SeaConfig.halfHeight, SeaConfig.halfHeight);
                break;
            case 2:
                z = -SeaConfig.halfHeight;
                x = rng.NextFloat(-SeaConfig.halfWidth, SeaConfig.halfWidth);
                break;
            case 3:
                z = SeaConfig.halfHeight;
                x = rng.NextFloat(-SeaConfig.halfWidth, SeaConfig.halfWidth);
                break;
        }

        transform.Position = new float3(x, 0f, z);
    }
}
