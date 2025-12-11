using Unity.Burst;
using Unity.Burst.Intrinsics;
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
    Unity.Mathematics.Random rand;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        filter = new CollisionFilter
        {
            BelongsTo   = 1 << 0,
            CollidesWith = 1 << 1,
            GroupIndex  = 0
        };
        Unity.Mathematics.Random rand = new Unity.Mathematics.Random(5u);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        
        var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var physicsWorld          = physicsWorldSingleton.PhysicsWorld;

        var teamLookup      = SystemAPI.GetComponentLookup<TeamComponent>(true);
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

        float dt = SystemAPI.Time.DeltaTime;
        float elapsedTime = (float)SystemAPI.Time.ElapsedTime;
        var config  = SystemAPI.GetSingleton<Config>();

        var job = new CalcAimTargetJob
        {
            dt = dt,
            elapsedTime = elapsedTime,
            rand = rand,
            filter = filter,
            physicsWorld = physicsWorld,
            transformLookup = transformLookup,
            teamLookup = teamLookup

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
            job.Run();
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}

[BurstCompile]
public partial struct CalcAimTargetJob : IJobEntity
{
    public CollisionFilter filter;
    public float dt;                // for ShootTimeLeft etc, if needed
    public float elapsedTime;     // absolute time this frame

    public Unity.Mathematics.Random rand;

    [ReadOnly] public PhysicsWorld physicsWorld;
    [ReadOnly] public ComponentLookup<TeamComponent> teamLookup;
    [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;

    void Execute(Entity e, ref RotationComponent rotation, in LocalToWorld toWorld, ref Aim aim)
    {
        // Let rotation always follow current target if we have one
        if (aim.HasTarget)
        {
            rotation.desiredPosition = aim.TargetPosition;
            return;
        }

        // Not yet time to raycast again for this cannon
        if (elapsedTime < aim.NextRaycastTime)
            return;

        // Decide the next time *before* doing the raycast, so even failed casts keep cadence.
        aim.NextRaycastTime = elapsedTime + aim.RayCastInterval;

        float3 pos     = toWorld.Position;
        float3 forward = toWorld.Forward;
        float3 bestTarget = float3.zero;

        RaycastInput rayInput = new RaycastInput
        {
            Start  = pos,
            End    = pos + forward * CannonConfig.SenseDistance,
            Filter = filter
        };

        if (physicsWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
        {
            var hitEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;

            var teamComp = teamLookup[hitEntity];
            var team     = teamLookup[e];

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
                float3 targetVel  = moveDir * ShipConfig.ShipSpeed;
                float timeToHit   = dist / CannonConfig.CannonballSpeed + CannonConfig.CannonballLifeTime;
                float3 predictedPos = targetPosNow + targetVel * timeToHit;

                bestTarget = predictedPos;

                aim.HasTarget      = true;
                aim.TargetPosition = bestTarget;
                aim.ShootTimeLeft  = CannonConfig.CannonballLifeTime;
            }
        }

        rotation.desiredPosition = bestTarget;
    }
}
