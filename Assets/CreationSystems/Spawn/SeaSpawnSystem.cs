
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct SeaSpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;
        var config = SystemAPI.GetSingleton<Config>();

        state.Enabled = false;

        float tileSize = 50f;

        int tilesX = (int)math.ceil(config.MapSize*2 / tileSize);
        int tilesZ = (int)math.ceil(config.MapSize*2 / tileSize);

        float startX = -((tilesX * tileSize) * 0.5f);
        float startZ = -((tilesZ * tileSize) * 0.5f);

        for (int x = 0; x < tilesX; x++)
        {
            for (int z = 0; z < tilesZ; z++)
            {
                float posX = startX + x * tileSize + tileSize * 0.5f;
                float posZ = startZ + z * tileSize + tileSize * 0.5f;

                Entity seaTile = em.Instantiate(config.SeaPrefab);
                em.SetComponentData(seaTile,
                    LocalTransform.FromPositionRotationScale(
                        new float3(posX, 0f, posZ),
                        quaternion.identity,
                        1f
                    )
                );
            }
        }
    }
}

