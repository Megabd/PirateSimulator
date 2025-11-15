using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
        uint seed = 1;
        bool team = true;
        for (int i = 0; i < count; i++)
        {
            if (team)
            {
                team = false;
            }
            else
            {
                team = true;
            }
            float2 xz = rng.NextFloat2(
                new float2(-halfWidth, -halfHeight),
                new float2(halfWidth, halfHeight));

            var pos = new float3(xz.x, 0f, xz.y);

            em.SetComponentData(entities[i],
                LocalTransform.FromPositionRotationScale(pos, quaternion.identity, 1f));
            em.SetComponentData(entities[i], new TeamComponent { redTeam = team });
            em.SetComponentData(entities[i],
                new CooldownTimer { TimeLeft = 1.0f, MinSecs = 5.0f, MaxSecs = 15.0f, Seed = seed });
            var cannonBuffer = em.GetBuffer<ShipAuthoring.CannonElement>(entities[i]);

            // Apply team component to each cannon entity
            foreach (var ele in cannonBuffer) 
            {
                //var entity = GetEntity(ele, TransformUsageFlags.Dynamic);
                //em.SetComponentData(ele.Cannon, new TeamComponent { redTeam = team });
                em.SetComponentData(ele.Cannon, new TeamComponent { redTeam = team });
                //AddComponent(entity, new TeamComponent { redTeam = true });
            }
            seed+=1;
            //Debug.Log(team);
        }

        entities.Dispose();
    }
}
