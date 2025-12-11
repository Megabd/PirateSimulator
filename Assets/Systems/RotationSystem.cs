using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.LightTransport;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CalcAimTarget))]
[UpdateBefore(typeof(ShootingBallSystem))]
partial struct RotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var config = SystemAPI.GetSingleton<Config>();

        var parentLookup = SystemAPI.GetComponentLookup<Parent>(true);
        var parentLtwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);

        var job = new RotationJob
        {
            parentLookup = parentLookup,
            parentLtwLookup = parentLtwLookup,
            deltaTime = deltaTime
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
public partial struct RotationJob : IJobEntity
{
    public float deltaTime;

    [ReadOnly] public ComponentLookup<Parent> parentLookup;
    [ReadOnly] public ComponentLookup<LocalToWorld> parentLtwLookup;

    void Execute(Entity e, ref LocalTransform transform, ref RotationComponent rotation)
    {
        var lt = transform;

        quaternion startRot = rotation.startRotation;
        float maxAngle = rotation.maxTurnAngle;
        float turnSpeed = rotation.turnSpeed;

        // Compute parent world rotation and our world position
        quaternion parentWorldRot = quaternion.identity;
        float3 worldPos;

        if (parentLookup.HasComponent(e))
        {
            var parent = parentLookup[e].Value;
            if (parentLtwLookup.HasComponent(parent))
            {
                var parentLtw = parentLtwLookup[parent];
                parentWorldRot = parentLtw.Rotation;
                worldPos = parentLtw.Position + math.rotate(parentWorldRot, transform.Position);
            }
            else
            {
                worldPos = transform.Position; // fallback
            }
        }
        else
        {
            worldPos = transform.Position; // root
        }

        float3 toTarget = rotation.desiredPosition - worldPos;
        float lenSq = math.lengthsq(toTarget);
        bool hasTarget =
            lenSq > 1e-6f &&
            !math.all(rotation.desiredPosition == float3.zero);

        quaternion worldGoalRot;
        if (hasTarget)
        {
            float2 flat = new float2(toTarget.x, toTarget.z);
            float targetYaw = math.atan2(flat.x, flat.y);
            worldGoalRot = quaternion.RotateY(targetYaw);
        }
        else
        {
            worldGoalRot = math.mul(parentWorldRot, startRot);
        }

        quaternion localGoalRot = math.mul(math.inverse(parentWorldRot), worldGoalRot);

        lt.Rotation = RotateLimited(
            currentRot: lt.Rotation,
            targetRot: localGoalRot,
            startRot: startRot,
            maxAngle: maxAngle,
            turnSpeed: turnSpeed,
            deltaTime: deltaTime
        );

        transform.Rotation = lt.Rotation;
    }

    static quaternion RotateLimited(
        quaternion currentRot,
        quaternion targetRot,
        quaternion startRot,
        float maxAngle,
        float turnSpeed,
        float deltaTime)
    {
        float fromStart = AngleBetween(startRot, targetRot);
        if (maxAngle > 0f && fromStart > maxAngle)
        {
            float tCone = maxAngle / fromStart;
            targetRot = math.slerp(startRot, targetRot, tCone);
        }

        float angleToGoal = AngleBetween(currentRot, targetRot);
        float maxStep = turnSpeed * deltaTime;

        if (maxStep > 0f && angleToGoal > maxStep)
        {
            float tTurn = maxStep / angleToGoal;
            return math.slerp(currentRot, targetRot, tTurn);
        }

        return targetRot;
    }

    static float AngleBetween(quaternion a, quaternion b)
    {
        float dot = math.dot(a.value, b.value);
        dot = math.clamp(dot, -1f, 1f);
        return 2f * math.degrees(math.acos(math.abs(dot)));
    }
}


