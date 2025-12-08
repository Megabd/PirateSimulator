using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Unity.Collections;

partial struct CalcAimTarget : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

        CollisionFilter filter = new CollisionFilter
        {
            BelongsTo = 1u << 0,
            CollidesWith = 1u << 1,
            GroupIndex = 0
        };

        var teamLookup = SystemAPI.GetComponentLookup<TeamComponent>(true);
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var speedLookup = SystemAPI.GetComponentLookup<SpeedComponent>(true);

        float dt = SystemAPI.Time.DeltaTime;

        var config = SystemAPI.GetSingleton<Config>();

        if (config.ScheduleParallel)
        {
            new CalcAimTargetJob
            {
                
                dt = dt,
                filter = filter,
                physicsWorld = physicsWorld,
                teamLookup = teamLookup,
                transformLookup = transformLookup,
                speedLookup = speedLookup

            }.ScheduleParallel();
        }

        else if (config.Schedule)
        {
            new CalcAimTargetJob
            {
                dt = dt,
                filter = filter,
                physicsWorld = physicsWorld,
                teamLookup = teamLookup,
                transformLookup = transformLookup,
                speedLookup = speedLookup

            }.Schedule();
        }

        else {

        foreach (var (transform, rotation, team, sense, toWorld, timer, Aim) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<RotationComponent>, RefRO<TeamComponent>, RefRO<CanonSenseComponent>, RefRO<LocalToWorld>, RefRO<CooldownTimer>, RefRW<Aim>>())
        {
            if (timer.ValueRO.TimeLeft > 1f)
            {
                Aim.ValueRW.HasTarget = false;
                rotation.ValueRW.desiredPosition = float3.zero;
                continue;
            }

            if (Aim.ValueRW.HasTarget)
            {
                rotation.ValueRW.desiredPosition = Aim.ValueRW.TargetPosition;
                continue;
            }

            Aim.ValueRW.TimeLeft -= dt;
            if (Aim.ValueRW.TimeLeft > 0f)
                continue;
            Aim.ValueRW.TimeLeft = Aim.ValueRW.Interval;

            float3 pos = toWorld.ValueRO.Position;
            float3 forward = toWorld.ValueRO.Forward;
            float3 bestTarget = float3.zero;
            RaycastInput rayInput = new RaycastInput
            {
                Start = pos,
                End = pos + forward * sense.ValueRO.senseDistance,
                Filter = filter
            };

            if (physicsWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
            {
                var hitEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;

                if (!teamLookup.HasComponent(hitEntity) ||
                    !transformLookup.HasComponent(hitEntity) ||
                    !speedLookup.HasComponent(hitEntity))
                {
                    rotation.ValueRW.desiredPosition = bestTarget;
                    continue;
                }

                var teamComp = teamLookup[hitEntity];

                if (teamComp.redTeam == team.ValueRO.redTeam)
                {
                    rotation.ValueRW.desiredPosition = bestTarget;
                    continue;
                }

                var otherTransform = transformLookup[hitEntity];
                var speed = speedLookup[hitEntity];

                float projSpeed = sense.ValueRO.cannonballSpeed;
                float3 toTarget = otherTransform.Position - pos;
                float dist = math.length(toTarget);

                if (projSpeed > 0f && dist > 0f)
                {
                    float3 moveDir = otherTransform.Forward();
                    float3 targetVel = moveDir * speed.speed;
                    float timeToHit = dist / projSpeed;
                    float3 predictedPos = otherTransform.Position + targetVel * timeToHit;
                    bestTarget = predictedPos;

                    Aim.ValueRW.HasTarget = true;
                    Aim.ValueRW.TargetPosition = bestTarget;
                }
            }

            rotation.ValueRW.desiredPosition = bestTarget;
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
    [ReadOnly]
    public ComponentLookup<SpeedComponent> speedLookup;

    void Execute(Entity e,  ref RotationComponent rotation, ref CanonSenseComponent sense, ref LocalToWorld toWorld, ref CooldownTimer timer, ref Aim Aim)
    {
        if (timer.TimeLeft > 1f)
            {
                Aim.HasTarget = false;
                rotation.desiredPosition = float3.zero;
            }

        else if (Aim.HasTarget)
        {
            rotation.desiredPosition = Aim.TargetPosition;
        }
        else {

            Aim.TimeLeft -= dt;
            if (Aim.TimeLeft > 0f)
            {
                
            }
            else {
                Aim.TimeLeft = Aim.Interval;

                float3 pos = toWorld.Position;
                float3 forward = toWorld.Forward;
                float3 bestTarget = float3.zero;
                RaycastInput rayInput = new RaycastInput
                {
                    Start = pos,
                    End = pos + forward * sense.senseDistance,
                    Filter = filter
                };

                if (physicsWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
                {
                    var hitEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;

                    if (!teamLookup.HasComponent(hitEntity) ||
                        !transformLookup.HasComponent(hitEntity) ||
                        !speedLookup.HasComponent(hitEntity))
                    {
                        rotation.desiredPosition = bestTarget;
                    }

                    else
                    {
                        var teamComp = teamLookup[hitEntity];
                        var team = teamLookup[e];
                        if (teamComp.redTeam == team.redTeam)
                        {
                            rotation.desiredPosition = bestTarget;
                        }
                        else
                        {
                            var otherTransform = transformLookup[hitEntity];
                            var speed = speedLookup[hitEntity];

                            float projSpeed = sense.cannonballSpeed;
                            float3 toTarget = otherTransform.Position - pos;
                            float dist = math.length(toTarget);

                            if (projSpeed > 0f && dist > 0f)
                            {
                                float3 moveDir = otherTransform.Forward();
                                float3 targetVel = moveDir * speed.speed;
                                float timeToHit = dist / projSpeed;
                                float3 predictedPos = otherTransform.Position + targetVel * timeToHit;
                                bestTarget = predictedPos;

                                Aim.HasTarget = true;
                                Aim.TargetPosition = bestTarget;
                            }
                        }

                    
                    }
                }

                rotation.desiredPosition = bestTarget;
            }
        }
    }
}
