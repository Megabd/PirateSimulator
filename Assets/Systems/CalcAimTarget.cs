using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

partial struct CalcAimTarget : ISystem
{
    CollisionFilter filter;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        filter = new CollisionFilter
        {
            BelongsTo = 1 << 0,
            CollidesWith = 1 << 1,
            GroupIndex = 0
        };

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

        var teamLookup = SystemAPI.GetComponentLookup<TeamComponent>(true);
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

        float dt = SystemAPI.Time.DeltaTime;

        var config = SystemAPI.GetSingleton<Config>();

        if (config.ScheduleParallel)
        {
            new CalcAimTargetJob
            {
                
                dt = dt,
                filter = filter,
                physicsWorld = physicsWorld,
                transformLookup = transformLookup,
                teamLookup = teamLookup

            }.ScheduleParallel();
        }

        else if (config.Schedule)
        {
            new CalcAimTargetJob
            {
                dt = dt,
                filter = filter,
                physicsWorld = physicsWorld,
                transformLookup = transformLookup,
                teamLookup = teamLookup

            }.Schedule();
        }

        else {
        foreach (var (rotation, team, toWorld, aim) in SystemAPI.Query<RefRW<RotationComponent>, RefRO<TeamComponent>, RefRO<LocalToWorld>, RefRW<Aim>>())
        {

            var aimRW = aim.ValueRW;

            if (aimRW.HasTarget)
            {
                rotation.ValueRW.desiredPosition = aimRW.TargetPosition;
                continue;
            }

            aimRW.RayCastTimeLeft -= dt;
            if (aimRW.RayCastTimeLeft > 0f)
                {
                    aim.ValueRW = aimRW;
                    continue;
                }
            aimRW.RayCastTimeLeft = aimRW.RayCastInterval;

            float3 pos = toWorld.ValueRO.Position;
            float3 forward = toWorld.ValueRO.Forward;
            float3 bestTarget = float3.zero;
            RaycastInput rayInput = new RaycastInput
            {
                Start = pos,
                End = pos + forward * CannonConfig.SenseDistance,
                Filter = filter
            };

            if (physicsWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
            {
                var hitEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;

                var teamComp = teamLookup[hitEntity];

                if (teamComp.redTeam == team.ValueRO.redTeam)
                {
                    rotation.ValueRW.desiredPosition = bestTarget;
                    continue;
                }
                var otherTransform = transformLookup[hitEntity];

                float3 targetPosNow = otherTransform.Position;
                float3 toTarget = targetPosNow - pos;
                float dist = math.length(toTarget);

                if (dist > 0f)
                {
                    float3 moveDir = otherTransform.Forward();
                    float3 targetVel = moveDir * ShipConfig.ShipSpeed;
                    float timeToHit = dist / CannonConfig.CannonballSpeed + CannonConfig.ShootWarmupTime;
                    float3 predictedPos = targetPosNow + targetVel * timeToHit;
                    bestTarget = predictedPos;

                    aimRW.HasTarget = true;
                    aimRW.TargetPosition = bestTarget;

                    aimRW.ShootTimeLeft = CannonConfig.ShootWarmupTime;
                }
            }

            rotation.ValueRW.desiredPosition = bestTarget;
            aim.ValueRW = aimRW;
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}



[BurstCompile]
public partial struct CalcAimTargetJob : IJobEntity
{
    public float dt;
    public CollisionFilter filter;

    [ReadOnly]
    public PhysicsWorld physicsWorld;

    [ReadOnly]
    public ComponentLookup<TeamComponent> teamLookup;
    [ReadOnly]
    public ComponentLookup<LocalTransform> transformLookup;

    void Execute(Entity e,  ref RotationComponent rotation, ref LocalToWorld toWorld, ref Aim aim)
    {
        if (aim.HasTarget)
            {
                rotation.desiredPosition = aim.TargetPosition;
                return;
            }

        aim.RayCastTimeLeft -= dt;
        if (aim.RayCastTimeLeft > 0f)
        {
            return;
        }

        aim.RayCastTimeLeft = aim.RayCastInterval;

        float3 pos = toWorld.Position;
        float3 forward = toWorld.Forward;
        float3 bestTarget = float3.zero;
        RaycastInput rayInput = new RaycastInput
        {
            Start = pos,
            End = pos + forward * CannonConfig.SenseDistance,
            Filter = filter
        };

        if (physicsWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
        {
            var hitEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;


            var teamComp = teamLookup[hitEntity];
            var team = teamLookup[e];

            if (teamComp.redTeam == team.redTeam)
            {
                rotation.desiredPosition = bestTarget;
                return;
            }

            var otherTransform = transformLookup[hitEntity];

            float3 targetPosNow = otherTransform.Position;
            float3 toTarget = targetPosNow - pos;
            float dist = math.length(toTarget);

            if (dist > 0f)
            {
                float3 moveDir = otherTransform.Forward();
                float3 targetVel = moveDir * ShipConfig.ShipSpeed;
                float timeToHit = dist / CannonConfig.CannonballSpeed + CannonConfig.CannonballLifeTime;
                float3 predictedPos = otherTransform.Position + targetVel * timeToHit;
                bestTarget = predictedPos;

                aim.HasTarget = true;
                aim.TargetPosition = bestTarget;

                aim.ShootTimeLeft = CannonConfig.CannonballLifeTime;
            }
        }

        rotation.desiredPosition = bestTarget;
    }
}
