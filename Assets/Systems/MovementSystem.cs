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
        var tilt = quaternion.Euler(math.radians(90f), 0f, 0f);
        var yaw = quaternion.RotateY(0);

        foreach (var (transform, SpeedComponent, WindComponent) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<SpeedComponent>, RefRO<WindComponent>>()) 
        {

           
            transform.ValueRW.Rotation = math.mul(yaw, tilt);
            float3 upVector = transform.ValueRO.Up();

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
