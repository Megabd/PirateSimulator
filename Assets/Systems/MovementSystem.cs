using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

partial struct MovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        float dt = SystemAPI.Time.DeltaTime;
        

        foreach (var (transform, SpeedComponent, WindComponent) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<SpeedComponent>, RefRO<WindComponent>>()) 
        {

           
            //
            float3 upVector = transform.ValueRO.Forward();

            //Debug.Log("x: " + upVector.x);
            //Debug.Log("z: " + upVector.z);

            transform.ValueRW.Position.x += (upVector.x * SpeedComponent.ValueRO.speed + WindComponent.ValueRO.windDirection.x) * dt; 
            transform.ValueRW.Position.z += (upVector.z * SpeedComponent.ValueRO.speed + WindComponent.ValueRO.windDirection.y) * dt;  
            //Debug.Log("helo?");
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
