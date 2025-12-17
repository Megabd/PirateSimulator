using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]

partial struct CalcAimTarget : ISystem
{
    CollisionFilter filter;

    public Unity.Mathematics.Random rand;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        filter = new CollisionFilter
        {
            BelongsTo   = 1 << 0,
            CollidesWith = 1 << 1,
            GroupIndex  = 0
        };

        rand = new Unity.Mathematics.Random(146361515);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var physicsWorld          = physicsWorldSingleton.PhysicsWorld;

        var teamLookup      = SystemAPI.GetComponentLookup<TeamComponent>(true);
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var shipSenseLookup = SystemAPI.GetComponentLookup<ShipSense>(true);

        float dt = SystemAPI.Time.DeltaTime;
        float elapsedTime = (float)SystemAPI.Time.ElapsedTime;
        var config  = SystemAPI.GetSingleton<Config>();

        var job = new CalcAimTargetJob
        {
            dt = dt,
            elapsedTime = elapsedTime,
            filter = filter,
            physicsWorld = physicsWorld,
            transformLookup = transformLookup,
            teamLookup = teamLookup,
            shipSenseLookup = shipSenseLookup,
            rand = rand
        };

        if (config.ScheduleParallel)
        {
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
        else if (config.Schedule)
        {
            state.Dependency = job.Schedule(state.Dependency);
        }
        else
        {
            state.Dependency.Complete();
            job.Run();
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}

// Finds target for cannons. Raycast and lock on to enemy hit, start shooting warmup.
[BurstCompile]
public partial struct CalcAimTargetJob : IJobEntity
{
    public CollisionFilter filter;
    public float dt;                
    public float elapsedTime;      
    public Unity.Mathematics.Random rand;

    [ReadOnly] public PhysicsWorld physicsWorld;
    [ReadOnly] public ComponentLookup<TeamComponent> teamLookup;
    [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;
    [ReadOnly] public ComponentLookup<ShipSense> shipSenseLookup;

    void Execute(ref RotationComponent rotation, in LocalToWorld toWorld, ref Aim aim, in TeamComponent team, in CannonData data)
    {
        // Follow current target if we have one
        if (aim.HasTarget)
        {
            rotation.desiredPosition = aim.TargetPosition;
            return;
        }

        // Skip timer
        if (elapsedTime < aim.NextRaycastTime)
            return;
        aim.NextRaycastTime = elapsedTime + rand.NextFloat(0.4f, 0.8f);

        float3 pos     = toWorld.Position;
        float3 forward = toWorld.Forward;
        float3 bestTarget = float3.zero;

        RaycastInput rayInput = new RaycastInput
        {
            Start  = pos,
            End    = pos + forward * data.SenseDistance,
            Filter = filter
        };

        if (physicsWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
        {

            var hitEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;

            var teamComp = teamLookup[hitEntity];
            if (teamComp.redTeam == team.redTeam)
            {
                rotation.desiredPosition = bestTarget;
                return;
            }

             var otherTransform = transformLookup[hitEntity];

            float3 targetPosNow = otherTransform.Position;
            float3 toTarget     = targetPosNow - pos;
            float dist          = math.length(toTarget);

            if (dist > 0f)
            {
                float3 moveDir    = otherTransform.Forward();
                float3 targetVel  = moveDir * shipSenseLookup[hitEntity].ShipSpeed;
                float timeToHit   = dist / data.CannonballSpeed + data.ShootWarmupTime;
                float3 predictedPos = targetPosNow + targetVel * timeToHit;

                bestTarget = predictedPos;

                aim.HasTarget      = true;
                aim.TargetPosition = bestTarget;
                aim.ShootTimeLeft  = data.ShootWarmupTime;
            }
        }

        rotation.desiredPosition = bestTarget;
    }
}
