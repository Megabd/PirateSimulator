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

            float3 toTarget = rotation.ValueRO.desiredPosition - worldPos.ValueRO.Position;
            float lenSq = math.lengthsq(toTarget);

            bool hasTarget = lenSq > 1e-6f &&
                             !math.all(rotation.ValueRO.desiredPosition == float3.zero);

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

            transform.ValueRW.Rotation = lt.Rotation;
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
