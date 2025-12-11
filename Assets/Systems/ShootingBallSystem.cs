using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using System;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(RotationSystem))]
partial struct ShootingBallSystem : ISystem
{
    private Unity.Mathematics.Random rand;
    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        rand = new Unity.Mathematics.Random(1);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        var ballXform = SystemAPI.GetComponent<LocalTransform>(config.CannonBallPrefab);
        float dt = SystemAPI.Time.DeltaTime;

        if (config.ScheduleParallel)
        {
            state.Dependency = new ShootingBallJob
            {
                dt = dt,
                config = config,
                ecb = ecb,
                ballXform = ballXform,
            }.ScheduleParallel(state.Dependency);
        }

        else if (config.Schedule)
        {
            state.Dependency = new ShootingBallJob
            {
                dt = dt,
                config = config,
                ecb = ecb,
                ballXform = ballXform,
            }.Schedule(state.Dependency);
        }

        /*else
        {
            var em = state.EntityManager;
            foreach (var (transform,
             rotation,
             aim,
             worldPos,
             prevPos)
         in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<RotationComponent>,
                RefRW<Aim>,
                RefRO<LocalToWorld>,
                RefRW<PrevPosComponent>>())
        {


            float3 currentPos = worldPos.ValueRO.Position;
            float3 cannonVel = (currentPos - prevPos.ValueRO.PrePos) / dt; // world-space velocity
            prevPos.ValueRW.PrePos = currentPos; // store for next frame

            // No target, no warmup, no shot
            if (!aim.ValueRO.HasTarget)
            {
                aim.ValueRW.ShootTimeLeft = 0f;
                continue;
            }

            // Count down the warmup timer
            aim.ValueRW.ShootTimeLeft -= dt;
            if (aim.ValueRW.ShootTimeLeft > 0f)
            {
                // still winding up, don't shoot yet
                continue;
            }

            var ball = em.Instantiate(config.CannonBallPrefab);

            // spawn at cannon position
            ballXform.Position = currentPos;
            ballXform.Rotation = transform.ValueRO.Rotation; // local rotation of cannon entity
            em.SetComponentData(ball, ballXform);


            // ball inherits cannon velocity + its own shooting speed
            float3 dir = math.normalize(worldPos.ValueRO.Forward);
            em.SetComponentData(ball, new CannonBalls
            {
                Velocity = cannonVel + dir * CannonConfig.CannonballSpeed,
                Lifetime = CannonConfig.CannonballLifeTime,
                Radius = 0.5f //canonball hitbox
            });

            aim.ValueRW.HasTarget = false;
            rotation.ValueRW.desiredPosition = float3.zero;
        }
        }*/


    }


    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}


[BurstCompile]
public partial struct ShootingBallJob : IJobEntity
{
    public float dt;

    public Config config;

    public EntityCommandBuffer.ParallelWriter ecb;
    public LocalTransform ballXform;

    void Execute([EntityIndexInQuery] int entityInQueryIndex, Entity e, in LocalTransform transform, ref RotationComponent rotation, ref Aim aim, in LocalToWorld worldPos, ref PrevPosComponent prevPos, ref CanonBallRef ball)
    {
        float3 currentPos = worldPos.Position;
            float3 cannonVel = (currentPos - prevPos.PrePos) / dt; // world-space velocity
            prevPos.PrePos = currentPos; // store for next frame

            // No target, no warmup, no shot
            if (!aim.HasTarget)
            {
                aim.ShootTimeLeft = 0f;
                return;
            }

            // Count down the warmup timer
            aim.ShootTimeLeft -= dt;
            if (aim.ShootTimeLeft > 0f)
            {
                // still winding up, don't shoot yet
                return;
            }
            

            // spawn at cannon position
            ballXform.Position = currentPos;
            ballXform.Rotation = transform.Rotation; // local rotation of cannon entity
            ecb.SetComponent(entityInQueryIndex, ball.Canonball, ballXform);

            // ball inherits cannon velocity + its own shooting speed
            float3 dir = math.normalize(worldPos.Forward);
            ecb.SetComponent(entityInQueryIndex, ball.Canonball, new CannonBalls
            {
                Velocity = cannonVel + dir * CannonConfig.CannonballSpeed,
                Lifetime = CannonConfig.CannonballLifeTime,
                Radius = 0.5f //canonball hitbox
            });

            aim.HasTarget = false;
            rotation.desiredPosition = float3.zero;
    }
}
