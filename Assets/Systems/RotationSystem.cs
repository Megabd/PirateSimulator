using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

partial struct RotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float3 up = math.up();
        float deltaTime = SystemAPI.Time.DeltaTime;

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

            if (hasTarget)
            {
                
                float3 dir = toTarget * math.rsqrt(lenSq); // normalized

                float2 flat = new float2(toTarget.x, toTarget.z);
                float targetYaw = math.atan2(flat.x, flat.y);
                quaternion yaw = quaternion.RotateY(targetYaw);
                goalRot = yaw;

                /*
                float2 flat = new float2(toTarget.x, toTarget.z);
                float targetYaw = math.atan2(flat.x, flat.y);
                quaternion yaw = quaternion.RotateY(targetYaw);
                quaternion tilt = quaternion.Euler(math.radians(90f), 0f, 0f);
                goalRot = math.mul(yaw, tilt);*/
                //Debug.Log("happens?");
            }
            else
            {
                // No target -> rotate back toward starting rotation
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

            transform.ValueRW.Rotation = goalRot;
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
