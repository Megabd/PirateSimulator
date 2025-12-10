using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
public partial struct ShipRespawnSystem : ISystem
{
    private Random _random;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<HealthComponent>();
        _random = new Random(0x6E624EB7u);

        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Pull the RNG into a local (Burst requires structs to be local for modification)
        var rng = _random;
        foreach (var (health, transform) in SystemAPI.Query<RefRW<HealthComponent>, RefRW<LocalTransform>>())
        {
            if (health.ValueRO.health <= 0)
            {
                health.ValueRW.health = health.ValueRO.startingHealth;

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

                transform.ValueRW.Position.x = x;
                transform.ValueRW.Position.y = 0f;
                transform.ValueRW.Position.z = z;

            }
        }
        _random = rng;
    }
}
