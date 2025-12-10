using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.LightTransport;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CalcAimTarget))]         // AFTER aiming
[UpdateBefore(typeof(ShootingBallSystem))]   // BEFORE shooting
partial struct RotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        //Time.timeScale = 0.1f;
    }


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

    void Execute(Entity e, ref LocalTransform transform, ref RotationComponent rotation, in LocalToWorld worldPos)
    {
        var lt = transform;
        quaternion startRot = rotation.startRotation;
        float maxAngle = rotation.maxTurnAngle;
        float turnSpeed = rotation.turnSpeed;
        float3 toTarget = rotation.desiredPosition - worldPos.Position;
        float lenSq = math.lengthsq(toTarget);
        bool hasTarget = lenSq > 1e-6f && !math.all(rotation.desiredPosition == float3.zero);

        quaternion parentWorldRot = quaternion.identity;
        if (parentLookup.HasComponent(e))
        {
            var parent = parentLookup[e].Value;
            if (parentLtwLookup.HasComponent(parent))
                parentWorldRot = parentLtwLookup[parent].Rotation;
        }

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

        // 1) Clamp to maxAngle cone from startRot (if maxAngle > 0)
        float fromStart = AngleBetween(startRot, targetRot);
        if (maxAngle > 0f && fromStart > maxAngle)
        {
            float tCone = maxAngle / fromStart;   // 0..1
            targetRot = math.slerp(startRot, targetRot, tCone);
        }
        // 2) Clamp turn speed (deg/sec)
        float angleToGoal = AngleBetween(currentRot, targetRot);
        float maxStep = turnSpeed * deltaTime;    // degrees this frame

        if (maxStep > 0f && angleToGoal > maxStep)
        {
            float tTurn = maxStep / angleToGoal;  // 0..1
            return math.slerp(currentRot, targetRot, tTurn);
        }

        return targetRot;
    }

    // Angle between two quaternions, in degrees
    static float AngleBetween(quaternion a, quaternion b)
    {
        float dot = math.dot(a.value, b.value);
        dot = math.clamp(dot, -1f, 1f);
        return 2f * math.degrees(math.acos(math.abs(dot)));
    }
}

