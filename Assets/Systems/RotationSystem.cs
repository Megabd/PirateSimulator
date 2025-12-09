using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.LightTransport;
//Debug.DrawLine(worldPos.ValueRO.Position, rotation.ValueRO.desiredPosition);
partial struct RotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) {
       //Time.timeScale = 0.1f;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        var config = SystemAPI.GetSingleton<Config>();

        if (config.ScheduleParallel)
        {
            new RotationJob
            {
                deltaTime = deltaTime
            }.ScheduleParallel();
        }

        else if (config.Schedule)
        {
            new RotationJob
            {
                deltaTime = deltaTime
            }.Schedule();
        }

        else {
            foreach (var (transform, rotation, worldPos)
                    in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationComponent>, RefRO<LocalToWorld>>())
            {
                var lt = transform.ValueRO;
                quaternion startRot = rotation.ValueRO.startRotation;
                float maxAngle = rotation.ValueRO.maxTurnAngle;   // max degrees from startRot
                float turnSpeed = rotation.ValueRO.turnSpeed;     // max degrees per second

                float3 toTarget = rotation.ValueRO.desiredPosition - worldPos.ValueRO.Position;
                float lenSq = math.lengthsq(toTarget);
                // No target if desiredPosition == current position
                bool hasTarget = lenSq > 1e-6f;

                // Rotation to desired position with no limits
                quaternion goalRot;

                if ((rotation.ValueRO.desiredPosition.x == 0 && rotation.ValueRO.desiredPosition.y == 0 && rotation.ValueRO.desiredPosition.z == 0))
                {
                    hasTarget = false;
                }

                if (hasTarget)
                {
                    float3 dir = toTarget * math.rsqrt(lenSq); // normalized
                    float2 flat = new float2(toTarget.x, toTarget.z);
                    float targetYaw = math.atan2(flat.x, flat.y);
                    quaternion yaw = quaternion.RotateY(targetYaw);
                    goalRot = yaw;

                }
                else
                {
                    // No target -> rotate back toward starting rotation
                    //Debug.Log("yes");
                    goalRot = startRot;
                }

                lt.Rotation = RotateLimited(
                    currentRot: lt.Rotation,
                    targetRot: goalRot,
                    startRot: startRot,
                    maxAngle: maxAngle,
                    turnSpeed: turnSpeed,
                    deltaTime: deltaTime
                );

                transform.ValueRW.Rotation = lt.Rotation;
            }
        }
    }

    // Clamp to cone around startRot and to turnSpeed * deltaTime
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
        // Lookups so we can get parent & parent's LocalToWorld
        var parentLookup = SystemAPI.GetComponentLookup<Parent>(true);
        var parentLtwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);

        foreach (var (transform, rotation, worldPos, entity)
                 in SystemAPI.Query<
                        RefRW<LocalTransform>,
                        RefRO<RotationComponent>,
                        RefRO<LocalToWorld>>()
                     .WithEntityAccess())
        {
            //Debug.DrawLine(worldPos.ValueRO.Position, rotation.ValueRO.desiredPosition);
            var lt = transform.ValueRO;
            quaternion startRot = rotation.ValueRO.startRotation;   // local-space default
            float maxAngle = rotation.ValueRO.maxTurnAngle;
            float turnSpeed = rotation.ValueRO.turnSpeed;

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

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}



[BurstCompile]
public partial struct RotationJob : IJobEntity
{
    public float deltaTime;
    void Execute(Entity e, ref LocalTransform transform,  ref RotationComponent rotation, ref LocalToWorld worldPos)
    {
        var lt = transform;
            quaternion startRot = rotation.startRotation;
            float maxAngle = rotation.maxTurnAngle;   // max degrees from startRot
            float turnSpeed = rotation.turnSpeed;     // max degrees per second

            float3 toTarget = rotation.desiredPosition - worldPos.Position;
            float lenSq = math.lengthsq(toTarget);

            bool hasTarget = lenSq > 1e-6f &&
                             !math.all(rotation.ValueRO.desiredPosition == float3.zero);

            if (rotation.desiredPosition.x == 0 && rotation.desiredPosition.y == 0 && rotation.desiredPosition.z == 0)
            // 1) Figure out the parent's world rotation (or identity if no parent)
            quaternion parentWorldRot = quaternion.identity;
            if (parentLookup.HasComponent(entity))
            {
                var parent = parentLookup[entity].Value;
                if (parentLtwLookup.HasComponent(parent))
                {
                    parentWorldRot = parentLtwLookup[parent].Rotation;
                }
            }

            // 2) Build the desired world-space rotation (yaw toward target)
            quaternion worldGoalRot;
            if (hasTarget)
            {
                float2 flat = new float2(toTarget.x, toTarget.z);
                float targetYaw = math.atan2(flat.x, flat.y);
                worldGoalRot = quaternion.RotateY(targetYaw);
            }
            else
            {
                // no target -> just go back to start rotation in world space
                // startRot is local, so worldStart = parentWorldRot * startRot
                worldGoalRot = math.mul(parentWorldRot, startRot);
            }

            // 3) Convert world goal to local goal so LocalTransform is correct
            quaternion localGoalRot = math.mul(math.inverse(parentWorldRot), worldGoalRot);

            // 4) Apply your limits in LOCAL space (startRot/maxAngle/turnSpeed are local)
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

