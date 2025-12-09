using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.InputManagerEntry;

partial struct MovementSystem : ISystem
{
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        float dt = SystemAPI.Time.DeltaTime;

        if (config.ScheduleParallel)
        {
            new EntityMoveJob
            {
                DeltaTime = dt
            }.ScheduleParallel();
        }

        else if (config.Schedule)
        {
            new EntityMoveJob
            {
                DeltaTime = dt
            }.Schedule();
        }

        else
        {
            foreach (var (transform, SpeedComponent, WindComponent, velocity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<SpeedComponent>, RefRO<WindComponent>, RefRW<PhysicsVelocity>>())
        {


            //float3 upVector = transform.ValueRO.Forward();
            //Debug.Log("x: " + upVector.x);
            //Debug.Log("z: " + upVector.z);
            //transform.ValueRW.Position.x += (upVector.x * SpeedComponent.ValueRO.speed + WindComponent.ValueRO.windDirection.x) * dt; 
            //transform.ValueRW.Position.z += (upVector.z * SpeedComponent.ValueRO.speed + WindComponent.ValueRO.windDirection.y) * dt;  
            //Debug.Log("helo?");


            //Not using jobs
            
            float3 upVector = transform.ValueRO.Forward();

            float2 forwardXZ = math.normalizesafe(new float2(upVector.x, upVector.z));

            float2 windXZ = WindComponent.ValueRO.windDirection;

            float2 desiredXZ = forwardXZ * SpeedComponent.ValueRO.speed + windXZ;

            var v = velocity.ValueRW;

            // Preserve Y (gravity / waves / whatever), override XZ
            v.Linear.x = desiredXZ.x;
            v.Linear.z = desiredXZ.y;

            velocity.ValueRW = v;
        }
        }
            
        
        
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}


[BurstCompile]
public partial struct EntityMoveJob : IJobEntity
{
    public float DeltaTime;
    void Execute(Entity e, ref LocalTransform transform,  ref SpeedComponent SpeedComponent, ref WindComponent WindComponent, ref PhysicsVelocity velocity)
    {
        float3 upVector = transform.Forward();

        float2 forwardXZ = math.normalize(new float2(upVector.x, upVector.z));

        float2 windXZ = WindComponent.windDirection;

        float2 desiredXZ = forwardXZ * SpeedComponent.speed + windXZ;

        var v = velocity;

        // Preserve Y (gravity / waves / whatever), override XZ
        v.Linear.x = desiredXZ.x;
        v.Linear.z = desiredXZ.y;

        velocity = v;
    }
}
