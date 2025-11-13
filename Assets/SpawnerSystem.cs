using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct SpawnerSystem : ISystem
{
    float counter;
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        counter += 0.01f;

        /*foreach (var spawner in SystemAPI.Query<RefRW<SpawnerComponent>>())
        {

            spawner.ValueRW.TimeToNextSpawn -= SystemAPI.Time.DeltaTime;
            if (spawner.ValueRO.TimeToNextSpawn < 0)
            {
                spawner.ValueRW.TimeToNextSpawn = spawner.ValueRO.Timer;
                Entity e = ecb.Instantiate(spawner.ValueRO.Prefab);
                Debug.Log("Hello?");
            }
        }*/
        
        foreach (var (spawner, entity) in SystemAPI.Query<RefRW<SpawnerComponent>>().WithEntityAccess())
        {
            spawner.ValueRW.TimeToNextSpawn -= SystemAPI.Time.DeltaTime;

            if (spawner.ValueRW.TimeToNextSpawn <= 0f)
            {
                spawner.ValueRW.TimeToNextSpawn = spawner.ValueRO.Timer;
                var inst = ecb.Instantiate(spawner.ValueRO.Prefab);
                ecb.SetComponent(inst, LocalTransform.FromPositionRotationScale(
                new float3(-10 + counter, 0, 0), quaternion.identity, 1f));
                /*AddComponent(entity, new SpeedComponent { speed = 1.0f });
                AddComponent(entity, new RotationComponent { turnSpeed = 60.0f, desiredPosition = new float3(0.0f, 0.0f, 0.0f) });
                AddComponent(entity, new HealthComponent { health = 5 });
                AddComponent(entity, new WindComponent { windDirection = new float2(0.0f, 0.0f), power = 1.0f });*/
                UnityEngine.Debug.Log("Spawned!");
            }
        }
    }
}